using System;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private DRBuffConfig VarietyDefinition(int kind, int manual, int bake, int numerator = 1, int denominator = 2)
        {
            var buffer = new Luban.ByteBuf(); buffer.WriteInt(4); buffer.WriteString("测试目标效果"); buffer.WriteInt(kind);
            buffer.WriteInt(numerator); buffer.WriteInt(denominator); buffer.WriteInt(0); buffer.WriteInt(manual); buffer.WriteInt(bake);
            return new DRBuffConfig(buffer);
        }

        [TestCase(2, 0, 1, 1, 2)]
        [TestCase(2, 2, 0, 1, 2)]
        [TestCase(3, 1, 0, 1, 2)]
        [TestCase(3, 0, 999, 1, 2)]
        [TestCase(2, 1, 0, 0, 2)]
        [TestCase(3, 0, 1, 1, 0)]
        public void BuffVarietyConfigRejectsWrongCategoryHeldWorkUnknownRecipeAndNonpositiveRatio(int kind, int manual, int bake, int numerator, int denominator)
        {
            var rows = (System.Collections.Generic.List<DRBuffConfig>)Tables.Instance.DTBuffConfig.DataList;
            rows[3] = VarietyDefinition(kind, manual, bake, numerator, denominator);
            Assert.Throws<InvalidOperationException>(() => BuffConfig.ValidateAll()); Assert.That(host, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BuffVarietyRealOldAndNewManualOrBakePauseAndCarryKeepOwnFrozenTicks(bool baking)
        {
            int choice = baking ? 5 : 4;
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(101), duration: 20, pool: new[] { choice }));
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(102), pool: new[] { choice }));
            StartSolo(liveBuff: true); ClientSessionComponent client = host.Client;
            GridStation station = PrepStation(baking ? "烤箱" : "切配台");
            GridManualProcess oldManual = null; GridBakeProcess oldBake = null;
            if (baking) oldBake = DishBegin(client);
            else
            {
                var source = PrepStation("备料台"); long input = source.ItemId;
                PrepDo(client, GridAction.TakeCounter, source.Id, input); PrepDo(client, GridAction.PlaceCounter, station.Id, input);
                PrepDo(client, GridAction.StartStationWork, station.Id, input, input); oldManual = Grid.InputProcess(input);
            }
            int originalTicks = baking ? oldBake.RequiredTicks : oldManual.RequiredTicks;
            Assert.That(originalTicks, Is.EqualTo(baking ? 80 : 60));
            BuffDo(choice); Assert.That(baking ? oldBake.RequiredTicks : oldManual.RequiredTicks, Is.EqualTo(originalTicks));
            LevelDo(LevelControlAction.Pause); int progress = baking ? oldBake.ProgressTicks : oldManual.ProgressTicks;
            AdvanceTicks(20); Assert.That(baking ? oldBake.ProgressTicks : oldManual.ProgressTicks, Is.EqualTo(progress));
            LevelDo(LevelControlAction.Resume);
            if (!baking) PrepDo(client, GridAction.StopWork, input: oldManual.InputItemId, expectedTarget: oldManual.TargetItemId, process: oldManual.Id);
            LevelDo(LevelControlAction.Start); AdvanceTicks(22); PumpMessages(8);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
            long oldId = baking ? oldBake.Id : oldManual.Id;
            LevelDo(LevelControlAction.Continue); BuffDo(choice);
            Assert.That(baking ? oldBake.RequiredTicks : oldManual.RequiredTicks, Is.EqualTo(originalTicks));
            if (!baking) PrepDo(client, GridAction.StartStationWork, station.Id, oldManual.InputItemId, oldManual.TargetItemId, oldManual.Id);
            AdvanceTicks(originalTicks + 1); PumpMessages(8);
            long output = baking ? oldBake.OutputItemId : oldManual.OutputItemId;
            Assert.That(Grid.Item(output), Is.Not.Null);
            PrepDo(client, GridAction.TakeCounter, station.Id, output); PrepDo(client, GridAction.PlaceCounter, PrepStation(baking ? "面包台" : "备料台").Id, output);
            int material = baking ? GridKitchenRules.Bread : GridKitchenRules.Tomato;
            var sourceDefinition = FixtureSingle(Tables.Instance.DTSupplyConfig.DataList, row => row.MaterialDefinitionId == material, "new timed work material");
            var shipment = SupplyOrderReady(sourceDefinition.Id); SupplyDo(ReceiveIntent(shipment));
            var receiver = Grid.Station(shipment.ReceivingStationId); var package = Grid.Item(receiver.ItemId);
            long raw = package.Contents.First(); SupplyDo(PortionIntent(receiver, package.Id, raw));
            PrepDo(client, GridAction.PlaceCounter, station.Id, raw);
            PrepDo(client, baking ? GridAction.StartBake : GridAction.StartStationWork, station.Id, raw, raw);
            int frozen = baking ? Grid.BakeProcess(station.BakeProcessId).RequiredTicks : Grid.InputProcess(raw).RequiredTicks;
            int expected = baking ? 60 : 30;
            Assert.That(frozen, Is.EqualTo(expected));
            var view = client.GetComponent<GridClientComponent>();
            Assert.That(baking ? view.BakeProcesses[station.BakeProcessId].RequiredTicks : view.Processes[Grid.InputProcess(raw).Id].RequiredTicks, Is.EqualTo(expected));
            Assert.That(baking ? station.BakeProcessId : Grid.InputProcess(raw).Id, Is.Not.EqualTo(oldId));
            Assert.That(baking ? oldBake.RequiredTicks : oldManual.RequiredTicks, Is.EqualTo(originalTicks));
            GridManualProcess newManual = baking ? null : Grid.InputProcess(raw);
            LevelDo(LevelControlAction.Pause);
            int newProgress = baking ? Grid.BakeProcess(station.BakeProcessId).ProgressTicks : Grid.InputProcess(raw).ProgressTicks;
            AdvanceTicks(12); Assert.That(baking ? Grid.BakeProcess(station.BakeProcessId).ProgressTicks : Grid.InputProcess(raw).ProgressTicks, Is.EqualTo(newProgress));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(expected + 1); PumpMessages(8);
            Assert.That(baking ? Grid.Item(station.ItemId).DefinitionId : Grid.Item(newManual.OutputItemId).DefinitionId,
                Is.EqualTo(baking ? GridKitchenRules.Toast : GridKitchenRules.TomatoChunks));
        }

        [TestCase(4)]
        [TestCase(5)]
        public void BuffVarietyRealKcpRemoteCannotChooseHostSelectionAndFrozenWorkProjectIdentically(int choice)
        {
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(1), pool: new[] { choice }));
            StartHost(); ClientSessionComponent local = host.Client, remote = CoopRemote();
            var view = remote.GetComponent<GridClientComponent>();
            using (var denied = (ChooseBuffResponse)Finish(remote.CallProduct(view.CreateBuffChoice(choice, Buff.DrawVersion)), 500))
                Assert.That(denied.Message, Is.EqualTo("HostControlOnly"));
            Assert.That(Buff.SelectedId, Is.Zero); BuffDo(choice);
            if (choice == 5) DishBegin(local);
            else
            {
                var source = PrepStation("备料台"); var target = PrepStation("切配台"); long raw = source.ItemId;
                PrepDo(local, GridAction.TakeCounter, source.Id, raw); PrepDo(local, GridAction.PlaceCounter, target.Id, raw);
                PrepDo(local, GridAction.StartStationWork, target.Id, raw, raw);
            }
            PumpUntil(() => view.SelectedBuffId == choice && (choice == 5 ? view.BakeProcesses.Count > 0 : view.Processes.Count > 0),
                "Remote actual frozen work projection", 300, advanceTime: true);
            var localView = local.GetComponent<GridClientComponent>();
            Assert.That(view.SelectedBuffId, Is.EqualTo(localView.SelectedBuffId));
            int actual = choice == 5 ? view.BakeProcesses.Values.Single().RequiredTicks : view.Processes.Values.Single().RequiredTicks;
            Assert.That(actual, Is.EqualTo(choice == 5 ? 60 : 30));
            Assert.That(view.WorldId, Is.EqualTo(localView.WorldId)); Assert.That(view.LevelId, Is.EqualTo(localView.LevelId));
        }
    }
}
