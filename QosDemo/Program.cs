using System.Text;
using QosDemo;

var heartbeatData = Encoding.UTF8.GetBytes("PING");
var fireData = Encoding.UTF8.GetBytes("FIRE_MISSILE");

// 发送端：将不同业务数据打上不同的 Channel ID
byte[] packet1 = ChannelMux.Encode(channelId: 0, heartbeatData);
byte[] packet2 = ChannelMux.Encode(channelId: 1, fireData);

// 接收端：收到两个原始 UDP 数据包
void Dispatch(byte[] rawPacket)
{
    if (ChannelMux.TryDecode(rawPacket, out byte channelId, out var payload))
    {
        string message = Encoding.UTF8.GetString(payload.Span);
        switch (channelId)
        {
            case 0:
                Console.WriteLine($"[Heartbeat Channel] Received: {message}");
                break;
            case 1:
                Console.WriteLine($"[Control Channel] Received: {message}");
                break;
            default:
                Console.WriteLine($"[Unknown Channel {channelId}]");
                break;
        }
    }
}

Dispatch(packet1);
Dispatch(packet2);
