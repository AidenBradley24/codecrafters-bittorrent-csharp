using System.Net.Sockets;
using System.Security.Cryptography;

namespace BitTorrentFeatures
{
    public class TorrentClient(Torrent torrent, TcpClient tcp, string peerID) : IDisposable
    {
        public Torrent Tor { get; } = torrent;
        public string PeerID { get; } = peerID;

        private readonly SemaphoreSlim semaphore = new(5); // allowing 5 requests pending
        private readonly TcpClient tcp = tcp;
        private readonly NetworkStream ns = tcp.GetStream();

        private bool ready = false;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            tcp.Dispose();
        }

        private void GetReady()
        {
            var message = PeerMessage.Recieve(ns);
            if (message.Type != PeerMessage.Id.Bitfield) throw new Exception("not a bitfield");
            PeerMessage.Send(ns, PeerMessage.Id.Interested, null);
            message = PeerMessage.Recieve(ns);
            if (message.Type != PeerMessage.Id.Unchoke) throw new Exception("not a unchoke");
            ready = true;
        }

        public void DownloadPiece(FileInfo pieceFile, int pieceIndex)
        {
            if (!ready) GetReady();

            const uint BLOCK_LENGTH = 16 * 1024;
            uint PIECE_LENGTH = (uint)Math.Min(Tor.Length - (pieceIndex * Tor.PieceLength), Tor.PieceLength);
            int blockCount = (int)Math.Ceiling((double)PIECE_LENGTH / BLOCK_LENGTH);

            var recieveTask = RecieveBlocks(ns, blockCount);
            uint current = 0;
            while (current < PIECE_LENGTH)
            {
                uint next = current + BLOCK_LENGTH;
                uint length = next < PIECE_LENGTH ? BLOCK_LENGTH : PIECE_LENGTH - current;
                Console.WriteLine($"block: {current}, length: {length}");
                var request = PeerMessage.Request(pieceIndex, current, length);
                semaphore.Wait();
                request.Send(ns);
                current = next;
            }

            recieveTask.Wait();
            byte[] data = recieveTask.Result;
            byte[] hash = Tor.Pieces.ElementAt(pieceIndex);
            if (!SHA1.HashData(data).SequenceEqual(hash)) throw new Exception("Hash does not match for piece " + pieceIndex);
            using var fs = pieceFile.OpenWrite();
            fs.Write(data);
        }

        private async Task<byte[]> RecieveBlocks(NetworkStream ns, int blockCount)
        {
            List<Block> blocks = [];
            for (int i = 0; i < blockCount; i++)
            {
                var response = await PeerMessage.RecieveAsync(ns);
                Block block = response.AsBlock();
                blocks.Add(block);
                semaphore.Release();
            }

            return Block.Combine(blocks);
        }
    }
}
