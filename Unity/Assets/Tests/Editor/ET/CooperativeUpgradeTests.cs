using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [TestCase(false)]
        [TestCase(true)]
        [Category("CooperativeUpgrade")]
        public void CoopUpgradeRealKcpOldWorkKeepsFrozenTicksAndNewWorkCombinesOnce(bool baking)
        {
            int choice = baking ? 5 : 4;
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(1), duration: 20, pool: new[] { 1 }));
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(2), pool: new[] { choice }));
            StartHost();
            var local = (ClientSessionComponent)host.Client;
            var remote = CoopRemote();
            var projection = remote.GetComponent<GridClientComponent>();
            CurrentLevel.GetComponent<FrontLevelState>().NextArrivalOperatingTick = 100000;
            BuffDo(1);
            GridStation station = PrepStation(baking ? "烤箱" : "切配台");
            GridStation source = PrepStation(baking ? "面包台" : "备料台");
            long raw = source.ItemId;
            GridBakeProcess oldBake = null;
            GridManualProcess oldManual = null;
            if (baking)
            {
                oldBake = DishBegin(local);
            }
            else
            {
                PrepDo(local, GridAction.TakeCounter, source.Id, raw);
                PrepDo(local, GridAction.PlaceCounter, station.Id, raw);
                PrepDo(local, GridAction.StartStationWork, station.Id, raw, raw);
                oldManual = Grid.InputProcess(raw);
                PrepDo(local, GridAction.StopWork, input: raw, expectedTarget: raw, process: oldManual.Id);
            }
            int originalTicks = baking ? oldBake.RequiredTicks : oldManual.RequiredTicks;
            long originalProcess = baking ? oldBake.Id : oldManual.Id;
            int count = Grid.Stations.Count;
            GridCell cell = station.Cell;
            CoopRenovationComplete();
            UpgradeChoose(baking ? 4 : 3, station);
            Assert.That(station.UpgradeLevel, Is.Zero, "Choice is not installation");
            LevelDo(LevelControlAction.Continue);
            BuffDo(choice);
            Assert.That(Grid.Station(station.Id), Is.SameAs(station));
            Assert.That(station.Cell, Is.EqualTo(cell));
            Assert.That(Grid.Stations.Count, Is.EqualTo(count));
            Assert.That(station.UpgradeLevel, Is.EqualTo(1));
            Assert.That(baking ? Grid.BakeProcess(originalProcess).RequiredTicks : Grid.Process(originalProcess).RequiredTicks,
                Is.EqualTo(originalTicks));
            Assert.That(station.ItemId, Is.EqualTo(raw));
            if (!baking)
            {
                PrepDo(local, GridAction.StartStationWork, station.Id, raw, raw, originalProcess);
            }
            AdvanceTicks(originalTicks + 1);
            PumpMessages(8);
            long output = baking ? oldBake.OutputItemId : oldManual.OutputItemId;
            PrepDo(local, GridAction.TakeCounter, station.Id, output);
            PrepDo(local, GridAction.PlaceCounter, source.Id, output);
            var supply = Tables.Instance.DTSupplyConfig.DataList.Single(row => row.MaterialDefinitionId ==
                (baking ? GridKitchenRules.Bread : GridKitchenRules.Tomato));
            var shipment = SupplyOrderReady(supply.Id);
            SupplyDo(ReceiveIntent(shipment));
            GridStation receiving = Grid.Station(shipment.ReceivingStationId);
            GridItem package = Grid.Item(receiving.ItemId);
            long fresh = package.Contents.First();
            SupplyDo(PortionIntent(receiving, package.Id, fresh));
            PrepDo(local, GridAction.PlaceCounter, station.Id, fresh);
            PrepDo(local, baking ? GridAction.StartBake : GridAction.StartStationWork, station.Id, fresh, fresh);
            long next = baking ? station.BakeProcessId : Grid.InputProcess(fresh).Id;
            int expected = baking ? 45 : 23;
            Assert.That(baking ? Grid.BakeProcess(next).RequiredTicks : Grid.Process(next).RequiredTicks, Is.EqualTo(expected));
            PumpUntil(() => projection.Stations[station.Id].UpgradeLevel == 1 &&
                (baking ? projection.BakeProcesses.ContainsKey(next) : projection.Processes.ContainsKey(next)),
                "KCP receives installed grade and new frozen work", 200, advanceTime: true);
            Assert.That(baking ? projection.BakeProcesses[next].RequiredTicks : projection.Processes[next].RequiredTicks,
                Is.EqualTo(expected));
            Assert.That(baking ? oldBake.RequiredTicks : oldManual.RequiredTicks, Is.EqualTo(originalTicks));
            Assert.That(projection.Stations[station.Id].Cell, Is.EqualTo(cell));
            Assert.That(projection.Stations, Is.Not.SameAs(RenovationView.Stations));
        }

        [Test]
        [Category("CooperativeUpgrade")]
        public void CoopUpgradeRealKcpSelectionReplayLateTargetConflictAndResumeKeepSameStation()
        {
            StartHost();
            var remote = CoopRemote();
            ProductRuntimeComponent remoteRuntime = remote.Runtime;
            int member = remote.MemberId;
            CoopRenovationComplete();
            GridStation station = PrepStation("切配台");
            var view = remote.GetComponent<GridClientComponent>();
            Assert.That(view.UpgradeTargets.Any(target => target.DefinitionId == 3 && target.StationId == station.Id && target.ExpectedLevel == 0), Is.True);
            using (var denied = (ChooseRenovationResponse)Finish(remote.CallProduct(view.CreateRenovationChoice(3, view.RenovationChoiceVersion, station.Id)), 500))
            {
                Assert.That(denied.Message, Is.EqualTo("HostControlOnly"));
            }
            Assert.That(Renovation.SelectedDefinitionId, Is.Zero);
            UpgradeChoose(3, PrepStation("烤箱"), rejection: "InvalidUpgradeTarget");
            var request = RenovationView.CreateRenovationChoice(3, Renovation.ChoiceVersion, station.Id);
            var replay = CopyReplayRequest(request);
            var oldLevel = CopyReplayRequest(request);
            var conflict = CopyReplayRequest(request);
            conflict.TargetStationId = PrepStation("烤箱").Id;
            using (var accepted = RenovationReply(request))
            {
                Assert.That(accepted.Error, Is.Zero);
            }
            AdvanceTicks(1); PumpMessages(8);
            using (var repeated = RenovationReply(replay))
            {
                Assert.That(repeated.Status, Is.EqualTo((int)GridResultStatus.Executed));
            }
            using (var changed = RenovationReply(conflict))
            {
                Assert.That(changed.Message, Is.EqualTo("CommandConflict"));
            }
            long level = CurrentLevel.Id;
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            int count = Grid.Stations.Count;
            station.UpgradeLevel = 1; // 模拟选择后、提交前发现目标等级已变化。
            LevelDo(LevelControlAction.Continue, "UpgradeLevelChanged");
            Pump(20);
            Assert.That(CurrentLevel.Id, Is.EqualTo(level));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(Renovation.AppliedStations, Is.Empty);
            Assert.That(view.RenovationFailure, Is.EqualTo("UpgradeLevelChanged"));
            station.UpgradeLevel = 0;
            LevelDo(LevelControlAction.Continue);
            using (var old = RenovationReply(oldLevel))
            {
                Assert.That(old.Message, Is.EqualTo("WrongLevel"));
            }
            Assert.That(Renovation.AppliedStations[3], Is.EqualTo(new[] { station.Id }));
            Assert.That(Grid.Stations.Count, Is.EqualTo(count));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            ((Session)remote.Session).Dispose();
            Pump(240);
            Assert.That(remoteRuntime.Stage, Is.EqualTo(ProductStage.Disconnected));
            var resume = remoteRuntime.ResumeProduct();
            PumpUntil(() => resume.Status != UniTaskStatus.Pending, "Resume after installed upgrade", 500, advanceTime: true);
            resume.GetAwaiter().GetResult();
            remote = remoteRuntime.Client;
            PumpUntil(() => remote.GetComponent<GridClientComponent>().Confirmed, "Resumed independent projection", 100, advanceTime: true);
            view = remote.GetComponent<GridClientComponent>();
            Assert.That(remote.MemberId, Is.EqualTo(member));
            Assert.That(view.Stations[station.Id].UpgradeLevel, Is.EqualTo(1));
            Assert.That(view.Stations[station.Id].UpgradeDefinitionId, Is.EqualTo(1));
            Assert.That(view.RenovationApplied[3], Is.EqualTo(new[] { station.Id }));
            Assert.That(Grid.Station(station.Id), Is.SameAs(station));
            Assert.That(view.CanControlRestaurant, Is.False);
        }
    }
}
