﻿using System;
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

        public void Dispose()
        {
            tcp.Dispose();
        }

        public Task DownloadPiece(FileInfo pieceFile, int pieceIndex)
        {
            NetworkStream ns = tcp.GetStream();
            var message = PeerMessage.Recieve(ns);
            if (message.Type != PeerMessage.Id.Bitfield) throw new Exception("not a bitfield");
            PeerMessage.Send(ns, PeerMessage.Id.Interested, null);
            message = PeerMessage.Recieve(ns);
            if (message.Type != PeerMessage.Id.Unchoke) throw new Exception("not a unchoke");

            int current = 0;
            List<Block> blocks = [];
            while (current < Tor.PieceLength)
            {
                int next = current + 16 * 1024;
                int size = next < (int)Tor.PieceLength ? 16 * 1024 : (int)Tor.PieceLength - current;
                var request = PeerMessage.Request(pieceIndex, current, size);
                current = next;
                Block block = request.AsBlock();
                blocks.Add(block);
            }

            byte[] data = Block.Combine(blocks);
            byte[] hash = Tor.Pieces.ElementAt(pieceIndex);
            if (!SHA1.HashData(data).SequenceEqual(hash)) throw new Exception("Hash does not match for piece " + pieceIndex);
            using var fs = pieceFile.OpenWrite();
            fs.Write(data);

            return Task.CompletedTask;
        }
    }
}
