namespace QosDemo;

public sealed class MessageReassembler
{
    // 每个进行中的消息维护一个分片桶：Key 是 (ChannelId, MessageSeq)
    private readonly Dictionary<(byte ChannelId, ushort MessageSeq), byte[][]> _pendingBuffers = new();

    // 尝试添加分片。如果所有分片集齐，返回完整重组后的 byte[]；否则返回 null
    public byte[]? AddFragment(in FragmentedPacketHeader header, ReadOnlySpan<byte> fragmentPayload)
    {
        // 单包消息（未分片）：直接返回数据
        if (header.FragmentTotal <= 1)
        {
            return fragmentPayload.ToArray();
        }

        var key = (header.ChannelId, header.MessageSeq);
        if (!_pendingBuffers.TryGetValue(key, out var fragments))
        {
            fragments = new byte[header.FragmentTotal][];
            _pendingBuffers[key] = fragments;
        }

        if (header.FragmentIndex < fragments.Length && fragments[header.FragmentIndex] == null)
        {
            fragments[header.FragmentIndex] = fragmentPayload.ToArray();
        }

        // 检查是否所有分片都已到位
        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null)
            {
                return null; // 仍有空洞，继续等待后续分片
            }
        }

        // 全部集齐，拼接完整载荷
        int totalBytes = 0;
        for (int i = 0; i < fragments.Length; i++)
        {
            totalBytes += fragments[i].Length;
        }

        byte[] fullPayload = new byte[totalBytes];
        int offset = 0;
        for (int i = 0; i < fragments.Length; i++)
        {
            fragments[i].CopyTo(fullPayload, offset);
            offset += fragments[i].Length;
        }

        // 清理缓存并交付
        _pendingBuffers.Remove(key);
        return fullPayload;
    }
}
