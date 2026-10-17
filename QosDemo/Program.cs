using System.Text;
using QosDemo;

// 待发送的业务大数据（比如一张 50 字节的微缩图片）
string largeDataString = "[IMAGE_DATA:1234567890_ABCDEFGHIJ_KLMNOPQRST_UVWXYZ]";
byte[] largePayload = Encoding.UTF8.GetBytes(largeDataString);

int maxFragmentSize = 20; // 假定每个 UDP 包载荷最多装 20 字节
byte totalFragments = (byte)Math.Ceiling((double)largePayload.Length / maxFragmentSize);

Console.WriteLine($"Original Data Length: {largePayload.Length} bytes, Sliced into {totalFragments} fragments.\n");

ushort globalPacketSeq = 500;
ushort channel0MsgSeq = 42; // 这张大图片对应 Channel 0 的第 42 号消息

// 切片并模拟物理打包
List<byte[]> slices = new();
for (byte i = 0; i < totalFragments; i++)
{
    int offset = i * maxFragmentSize;
    int length = Math.Min(maxFragmentSize, largePayload.Length - offset);
    ReadOnlySpan<byte> sliceSpan = largePayload.AsSpan(offset, length);

    byte[] packet = FragmentedPacketHeader.Encode(
        channelId: 0,
        packetSeq: globalPacketSeq++,
        messageSeq: channel0MsgSeq,
        fragmentIndex: i,
        fragmentTotal: totalFragments,
        fragmentPayload: sliceSpan
    );
    slices.Add(packet);
}

// 接收端解析并验证每一个切片
for (int i = 0; i < slices.Count; i++)
{
    if (FragmentedPacketHeader.TryDecode(slices[i], out var header, out var payload))
    {
        string chunkStr = Encoding.UTF8.GetString(payload.Span);
        Console.WriteLine($"Received Fragment: Ch={header.ChannelId}, MsgSeq={header.MessageSeq}, Frag={header.FragmentIndex + 1}/{header.FragmentTotal}, GlobalPkt={header.PacketSeq}, Content='{chunkStr}'");
    }
}
