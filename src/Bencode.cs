namespace BitTorrentFeatures
{
    public static class Bencode
    {
        public static object Decode(string encodedValue)
        {
            char first = encodedValue[0];
            if (char.IsDigit(first))
            {
                // string
                int colonIndex = encodedValue.IndexOf(':');
                if (colonIndex != -1)
                {
                    int length = int.Parse(encodedValue[..colonIndex]);
                    string value = encodedValue.Substring(colonIndex + 1, length);
                    return value;
                }
                else
                {
                    throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
                }
            }
            else if (first == 'i')
            {
                // integer
                int endIndex = encodedValue.IndexOf('e');
                if (endIndex != -1)
                {
                    long value = long.Parse(encodedValue[1..endIndex]);
                    return value;
                }
                else
                {
                    throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
                }
            }

            throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
        }
    }
}
