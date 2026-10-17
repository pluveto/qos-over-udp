using QosDemo;

Console.WriteLine("=== Verification of Zero-Alloc DRR Scheduler ===");

// 构造两个平级通道：每轮分配 500 字节信用额度 (Quantum = 500)
var chImage = new ZeroAllocDrrChannel(channelId: 0, quantum: 500, ringCapacity: 1024);
var chChat = new ZeroAllocDrrChannel(channelId: 1, quantum: 500, ringCapacity: 1024);

var scheduler = new ZeroAllocDrrScheduler(new[] { chImage, chChat });

// 预热
var dummy = new OutboundDescriptor(0, 0, 0, 1, DeliveryPolicy.Unreliable, 100, 0);
scheduler.TryEnqueue(0, in dummy);
scheduler.TryDequeueNext(out _);

long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

// 模拟 10,000 次调度运转：
// 通道 0 灌入大包（载荷 793B + 7B头 = 800B）
// 通道 1 灌入小包（载荷 193B + 7B头 = 200B）
for (int i = 0; i < 5000; i++)
{
    var bigDesc = new OutboundDescriptor(0, (ushort)i, 0, 1, DeliveryPolicy.Reliable, 793, 1000);
    var smallDesc = new OutboundDescriptor(1, (ushort)i, 0, 1, DeliveryPolicy.Unreliable, 193, 1000);

    scheduler.TryEnqueue(0, in bigDesc);
    scheduler.TryEnqueue(1, in smallDesc);

    // 调度出队
    while (scheduler.TryDequeueNext(out var popped))
    {
        // 模拟向网卡投递（零分配消费）
    }
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;

Console.WriteLine($"10,000 High-Throughput DRR Operations Allocated: {allocated} bytes.");
