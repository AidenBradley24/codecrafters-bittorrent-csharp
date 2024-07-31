using System.Text;

namespace BitTorrentFeatures
{
    public class BencodeReader(Stream baseStream)
    {
        public Stream BaseStream { get => br.BaseStream; }
        private readonly BinaryReader br = new(baseStream, Encoding.UTF8);

        public string ReadString()
        {
            int length = int.Parse(ReadUntilChar(':'));
            return Encoding.UTF8.GetString(br.ReadBytes(length));
        }

        public long ReadInt()
        {
            BaseStream.Position++; // skip 'i'
            return long.Parse(ReadUntilChar('e')); 
        }

        public List<object> ReadList()
        {
            List<object> list = [];
            BaseStream.Position++;
            while ((char)br.PeekChar() != 'e')
            {
                list.Add(ReadAny());
            }
            BaseStream.Position++; // skip 'e'
            return list;
        }

        public Dictionary<string, object> ReadDictionary()
        {
            BaseStream.Position++; // skip 'd'
            Dictionary<string, object> result = [];
            while ((char)br.PeekChar() != 'e')
            {
                string key = ReadString();
                object value = ReadAny();
                result.Add(key, value);
            }
            return result;
        }

        public object ReadAny()
        {
            char first = (char)br.PeekChar();
            if (char.IsDigit(first))
            {
                return ReadString();
            }
            else if (first == 'i')
            {
                return ReadInt();
            }
            else if (first == 'l')
            {
                return ReadList();
            }
            else if (first == 'd')
            {
                return ReadDictionary();
            }
            else
            {
                throw new Exception("invalid bencode value");
            }
        }

        private string ReadUntilChar(char c)
        {
            List<char> chars = [];
            while (br.PeekChar() != c)
            {
                chars.Add(br.ReadChar());
            }
            BaseStream.Position++; // skip seeked char
            return string.Concat(chars);
        }
    }
}
