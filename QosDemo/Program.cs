using System.Text;
using QosDemo;

Console.WriteLine("=== Verification of Real Socket Zero-Alloc Pipeline ===");

int portA = 19001;
int portB = 19002;

using var nodeA = new UdpTransportEngine(localPort: portA, remotePort: portB);
using var nodeB = new UdpTransportEngine(localPort: portB, remotePort: portA);

// 预先准备好要发送的静态测试载荷，避免测试代码本身的 string/byte[] 干扰引擎指标
byte[] staticPayload = Encoding.UTF8.GetBytes("STATIC_PAYLOAD_TEST");

// 预热：让操作系统和运行时网络栈完成初始化
using (var warmupLease = new BufferLease(64))
{
    nodeA.Send(warmupLease.Span);
    nodeB.Receive();
}

long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

// 连续进行 1,000 次通过真实操作系统内核网络栈的 UDP 发射与接收
for (int i = 0; i < 1000; i++)
{
    // 1. 发送端：池化租借并物理发射
    using (var lease = new BufferLease(64))
    {
        staticPayload.CopyTo(lease.Span.Slice(ZeroAllocFraming.HeaderSize));

        ZeroAllocFraming.WriteHeader(
            destination: lease.Span,
            channelId: 2,
            packetSeq: (ushort)i,
            messageSeq: (ushort)i,
            fragmentIndex: 0,
            fragmentTotal: 1
        );

        int wireLength = ZeroAllocFraming.HeaderSize + staticPayload.Length;
        nodeA.Send(lease.Span.Slice(0, wireLength));
    }

    // 2. 接收端：网络抓包并切片解析
    int bytesReceived = nodeB.Receive();
    ReadOnlySpan<byte> receivedSpan = nodeB.GetReceivedSpan(bytesReceived);

    ZeroAllocFraming.TryReadHeader(
        receivedSpan,
        out byte ch,
        out ushort pktSeq,
        out ushort msgSeq,
        out byte fragIdx,
        out byte fragTotal,
        out ReadOnlySpan<byte> payload
    );
}

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;

Console.WriteLine($"Transmitted 1,000 real UDP datagrams across real OS socket.");
Console.WriteLine($"Steady-state allocation for 1,000 socket round-trips: {allocated} bytes.");
