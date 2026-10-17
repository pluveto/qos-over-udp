namespace QosDemo;

// 预分配的连续载荷环形存储区（Payload Arena）
// 所有入队的真实业务载荷字节全部连续排放在这个大 byte[] 中，零堆分配
public sealed class PayloadArena
{
    private readonly byte[] _buffer;
    private readonly int _capacity;
    private int _head = 0; // 当前写入偏移
    private int _tail = 0; // 当前释放偏移

    public PayloadArena(int capacityBytes = 64 * 1024)
    {
        _capacity = capacityBytes;
        _buffer = new byte[capacityBytes];
    }

    // 将外部只读切片拷贝进连续存储区，并返回它在区内的相对偏移量
    public bool TryAllocateAndCopy(ReadOnlySpan<byte> source, out int allocatedOffset)
    {
        int length = source.Length;
        // 简单连续写入（当剩余空间足够时）
        if (_head + length > _capacity)
        {
            // 如果撞到物理尾部，折返到头部（只要不覆盖尚未出队的 tail）
            if (length > _tail)
            {
                allocatedOffset = 0;
                return false; // 空间被占满
            }
            _head = 0;
        }

        allocatedOffset = _head;
        source.CopyTo(_buffer.AsSpan(allocatedOffset, length));
        _head += length;
        return true;
    }

    // 释放指定长度的空间（随数据包出队被网卡发出后推进）
    public void AdvanceTail(int length)
    {
        _tail = (_tail + length) % _capacity;
    }

    // 根据偏移和长度，直接获取只读切片
    public ReadOnlySpan<byte> GetSpan(int offset, int length)
    {
        return _buffer.AsSpan(offset, length);
    }
}
