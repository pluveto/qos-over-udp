using System.Text;
using QosDemo;

// 模拟发送端发出了 3 个数据包：
// 1. 遥测坐标：不可靠（丢了就算了）
// 2. 扣费订单：强可靠（必须死磕到底）
// 3. 战斗技能：部分可靠（TTL = 100ms，超过 100ms 就算重传到也没意义了）

long now = 1000; // 当前时间戳 1000ms

var p1 = new TrackedPacket(
    packetSeq: 101, channelId: 0, messageSeq: 1,
    payload: Encoding.UTF8.GetBytes("COORD_X_10"),
    policy: DeliveryPolicy.Unreliable,
    sentTimestampMs: now
);

var p2 = new TrackedPacket(
    packetSeq: 102, channelId: 1, messageSeq: 1,
    payload: Encoding.UTF8.GetBytes("ORDER_PAY_100"),
    policy: DeliveryPolicy.Reliable,
    sentTimestampMs: now
);

var p3 = new TrackedPacket(
    packetSeq: 103, channelId: 2, messageSeq: 1,
    payload: Encoding.UTF8.GetBytes("CAST_FIREBALL"),
    policy: DeliveryPolicy.TimedReliable,
    sentTimestampMs: now,
    ttlMs: 100
);

List<TrackedPacket> inflightPackets = new() { p1, p2, p3 };

// 模拟场景 A：时间推进了 50ms（当前时间 1050ms），收到 ACK 发现 3 个包全都没确认
Console.WriteLine("=== Check at T = 1050ms (Elapsed 50ms) ===");
long t1 = 1050;
foreach (var pkt in inflightPackets)
{
    bool retransmit = pkt.ShouldRetransmit(t1);
    Console.WriteLine($"Pkt {pkt.PacketSeq} [{pkt.Policy}]: ShouldRetransmit = {retransmit}");
}

// 模拟场景 B：时间推进了 150ms（当前时间 1150ms），网络依然卡顿
Console.WriteLine("\n=== Check at T = 1150ms (Elapsed 150ms) ===");
long t2 = 1150;
foreach (var pkt in inflightPackets)
{
    bool retransmit = pkt.ShouldRetransmit(t2);
    Console.WriteLine($"Pkt {pkt.PacketSeq} [{pkt.Policy}]: ShouldRetransmit = {retransmit}");
}
