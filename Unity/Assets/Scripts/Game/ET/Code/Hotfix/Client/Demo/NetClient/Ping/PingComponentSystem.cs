using System;
using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [EntitySystemOf(typeof(PingComponent))]
    public static partial class PingComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PingComponent self)
        {
            self.GetAsyncOwner().Run(context => self.PingAsync(context));
        }
        
        [EntitySystem]
        private static void Destroy(this PingComponent self)
        {
            self.Ping = default;
        }
        
        private static async UniTask PingAsync(this PingComponent self, AsyncContext context)
        {
            Session session = self.GetParent<Session>();
            long instanceId = self.InstanceId;
            Fiber fiber = self.Fiber();
            
            while (true)
            {
                try
                {
                    await context.DelayAsync(fiber.Root.GetComponent<TimerComponent>(), 2000);
                    if (self.InstanceId != instanceId)
                    {
                        return;
                    }
                    long time1 = TimeInfo.Instance.ClientNow();
                    C2G_Ping c2GPing = C2G_Ping.Create(true);
                    long serverTime = await context.CallSessionValueAsync(session, c2GPing, response =>
                    {
                        if (response.Error != 0) throw new RpcException(response.Error, response.Message);
                        return ((G2C_Ping)response).Time;
                    });

                    if (self.InstanceId != instanceId)
                    {
                        return;
                    }

                    long time2 = TimeInfo.Instance.ClientNow();
                    context.Apply(() =>
                    {
                        self.Ping = time2 - time1;
                        TimeInfo.Instance.ServerMinusClientTime = serverTime + (time2 - time1) / 2 - time2;
                    });
                }
                catch (OperationCanceledException) { return; }
                catch (RpcException e)
                {
                    // session断开导致ping rpc报错，记录一下即可，不需要打成error
                    Log.Debug($"session disconnect, ping error: {self.Id} {e.Error}");
                    return;
                }
                catch (Exception e)
                {
                    Log.Debug($"ping error: \n{e}");
                }
            }
        }
    }
}
