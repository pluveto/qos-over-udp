namespace QosDemo;

public sealed class ConflationQueue
{
    // 按入队顺序记录待发送的 key
    private readonly LinkedList<string> _order = new();
    // 记录每个 key 对应的最新数据节点引用
    private readonly Dictionary<string, (LinkedListNode<string> Node, byte[] Payload)> _map = new();

    // 入队或者覆写
    public void EnqueueOrUpdate(string key, byte[] payload)
    {
        if (_map.TryGetValue(key, out var existing))
        {
            // Key 已存在：原地覆写数据，保持队列中已有的排队位置（或者移到末尾，这里原地更新最新值）
            _map[key] = (existing.Node, payload);
        }
        else
        {
            // 新 Key：加到队列末尾
            var node = _order.AddLast(key);
            _map[key] = (node, payload);
        }
    }

    // 出队：按先后顺序取出最新保留的单条数据
    public (string Key, byte[] Payload)? Dequeue()
    {
        if (_order.Count == 0) return null;

        string key = _order.First!.Value;
        _order.RemoveFirst();

        var payload = _map[key].Payload;
        _map.Remove(key);

        return (key, payload);
    }

    public int Count => _order.Count;
}
