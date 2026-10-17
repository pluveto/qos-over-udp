namespace QosDemo;

public enum DeliveryPolicy
{
    // 不可靠：丢失绝不重传（如高频传感器数据）
    Unreliable,

    // 可靠：丢包必须重传，直至收到 ACK（如充值指令）
    Reliable,

    // 部分可靠（有时效性）：在 TTL 范围内重传，超时直接放弃（如实时对战射击）
    TimedReliable
}

public sealed class TrackedPacket
{
    public ushort PacketSeq { get; }
    public byte ChannelId { get; }
    public ushort MessageSeq { get; }
    public byte[] Payload { get; }
    public DeliveryPolicy Policy { get; }
    public long SentTimestampMs { get; set; }
    public long TtlMs { get; }
    public int RetryCount { get; set; }

    public TrackedPacket(ushort packetSeq, byte channelId, ushort messageSeq, byte[] payload, DeliveryPolicy policy, long sentTimestampMs, long ttlMs = 0)
    {
        PacketSeq = packetSeq;
        ChannelId = channelId;
        MessageSeq = messageSeq;
        Payload = payload;
        Policy = policy;
        SentTimestampMs = sentTimestampMs;
        TtlMs = ttlMs;
        RetryCount = 0;
    }

    // 检查该未确认包在当前时间戳下是否还值得重传
    public bool ShouldRetransmit(long currentTimestampMs)
    {
        return Policy switch
        {
            DeliveryPolicy.Unreliable => false,
            DeliveryPolicy.Reliable => true,
            DeliveryPolicy.TimedReliable => (currentTimestampMs - SentTimestampMs) <= TtlMs,
            _ => false
        };
    }
}
