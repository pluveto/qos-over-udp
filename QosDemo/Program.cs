using System.Text;
using QosDemo;

// 缓冲区容量 5 个，高水位线 80% (4个)，低水位线 40% (2个)
var buffer = new BufferManager(capacity: 5, highRatio: 0.8, lowRatio: 0.4);

buffer.OnBackpressureTriggered += () =>
{
    Console.WriteLine("[EVENT] High Watermark Hit! Backpressure ACTIVE. Upper layers must slow down.");
};

buffer.OnBackpressureReleased += () =>
{
    Console.WriteLine("[EVENT] Low Watermark Hit! Backpressure RELEASED. Normal ingestion resumed.");
};

Console.WriteLine("--- Phase 1: Ingress & Watermark Rising ---");
long t0 = 1000;

// 入队 4 个包：第 4 个包触发高水位线
buffer.TryEnqueue(0, Encoding.UTF8.GetBytes("SENSOR_1"), t0, maxQueueDurationMs: 100);
buffer.TryEnqueue(0, Encoding.UTF8.GetBytes("SENSOR_2"), t0, maxQueueDurationMs: 100);
buffer.TryEnqueue(0, Encoding.UTF8.GetBytes("SENSOR_3"), t0, maxQueueDurationMs: 100);
buffer.TryEnqueue(0, Encoding.UTF8.GetBytes("SENSOR_4"), t0, maxQueueDurationMs: 100);

// 入队第 5 个包，达到容量上限
buffer.TryEnqueue(0, Encoding.UTF8.GetBytes("SENSOR_5"), t0, maxQueueDurationMs: 100);

// 尝试入队第 6 个包，触发 Tail Drop
bool enq6 = buffer.TryEnqueue(0, Encoding.UTF8.GetBytes("SENSOR_6"), t0, maxQueueDurationMs: 100);
Console.WriteLine($"Enqueue 6th packet result (Tail Drop): {enq6}");

Console.WriteLine($"\nCurrent Buffer Count: {buffer.Count}, Backpressure Active: {buffer.IsBackpressureActive}\n");

Console.WriteLine("--- Phase 2: Time Passes (T = 1150ms, 150ms passed) & Egress ---");
// 设定当前时间为 1150ms，之前入队的 SENSOR 1~5 均已在队列中滞留 150ms（超过了 100ms 最大排队时间）
long t1 = 1150;

// 出队循环：验证超时包是否被过滤，合法包是否正常出队
while (buffer.Count > 0)
{
    var msg = buffer.DequeueValid(t1);
    if (msg != null)
    {
        string text = Encoding.UTF8.GetString(msg.Value.Payload);
        Console.WriteLine($"[EGRESS DELIVER] Ch={msg.Value.ChannelId}: '{text}' sent to network.");
    }
}

// 此时排空旧包后，新入队一条即时紧急指令并出队
buffer.TryEnqueue(1, Encoding.UTF8.GetBytes("LIVE_STOP"), t1, maxQueueDurationMs: 500);
var liveMsg = buffer.DequeueValid(t1);
if (liveMsg != null)
{
    string text = Encoding.UTF8.GetString(liveMsg.Value.Payload);
    Console.WriteLine($"[EGRESS DELIVER] Ch={liveMsg.Value.ChannelId}: '{text}' sent to network.");
}


Console.WriteLine($"Final Buffer Count: {buffer.Count}, Backpressure Active: {buffer.IsBackpressureActive}");
