using System.Buffers.Binary;

namespace QosDemo;

// 协议线路多路复用器（零拷贝、零分配）
// 约定：
// - Channel 255: 专属控制帧（ACK / SACK）
// - Channel 0..254: 业务数据帧
public static class WireDemuxer
{
    public const byte ControlChannelAck = 255;
    public const int AckFrameWireSize = 7; // 1B (ChannelId=255) + 2B (BaseSeq) + 4B (SackMask)

    // 零分配编码一个 ACK 控制包到 Span 中
    public static int WriteAckFrame(Span<byte> destination, ushort basePacketSeq, uint sackMask)
    {
        destination[0] = ControlChannelAck;
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(1, 2), basePacketSeq);
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(3, 4), sackMask);
        return AckFrameWireSize;
    }

    // 从接收到的 UDP 报文第一字节判定是否为 ACK 控制帧
    public static bool TryReadAckFrame(ReadOnlySpan<byte> source, out ushort basePacketSeq, out uint sackMask)
    {
        if (source.Length >= AckFrameWireSize && source[0] == ControlChannelAck)
        {
            basePacketSeq = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(1, 2));
            sackMask = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(3, 4));
            return true;
        }

        basePacketSeq = 0;
        sackMask = 0;
        return false;
    }
}
