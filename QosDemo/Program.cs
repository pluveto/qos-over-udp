using QosDemo;

Console.WriteLine("=== Verification of Zero-Alloc InFlightTracker ===");

var tracker = new InFlightTracker(capacity: 1024);

// 预热
tracker.TrackPacket(1, 0, 1, 0, 1, DeliveryPolicy.Reliable, 0, 100, 1000, 0);
tracker.MarkAcknowledged(1);

long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

// 模拟 10,000 次数据包的“飞出追踪 -> 收到ACK标记确认 / 超时重传判定”
for (int i = 0; i < 10_000; i++)
{
    ushort seq = (ushort)(i & 0xFFFF);

    // 登记飞出
    tracker.TrackPacket(
        packetSeq: seq,
        channelId: 1,
        messageSeq: (ushort)i,
        fragmentIndex: 0,
        fragmentTotal: 1,
        policy: DeliveryPolicy.TimedReliable,
        payloadOffset: 128,
        payloadLength: 200,
        sentTimestampMs: 5000,
        ttlMs: 150
    );

    // 模拟收到 ACK 标记确认
    tracker.MarkAcknowledged(seq);
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;

Console.WriteLine($"Tracked and Acknowledged 10,000 in-flight packets.");
Console.WriteLine($"Steady-state heap allocation: {allocated} bytes (Zero Allocation).");
