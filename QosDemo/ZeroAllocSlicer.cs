namespace QosDemo;

public static class ZeroAllocSlicer
{
    // 将业务数据写入常驻 PayloadArena，并就地拆分成若干 OutboundDescriptor 压入目标环形缓冲
    // 全过程：0 次堆分配，0 次装箱
    public static bool TrySliceAndEnqueue(
        PayloadArena arena,
        ValueRingBuffer<OutboundDescriptor> ringBuffer,
        byte channelId,
        ushort messageSeq,
        DeliveryPolicy policy,
        ReadOnlySpan<byte> messagePayload,
        ushort maxSliceSize = 1200)
    {
        if (messagePayload.Length == 0) return false;

        // 1. 将完整业务载荷整块拷贝进常驻 Arena（仅此一次内存写入，无任何托管堆对象创建）
        if (!arena.TryAllocateAndCopy(messagePayload, out int baseOffset))
        {
            return false; // Arena 缓冲区暂时已满
        }

        // 2. 计算需要切成几个分片
        int totalBytes = messagePayload.Length;
        byte sliceTotal = (byte)Math.Ceiling((double)totalBytes / maxSliceSize);

        // 检查环形队列剩余可用槽位是否足够
        if (ringBuffer.Capacity - ringBuffer.Count < sliceTotal)
        {
            return false;
        }

        // 3. 在栈上为每个切片生成 16 字节的值类型描述符，写入环形队列
        for (byte i = 0; i < sliceTotal; i++)
        {
            int sliceOffset = baseOffset + (i * maxSliceSize);
            ushort sliceLen = (ushort)Math.Min((int)maxSliceSize, totalBytes - (i * maxSliceSize));

            var descriptor = new OutboundDescriptor(
                channelId: channelId,
                messageSeq: messageSeq,
                fragmentIndex: i,
                fragmentTotal: sliceTotal,
                policy: policy,
                payloadOffset: sliceOffset,
                payloadLength: sliceLen
            );

            ringBuffer.TryEnqueue(in descriptor);
        }

        return true;
    }
}
