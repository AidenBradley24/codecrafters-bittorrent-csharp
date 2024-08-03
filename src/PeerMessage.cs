using System.Collections;

namespace BitTorrentFeatures
{
    public class PeerMessage(PeerMessage.Id type, byte[]? payload)
    {
        public Id Type { get; } = type;
        public byte[]? Payload { get; } = payload;

        public enum Id 
        {
            Choke = 0,
            Unchoke = 1,
            Interested = 2,
            NotInterested = 3,
            Have = 4,
            Bitfield = 5,
            Request = 6,
            Piece = 7,
            Cancel = 8
        }

        public static PeerMessage Recieve(Stream stream)
        {
            BinaryReader br = new(stream);
            int length = BitConverter.ToInt32(MiscUtils.BigEndian(br.ReadBytes(4)));
            Id type = (Id)br.ReadByte();
            Console.WriteLine(length);
            Console.WriteLine(type);
            byte[] payload = br.ReadBytes(length);
            var message = new PeerMessage(type, payload);
            return message;
        }

        public static void Send(Stream stream, Id type, byte[]? payload)
        {
            BinaryWriter bw = new(stream);
            bw.Write(MiscUtils.BigEndian(BitConverter.GetBytes(payload?.Length ?? 0)));
            bw.Write((byte)type);
            if (payload != null) bw.Write(payload);
        }

        public static void Send(Stream stream, PeerMessage message)
        {
            Send(stream, message.Type, message.Payload);
        }

        public void Send(Stream stream)
        {
            Send(stream, this);
        }

        public static PeerMessage Request(int index, int begin, int length)
        {
            byte[] payload =
            [
                .. MiscUtils.BigEndian(BitConverter.GetBytes(index)),
                .. MiscUtils.BigEndian(BitConverter.GetBytes(begin)),
                .. MiscUtils.BigEndian(BitConverter.GetBytes(length)),
            ];

            return new PeerMessage(Id.Request, payload);
        }

        public Block AsBlock()
        {
            if (Type != Id.Piece) throw new InvalidOperationException("is not a piece");
            return Block.Read(Payload);
        }
    }

    public class Block
    {
        public int start;
        public int end;
        public byte[] block;

        private Block(int start, int end, byte[] block)
        {
            this.start = start;
            this.end = end;
            this.block = block;
        }

        public static Block Read(ReadOnlySpan<byte> data)
        {
            int start = BitConverter.ToInt32(MiscUtils.BigEndian(data[0..4]));
            int end = BitConverter.ToInt32(MiscUtils.BigEndian(data[4..8]));
            byte[] block = data[8..].ToArray();
            return new Block(start, end, block);
        }

        public static byte[] Combine(IEnumerable<Block> blocks)
        {
            var ordered = blocks.OrderBy(b => b.start);
            byte[] final = ordered.Select(b => b.block)
                .Aggregate<IEnumerable<byte>>((left, right) => left.Concat(right))
                .ToArray();
            return final;
        }
    }
}
