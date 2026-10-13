using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        public void CoopLogisticsCleanupRemoteFarWaterFillDrainAndReplayKeepSameBowl()
        {
            StartHost(); var remote = CoopRemote(); var view = remote.GetComponent<GridClientComponent>();
            var sink = CleanupFacility(GridStationKind.Wash); long bowl = PrepStation("碗台").ItemId;
            PrepDo(remote, GridAction.TakeCounter, PrepStation("碗台").Id, bowl);
            PrepDo(remote, GridAction.Move, cell: new GridCell(1, 16));
            GridIntent intent = view.CleanupIntent(sink.Id, GridAction.FillWater);
            var request = view.CreateGridCommand(intent); long command = request.CommandId; var replay = CopyReplayRequest(request);
            Assert.That(CoopLogisticsCall(remote, request).Error, Is.Zero);
            Assert.That(Grid.Item(bowl).Contents, Is.Empty, "Far receipt cannot create water");
            AdvanceTicks(1); Assert.That(Grid.Item(bowl).Contents, Is.Empty);
            CleanupWait(remote, command); long water = Grid.Item(bowl).Contents.Single();
            Assert.That(Grid.AtStation(Grid.Actor(remote.MemberId), sink), Is.True);
            Assert.That(Grid.Item(water).DefinitionId, Is.EqualTo(GridKitchenRules.Water)); Assert.That(Grid.Item(water).HolderId, Is.EqualTo(bowl));
            Assert.That(CoopLogisticsCall(remote, replay).Status, Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { water }));
            CoopCleanupDo(remote, GridAction.DrainLiquid, CleanupFacility(GridStationKind.Trash), "RequiresSink");
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { water }));
            CoopCleanupDo(remote, GridAction.FillWater, sink, "ContainerFull");
            CoopCleanupDo(remote, GridAction.DrainLiquid, sink);
            Assert.That(Grid.Item(water), Is.Null); Assert.That(Grid.Item(bowl).Contents, Is.Empty);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(bowl)); Assert.That(Grid.Item(bowl).Hygiene, Is.EqualTo(ItemHygiene.Clean));
            CoopCleanupDo(remote, GridAction.FillWater, sink); Assert.That(Grid.Item(bowl).Contents.Single(), Is.Not.EqualTo(water));
        }

        [TestCase("dirty")]
        [TestCase("plate")]
        public void CoopLogisticsCleanupRemoteDirtyOrIncompatibleVesselCannotCreateWater(string fault)
        {
            StartHost(); var remote = CoopRemote(); var station = PrepStation(fault == "plate" ? "盘台" : "碗台");
            long vessel = station.ItemId; if (fault == "dirty") Grid.Item(vessel).Hygiene = ItemHygiene.Dirty;
            PrepDo(remote, GridAction.TakeCounter, station.Id, vessel); int items = Grid.Items.Count;
            CoopCleanupDo(remote, GridAction.FillWater, CleanupFacility(GridStationKind.Wash), fault == "dirty" ? "DirtyUtensil" : "IncompatibleContainer");
            Assert.That(Grid.Items.Count, Is.EqualTo(items)); Assert.That(Grid.Item(vessel).Contents, Is.Empty);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(vessel));
        }

        [Test]
        public void CoopLogisticsCleanupRemoteCookingLockWrongFacilityAndRemainingSoupRetireOnlyOriginalBatch()
        {
            StartHost(); ClientSessionComponent local = host.Client; var remote = CoopRemote();
            var batch = CoopSoup(local, remote); batch.RequiredTicks = 600; long pot = batch.PotItemId, batchId = batch.Id;
            var stove = PrepStation("灶台"); long[] inputs = batch.InputItemIds.ToArray();
            PrepDo(remote, GridAction.TakeCounter, stove.Id, pot);
            CoopCleanupDo(remote, GridAction.DrainLiquid, CleanupFacility(GridStationKind.Wash), "ProcessLocked");
            Assert.That(Grid.Item(pot).Contents, Is.EqualTo(inputs)); Assert.That(Grid.AutoProcess(batchId), Is.SameAs(batch));
            PrepDo(remote, GridAction.PlaceCounter, PrepStation("交接台").Id, pot); AdvanceTicks(batch.RequiredTicks); PumpMessages(8);
            long bowl = PrepStation("碗台").ItemId;
            PrepDo(local, GridAction.TakeCounter, PrepStation("碗台").Id, bowl);
            PrepDo(local, GridAction.PortionSoup, PrepStation("交接台").Id, bowl, pot, batchId);
            long portion = Grid.Item(bowl).Contents.Single(); Assert.That(batch.RemainingPortions, Is.EqualTo(1));
            PrepDo(remote, GridAction.TakeCounter, PrepStation("交接台").Id, pot);
            CoopCleanupDo(remote, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash), "RequiresSolid");
            Assert.That(Grid.Item(pot).AutoProcessId, Is.EqualTo(batchId)); Assert.That(batch.RemainingPortions, Is.EqualTo(1));
            PrepDo(remote, GridAction.Move, cell: new GridCell(1, 16));
            var view = remote.GetComponent<GridClientComponent>(); var request = view.CreateGridCommand(view.CleanupIntent(CleanupFacility(GridStationKind.Wash).Id, GridAction.DrainLiquid));
            long command = request.CommandId; var replay = CopyReplayRequest(request);
            Assert.That(CoopLogisticsCall(remote, request).Error, Is.Zero); Assert.That(Grid.Item(pot).AutoProcessId, Is.EqualTo(batchId));
            CleanupWait(remote, command); Assert.That(CoopLogisticsCall(remote, replay).Status, Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(Grid.AutoProcess(batchId), Is.Null); Assert.That(Grid.Item(pot).AutoProcessId, Is.Zero); Assert.That(stove.AutoProcessId, Is.Zero);
            Assert.That(Grid.Item(pot).Contents, Is.Empty); Assert.That(inputs.All(id => Grid.Item(id) == null), Is.True);
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { portion })); Assert.That(Grid.Item(portion), Is.Not.Null);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(pot));
        }

        [Test]
        public void CoopLogisticsCleanupRemoteCannotUseSinkOccupiedByRealCompanionWash()
        {
            var remote = CoopFrontFixture(); FrontWork clear = InitialDirtyTable();
            CoopFrontDo(remote, FrontWorkAction.Claim, clear); FrontWait(() => clear.State == FrontWorkState.Completed);
            FrontWork wash = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash); wash.BaseTicks = 600;
            Assert.That(FrontNpcReply(FrontRole, new FrontWorkIntent(FrontWorkAction.Claim, wash.Id, wash.ClaimSerial)).Error, Is.Zero);
            AdvanceTicks(1); FrontWait(() => wash.State == FrontWorkState.Working);
            var source = PrepStation("第二碗台"); long bowl = source.ItemId; PrepDo(remote, GridAction.TakeCounter, source.Id, bowl);
            long worker = wash.WorkerId, serial = wash.ClaimSerial; double progress = wash.Progress;
            CoopCleanupDo(remote, GridAction.FillWater, CleanupFacility(GridStationKind.Wash), "StationBusy");
            Assert.That(Grid.Item(bowl).Contents, Is.Empty); Assert.That(wash.WorkerId, Is.EqualTo(worker)); Assert.That(wash.ClaimSerial, Is.EqualTo(serial));
            Assert.That(wash.Progress, Is.GreaterThanOrEqualTo(progress)); Assert.That(wash.State, Is.EqualTo(FrontWorkState.Working));
        }
    }
}
