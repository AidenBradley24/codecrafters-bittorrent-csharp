using System.Collections.Frozen;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BitTorrentFeatures
{
    public class Torrent
    {
        const string MY_PEER_ID = "00112233445566778899";
        const string PROTOCOL = "BitTorrent protocol";

        private readonly FrozenDictionary<string, object> dict;

        public string TrackerUrl { get => (string)(BencodeString)dict["announce"]; }
        public FrozenDictionary<string, object> Info { get => (FrozenDictionary<string, object>) dict["info"]; }
        public long Length { get => (long)Info["length"]; }
        public string Name { get => (string)(BencodeString)Info["name"]; }
        public byte[] InfoHash { get; }
        public long PieceLength { get => (long)Info["piece length"]; }
        public IEnumerable<byte[]> Pieces
        {
            get 
            {
                BencodeString pieces = (BencodeString)Info["pieces"];
                return Enumerable.Range(0, pieces.Length / 20).Select(i => pieces.Bytes[(i*20)..(i*20+20)]);
            }
        }

        private Torrent(Stream stream)
        {
            BencodeReader reader = new(stream);
            dict = reader.ReadDictionary().ToFrozenDictionary();

            using MemoryStream ms = new();
            BencodeWriter writer = new(ms);
            writer.WriteDictionary(Info);
            byte[] bytes = ms.ToArray();
            InfoHash = SHA1.HashData(bytes);
        }

        public static Torrent ReadStream(Stream stream)
        {
            return new Torrent(stream);
        }

        private List<IPEndPoint>? myPeers = null;
        public List<IPEndPoint> Peers
        {
            get
            {
                if (myPeers != null) return myPeers;
                return FetchPeers();
            }
        }

        private List<IPEndPoint> FetchPeers()
        {
            myPeers = [];

            using HttpClient client = new();
            UriBuilder builder = new(TrackerUrl)
            {
                Query =
                $"info_hash=%{BitConverter.ToString(InfoHash).Replace('-', '%').ToLower()}&" +
                $"peer_id={MY_PEER_ID}&" +
                $"port=6881&" +
                $"uploaded=0&" +
                $"downloaded=0&" +
                $"left={Length}&" +
                $"compact=1"
            };

            var response = client.Send(new HttpRequestMessage(HttpMethod.Get, builder.Uri));
            Stream stream = response.Content.ReadAsStream();
            BencodeReader reader = new(stream);
            var dict = reader.ReadDictionary();
            ReadOnlySpan<byte> peers = ((BencodeString)dict["peers"]).Bytes;

            for (int i = 0; i < peers.Length; i += 6)
            {
                IPAddress address = new(peers[i..(i + 4)]);
                ushort portNumber = BitConverter.ToUInt16(MiscUtils.BigEndian(peers[(i + 4)..(i + 6)]));
                myPeers.Add(new IPEndPoint(address, portNumber));
            }

            return myPeers;
        }

        public TorrentClient PerformHandshake(IPEndPoint peer)
        {
            TcpClient client = new();
            client.Connect(peer);
            NetworkStream ns = client.GetStream();

            ns.WriteByte(Convert.ToByte(PROTOCOL.Length));
            ns.Write(Encoding.UTF8.GetBytes(PROTOCOL));
            for (int i = 0; i < 8; i++) ns.WriteByte(0);
            ns.Write(InfoHash);
            ns.Write(Encoding.UTF8.GetBytes(MY_PEER_ID));

            BinaryReader br = new(ns);
            byte len = br.ReadByte();
            string protocol = Encoding.UTF8.GetString(br.ReadBytes(len));
            if (protocol != PROTOCOL) throw new Exception("protocol does not match");
            br.ReadBytes(8);
            byte[] hash = br.ReadBytes(20);
            string peerID = MiscUtils.Hex(br.ReadBytes(20));
            return new TorrentClient(this, client, peerID);
        }

        public TorrentClient PerformHandshake()
        {
            return PerformHandshake(Peers.First());
        }

        public void Download(FileInfo finalFile)
        {
            // 1) get pieces
            int numberOfPieces = (int)Math.Ceiling((double)Length / PieceLength);
            Queue<int> pieces = new(Enumerable.Range(0, numberOfPieces));

            // 2) start connections
            const int MAX_CONNECTIONS = 5;
            int peerCount = Math.Min(MAX_CONNECTIONS, Peers.Count);
            TorrentClient[] connections = new TorrentClient[peerCount];
            for (int i = 0; i < peerCount; i++)
            {
                TorrentClient client = PerformHandshake(Peers.ElementAt(i));
                connections[i] = client;
            }

            // 3) download
            List<Task<FileInfo>> downloadTasks = [];
            DirectoryInfo downloadDir = Directory.CreateTempSubdirectory();
            foreach (TorrentClient client in connections) 
            {
                if (pieces.TryDequeue(out int piece))
                {
                    var task = PieceWorker(client, piece, downloadDir);
                    downloadTasks.Add(task);
                }
            }

            List<FileInfo> pieceFiles = [];
            while (downloadTasks.Count > 0)
            {
                Task.WaitAny(downloadTasks.ToArray());

                int completedIndex = downloadTasks.FindIndex(t => t.IsCompleted);
                Task<FileInfo> completedTask = downloadTasks[completedIndex];
                if (completedTask.IsFaulted)
                {
                    Console.WriteLine($"download failed on peer " + completedIndex);
                    pieces.Enqueue(completedIndex);
                }
                else
                {
                    Console.WriteLine($"download completed on peer " + completedIndex);
                    FileInfo result = completedTask.Result;
                    pieceFiles.Add(result);
                }

                if (pieces.TryDequeue(out int piece))
                {
                    var task = PieceWorker(connections[completedIndex], piece, downloadDir);
                    downloadTasks[completedIndex] = task;
                }
                else
                {
                    downloadTasks.RemoveAt(completedIndex);
                }
            }

            // 4) reconstruct file
            using FileStream fs = finalFile.OpenWrite();
            foreach (var pieceFile in pieceFiles.OrderBy(f => f.Name))
            {
                using FileStream pfs = pieceFile.OpenRead();
                pfs.CopyTo(fs);
            }

            // 5) cleanup
            downloadDir.Delete(true);
        }

        private static Task<FileInfo> PieceWorker(TorrentClient client, int piece, DirectoryInfo downloadDir)
        {
            FileInfo file = new(Path.Combine(downloadDir.FullName, piece.ToString()));
            try
            {
                var task = Task.Run(() => client.DownloadPiece(file, piece));
                task.Wait();
            }
            catch (Exception ex)
            {
                return Task.FromException<FileInfo>(ex);
            }
            return Task.FromResult(file);
        }
    }
}
