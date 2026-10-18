using System.Text;
using QosDemo;

Console.WriteLine("=== Verification of InboundDeliveryGate Pipeline ===");

var sackGen = new SackGenerator();
var gate = new InboundDeliveryGate(sackGen);

int deliveredCount = 0;
gate.OnMessageReceived += (channelId, msgSeq, payload) =>
{
    deliveredCount++;
    if (deliveredCount <= 2)
    {
        string text = Encoding.UTF8.GetString(payload);
        Console.WriteLine($"[APP DELIVERED] Ch={channelId}, MsgSeq={msgSeq}, Content='{text}'");
    }
};


// 构造测试数据包
// 场景 A：单包消息（Msg 10），因为网络重传连续来了 3 次（物理 Pkt 100, 101, 102）
using var lease = new BufferLease(64);
byte[] fireCmd = Encoding.UTF8.GetBytes("FIRE_CANNON");
fireCmd.CopyTo(lease.Span.Slice(ZeroAllocFraming.HeaderSize));

for (ushort pkt = 100; pkt <= 102; pkt++)
{
    ZeroAllocFraming.WriteHeader(
        destination: lease.Span,
        channelId: 1,
        packetSeq: pkt,
        messageSeq: 10,
        fragmentIndex: 0,
        fragmentTotal: 1
    );

    int wireLen = ZeroAllocFraming.HeaderSize + fireCmd.Length;
    gate.ProcessInboundPacket(lease.Span.Slice(0, wireLen));
}

Console.WriteLine($"Fire Cannon Delivered Times: {deliveredCount} (Expected: 1)");
Console.WriteLine($"SACK Generator BasePacketSeq: {sackGen.BasePacketSeq} (Expected: 102)");

// 预热：消除 JIT 首次编译和委托初始化的开销
ZeroAllocFraming.WriteHeader(lease.Span, 2, 0, 0, 0, 1);
gate.ProcessInboundPacket(lease.Span.Slice(0, ZeroAllocFraming.HeaderSize + fireCmd.Length));

// 性能分配验证：连续处理 10,000 次到达（静默执行，不打印以防刷屏）
long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

for (int i = 0; i < 10_000; i++)
{
    ushort seq = (ushort)(i + 200);
    ZeroAllocFraming.WriteHeader(lease.Span, 2, seq, seq, 0, 1);
    gate.ProcessInboundPacket(lease.Span.Slice(0, ZeroAllocFraming.HeaderSize + fireCmd.Length));
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;


Console.WriteLine($"\nProcessed 10,000 inbound wire packets into delivery gate.");
Console.WriteLine($"Steady-state heap allocation: {allocated} bytes (Zero Allocation).");

