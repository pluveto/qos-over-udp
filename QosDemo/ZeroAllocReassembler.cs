namespace QosDemo;

// 预分配的定长重组槽位：负责收集单条大消息的所有碎片
public struct InboundAssemblyBucket
{
    public ushort MessageSeq;
    public byte FragmentTotal;
    public ulong ReceivedMask; // 64 位掩码，第 i 位为 1 表示第 i 个碎片已到位
    public int TotalBytesReceived;
    public bool InUse;
}

public sealed class ZeroAllocReassembler
{
    private const int MaxConcurrentMessages = 16;
    private const int MaxMessageSize = 64 * 1024; // 单条大消息支持最大 64KB

    private readonly InboundAssemblyBucket[] _buckets = new InboundAssemblyBucket[MaxConcurrentMessages];
    // 为每个并发消息槽位预分配连续内存空间（16 * 64KB = 1MB 物理常驻，稳态零 GC）
    private readonly byte[] _payloadArena = new byte[MaxConcurrentMessages * MaxMessageSize];

    // 处理到达的分片，若凑齐则通过 Span 传出完整数据（零分配）
    public bool TryAddFragment(
        ushort messageSeq, 
        byte fragmentIndex, 
        byte fragmentTotal, 
        ReadOnlySpan<byte> fragmentPayload, 
        out ReadOnlySpan<byte> assembledPayload)
    {
        // 1. 单包消息：无需重组，直接原样零拷贝透传
        if (fragmentTotal <= 1)
        {
            assembledPayload = fragmentPayload;
            return true;
        }

        int bucketIndex = messageSeq % MaxConcurrentMessages;
        ref var bucket = ref _buckets[bucketIndex];

        // 首次遇到该消息序号，初始化槽位
        if (!bucket.InUse || bucket.MessageSeq != messageSeq)
        {
            bucket.MessageSeq = messageSeq;
            bucket.FragmentTotal = fragmentTotal;
            bucket.ReceivedMask = 0;
            bucket.TotalBytesReceived = 0;
            bucket.InUse = true;
        }

        // 检查该分片是否已经收到过（防重复写入）
        ulong fragBit = 1UL << fragmentIndex;
        if ((bucket.ReceivedMask & fragBit) == 0)
        {
            // 将分片数据直接拷贝到属于该槽位的预分配连续内存中
            // 假设单片最大 1200 字节，直接计算物理存放偏移
            int destOffset = (bucketIndex * MaxMessageSize) + (fragmentIndex * 1200);
            fragmentPayload.CopyTo(_payloadArena.AsSpan(destOffset, fragmentPayload.Length));

            bucket.ReceivedMask |= fragBit;
            bucket.TotalBytesReceived += fragmentPayload.Length;
        }

        // 检查是否所有分片全部集齐：前 fragmentTotal 位全为 1
        ulong expectedMask = (1UL << bucket.FragmentTotal) - 1UL;
        if (bucket.ReceivedMask == expectedMask)
        {
            int startOffset = bucketIndex * MaxMessageSize;
            assembledPayload = _payloadArena.AsSpan(startOffset, bucket.TotalBytesReceived);
            bucket.InUse = false; // 交付后重置槽位供后续消息复用
            return true;
        }

        assembledPayload = ReadOnlySpan<byte>.Empty;
        return false;
    }
}
