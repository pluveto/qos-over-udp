using QosDemo;

Console.WriteLine("=== Verification of ValueRingBuffer Zero-Alloc ===");

// 预分配容量为 1024 的定长环形队列，存放 OutboundDescriptor 值类型
var ring = new ValueRingBuffer<OutboundDescriptor>(1024);

// 预热
var dummy = new OutboundDescriptor(0, 0, 0, 1, DeliveryPolicy.Unreliable, 0, 0);
ring.TryEnqueue(in dummy);
ring.TryDequeue(out _);

long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

// 模拟 100,000 次持续的高频“生产者入队 -> 消费者出队”
for (int i = 0; i < 100_000; i++)
{
    var desc = new OutboundDescriptor(
        channelId: 1,
        messageSeq: (ushort)(i & 0xFFFF),
        fragmentIndex: 0,
        fragmentTotal: 1,
        policy: DeliveryPolicy.Reliable,
        payloadLength: 256,
        enqueueTimestampMs: 1000 + i
    );

    // 零分配入队
    if (!ring.TryEnqueue(in desc))
    {
        Console.WriteLine("Buffer full!");
    }

    // 零分配出队
    if (!ring.TryDequeue(out var popped))
    {
        Console.WriteLine("Buffer empty!");
    }
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;

Console.WriteLine($"100,000 Enqueue-Dequeue Cycles Allocated: {allocated} bytes.");
Console.WriteLine($"Ring Buffer Final Count: {ring.Count}, Capacity: {ring.Capacity}");
