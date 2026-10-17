using System.Text;
using QosDemo;

var scheduler = new DeficitRoundRobinScheduler();

// 设置两个平级通道，每轮分配相同的 500 字节信用额度 (Quantum = 500)
// 通道 0：发大图切片，每个包 800 字节
// 通道 1：发聊天文字，每个包 200 字节
var qImage = new DrrQueue(channelId: 0, quantum: 500);
var qChat = new DrrQueue(channelId: 1, quantum: 500);

scheduler.AddQueue(qImage);
scheduler.AddQueue(qChat);

// 入队待发数据：
// 通道 0 塞入 2 个 800 字节的大包
scheduler.Enqueue(0, new byte[800]);
scheduler.Enqueue(0, new byte[800]);

// 通道 1 塞入 4 个 200 字节的小包
scheduler.Enqueue(1, new byte[200]);
scheduler.Enqueue(1, new byte[200]);
scheduler.Enqueue(1, new byte[200]);
scheduler.Enqueue(1, new byte[200]);

Console.WriteLine("=== Deficit Round Robin (DRR) Schedule Simulation ===");
int step = 1;
while (scheduler.HasPendingPackets)
{
    var item = scheduler.DequeueNext();
    if (item != null)
    {
        string chName = item.Value.ChannelId == 0 ? "Image(800B)" : "Chat(200B)";
        Console.WriteLine($"Step {step++}: Emitted packet from [{chName}]");
    }
}
