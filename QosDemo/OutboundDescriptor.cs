namespace QosDemo;

// 待发包的元数据描述符（只读值类型，共 16 字节，完全在栈上传递或连续存储在数组中）
// 绝对不包含引用类型的堆指针，绝对不发生装箱
public readonly struct OutboundDescriptor
{
    public byte ChannelId { get; }
    public ushort MessageSeq { get; }
    public byte FragmentIndex { get; }
    public byte FragmentTotal { get; }
    public DeliveryPolicy Policy { get; }
    public ushort PayloadLength { get; }
    public long EnqueueTimestampMs { get; }

    public OutboundDescriptor(
        byte channelId, 
        ushort messageSeq, 
        byte fragmentIndex, 
        byte fragmentTotal, 
        DeliveryPolicy policy, 
        ushort payloadLength, 
        long enqueueTimestampMs)
    {
        ChannelId = channelId;
        MessageSeq = messageSeq;
        FragmentIndex = fragmentIndex;
        FragmentTotal = fragmentTotal;
        Policy = policy;
        PayloadLength = payloadLength;
        EnqueueTimestampMs = enqueueTimestampMs;
    }
}
