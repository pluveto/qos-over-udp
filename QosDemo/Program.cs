using QosDemo;

var filter = new SequenceWindowFilter();

// 模拟场景：
// 发送端发射了 Reliable 指令：MessageSeq = 100（"FIRE_BULLET"）
// 因为 ACK 丢包，发送端重传了两次：网卡共收到了 3 次 MessageSeq = 100
// 随后发送端继续发了 101, 102，并发生了轻微乱序重传

ushort[] incomingStream = {
    100, // 首次到达
    100, // 重传包到达（应该拦截）
    100, // 再次重传到达（应该拦截）
    102, // 乱序新包到达
    101, // 滞后的 101 到达（应该允许交付）
    101, // 101 重传到达（应该拦截）
    20   // 超大跨度的陈旧历史包（应该拦截）
};

Console.WriteLine("=== Ingress Sequence Filtering (Sliding Window Dedup) ===");
for (int i = 0; i < incomingStream.Length; i++)
{
    ushort seq = incomingStream[i];
    bool accepted = filter.TryAccept(seq);
    if (accepted)
    {
        Console.WriteLine($"Step {i + 1}: MsgSeq {seq} -> [ACCEPTED] Deliver to business logic!");
    }
    else
    {
        Console.WriteLine($"Step {i + 1}: MsgSeq {seq} -> [DROPPED] Duplicate or obsolete packet.");
    }
}
