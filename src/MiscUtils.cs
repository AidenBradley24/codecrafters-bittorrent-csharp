namespace BitTorrentFeatures
{
    public static class MiscUtils
    {
        public static string Hex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public static byte[] BigEndian(ReadOnlySpan<byte> span)
        {
            byte[] result = span.ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(result);
            return result;
        }
    }
}
