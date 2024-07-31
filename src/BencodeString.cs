using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitTorrentFeatures
{
    [JsonConverter(typeof(BencodeStringJsonConverter))]
    public class BencodeString(byte[] bytes)
    {
        public byte[] Bytes { get; } = bytes;
        public int Length { get => Bytes.Length; }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(Bytes);
        }

        public static explicit operator string(BencodeString bencodeString) => bencodeString.ToString();

        public static explicit operator BencodeString(string s) => new(Encoding.UTF8.GetBytes(s));
    }

    public class BencodeStringJsonConverter : JsonConverter<BencodeString>
    {
        public override BencodeString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, BencodeString value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
