namespace QosDemo;

// 接收端去重滑动窗口（基于 64 位整型掩码）
public sealed class SequenceWindowFilter
{
    private ushort _maxDeliveredSeq = 0;
    private ulong _windowMask = 0;
    private bool _initialized = false;

    // 返回 true 表示该包是首次到达的合法新包，允许交给业务层
    // 返回 false 表示该包已经交付过或严重过期，底层直接拦截丢弃
    public bool TryAccept(ushort messageSeq)
    {
        if (!_initialized)
        {
            _initialized = true;
            _maxDeliveredSeq = messageSeq;
            _windowMask = 1UL;
            return true;
        }

        int diff = messageSeq - _maxDeliveredSeq;

        // 情况 1：收到一个比当前最大序号更新的包
        if (diff > 0)
        {
            if (diff >= 64)
            {
                // 超出窗口跨度，重置窗口
                _windowMask = 1UL;
            }
            else
            {
                // 窗口向左滑动 diff 位，并将最低位标为 1
                _windowMask = (_windowMask << diff) | 1UL;
            }
            _maxDeliveredSeq = messageSeq;
            return true;
        }

        // 情况 2：收到刚好等于最大序号的包（重复包）
        if (diff == 0)
        {
            return false;
        }

        // 情况 3：收到乱序或滞后到达的旧包（messageSeq < _maxDeliveredSeq）
        int offset = _maxDeliveredSeq - messageSeq;
        if (offset >= 64)
        {
            // 滞后超过 64 个序号，超出防重安全窗口，安全丢弃
            return false;
        }

        ulong mask = 1UL << offset;
        if ((_windowMask & mask) != 0)
        {
            // 该位已经是 1，说明之前已经接收并交付过，属于网络重复重传，直接拦截
            return false;
        }

        // 首次到达的乱序包：打上标记并允许交付
        _windowMask |= mask;
        return true;
    }
}
