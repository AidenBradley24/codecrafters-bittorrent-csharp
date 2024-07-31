﻿using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;

namespace BitTorrentFeatures
{
    public class Torrent
    {
        private readonly FrozenDictionary<string, object> dict;

        public string TrackerUrl { get => (string)dict["announce"]; }
        public FrozenDictionary<string, object> Info { get => (FrozenDictionary<string, object>) dict["info"]; }
        public long Length { get => (long)Info["length"]; }
        public string Name { get => (string)Info["name"]; }
        public string InfoHash { get; }

        private Torrent(Stream stream)
        {
            BencodeReader reader = new(stream);
            dict = reader.ReadDictionary().ToFrozenDictionary();

            // test
            stream.Position = 0;
            StreamReader sr = new StreamReader(stream);
            Console.WriteLine("EXPECTED (including whole)");
            Console.WriteLine(sr.ReadToEnd());
            Console.WriteLine("ACTUAL");

            using MemoryStream ms = new();
            BencodeWriter writer = new(ms);
            writer.WriteDictionary(Info);
            byte[] bytes = ms.ToArray();

            Console.WriteLine(Encoding.UTF8.GetString(bytes));

            byte[] hash = SHA1.HashData(bytes);
            InfoHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public static Torrent ReadStream(Stream stream)
        {
            return new Torrent(stream);
        }
    }
}
