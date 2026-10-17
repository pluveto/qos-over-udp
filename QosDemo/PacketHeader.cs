using System.Buffers.Binary;

namespace QosDemo;

// 帧结构定义：
// [0..0]   : Channel ID (1 Byte)
// [1..2]   : Packet Sequence (2 Bytes, Big Endian) - 全局链路序号
// [3..4]   : Message Sequence (2 Bytes, Big Endian) - 通道内逻辑序号
// [5..End] : Payload (N Bytes)
public readonly struct PacketHeader
{
    public const int HeaderSize = 5;

    public byte ChannelId { get; }
    public ushort PacketSeq { get; }
    public ushort MessageSeq { get; }

    public PacketHeader(byte channelId, ushort packetSeq, ushort messageSeq)
    {
        ChannelId = channelId;
        PacketSeq = packetSeq;
        MessageSeq = messageSeq;
    }

    public static byte[] Encode(byte channelId, ushort packetSeq, ushort messageSeq, ReadOnlySpan<byte> payload)
    {
        byte[] buffer = new byte[HeaderSize + payload.Length];
        buffer[0] = channelId;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(1, 2), packetSeq);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(3, 2), messageSeq);
        payload.CopyTo(buffer.AsSpan(HeaderSize));
        return buffer;
    }

    public static bool TryDecode(ReadOnlySpan<byte> rawPacket, out PacketHeader header, out ReadOnlyMemory<byte> payload)
    {
        if (rawPacket.Length < HeaderSize)
        {
            header = default;
            payload = ReadOnlyMemory<byte>.Empty;
            return false;
        }

        byte channelId = rawPacket[0];
        ushort packetSeq = BinaryPrimitives.ReadUInt16BigEndian(rawPacket.Slice(1, 2));
        ushort messageSeq = BinaryPrimitives.ReadUInt16BigEndian(rawPacket.Slice(3, 2));

        header = new PacketHeader(channelId, packetSeq, messageSeq);
        payload = rawPacket.Slice(HeaderSize).ToArray();
        return true;
    }
}
