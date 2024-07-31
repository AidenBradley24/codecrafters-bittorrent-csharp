using BitTorrentFeatures;
using System.Text;
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
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
