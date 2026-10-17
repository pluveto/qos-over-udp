using QosDemo;

Console.WriteLine("=== Verification of Zero-Alloc BufferLease ===");

// 预热：让 ArrayPool 内部池化结构完成首次静态分配
using (var warmup = new BufferLease(1400)) { }

long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

for (int i = 0; i < 1000; i++)
{
    using (var lease = new BufferLease(1400))
    {
        lease.Span[0] = 42;
        lease.Span[1] = 0xAB;
        lease.Span[2] = 0xCD;
    }
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;

Console.WriteLine($"Steady state: Allocated bytes across 1000 cycles: {allocated} bytes");

