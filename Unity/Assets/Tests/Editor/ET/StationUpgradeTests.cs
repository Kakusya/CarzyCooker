using System;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void UpgradeChoose(int reward, GridStation station, int expectedLevel = 0, string rejection = null)
        {
            using var reply = RenovationReply(RenovationView.CreateRenovationChoice(reward, RenovationView.RenovationChoiceVersion, station?.Id ?? 0, expectedLevel));
            Assert.That(reply.Error, Is.Zero, reply.Message); AdvanceTicks(1); PumpMessages(8);
            Assert.That(RenovationPeer.LastCommandStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), RenovationPeer.LastCommandReason);
            Assert.That(RenovationPeer.LastCommandReason, Is.EqualTo(rejection));
        }

        [Test]
        [Category("CooperativeUpgrade")]
        public void CoopUpgradeWrongPhaseCannotSelectBeforeSettlement()
        {
            StartHost(); Assert.That(Renovation.UpgradeTargets, Is.Empty); Assert.That(CurrentLevel.AllowedRenovationIds, Is.EqualTo(new[] { 1, 2, 3, 4 }));
            using var reply = RenovationReply(RenovationView.CreateRenovationChoice(3, 1, PrepStation("切配台").Id));
            Assert.That(reply.Error, Is.Not.Zero); Assert.That(reply.Message, Is.EqualTo("WrongPhase"));
            Assert.That(PrepStation("切配台").UpgradeLevel, Is.Zero);
        }

        [TestCase(1, 4, 2, 1, 3, 4)]
        [TestCase(99, 1, 2, 1, 3, 4)]
        [TestCase(1, 1, 2, 2, 3, 4)]
        [TestCase(1, 1, 2, 1, 3, 0)]
        public void StationUpgradeConfigRejectsCapabilityMissingKindMultilevelAndInvalidRatio(int kind, int capability, int work, int level, int numerator, int denominator)
        {
            var buffer = new Luban.ByteBuf(); buffer.WriteInt(1); buffer.WriteString("测试升级"); buffer.WriteInt(kind); buffer.WriteInt(capability);
            buffer.WriteInt(work); buffer.WriteInt(level); buffer.WriteInt(numerator); buffer.WriteInt(denominator);
            var rows = (System.Collections.Generic.List<DRStationUpgradeConfig>)Tables.Instance.DTStationUpgradeConfig.DataList;
            rows[0] = new DRStationUpgradeConfig(buffer);
            Assert.Throws<InvalidOperationException>(() => StationUpgradeConfig.ValidateAll()); Assert.That(host, Is.Null);
        }

        [Test]
        [Category("ProductSemanticEnums")]
        public void StationUpgradeRealManualNewWorkUsesCombinedTwentyThreeTicksAndOldWorkKeepsSixty()
        {
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(101), duration: 20));
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(102), pool: new[] { 4 }));
            StartSolo(); ClientSessionComponent client = host.Client; var source = PrepStation("备料台"); var chopping = PrepStation("切配台"); long raw = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, raw); PrepDo(client, GridAction.PlaceCounter, chopping.Id, raw);
            PrepDo(client, GridAction.StartStationWork, chopping.Id, raw, raw); var old = Grid.InputProcess(raw);
            PrepDo(client, GridAction.StopWork, input: raw, expectedTarget: raw, process: old.Id);
            RenovationComplete(); UpgradeChoose(3, chopping); LevelDo(LevelControlAction.Continue); BuffDo(4);
            Assert.That(Grid.Process(old.Id), Is.SameAs(old)); Assert.That(old.RequiredTicks, Is.EqualTo(60)); Assert.That(chopping.UpgradeLevel, Is.EqualTo(1));
            PrepDo(client, GridAction.StartStationWork, chopping.Id, raw, raw, old.Id); AdvanceTicks(61); PumpMessages(8);
            long chunks = old.OutputItemId; PrepDo(client, GridAction.TakeCounter, chopping.Id, chunks); PrepDo(client, GridAction.PlaceCounter, source.Id, chunks);
            var shipment = SupplyOrderReady(TomatoSupply.Id); SupplyDo(ReceiveIntent(shipment)); var receiver = Grid.Station(shipment.ReceivingStationId);
            var package = Grid.Item(receiver.ItemId); long nextRaw = package.Contents.First(); SupplyDo(PortionIntent(receiver, package.Id, nextRaw));
            PrepDo(client, GridAction.PlaceCounter, chopping.Id, nextRaw); PrepDo(client, GridAction.StartStationWork, chopping.Id, nextRaw, nextRaw);
            var next = Grid.InputProcess(nextRaw); Assert.That(next.RequiredTicks, Is.EqualTo(23)); Assert.That(old.RequiredTicks, Is.EqualTo(60));
            Assert.That(RenovationView.Processes[next.Id].RequiredTicks, Is.EqualTo(23));
            LevelDo(LevelControlAction.Pause); int progress = next.ProgressTicks; AdvanceTicks(20); Assert.That(next.ProgressTicks, Is.EqualTo(progress));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(24); PumpMessages(8); Assert.That(next.State, Is.EqualTo(GridWorkState.Completed));
            Assert.That(Grid.Item(next.OutputItemId).DefinitionId, Is.EqualTo(GridKitchenRules.TomatoChunks));
        }

        [TestCase(2, 2, 3, 2, 3, 1)]
        [TestCase(60, 1, 2, 3, 4, 23)]
        [TestCase(1, 1, 99, 1, 99, 1)]
        public void StationUpgradeTimingMultipliesBasisBeforeOneCeiling(int basis, int buffN, int buffD, int upgradeN, int upgradeD, int expected)
        {
            Assert.That(StationUpgradeConfig.CombinedTicks(basis, buffN, buffD, upgradeN, upgradeD), Is.EqualTo(expected));
        }

        [Test]
        public void StationUpgradeChoiceRealIdentityReplayConflictAndLateLevelMismatchKeepEverything()
        {
            StartSolo(); RenovationComplete(); GridStation target = PrepStation("切配台");
            UpgradeChoose(3, PrepStation("烤箱"), rejection: "InvalidUpgradeTarget");
            Assert.That(Renovation.SelectedDefinitionId, Is.Zero);
            var request = RenovationView.CreateRenovationChoice(3, Renovation.ChoiceVersion, target.Id, 0);
            var replay = CopyReplayRequest(request); var conflict = CopyReplayRequest(request); conflict.TargetStationId = PrepStation("烤箱").Id;
            using (var reply = RenovationReply(request)) Assert.That(reply.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            using (var reply = RenovationReply(replay)) Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
            using (var reply = RenovationReply(conflict)) Assert.That(reply.Message, Is.EqualTo("CommandConflict"));
            Assert.That((GridStation)Renovation.PendingUpgradeStation, Is.SameAs(target)); Assert.That(target.UpgradeLevel, Is.Zero);
            Level level = CurrentLevel; long layout = Grid.LayoutVersion; GridCell cell = target.Cell; long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            target.UpgradeLevel = 1;
            LevelDo(LevelControlAction.Continue, "UpgradeLevelChanged");
            Assert.That(CurrentLevel, Is.SameAs(level)); Assert.That(Grid.LayoutVersion, Is.EqualTo(layout));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items)); Assert.That(Renovation.AppliedStations, Is.Empty);
            target.UpgradeLevel = 0; LevelDo(LevelControlAction.Continue);
            Assert.That(Grid.Station(target.Id), Is.SameAs(target)); Assert.That(target.Cell, Is.EqualTo(cell)); Assert.That(target.UpgradeLevel, Is.EqualTo(1));
            Assert.That(target.UpgradeDefinitionId, Is.EqualTo(1)); Assert.That(Grid.Stations.Count, Is.EqualTo(16));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items)); Assert.That(Renovation.AppliedStations[3], Is.EqualTo(new[] { target.Id }));
            Assert.That(RenovationView.Stations[target.Id].UpgradeLevel, Is.EqualTo(1));
        }

        [Test]
        public void StationUpgradeCandidatesExcludeUpgradedMissingTargetsAndAllTerminalRewards()
        {
            StartSolo(); GridStation chopping = PrepStation("切配台"), oven = PrepStation("烤箱");
            chopping.UpgradeDefinitionId = 1; chopping.UpgradeLevel = 1;
            Renovation.AppliedStations.Add(3, Array.AsReadOnly(new[] { chopping.Id }));
            RenovationComplete(); Assert.That(Renovation.Candidates, Has.No.Member(3));
            Assert.That(Renovation.UpgradeTargets.Select(t => t.StationId), Is.EqualTo(new[] { oven.Id }));
            UpgradeChoose(4, oven); LevelDo(LevelControlAction.Continue); RenovationComplete();
            Assert.That(Renovation.UpgradeTargets, Is.Empty); Assert.That(Renovation.Candidates, Has.No.Member(3)); Assert.That(Renovation.Candidates, Has.No.Member(4));
            LevelDo(LevelControlAction.Continue); ChapterNaturalComplete(); Assert.That(Renovation.Candidates, Is.Empty); Assert.That(Renovation.UpgradeTargets, Is.Empty);
        }

        [Test]
        [Category("ProductSemanticEnums")]
        public void StationUpgradeRealBakingRetainsOldFrozenWorkAndNewCombinedBuffDurationAfterMove()
        {
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(101), duration: 20));
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(102), pool: new[] { 5 }));
            StartSolo(); var client = (ClientSessionComponent)host.Client; var oven = PrepStation("烤箱");
            var old = DishBegin(client); int ticks = old.RequiredTicks; long input = old.InputItemId;
            RenovationComplete(); int progress = old.ProgressTicks; UpgradeChoose(4, oven);
            Assert.That(oven.UpgradeLevel, Is.Zero); Assert.That(oven.ItemId, Is.EqualTo(input)); LevelDo(LevelControlAction.Continue);
            Assert.That(Grid.BakeProcess(old.Id), Is.SameAs(old)); Assert.That(old.RequiredTicks, Is.EqualTo(ticks)); Assert.That(old.ProgressTicks, Is.GreaterThanOrEqualTo(progress));
            Assert.That(Grid.Station(oven.Id), Is.SameAs(oven)); Assert.That(oven.UpgradeLevel, Is.EqualTo(1));
            BuffDo(5); LayoutDo(oven, new GridCell(5, 13)); Assert.That(old.RequiredTicks, Is.EqualTo(ticks));
            AdvanceTicks(ticks + 1); PumpMessages(8); long toast = old.OutputItemId;
            PrepDo(client, GridAction.TakeCounter, oven.Id, toast); PrepDo(client, GridAction.PlaceCounter, PrepStation("面包台").Id, toast);
            var breadSource = FixtureSingle(Tables.Instance.DTSupplyConfig.DataList, s => s.MaterialDefinitionId == GridKitchenRules.Bread, "upgrade bread supply");
            var shipment = SupplyOrderReady(breadSource.Id); SupplyDo(ReceiveIntent(shipment)); var receiving = Grid.Station(shipment.ReceivingStationId);
            var package = Grid.Item(receiving.ItemId); long raw = package.Contents.First(); SupplyDo(PortionIntent(receiving, package.Id, raw));
            PrepDo(client, GridAction.PlaceCounter, oven.Id, raw); PrepDo(client, GridAction.StartBake, oven.Id, raw, raw);
            var next = Grid.BakeProcess(oven.BakeProcessId); Assert.That(next.RequiredTicks, Is.EqualTo(45)); Assert.That(old.RequiredTicks, Is.EqualTo(80));
            Assert.That(RenovationView.Stations[oven.Id].Cell, Is.EqualTo(new GridCell(5, 13)));
            Assert.That(RenovationView.BakeProcesses[next.Id].RequiredTicks, Is.EqualTo(45));
        }

        [Test]
        public void StationUpgradeActualFilePendingAndInstalledRoundTripKeepGradeAndRejectCorruptValue()
        {
            StartSolo(); RenovationComplete(); var station = PrepStation("切配台"); UpgradeChoose(3, station);
            var header = SaveHeader(); var pending = SoloSaveCapture.Kitchen(Restaurant); var repository = TestSaveRepository(out string directory);
            try
            {
                Assert.That(pending.Stations.Single(s => s.Key == pending.PendingUpgradeStationKey).UpgradeLevel, Is.Zero);
                repository.Commit(header, pending); var data = repository.Select(header.SlotId, out var selected);
                host.StopProduct(); StartSavedFixture(selected, data); PumpMessages(8);
                var restored = PrepStation("切配台"); Assert.That(restored.UpgradeLevel, Is.EqualTo(1)); Assert.That(Grid.Stations.Count, Is.EqualTo(16));
                Assert.That(Renovation.AppliedStations[3], Is.EqualTo(new[] { restored.Id }));
                CurrentLevel.GetComponent<LevelBuff>().RequireChoice = false;
                RenovationComplete(); RenovationChoose(1); var installed = SoloSaveCapture.Kitchen(Restaurant); var nextHeader = SaveHeader();
                Assert.That(installed.Stations.Single(s => s.UpgradeDefinitionId == 1).UpgradeLevel, Is.EqualTo(1));
                repository.Commit(nextHeader, installed); var again = repository.Select(nextHeader.SlotId, out var second);
                var upgrade = again.Stations.Single(s => s.UpgradeDefinitionId == 1); upgrade.UpgradeLevel = 2;
                Assert.Throws<InvalidOperationException>(() => SoloSaveValidation.Payload(second, again, SaveProfileId)); upgrade.UpgradeLevel = 1;
                host.StopProduct(); StartSavedFixture(second, again); PumpMessages(8);
                Assert.That(PrepStation("切配台").UpgradeLevel, Is.EqualTo(1)); Assert.That(Grid.Stations.Count, Is.EqualTo(17));
                Assert.That(Renovation.AppliedStations[3].Count, Is.EqualTo(1)); Assert.That(repository.Browse().Length, Is.EqualTo(2));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
