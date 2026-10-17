using System.Text;
using QosDemo;

var queue = new ConflationQueue();

Console.WriteLine("--- Rapid Ingress: 5 consecutive vehicle speeds generated ---");
// 模拟 10ms 生成一个车速，当前网络卡顿，没有网卡发送动作，全部积压在队列中
queue.EnqueueOrUpdate("vehicle_speed", Encoding.UTF8.GetBytes("SPEED: 40 km/h"));
queue.EnqueueOrUpdate("vehicle_speed", Encoding.UTF8.GetBytes("SPEED: 42 km/h"));
queue.EnqueueOrUpdate("vehicle_speed", Encoding.UTF8.GetBytes("SPEED: 45 km/h"));
queue.EnqueueOrUpdate("vehicle_speed", Encoding.UTF8.GetBytes("SPEED: 47 km/h"));
queue.EnqueueOrUpdate("vehicle_speed", Encoding.UTF8.GetBytes("SPEED: 50 km/h"));

// 同时队列里还掺杂了另一个 key 的数据（比如电池电量）
queue.EnqueueOrUpdate("battery_soc", Encoding.UTF8.GetBytes("BATTERY: 88%"));

Console.WriteLine($"Queue size in memory: {queue.Count} items (instead of 6 items)\n");

Console.WriteLine("--- Network Recovery: Egress Transmission ---");
while (queue.Count > 0)
{
    var item = queue.Dequeue();
    if (item != null)
    {
        string val = Encoding.UTF8.GetString(item.Value.Payload);
        Console.WriteLine($"Egress Transmitting -> Key: '{item.Value.Key}', Value: '{val}'");
    }
}
