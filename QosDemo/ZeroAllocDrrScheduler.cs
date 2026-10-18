namespace QosDemo;

public sealed class ZeroAllocDrrChannel
{
    public byte ChannelId { get; }
    public int Quantum { get; }
    public int DeficitCounter { get; set; }
    public ValueRingBuffer<OutboundDescriptor> RingBuffer { get; }

    public ZeroAllocDrrChannel(byte channelId, int quantum, int ringCapacity = 1024)
    {
        ChannelId = channelId;
        Quantum = quantum;
        DeficitCounter = 0;
        RingBuffer = new ValueRingBuffer<OutboundDescriptor>(ringCapacity);
    }
}

public sealed class ZeroAllocDrrScheduler
{
    private readonly ZeroAllocDrrChannel[] _channels;
    private int _currentIndex = 0;

    public ZeroAllocDrrScheduler(ZeroAllocDrrChannel[] channels)
    {
        _channels = channels;
    }

    // 零分配入队：直接通过 in 引用塞入对应通道的环形缓冲区
    public bool TryEnqueue(byte channelId, in OutboundDescriptor descriptor)
    {
        for (int i = 0; i < _channels.Length; i++)
        {
            if (_channels[i].ChannelId == channelId)
            {
                return _channels[i].RingBuffer.TryEnqueue(in descriptor);
            }
        }
        return false;
    }

    // 零分配出队：通过 out 传出结构体值，绝对不发生堆分配
    public bool TryDequeueNext(out OutboundDescriptor descriptor)
    {
        if (_channels.Length == 0)
        {
            descriptor = default;
            return false;
        }

        int scanned = 0;
        while (scanned < _channels.Length)
        {
            var ch = _channels[_currentIndex];

            if (ch.RingBuffer.Count == 0)
            {
                ch.DeficitCounter = 0;
                Advance();
                scanned++;
                continue;
            }

            if (ch.DeficitCounter == 0)
            {
                ch.DeficitCounter += ch.Quantum;
            }

            // 查看队头描述符（只读查看，不弹出）
            if (ch.RingBuffer.TryPeek(out var nextDesc))
            {
                int packetSize = ZeroAllocFraming.HeaderSize + nextDesc.PayloadLength;
                if (packetSize <= ch.DeficitCounter)
                {
                    // 额度充足：扣除信用额度，正式弹出
                    ch.DeficitCounter -= packetSize;
                    ch.RingBuffer.TryDequeue(out descriptor);

                    if (ch.RingBuffer.Count == 0)
                    {
                        ch.DeficitCounter = 0;
                    }
                    return true;
                }
            }

            // 额度不足以发送当前包：将额度存入下一轮，切换下一个通道
            ch.DeficitCounter += ch.Quantum;
            Advance();
            scanned++;
        }

        descriptor = default;
        return false;
    }


    private void Advance()
    {
        _currentIndex = (_currentIndex + 1) % _channels.Length;
    }

    public bool HasPendingPackets
    {
        get
        {
            for (int i = 0; i < _channels.Length; i++)
            {
                if (_channels[i].RingBuffer.Count > 0) return true;
            }
            return false;
        }
    }
}
