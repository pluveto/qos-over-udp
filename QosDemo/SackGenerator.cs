namespace QosDemo;

// 接收端 SACK 动态生成器（基于 32 位位图，零堆分配）
public sealed class SackGenerator
{
    private ushort _basePacketSeq;
    private uint _sackMask;
    private bool _initialized;

    public ushort BasePacketSeq => _basePacketSeq;
    public uint SackMask => _sackMask;

    public SackGenerator()
    {
        _basePacketSeq = 0;
        _sackMask = 0;
        _initialized = false;
    }

    // 每收到一个物理包（无论是哪个业务通道），向生成器汇报其全局 PacketSeq
    public void RecordReceived(ushort packetSeq)
    {
        if (!_initialized)
        {
            _initialized = true;
            _basePacketSeq = packetSeq;
            _sackMask = 0;
            return;
        }

        // 情况 1：收到小于等于基准序号的包（历史旧包或重复包），直接忽略
        if (packetSeq <= _basePacketSeq)
        {
            return;
        }

        // 情况 2：刚好收到期待的下一个连续包（packetSeq == _basePacketSeq + 1）
        if (packetSeq == _basePacketSeq + 1)
        {
            _basePacketSeq++;
            // 检查位图中后续紧邻的包是否之前已经提前到达，若已到达则连续推进基准并右移位图
            while ((_sackMask & 1u) != 0)
            {
                _sackMask >>= 1;
                _basePacketSeq++;
            }
            return;
        }

        // 情况 3：收到跳跃包或填补之前空洞的乱序包
        int offset = packetSeq - _basePacketSeq - 1;
        if (offset >= 0 && offset < 32)
        {
            // 在 32 位位图中标记该位置为 1
            _sackMask |= (1u << offset);
        }


        else
        {
            // 跨度超过 32，强制跃进基准（避免位图溢出）
            int shift = offset - 31;
            _basePacketSeq += (ushort)shift;
            _sackMask >>= shift;
            _sackMask |= (1u << 31);
        }
    }
}
