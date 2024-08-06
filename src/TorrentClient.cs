using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentFeatures
{
    public class TorrentClient(Torrent torrent, TcpClient tcp, byte[] hash, string peerID) : IDisposable
    {
        public Torrent Tor { get; } = torrent;
        public string PeerID { get; } = peerID;

        private readonly SemaphoreSlim semaphore = new(5);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            tcp.Dispose();
        }

        public void DownloadPiece(FileInfo pieceFile, int pieceIndex)
        {
            Console.WriteLine("start");
            NetworkStream ns = tcp.GetStream();
            var message = PeerMessage.Recieve(ns);
            if (message.Type != PeerMessage.Id.Bitfield) throw new Exception("not a bitfield");
            PeerMessage.Send(ns, PeerMessage.Id.Interested, null);
            message = PeerMessage.Recieve(ns);
            if (message.Type != PeerMessage.Id.Unchoke) throw new Exception("not a unchoke");

            uint current = 0;

            const uint BLOCK_LENGTH = 16 * 1024;
            uint PIECE_LENGTH = (uint)Math.Min(Tor.Length - (pieceIndex * BLOCK_LENGTH), Tor.PieceLength);
            int blockCount = (int)Math.Ceiling((double)PIECE_LENGTH / BLOCK_LENGTH);

            var recieveTask = RecieveBlocks(ns, blockCount);
            while (current < Tor.PieceLength)
            {
                uint next = current + BLOCK_LENGTH;
                uint length = next < PIECE_LENGTH ? BLOCK_LENGTH : PIECE_LENGTH - current;
                Console.WriteLine($"block: {current}, length: {length}");
                var request = PeerMessage.Request(pieceIndex, current, length);
                semaphore.Wait();
                request.Send(ns);
                current = next;
            }

            recieveTask.Wait();
            byte[] data = recieveTask.Result;
            byte[] hash = Tor.Pieces.ElementAt(pieceIndex);
            if (!SHA1.HashData(data).SequenceEqual(hash)) throw new Exception("Hash does not match for piece " + pieceIndex);
            using var fs = pieceFile.OpenWrite();
            fs.Write(data);
        }

        private async Task<byte[]> RecieveBlocks(NetworkStream ns, int blockCount)
        {
            List<Block> blocks = [];
            for (int i = 0; i < blockCount; i++)
            {
                var response = await PeerMessage.RecieveAsync(ns);
                Block block = response.AsBlock();
                blocks.Add(block);
                semaphore.Release();
            }

            return Block.Combine(blocks);
        }
    }
}
