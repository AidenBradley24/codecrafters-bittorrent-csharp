using BitTorrentFeatures;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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
    foreach (IPEndPoint peer in tor.Peers)
    {
        Console.WriteLine(peer);
    }
}
else if (command == "handshake")
{
    using FileStream fs = new(param1, FileMode.Open);
    var tor = Torrent.ReadStream(fs);
    using TorrentClient client = tor.PerformHandshake(IPEndPoint.Parse(param2!));
    Console.WriteLine($"Peer ID: {client.PeerID}");
}
else if (command == "download_piece")
{
    FileInfo file = new(args[2]);
    using FileStream fs = new(args[3], FileMode.Open);
    var tor = Torrent.ReadStream(fs);
    int pieceIndex = int.Parse(args[4]);
    using TorrentClient client = tor.PerformHandshake();
    client.DownloadPiece(file, pieceIndex);
    Console.WriteLine($"Piece {pieceIndex} downloaded to {args[2]}");
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
