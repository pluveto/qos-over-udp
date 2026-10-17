namespace QosDemo;

public readonly record struct QueuedMessage(
    byte ChannelId,
    byte[] Payload,
    long EnqueueTimestampMs,
    long MaxQueueDurationMs
);

public sealed class BufferManager
{
    private readonly int _capacity;
    private readonly int _highWatermark;
    private readonly int _lowWatermark;
    private readonly LinkedList<QueuedMessage> _queue = new();

    public bool IsBackpressureActive { get; private set; } = false;

    // 回调事件通知上层业务
    public event Action? OnBackpressureTriggered;
    public event Action? OnBackpressureReleased;

    public BufferManager(int capacity, double highRatio = 0.8, double lowRatio = 0.4)
    {
        _capacity = capacity;
        _highWatermark = (int)(capacity * highRatio);
        _lowWatermark = (int)(capacity * lowRatio);
    }

    // 入队前检查：如果超过容量硬上限，或者处于背压且消息低优，拒绝入队
    public bool TryEnqueue(byte channelId, byte[] payload, long nowMs, long maxQueueDurationMs)
    {
        if (_queue.Count >= _capacity)
        {
            // 物理队列已满：硬尾部丢弃（Tail Drop）
            return false;
        }

        _queue.AddLast(new QueuedMessage(channelId, payload, nowMs, maxQueueDurationMs));

        // 检查是否跨过高水位线
        if (!IsBackpressureActive && _queue.Count >= _highWatermark)
        {
            IsBackpressureActive = true;
            OnBackpressureTriggered?.Invoke();
        }

        return true;
    }

    // 出队时检查：剔除在队列里排队已经超时的陈旧包
    public QueuedMessage? DequeueValid(long nowMs)
    {
        while (_queue.Count > 0)
        {
            var msg = _queue.First!.Value;
            _queue.RemoveFirst();

            // 检查排队时间是否超标
            long queueDuration = nowMs - msg.EnqueueTimestampMs;
            if (msg.MaxQueueDurationMs > 0 && queueDuration > msg.MaxQueueDurationMs)
            {
                // 超期丢弃，不送往网卡，继续看下一个
                continue;
            }

            // 检查是否跌破低水位线，解除背压
            CheckWatermarkAfterRemoval();
            return msg;
        }

        CheckWatermarkAfterRemoval();
        return null;
    }

    private void CheckWatermarkAfterRemoval()
    {
        if (IsBackpressureActive && _queue.Count <= _lowWatermark)
        {
            IsBackpressureActive = false;
            OnBackpressureReleased?.Invoke();
        }
    }

    public int Count => _queue.Count;
}
