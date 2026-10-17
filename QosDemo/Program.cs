using System.Text;
using QosDemo;

Console.WriteLine("=== Verification of Zero-Alloc Slicing Engine ===");

var arena = new PayloadArena(capacityBytes: 64 * 1024);
var ring = new ValueRingBuffer<OutboundDescriptor>(capacity: 128);

// 构造一个 3200 字节的大消息（需要切成 3 片：1200 + 1200 + 800）
byte[] largePayload = new byte[3200];
for (int i = 0; i < largePayload.Length; i++)
{
    largePayload[i] = (byte)(i % 256);
}

// 预热：让 JIT 完成首次编译
ZeroAllocSlicer.TrySliceAndEnqueue(
    arena, ring, channelId: 0, messageSeq: 1, 
    policy: DeliveryPolicy.Reliable, 
    messagePayload: largePayload.AsSpan(0, 100), 
    maxSliceSize: 1200
);
ring.TryDequeue(out _);

long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

// 模拟 1,000 次大包切片入队与调度出队消费
for (int cycle = 0; cycle < 1000; cycle++)
{
    ushort msgSeq = (ushort)(cycle + 10);
    
    // 切片并入队
    bool sliced = ZeroAllocSlicer.TrySliceAndEnqueue(
        arena,
        ring,
        channelId: 0,
        messageSeq: msgSeq,
        policy: DeliveryPolicy.Reliable,
        messagePayload: largePayload,
        maxSliceSize: 1200
    );

    // 调度消费这 3 个切片
    while (ring.TryDequeue(out var desc))
    {
        ReadOnlySpan<byte> sliceSpan = arena.GetSpan(desc.PayloadOffset, desc.PayloadLength);
        arena.AdvanceTail(desc.PayloadLength);
    }
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;

Console.WriteLine($"Processed 1,000 large messages (3,000 sliced packets).");
Console.WriteLine($"Steady-state heap allocation across all cycles: {allocated} bytes.");
