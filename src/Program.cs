using BitTorrentFeatures;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

byte[] MY_PEER_ID = [0x0, 0x0, 0x1, 0x1, 0x2, 0x2, 0x3, 0x3, 0x4, 0x4, 0x5, 0x5, 0x6, 0x6, 0x7, 0x7, 0x8, 0x8, 0x9, 0x9];
const string PROTOCOL = "BitTorrent protocol";

// Parse arguments
var (command, param1, param2) = args.Length switch
{
    0 or 1 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    2 => (args[0], args[1], null),
    _ => (args[0], args[1], args[2])
};

// Parse command and act accordingly
if (command == "decode")
{
    MemoryStream ms = new(Encoding.UTF8.GetBytes(param1));
    ms.Position = 0;
    BencodeReader reader = new(ms);
    Console.WriteLine(JsonSerializer.Serialize(reader.ReadAny()));
}
else if (command == "info")
{
    using FileStream fs = new(param1, FileMode.Open);
    var tor = Torrent.ReadStream(fs);
    Console.WriteLine(
        $"Tracker URL: {tor.TrackerUrl}\n" +
        $"Length: {tor.Length}\n" +
        $"Info Hash: {MiscUtils.Hex(tor.InfoHash)}\n" +
        $"Piece Length: {tor.PieceLength}\n" +
        $"Piece Hashes:\n{string.Concat(tor.Pieces.Select(h => MiscUtils.Hex(h) + '\n'))}");
}
else if (command == "peers")
{
    using FileStream fs = new(param1, FileMode.Open);
    var tor = Torrent.ReadStream(fs);
    HttpClient client = new();

    UriBuilder builder = new(tor.TrackerUrl);
    builder.Query = 
        $"info_hash=%{BitConverter.ToString(tor.InfoHash).Replace('-', '%').ToLower()}&" +
        $"peer_id={MiscUtils.Hex(MY_PEER_ID)}&" +
        $"port=6881&" +
        $"uploaded=0&" +
        $"downloaded=0&" +
        $"left={tor.Length}&" +
        $"compact=1";

    var response = client.Send(new HttpRequestMessage(HttpMethod.Get, builder.Uri));
    Stream stream = response.Content.ReadAsStream();
    BencodeReader reader = new(stream);
    var dict = reader.ReadDictionary();
    ReadOnlySpan<byte> peers = ((BencodeString)dict["peers"]).Bytes;

    for (int i = 0; i < peers.Length; i += 6)
    {
        IPAddress address = new(peers[i..(i+4)]);
        ushort portNumber = BitConverter.ToUInt16(MiscUtils.BigEndian(peers[(i+4)..(i+6)]));
        string ipString = $"{address}:{portNumber}";
        Console.WriteLine(ipString);
    }
}
else if (command == "handshake")
{
    using FileStream fs = new(param1, FileMode.Open);
    var tor = Torrent.ReadStream(fs);
    TcpClient client = new();
    var strs = param2!.Split(':');
    IPAddress ip = IPAddress.Parse(strs[0]);
    int port = int.Parse(strs[1]);
    client.Connect(ip, port);
    NetworkStream ns = client.GetStream();

    ns.WriteByte(Convert.ToByte(PROTOCOL.Length));
    ns.Write(Encoding.UTF8.GetBytes(PROTOCOL));
    for (int i = 0; i < 8; i++) ns.WriteByte(0);
    ns.Write(tor.InfoHash);
    ns.Write(MY_PEER_ID);

    BinaryReader br = new(ns);
    byte len = br.ReadByte();
    string protocol = Encoding.UTF8.GetString(br.ReadBytes(len));
    if (protocol != PROTOCOL) throw new Exception("protocol does not match");
    br.ReadBytes(8);
    byte[] hash = br.ReadBytes(20);
    byte[] peer = br.ReadBytes(20);

    Console.WriteLine($"Peer ID: {MiscUtils.Hex(peer)}");
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
