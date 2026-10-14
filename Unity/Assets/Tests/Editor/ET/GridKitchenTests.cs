using System.Linq;
using Cysharp.Threading.Tasks;
using Game;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(GridActor))]
    [FriendOf(typeof(GridStation))]
    [FriendOf(typeof(GridItem))]
    [FriendOf(typeof(GridClientComponent))]
    public sealed partial class ProductSessionTests
    {
        private GridKitchenComponent Grid => ((Match)((HostSessionComponent)host.Authority).Match).ProductKitchen();

        private void GridSubmit(ClientSessionComponent client, long command, GridAction action, GridCell cell = default, long target = 0, long expected = 0)
        {
            using var receipt = (GridCommandResponse)FinishMessages(client.CallProduct(GridRequest(client, command, action, cell, target, expected)), "grid two-mailbox receipt");
            Assert.That(receipt.Error, Is.Zero, receipt.Message);
        }

        private GridSubmitRequest GridRetiredBindingRequest(ClientSessionComponent client, GridAction action, GridCell cell,
            long target, long input = 0, long expectedTarget = 0, long process = 0)
        {
            var view = client.GetComponent<GridClientComponent>();
            HostSessionComponent authority = host.Authority;
            HostPeer peer = authority.Children.Values.OfType<HostPeer>().Single(value => value.MemberId == client.MemberId && value.Generation == client.BindingGeneration);
            return ProductMessages.Create<GridSubmitRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = view.MatchId; value.WorldId = view.WorldId; value.WorldGeneration = view.WorldGeneration;
                value.LevelId = view.LevelId; value.LevelGeneration = view.LevelGeneration;
                value.PeerId = peer.Id; value.PeerInstanceId = peer.InstanceId; value.PeerGeneration = peer.Generation; value.BindingGeneration = peer.Generation;
                value.CommandId = 999; value.Action = (int)action; value.X = cell.X; value.Y = cell.Y;
                value.TargetId = target; value.ExpectedItemId = input; value.ExpectedTargetItemId = expectedTarget; value.ExpectedProcessId = process;
            });
        }

        private void GridRejectRetiredBinding(GridSubmitRequest request)
        {
            HostSessionComponent authority = host.Authority; ProcessInnerSender sender = host.Sender;
            using var response = (GridSubmitResponse)FinishMessages(sender.Call(authority.GetActorId(), request, false), "retired binding through authority mailbox");
            Assert.That(response.Error, Is.Not.Zero); Assert.That(response.Message, Is.EqualTo("EntryClosed"));
        }

        private ClientSessionComponent GridJoin()
        {
            ClientSessionComponent client = AddLocalClient();
            client.JoinProduct().Forget();
            Pump(8);
            return client;
        }

        [Test]
        public void GridCoreMoveCompletesOnArrivalAndCancelsReplacedMove()
        {
            StartHost();
            ClientSessionComponent client = host.Client;
            GridActor actor = Grid.Actor(client.MemberId);
            GridClientComponent projection = client.GetComponent<GridClientComponent>();
            GridCell destination = new(4, 10);
            GridCall(client, 1, GridAction.Move, destination);
            Pump(4);
            Assert.That(actor.Cell, Is.Not.EqualTo(destination));
            Assert.That(actor.LastResultStatus, Is.EqualTo(GridResultStatus.PendingApproach));
            Assert.That(projection.LastStatus, Is.EqualTo(GridResultStatus.PendingApproach));
            Assert.That(Grid.Results.Any(value => value.CommandId == 1 && value.Status == GridResultStatus.Executed), Is.False);

            GridCell next = actor.NextCell;
            GridCell replacement = new(1, 9);
            GridCall(client, 2, GridAction.Move, replacement);
            Pump(17);
            Assert.That(actor.Cell, Is.EqualTo(next), "Retarget must finish the current step");
            Assert.That(Grid.Results.Any(value => value.CommandId == 1 && value.Status == GridResultStatus.Cancelled), Is.True);
            Assert.That(actor.LastResultStatus, Is.EqualTo(GridResultStatus.PendingApproach));
            Pump(100);
            Assert.That(actor.Cell, Is.EqualTo(replacement));
            Assert.That(projection.LastStatus, Is.EqualTo(GridResultStatus.Executed));
            Assert.That(Grid.Results.Count(value => value.CommandId == 2 && value.Status == GridResultStatus.Executed), Is.EqualTo(1));
            Assert.That(Grid.Results.Any(value => value.CommandId == 1 && value.Status == GridResultStatus.Executed), Is.False);
            Assert.That(ProductOutcomeTestObserver.Received.Count(value => value.Value.Context.BindingGeneration == client.BindingGeneration && value.Value.Context.CommandId == 1 && value.Value.Status == GridResultStatus.Cancelled), Is.EqualTo(1));
            Assert.That(ProductOutcomeTestObserver.Received.Count(value => value.Value.Context.BindingGeneration == client.BindingGeneration && value.Value.Context.CommandId == 2 && value.Value.Status == GridResultStatus.Executed), Is.EqualTo(1));
        }

        [Test]
        [Category("PerFramePath")]
        public void GridCoreRetargetFinishesStepAndCancelsOldInteraction()
        {
            StartHost();
            ClientSessionComponent client = host.Client;
            GridActor actor = Grid.Actor(client.MemberId);
            GridStation bowl = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Name == "碗台");
            var path = Grid.FindGridPath(new GridCell(2, 8), new GridCell(4, 8));
            Assert.That(path.All(cell => Grid.IsWalkable(cell)), Is.True);
            for (int i = 1; i < path.Count; ++i) Assert.That(GridKitchenSystem.GridDistance(path[i - 1], path[i]), Is.EqualTo(1));
            Assert.That(path.Count, Is.GreaterThan(3), "BFS must route around the occupied counter");
            GridSubmit(client, 1, GridAction.TakeCounter, target: bowl.Id, expected: bowl.ItemId);
            Pump(4);
            Assert.That(actor.Motion, Is.EqualTo(GridMotion.Transit));
            GridCell committed = actor.Cell, next = actor.NextCell;
            GridSubmit(client, 2, GridAction.Move, new GridCell(1, 9));
            Pump();
            Assert.That(actor.Cell, Is.EqualTo(committed), "Retarget cannot teleport or cut the current step");
            Pump(17);
            Assert.That(actor.Cell, Is.EqualTo(next));
            Assert.That(actor.Goal, Is.EqualTo(new GridCell(1, 9)));
            Pump(100);
            Assert.That(actor.Cell, Is.EqualTo(new GridCell(1, 9)));
            Assert.That(actor.HandItemId, Is.Zero);
            Assert.That(Grid.Results.Any(value => value.CommandId == 1 && value.Status == GridResultStatus.Cancelled), Is.True);
            Assert.That(((Match)Grid.Match).Children.Values.OfType<Participant>().Count(), Is.EqualTo(1), "Grid data must not enter the Participant child list");
        }

        [Test]
        [Category("PerFramePath")]
        public void GridCoreTwoStandThirdWaitsAndCanCrossFullCell()
        {
            StartHost();
            ClientSessionComponent first = host.Client, second = GridJoin(), third = GridJoin();
            GridCell destination = new(2, 9);
            GridSubmit(first, 1, GridAction.Move, destination);
            GridSubmit(second, 1, GridAction.Move, destination);
            GridSubmit(third, 1, GridAction.Move, destination);
            Pump(100);
            Assert.That(Grid.Actor(first.MemberId).Cell, Is.EqualTo(destination));
            Assert.That(Grid.Actor(second.MemberId).Cell, Is.EqualTo(destination));
            Assert.That(Grid.Actor(third.MemberId).Cell, Is.Not.EqualTo(destination));
            Assert.That(Grid.Actor(third.MemberId).Motion, Is.EqualTo(GridMotion.Waiting));
            Assert.That(Grid.Actor(third.MemberId).LastResultStatus, Is.EqualTo(GridResultStatus.PendingApproach), "Waiting for a standing slot is not arrival");
            GridSubmit(third, 2, GridAction.Move, new GridCell(2, 8));
            Pump(100);
            Assert.That(Grid.Actor(third.MemberId).Cell, Is.EqualTo(new GridCell(2, 8)), "Players are not pathfinding obstacles");
            GridSubmit(third, 3, GridAction.Move, destination);
            Pump(70);
            GridSubmit(first, 2, GridAction.Move, new GridCell(1, 9));
            Pump(100);
            Assert.That(Grid.Actor(third.MemberId).Cell, Is.EqualTo(destination), "A waiting destination resumes when a slot is released");
        }

        [Test]
        [Category("PerFrameTraversal")]
        public void GridCoreItemRaceDropIdentityAndSameCellPickup()
        {
            StartHost();
            ClientSessionComponent first = host.Client, second = GridJoin();
            GridStation bowl = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Name == "碗台");
            long itemId = bowl.ItemId;
            GridSubmit(first, 1, GridAction.TakeCounter, target: bowl.Id, expected: itemId);
            GridSubmit(second, 1, GridAction.TakeCounter, target: bowl.Id, expected: itemId);
            Pump(100);
            Assert.That(Grid.Actor(first.MemberId).HandItemId, Is.EqualTo(itemId));
            Assert.That(Grid.Actor(second.MemberId).HandItemId, Is.Zero);
            Assert.That(Grid.Actor(second.MemberId).LastResultStatus, Is.EqualTo(GridResultStatus.Rejected));
            Assert.That(bowl.ItemId, Is.Zero);
            GridItem item = Grid.Item(itemId);
            Assert.That(item.Contents, Is.Empty);
            GridCell dropCell = Grid.Actor(first.MemberId).Cell;
            GridSubmit(first, 2, GridAction.Move, new GridCell(4, 9));
            Pump(4);
            Assert.That(Grid.Actor(first.MemberId).Motion, Is.EqualTo(GridMotion.Transit));
            HostPeer peer = ((LocalMessageComponent)first.Local).Peer;
            peer.CloseEntry(); peer.CloseEntry();
            Pump(12);
            Assert.That(Grid.Actor(first.MemberId), Is.Null);
            Assert.That(item.Location, Is.EqualTo(GridItemLocation.Ground));
            Assert.That(item.Cell, Is.EqualTo(dropCell), "Disconnect uses the last committed cell, not the interpolated position");
            GridSubmit(second, 2, GridAction.Move, new GridCell(dropCell.X - 1, dropCell.Y));
            Pump(100);
            GridSubmit(second, 3, GridAction.PickupDrop, target: itemId);
            Pump(8);
            Assert.That(Grid.Actor(second.MemberId).LastResultReason, Is.EqualTo("PickupRequiresSameCell"));
            Assert.That(item.Location, Is.EqualTo(GridItemLocation.Ground));
            GridSubmit(second, 4, GridAction.Move, dropCell);
            Pump(70);
            GridSubmit(second, 5, GridAction.PickupDrop, target: itemId);
            Pump(8);
            Assert.That(Grid.Actor(second.MemberId).HandItemId, Is.EqualTo(itemId));
            Assert.That(item.Contents, Is.Empty);
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).Id == itemId), Is.EqualTo(1));
        }

        private GridCommandRequest GridRequest(ClientSessionComponent client, long command, GridAction action, GridCell cell = default, long target = 0, long expected = 0)
        {
            GridClientComponent grid = client.GetComponent<GridClientComponent>();
            Assert.That(grid.Confirmed, Is.True);
            return ProductMessages.Create<GridCommandRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = client.MatchId;
                value.WorldId = grid.WorldId; value.WorldGeneration = grid.WorldGeneration; value.BindingGeneration = client.BindingGeneration;
                value.CommandId = command; value.Action = (int)action; value.X = cell.X; value.Y = cell.Y; value.TargetId = target; value.ExpectedItemId = expected;
                value.LevelId = grid.LevelId; value.LevelGeneration = grid.LevelGeneration;
            });
        }

        private void GridCall(ClientSessionComponent client, long command, GridAction action, GridCell cell = default, long target = 0, long expected = 0, bool replay = false)
        {
            using var receipt = (GridCommandResponse)Finish(client.CallProduct(GridRequest(client, command, action, cell, target, expected)), 300);
            Assert.That(receipt.Error, Is.Zero, receipt.Message);
            if (replay) Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted).Or.EqualTo((int)GridResultStatus.PendingApproach).Or.EqualTo((int)GridResultStatus.Executed));
            else Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted));
        }

        [Test]
        public void GridWireSoloAndLocalUseSameTickAndRejectStaleIdentity()
        {
            var config = RuntimeInstanceConfig.Create("grid-solo", "test", "solo");
            Assert.Throws<System.ArgumentException>(() => RuntimeInstanceConfig.Create("grid-solo", "test", "solo", "127.0.0.1:0"));
            RuntimeInstanceContext.Prepare(config);
            Fiber fiber = CreateFiber();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeProduct(); Pump(12);
            Assert.That(host.Stage, Is.EqualTo(ProductStage.SoloReady));
            Assert.That(((Scene)host.HostScene).GetComponent<NetComponent>(), Is.Null);
            Assert.That(host.ListenEndpoint, Is.Null);
            ClientSessionComponent client = host.Client;
            GridCell destination = new(2, 9);
            GridCall(client, 1, GridAction.Move, destination);
            GridCall(client, 1, GridAction.Move, destination, replay: true);
            using (var rejected = (GridCommandResponse)Finish(client.CallProduct(GridRequest(client, 1, GridAction.Move, new GridCell(2, 8)))))
                Assert.That(rejected.Message, Is.EqualTo("CommandConflict"));
            var stale = GridRequest(client, 2, GridAction.Move, new GridCell(2, 8)); stale.BindingGeneration++;
            using (var rejected = (GridCommandResponse)Finish(client.CallProduct(stale))) Assert.That(rejected.Message, Is.EqualTo("EntryClosed"));
            var oldProtocol = GridRequest(client, 2, GridAction.Move, new GridCell(2, 8)); oldProtocol.ProtocolVersion = 1;
            using (var rejected = (GridCommandResponse)Finish(client.CallProduct(oldProtocol))) Assert.That(rejected.Message, Is.EqualTo("IncompatibleProtocol"));
            var wrongWorld = GridRequest(client, 2, GridAction.Move, new GridCell(2, 8)); wrongWorld.WorldGeneration++;
            using (var rejected = (GridCommandResponse)Finish(client.CallProduct(wrongWorld))) Assert.That(rejected.Message, Is.EqualTo("WrongWorld"));
            Pump(100);
            Assert.That(Grid.Actor(client.MemberId).Cell, Is.EqualTo(destination));
            Assert.That(Grid.Results.Count(value => value.CommandId == 1 && value.Status == GridResultStatus.Executed), Is.EqualTo(1));
            host.StopProduct(); Pump(8);
            Assert.That(GridSafeWorld(host), Is.Null);
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.True);
        }

        private static GridKitchenComponent GridSafeWorld(ProductRuntimeComponent runtime)
        {
            HostSessionComponent authority = runtime.Authority;
            Match match = authority?.Match ?? default;
            return match.ProductKitchen();
        }

        [Test]
        public void GridWireKcpSharesItemsDropsAndContinuesAfterMemberExit()
        {
            StartHost();
            ClientSessionComponent local = host.Client, remote = CreateRemoteClient();
            remote.JoinProduct().Forget(); Pump(100);
            GridStation bowl = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Name == "碗台");
            GridStation food = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Name == "备料台");
            long bowlId = bowl.ItemId, foodId = food.ItemId;
            GridCall(remote, 1, GridAction.TakeCounter, target: bowl.Id, expected: bowlId);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.Zero, "Receipt returns without waiting for approach");
            GridCall(local, 1, GridAction.TakeCounter, target: food.Id, expected: foodId);
            Pump(140);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(bowlId));
            Assert.That(Grid.Actor(local.MemberId).HandItemId, Is.EqualTo(foodId));
            GridCell drop = new(2, 9);
            GridCall(local, 2, GridAction.Move, drop); GridCall(remote, 2, GridAction.Move, drop); Pump(120);
            GridClientComponent remoteProjection = remote.GetComponent<GridClientComponent>(), localProjection = local.GetComponent<GridClientComponent>();
            Assert.That(remoteProjection.WorldId, Is.EqualTo(localProjection.WorldId));
            Assert.That(remoteProjection.Version, Is.EqualTo(localProjection.Version));
            Assert.That(remoteProjection.Items[bowlId].Contents, Is.Empty);
            ClientSessionComponent remaining = GridJoin();
            Session remoteSession = remote.Session; remoteSession.Dispose();
            Pump(900); // Host 的现有 Session idle 检测处理真实传输断开。
            HostPeer localPeer = ((LocalMessageComponent)local.Local).Peer;
            localPeer.CloseEntry(); localPeer.CloseEntry(); Pump(20);
            Assert.That(Grid.Actor(remote.MemberId), Is.Null);
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).Location == GridItemLocation.Ground && ((GridItem)value).Cell == drop), Is.EqualTo(2));
            GridCall(remaining, 1, GridAction.Move, drop); Pump(120);
            GridCall(remaining, 2, GridAction.PickupDrop, target: bowlId); Pump(12);
            Assert.That(Grid.Actor(remaining.MemberId).HandItemId, Is.EqualTo(bowlId));
            GridCall(remaining, 3, GridAction.PickupDrop, target: foodId); Pump(12);
            Assert.That(Grid.Actor(remaining.MemberId).LastResultReason, Is.EqualTo("HandFull"));
            Assert.That(Grid.Item(foodId).Location, Is.EqualTo(GridItemLocation.Ground));
            GridStation empty = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Name == "交接台");
            GridCall(remaining, 4, GridAction.PlaceCounter, target: empty.Id, expected: bowlId); Pump(100);
            Assert.That(empty.ItemId, Is.EqualTo(bowlId));
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }

        [Test]
        public void GridWireProjectionCopiesDtoAndRejectsOldWorldAndInternalWire()
        {
            StartHost();
            ClientSessionComponent local = host.Client;
            HostPeer peer = ((LocalMessageComponent)local.Local).Peer;
            GridClientComponent projection = local.GetComponent<GridClientComponent>();
            long version = projection.Version;
            using (var old = GridBroadcast.CreateSnapshot(Grid, peer))
            {
                old.Version = version - 1; old.Actors.Clear(); projection.ApplyGridSnapshot(old);
                Assert.That(projection.Actors.ContainsKey(local.MemberId), Is.True);
                old.Version = version + 1; old.WorldGeneration++; projection.ApplyGridSnapshot(old);
                Assert.That(projection.Version, Is.EqualTo(version));
            }
            using (var copied = GridBroadcast.CreateSnapshot(Grid, peer))
            {
                copied.Version = version + 1; projection.ApplyGridSnapshot(copied);
                copied.Actors[0].X = 100; copied.Items[0].Name = "mutated";
                Assert.That(projection.Actors[local.MemberId].Cell.X, Is.Not.EqualTo(100));
                Assert.That(projection.Items.Values.Any(item => item.Name == "mutated"), Is.False);
            }
            ClientSessionComponent oldVersion = CreateRemoteClient();
            using (var response = (ProductJoinResponse)Finish(oldVersion.CallProduct(ProductMessages.Create<ProductJoinRequest>(value => value.ProtocolVersion = 1)), 300))
                Assert.That(response.Error, Is.EqualTo(ProductError.IncompatibleProtocol));
            ClientSessionComponent remote = CreateRemoteClient(); remote.JoinProduct().Forget(); Pump(100);
            Session session = remote.Session;
            using (var forged = GridSubmitRequest.Create()) session.Send(forged);
            Pump(900);
            Assert.That(((Participant)((Match)Grid.Match).GetChild<Participant>(local.MemberId)).Online, Is.True);
            Assert.That(((Participant)((Match)Grid.Match).GetChild<Participant>(remote.MemberId)).Online, Is.False);
        }
    }
}
