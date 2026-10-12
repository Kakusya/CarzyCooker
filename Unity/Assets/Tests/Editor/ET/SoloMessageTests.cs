using System;
using Cysharp.Threading.Tasks;
using Game;
using NUnit.Framework;

namespace ET
{
    [Message(0)]
    [ResponseType(nameof(SoloHeldResponse))]
    public sealed class SoloHeldRequest : MessageObject, IRequest
    {
        public int RpcId { get; set; }
        public bool Hold;
        public static int Disposed;
        public override void Dispose() => ++Disposed;
    }

    [Message(0)]
    public sealed class SoloHeldResponse : MessageObject, IResponse
    {
        public int RpcId { get; set; }
        public int Error { get; set; }
        public string Message { get; set; }
        public static int Disposed;
        public override void Dispose() => ++Disposed;
    }

    [MessageHandler(SceneType.ProductHost)]
    public sealed class SoloHeldHandler : ProductActorRequestHandler<HostPeer, SoloHeldRequest, SoloHeldResponse>
    {
        public static UniTaskCompletionSource Gate;
        public static int Entered;

        protected override async UniTask Run(HostPeer peer, SoloHeldRequest request, SoloHeldResponse response)
        {
            ++Entered;
            if (request.Hold) await Gate.Task;
        }
    }

    public sealed partial class ProductSessionTests
    {
        private static SoloHeldRequest SoloRequest(bool hold = false)
            => ProductMessages.Create<SoloHeldRequest>(request => request.Hold = hold);

        [Test]
        public void SoloMessageCanceledOwnerKeepsPhysicalPeerUntilReply()
        {
            StartSolo();
            AsyncRuntime owned = AsyncRuntime.Begin();
            var first = new AsyncScope(owned);
            SoloHeldHandler.Gate = new UniTaskCompletionSource();
            SoloHeldHandler.Entered = SoloHeldRequest.Disposed = SoloHeldResponse.Disposed = 0;
            ClientSessionComponent client = host.Client;
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            int reads = 0;
            try
            {
                var pending = first.RunAsync(context => client.CallProductValueAsync(context, SoloRequest(true), response => ++reads));
                PumpMessages(5);
                Assert.That(SoloHeldHandler.Entered, Is.EqualTo(1));
                Assert.That(peer.InFlight, Is.EqualTo(1));
                first.Stop();
                Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Canceled));
                Assert.Throws<OperationCanceledException>(() => pending.GetAwaiter().GetResult());
                Assert.That(peer.InFlight, Is.EqualTo(1), "Canceling display waiting must not release the physical Call");
                peer.CloseEntry();
                PumpMessages(5);
                Assert.That(peer.CloseFinished, Is.True);
                Assert.That(peer.IsDisposed, Is.False, "The old MailBox must survive its held request");

                ClientSessionComponent other = AddLocalClient();
                other.JoinProduct().Forget();
                PumpMessages(8);
                var second = new AsyncScope(owned);
                var normal = second.RunAsync(context => other.CallProductValueAsync(context, SoloRequest(), response => response.Error));
                PumpMessages(8);
                Assert.That(normal.GetAwaiter().GetResult(), Is.Zero);
                Assert.That(fibers[0].Root.GetComponent<ProcessInnerSender>().IsStopped, Is.False);
                Assert.That(peer.IsDisposed, Is.False);

                SoloHeldHandler.Gate.TrySetResult();
                PumpMessages(8);
                Assert.That(peer.IsDisposed, Is.True);
                Assert.That(reads, Is.Zero, "Canceled owner cannot read a late response");
                Assert.That(SoloHeldRequest.Disposed, Is.EqualTo(2));
                Assert.That(SoloHeldResponse.Disposed, Is.EqualTo(2));
            }
            finally
            {
                SoloHeldHandler.Gate.TrySetResult();
                PumpMessages(8);
                owned.Stop();
            }
        }

        [Test]
        public void SoloMessageTimeoutReleasesEntryAndDisposesLateResponseOnce()
        {
            StartSolo();
            AsyncRuntime owned = AsyncRuntime.Begin();
            var scope = new AsyncScope(owned);
            ClientSessionComponent client = host.Client;
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            SoloHeldHandler.Gate = new UniTaskCompletionSource();
            SoloHeldRequest.Disposed = SoloHeldResponse.Disposed = 0;
            try
            {
                var pending = scope.RunAsync(context => client.CallProductValueAsync(context, SoloRequest(true), response => response.Error));
                PumpMessages(5);
                Assert.That(peer.InFlight, Is.EqualTo(1));
                clock.Milliseconds += ProcessInnerSender.TIMEOUT_TIME + 1;
                PumpMessages(8);
                Assert.That(pending.GetAwaiter().GetResult(), Is.EqualTo(ErrorCore.ERR_Timeout));
                Assert.That(peer.InFlight, Is.Zero);
                Assert.That(SoloHeldResponse.Disposed, Is.EqualTo(1));
                SoloHeldHandler.Gate.TrySetResult();
                PumpMessages(8);
                Assert.That(peer.InFlight, Is.Zero);
                Assert.That(SoloHeldRequest.Disposed, Is.EqualTo(1));
                Assert.That(SoloHeldResponse.Disposed, Is.EqualTo(2), "Timeout value and late physical response each retire once");
            }
            finally
            {
                SoloHeldHandler.Gate.TrySetResult();
                PumpMessages(8);
                owned.Stop();
            }
        }

        [Test]
        public void SoloMessageWholeStopCancelsHeldCallAndRetiresItsLateInput()
        {
            StartSolo();
            AsyncRuntime owned = AsyncRuntime.Begin();
            var scope = new AsyncScope(owned);
            ClientSessionComponent client = host.Client;
            SoloHeldHandler.Gate = new UniTaskCompletionSource();
            SoloHeldRequest.Disposed = SoloHeldResponse.Disposed = 0;
            try
            {
                var pending = scope.RunAsync(context => client.CallProductValueAsync(context, SoloRequest(true), response => response.Error));
                PumpMessages(5);
                host.StopProduct();
                Assert.That(pending.Status, Is.EqualTo(UniTaskStatus.Canceled));
                Assert.Throws<OperationCanceledException>(() => pending.GetAwaiter().GetResult());
                Assert.That(SoloHeldResponse.Disposed, Is.EqualTo(1));
                SoloHeldHandler.Gate.TrySetResult();
                PumpMessages(8);
                Assert.That(SoloHeldRequest.Disposed, Is.EqualTo(1));
                Assert.That(SoloHeldResponse.Disposed, Is.EqualTo(2));
            }
            finally
            {
                SoloHeldHandler.Gate.TrySetResult();
                PumpMessages(8);
                owned.Stop();
            }
        }

        [Test]
        public void SoloMessageStoppedSenderAndInactiveOwnerReleaseUnsentInput()
        {
            StartSolo();
            AsyncRuntime owned = AsyncRuntime.Begin();
            owned.ErrorObserver = error => Assert.That(error, Is.TypeOf<ObjectDisposedException>());
            var scope = new AsyncScope(owned);
            ClientSessionComponent client = host.Client;
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            SoloHeldRequest.Disposed = SoloHeldResponse.Disposed = 0;
            try
            {
                fibers[0].Root.GetComponent<ProcessInnerSender>().Stop();
                var pending = scope.RunAsync(context => client.CallProductValueAsync(context, SoloRequest(), response => response.Error));
                Assert.Throws<ObjectDisposedException>(() => pending.GetAwaiter().GetResult());
                Assert.That(peer.InFlight, Is.Zero);
                Assert.That(SoloHeldRequest.Disposed, Is.EqualTo(1));
                Assert.That(SoloHeldResponse.Disposed, Is.Zero);
                scope.Stop();
                var canceled = client.CallProductValueAsync(new AsyncContext(scope), SoloRequest(), response => response.Error);
                Assert.Throws<OperationCanceledException>(() => canceled.GetAwaiter().GetResult());
                Assert.That(peer.InFlight, Is.Zero);
                Assert.That(SoloHeldRequest.Disposed, Is.EqualTo(2));
            }
            finally { owned.Stop(); }
        }

        [Test]
        public void SoloMessagePumpAndBusinessTimeAreIndependent()
        {
            StartSolo();
            ClientSessionComponent client = host.Client;
            GridActor actor = Grid.Actor(client.MemberId);
            var projection = client.GetComponent<GridClientComponent>();
            long tick = Grid.Tick;
            var command = projection.CreateGridCommand(GridAction.Move, actor.Cell);
            long id = command.CommandId;
            var pending = client.CallProduct(command);
            PumpMessages(8);
            using var reply = (GridCommandResponse)pending.GetAwaiter().GetResult();
            Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(Grid.Tick, Is.EqualTo(tick));
            Assert.That(actor.LastResultCommandId, Is.Not.EqualTo(id));
            AdvanceTicks(1);
            PumpMessages(5);
            Assert.That(Grid.Tick, Is.EqualTo(tick + 1));
            Assert.That(actor.LastResultCommandId, Is.EqualTo(id));
            Assert.That(actor.LastResultStatus, Is.EqualTo(GridResultStatus.Executed));
            Assert.That(projection.LastCommandId, Is.EqualTo(id));
            Assert.That(projection.LastStatus, Is.EqualTo(GridResultStatus.Executed));
            Assert.That(projection.Tick, Is.EqualTo(Grid.Tick));
        }

        private GridCommandResponse SoloReply(ClientSessionComponent client, GridCommandRequest request)
        {
            var task = client.CallProduct(request);
            PumpMessages(8);
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            return (GridCommandResponse)task.GetAwaiter().GetResult();
        }

        [Test]
        public void SoloMessageSharedSequenceLatestFactsAndContextRejections()
        {
            StartSolo();
            ClientSessionComponent client = host.Client;
            var projection = client.GetComponent<GridClientComponent>();
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            GridCell cell = Grid.Actor(client.MemberId).Cell;
            var first = projection.CreateGridCommand(GridAction.Move, cell);
            long firstId = first.CommandId;
            using (var receipt = SoloReply(client, first)) Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            using (var replay = SoloReply(client, GridRequest(client, firstId, GridAction.Move, cell)))
                Assert.That(replay.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(Grid.Incoming.Count, Is.EqualTo(1));
            AdvanceTicks(1); PumpMessages(5);
            long version = Grid.Version;
            using (var replay = SoloReply(client, GridRequest(client, firstId, GridAction.Move, cell)))
                Assert.That(replay.Status, Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(Grid.Version, Is.EqualTo(version));
            Assert.That(Grid.Results.FindAll(value => value.CommandId == firstId && value.Status == GridResultStatus.Executed).Count, Is.EqualTo(1));

            ProductCommandContext otherFamilyContext = client.CreateProductCommandContext(projection.WorldId, projection.WorldGeneration);
            var next = projection.CreateGridCommand(GridAction.Move, cell);
            long nextId = next.CommandId;
            Assert.That(otherFamilyContext.CommandId, Is.EqualTo(firstId + 1));
            Assert.That(nextId, Is.EqualTo(otherFamilyContext.CommandId + 1));
            using (var receipt = SoloReply(client, next)) Assert.That(receipt.Error, Is.Zero);
            using (var old = SoloReply(client, GridRequest(client, firstId, GridAction.Move, cell)))
                Assert.That(old.Message, Is.EqualTo("ExpiredCommand"));
            using (var conflict = SoloReply(client, GridRequest(client, nextId, GridAction.Move, new GridCell(2, 8))))
                Assert.That(conflict.Message, Is.EqualTo("CommandConflict"));

            var wrongWorld = GridRequest(client, nextId + 1, GridAction.Move, cell); wrongWorld.WorldGeneration++;
            using (var response = SoloReply(client, wrongWorld)) Assert.That(response.Message, Is.EqualTo("WrongWorld"));
            var wrongMatch = GridRequest(client, nextId + 1, GridAction.Move, cell); wrongMatch.MatchId = "other-match";
            using (var response = SoloReply(client, wrongMatch)) Assert.That(response.Message, Is.EqualTo("WrongMatch"));
            var wrongBinding = GridRequest(client, nextId + 1, GridAction.Move, cell); wrongBinding.BindingGeneration++;
            using (var response = SoloReply(client, wrongBinding)) Assert.That(response.Message, Is.EqualTo("EntryClosed"));
            var wrongLevel = GridRequest(client, nextId + 1, GridAction.Move, cell); wrongLevel.LevelId = 9; wrongLevel.LevelGeneration = 7;
            using (var response = SoloReply(client, wrongLevel)) Assert.That(response.Message, Is.EqualTo("WrongLevel"));
            var oldProtocol = GridRequest(client, nextId + 1, GridAction.Move, cell); oldProtocol.ProtocolVersion--;
            using (var response = SoloReply(client, oldProtocol)) Assert.That(response.Message, Is.EqualTo("IncompatibleProtocol"));
            Assert.That(peer.LastCommandContext.CommandId, Is.EqualTo(nextId));
            Assert.That(peer.LastCommandKind, Is.EqualTo(ProductCommandKind.Grid));
        }

        [Test]
        public void SoloMessageOneItemTransferAndIndependentProjection()
        {
            StartSolo();
            ClientSessionComponent client = host.Client;
            GridStation source = PrepStation("备料台");
            long item = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, item);
            var projection = client.GetComponent<GridClientComponent>();
            long command = projection.LastCommandId;
            int count = Grid.Items.Count;
            using (var replay = SoloReply(client, GridRequest(client, command, GridAction.TakeCounter, target: source.Id, expected: item)))
                Assert.That(replay.Status, Is.EqualTo((int)GridResultStatus.Executed));
            AdvanceTicks(3); PumpMessages(5);
            Assert.That(Grid.Items.Count, Is.EqualTo(count));
            Assert.That(source.ItemId, Is.Zero);
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(item));
            Assert.That(projection.Actors[client.MemberId].HandItemId, Is.EqualTo(item));
            Assert.That(projection.Items[item].Location, Is.EqualTo(GridItemLocation.Hand));

            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            GridWorldSnapshot snapshot = GridBroadcast.CreateSnapshot(Grid, peer);
            snapshot.Version = projection.Version + 1;
            projection.ApplyGridSnapshot(snapshot);
            snapshot.IsFromPool = true;
            snapshot.Dispose();
            Assert.That(projection.Items[item].Id, Is.EqualTo(item));
            Assert.That(projection.Actors[client.MemberId].HandItemId, Is.EqualTo(item));
            Assert.That(projection.Stations[source.Id].ItemId, Is.Zero);
            Assert.That(projection.Items[item].Contents, Is.Empty);
        }
    }
}
