namespace QosDemo;

public delegate void MessageReceivedHandler(byte channelId, ushort messageSeq, ReadOnlySpan<byte> payload);

// 接收端业务交付网关：串联 SACK 记录、去重过滤器与分片重组器
public sealed class InboundDeliveryGate
{
    private readonly SackGenerator _sackGenerator;
    private readonly SequenceWindowFilter[] _channelFilters = new SequenceWindowFilter[256];
    private readonly ZeroAllocReassembler _reassembler = new();

    public event MessageReceivedHandler? OnMessageReceived;

    public InboundDeliveryGate(SackGenerator sackGenerator)
    {
        _sackGenerator = sackGenerator;
        for (int i = 0; i < _channelFilters.Length; i++)
        {
            _channelFilters[i] = new SequenceWindowFilter();
        }
    }

    // 驱动入口：网卡抓出一个完整的业务包切片（Ch < 255）时调用
    public void ProcessInboundPacket(ReadOnlySpan<byte> rawPacket)
    {
        // 1. 解构包头（零分配）
        if (!ZeroAllocFraming.TryReadHeader(
            rawPacket,
            out byte channelId,
            out ushort packetSeq,
            out ushort messageSeq,
            out byte fragmentIndex,
            out byte fragmentTotal,
            out ReadOnlySpan<byte> fragmentPayload))
        {
            return;
        }

        // 2. 无论是否业务重复，物理包既然到了，必须先登记进 SACK 状态机（以便回传 ACK 阻止对方无谓重发）
        _sackGenerator.RecordReceived(packetSeq);

        // 3. 通道去重检查：防止重传包让业务重复执行
        var filter = _channelFilters[channelId];
        // 注意：分片消息中不同分片拥有相同的 MessageSeq，去重只作用于单包或分片0首检
        // 对于多片消息，分片内部由重组器的位图防重，整包交付给业务层保证唯一一次
        if (fragmentIndex == 0 && !filter.TryAccept(messageSeq))
        {
            return; // 业务层已经交付过该消息，底层丢弃
        }

        // 4. 进入分片重组器
        if (_reassembler.TryAddFragment(messageSeq, fragmentIndex, fragmentTotal, fragmentPayload, out ReadOnlySpan<byte> fullMessage))
        {
            // 5. 零分配直接交付给业务回调
            OnMessageReceived?.Invoke(channelId, messageSeq, fullMessage);
        }
    }
}
