using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(ProductRoomReadiness))]
    public sealed partial class ProductSessionTests
    {
        private ProductRoomProjection RoomState(ClientSessionComponent client) => client.GetComponent<ProductRoomClient>().Current;
        private ProductRoomReadiness ReadyState => ((Match)((HostSessionComponent)host.Authority).Match).GetComponent<ProductRoomReadiness>();

        private void ReadinessAction(ClientSessionComponent client, ProductRoomAction action, string rejected = null, ProductMenuVariant menu = ProductMenuVariant.Basic, int chapter = 0)
        {
            using var reply = RoomReply(client, client.CreateRoomCommand(action, menu, chapter));
            Assert.That(reply.Error == 0, Is.EqualTo(rejected == null), reply.Message);
            Assert.That(reply.Message, Is.EqualTo(rejected));
            Pump(12);
        }

        [Test, Category("RoomReadiness")]
        public void EntryRoomReadinessActualExplicitHostBootstrapPublishesButCannotCreateKitchenBeforeHostReady()
        {
            StartHost(startRestaurant: false);
            ClientSessionComponent local = host.Client;
            Assert.That(ReadyState.HasProposal, Is.True);
            Assert.That(ReadyState.Ready, Is.Empty);
            Assert.That(Grid, Is.Null);
            ReadinessAction(local, ProductRoomAction.StartRestaurant, "MembersNotReady");
            StartPublishedRoomForFixture();
            Assert.That(Grid, Is.Not.Null);
            Assert.That(Grid.Actor(local.MemberId), Is.Not.Null);
        }

        [Test, Category("RoomReadiness")]
        public void EntryRoomReadinessActualKcpSharedProposalAllMembersCannotForceAndReplayCannotCreateTwice()
        {
            StartRoomFixture();
            ClientSessionComponent local = host.Client;
            var remote = JoinRoomFixture();
            ReadinessAction(local, ProductRoomAction.StartRestaurant, "RoomProposalRequired");
            ReadinessAction(local, ProductRoomAction.PublishProposal);
            PumpUntil(() => RoomState(remote).HasProposal && RoomState(remote).ProposalVersion == RoomState(local).ProposalVersion, "Real KCP shares proposal without kitchen", 100, true);
            long proposal = ReadyState.ProposalVersion;
            Assert.That(Grid, Is.Null);
            Assert.That(RoomState(remote).ReadyMembers.Count, Is.EqualTo(2));
            ReadinessAction(local, ProductRoomAction.StartRestaurant, "MembersNotReady");
            ReadinessAction(local, ProductRoomAction.Ready);
            ReadinessAction(local, ProductRoomAction.StartRestaurant, "MembersNotReady");
            var ready = remote.CreateRoomCommand(ProductRoomAction.Ready);
            var duplicate = RoomCopy(ready);
            using (var reply = RoomReply(remote, ready))
            {
                Assert.That(reply.Error, Is.Zero, reply.Message);
            }
            using (var replay = RoomReply(remote, duplicate))
            {
                Assert.That(replay.Error, Is.Zero, replay.Message);
            }
            PumpUntil(() => RoomState(local).AllReady && RoomState(remote).AllReady, "Both peers ready on same proposal", 100, true);
            Assert.That(ReadyState.ProposalVersion, Is.EqualTo(proposal));
            Assert.That(Grid, Is.Null);
            ReadinessAction(remote, ProductRoomAction.CancelReady);
            Assert.That(RoomState(local).AllReady, Is.False);
            Assert.That(ReadyState.Ready.Count, Is.EqualTo(1));
            ReadinessAction(remote, ProductRoomAction.Ready);
            ReadinessAction(local, ProductRoomAction.PublishProposal, menu: ProductMenuVariant.Drink);
            Assert.That(ReadyState.Ready, Is.Empty, "Publishing another menu clears every member, including Host");
            ReadinessAction(local, ProductRoomAction.StartRestaurant, "MembersNotReady", ProductMenuVariant.Drink);
            ReadinessAction(local, ProductRoomAction.Ready);
            ReadinessAction(remote, ProductRoomAction.Ready);
            var start = local.CreateRoomCommand(ProductRoomAction.StartRestaurant, ProductMenuVariant.Drink);
            var repeated = RoomCopy(start);
            using (var reply = RoomReply(local, start))
            {
                Assert.That(reply.Error, Is.Zero, reply.Message);
            }
            Match match = ((HostSessionComponent)host.Authority).Match;
            RestaurantRuntime restaurant = match.Restaurant;
            using (var replay = RoomReply(local, repeated))
            {
                Assert.That(replay.Error, Is.Zero, replay.Message);
            }
            PumpUntil(() => remote.GetComponent<GridClientComponent>().Confirmed, "Only one actual kitchen is installed", 100, true);
            Assert.That((RestaurantRuntime)match.Restaurant, Is.SameAs(restaurant));
            Assert.That(match.Children.Values.OfType<RestaurantRuntime>().Count(), Is.EqualTo(1));
            Assert.That(ReadyState.Ready, Is.Empty);
            ReadinessAction(local, ProductRoomAction.CloseRestaurant);
            Assert.That(Grid, Is.Null);
            Assert.That(ReadyState.HasProposal, Is.False);
            ReadinessAction(local, ProductRoomAction.StartRestaurant, "RoomProposalRequired");
            ReadinessAction(local, ProductRoomAction.PublishProposal);
            var stale = remote.CreateRoomCommand(ProductRoomAction.Ready);
            stale.ExpectedProposalVersion = proposal;
            using var denied = RoomReply(remote, stale);
            Assert.That(denied.Message, Is.EqualTo("RoomProposalChanged"));
            Assert.That(ReadyState.Ready, Is.Empty);
        }

        [Test, Category("RoomReadiness")]
        public void EntryRoomReadinessActualJoinLeaveDisconnectAndResumeClearEveryReadyBinding()
        {
            StartRoomFixture();
            ClientSessionComponent local = host.Client;
            var remote = JoinRoomFixture();
            ReadinessAction(local, ProductRoomAction.PublishProposal);
            ReadinessAction(local, ProductRoomAction.Ready);
            ReadinessAction(remote, ProductRoomAction.Ready);
            long proposal = ReadyState.ProposalVersion;
            var third = JoinRoomFixture();
            Assert.That(ReadyState.ProposalVersion, Is.GreaterThan(proposal));
            Assert.That(ReadyState.Ready, Is.Empty);
            ReadinessAction(local, ProductRoomAction.StartRestaurant, "MembersNotReady");
            ReadinessAction(local, ProductRoomAction.Ready);
            ReadinessAction(remote, ProductRoomAction.Ready);
            ReadinessAction(third, ProductRoomAction.Ready);
            using (var leave = (ProductLeaveResponse)Finish(third.CallProduct(ProductMessages.Create<ProductLeaveRequest>(value => value.MatchId = third.MatchId)), 500))
            {
                Assert.That(leave.Error, Is.Zero);
            }
            Assert.That(ReadyState.Ready, Is.Empty);
            ReadinessAction(local, ProductRoomAction.Ready);
            ReadinessAction(remote, ProductRoomAction.Ready);
            long binding = remote.BindingGeneration;
            var oldBindingReady = remote.CreateRoomCommand(ProductRoomAction.Ready);
            ProductRuntimeComponent runtime = remote.Runtime;
            ((Session)remote.Session).Dispose();
            Pump(240);
            Assert.That(runtime.Stage, Is.EqualTo(ProductStage.Disconnected));
            Assert.That(ReadyState.Ready, Is.Empty);
            ReadinessAction(local, ProductRoomAction.Ready);
            var recovery = runtime.ResumeProduct();
            PumpUntil(() => recovery.Status != UniTaskStatus.Pending, "Actual room resume changes binding", 500, true);
            recovery.GetAwaiter().GetResult();
            remote = runtime.Client;
            PumpUntil(() => RoomState(remote).Version > 0, "Recovered empty-room projection", 100, true);
            Assert.That(remote.BindingGeneration, Is.GreaterThan(binding));
            Assert.That(ReadyState.Ready, Is.Empty, "Recovery clears even the Host ready recorded while Client was offline");
            Assert.That(Grid, Is.Null);
            using (var oldBinding = RoomReply(remote, oldBindingReady))
            {
                Assert.That(oldBinding.Message, Is.EqualTo("EntryClosed"));
            }
            ReadinessAction(local, ProductRoomAction.StartRestaurant, "MembersNotReady");
        }

        [Test, Category("RoomReadiness")]
        public void EntryRoomReadinessActualLocalSourceRevisionRequiresRepublishAndPrepareWithoutWritingProgress()
        {
            StartSolo();
            var payload = CompleteForSave(); var header = SaveHeader();
            var repository = TestSaveRepository(out string directory);
            try
            {
                repository.Commit(header, payload);
                string record = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] original = File.ReadAllBytes(record);
                repository.Dispose(); host.StopProduct(); Game.RuntimeInstanceContext.Reset(); StartRoomFixture();
                repository = host.Root().AddComponent<SoloSaveRepository, string, string>(directory, SaveProfileId);
                ClientSessionComponent local = host.Client;
                ProveFixtureProfile(local, repository);
                byte[] authorization = CooperativeBaselineSystem.Authorize(repository, header.SlotId, local.MatchId, OfflineProfiles.OwnIdentity(repository));
                using (var reply = BaselineReply(local, local.CreateBaselineCommand(authorization)))
                {
                    Assert.That(reply.Error, Is.Zero, reply.Message);
                }
                Pump(12);
                Assert.That(Grid, Is.Null, "Loading a valid source only publishes a proposal");
                Assert.That(ReadyState.HasProposal, Is.True);
                Assert.That(File.ReadAllBytes(record), Is.EqualTo(original));
                ReadinessAction(local, ProductRoomAction.Ready);
                header.Revision++; header.ResultId = Guid.NewGuid().ToString("N"); header.SavedUtcTicks++;
                byte[] revised = SoloSaveFile.Encode(header, payload);
                File.WriteAllBytes(record, revised);
                ReadinessAction(local, ProductRoomAction.StartRestaurant, "RoomSourceChanged");
                Assert.That(ReadyState.Ready, Is.Empty);
                Assert.That(ReadyState.HasProposal, Is.False);
                Assert.That(Grid, Is.Null);
                authorization = CooperativeBaselineSystem.Authorize(repository, header.SlotId, local.MatchId, OfflineProfiles.OwnIdentity(repository));
                using (var reply = BaselineReply(local, local.CreateBaselineCommand(authorization)))
                {
                    Assert.That(reply.Error, Is.Zero, reply.Message);
                }
                Pump(12);
                ReadinessAction(local, ProductRoomAction.Ready);
                ReadinessAction(local, ProductRoomAction.StartRestaurant);
                Assert.That(Grid, Is.Not.Null);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(header.NextLevelId));
                Assert.That(File.ReadAllBytes(record), Is.EqualTo(revised));
                Assert.That(Directory.GetFiles(repository.DirectoryPath, "*.ccsave").Length, Is.EqualTo(1));
            }
            finally
            {
                repository.Dispose(); RemoveOwnSaveTestDirectory(directory);
            }
        }
    }
}
