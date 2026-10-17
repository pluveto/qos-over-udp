namespace QosDemo;

public enum TrackedSlotState : byte
{
    Free = 0,
    InFlight = 1,
    Acknowledged = 2
}

// 固定 32 字节的未确认包追踪槽位（值类型，预分配在数组中，零堆分配）
public struct InFlightSlot
{
    public ushort PacketSeq;
    public byte ChannelId;
    public ushort MessageSeq;
    public byte FragmentIndex;
    public byte FragmentTotal;
    public DeliveryPolicy Policy;
    public int PayloadOffset;
    public ushort PayloadLength;
    public long SentTimestampMs;
    public long TtlMs;
    public TrackedSlotState State;
}

public sealed class InFlightTracker
{
    private readonly InFlightSlot[] _slots;
    private readonly int _mask;

    public int Capacity { get; }

    public InFlightTracker(int capacity = 1024)
    {
        // 确保容量是 2 的次幂
        int cap = 2;
        while (cap < capacity) cap <<= 1;
        Capacity = cap;
        _mask = cap - 1;
        _slots = new InFlightSlot[cap];
    }

    // 发射时登记追踪槽位（O(1) 原地赋值）
    public void TrackPacket(
        ushort packetSeq, 
        byte channelId, 
        ushort messageSeq, 
        byte fragmentIndex, 
        byte fragmentTotal, 
        DeliveryPolicy policy, 
        int payloadOffset, 
        ushort payloadLength, 
        long sentTimestampMs, 
        long ttlMs)
    {
        int index = packetSeq & _mask;
        ref var slot = ref _slots[index];

        slot.PacketSeq = packetSeq;
        slot.ChannelId = channelId;
        slot.MessageSeq = messageSeq;
        slot.FragmentIndex = fragmentIndex;
        slot.FragmentTotal = fragmentTotal;
        slot.Policy = policy;
        slot.PayloadOffset = payloadOffset;
        slot.PayloadLength = payloadLength;
        slot.SentTimestampMs = sentTimestampMs;
        slot.TtlMs = ttlMs;
        slot.State = TrackedSlotState.InFlight;
    }

    // 收到 ACK 时标记为已确认（O(1) 原地更新）
    public bool MarkAcknowledged(ushort packetSeq)
    {
        int index = packetSeq & _mask;
        ref var slot = ref _slots[index];

        if (slot.State == TrackedSlotState.InFlight && slot.PacketSeq == packetSeq)
        {
            slot.State = TrackedSlotState.Acknowledged;
            return true;
        }

        return false;
    }

    // 检查指定包是否超时且需要重传（O(1)）
    public bool CheckRetransmit(ushort packetSeq, long nowMs, out InFlightSlot slotCopy)
    {
        int index = packetSeq & _mask;
        ref var slot = ref _slots[index];

        if (slot.State == TrackedSlotState.InFlight && slot.PacketSeq == packetSeq)
        {
            long elapsed = nowMs - slot.SentTimestampMs;
            bool shouldRetransmit = slot.Policy switch
            {
                DeliveryPolicy.Unreliable => false,
                DeliveryPolicy.Reliable => true,
                DeliveryPolicy.TimedReliable => elapsed <= slot.TtlMs,
                _ => false
            };

            slotCopy = slot;
            return shouldRetransmit;
        }

        slotCopy = default;
        return false;
    }
}
