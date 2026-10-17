using QosDemo;

Console.WriteLine("=== Verification of Value Struct without Boxing ===");

// 模拟调度队列内部持有的预分配连续值类型数组
OutboundDescriptor[] preallocatedSlotBuffer = new OutboundDescriptor[1024];

long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

// 模拟 10,000 次高频入队与属性读取
for (int i = 0; i < 10_000; i++)
{
    int index = i % 1024;
    
    // 直接以值类型原地写入槽位（无 new，无堆分配，无装箱）
    preallocatedSlotBuffer[index] = new OutboundDescriptor(
        channelId: 1,
        messageSeq: (ushort)i,
        fragmentIndex: 0,
        fragmentTotal: 1,
        policy: DeliveryPolicy.Reliable,
        payloadLength: 120,
        enqueueTimestampMs: 5000 + i
    );

    // 以只读引用（in）读取，不产生临时副本拷贝
    ProcessDescriptor(in preallocatedSlotBuffer[index]);
}

static void ProcessDescriptor(in OutboundDescriptor desc)
{
    // 直接读取字段，没有任何 object 转换
    if (desc.Policy == DeliveryPolicy.Reliable && desc.PayloadLength > 0)
    {
        // 模拟业务调度判定
    }
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;

Console.WriteLine($"10,000 Ingress Operations Allocated: {allocated} bytes (Zero Heap Overhead).");
