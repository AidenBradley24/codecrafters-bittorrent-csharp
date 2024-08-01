namespace BitTorrentFeatures
{
    public static class MiscUtils
    {
        public static string HashHex(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public static byte[] BigEndian(ReadOnlySpan<byte> span)
        {
            byte[] result = span.ToArray();
            if (BitConverter.IsLittleEndian) Array.Reverse(result);
            return result;
        }
    }
}
