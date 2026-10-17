using System.Buffers.Binary;

namespace QosDemo;

// 扩展后的帧报头（共 7 字节）：
// [0..0] : Channel ID (1 Byte)
// [1..2] : Packet Sequence (2 Bytes, ushort) - 全局链路号
// [3..4] : Message Sequence (2 Bytes, ushort) - 通道内消息号
// [5..5] : Fragment Index (1 Byte) - 当前分片序号（从 0 开始）
// [6..6] : Fragment Total (1 Byte) - 该消息的总分片数
// [7..End] : Payload Fragment
public readonly struct FragmentedPacketHeader
{
    public const int HeaderSize = 7;

    public byte ChannelId { get; }
    public ushort PacketSeq { get; }
    public ushort MessageSeq { get; }
    public byte FragmentIndex { get; }
    public byte FragmentTotal { get; }

    public FragmentedPacketHeader(byte channelId, ushort packetSeq, ushort messageSeq, byte fragmentIndex, byte fragmentTotal)
    {
        ChannelId = channelId;
        PacketSeq = packetSeq;
        MessageSeq = messageSeq;
        FragmentIndex = fragmentIndex;
        FragmentTotal = fragmentTotal;
    }

    public static byte[] Encode(
        byte channelId, 
        ushort packetSeq, 
        ushort messageSeq, 
        byte fragmentIndex, 
        byte fragmentTotal, 
        ReadOnlySpan<byte> fragmentPayload)
    {
        byte[] buffer = new byte[HeaderSize + fragmentPayload.Length];
        buffer[0] = channelId;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(1, 2), packetSeq);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(3, 2), messageSeq);
        buffer[5] = fragmentIndex;
        buffer[6] = fragmentTotal;
        fragmentPayload.CopyTo(buffer.AsSpan(HeaderSize));
        return buffer;
    }

    public static bool TryDecode(
        ReadOnlySpan<byte> rawPacket, 
        out FragmentedPacketHeader header, 
        out ReadOnlyMemory<byte> fragmentPayload)
    {
        if (rawPacket.Length < HeaderSize)
        {
            header = default;
            fragmentPayload = ReadOnlyMemory<byte>.Empty;
            return false;
        }

        byte channelId = rawPacket[0];
        ushort packetSeq = BinaryPrimitives.ReadUInt16BigEndian(rawPacket.Slice(1, 2));
        ushort messageSeq = BinaryPrimitives.ReadUInt16BigEndian(rawPacket.Slice(3, 2));
        byte fragmentIndex = rawPacket[5];
        byte fragmentTotal = rawPacket[6];

        header = new FragmentedPacketHeader(channelId, packetSeq, messageSeq, fragmentIndex, fragmentTotal);
        fragmentPayload = rawPacket.Slice(HeaderSize).ToArray();
        return true;
    }
}
