using System.Buffers.Binary;

namespace QosDemo;

// ACK 报文结构（固定 6 字节）：
// [0..1] : Base Packet Seq (2 Bytes, ushort) - 累计确认基准号（<= BaseSeq 的包已全部收到）
// [2..5] : SACK Bitmask (4 Bytes, uint) - 选择性确认位图（第 i 位为 1 表示 BaseSeq + 1 + i 已收到）
public readonly struct AckFrame
{
    public const int Size = 6;

    public ushort BasePacketSeq { get; }
    public uint SackMask { get; }

    public AckFrame(ushort basePacketSeq, uint sackMask)
    {
        BasePacketSeq = basePacketSeq;
        SackMask = sackMask;
    }

    public static byte[] Encode(ushort basePacketSeq, uint sackMask)
    {
        byte[] buffer = new byte[Size];
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), basePacketSeq);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(2, 4), sackMask);
        return buffer;
    }

    public static bool TryDecode(ReadOnlySpan<byte> buffer, out AckFrame ack)
    {
        if (buffer.Length < Size)
        {
            ack = default;
            return false;
        }

        ushort baseSeq = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(0, 2));
        uint mask = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(2, 4));
        ack = new AckFrame(baseSeq, mask);
        return true;
    }

    // 判断指定的 packetSeq 是否已被当前 ACK 确认
    public bool IsAcknowledged(ushort packetSeq)
    {
        if (packetSeq <= BasePacketSeq)
        {
            return true;
        }

        int offset = packetSeq - BasePacketSeq - 1;
        if (offset >= 0 && offset < 32)
        {
            return ((SackMask >> offset) & 1) == 1;
        }

        return false;
    }
}
