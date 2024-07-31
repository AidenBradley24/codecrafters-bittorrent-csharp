using System.Collections;
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
        public string InfoHash { get; }
        public long PieceLength { get => (long)Info["piece length"]; }
        public IEnumerable<string> Pieces
        {
            get 
            {
                string pieces = (string)(BencodeString)Info["pieces"];
                return Enumerable.Range(0, pieces.Length / 40).Select(i => pieces.Substring(i * 40, 40));
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
            byte[] hash = SHA1.HashData(bytes);
            InfoHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public static Torrent ReadStream(Stream stream)
        {
            return new Torrent(stream);
        }
    }
}
