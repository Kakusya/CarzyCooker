using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Game;
using System.Net;

namespace ET
{
    [Message(0)]
    [ResponseType(nameof(ShutdownTestResponse))]
    public sealed class ShutdownTestRequest : MessageObject, IRequest
    {
        public int RpcId { get; set; }
        public int Error;
        public bool Hold;
    }

    [Message(0)]
    public sealed class ShutdownTestResponse : MessageObject, IResponse
    {
        public int RpcId { get; set; }
        public int Error { get; set; }
        public string Message { get; set; }
    }

    [Message(0)]
    [ResponseType(nameof(OwnershipTestResponse))]
    public sealed class OwnershipTestRequest : MessageObject, IRequest
    {
        public int RpcId { get; set; }
        public bool Hold;
        public int DisposeCount;
        public override void Dispose() => ++DisposeCount;
    }

    [Message(0)]
    public sealed class OwnershipTestResponse : MessageObject, IResponse
    {
        public int RpcId { get; set; }
        public int Error { get; set; }
        public string Message { get; set; }
        public int DisposeCount;
        public override void Dispose() => ++DisposeCount;
    }

    [Message(0)]
    public sealed class OwnershipTestNotice : MessageObject
    {
        public int DisposeCount;
        public override void Dispose() => ++DisposeCount;
    }

    [MessageHandler((SceneType)(1L << 60))]
    public sealed class OwnershipTestHandler : ProductActorRequestHandler<Scene, OwnershipTestRequest, OwnershipTestResponse>
    {
        public static int Calls;
        public static OwnershipTestResponse LastResponse;
        protected override async UniTask Run(Scene scene, OwnershipTestRequest request, OwnershipTestResponse response)
        {
            ++Calls;
            LastResponse = response;
            if (request.Hold)
            {
                await ShutdownTestHandler.Gate.Task;
            }
        }
    }

    [MessageHandler((SceneType)(1L << 60))]
    public sealed class ShutdownTestHandler : MessageHandler<Scene, ShutdownTestRequest, ShutdownTestResponse>
    {
        public static UniTaskCompletionSource Gate;

        protected override async UniTask Run(Scene actor, ShutdownTestRequest request, ShutdownTestResponse response)
        {
            response.Error = request.Error;
            if (request.Hold)
            {
                await Gate.Task;
            }
        }
    }

    public sealed class ShutdownTestClock : ITimeNow
    {
        public long Milliseconds = 1700000000000;
        public long GetUtcNowTicks() => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks + Milliseconds * 10000;
        public void Update() { }
    }

    public sealed class ShutdownTestLog : ILog
    {
        public void Trace(string message) { }
        public void Warning(string message) { }
        public void Info(string message) { }
        public void Debug(string message) { }
        public void Error(string message) => UnityEngine.Debug.LogError(message);
        public void Error(Exception e) => UnityEngine.Debug.LogException(e);
    }

    public sealed class OwnedTestService : AService
    {
        public int Removed;
        public OwnedTestService() => ServiceType = ServiceType.Outer;
        public override void Update() { }
        public override void Remove(long id, int error = 0) => Removed++;
        public override bool IsDisposed() => false;
        public override void Create(long id, IPEndPoint endpoint) { }
        public override void Send(long id, MemoryBuffer buffer) => Recycle(buffer);
    }

    // PlayMode UTF，独占测试 World；不在用户已有 Play 上运行。
    public sealed class LocalMessageShutdownTests
    {
        private World world;
        private Fiber fiber;
        private ProcessInnerSender sender;
        private Scene actor;
        private ShutdownTestClock clock;
        private GameObject viewRoot;
        private AsyncRuntime ownedRuntime;
        private static readonly FieldInfo CurrentFiber = typeof(Fiber).GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static);

        [SetUp]
        public void Setup()
        {
            if (FiberManager.Instance != null || CodeTypes.Instance != null || MessageQueue.Instance != null || EntitySystemSingleton.Instance != null)
            {
                throw new InvalidOperationException("Refusing to replace an active ET World");
            }
            // EditorLogHelper 的 Options/Logger 不代表活跃业务；这里只有其最小日志 World。
            World.Instance.Dispose();
            world = World.Instance;
            world.AddSingleton(new Options { Process = 1 });
            world.AddSingleton<Logger, ILog>(new ShutdownTestLog());
            clock = new ShutdownTestClock();
            world.AddSingleton<TimeInfo, ITimeNow>(clock);
            world.AddSingleton<IdGenerater>();
            world.AddSingleton<ObjectPool>();
            world.AddSingleton<CodeTypes, Assembly[]>(new[] { typeof(Entity).Assembly, typeof(MessageHandler<,,>).Assembly, typeof(Entry).Assembly, typeof(AsyncOwnerComponent).Assembly, GetType().Assembly });
            world.AddSingleton<EntitySystemSingleton>();
            world.AddSingleton<OpcodeType>();
            world.AddSingleton<EventSystem>();
            world.AddSingleton<MessageDispatcher>();
            world.AddSingleton<MessageQueue>();
            viewRoot = new GameObject("ShutdownTestView");
#if UNITY_ET_VIEW && UNITY_EDITOR
            Entity.SetRootView(viewRoot.transform);
#endif
            // 仅构造测试 Fiber 使用非公开构造器；消息、Timer、Mailbox、Dispose 全走真实 API。
            fiber = (Fiber)Activator.CreateInstance(typeof(Fiber), BindingFlags.NonPublic | BindingFlags.Instance, null,
                new object[] { 90001, 0, (SceneType)(1L << 60), "ShutdownTest" }, null);
            CurrentFiber.SetValue(null, fiber);
            fiber.Root.AddComponent<TimerComponent>();
            fiber.Root.AddComponent<CoroutineLockComponent>();
            sender = fiber.Root.AddComponent<ProcessInnerSender>();
            actor = new Scene(fiber, 90002, IdGenerater.Instance.GenerateInstanceId(), (SceneType)(1L << 60), "Receiver");
            fiber.Root.AddChild(actor);
            actor.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
            ShutdownTestHandler.Gate = new UniTaskCompletionSource();
            OwnershipTestHandler.Calls = 0;
            OwnershipTestHandler.LastResponse = null;
        }

        [TearDown]
        public void Cleanup()
        {
            if (world == null)
            {
                return;
            }
            try
            {
                ownedRuntime?.Stop();
                ownedRuntime = null;
                ShutdownTestHandler.Gate.TrySetResult();
                if (TimeInfo.Instance != null && sender != null && !sender.IsStopped)
                {
                    Pump();
                }
                sender?.Stop();
                fiber?.Dispose();
            }
            finally
            {
                CurrentFiber.SetValue(null, null);
                world.Dispose();
                world = null;
                UnityEngine.Object.DestroyImmediate(viewRoot);
            }
        }

        private static ShutdownTestRequest Request(int error = 0, bool hold = false)
        {
            var request = (ShutdownTestRequest)Activator.CreateInstance(typeof(ShutdownTestRequest));
            request.Error = error;
            request.Hold = hold;
            return request;
        }

        private static ShutdownTestResponse Response(int rpcId)
        {
            var response = (ShutdownTestResponse)Activator.CreateInstance(typeof(ShutdownTestResponse));
            response.RpcId = rpcId;
            return response;
        }

        private void Pump(int frames = 3)
        {
            for (int i = 0; i < frames; i++)
            {
                TimeInfo.Instance.Update();
                fiber.EntitySystem.Update();
                fiber.EntitySystem.LateUpdate();
                fiber.ThreadSynchronizationContext.Update();
            }
        }

        private IResponse Finish(UniTask<IResponse> task)
        {
            Pump();
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            return task.GetAwaiter().GetResult();
        }

        [Test]
        public void LocalMessageOwnershipCallValidationFailureReleasesBeforeQueueHandoff()
        {
            var invalid = (OwnershipTestRequest)Activator.CreateInstance(typeof(OwnershipTestRequest));
            Assert.Throws<Exception>(() => sender.Call(default, invalid, false).GetAwaiter().GetResult());
            Assert.That(invalid.DisposeCount, Is.EqualTo(1));
            sender.Stop();
            var stopped = (OwnershipTestRequest)Activator.CreateInstance(typeof(OwnershipTestRequest));
            Assert.Throws<ObjectDisposedException>(() => sender.Call(actor.GetActorId(), stopped, false).GetAwaiter().GetResult());
            Assert.That(stopped.DisposeCount, Is.EqualTo(1));
            Assert.That(OwnershipTestHandler.Calls, Is.Zero);
        }

        [Test]
        public void LocalMessageOwnershipMissingQueueReleasesRequestNoticeAndLateResponseOnce()
        {
            ActorId absent = new(fiber.Process, fiber.Id + 1, 90003);
            var request = (OwnershipTestRequest)Activator.CreateInstance(typeof(OwnershipTestRequest));
            using var response = (OwnershipTestResponse)sender.Call(absent, request, false).GetAwaiter().GetResult();
            Assert.That(response.Error, Is.EqualTo(ErrorCore.ERR_NotFoundActor));
            Assert.That(request.DisposeCount, Is.EqualTo(1));
            var notice = (OwnershipTestNotice)Activator.CreateInstance(typeof(OwnershipTestNotice));
            sender.Send(absent, notice);
            Assert.That(notice.DisposeCount, Is.EqualTo(1));
            var late = (OwnershipTestResponse)Activator.CreateInstance(typeof(OwnershipTestResponse));
            sender.Reply(absent.Address, late);
            Assert.That(late.DisposeCount, Is.EqualTo(1));
            Assert.That(OwnershipTestHandler.Calls, Is.Zero);
        }

        [Test]
        public void LocalMessageOwnershipMissingMailboxReleasesUndeliveredInputOnce()
        {
            ActorId missing = actor.GetActorId();
            actor.RemoveComponent<MailBoxComponent>();
            var request = (OwnershipTestRequest)Activator.CreateInstance(typeof(OwnershipTestRequest));
            using var response = (OwnershipTestResponse)Finish(sender.Call(missing, request, false));
            Assert.That(response.Error, Is.EqualTo(ErrorCore.ERR_NotFoundActor));
            Assert.That(request.DisposeCount, Is.EqualTo(1));
            var notice = (OwnershipTestNotice)Activator.CreateInstance(typeof(OwnershipTestNotice));
            sender.Send(missing, notice);
            Pump();
            Assert.That(notice.DisposeCount, Is.EqualTo(1));
            Assert.That(OwnershipTestHandler.Calls, Is.Zero);
        }

        [Test]
        public void LocalMessageOwnershipStopDrainsQueuedInputAndCancelsWithoutFuturePump()
        {
            var request = (OwnershipTestRequest)Activator.CreateInstance(typeof(OwnershipTestRequest));
            var notice = (OwnershipTestNotice)Activator.CreateInstance(typeof(OwnershipTestNotice));
            UniTask<IResponse> pending = sender.Call(actor.GetActorId(), request, false);
            sender.Send(actor.GetActorId(), notice);
            sender.Stop();
            using var cancelled = (OwnershipTestResponse)pending.GetAwaiter().GetResult();
            Assert.That(cancelled.Error, Is.EqualTo(ErrorCore.ERR_Cancel));
            Assert.That(request.DisposeCount, Is.EqualTo(1));
            Assert.That(notice.DisposeCount, Is.EqualTo(1));
            Assert.That(OwnershipTestHandler.Calls, Is.Zero);
            sender.Stop();
            Assert.That(request.DisposeCount, Is.EqualTo(1));
            Assert.That(notice.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void LocalMessageOwnershipOrderedWaitRejectsRetiredMailboxWithoutTouchingReplacement()
        {
            var first = (OwnershipTestRequest)Activator.CreateInstance(typeof(OwnershipTestRequest));
            first.Hold = true;
            var second = (OwnershipTestRequest)Activator.CreateInstance(typeof(OwnershipTestRequest));
            UniTask<IResponse> active = sender.Call(actor.GetActorId(), first, false);
            Pump();
            UniTask<IResponse> waiting = sender.Call(actor.GetActorId(), second, false);
            Pump();
            long id = actor.Id;
            actor.Dispose();
            var replacement = new Scene(fiber, id, IdGenerater.Instance.GenerateInstanceId(), (SceneType)(1L << 60), "Replacement");
            fiber.Root.AddChild(replacement);
            replacement.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
            ShutdownTestHandler.Gate.TrySetResult();
            using var finished = (OwnershipTestResponse)Finish(active);
            using var rejected = (OwnershipTestResponse)Finish(waiting);
            Assert.That(rejected.Error, Is.EqualTo(ErrorCore.ERR_NotFoundActor));
            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(second.DisposeCount, Is.EqualTo(1));
            Assert.That(OwnershipTestHandler.Calls, Is.EqualTo(1));
        }

        [Test]
        public void LocalMessageOwnershipStopDoesNotReleaseHandlerInputBeforeItsLateResponse()
        {
            var request = (OwnershipTestRequest)Activator.CreateInstance(typeof(OwnershipTestRequest));
            request.Hold = true;
            UniTask<IResponse> pending = sender.Call(actor.GetActorId(), request, false);
            Pump();
            OwnershipTestResponse physical = OwnershipTestHandler.LastResponse;
            sender.Stop();
            using var cancelled = (OwnershipTestResponse)pending.GetAwaiter().GetResult();
            Assert.That(cancelled.Error, Is.EqualTo(ErrorCore.ERR_Cancel));
            Assert.That(request.DisposeCount, Is.Zero, "Already handed to the handler");
            ShutdownTestHandler.Gate.TrySetResult();
            Assert.That(request.DisposeCount, Is.EqualTo(1));
            Assert.That(physical.DisposeCount, Is.EqualTo(1));
        }

        [Test] public void OwnedEntity_DisposeAndSameIdReuseInvalidateOriginalInstance()
        {
            ownedRuntime = AsyncRuntime.Begin();
            var child = new Scene(fiber, 90003, IdGenerater.Instance.GenerateInstanceId(), (SceneType)(1L << 60), "OwnedChild");
            actor.AddChild(child);
            var owner = child.GetAsyncOwner();
            long original = child.InstanceId;
            var pending = new UniTaskCompletionSource<int>();
            AsyncContext context = default;
            var task = owner.RunAsync(c => { context = c; return pending.Task; });
            child.Dispose();
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
            var next = new Scene(fiber, 90003, IdGenerater.Instance.GenerateInstanceId(), (SceneType)(1L << 60), "NextChild");
            actor.AddChild(next);
            Assert.That(next.InstanceId, Is.Not.EqualTo(original));
            pending.TrySetResult(8);
            Assert.That(context.Apply(() => Assert.Fail("stale ET instance")), Is.False);
            Assert.That(next.GetAsyncOwner().AsyncScope.IsActive, Is.True);
            Assert.That(actor.IsDisposed, Is.False);
        }

        [Test] public void OwnedSession_TwoOwnersCancelOneAndDisposeUnconsumedResponse()
        {
            ownedRuntime = AsyncRuntime.Begin();
            world.AddSingleton<LogMsg>();
            var service = new OwnedTestService();
            Session session = actor.AddChildWithId<Session, AService>(90004, service);
            var first = new AsyncScope(ownedRuntime);
            var second = new AsyncScope(ownedRuntime);
            C2G_Ping request1 = C2G_Ping.Create(true);
            C2G_Ping request2 = C2G_Ping.Create(true);
            var one = first.RunAsync(context => context.CallSessionValueAsync(session, request1, r => ((G2C_Ping)r).Time));
            var two = second.RunAsync(context => context.CallSessionValueAsync(session, request2, r => ((G2C_Ping)r).Time));
            int rpc2 = request2.RpcId;
            Assert.That(session.requestCallbacks.Count, Is.EqualTo(2));
            first.Stop();
            Assert.That(one.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(session.requestCallbacks.Count, Is.EqualTo(1));
            Assert.That(service.Removed, Is.Zero);
            var response = G2C_Ping.Create(true);
            response.RpcId = rpc2;
            response.Time = 123;
            session.OnResponse(response);
            Assert.That(two.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(session.requestCallbacks.Count, Is.Zero);
            Assert.That(response.Time, Is.Zero);
            Assert.That(two.GetAwaiter().GetResult(), Is.EqualTo(123));
        }

        [Test] public void OwnedLocal_CancelOnlyBusinessWaitAndLetOriginalReplyRetire()
        {
            ownedRuntime = AsyncRuntime.Begin();
            var first = new AsyncScope(ownedRuntime);
            var second = new AsyncScope(ownedRuntime);
            var one = first.RunAsync(context => context.CallLocalValueAsync(sender, actor.GetActorId(), Request(hold: true), r => r.Error));
            Pump();
            first.Stop();
            Assert.That(one.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(sender.IsStopped, Is.False);
            var two = second.RunAsync(context => context.CallLocalValueAsync(sender, actor.GetActorId(), Request(), r => r.Error));
            ShutdownTestHandler.Gate.TrySetResult();
            Pump();
            Assert.That(two.GetAwaiter().GetResult(), Is.Zero);
            Assert.That(one.Status, Is.EqualTo(UniTaskStatus.Canceled));
        }

        [Test] public void OwnedETDelay_StopBeforeTimerUpdateCancelsAndRemovesWait()
        {
            ownedRuntime = AsyncRuntime.Begin();
            var owner = new AsyncScope(ownedRuntime);
            var task = owner.RunAsync(context => context.DelayAsync(fiber.Root.GetComponent<TimerComponent>(), 100000));
            owner.Stop();
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(owner.PendingCount, Is.Zero);
            Assert.That(fiber.Root.GetComponent<TimerComponent>().timerActions.Count, Is.Zero);
        }

        [Test]
        public void NormalReplyBusinessFailureAndReplyBeforeStopKeepResults()
        {
            IResponse success = Finish(sender.Call(actor.GetActorId(), Request()));
            Assert.That(success.Error, Is.Zero);
            IResponse business = Finish(sender.Call(actor.GetActorId(), Request(ErrorCore.ERR_MyErrorCode), false));
            Assert.That(business.Error, Is.EqualTo(ErrorCore.ERR_MyErrorCode));
            var pending = sender.Call(actor.GetActorId(), Request());
            Pump();
            sender.Stop();
            Assert.That(pending.GetAwaiter().GetResult().Error, Is.Zero);
        }

        [Test]
        public void StopDirectDisposeAndNoTimerSettleBothModes()
        {
            var exception = sender.Call(actor.GetActorId(), Request(hold: true));
            var result = sender.Call(actor.GetActorId(), Request(hold: true), false);
            fiber.Root.RemoveComponent<TimerComponent>();
            OpcodeType.Instance.Dispose();
            TimeInfo.Instance.Dispose();
            sender.Dispose();
            sender.Stop();
            Assert.That(Assert.Throws<RpcException>(() => exception.GetAwaiter().GetResult()).Error, Is.EqualTo(ErrorCore.ERR_Cancel));
            Assert.That(result.GetAwaiter().GetResult().Error, Is.EqualTo(ErrorCore.ERR_Cancel));
            Assert.Throws<ObjectDisposedException>(() => sender.Call(actor.GetActorId(), Request()).GetAwaiter().GetResult());
            Assert.Throws<ObjectDisposedException>(() => sender.Send(actor.GetActorId(), Request()));
        }

        [Test]
        public void RealTimerKeepsBothTimeoutModes()
        {
            var throws = sender.Call(actor.GetActorId(), Request(hold: true));
            var returns = sender.Call(actor.GetActorId(), Request(hold: true), false);
            clock.Milliseconds += ProcessInnerSender.TIMEOUT_TIME + 1;
            Pump();
            Assert.Throws<Exception>(() => throws.GetAwaiter().GetResult());
            Assert.That(returns.GetAwaiter().GetResult().Error, Is.EqualTo(ErrorCore.ERR_Timeout));
        }

        [Test]
        public void OldReplyAndRealOldTimerCannotCompleteReusedSender()
        {
            fiber.Root.RemoveComponent<ProcessInnerSender>();
            sender = fiber.Root.AddComponent<ProcessInnerSender>(true);
            var oldRequest = Request(hold: true);
            var old = sender.Call(actor.GetActorId(), oldRequest, false);
            Pump(1);
            int oldRpc = oldRequest.RpcId;
            ProcessInnerSender previous = sender;
            sender.Stop();
            Assert.That(old.GetAwaiter().GetResult().Error, Is.EqualTo(ErrorCore.ERR_Cancel));
            fiber.Root.RemoveComponent<ProcessInnerSender>();
            clock.Milliseconds += ProcessInnerSender.TIMEOUT_TIME / 2;
            TimeInfo.Instance.Update();
            sender = fiber.Root.AddComponent<ProcessInnerSender>(true);
            Assert.That(ReferenceEquals(previous, sender), Is.True, "实际复用池化 sender");
            var newRequest = Request(hold: true);
            var current = sender.Call(actor.GetActorId(), newRequest, false);
            Assert.That(newRequest.RpcId, Is.Not.EqualTo(oldRpc));
            // 旧计时实际恢复，新请求的计时尚未到；旧答复实际进入同一个 Fiber 的新队列。
            clock.Milliseconds += ProcessInnerSender.TIMEOUT_TIME / 2 + 1;
            MessageQueue.Instance.Send(fiber.Address, new ActorId(fiber.Address, 0), Response(oldRpc));
            Pump(1);
            Assert.That(current.Status, Is.EqualTo(UniTaskStatus.Pending));
            ShutdownTestHandler.Gate.TrySetResult();
            Assert.That(Finish(current).Error, Is.Zero);
            clock.Milliseconds += ProcessInnerSender.TIMEOUT_TIME;
            Pump();
            Assert.That(Finish(sender.Call(actor.GetActorId(), Request())).Error, Is.Zero);
        }

        [Test]
        public void StopInsideResponseBatchSettlesOnceAndDropsRemainingOldReply()
        {
            var first = sender.Call(actor.GetActorId(), Request(), false);
            var second = sender.Call(actor.GetActorId(), Request(), false);
            async UniTask StopAfterReply()
            {
                Assert.That((await first).Error, Is.Zero);
                sender.Stop();
            }
            var continuation = StopAfterReply();
            Pump();
            continuation.GetAwaiter().GetResult();
            Assert.That(second.GetAwaiter().GetResult().Error, Is.EqualTo(ErrorCore.ERR_Cancel));
            Assert.That(sender.requestCallback.Count, Is.Zero);
            sender.Stop();
        }

        [Test]
        public void StopWithoutOpcodePoolTimerOrQueueSingletonStillCompletes()
        {
            var response = sender.Call(actor.GetActorId(), Request(), false);
            var exception = sender.Call(actor.GetActorId(), Request());
            fiber.Root.RemoveComponent<TimerComponent>();
            OpcodeType.Instance.Dispose();
            TimeInfo.Instance.Dispose();
            MessageQueue.Instance.Dispose();
            ObjectPool.Instance.Dispose();
            MessageQueue nextQueue = world.AddSingleton<MessageQueue>();
            nextQueue.AddQueue(fiber.Id);
            try
            {
                sender.Stop();
                Assert.That(response.GetAwaiter().GetResult().Error, Is.EqualTo(ErrorCore.ERR_Cancel));
                Assert.That(Assert.Throws<RpcException>(() => exception.GetAwaiter().GetResult()).Error, Is.EqualTo(ErrorCore.ERR_Cancel));
                Assert.That(nextQueue.Send(fiber.Address, new ActorId(fiber.Address, 0), Response(42)), Is.True,
                    "旧发送器不能删除替换单例的新队列");
            }
            finally
            {
                // Entity 的既有 Dispose 本身要求 ObjectPool；仅恢复测试收尾所需单例。
                world.AddSingleton<ObjectPool>();
            }
        }

        [Test]
        public void WholeFiberDisposeSettlesBothWaitersWithoutTimerUpdates()
        {
            var result = sender.Call(actor.GetActorId(), Request(), false);
            var exception = sender.Call(actor.GetActorId(), Request());
            fiber.Dispose();
            Assert.That(result.GetAwaiter().GetResult().Error, Is.EqualTo(ErrorCore.ERR_Cancel));
            Assert.That(Assert.Throws<RpcException>(() => exception.GetAwaiter().GetResult()).Error, Is.EqualTo(ErrorCore.ERR_Cancel));
            fiber.Dispose();
        }

        [Test]
        public void ReentrantCompletionDoesNotDeleteNewQueueAndStopsBatch()
        {
            var first = sender.Call(actor.GetActorId(), Request(), false);
            var second = sender.Call(actor.GetActorId(), Request(), false);
            ProcessInnerSender old = sender;
            bool notified = false;
            async UniTask Continue()
            {
                Assert.That((await first).Error, Is.EqualTo(ErrorCore.ERR_Cancel));
                old.Stop();
                fiber.Root.RemoveComponent<ProcessInnerSender>();
                sender = fiber.Root.AddComponent<ProcessInnerSender>();
                notified = true;
            }
            var continuation = Continue();
            old.Stop();
            continuation.GetAwaiter().GetResult();
            Assert.That(notified, Is.True);
            Assert.That(second.GetAwaiter().GetResult().Error, Is.EqualTo(ErrorCore.ERR_Cancel));
            old.Dispose();
            Assert.That(Finish(sender.Call(actor.GetActorId(), Request())).Error, Is.Zero);
        }

        [Test]
        public void RpcIdExhaustionRejectsWithoutWrappingOrPendingRequest()
        {
            var field = typeof(ProcessInnerSenderSystem).GetField("s_RpcId", BindingFlags.NonPublic | BindingFlags.Static);
            int saved = (int)field.GetValue(null);
            try
            {
                field.SetValue(null, int.MaxValue);
                Assert.Throws<InvalidOperationException>(() => sender.Call(actor.GetActorId(), Request()).GetAwaiter().GetResult());
                Assert.That(sender.requestCallback.Count, Is.Zero);
            }
            finally
            {
                field.SetValue(null, saved);
            }
        }
    }
}
