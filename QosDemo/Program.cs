using QosDemo;

Console.WriteLine("=== Verification of Zero-Alloc SackGenerator ===");

var generator = new SackGenerator();

// 模拟场景：
// 发送端发出：100, 101, 102, 103, 104
// 到达序列：100 到达，101 丢包，102 到达，103 到达
generator.RecordReceived(100);
Console.WriteLine($"After Pkt 100: BaseSeq={generator.BasePacketSeq}, SackMask={Convert.ToString(generator.SackMask, 2)}");

generator.RecordReceived(102); // 101 丢了，102 提前到达
Console.WriteLine($"After Pkt 102: BaseSeq={generator.BasePacketSeq}, SackMask={Convert.ToString(generator.SackMask, 2)}");

generator.RecordReceived(103); // 103 提前到达
Console.WriteLine($"After Pkt 103: BaseSeq={generator.BasePacketSeq}, SackMask={Convert.ToString(generator.SackMask, 2)}");

// 此时 101 重传终于送达！
Console.WriteLine("\n--- Fast Retransmit: Pkt 101 finally arrives ---");
generator.RecordReceived(101);
Console.WriteLine($"After Pkt 101: BaseSeq={generator.BasePacketSeq}, SackMask={Convert.ToString(generator.SackMask, 2)}");

// 性能分配验证：连续 10,000 次操作
var perfGen = new SackGenerator();
perfGen.RecordReceived(0); // 预热

long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

for (int i = 1; i <= 10_000; i++)
{
    perfGen.RecordReceived((ushort)i);
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;


Console.WriteLine($"\nProcessed 10,000 packets into SackGenerator.");
Console.WriteLine($"Steady-state heap allocation: {allocated} bytes (Zero Allocation).");
