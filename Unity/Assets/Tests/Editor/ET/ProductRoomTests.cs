using System.Linq;
using Cysharp.Threading.Tasks;
using Game;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(ProductRoomClient))]
    public sealed partial class ProductSessionTests
    {
        private void StartRoomFixture()
        {
            var config = RuntimeInstanceConfig.Create("room-test", "host", "host", "127.0.0.1:0");
            RuntimeInstanceContext.Prepare(config);
            Fiber fiber = CreateFiber();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeProductRoom(menuVariant: 0);
            PumpUntil(() => ((ClientSessionComponent)host.Client).Stage == ProductClientStage.Joined &&
                ((ClientSessionComponent)host.Client).GetComponent<ProductRoomClient>().Current.Version > 0,
                "Host confirms room without a kitchen", 100, advanceTime: true);
        }

        private ClientSessionComponent JoinRoomFixture()
        {
            var client = CreateRemoteClient();
            client.JoinProduct().Forget();
            PumpUntil(() => client.Stage == ProductClientStage.Joined && client.GetComponent<ProductRoomClient>().Current.Version > 0,
                "KCP joins a room without a kitchen", 200, advanceTime: true);
            return client;
        }

        private ProductRoomControlResponse RoomReply(ClientSessionComponent client, ProductRoomControlRequest request)
            => (ProductRoomControlResponse)Finish(client.CallProduct(request), 500);

        private void RoomDo(ProductRoomAction action)
        {
            var client = (ClientSessionComponent)host.Client;
            using var reply = RoomReply(client, client.CreateRoomCommand(action, menuVariant: 0));
            Assert.That(reply.Error, Is.Zero, reply.Message);
            Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
            Pump(16);
        }

        private ProductRoomControlRequest RoomCopy(ProductRoomControlRequest source) => ProductMessages.Create<ProductRoomControlRequest>(copy =>
        {
            copy.ProtocolVersion = source.ProtocolVersion;
            copy.MatchId = source.MatchId;
            copy.BindingGeneration = source.BindingGeneration;
            copy.CommandId = source.CommandId;
            copy.ExpectedRoomVersion = source.ExpectedRoomVersion;
            copy.Action = source.Action;
            copy.MenuVariant = source.MenuVariant;
            copy.ContentChapterId = source.ContentChapterId;
        });

        [Test]
        [Category("ProductRoom")]
        [Category("ProductSemanticEnums")]
        public void ProductRoomRealKcpJoinWithoutWorldStartCloseAndRestartKeepMembersSessionsAndSender()
        {
            StartRoomFixture();
            var local = (ClientSessionComponent)host.Client;
            var remote = JoinRoomFixture();
            HostSessionComponent authority = host.Authority;
            Match match = authority.Match;
            string matchId = match.MatchId;
            int member = remote.MemberId;
            Session session = remote.Session;
            Assert.That(Grid, Is.Null);
            Assert.That(match.Children.Values.OfType<RestaurantRuntime>(), Is.Empty);
            Assert.That(remote.GetComponent<GridClientComponent>().Confirmed, Is.False);
            Assert.That(remote.GetComponent<GridClientComponent>().WorldId, Is.Null);
            Assert.That(remote.GetComponent<ProductRoomClient>().Current.HasRestaurant, Is.False);
            Assert.That(remote.GetComponent<ProductRoomClient>().Current.CanControlRoom, Is.False);
            RoomDo(ProductRoomAction.StartRestaurant);
            PumpUntil(() => remote.GetComponent<GridClientComponent>().Confirmed, "First world ready after actual snapshot", 100, advanceTime: true);
            RestaurantRuntime first = match.Restaurant;
            string world = Grid.WorldId;
            PrepDo(remote, GridAction.TakeCounter, PrepStation("碗台").Id, PrepStation("碗台").ItemId);
            RoomDo(ProductRoomAction.CloseRestaurant);
            PumpUntil(() => !remote.GetComponent<ProductRoomClient>().Current.HasRestaurant, "Remote sees retirement", 100, advanceTime: true);
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(Grid, Is.Null);
            Assert.That(remote.GetComponent<GridClientComponent>().Confirmed, Is.False);
            Assert.That(remote.GetComponent<GridClientComponent>().Items, Is.Empty);
            Assert.That(remote.Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That((Session)remote.Session, Is.SameAs(session));
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
            Assert.That(match.Children.Values.OfType<Participant>().Count(), Is.EqualTo(2));
            RoomDo(ProductRoomAction.StartRestaurant);
            PumpUntil(() => remote.GetComponent<GridClientComponent>().Confirmed, "Second world ready", 100, advanceTime: true);
            Assert.That(Grid.WorldId, Is.Not.EqualTo(world));
            Assert.That(match.MatchId, Is.EqualTo(matchId));
            Assert.That(remote.MemberId, Is.EqualTo(member));
            Assert.That(remote.GetComponent<GridClientComponent>().Actors[member].HandItemId, Is.Zero);
            Assert.That(match.Children.Values.OfType<RestaurantRuntime>().Count(), Is.EqualTo(1));
            Assert.That(local.GetComponent<ProductRoomClient>().Current.WorldId, Is.EqualTo(Grid.WorldId));
        }

        [Test]
        [Category("ProductRoom")]
        public void ProductRoomRealKcpCannotStartAndWorldCommandWithoutRestaurantRejects()
        {
            StartRoomFixture();
            var remote = JoinRoomFixture();
            using (var denied = RoomReply(remote, remote.CreateRoomCommand(ProductRoomAction.StartRestaurant, 0)))
            {
                Assert.That(denied.Message, Is.EqualTo("HostControlOnly"));
            }
            using (var denied = (GridCommandResponse)Finish(remote.CallProduct(ProductMessages.Create<GridCommandRequest>(request =>
            {
                request.ProtocolVersion = ProductSessionRules.ProtocolVersion;
                request.MatchId = remote.MatchId;
                request.BindingGeneration = remote.BindingGeneration;
                request.CommandId = 2;
                request.Action = (int)GridAction.Move;
                request.WorldId = "no-world";
                request.WorldGeneration = 1;
            })), 500))
            {
                Assert.That(denied.Message, Is.EqualTo("RestaurantUnavailable"));
            }
            Assert.That(Grid, Is.Null);
            Assert.That(remote.Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
        }

        [Test]
        [Category("ProductRoom")]
        public void ProductRoomReplayConflictOldRoomAndRetiredSnapshotNeverCreateAnotherWorld()
        {
            StartRoomFixture();
            var client = (ClientSessionComponent)host.Client;
            var request = client.CreateRoomCommand(ProductRoomAction.StartRestaurant, 0);
            var replay = RoomCopy(request);
            var conflict = RoomCopy(request);
            conflict.MenuVariant = (int)ProductMenuVariant.Drink;
            using (var reply = RoomReply(client, request))
            {
                Assert.That(reply.Error, Is.Zero);
            }
            Pump(16);
            string world = Grid.WorldId;
            using (var repeated = RoomReply(client, replay))
            {
                Assert.That(repeated.Status, Is.EqualTo((int)GridResultStatus.Executed));
            }
            using (var changed = RoomReply(client, conflict))
            {
                Assert.That(changed.Message, Is.EqualTo("CommandConflict"));
            }
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            using var staleWorld = GridBroadcast.CreateSnapshot(Grid, peer);
            RoomDo(ProductRoomAction.CloseRestaurant);
            var grid = client.GetComponent<GridClientComponent>();
            staleWorld.Version = long.MaxValue;
            grid.ApplyGridSnapshot(staleWorld);
            Assert.That(grid.WorldId, Is.Null);
            Assert.That(grid.Confirmed, Is.False);
            var oldRoom = client.CreateRoomCommand(ProductRoomAction.StartRestaurant, 0);
            --oldRoom.ExpectedRoomVersion;
            using (var denied = RoomReply(client, oldRoom))
            {
                Assert.That(denied.Message, Is.EqualTo("RoomChanged"));
            }
            Assert.That(Grid, Is.Null);
            RoomDo(ProductRoomAction.StartRestaurant);
            grid.ApplyGridSnapshot(staleWorld);
            Assert.That(grid.WorldId, Is.EqualTo(Grid.WorldId).And.Not.EqualTo(world));
        }

        [Test]
        [Category("ProductRoom")]
        public void ProductRoomExistingDirectHostStillLoadsOneWorldAndCanCook()
        {
            StartHost();
            var remote = CoopRemote();
            Assert.That(((ClientSessionComponent)host.Client).GetComponent<ProductRoomClient>().Current.HasRestaurant, Is.True);
            Assert.That(remote.GetComponent<ProductRoomClient>().Current.WorldId, Is.EqualTo(Grid.WorldId));
            PrepDo(remote, GridAction.TakeCounter, PrepStation("碗台").Id, PrepStation("碗台").ItemId);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.Not.Zero);
        }
    }
}
