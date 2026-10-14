using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(GridAutoProcess))]
    public sealed partial class ProductSessionTests
    {
        private long SoupChop(ClientSessionComponent client)
        {
            GridStation source = PrepStation("备料台"), chopping = PrepStation("切配台");
            long tomato = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, tomato);
            PrepDo(client, GridAction.PlaceCounter, chopping.Id, tomato);
            PrepDo(client, GridAction.StartStationWork, chopping.Id, tomato, tomato);
            GridManualProcess work = Grid.InputProcess(tomato);
            AdvanceTicks(work.RequiredTicks + 1); PumpMessages(5);
            Assert.That(work.State, Is.EqualTo(GridWorkState.Completed));
            return work.OutputItemId;
        }

        private long SoupEggLiquid(ClientSessionComponent client)
        {
            long bowl = PrepEggBowl(client), egg = Grid.Item(bowl).Contents.Single();
            PrepDo(client, GridAction.StartHeldWork, bowl, egg, bowl);
            GridManualProcess work = Grid.InputProcess(egg);
            AdvanceTicks(work.RequiredTicks + 1); PumpMessages(5);
            Assert.That(work.State, Is.EqualTo(GridWorkState.Completed));
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { work.OutputItemId }));
            return bowl;
        }

        private long SoupFillPot(ClientSessionComponent client, bool eggFirst = false)
        {
            GridStation stove = PrepStation("灶台"), chopping = PrepStation("切配台"), bowlCounter = PrepStation("碗台");
            long pot = stove.ItemId, chopped = SoupChop(client);
            if (!eggFirst)
            {
                PrepDo(client, GridAction.TakeCounter, chopping.Id, chopped);
                PrepDo(client, GridAction.AddToContainer, stove.Id, chopped, pot);
            }
            long bowl = SoupEggLiquid(client), liquid = Grid.Item(bowl).Contents.Single();
            PrepDo(client, GridAction.PourContents, stove.Id, bowl, pot);
            Assert.That(Grid.Item(bowl).Contents, Is.Empty);
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(bowl));
            Assert.That(Grid.Item(liquid).HolderId, Is.EqualTo(pot));
            Assert.That(Grid.Item(liquid).Location, Is.EqualTo(GridItemLocation.Container));
            Assert.That(Grid.Processes.Values.Select(value => (GridManualProcess)value).Any(work => work.TargetItemId == bowl), Is.False);
            PrepDo(client, GridAction.PlaceCounter, bowlCounter.Id, bowl);
            if (eggFirst)
            {
                PrepDo(client, GridAction.TakeCounter, chopping.Id, chopped);
                PrepDo(client, GridAction.AddToContainer, stove.Id, chopped, pot);
            }
            Assert.That(Grid.Item(pot).Contents.Select(id => Grid.Item(id).DefinitionId).OrderBy(id => id), Is.EqualTo(new[] { GridKitchenRules.TomatoChunks, GridKitchenRules.EggLiquid }));
            return pot;
        }

        private GridAutoProcess SoupBegin(ClientSessionComponent client, long pot)
        {
            GridStation stove = PrepStation("灶台");
            PrepDo(client, GridAction.Move, cell: new GridCell(1, 9));
            var projection = client.GetComponent<GridClientComponent>();
            using (var reply = SoloReply(client, projection.CreateGridCommand(GridAction.StartAutoWork, target: stove.Id, expected: pot, expectedTarget: pot)))
                Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            AdvanceTicks(1); PumpMessages(5);
            GridAutoProcess process = Grid.AutoProcess(Grid.Item(pot).AutoProcessId);
            Assert.That(process, Is.Not.Null);
            Assert.That(process.ProgressTicks, Is.Zero, "The start Tick cannot also cook");
            Assert.That(process.Parent, Is.SameAs(Grid));
            Assert.That(process.PotItemId, Is.EqualTo(pot));
            Assert.That(stove.AutoProcessId, Is.EqualTo(process.Id));
            Assert.That(process.RemainingPortions, Is.Zero);
            return process;
        }

        [Test]
        public void GridSoupConfigAndActualInitialSupplies()
        {
            var items = Tables.Instance.DTGridItemConfig.DataList;
            var recipes = Tables.Instance.DTGridAutoRecipeConfig.DataList;
            GridKitchenSystem.ValidateGridSoupDefinitions(items, recipes);
            Assert.Throws<InvalidOperationException>(() => GridKitchenSystem.ValidateGridSoupDefinitions(items.Where(item => item.Id != GridKitchenRules.Pot), recipes));
            Assert.Throws<InvalidOperationException>(() => GridKitchenSystem.ValidateGridSoupDefinitions(items.Where(item => item.Id != GridKitchenRules.Soup), recipes));
            Assert.Throws<InvalidOperationException>(() => GridKitchenSystem.ValidateGridSoupDefinitions(items, recipes.Where(recipe => recipe.Id != 1)));
            StartSolo();
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == GridKitchenRules.Bowl), Is.EqualTo(2));
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == GridKitchenRules.Pot), Is.EqualTo(1));
            Assert.That(Grid.Items.Values.All(value => ((GridItem)value).Parent == Grid), Is.True);
            Assert.That(PrepStation("灶台").Kind, Is.EqualTo(GridStationKind.Stove));
            Assert.That(Grid.FindGridPath(Grid.Actor(((ClientSessionComponent)host.Client).MemberId).Cell, new GridCell(2, 14)), Is.Not.Null);
        }

        [Test]
        public void GridSoupConfigRejectsMalformedCountsAndIncompatibleOutput()
        {
            var items = Tables.Instance.DTGridItemConfig.DataList;
            for (int fault = 1; fault <= 5; ++fault)
            {
                var buffer = new Luban.ByteBuf();
                buffer.WriteInt(1); buffer.WriteInt(GridKitchenRules.Pot);
                buffer.WriteSize(2); buffer.WriteInt(GridKitchenRules.TomatoChunks);
                buffer.WriteInt(fault == 3 ? GridKitchenRules.TomatoChunks : GridKitchenRules.EggLiquid);
                buffer.WriteSize(fault == 1 ? 1 : 2);
                buffer.WriteInt(fault == 2 ? 0 : fault == 5 ? 2 : 1);
                if (fault != 1) buffer.WriteInt(1);
                buffer.WriteInt(120); buffer.WriteInt(fault == 4 ? GridKitchenRules.Tomato : GridKitchenRules.Soup);
                buffer.WriteInt(2); buffer.WriteInt(GridKitchenRules.Bowl);
                buffer.WriteInt((int)GridStationCapability.Cook); buffer.WriteBool(false); buffer.WriteInt(0);
                var malformed = new DRGridAutoRecipeConfig(buffer);
                Assert.Throws<InvalidOperationException>(() => GridKitchenSystem.ValidateGridSoupDefinitions(items, new[] { malformed }), $"Fault {fault}");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GridSoupTransferOrdersPreserveBowlAndActualMaterials(bool eggFirst)
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long pot = SoupFillPot(client, eggFirst);
            Assert.That(Grid.Item(pot).Contents.Count, Is.EqualTo(2));
            Assert.That(Grid.Items.Values.Select(value => (GridItem)value).Count(item => item.DefinitionId == GridKitchenRules.EggLiquid), Is.EqualTo(1));
            Assert.That(Grid.Items.Values.Select(value => (GridItem)value).Count(item => item.DefinitionId == GridKitchenRules.TomatoChunks), Is.EqualTo(1));
            Assert.That(Grid.AutoProcesses, Is.Empty, "Full inputs never automatically start the stove");
            GridAutoProcess process = SoupBegin(client, pot);
            Assert.That(process.InputItemIds, Is.EqualTo(Grid.Item(pot).Contents));
        }

        [Test]
        public void GridSoupCookingLocksInputsAndMovesOriginalPot()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long pot = SoupFillPot(client);
            GridStation stove = PrepStation("灶台"), handoff = PrepStation("交接台"), bowl = PrepStation("碗台");
            GridAutoProcess process = SoupBegin(client, pot);
            long batch = process.Id;
            PrepDo(client, GridAction.StartAutoWork, stove.Id, pot, pot, batch, rejection: "ProcessLocked");
            PrepDo(client, GridAction.TakeCounter, stove.Id, pot);
            Assert.That(Grid.Item(pot).AutoProcessId, Is.EqualTo(batch));
            Assert.That(stove.AutoProcessId, Is.EqualTo(batch));
            PrepDo(client, GridAction.PlaceCounter, handoff.Id, pot);
            Assert.That(Grid.AutoProcess(batch), Is.SameAs(process));
            GridItem otherPot = SoupInitialItem(GridKitchenRules.Pot, stove);
            PrepDo(client, GridAction.StartAutoWork, stove.Id, otherPot.Id, otherPot.Id, rejection: "StationBusy");
            PrepDo(client, GridAction.TakeCounter, bowl.Id, bowl.ItemId);
            Assert.That(process.State, Is.EqualTo(GridAutoState.Cooking));
            PrepDo(client, GridAction.PortionSoup, handoff.Id, Grid.Actor(client.MemberId).HandItemId, pot, batch, rejection: "NotReady");
            AdvanceTicks(process.RequiredTicks + 1); PumpMessages(5);
            Assert.That(process.State, Is.EqualTo(GridAutoState.Ready));
            Assert.That(process.RemainingPortions, Is.EqualTo(2));
            Assert.That(stove.AutoProcessId, Is.Zero);
            Assert.That(Grid.Items.Values.Select(value => (GridItem)value).Any(item => item.DefinitionId == GridKitchenRules.Soup), Is.False);
            Assert.That(Grid.Item(pot).Contents, Is.EqualTo(process.InputItemIds));
        }

        [Test]
        public void GridSoupPortionsConsumeInputsOnLastAndRejectOldBatch()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long pot = SoupFillPot(client);
            GridAutoProcess process = SoupBegin(client, pot);
            long batch = process.Id; long[] inputs = process.InputItemIds.ToArray();
            AdvanceTicks(process.RequiredTicks + 1); PumpMessages(5);
            GridStation stove = PrepStation("灶台"), first = PrepStation("碗台"), second = PrepStation("第二碗台");
            long firstBowl = first.ItemId, secondBowl = second.ItemId;
            PrepDo(client, GridAction.TakeCounter, first.Id, firstBowl);
            PrepDo(client, GridAction.PortionSoup, stove.Id, firstBowl, pot, batch);
            long firstSoup = Grid.Item(firstBowl).Contents.Single();
            Assert.That(process.RemainingPortions, Is.EqualTo(1));
            Assert.That(inputs.All(id => Grid.Item(id) != null), Is.True);
            PrepDo(client, GridAction.PortionSoup, stove.Id, firstBowl, pot, batch, rejection: "ContainerFull");
            Assert.That(process.RemainingPortions, Is.EqualTo(1));
            PrepDo(client, GridAction.PlaceCounter, first.Id, firstBowl);
            PrepDo(client, GridAction.TakeCounter, second.Id, secondBowl);
            PrepDo(client, GridAction.PortionSoup, stove.Id, secondBowl, pot, batch);
            long secondSoup = Grid.Item(secondBowl).Contents.Single();
            Assert.That(secondSoup, Is.Not.EqualTo(firstSoup));
            Assert.That(Grid.AutoProcess(batch), Is.Null);
            Assert.That(Grid.Item(pot).AutoProcessId, Is.Zero);
            Assert.That(Grid.Item(pot).Contents, Is.Empty);
            Assert.That(inputs.All(id => Grid.Item(id) == null), Is.True);
            Assert.That(stove.ItemId, Is.EqualTo(pot));
            PrepDo(client, GridAction.PortionSoup, stove.Id, secondBowl, pot, batch, rejection: "ProcessChanged");
            Assert.That(Grid.Items.Values.Select(value => (GridItem)value).Count(item => item.DefinitionId == GridKitchenRules.Soup), Is.EqualTo(2));
            var projection = client.GetComponent<GridClientComponent>();
            Assert.That(projection.AutoProcesses.ContainsKey(batch), Is.False);
            Assert.That(projection.Items[firstBowl].Contents, Is.EqualTo(new[] { firstSoup }));
            Assert.That(projection.Items[secondBowl].Contents, Is.EqualTo(new[] { secondSoup }));

            // 只补本用例的初始原料和空碗；第二批仍经实际切配、打蛋、投料和开始。
            PrepDo(client, GridAction.PlaceCounter, PrepStation("交接台").Id, secondBowl);
            PrepDo(client, GridAction.TakeCounter, first.Id, firstBowl);
            PrepDo(client, GridAction.PlaceCounter, second.Id, firstBowl);
            SoupInitialItem(GridKitchenRules.Tomato, PrepStation("备料台"));
            SoupInitialItem(GridKitchenRules.Egg, PrepStation("食材台"));
            long nextBowl = SoupInitialItem(GridKitchenRules.Bowl, first).Id;
            Assert.That(SoupFillPot(client), Is.EqualTo(pot));
            GridAutoProcess next = SoupBegin(client, pot);
            Assert.That(next.Id, Is.Not.EqualTo(batch));
            AdvanceTicks(next.RequiredTicks + 1); PumpMessages(5);
            PrepDo(client, GridAction.TakeCounter, first.Id, nextBowl);
            PrepDo(client, GridAction.PortionSoup, stove.Id, nextBowl, pot, batch, rejection: "ProcessChanged");
            Assert.That(next.RemainingPortions, Is.EqualTo(2));
            Assert.That(Grid.Item(nextBowl).Contents, Is.Empty);
            Assert.That(Grid.AutoProcess(batch), Is.Null);
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == GridKitchenRules.Soup), Is.EqualTo(2));
        }

        private GridCommandRequest SoupReplay(ClientSessionComponent client, long command, GridAction action, long station, long held, long pot, long batch = 0)
        {
            GridCommandRequest request = GridRequest(client, command, action, target: station, expected: held);
            request.ExpectedTargetItemId = pot; request.ExpectedProcessId = batch;
            return request;
        }

        [Test]
        public void GridSoupLocalReplayContextAndRecycledProjection()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long pot = SoupFillPot(client);
            GridStation stove = PrepStation("灶台"), bowlStation = PrepStation("碗台");
            var projection = client.GetComponent<GridClientComponent>();
            PrepDo(client, GridAction.Move, cell: new GridCell(1, 9));
            var start = projection.CreateGridCommand(GridAction.StartAutoWork, target: stove.Id, expected: pot, expectedTarget: pot);
            long startCommand = start.CommandId;
            using (var reply = SoloReply(client, start)) Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            using (var replay = SoloReply(client, SoupReplay(client, startCommand, GridAction.StartAutoWork, stove.Id, pot, pot)))
                Assert.That(replay.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(Grid.Incoming.Count, Is.EqualTo(1));
            AdvanceTicks(1); PumpMessages(5);
            GridAutoProcess process = Grid.AutoProcess(Grid.Item(pot).AutoProcessId);
            long batch = process.Id;
            using (var replay = SoloReply(client, SoupReplay(client, startCommand, GridAction.StartAutoWork, stove.Id, pot, pot)))
                Assert.That(replay.Status, Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(Grid.AutoProcesses.Count, Is.EqualTo(1));
            Assert.That(process.ProgressTicks, Is.Zero);
            var wrongWorld = SoupReplay(client, startCommand + 1, GridAction.StartAutoWork, stove.Id, pot, pot);
            wrongWorld.WorldGeneration++;
            using (var response = SoloReply(client, wrongWorld)) Assert.That(response.Message, Is.EqualTo("WrongWorld"));
            var wrongBinding = SoupReplay(client, startCommand + 1, GridAction.StartAutoWork, stove.Id, pot, pot);
            wrongBinding.BindingGeneration++;
            using (var response = SoloReply(client, wrongBinding)) Assert.That(response.Message, Is.EqualTo("EntryClosed"));
            var oldProtocol = SoupReplay(client, startCommand + 1, GridAction.StartAutoWork, stove.Id, pot, pot);
            oldProtocol.ProtocolVersion--;
            using (var response = SoloReply(client, oldProtocol)) Assert.That(response.Message, Is.EqualTo("IncompatibleProtocol"));

            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            GridWorldSnapshot snapshot = GridBroadcast.CreateSnapshot(Grid, peer);
            snapshot.Version = projection.Version + 1;
            projection.ApplyGridSnapshot(snapshot);
            snapshot.IsFromPool = true; snapshot.Dispose();
            Assert.That(projection.AutoProcesses[batch].InputItemIds, Is.EqualTo(process.InputItemIds));
            Assert.That(projection.Items[pot].Contents, Is.EqualTo(process.InputItemIds));
            Assert.That(projection.Items[pot].AutoProcessId, Is.EqualTo(batch));
            Assert.That(projection.Stations[stove.Id].AutoProcessId, Is.EqualTo(batch));
            using (var stale = GridBroadcast.CreateSnapshot(Grid, peer))
            {
                stale.Version = projection.Version - 1; stale.AutoProcesses.Clear();
                projection.ApplyGridSnapshot(stale);
                Assert.That(projection.AutoProcesses.ContainsKey(batch), Is.True);
                stale.Version = projection.Version + 1; stale.WorldGeneration++;
                projection.ApplyGridSnapshot(stale);
                Assert.That(projection.AutoProcesses.ContainsKey(batch), Is.True);
            }
            AdvanceTicks(process.RequiredTicks + 1); PumpMessages(5);
            long bowl = bowlStation.ItemId;
            PrepDo(client, GridAction.TakeCounter, bowlStation.Id, bowl);
            PrepDo(client, GridAction.Move, cell: new GridCell(1, 9));
            var serve = projection.CreateGridCommand(GridAction.PortionSoup, target: stove.Id, expected: bowl, expectedTarget: pot, process: batch);
            long serveCommand = serve.CommandId;
            using (var response = SoloReply(client, serve)) Assert.That(response.Error, Is.Zero);
            using (var replay = SoloReply(client, SoupReplay(client, serveCommand, GridAction.PortionSoup, stove.Id, bowl, pot, batch)))
                Assert.That(replay.Status, Is.EqualTo((int)GridResultStatus.Accepted));
            AdvanceTicks(1); PumpMessages(5);
            long soup = Grid.Item(bowl).Contents.Single();
            using (var replay = SoloReply(client, SoupReplay(client, serveCommand, GridAction.PortionSoup, stove.Id, bowl, pot, batch)))
                Assert.That(replay.Status, Is.EqualTo((int)GridResultStatus.Executed));
            var conflict = SoupReplay(client, serveCommand, GridAction.PortionSoup, stove.Id, bowl, pot, batch + 1);
            using (var response = SoloReply(client, conflict)) Assert.That(response.Message, Is.EqualTo("CommandConflict"));
            Assert.That(process.RemainingPortions, Is.EqualTo(1));
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { soup }));
            Assert.That(projection.AutoProcesses[batch].RemainingPortions, Is.EqualTo(1));
        }

        [Test]
        public void GridSoupTransferFullPotAndCookingLocksKeepMaterial()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            long pot = SoupFillPot(client);
            GridStation stove = PrepStation("灶台"), handoff = PrepStation("交接台"), bowlStation = PrepStation("碗台");
            long[] inputs = Grid.Item(pot).Contents.ToArray();
            PrepDo(client, GridAction.TakeCounter, stove.Id, pot);
            PrepDo(client, GridAction.PlaceCounter, handoff.Id, pot);
            PrepDo(client, GridAction.StartAutoWork, handoff.Id, pot, pot, rejection: "RequiresStove");
            Assert.That(Grid.AutoProcesses, Is.Empty);
            Assert.That(Grid.Item(pot).Contents, Is.EqualTo(inputs));
            PrepDo(client, GridAction.TakeCounter, handoff.Id, pot);
            PrepDo(client, GridAction.PlaceCounter, stove.Id, pot);
            GridStation food = PrepStation("食材台");
            long extra = SoupInitialItem(GridKitchenRules.EggLiquid, food).Id, bowl = bowlStation.ItemId;
            PrepDo(client, GridAction.TakeCounter, food.Id, extra);
            PrepDo(client, GridAction.AddToContainer, stove.Id, extra, pot, rejection: "ContainerFull");
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(extra));
            PrepDo(client, GridAction.AddToContainer, bowlStation.Id, extra, bowl);
            PrepDo(client, GridAction.TakeCounter, bowlStation.Id, bowl);
            PrepDo(client, GridAction.PourContents, stove.Id, bowl, pot, rejection: "ContainerFull");
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { extra }));
            Assert.That(Grid.Item(pot).Contents, Is.EqualTo(inputs));
            PrepDo(client, GridAction.PlaceCounter, bowlStation.Id, bowl);
            GridAutoProcess process = SoupBegin(client, pot);
            PrepDo(client, GridAction.TakeCounter, bowlStation.Id, bowl);
            PrepDo(client, GridAction.PourContents, stove.Id, bowl, pot, rejection: "ProcessLocked");
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { extra }));
            Assert.That(Grid.Item(pot).Contents, Is.EqualTo(inputs));
            PrepDo(client, GridAction.PlaceCounter, bowlStation.Id, bowl);
            GridStation second = PrepStation("第二碗台"); long empty = second.ItemId;
            PrepDo(client, GridAction.TakeCounter, second.Id, empty);
            AdvanceTicks(process.RequiredTicks + 1); PumpMessages(5);
            PrepDo(client, GridAction.PortionSoup, stove.Id, empty, pot + 1, process.Id, rejection: "TargetChanged");
            Assert.That(process.RemainingPortions, Is.EqualTo(2));
            Assert.That(Grid.Item(empty).Contents, Is.Empty);
        }

        private GridItem SoupInitialItem(int definition, GridStation station)
        {
            Assert.That(station.ItemId, Is.Zero);
            GridItem item = Grid.AddChild<GridItem>();
            item.DefinitionId = definition; item.Name = Tables.Instance.DTGridItemConfig.Get(definition).Name;
            item.Location = GridItemLocation.Counter; item.HolderId = station.Id; item.Cell = station.Cell;
            item.AutoProcessId = 0; item.Contents.Clear();
            Grid.Items.Add(item.Id, item); station.ItemId = item.Id; Grid.Dirty = true;
            AdvanceTicks(1); PumpMessages(5);
            return item;
        }

        [Test]
        public void GridSoupTransferRejectionsKeepBothSides()
        {
            StartSolo(); ClientSessionComponent client = host.Client;
            GridStation stove = PrepStation("灶台"), source = PrepStation("备料台"), food = PrepStation("食材台");
            long pot = stove.ItemId, tomato = source.ItemId;
            PrepDo(client, GridAction.StartAutoWork, stove.Id, pot, pot, rejection: "NoRecipe");
            PrepDo(client, GridAction.TakeCounter, source.Id, tomato);
            PrepDo(client, GridAction.AddToContainer, stove.Id, tomato, pot, rejection: "IncompatibleMaterial");
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(tomato));
            Assert.That(Grid.Item(pot).Contents, Is.Empty);
            PrepDo(client, GridAction.PlaceCounter, source.Id, tomato);
            long bowl = PrepEggBowl(client), egg = Grid.Item(bowl).Contents.Single();
            PrepDo(client, GridAction.PourContents, stove.Id, bowl, pot, rejection: "RequiresEggLiquidBowl");
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { egg }));
            Assert.That(Grid.Item(pot).Contents, Is.Empty);
            PrepDo(client, GridAction.StartHeldWork, bowl, egg, bowl);
            GridManualProcess work = Grid.InputProcess(egg);
            AdvanceTicks(work.RequiredTicks + 1); PumpMessages(5);
            long liquid = Grid.Item(bowl).Contents.Single();
            PrepDo(client, GridAction.PourContents, stove.Id, bowl, pot + 1, rejection: "TargetChanged");
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { liquid }));
            PrepDo(client, GridAction.PourContents, stove.Id, bowl, pot);
            PrepDo(client, GridAction.PlaceCounter, PrepStation("碗台").Id, bowl);
            GridItem duplicate = SoupInitialItem(GridKitchenRules.EggLiquid, food);
            PrepDo(client, GridAction.TakeCounter, food.Id, duplicate.Id);
            PrepDo(client, GridAction.AddToContainer, stove.Id, duplicate.Id, pot, rejection: "IncompatibleMaterial");
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(duplicate.Id));
            Assert.That(Grid.Item(pot).Contents, Is.EqualTo(new[] { liquid }));
            PrepDo(client, GridAction.PlaceCounter, food.Id, duplicate.Id);
            PrepDo(client, GridAction.StartAutoWork, stove.Id, pot, pot, rejection: "NoRecipe");
            Assert.That(Grid.AutoProcesses, Is.Empty);
        }

        [Test]
        public void GridSoupPortionLastRaceUsesRealLocalRequests()
        {
            StartSolo(); ClientSessionComponent firstClient = host.Client;
            long pot = SoupFillPot(firstClient);
            GridAutoProcess process = SoupBegin(firstClient, pot);
            long batch = process.Id;
            AdvanceTicks(process.RequiredTicks + 1); PumpMessages(5);
            GridStation stove = PrepStation("灶台"), bowl = PrepStation("碗台"), secondBowl = PrepStation("第二碗台");
            long servedBowl = bowl.ItemId;
            PrepDo(firstClient, GridAction.TakeCounter, bowl.Id, servedBowl);
            PrepDo(firstClient, GridAction.PortionSoup, stove.Id, servedBowl, pot, batch);
            PrepDo(firstClient, GridAction.PlaceCounter, bowl.Id, servedBowl);
            ClientSessionComponent secondClient = GridJoin();
            GridStation food = PrepStation("食材台");
            long extra = SoupInitialItem(GridKitchenRules.Bowl, food).Id;
            PrepDo(secondClient, GridAction.TakeCounter, food.Id, extra);
            long empty = secondBowl.ItemId;
            PrepDo(firstClient, GridAction.TakeCounter, secondBowl.Id, empty);
            PrepDo(firstClient, GridAction.Move, cell: new GridCell(1, 9));
            PrepDo(secondClient, GridAction.Move, cell: new GridCell(2, 8));
            using (var receipt = SoloReply(firstClient, firstClient.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionSoup, target: stove.Id, expected: empty, expectedTarget: pot, process: batch)))
                Assert.That(receipt.Error, Is.Zero);
            using (var receipt = SoloReply(secondClient, secondClient.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionSoup, target: stove.Id, expected: extra, expectedTarget: pot, process: batch)))
                Assert.That(receipt.Error, Is.Zero);
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(Grid.Item(empty).Contents.Count, Is.EqualTo(1));
            Assert.That(Grid.Item(extra).Contents, Is.Empty);
            Assert.That(Grid.Actor(secondClient.MemberId).LastResultStatus, Is.EqualTo(GridResultStatus.Rejected));
            Assert.That(Grid.AutoProcesses, Is.Empty);
            Assert.That(Grid.Items.Values.Select(value => (GridItem)value).Count(item => item.DefinitionId == GridKitchenRules.Soup), Is.EqualTo(2));
        }

        [Test]
        public void GridSoupCookingLocalLeaveDropsOnceAndWholeStopDisposesProcess()
        {
            StartSolo(); ClientSessionComponent first = host.Client;
            long pot = SoupFillPot(first);
            GridAutoProcess process = SoupBegin(first, pot);
            long batch = process.Id;
            GridStation stove = PrepStation("灶台");
            PrepDo(first, GridAction.TakeCounter, stove.Id, pot);
            GridCell drop = Grid.Actor(first.MemberId).Cell;
            ClientSessionComponent next = GridJoin();
            Assert.That(Finish(first.LeaveProduct()), Is.True);
            AdvanceTicks(1); PumpMessages(8);
            int progress = process.ProgressTicks;
            Assert.That(Grid.Item(pot).Location, Is.EqualTo(GridItemLocation.Ground));
            Assert.That(Grid.Item(pot).Cell, Is.EqualTo(drop));
            AdvanceTicks(3); Assert.That(process.ProgressTicks, Is.EqualTo(progress + 3));
            PrepDo(next, GridAction.Move, cell: drop);
            PrepDo(next, GridAction.PickupDrop, pot, pot);
            Assert.That(Grid.Actor(next.MemberId).HandItemId, Is.EqualTo(pot));
            Assert.That(Grid.Item(pot).AutoProcessId, Is.EqualTo(batch));
            Assert.That(Grid.AutoProcess(batch), Is.SameAs(process));
            host.StopProduct();
            Assert.That(process.IsDisposed, Is.True);
            Assert.That(GridSafeWorld(host), Is.Null);
        }
    }
}
