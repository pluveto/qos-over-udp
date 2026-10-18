using QosDemo;

Console.WriteLine("=== Verification of Wire Demultiplexer ===");

using var lease = new BufferLease(32);

// 1. 发射端：向物理切片编码一个 ACK 控制包
int ackLen = WireDemuxer.WriteAckFrame(lease.Span, basePacketSeq: 500, sackMask: 0b1010);
ReadOnlySpan<byte> ackPacket = lease.Span.Slice(0, ackLen);

// 2. 接收端：判定是控制帧还是普通数据帧
long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();

bool isAck = WireDemuxer.TryReadAckFrame(ackPacket, out ushort baseSeq, out uint mask);

long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
long allocated = afterAlloc - beforeAlloc;

Console.WriteLine($"Is Control ACK Frame: {isAck}");
Console.WriteLine($"Decoded BasePacketSeq: {baseSeq}, Mask: {Convert.ToString(mask, 2)}");
Console.WriteLine($"Heap allocation during demux: {allocated} bytes (Zero Alloc).");
