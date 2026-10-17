namespace QosDemo;

public enum PriorityLevel : byte
{
    Normal = 0,
    High = 1
}

public sealed class EgressPacketScheduler
{
    private readonly Queue<byte[]> _highQueue = new();
    private readonly Queue<byte[]> _normalQueue = new();

    public void Enqueue(PriorityLevel priority, byte[] packet)
    {
        if (priority == PriorityLevel.High)
        {
            _highQueue.Enqueue(packet);
        }
        else
        {
            _normalQueue.Enqueue(packet);
        }
    }

    // 调度出队：高优先级绝对抢占
    public byte[]? DequeueNext()
    {
        if (_highQueue.Count > 0)
        {
            return _highQueue.Dequeue();
        }

        if (_normalQueue.Count > 0)
        {
            return _normalQueue.Dequeue();
        }

        return null;
    }

    public int TotalPending => _highQueue.Count + _normalQueue.Count;
}
