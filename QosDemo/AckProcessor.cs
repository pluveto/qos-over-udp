namespace QosDemo;

// 负责将收到的 ACK 控制帧（BaseSeq + SackMask）还原为已确认的物理包号，并驱动 InFlightTracker 更新
public static class AckProcessor
{
    // 处理一个 ACK 帧，将所有被该 ACK 确认的包从追踪表中核销
    public static void ProcessAck(InFlightTracker tracker, ushort basePacketSeq, uint sackMask)
    {
        // 1. 基准号对应的包必然已被累计确认
        tracker.MarkAcknowledged(basePacketSeq);

        // 2. 检查 32 位 SACK 位图，核销非连续到达的包
        if (sackMask == 0) return;

        for (int i = 0; i < 32; i++)
        {
            if (((sackMask >> i) & 1u) == 1u)
            {
                ushort ackedSeq = (ushort)(basePacketSeq + 1 + i);
                tracker.MarkAcknowledged(ackedSeq);
            }
        }
    }
}
