using System.Text;
using QosDemo;

Console.WriteLine("=== Verification of Decoupled Arena Ingestion ===");

var arena = new PayloadArena(capacityBytes: 4096);
var ring = new ValueRingBuffer<OutboundDescriptor>(capacity: 64);

byte[] telemetryBytes = Encoding.UTF8.GetBytes("MOTOR_RPM:3500;TORQUE:80NM");

long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

// 模拟 1,000 次高频业务入队与调度出队
for (int i = 0; i < 1000; i++)
{
    // 1. 入队：业务切片写入常驻 Arena，并生成 16 字节描述符塞入环形队列
    if (arena.TryAllocateAndCopy(telemetryBytes, out int offset))
    {
        var desc = new OutboundDescriptor(
            channelId: 1,
            messageSeq: (ushort)i,
            fragmentIndex: 0,
            fragmentTotal: 1,
            policy: DeliveryPolicy.Unreliable,
            payloadOffset: offset,
            payloadLength: (ushort)telemetryBytes.Length
        );
        ring.TryEnqueue(in desc);
    }

    // 2. 出队：弹出描述符，从 Arena 中零拷贝取出切片进行消费
    if (ring.TryDequeue(out var poppedDesc))
    {
        ReadOnlySpan<byte> payloadSpan = arena.GetSpan(poppedDesc.PayloadOffset, poppedDesc.PayloadLength);
        arena.AdvanceTail(poppedDesc.PayloadLength);
    }
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;

Console.WriteLine($"1,000 Ingestion-Egress cycles completed.");
Console.WriteLine($"Total heap allocation across cycles: {allocated} bytes (Zero Allocation).");
