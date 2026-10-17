namespace QosDemo;

// 待发包元数据描述符：只读值类型（16 字节）
// 记录它在连续载荷存储区（Payload Arena）中的物理偏移与长度
public readonly struct OutboundDescriptor
{
    public byte ChannelId { get; }
    public ushort MessageSeq { get; }
    public byte FragmentIndex { get; }
    public byte FragmentTotal { get; }
    public DeliveryPolicy Policy { get; }
    public int PayloadOffset { get; }
    public ushort PayloadLength { get; }

    public OutboundDescriptor(
        byte channelId, 
        ushort messageSeq, 
        byte fragmentIndex, 
        byte fragmentTotal, 
        DeliveryPolicy policy, 
        int payloadOffset, 
        ushort payloadLength)
    {
        ChannelId = channelId;
        MessageSeq = messageSeq;
        FragmentIndex = fragmentIndex;
        FragmentTotal = fragmentTotal;
        Policy = policy;
        PayloadOffset = payloadOffset;
        PayloadLength = payloadLength;
    }
}
