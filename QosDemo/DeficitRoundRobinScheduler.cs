namespace QosDemo;

public sealed class DrrQueue
{
    public byte ChannelId { get; }
    public int Quantum { get; }           // 每一轮分配的字节配额（权重）
    public int DeficitCounter { get; set; } // 当前累积的信用额度（字节）
    public Queue<byte[]> Packets { get; } = new();

    public DrrQueue(byte channelId, int quantum)
    {
        ChannelId = channelId;
        Quantum = quantum;
        DeficitCounter = 0;
    }
}

public sealed class DeficitRoundRobinScheduler
{
    private readonly List<DrrQueue> _queues = new();
    private int _currentQueueIndex = 0;

    public void AddQueue(DrrQueue queue)
    {
        _queues.Add(queue);
    }

    public void Enqueue(byte channelId, byte[] packet)
    {
        var target = _queues.Find(q => q.ChannelId == channelId);
        target?.Packets.Enqueue(packet);
    }

    // 执行一次 DRR 调度轮转，弹出一个符合额度要求的包
    public (byte ChannelId, byte[] Packet)? DequeueNext()
    {
        if (_queues.Count == 0) return null;

        int scanned = 0;
        while (scanned < _queues.Count)
        {
            var q = _queues[_currentQueueIndex];

            // 如果队列为空，清空它的累积额度，避免无活干却存巨款
            if (q.Packets.Count == 0)
            {
                q.DeficitCounter = 0;
                Advance();
                scanned++;
                continue;
            }

            // 新的一轮开始，注入每轮配额 Quantum
            if (q.DeficitCounter == 0)
            {
                q.DeficitCounter += q.Quantum;
            }

            byte[] nextPacket = q.Packets.Peek();
            if (nextPacket.Length <= q.DeficitCounter)
            {
                // 额度充足：扣除包大小的额度，并成功出队
                q.DeficitCounter -= nextPacket.Length;
                q.Packets.Dequeue();

                // 如果发完后队列空了，清空额度
                if (q.Packets.Count == 0)
                {
                    q.DeficitCounter = 0;
                }
                return (q.ChannelId, nextPacket);
            }

            // 额度不足以发送当前包：保留现有额度给下一轮，换下一个队列
            q.DeficitCounter += q.Quantum;
            Advance();
            scanned++;
        }

        return null;
    }

    private void Advance()
    {
        _currentQueueIndex = (_currentQueueIndex + 1) % _queues.Count;
    }

    public bool HasPendingPackets => _queues.Exists(q => q.Packets.Count > 0);
}
