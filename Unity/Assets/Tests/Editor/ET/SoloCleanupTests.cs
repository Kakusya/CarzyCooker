using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private GridStation CleanupFacility(GridStationKind kind) => Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Kind == kind);

        private void CleanupWait(ClientSessionComponent client, long command, string rejection = null)
        {
            var actor = Grid.Actor(client.MemberId);
            for (int i = 0; i < 180 && (actor.LastResultCommandId != command || actor.LastResultStatus == GridResultStatus.PendingApproach); ++i)
                AdvanceTicks(1);
            PumpMessages(8);
            Assert.That(actor.LastResultCommandId, Is.EqualTo(command));
            Assert.That(actor.LastResultStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), actor.LastResultReason);
            Assert.That(actor.LastResultReason, Is.EqualTo(rejection));
        }

        private long CleanupDo(ClientSessionComponent client, GridAction action, GridStation facility, string rejection = null)
        {
            PumpMessages(8); // 等已提交快照投递，不推进业务 Tick 或改写物品来制造预期初态。
            var projection = client.GetComponent<GridClientComponent>();
            Assert.That(projection.Actors[client.MemberId].HandItemId, Is.EqualTo(Grid.Actor(client.MemberId).HandItemId));
            var request = projection.CreateGridCommand(projection.CleanupIntent(facility.Id, action));
            long command = request.CommandId;
            using (var receipt = SoloReply(client, request)) Assert.That(receipt.Error, Is.Zero, receipt.Message);
            CleanupWait(client, command, rejection);
            return command;
        }

        private GridCommandRequest CleanupReplay(ClientSessionComponent client, GridIntent intent, long command)
        {
            var request = GridRequest(client, command, intent.Action, intent.Cell, intent.TargetId, intent.ExpectedItemId);
            request.ExpectedTargetItemId = intent.ExpectedTargetItemId; request.ExpectedProcessId = intent.ExpectedProcessId;
            request.ExpectedContentIds.AddRange(intent.ExpectedContentIds);
            request.ExpectedRemainingPortions = intent.ExpectedRemainingPortions; request.ExpectedLayoutVersion = intent.ExpectedLayoutVersion;
            return request;
        }

        [Test]
        public void SoloSinkWaterRealApproachFillDrainAndRefillKeepSameBowl()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            var sink = CleanupFacility(GridStationKind.Wash); var counter = PrepStation("碗台"); long bowlId = counter.ItemId;
            PrepDo(client, GridAction.TakeCounter, counter.Id, bowlId);
            PrepDo(client, GridAction.Move, cell: new GridCell(1, 16));
            var projection = client.GetComponent<GridClientComponent>();
            var request = projection.CreateGridCommand(projection.CleanupIntent(sink.Id, GridAction.FillWater)); long command = request.CommandId;
            using (var receipt = SoloReply(client, request)) Assert.That(receipt.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(Grid.Item(bowlId).Contents, Is.Empty, "Receipt cannot create water");
            AdvanceTicks(1); PumpMessages(5);
            Assert.That(Grid.Item(bowlId).Contents, Is.Empty, "Starting the route cannot create water");
            CleanupWait(client, command);
            var bowl = Grid.Item(bowlId); long waterId = bowl.Contents.Single();
            Assert.That(Grid.AtStation(Grid.Actor(client.MemberId), sink), Is.True);
            Assert.That(Grid.Item(waterId).DefinitionId, Is.EqualTo(GridKitchenRules.Water));
            Assert.That(Grid.Item(waterId).Parent, Is.SameAs(Grid));
            Assert.That(Grid.Item(waterId).HolderId, Is.EqualTo(bowlId));
            Assert.That(Grid.Item(waterId).Location, Is.EqualTo(GridItemLocation.Container));
            Assert.That(Grid.Items.Count, Is.EqualTo(8));
            Assert.That(projection.Items[waterId].Form, Is.EqualTo(GridMaterialForm.Liquid));
            Assert.That(projection.Stations[sink.Id].WaterDefinitionId, Is.EqualTo(GridKitchenRules.Water));
            CleanupDo(client, GridAction.FillWater, sink, "ContainerFull");
            Assert.That(bowl.Contents, Is.EqualTo(new[] { waterId }));
            CleanupDo(client, GridAction.DrainLiquid, sink);
            Assert.That(Grid.Item(waterId), Is.Null); Assert.That(bowl.Contents, Is.Empty);
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(bowlId));
            Assert.That(bowl.Hygiene, Is.EqualTo(ItemHygiene.Clean));
            Assert.That(Grid.Items.Count, Is.EqualTo(7));
            CleanupDo(client, GridAction.FillWater, sink);
            Assert.That(bowl.Contents.Single(), Is.Not.EqualTo(waterId));
        }

        [TestCase("dirty")]
        [TestCase("plate")]
        public void SoloSinkWaterRejectsDirtyOrIncompatibleVesselWithoutCreatingWater(string fault)
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            var counter = PrepStation(fault == "plate" ? "盘台" : "碗台"); long vesselId = counter.ItemId;
            if (fault == "dirty") Grid.Item(vesselId).Hygiene = ItemHygiene.Dirty; // 合法脏餐具初态，不能靠接水洗净。
            PrepDo(client, GridAction.TakeCounter, counter.Id, vesselId);
            int stock = Grid.Items.Count;
            CleanupDo(client, GridAction.FillWater, CleanupFacility(GridStationKind.Wash), fault == "dirty" ? "DirtyUtensil" : "IncompatibleContainer");
            Assert.That(Grid.Items.Count, Is.EqualTo(stock)); Assert.That(Grid.Item(vesselId).Contents, Is.Empty);
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(vesselId));
            Assert.That(Grid.Item(vesselId).Hygiene, Is.EqualTo(fault == "dirty" ? ItemHygiene.Dirty : ItemHygiene.Clean));
        }

        [Test]
        public void SoloCleanupFacilitiesPlateContentsAndBareSolidRequireTrashAndKeepVessel()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            var oven = PrepStation("烤箱"); long toast = DishBake(client); var plateCounter = PrepStation("盘台"); long plateId = plateCounter.ItemId;
            long bakeId = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single(value => value.OutputItemId == toast).Id;
            PrepDo(client, GridAction.TakeCounter, oven.Id, toast);
            PrepDo(client, GridAction.AddToContainer, plateCounter.Id, toast, plateId);
            PrepDo(client, GridAction.TakeCounter, plateCounter.Id, plateId);
            Grid.Item(plateId).Hygiene = ItemHygiene.Dirty; // 倾倒只处理内容，不洗净同一盘。
            AdvanceTicks(1); PumpMessages(5);
            CleanupDo(client, GridAction.DrainLiquid, CleanupFacility(GridStationKind.Wash), "RequiresLiquid");
            Assert.That(Grid.Item(plateId).Contents, Is.EqualTo(new[] { toast }));
            CleanupDo(client, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash));
            Assert.That(Grid.Item(toast), Is.Null); Assert.That(Grid.Item(plateId).Contents, Is.Empty);
            Assert.That(Grid.BakeProcess(bakeId), Is.Null, "Deleting baked output retires its completed process reference");
            Assert.That(Grid.Item(plateId).Hygiene, Is.EqualTo(ItemHygiene.Dirty));
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(plateId));
            PrepDo(client, GridAction.PlaceCounter, plateCounter.Id, plateId);
            var tomatoCounter = PrepStation("备料台"); long tomato = tomatoCounter.ItemId;
            PrepDo(client, GridAction.TakeCounter, tomatoCounter.Id, tomato);
            CleanupDo(client, GridAction.DiscardSolid, PrepStation("交接台"), "RequiresTrash");
            Assert.That(Grid.Item(tomato), Is.Not.Null);
            CleanupDo(client, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash));
            Assert.That(Grid.Item(tomato), Is.Null); Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.Zero);
            Assert.That(CurrentLevel.Score, Is.Zero);
        }

        [Test]
        public void SoloCleanupFacilitiesRemainingSoupUsesOutputFormAndConsumesOnlyThatBatch()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long potId = SoupFillPot(client); var batch = SoupBegin(client, potId); long batchId = batch.Id;
            var inputs = batch.InputItemIds.ToArray(); AdvanceTicks(batch.RequiredTicks); PumpMessages(5);
            var bowlCounter = PrepStation("碗台"); long bowlId = bowlCounter.ItemId; var stove = PrepStation("灶台");
            PrepDo(client, GridAction.TakeCounter, bowlCounter.Id, bowlId);
            PrepDo(client, GridAction.PortionSoup, stove.Id, bowlId, potId, batchId);
            long servedSoup = Grid.Item(bowlId).Contents.Single();
            PrepDo(client, GridAction.PlaceCounter, bowlCounter.Id, bowlId);
            PrepDo(client, GridAction.TakeCounter, stove.Id, potId);
            Grid.Item(potId).Hygiene = ItemHygiene.Dirty; AdvanceTicks(1); PumpMessages(5);
            CleanupDo(client, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash), "RequiresSolid");
            Assert.That(batch.RemainingPortions, Is.EqualTo(1)); Assert.That(Grid.Item(potId).Contents, Is.EqualTo(inputs));
            PumpMessages(8);
            var projection = client.GetComponent<GridClientComponent>(); var sink = CleanupFacility(GridStationKind.Wash);
            var request = projection.CreateGridCommand(projection.CleanupIntent(sink.Id, GridAction.DrainLiquid)); long command = request.CommandId;
            using (var receipt = SoloReply(client, request)) Assert.That(receipt.Error, Is.Zero);
            Assert.That(Grid.Item(potId).Contents, Is.EqualTo(inputs), "Receipt cannot consume the remaining batch");
            AdvanceTicks(1);
            Assert.That(Grid.AtStation(Grid.Actor(client.MemberId), sink), Is.False);
            Assert.That(Grid.Item(potId).Contents, Is.EqualTo(inputs), "Approach cannot consume the remaining batch");
            CleanupWait(client, command);
            Assert.That(Grid.AutoProcess(batchId), Is.Null); Assert.That(batch.IsDisposed, Is.True);
            Assert.That(inputs.All(id => Grid.Item(id) == null), Is.True);
            Assert.That(Grid.Processes.Count, Is.Zero, "Consumed input/output references must retire together");
            var pot = Grid.Item(potId); Assert.That(pot.Contents, Is.Empty); Assert.That(pot.AutoProcessId, Is.Zero);
            Assert.That(pot.Hygiene, Is.EqualTo(ItemHygiene.Dirty)); Assert.That(pot.Location, Is.EqualTo(GridItemLocation.Hand));
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(potId));
            Assert.That(Grid.Item(bowlId).Contents, Is.EqualTo(new[] { servedSoup }));
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == GridKitchenRules.Soup), Is.EqualTo(1));
        }

        [Test]
        public void SoloCleanupFacilitiesRawMixtureRejectsBothFacilitiesWithoutSeparatingInputs()
        {
            StartSolo(); ClientSessionComponent client = host.Client; long potId = SoupFillPot(client); var inputs = Grid.Item(potId).Contents.ToArray();
            PrepDo(client, GridAction.TakeCounter, PrepStation("灶台").Id, potId);
            CleanupDo(client, GridAction.DrainLiquid, CleanupFacility(GridStationKind.Wash), "MixedContents");
            CleanupDo(client, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash), "MixedContents");
            Assert.That(Grid.Item(potId).Contents, Is.EqualTo(inputs)); Assert.That(inputs.All(id => Grid.Item(id).HolderId == potId), Is.True);
            Assert.That(Grid.Item(potId).AutoProcessId, Is.Zero);
        }

        [TestCase("碗台")]
        [TestCase("盘台")]
        [TestCase("灶台")]
        public void SoloCleanupLocksReusableEmptyVesselsCannotBeDiscarded(string name)
        {
            StartSolo(); ClientSessionComponent client = host.Client; var counter = PrepStation(name); long id = counter.ItemId;
            PrepDo(client, GridAction.TakeCounter, counter.Id, id);
            CleanupDo(client, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash), "ProtectedContainer");
            Assert.That(Grid.Item(id), Is.Not.Null); Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(id));
        }

        [Test]
        public void SoloCleanupLocksNestedVesselIsNeverDestroyedWithItsOuterContainer()
        {
            StartSolo(); ClientSessionComponent client = host.Client; long bowlId = PrepStation("碗台").ItemId;
            PrepDo(client, GridAction.TakeCounter, PrepStation("碗台").Id, bowlId);
            var innerCounter = PrepStation("第二碗台"); long innerId = innerCounter.ItemId;
            // 非法嵌套关系故障注入；它不能使消费者递归销毁另一只有限餐具。
            innerCounter.ItemId = 0; var inner = Grid.Item(innerId); inner.Location = GridItemLocation.Container; inner.HolderId = bowlId;
            Grid.Item(bowlId).Contents.Add(innerId); Grid.Dirty = true; AdvanceTicks(1); PumpMessages(8);
            CleanupDo(client, GridAction.DrainLiquid, CleanupFacility(GridStationKind.Wash), "NestedContainer");
            CleanupDo(client, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash), "NestedContainer");
            Assert.That(Grid.Item(innerId), Is.SameAs(inner)); Assert.That(Grid.Item(bowlId).Contents, Is.EqualTo(new[] { innerId }));
            Assert.That(Grid.Items.Count, Is.EqualTo(7));
        }

        [Test]
        public void SoloCleanupLocksWorkingEggRejectsBeforeWalkingAndStoppedPrepRetiresOnTrash()
        {
            StartSolo(); ClientSessionComponent client = host.Client; long bowlId = PrepEggBowl(client), eggId = Grid.Item(bowlId).Contents.Single();
            PrepDo(client, GridAction.StartHeldWork, bowlId, eggId, bowlId); var work = Grid.InputProcess(eggId); long workId = work.Id;
            var before = Grid.Actor(client.MemberId).Cell;
            CleanupDo(client, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash), "WorkBusy");
            Assert.That(Grid.Actor(client.MemberId).Cell, Is.EqualTo(before)); Assert.That(work.State, Is.EqualTo(GridWorkState.Working));
            Assert.That(Grid.Item(bowlId).Contents, Is.EqualTo(new[] { eggId }));
            PrepDo(client, GridAction.StopWork, bowlId, eggId, bowlId, workId);
            CleanupDo(client, GridAction.DiscardSolid, CleanupFacility(GridStationKind.Trash));
            Assert.That(Grid.Item(eggId), Is.Null); Assert.That(Grid.Process(workId), Is.Null);
            Assert.That(Grid.Actor(client.MemberId).WorkId, Is.Zero); Assert.That(Grid.Item(bowlId).Contents, Is.Empty);
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(bowlId));
        }

        [Test]
        public void SoloCleanupLocksCookingPotCannotDrainAndOvenInputCannotBeTakenToTrash()
        {
            StartSolo(); ClientSessionComponent client = host.Client; long potId = SoupFillPot(client); var batch = SoupBegin(client, potId);
            PrepDo(client, GridAction.TakeCounter, PrepStation("灶台").Id, potId);
            var inputs = batch.InputItemIds.ToArray(); CleanupDo(client, GridAction.DrainLiquid, CleanupFacility(GridStationKind.Wash), "ProcessLocked");
            Assert.That(Grid.Item(potId).Contents, Is.EqualTo(inputs)); Assert.That(batch.State, Is.EqualTo(GridAutoState.Cooking));
            PrepDo(client, GridAction.PlaceCounter, PrepStation("灶台").Id, potId);
            var bake = DishBegin(client); var oven = PrepStation("烤箱"); long bread = bake.InputItemId;
            PrepDo(client, GridAction.TakeCounter, oven.Id, bread, rejection: "WorkBusy");
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.Zero); Assert.That(oven.ItemId, Is.EqualTo(bread));
            Assert.That(Grid.Item(bread).BakeProcessId, Is.EqualTo(bake.Id)); Assert.That(bake.State, Is.EqualTo(GridBakeState.Baking));
        }

        [Test]
        public void SoloCleanupLocksSharedSinkWaitsForRealWashAndKeepsOriginalCleanPoolItem()
        {
            StartSolo(); ClientSessionComponent client = host.Client; LevelDo(LevelControlAction.Start);
            long heldBowl = PrepStation("第二碗台").ItemId;
            PrepDo(client, GridAction.TakeCounter, PrepStation("第二碗台").Id, heldBowl);
            var clear = InitialDirtyTable(); long dirtyBowl = clear.ItemId;
            // 合法在途洗碗初态由既有目标 fixture 构造；完成效果仍由真实业务 Tick 提交。
            InitialTargetReady(clear); AdvanceTicks(1); PumpMessages(5);
            var wash = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Wash);
            InitialTargetReady(wash);
            CleanupDo(client, GridAction.FillWater, CleanupFacility(GridStationKind.Wash), "StationBusy");
            Assert.That(wash.State, Is.EqualTo(FrontWorkState.Completed));
            Assert.That(Grid.Item(dirtyBowl).Hygiene, Is.EqualTo(ItemHygiene.Clean));
            Assert.That(Grid.Item(dirtyBowl).Location, Is.EqualTo(GridItemLocation.CleanPool));
            Assert.That(Grid.Item(heldBowl).Contents, Is.Empty);
            CleanupDo(client, GridAction.FillWater, CleanupFacility(GridStationKind.Wash));
            Assert.That(Grid.Item(heldBowl).Contents.Count, Is.EqualTo(1));
            Assert.That(Grid.Item(dirtyBowl).Location, Is.EqualTo(GridItemLocation.CleanPool));
        }

        [Test]
        public void SoloCleanupMessagesLatestReplayCopiesContentAndRejectsContextPauseAndMissingFacility()
        {
            StartSolo(); ClientSessionComponent client = host.Client; var projection = client.GetComponent<GridClientComponent>();
            var sink = CleanupFacility(GridStationKind.Wash); long bowlId = PrepStation("碗台").ItemId;
            PrepDo(client, GridAction.TakeCounter, PrepStation("碗台").Id, bowlId); CleanupDo(client, GridAction.FillWater, sink);
            long water = Grid.Item(bowlId).Contents.Single(); var frozen = projection.CleanupIntent(sink.Id, GridAction.DrainLiquid);
            var request = projection.CreateGridCommand(frozen); long command = request.CommandId;
            using (var receipt = SoloReply(client, request)) Assert.That(receipt.Error, Is.Zero);
            Assert.That(frozen.ExpectedContentIds, Is.EqualTo(new[] { water }));
            Assert.That(Grid.Actor(client.MemberId).LastReceivedIntent.ExpectedContentIds, Is.EqualTo(new[] { water }), "The original pooled request is already retired");
            CleanupWait(client, command);
            using (var replay = SoloReply(client, CleanupReplay(client, frozen, command))) Assert.That(replay.Status, Is.EqualTo((int)GridResultStatus.Executed));
            var conflict = CleanupReplay(client, frozen, command); conflict.ExpectedContentIds.Clear();
            using (var reply = SoloReply(client, conflict)) Assert.That(reply.Error, Is.Not.Zero);
            var missing = projection.CreateGridCommand(GridAction.DrainLiquid, expected: bowlId, expectedLayoutVersion: projection.LayoutVersion);
            using (var reply = SoloReply(client, missing)) Assert.That(reply.Message, Is.EqualTo("UnknownCounter"));
            var wrongBinding = projection.CreateGridCommand(projection.CleanupIntent(sink.Id, GridAction.FillWater)); ++wrongBinding.BindingGeneration;
            using (var reply = SoloReply(client, wrongBinding)) Assert.That(reply.Message, Is.EqualTo("EntryClosed"));
            var wrongLevel = projection.CreateGridCommand(projection.CleanupIntent(sink.Id, GridAction.FillWater)); ++wrongLevel.LevelGeneration;
            using (var reply = SoloReply(client, wrongLevel)) Assert.That(reply.Message, Is.EqualTo("WrongLevel"));
            LevelDo(LevelControlAction.Pause);
            using (var reply = SoloReply(client, projection.CreateGridCommand(projection.CleanupIntent(sink.Id, GridAction.FillWater)))) Assert.That(reply.Message, Is.EqualTo("Paused"));
            AdvanceTicks(15); Assert.That(Grid.Item(bowlId).Contents, Is.Empty); Assert.That(Grid.Item(water), Is.Null);
            LevelDo(LevelControlAction.Resume);
            Assert.That(Grid.Items.Count, Is.EqualTo(7));
            long oldLevel = CurrentLevel.Id, oldGeneration = CurrentLevel.Generation;
            LevelDo(LevelControlAction.Start); AdvanceTicks(CurrentLevel.OperatingTicks + 2); PumpMessages(8);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed)); LevelDo(LevelControlAction.Continue);
            var oldReplay = CleanupReplay(client, frozen, command); oldReplay.LevelId = oldLevel; oldReplay.LevelGeneration = oldGeneration;
            using (var reply = SoloReply(client, oldReplay)) Assert.That(reply.Message, Is.EqualTo("WrongLevel"));
            Assert.That(CurrentLevel.Id, Is.Not.EqualTo(oldLevel)); Assert.That(Grid.Items.Count, Is.EqualTo(7));
            Assert.That(Grid.Item(bowlId).Location, Is.EqualTo(GridItemLocation.Ground));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SoloCleanupMessagesApproachRechecksContentsAndFacilityLayout(bool changeLayout)
        {
            StartSolo(); ClientSessionComponent client = host.Client; var projection = client.GetComponent<GridClientComponent>();
            long bowlId = PrepStation("碗台").ItemId; PrepDo(client, GridAction.TakeCounter, PrepStation("碗台").Id, bowlId);
            PrepDo(client, GridAction.Move, cell: new GridCell(1, 16));
            var request = projection.CreateGridCommand(projection.CleanupIntent(CleanupFacility(GridStationKind.Wash).Id, GridAction.FillWater)); long command = request.CommandId;
            using (var receipt = SoloReply(client, request)) Assert.That(receipt.Error, Is.Zero);
            AdvanceTicks(1); Assert.That(Grid.Actor(client.MemberId).LastResultStatus, Is.EqualTo(GridResultStatus.PendingApproach));
            long injected = 0;
            // 故障注入模拟在途源或布局被替换；业务拒绝仍必须经原请求、自动到位和 Tick。
            if (changeLayout) ++Grid.LayoutVersion;
            else
            {
                var water = Grid.AddChild<GridItem>(); water.DefinitionId = GridKitchenRules.Water; water.Name = "水";
                water.Location = GridItemLocation.Container; water.HolderId = bowlId; water.Hygiene = ItemHygiene.Clean;
                Grid.Items.Add(water.Id, water); Grid.Item(bowlId).Contents.Add(water.Id); injected = water.Id;
            }
            Grid.Dirty = true; CleanupWait(client, command, changeLayout ? "LayoutChanged" : "ContentsChanged");
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(bowlId));
            Assert.That(Grid.Item(bowlId).Contents, Is.EqualTo(changeLayout ? System.Array.Empty<long>() : new[] { injected }));
            Assert.That(Grid.Items.Count, Is.EqualTo(changeLayout ? 7 : 8));
        }
    }
}
