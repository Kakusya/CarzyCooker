using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class ProcessInnerSender: Entity, IAwake, IDestroy, IUpdate
    {
        public const long TIMEOUT_TIME = 40 * 1000;
        
        public bool IsStopped;

        // 捕获创建入口的队列；旧 World 退出后不查找新 World 的队列。
        public MessageQueue Queue;
        public int QueueFiberId;

        public readonly Dictionary<int, MessageSenderStruct> requestCallback = new();
        
        public readonly List<MessageInfo> list = new();
    }
}
