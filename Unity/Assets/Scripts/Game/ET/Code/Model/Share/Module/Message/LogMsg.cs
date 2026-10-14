using System.Collections.Generic;

namespace ET
{
    public class LogMsg: Singleton<LogMsg>, ISingletonAwake
    {
        private readonly HashSet<ushort> ignore = new()
        {
            Message_ET_Client.C2G_Ping, 
            Message_ET_Client.G2C_Ping, 
            Message_ET_Client.C2G_Benchmark, 
            Message_ET_Client.G2C_Benchmark,
        };

        public void Awake()
        {
#if !DOTNET
            // 这些消息含本轮恢复能力凭证，不能进入通用 ToString 调试日志。
            this.ignore.Add(Message_ET_Client.ProductJoinResponse);
            this.ignore.Add(Message_ET_Client.ProductControlRequest);
            this.ignore.Add(Message_ET_Client.ProductControlResponse);
            this.ignore.Add(Message_ET_Client.ProductResumeRequest);
            this.ignore.Add(Message_ET_Client.ProductResumeResponse);
            // 完整世界快照高频发送，调试展开会淹没普通动作和错误诊断。
            this.ignore.Add(Message_ET_Client.GridWorldSnapshot);
#endif
        }

        public void Debug(Fiber fiber, object msg)
        {
            ushort opcode = OpcodeType.Instance.GetOpcode(msg.GetType());
            if (this.ignore.Contains(opcode))
            {
                return;
            }
            fiber.Log.Debug(msg.ToString());
        }
    }
}
