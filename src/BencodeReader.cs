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
            return list;
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
