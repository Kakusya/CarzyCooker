using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using ET.Client;

namespace ET
{
    [FriendOf(typeof(CooperativeTargetPings))]
    public sealed partial class ProductSessionTests
    {
        private CooperativeTargetPings PingState => CurrentLevel.GetComponent<CooperativeTargetPings>();
        private TargetPingResponse PingReply(ClientSessionComponent client, TargetPingRequest request)
        {
            var task = client.CallProduct(request);
            return (TargetPingResponse)((LocalMessageComponent)client.Local != null ? FinishMessages(task, "Actual local marker mailbox reply") : Finish(task, 500));
        }

        private void PingDo(ClientSessionComponent client, TargetPingAction action, TargetPingKind kind = TargetPingKind.Station, long target = 0, string rejected = null)
        {
            using var reply = PingReply(client, client.GetComponent<GridClientComponent>().CreateTargetPing(action, kind, target));
            Assert.That(reply.Error == 0, Is.EqualTo(rejected == null), reply.Message);
            Assert.That(reply.Message, Is.EqualTo(rejected));
            if (rejected == null)
            {
                Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
            }
            Pump(8);
        }

        private TargetPingRequest PingCopy(TargetPingRequest original) => ProductMessages.Create<TargetPingRequest>(copy =>
        {
            copy.ProtocolVersion = original.ProtocolVersion; copy.MatchId = original.MatchId; copy.WorldId = original.WorldId;
            copy.WorldGeneration = original.WorldGeneration; copy.LevelId = original.LevelId; copy.LevelGeneration = original.LevelGeneration;
            copy.BindingGeneration = original.BindingGeneration; copy.CommandId = original.CommandId;
            copy.Action = original.Action; copy.TargetKind = original.TargetKind; copy.TargetId = original.TargetId;
        });

        [Test, Category("TargetPings")]
        public void EntryTargetPingActualKcpStableItemMovesReplacesCancelsAndConsumptionDoesNotRetargetNewContents()
        {
            StartHost(); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            var stove = PrepStation("灶台"); long pot = stove.ItemId;
            PrepDo(remote, GridAction.Move, cell: stove.StationApproaches().First(Grid.IsWalkable));
            PingDo(local, TargetPingAction.Set, TargetPingKind.Station, stove.Id);
            PingDo(remote, TargetPingAction.Set, TargetPingKind.Item, pot);
            Assert.That(PingState.Members.Count, Is.EqualTo(2));
            long serial = PingState.Members[remote.MemberId].Serial;
            PingDo(remote, TargetPingAction.Set, TargetPingKind.Table, pot, "PingTargetChanged");
            Assert.That(PingState.Members[remote.MemberId].Serial, Is.EqualTo(serial));
            PrepDo(remote, GridAction.TakeCounter, stove.Id, pot);
            var remoteGrid = remote.GetComponent<GridClientComponent>();
            PumpUntil(() => remoteGrid.Items[pot].Location == GridItemLocation.Hand && remoteGrid.TargetPings.Count == 2, "KCP copies marker and original held pot", 100, true);
            Assert.That(GridPresentationSystem.TargetPingCell(remoteGrid, remoteGrid.TargetPings.Single(value => value.MemberId == remote.MemberId)), Is.EqualTo(remoteGrid.Actors[remote.MemberId].Cell));
            var handoff = PrepStation("交接台");
            PrepDo(remote, GridAction.Move, cell: handoff.StationApproaches().First(Grid.IsWalkable));
            Pump(12);
            Assert.That(GridPresentationSystem.TargetPingCell(remoteGrid, remoteGrid.TargetPings.Single(value => value.MemberId == remote.MemberId)), Is.EqualTo(Grid.Actor(remote.MemberId).Cell));
            PrepDo(remote, GridAction.PlaceCounter, handoff.Id, pot);
            Pump(12);
            Assert.That(PingState.Members[remote.MemberId].TargetId, Is.EqualTo(pot));
            Assert.That(GridPresentationSystem.TargetPingCell(remoteGrid, remoteGrid.TargetPings.Single(value => value.MemberId == remote.MemberId)), Is.EqualTo(handoff.Cell));
            PingDo(remote, TargetPingAction.Set, TargetPingKind.Table, Grid.Stations.Values.Select(value => (GridStation)value).First(value => value.GetComponent<FrontTable>() != null).Id);
            Assert.That(PingState.Members[remote.MemberId].Kind, Is.EqualTo(TargetPingKind.Table));
            PingDo(remote, TargetPingAction.Clear);
            Assert.That(PingState.Members.Keys, Is.EqualTo(new[] { local.MemberId }));
            var source = PrepStation("备料台"); var chopping = PrepStation("切配台"); long tomato = source.ItemId;
            PrepDo(local, GridAction.TakeCounter, source.Id, tomato);
            PrepDo(local, GridAction.PlaceCounter, chopping.Id, tomato);
            PingDo(local, TargetPingAction.Set, TargetPingKind.Item, tomato);
            PrepDo(local, GridAction.StartStationWork, chopping.Id, tomato, tomato);
            var work = Grid.InputProcess(tomato);
            AdvanceTicks(work.RequiredTicks + 1); PumpMessages(8);
            Assert.That(work.State, Is.EqualTo(GridWorkState.Completed));
            Assert.That(Grid.Item(tomato), Is.Null);
            Assert.That(Grid.Item(work.OutputItemId).DefinitionId, Is.EqualTo(GridKitchenRules.TomatoChunks));
            Assert.That(PingState.Members, Is.Empty);
            PingDo(local, TargetPingAction.Set, TargetPingKind.Item, tomato, "PingTargetChanged");
            Assert.That(Grid.Item(pot).HolderId, Is.EqualTo(handoff.Id));
            PingDo(local, TargetPingAction.Set, TargetPingKind.Station, stove.Id);
            PingDo(remote, TargetPingAction.Set, TargetPingKind.Item, pot);
            var projected = remote.GetComponent<GridClientComponent>();
            var peer = (HostPeer)((Match)Grid.Match).GetChild<Participant>(remote.MemberId).BoundPeer;
            using (var dto = GridBroadcast.CreateSnapshot(Grid, peer))
            {
                dto.Version = projected.Version + 1;
                projected.ApplyGridSnapshot(dto);
                dto.TargetPings[0].TargetId = -1; dto.TargetPings.Clear();
            }
            Assert.That(projected.TargetPings.Count, Is.EqualTo(2));
            Assert.That(projected.TargetPings.All(value => value.TargetId > 0), Is.True);

        }

        [Test, Category("TargetPings")]
        public void EntryTargetPingActualTenBusinessSecondsFreezePauseAndReplayDoesNotExtendOrRevive()
        {
            StartHost(); ClientSessionComponent local = host.Client;
            var view = local.GetComponent<GridClientComponent>();
            var request = view.CreateTargetPing(TargetPingAction.Set, TargetPingKind.Station, PrepStation("灶台").Id);
            var replay = PingCopy(request); var lateReplay = PingCopy(request);
            long started = Grid.Tick;
            using (var result = PingReply(local, request))
            {
                Assert.That(result.Error, Is.Zero);
            }
            var marker = PingState.Members[local.MemberId];
            Assert.That(marker.ExpiresTick - started, Is.EqualTo(10000 / GridKitchenRules.TickMilliseconds));
            AdvanceTicks(30);
            long version = Grid.Version;
            using (var result = PingReply(local, replay))
            {
                Assert.That(result.Error, Is.Zero);
            }
            Assert.That(PingState.Members[local.MemberId].ExpiresTick, Is.EqualTo(marker.ExpiresTick));
            Assert.That(PingState.Members[local.MemberId].Serial, Is.EqualTo(marker.Serial));
            Assert.That(Grid.Version, Is.EqualTo(version), "Identical latest request cannot rebroadcast a new marker version");
            LevelDo(LevelControlAction.Pause);
            long frozen = Grid.Tick;
            AdvanceTicks(400);
            Assert.That(Grid.Tick, Is.EqualTo(frozen));
            Assert.That(PingState.Members.Count, Is.EqualTo(1));
            PingDo(local, TargetPingAction.Clear, rejected: "Paused");
            LevelDo(LevelControlAction.Resume);
            AdvanceTicks((int)(marker.ExpiresTick - Grid.Tick)); PumpMessages(8);
            Assert.That(PingState.Members, Is.Empty);
            using var late = PingReply(local, lateReplay);
            Assert.That(late.Error, Is.Not.Zero);
            Assert.That(PingState.Members, Is.Empty);
        }

        [Test, Category("TargetPings")]
        public void EntryTargetPingActualKcpDisconnectResumeLeaveAndNextLevelClearWithoutStoppingSharedSender()
        {
            StartHost(); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            PingDo(local, TargetPingAction.Set, TargetPingKind.Station, PrepStation("灶台").Id);
            PingDo(remote, TargetPingAction.Set, TargetPingKind.Item, PrepStation("灶台").ItemId);
            var oldBinding = remote.GetComponent<GridClientComponent>().CreateTargetPing(TargetPingAction.Set, TargetPingKind.Station, PrepStation("切配台").Id);
            long binding = remote.BindingGeneration;
            ProductRuntimeComponent runtime = remote.Runtime;
            ((Session)remote.Session).Dispose();
            Pump(240);
            Assert.That(PingState.Members.Keys, Is.EqualTo(new[] { local.MemberId }));
            var resume = runtime.ResumeProduct();
            PumpUntil(() => resume.Status != UniTaskStatus.Pending, "Real KCP marker resume", 500, true);
            resume.GetAwaiter().GetResult(); remote = runtime.Client;
            Assert.That(remote.BindingGeneration, Is.GreaterThan(binding));
            using (var denied = PingReply(remote, oldBinding))
            {
                Assert.That(denied.Error, Is.Not.Zero);
            }
            Assert.That(PingState.Members.ContainsKey(remote.MemberId), Is.False);
            PingDo(remote, TargetPingAction.Set, TargetPingKind.Station, PrepStation("切配台").Id);
            using (var leave = (ProductLeaveResponse)Finish(remote.CallProduct(ProductMessages.Create<ProductLeaveRequest>(value => value.MatchId = remote.MatchId)), 500))
            {
                Assert.That(leave.Error, Is.Zero);
            }
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(PingState.Members.ContainsKey(remote.MemberId), Is.False);
            PrepDo(local, GridAction.TakeCounter, PrepStation("碗台").Id, PrepStation("碗台").ItemId);
            Assert.That(Grid.Actor(local.MemberId).HandItemId, Is.Not.Zero, "Original sender and physical operations remain alive");
            var oldLevel = local.GetComponent<GridClientComponent>().CreateTargetPing(TargetPingAction.Set, TargetPingKind.Station, PrepStation("灶台").Id);
            CoopFrontStart(); FrontWait(() => CurrentLevel.Phase == LevelPhase.Completed, 4000);
            FrontWait(() => Renovation.Prepared, 20);
            if (!Renovation.SelectionFinalized)
            {
                RenovationChoose(Renovation.Candidates.First());
            }
            LevelDo(LevelControlAction.Continue);
            Assert.That(PingState.Members, Is.Empty);
            using var stale = PingReply(local, oldLevel);
            Assert.That(stale.Message, Is.EqualTo("WrongLevel"));
        }
    }
}
