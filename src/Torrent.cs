using System.Collections.Frozen;
using System.Security.Cryptography;

namespace BitTorrentFeatures
{
    public class Torrent
    {
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
    }
}
