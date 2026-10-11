using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET
{
    [EnableClass]
    public sealed class RpcInfo
    {
        public Type RequestType { get; }
        
        private readonly UniTaskCompletionSource<IResponse> tcs;
        private CancellationTokenRegistration registration;
        private Action cancelAction;
        private bool finished;

        public RpcInfo(Type requestType)
        {
            this.RequestType = requestType;
            
            this.tcs = new UniTaskCompletionSource<IResponse>();
        }

        public void SetResult(IResponse response)
        {
            if (finished) { (response as MessageObject)?.Dispose(); return; }
            finished = true;
            registration.Dispose();
            cancelAction = null;
            this.tcs.TrySetResult(response);
        }

        public void SetException(Exception exception)
        {
            if (finished) return;
            finished = true;
            registration.Dispose();
            cancelAction = null;
            this.tcs.TrySetException(exception);
        }

        public UniTask<IResponse> Wait()
        {
            return this.tcs.Task;
        }

        public void AddOnCancelAction(Action cancelAction)
        {
            this.cancelAction += cancelAction;
        }

        public void AttachCancellation(CancellationToken token)
        {
            if (finished) return;
            registration = token.Register(() => cancelAction?.Invoke());
            if (finished) registration.Dispose();
        }
    }
    
    [EntitySystemOf(typeof(Session))]
    [FriendOf(typeof(Session))]
    public static partial class SessionSystem
    {
        [EntitySystem]
        private static void Awake(this Session self, AService aService)
        {
            self.AService = aService;
            long timeNow = TimeInfo.Instance.ClientNow();
            self.LastRecvTime = timeNow;
            self.LastSendTime = timeNow;

            self.requestCallbacks.Clear();
            
            Log.Info($"session create: zone: {self.Zone()} id: {self.Id} {timeNow} ");
        }
        
        [EntitySystem]
        private static void Destroy(this Session self)
        {
            self.AService.Remove(self.Id, self.Error);
            
            RpcInfo[] pending = self.requestCallbacks.Values.ToArray();
            self.requestCallbacks.Clear();
            foreach (RpcInfo responseCallback in pending)
            {
                responseCallback.SetException(new RpcException(self.Error, $"session dispose: {self.Id} {self.RemoteAddress}"));
            }

            Log.Info($"session dispose: {self.RemoteAddress} id: {self.Id} ErrorCode: {self.Error}, please see ErrorCode.cs! {TimeInfo.Instance.ClientNow()}");
            
        }
        
        public static void OnResponse(this Session self, IResponse response)
        {
            if (!self.requestCallbacks.Remove(response.RpcId, out RpcInfo action))
            {
                (response as MessageObject)?.Dispose();
                return;
            }
            action.SetResult(response);
        }
        
        public static UniTask<IResponse> Call(this Session self, IRequest request, CancellationToken token)
        {
            if (self.IsDisposed) throw new ObjectDisposedException(nameof(Session));
            if (token.IsCancellationRequested) return UniTask.FromCanceled<IResponse>(token);
            EntityRef<Session> selfRef = self;
            Type responseType = OpcodeType.Instance.GetResponseType(request.GetType());
            int rpcId = ++self.RpcId;
            RpcInfo rpcInfo = new(request.GetType());
            self.requestCallbacks[rpcId] = rpcInfo;
            request.RpcId = rpcId;

            self.Send(request);
            
            void CancelAction()
            {
                Session current = selfRef;
                if (current == null || !current.requestCallbacks.Remove(rpcId, out RpcInfo action))
                {
                    return;
                }

                IResponse response = (IResponse) Activator.CreateInstance(responseType);
                response.RpcId = rpcId;
                response.Error = ErrorCore.ERR_Cancel;
                action.SetResult(response);
            }

            rpcInfo.AddOnCancelAction(CancelAction);
            rpcInfo.AttachCancellation(token);
            return rpcInfo.Wait();
        }

        public static async UniTask<IResponse> Call(this Session self, IRequest request, int time = 0)
        {
            if (self.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Session));
            }
            EntityRef<Session> selfRef = self;
            int rpcId = ++self.RpcId;
            RpcInfo rpcInfo = new(request.GetType());
            self.requestCallbacks[rpcId] = rpcInfo;
            request.RpcId = rpcId;
            self.Send(request);

            if (time > 0)
            {
                async UniTaskVoid Timeout()
                {
                    await self.Root().GetComponent<TimerComponent>().WaitAsync(time);
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }
                    if (!self.requestCallbacks.TryGetValue(rpcId, out RpcInfo action))
                    {
                        return;
                    }

                    if (!self.requestCallbacks.Remove(rpcId))
                    {
                        return;
                    }
                    
                    action.SetException(new Exception($"session call timeout: {action.RequestType.FullName} {time}"));
                }
                
                Timeout().Forget();
            }

            return await rpcInfo.Wait();
        }

        public static void Send(this Session self, IMessage message)
        {
            self.Send(default, message);
        }
        
        public static void Send(this Session self, ActorId actorId, IMessage message)
        {
            self.LastSendTime = TimeInfo.Instance.ClientNow();
            LogMsg.Instance.Debug(self.Fiber(), message);

            (ushort opcode, MemoryBuffer memoryBuffer) = MessageSerializeHelper.ToMemoryBuffer(self.AService, actorId, message);
            self.AService.Send(self.Id, memoryBuffer);
        }
    }

    [ChildOf]
    public sealed class Session: Entity, IAwake<AService>, IDestroy
    {
        public AService AService { get; set; }
        
        public int RpcId
        {
            get;
            set;
        }

        public readonly Dictionary<int, RpcInfo> requestCallbacks = new();
        
        public long LastRecvTime
        {
            get;
            set;
        }

        public long LastSendTime
        {
            get;
            set;
        }

        public int Error
        {
            get;
            set;
        }

        public IPEndPoint RemoteAddress
        {
            get;
            set;
        }
    }
}
