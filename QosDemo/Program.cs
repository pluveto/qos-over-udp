using System.Text;
using QosDemo;

// 模拟发送端：在同一个租借缓冲区内完成业务载荷与报头的拼接，零二次拷贝与零分配
using var lease = new BufferLease(100);

byte[] rawBusinessData = Encoding.UTF8.GetBytes("HIGH_PRECISION_TELEMETRY");
// 将业务数据直接拷贝到租借缓冲区的 Payload 区域（偏移 7 之后）
rawBusinessData.CopyTo(lease.Span.Slice(ZeroAllocFraming.HeaderSize));

// 就地写包头
ZeroAllocFraming.WriteHeader(
    destination: lease.Span,
    channelId: 3,
    packetSeq: 888,
    messageSeq: 12,
    fragmentIndex: 0,
    fragmentTotal: 1
);

int actualPacketLength = ZeroAllocFraming.HeaderSize + rawBusinessData.Length;
ReadOnlySpan<byte> packetSpan = lease.Span.Slice(0, actualPacketLength);

Console.WriteLine($"Wire packet encoded: {packetSpan.Length} bytes.");

// 模拟接收端：直接在入站 Span 上进行零拷贝解析
long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

if (ZeroAllocFraming.TryReadHeader(
    source: packetSpan,
    out byte ch, 
    out ushort pktSeq, 
    out ushort msgSeq, 
    out byte fragIdx, 
    out byte fragTotal, 
    out ReadOnlySpan<byte> payload))
{
    long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
    string payloadText = Encoding.UTF8.GetString(payload);
    Console.WriteLine($"Parsed: Ch={ch}, PktSeq={pktSeq}, MsgSeq={msgSeq}, Frag={fragIdx+1}/{fragTotal}, Text='{payloadText}'");
    Console.WriteLine($"Allocation during parsing: {afterAlloc - beforeAlloc} bytes.");
}
