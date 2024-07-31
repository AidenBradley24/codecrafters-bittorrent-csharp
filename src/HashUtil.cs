namespace BitTorrentFeatures
{
    public static class HashUtil
    {
        public static string HashHex(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
