using System.Text;
using QosDemo;

var scheduler = new EgressPacketScheduler();

// 场景：先排入了 3 个普通的大数据分片/后台日志
scheduler.Enqueue(PriorityLevel.Normal, Encoding.UTF8.GetBytes("BULK_CHUNK_1"));
scheduler.Enqueue(PriorityLevel.Normal, Encoding.UTF8.GetBytes("BULK_CHUNK_2"));
scheduler.Enqueue(PriorityLevel.Normal, Encoding.UTF8.GetBytes("BULK_CHUNK_3"));

// 此时突发紧急事件：机械臂急停指令！
Console.WriteLine("Incoming Event: Emergency STOP issued!\n");
scheduler.Enqueue(PriorityLevel.High, Encoding.UTF8.GetBytes("EMERGENCY_STOP"));

// 网卡循环调用 DequeueNext() 取包发送
int sendStep = 1;
while (scheduler.TotalPending > 0)
{
    byte[]? pkt = scheduler.DequeueNext();
    if (pkt != null)
    {
        string text = Encoding.UTF8.GetString(pkt);
        Console.WriteLine($"Step {sendStep++}: Transmitting -> {text}");
    }
}
