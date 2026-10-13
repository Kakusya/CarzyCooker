using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET
{
    [EntitySystemOf(typeof(ProcessInnerSender))]
    [FriendOf(typeof(ProcessInnerSender))]
    public static partial class ProcessInnerSenderSystem
    {
        [StaticField]
        private static int s_RpcId;

        [EntitySystem]
        private static void Destroy(this ProcessInnerSender self)
        {
            self.Stop();
        }

        [EntitySystem]
        private static void Awake(this ProcessInnerSender self)
        {
            Fiber fiber = self.Fiber();
            self.IsStopped = false;
            self.requestCallback.Clear();
            self.list.Clear();
            self.Queue = MessageQueue.Instance;
            self.QueueFiberId = fiber.Id;
            self.Queue.AddQueue(fiber.Id);
        }

        public static void Stop(this ProcessInnerSender self)
        {
            if (self.IsStopped)
            {
                return;
            }
            self.IsStopped = true;
            var pending = new List<KeyValuePair<int, MessageSenderStruct>>(self.requestCallback);
            self.requestCallback.Clear();
            MessageQueue queue = self.Queue;
            self.Queue = null;
            queue?.RemoveQueue(self.QueueFiberId);
            // 清空名单、撤下队列之后才通知；通知可能立即 Stop、Dispose 或重建 sender。
            foreach (var entry in pending)
            {
                MessageSenderStruct waiter = entry.Value;
                if (waiter.NeedException)
                {
                    waiter.SetException(new RpcException(ErrorCore.ERR_Cancel, "ProcessInnerSender stopped"));
                }
                else
                {
                    IResponse response = (IResponse)Activator.CreateInstance(waiter.ResponseType);
                    response.RpcId = entry.Key;
                    response.Error = ErrorCore.ERR_Cancel;
                    response.Message = "ProcessInnerSender stopped";
                    waiter.SetResult(response);
                }
            }
        }

        private static void ThrowIfStopped(this ProcessInnerSender self)
        {
            if (self.IsDisposed || self.IsStopped)
            {
                throw new ObjectDisposedException(nameof(ProcessInnerSender));
            }
        }

        [EntitySystem]
        private static void Update(this ProcessInnerSender self)
        {
            if (self.IsStopped)
            {
                return;
            }
            long instanceId = self.InstanceId;
            Fiber fiber = self.Fiber();
            self.Queue.Fetch(fiber.Id, 1000, self.list);
            MessageInfo[] batch = self.list.ToArray();
            self.list.Clear();

            foreach (MessageInfo actorMessageInfo in batch)
            {
                if (self.InstanceId != instanceId || self.IsStopped)
                {
                    if (ObjectPool.Instance != null)
                    {
                        actorMessageInfo.MessageObject.Dispose();
                    }
                    continue;
                }
                self.HandleMessage(fiber, actorMessageInfo);
            }
        }

        private static void HandleMessage(this ProcessInnerSender self, Fiber fiber, in MessageInfo messageInfo)
        {
            if (messageInfo.MessageObject is IResponse response)
            {
                self.HandleIActorResponse(response);
                return;
            }

            ActorId actorId = messageInfo.ActorId;
            MessageObject message = messageInfo.MessageObject;

            MailBoxComponent mailBoxComponent = self.Fiber().Mailboxes.Get(actorId.InstanceId);
            if (mailBoxComponent == null)
            {
                Log.Warning($"actor not found mailbox, from: {actorId} current: {fiber.Address} {message}");
                if (message is IRequest request)
                {
                    IResponse resp = MessageHelper.CreateResponse(request.GetType(), request.RpcId, ErrorCore.ERR_NotFoundActor);
                    self.Reply(actorId.Address, resp);
                }
                message.Dispose();
                return;
            }
            mailBoxComponent.Add(actorId.Address, message);
        }

        private static void HandleIActorResponse(this ProcessInnerSender self, IResponse response)
        {
            if (!self.requestCallback.Remove(response.RpcId, out MessageSenderStruct actorMessageSender))
            {
                ((MessageObject)response).Dispose();
                return;
            }
            Run(actorMessageSender, response);
        }
        
        private static void Run(MessageSenderStruct self, IResponse response)
        {
            if (response.Error == ErrorCore.ERR_MessageTimeout)
            {
                var exception = new RpcException(response.Error, $"Rpc error: request, 注意Actor消息超时，请注意查看是否死锁或者没有reply: actorId: {self.ActorId} {self.RequestType.FullName}, response: {response}");
                ((MessageObject)response).Dispose();
                self.SetException(exception);
                return;
            }

            if (self.NeedException && ErrorCore.IsRpcNeedThrowException(response.Error))
            {
                var exception = new RpcException(response.Error, $"Rpc error: actorId: {self.ActorId} request: {self.RequestType.FullName}, response: {response}");
                ((MessageObject)response).Dispose();
                self.SetException(exception);
                return;
            }

            self.SetResult(response);
        }
        
        public static void Reply(this ProcessInnerSender self, Address fromAddress, IResponse message)
        {
            if (!self.SendInner(new ActorId(fromAddress, 0), (MessageObject)message))
            {
                ((MessageObject)message).Dispose();
            }
        }

        public static void Send(this ProcessInnerSender self, ActorId actorId, IMessage message)
        {
            if (!self.SendInner(actorId, (MessageObject)message))
            {
                ((MessageObject)message).Dispose();
            }
        }

        private static bool SendInner(this ProcessInnerSender self, ActorId actorId, MessageObject message)
        {
            self.ThrowIfStopped();
            Fiber fiber = self.Fiber();
            
            // 如果发向同一个进程，则扔到消息队列中
            if (actorId.Process != fiber.Process)
            {
                throw new Exception($"actor inner process diff: {actorId.Process} {fiber.Process}");
            }
            
            return self.Queue.Send(fiber.Address, actorId, message);
        }

        private static int GetRpcId(this ProcessInnerSender self)
        {
            while (true)
            {
                int current = Volatile.Read(ref s_RpcId);
                if (current == int.MaxValue)
                {
                    throw new InvalidOperationException("Local RpcId exhausted in this managed domain");
                }
                if (Interlocked.CompareExchange(ref s_RpcId, current + 1, current) == current)
                {
                    return current + 1;
                }
            }
        }

        public static async UniTask<IResponse> Call(
                this ProcessInnerSender self,
                ActorId actorId,
                IRequest request,
                bool needException = true
        )
        {
            int rpcId;
            Fiber fiber;
            Type requestType = request.GetType();
            Type responseType;
            bool sent;
            try
            {
                self.ThrowIfStopped();
                rpcId = self.GetRpcId();
                request.RpcId = rpcId;
                if (actorId == default)
                {
                    throw new Exception($"actor id is 0: {request}");
                }
                fiber = self.Fiber();
                if (fiber.Process != actorId.Process)
                {
                    throw new Exception($"actor inner process diff: {actorId.Process} {fiber.Process}");
                }
                responseType = OpcodeType.Instance.GetResponseType(requestType);
                sent = self.SendInner(actorId, (MessageObject)request);
            }
            catch
            {
                // Call 已接管输入；未入队的拒绝不能交给不存在的 handler 收尾。
                ((MessageObject)request).Dispose();
                throw;
            }
            EntityRef<ProcessInnerSender> senderRef = self;
            IResponse response;
            if (!sent)  // 纤程不存在
            {
                ((MessageObject)request).Dispose();
                response = MessageHelper.CreateResponse(requestType, rpcId, ErrorCore.ERR_NotFoundActor);
                return response;
            }
            
            MessageSenderStruct messageSenderStruct = new(actorId, requestType, needException, responseType);
            self.requestCallback.Add(rpcId, messageSenderStruct);
            
            async UniTask Timeout()
            {
                await fiber.Root.GetComponent<TimerComponent>().WaitAsync(ProcessInnerSender.TIMEOUT_TIME);

                ProcessInnerSender sender = senderRef;
                if (sender == null || sender.IsStopped || !sender.requestCallback.Remove(rpcId, out MessageSenderStruct action))
                {
                    return;
                }
                
                if (needException)
                {
                    action.SetException(new Exception($"actor sender timeout: {requestType.FullName}"));
                }
                else
                {
                    IResponse response = (IResponse)Activator.CreateInstance(action.ResponseType);
                    response.RpcId = rpcId;
                    response.Error = ErrorCore.ERR_Timeout;
                    action.SetResult(response);
                }
            }
            
            Timeout().Forget();
            
            long beginTime = TimeInfo.Instance.ServerFrameTime();

            response = await messageSenderStruct.Wait();

            ProcessInnerSender currentSender = senderRef;
            if (currentSender == null || currentSender.IsStopped || TimeInfo.Instance == null)
            {
                return response;
            }
            
            long endTime = TimeInfo.Instance.ServerFrameTime();

            long costTime = endTime - beginTime;
            if (costTime > 200)
            {
                Log.Warning($"actor rpc time > 200: {costTime} {requestType.FullName}");
            }
            
            return response;
        }
    }
}
