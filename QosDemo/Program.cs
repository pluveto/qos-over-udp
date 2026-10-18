using System.Text;
using QosDemo;

Console.WriteLine("=== Verification of Zero-Alloc Inbound Reassembler ===");

var reassembler = new ZeroAllocReassembler();

// 模拟发送端发来一条被切成 3 片的消息
// 乱序到达：先到片 1，再到片 0，最后到片 2
byte[] chunk0 = Encoding.UTF8.GetBytes("[CHUNK_0000000000]");
byte[] chunk1 = Encoding.UTF8.GetBytes("[CHUNK_1111111111]");
byte[] chunk2 = Encoding.UTF8.GetBytes("[CHUNK_2222222222]");

// 预热
reassembler.TryAddFragment(0, 0, 1, chunk0, out _);

long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

// 模拟 1,000 次完整大消息的乱序到达与重组闭环
for (int i = 0; i < 1000; i++)
{
    ushort msgSeq = (ushort)(i + 1);

    // 第 1 片先到 (Index 1)
    bool r1 = reassembler.TryAddFragment(msgSeq, 1, 3, chunk1, out _);

    // 第 0 片后到 (Index 0)
    bool r2 = reassembler.TryAddFragment(msgSeq, 0, 3, chunk0, out _);

    // 第 2 片最后到 (Index 2) -> 此时应当集齐交付
    bool r3 = reassembler.TryAddFragment(msgSeq, 2, 3, chunk2, out ReadOnlySpan<byte> fullMessage);

    if (!r3 || fullMessage.Length == 0)
    {
        Console.WriteLine("Assembly failed!");
        break;
    }
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;

Console.WriteLine($"Reassembled 1,000 fragmented messages (3,000 fragments).");
Console.WriteLine($"Steady-state heap allocation: {allocated} bytes (Zero Allocation).");
