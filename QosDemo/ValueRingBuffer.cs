using System.Numerics;

namespace QosDemo;

// 预分配连续存储的定长环形队列（针对只读值类型特化，零堆分配、零装箱）
public sealed class ValueRingBuffer<T> where T : struct
{
    private readonly T[] _buffer;
    private readonly int _mask;
    private int _head = 0; // 写入游标（入队递增）
    private int _tail = 0; // 读取游标（出队递增）

    public int Capacity { get; }
    public int Count => _head - _tail;

    public ValueRingBuffer(int capacity)
    {
        // 强制容量向上对齐到 2 的次幂，以便使用位与代替整数除法
        if (capacity < 2 || (capacity & (capacity - 1)) != 0)
        {
            capacity = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(2, capacity));
        }

        Capacity = capacity;
        _mask = capacity - 1;
        _buffer = new T[capacity];
    }

    // 入队：如果缓冲区已满返回 false，否则以只读引用写入目标槽位（无栈拷贝、无堆分配）
    public bool TryEnqueue(in T item)
    {
        if (Count >= Capacity)
        {
            return false;
        }

        _buffer[_head & _mask] = item;
        _head++;
        return true;
    }

    // 出队：如果为空返回 false，否则以引用传出槽位值
    public bool TryDequeue(out T item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }

        item = _buffer[_tail & _mask];
        _tail++;
        return true;
    }

    // 查看队头元素但不弹出
    public bool TryPeek(out T item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }

        item = _buffer[_tail & _mask];
        return true;
    }

    public void Clear()
    {
        _head = 0;
        _tail = 0;
    }
}
