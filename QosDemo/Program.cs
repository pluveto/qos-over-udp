using QosDemo;

Console.WriteLine("=== Verification of ACK Feedback Integration ===");

var tracker = new InFlightTracker(capacity: 64);

// 发送端发出两个物理包：500 和 502（假设 501 丢了）
tracker.TrackPacket(500, 0, 1, 0, 1, DeliveryPolicy.Reliable, 0, 100, 1000, 0);
tracker.TrackPacket(501, 0, 2, 0, 1, DeliveryPolicy.Reliable, 0, 100, 1000, 0);
tracker.TrackPacket(502, 0, 3, 0, 1, DeliveryPolicy.Reliable, 0, 100, 1000, 0);

// 接收端生成 SACK：Base=500，第 1 位是 1（502 到达，偏移 1）
ushort baseSeq = 500;
uint sackMask = 0b0010; // 502 - 500 - 1 = 1

// 处理该 ACK
AckProcessor.ProcessAck(tracker, baseSeq, sackMask);

// 检查三个包的当前状态
tracker.CheckRetransmit(500, 1500, out var slot500);
tracker.CheckRetransmit(501, 1500, out var slot501);
tracker.CheckRetransmit(502, 1500, out var slot502);

Console.WriteLine($"Pkt 500 State: {slot500.State} (Expected: Acknowledged)");
Console.WriteLine($"Pkt 501 State: {slot501.State} (Expected: InFlight -> Needs Retransmit!)");
Console.WriteLine($"Pkt 502 State: {slot502.State} (Expected: Acknowledged)");
