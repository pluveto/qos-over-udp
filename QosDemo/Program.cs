using System.Text;
using QosDemo;

// 模拟发送端状态：
ushort globalPacketSeq = 100;
ushort channel0MsgSeq = 1;
ushort channel1MsgSeq = 1;

// 1. 发送通道 0 的第一部分数据
byte[] p1 = PacketHeader.Encode(
    channelId: 0, 
    packetSeq: globalPacketSeq++, 
    messageSeq: channel0MsgSeq++, 
    Encoding.UTF8.GetBytes("IMG_PART_1")
);

// 2. 发送通道 0 的第二部分数据（假设它在网络中丢失了）
byte[] p2 = PacketHeader.Encode(
    channelId: 0, 
    packetSeq: globalPacketSeq++, 
    messageSeq: channel0MsgSeq++, 
    Encoding.UTF8.GetBytes("IMG_PART_2")
);

// 3. 发送通道 1 的心跳指令
byte[] p3 = PacketHeader.Encode(
    channelId: 1, 
    packetSeq: globalPacketSeq++, 
    messageSeq: channel1MsgSeq++, 
    Encoding.UTF8.GetBytes("PING")
);

Console.WriteLine("--- Network Transmission ---");
Console.WriteLine($"Sent: Pkt {100} (Ch 0, Msg 1)");
Console.WriteLine($"Sent: Pkt {101} (Ch 0, Msg 2) [SIMULATED DROP]");
Console.WriteLine($"Sent: Pkt {102} (Ch 1, Msg 1)");

Console.WriteLine("\n--- Receiver Processing ---");
// 接收端维护每个通道最后处理完成的 MessageSeq（初始为 0）
Dictionary<byte, ushort> expectedMsgSeq = new() { { 0, 1 }, { 1, 1 } };

void Receive(byte[] rawPacket)
{
    if (!PacketHeader.TryDecode(rawPacket, out var header, out var payload))
        return;

    ushort expected = expectedMsgSeq[header.ChannelId];
    string content = Encoding.UTF8.GetString(payload.Span);

    if (header.MessageSeq == expected)
    {
        Console.WriteLine($"[Ch {header.ChannelId}] Delivered Msg {header.MessageSeq}: '{content}' (Global Pkt {header.PacketSeq})");
        expectedMsgSeq[header.ChannelId]++;
    }
    else if (header.MessageSeq > expected)
    {
        Console.WriteLine($"[Ch {header.ChannelId}] Blocked! Got Msg {header.MessageSeq}, but waiting for Msg {expected} (Global Pkt {header.PacketSeq})");
    }
}

// 模拟到达顺序：p1 到达，p2 丢失，p3 到达
Receive(p1);
// p2 被路由器丢弃，未调用 Receive(p2)
Receive(p3);
