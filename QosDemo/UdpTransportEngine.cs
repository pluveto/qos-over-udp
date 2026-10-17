using System.Net;
using System.Net.Sockets;

namespace QosDemo;

// 负责管理底层真实单一 UDP 端口的收发引擎（.NET 8 SocketAddress 原生零分配）
public sealed class UdpTransportEngine : IDisposable
{
    private readonly Socket _socket;
    private readonly SocketAddress _remoteAddress;
    private readonly SocketAddress _receiveAddress;
    private readonly byte[] _receiveArray;

    public UdpTransportEngine(int localPort, int remotePort)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Loopback, localPort));
        
        var remoteEp = new IPEndPoint(IPAddress.Loopback, remotePort);
        _remoteAddress = remoteEp.Serialize();
        _receiveAddress = new SocketAddress(AddressFamily.InterNetwork);

        _receiveArray = new byte[2048];
    }

    // 基于 Span 与只读 SocketAddress 发送（零堆分配）
    public int Send(ReadOnlySpan<byte> datagram)
    {
        return _socket.SendTo(datagram, SocketFlags.None, _remoteAddress);
    }

    // 基于 Span 与可变 SocketAddress 接收（零堆分配）
    public int Receive()
    {
        return _socket.ReceiveFrom(_receiveArray.AsSpan(), SocketFlags.None, _receiveAddress);
    }

    public ReadOnlySpan<byte> GetReceivedSpan(int bytesReceived)
    {
        return _receiveArray.AsSpan(0, bytesReceived);
    }

    public void Dispose()
    {
        _socket.Dispose();
    }
}
