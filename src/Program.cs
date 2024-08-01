using BitTorrentFeatures;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

// Parse arguments
var (command, param) = args.Length switch
{
    0 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    1 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    _ => (args[0], args[1])
};

// Parse command and act accordingly
if (command == "decode")
{
    MemoryStream ms = new(Encoding.UTF8.GetBytes(param));
    ms.Position = 0;
    BencodeReader reader = new(ms);
    Console.WriteLine(JsonSerializer.Serialize(reader.ReadAny()));
}
else if (command == "info")
{
    using FileStream fs = new(param, FileMode.Open);
    var tor = Torrent.ReadStream(fs);
    Console.WriteLine(
        $"Tracker URL: {tor.TrackerUrl}\n" +
        $"Length: {tor.Length}\n" +
        $"Info Hash: {HashUtil.HashHex(tor.InfoHash)}\n" +
        $"Piece Length: {tor.PieceLength}\n" +
        $"Piece Hashes:\n{string.Concat(tor.Pieces.Select(h => HashUtil.HashHex(h) + '\n'))}");
}
else if (command == "peers")
{
    using FileStream fs = new(param, FileMode.Open);
    var tor = Torrent.ReadStream(fs);
    HttpClient client = new();

    UriBuilder builder = new(tor.TrackerUrl);
    builder.Query = 
        $"info_hash=%{BitConverter.ToString(tor.InfoHash).Replace('-', '%').ToLower()}" +
        $"peer_id=00112233445566778899" +
        $"port=6881" +
        $"uploaded=0" +
        $"downloaded=0" +
        $"left={tor.Length}" +
        $"compact=1";

    Console.WriteLine(builder.Uri);

    var response = client.Send(new HttpRequestMessage(HttpMethod.Get, builder.Uri));
    Stream stream = response.Content.ReadAsStream();
    BencodeReader reader = new(stream);
    var dict = reader.ReadDictionary();
    Console.WriteLine(string.Concat(dict.Keys.Select(k => $"key: {k},")));
    ReadOnlySpan<byte> peers = ((BencodeString)dict["peers"]).Bytes;

    for (int i = 0; i < peers.Length; i += 6)
    {
        IPAddress address = new(peers[i..(i+4)]);
        ushort portNumber = BitConverter.ToUInt16(peers[(i+4)..(i+6)]);
        string ipString = $"{address}:{portNumber}";
        Console.WriteLine(ipString);
    }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
