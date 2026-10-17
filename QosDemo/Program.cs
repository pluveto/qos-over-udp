using System.Text;
using QosDemo;

// 模拟场景：
// 发送端连续发出 5 个包：Pkt 100, 101, 102, 103, 104
// 其中 Pkt 101 和 103 在路上丢了！
// 接收端实际上收到了：100, 102, 104

ushort baseSeq = 100; // 连续收到的最高号是 100
uint sackMask = 0;

// 102 对应的偏移是: 102 - 100 - 1 = 1 (第 1 位置 1)
// 104 对应的偏移是: 104 - 100 - 1 = 3 (第 3 位置 1)
sackMask |= (1u << 1);
sackMask |= (1u << 3);

byte[] rawAck = AckFrame.Encode(baseSeq, sackMask);

// 发送端收到并解析这个 ACK
if (AckFrame.TryDecode(rawAck, out var ack))
{
    Console.WriteLine($"Received ACK: BaseSeq={ack.BasePacketSeq}, Mask={Convert.ToString(ack.SackMask, 2).PadLeft(4, '0')}");

    for (ushort seq = 100; seq <= 104; seq++)
    {
        bool ok = ack.IsAcknowledged(seq);
        if (ok)
        {
            Console.WriteLine($"  Packet {seq}: Confirmed received.");
        }
        else
        {
            Console.WriteLine($"  Packet {seq}: MISSING -> Need Retransmit!");
        }
    }
}
