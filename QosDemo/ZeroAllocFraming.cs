using System.Buffers.Binary;

namespace QosDemo;

// 零拷贝协议报头编解码工具：直接在已有的 Span<byte> / ReadOnlySpan<byte> 上读写，零堆分配
public static class ZeroAllocFraming
{
    public const int HeaderSize = 7;

    // 直接在租借的物理缓冲区目标 Span 上就地写入 7 字节报头
    public static void WriteHeader(
        Span<byte> destination, 
        byte channelId, 
        ushort packetSeq, 
        ushort messageSeq, 
        byte fragmentIndex, 
        byte fragmentTotal)
    {
        destination[0] = channelId;
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(1, 2), packetSeq);
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(3, 2), messageSeq);
        destination[5] = fragmentIndex;
        destination[6] = fragmentTotal;
    }

    // 从接收到的只读切片中直接解构报头与载荷切片，不发生任何 byte[] 复制
    public static bool TryReadHeader(
        ReadOnlySpan<byte> source, 
        out byte channelId, 
        out ushort packetSeq, 
        out ushort messageSeq, 
        out byte fragmentIndex, 
        out byte fragmentTotal, 
        out ReadOnlySpan<byte> payloadSpan)
    {
        if (source.Length < HeaderSize)
        {
            channelId = 0;
            packetSeq = 0;
            messageSeq = 0;
            fragmentIndex = 0;
            fragmentTotal = 0;
            payloadSpan = ReadOnlySpan<byte>.Empty;
            return false;
        }

        channelId = source[0];
        packetSeq = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(1, 2));
        messageSeq = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(3, 2));
        fragmentIndex = source[5];
        fragmentTotal = source[6];
        payloadSpan = source.Slice(HeaderSize);
        return true;
    }
}
