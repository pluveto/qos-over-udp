namespace QosDemo;

public static class ChannelMux
{
    // 将 channelId 与业务载荷编码成一个完整的物理包
    public static byte[] Encode(byte channelId, ReadOnlySpan<byte> payload)
    {
        byte[] packet = new byte[1 + payload.Length];
        packet[0] = channelId;
        payload.CopyTo(packet.AsSpan(1));
        return packet;
    }

    // 从物理包中解出 channelId 与实际业务载荷
    public static bool TryDecode(ReadOnlySpan<byte> packet, out byte channelId, out ReadOnlyMemory<byte> payload)
    {
        if (packet.Length < 1)
        {
            channelId = 0;
            payload = ReadOnlyMemory<byte>.Empty;
            return false;
        }

        channelId = packet[0];
        // 截取通道 ID 之后的数据作为业务载荷
        payload = packet.Slice(1).ToArray();
        return true;
    }
}
