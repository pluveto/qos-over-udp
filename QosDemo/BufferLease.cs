using System.Buffers;

namespace QosDemo;

// 负责封装 ArrayPool 租借生命周期的轻量值类型凭证
// 实现 IDisposable，配合 using 语句确保使用完毕后无条件归还
public readonly struct BufferLease : IDisposable
{
    private readonly byte[] _array;
    public int Length { get; }

    public BufferLease(int length)
    {
        Length = length;
        // 向共享池租借至少 length 字节的数组（池返回的实际长度通常会向上对齐到 2 的次幂）
        _array = ArrayPool<byte>.Shared.Rent(length);
    }

    // 暴露只读内存视图，长度严格受限于业务申请的 Length，屏蔽 ArrayPool 的多余填充
    public Memory<byte> Memory => _array.AsMemory(0, Length);
    public Span<byte> Span => _array.AsSpan(0, Length);

    // 归还逻辑
    public void Dispose()
    {
        if (_array != null)
        {
            ArrayPool<byte>.Shared.Return(_array);
        }
    }
}
