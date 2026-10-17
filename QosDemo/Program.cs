using System.Text;
using QosDemo;

var reassembler = new MessageReassembler();

// 模拟发送交错到达的三个物理包：
// 1. 大图片的第一块 (Ch 0, Msg 42, Frag 0/2)
// 2. 紧急心跳指令 (Ch 1, Msg 1, Frag 0/1) —— 强行插入中间！
// 3. 大图片的第二块 (Ch 0, Msg 42, Frag 1/2)

byte[] pkt1 = FragmentedPacketHeader.Encode(
    channelId: 0, packetSeq: 100, messageSeq: 42,
    fragmentIndex: 0, fragmentTotal: 2,
    Encoding.UTF8.GetBytes("[IMAGE_LEFT]")
);

byte[] pkt2 = FragmentedPacketHeader.Encode(
    channelId: 1, packetSeq: 101, messageSeq: 1,
    fragmentIndex: 0, fragmentTotal: 1,
    Encoding.UTF8.GetBytes("URGENT_PING")
);

byte[] pkt3 = FragmentedPacketHeader.Encode(
    channelId: 0, packetSeq: 102, messageSeq: 42,
    fragmentIndex: 1, fragmentTotal: 2,
    Encoding.UTF8.GetBytes("[IMAGE_RIGHT]")
);

// 接收端统一接收循环
void ProcessIncoming(byte[] rawPacket)
{
    if (!FragmentedPacketHeader.TryDecode(rawPacket, out var header, out var payload))
        return;

    byte[]? fullMessage = reassembler.AddFragment(header, payload.Span);
    if (fullMessage != null)
    {
        string text = Encoding.UTF8.GetString(fullMessage);
        Console.WriteLine($"[DELIVERED] Ch={header.ChannelId}, MsgSeq={header.MessageSeq}: \"{text}\"");
    }
    else
    {
        Console.WriteLine($"[BUFFERING] Ch={header.ChannelId}, MsgSeq={header.MessageSeq}: Waiting for remaining fragments...");
    }
}

Console.WriteLine("Step 1: Receive Pkt 100 (Image Part 1)");
ProcessIncoming(pkt1);

Console.WriteLine("\nStep 2: Receive Pkt 101 (Interleaved Urgent Ping)");
ProcessIncoming(pkt2);

Console.WriteLine("\nStep 3: Receive Pkt 102 (Image Part 2)");
ProcessIncoming(pkt3);
