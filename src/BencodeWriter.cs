using System.Globalization;
using System.Text;

namespace BitTorrentFeatures
{
    public class BencodeWriter(Stream baseStream)
    {
        public Stream BaseStream { get; } = baseStream;

        private void Write(object value)
        {
            BaseStream.Write(Encoding.UTF8.GetBytes(Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""));
        }

        public void WriteString(string value)
        {
            Write(value.Length);
            Write(":");
            Write(value);
        }

        public void WriteString(BencodeString value)
        {
            Write(value.Length);
            Write(":");
            Write(value);
        }

        public void WriteInt(long value)
        {
            Write("i");
            Write(value);
            Write("e");
        }

        public void WriteList(IEnumerable<object> value)
        {
            Write("l");
            foreach (object item in value)
            {
                WriteAny(item);
            }
            Write("e");
        }

        public void WriteDictionary(IDictionary<string, object> value)
        {
            Write("d");
            foreach (KeyValuePair<string, object> item in value)
            {
                WriteString(item.Key);
                WriteAny(item.Value);
            }
            Write("e");
        }

        public void WriteAny(object value)
        {
            if (value is BencodeString bs)
            {
                WriteString(bs);
            }
            else if (value is string s)
            {
                WriteString(s);
            }
            else if (value is int or long or short)
            {
                WriteInt((long)value);
            }
            else if (value is IDictionary<string, object> dict)
            {
                WriteDictionary(dict);
            }
            else if (value is IEnumerable<object> list)
            {
                WriteList(list);
            }
            else
            {
                throw new Exception($"invalid type for value: {value.GetType().FullName}");
            }
        }
    }
}
