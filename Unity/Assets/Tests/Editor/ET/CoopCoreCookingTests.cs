using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private GridAutoProcess CoopSoup(ClientSessionComponent local, ClientSessionComponent remote)
        {
            long chunks = SoupChop(local); var chopping = PrepStation("切配台"); var handoff = PrepStation("交接台"); var stove = PrepStation("灶台");
            long pot = stove.ItemId;
            PrepDo(local, GridAction.TakeCounter, chopping.Id, chunks); PrepDo(local, GridAction.PlaceCounter, handoff.Id, chunks);
            PrepDo(remote, GridAction.TakeCounter, handoff.Id, chunks); PrepDo(remote, GridAction.AddToContainer, stove.Id, chunks, pot);
            long bowl = SoupEggLiquid(remote); PrepDo(remote, GridAction.PourContents, stove.Id, bowl, pot);
            PrepDo(remote, GridAction.PlaceCounter, PrepStation("碗台").Id, bowl);
            PrepDo(remote, GridAction.StartAutoWork, stove.Id, pot, pot);
            var batch = Grid.AutoProcess(Grid.Item(pot).AutoProcessId); Assert.That(batch, Is.Not.Null);
            Assert.That(batch.Parent, Is.SameAs(Grid)); Assert.That(batch.PotItemId, Is.EqualTo(pot));
            return batch;
        }

        [Test]
        public void CoopCoreCookingRealLocalAndKcpCompleteSoupToastAndIndependentProjection()
        {
            StartHost(); ClientSessionComponent local = host.Client; var remote = CoopRemote();
            var batch = CoopSoup(local, remote); long pot = batch.PotItemId; var stove = PrepStation("灶台");
            PrepDo(remote, GridAction.StartAutoWork, stove.Id, pot, pot, batch.Id, rejection: "ProcessLocked");
            long bowl = PrepStation("碗台").ItemId;
            PrepDo(local, GridAction.TakeCounter, PrepStation("碗台").Id, bowl);
            PrepDo(local, GridAction.PortionSoup, stove.Id, bowl, pot, batch.Id, rejection: "NotReady");
            PrepDo(remote, GridAction.TakeCounter, stove.Id, pot); PrepDo(remote, GridAction.PlaceCounter, PrepStation("交接台").Id, pot);
            Assert.That(Grid.Item(pot).AutoProcessId, Is.EqualTo(batch.Id)); Assert.That(stove.AutoProcessId, Is.EqualTo(batch.Id));
            AdvanceTicks(batch.RequiredTicks); PumpMessages(8); Assert.That(batch.State, Is.EqualTo(GridAutoState.Ready));
            PrepDo(local, GridAction.PortionSoup, PrepStation("交接台").Id, bowl, pot, batch.Id);
            Assert.That(batch.RemainingPortions, Is.EqualTo(1)); Assert.That(Grid.IdentifyDish(bowl), Is.EqualTo(1));
            PrepDo(local, GridAction.PlaceCounter, PrepStation("碗台").Id, bowl);
            var breadStation = Grid.Stations.Values.Select(value => (GridStation)value).Single(s => Grid.Item(s.ItemId)?.DefinitionId == GridKitchenRules.Bread);
            var oven = Grid.Stations.Values.Select(value => (GridStation)value).Single(s => s.Kind == GridStationKind.Oven); long bread = breadStation.ItemId;
            PrepDo(local, GridAction.TakeCounter, breadStation.Id, bread); PrepDo(local, GridAction.PlaceCounter, oven.Id, bread);
            var viewBeforeBake = remote.GetComponent<GridClientComponent>();
            var bakeRequest = viewBeforeBake.CreateGridCommand(GridAction.StartBake, target: oven.Id, expected: bread, expectedTarget: bread);
            long bakeCommand = bakeRequest.CommandId;
            using (var receipt = (GridCommandResponse)Finish(remote.CallProduct(bakeRequest), 500)) Assert.That(receipt.Error, Is.Zero);
            Assert.That(Grid.BakeProcesses, Is.Empty, "An accepted distant command must not bake before approach finishes.");
            for (int i = 0; i < 350 && (Grid.Actor(remote.MemberId).LastResultCommandId != bakeCommand || Grid.Actor(remote.MemberId).LastResultStatus != GridResultStatus.Executed); ++i) Pump();
            Assert.That(Grid.Actor(remote.MemberId).LastResultCommandId, Is.EqualTo(bakeCommand));
            Assert.That(Grid.Actor(remote.MemberId).LastResultStatus, Is.EqualTo(GridResultStatus.Executed));
            var baking = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single();
            AdvanceTicks(baking.RequiredTicks); PumpMessages(8); Assert.That(baking.State, Is.EqualTo(GridBakeState.Completed));
            PrepDo(remote, GridAction.TakeCounter, oven.Id, baking.OutputItemId);
            var plateStation = PrepStation("盘台"); long plate = plateStation.ItemId;
            PrepDo(remote, GridAction.AddToContainer, plateStation.Id, baking.OutputItemId, plate);
            Assert.That(Grid.IdentifyDish(plate), Is.EqualTo(2)); Assert.That(Grid.Item(bread), Is.Null);
            Assert.That(Grid.Item(plate).Contents, Is.EqualTo(new[] { baking.OutputItemId }));
            Pump(20); var remoteView = remote.GetComponent<GridClientComponent>(); var localView = local.GetComponent<GridClientComponent>();
            Assert.That(remoteView.WorldId, Is.EqualTo(localView.WorldId)); Assert.That(remoteView.LevelId, Is.EqualTo(localView.LevelId));
            Assert.That(remoteView.Items[plate].Contents, Is.EqualTo(localView.Items[plate].Contents));
            Assert.That(remoteView.AutoProcesses[batch.Id].RemainingPortions, Is.EqualTo(1)); Assert.That(remoteView.BakeProcesses[baking.Id].State, Is.EqualTo(GridBakeState.Completed));
            Assert.That(remoteView.CanControlRestaurant, Is.False); Assert.That(localView.CanControlRestaurant, Is.True);
        }

        [Test]
        public void CoopCoreCookingRepeatedRemoteStartAndLastPortionRaceConsumeOnlyOnce()
        {
            StartHost(); ClientSessionComponent local = host.Client; var remote = CoopRemote(); var batch = CoopSoup(local, remote);
            long pot = batch.PotItemId; var stove = PrepStation("灶台"); var remoteView = remote.GetComponent<GridClientComponent>();
            long last = remoteView.LastCommandId;
            var replay = SoupReplay(remote, last, GridAction.StartAutoWork, stove.Id, pot, pot);
            using (var response = (GridCommandResponse)Finish(remote.CallProduct(replay), 500)) Assert.That(response.Status, Is.EqualTo((int)GridResultStatus.Executed));
            Assert.That(Grid.AutoProcesses.Count, Is.EqualTo(1)); Assert.That(batch.RemainingPortions, Is.Zero);
            AdvanceTicks(batch.RequiredTicks); PumpMessages(8);
            long firstBowl = PrepStation("碗台").ItemId;
            PrepDo(local, GridAction.TakeCounter, PrepStation("碗台").Id, firstBowl);
            PrepDo(local, GridAction.PortionSoup, stove.Id, firstBowl, pot, batch.Id);
            PrepDo(local, GridAction.PlaceCounter, PrepStation("碗台").Id, firstBowl);
            long extraBowl = SoupInitialItem(GridKitchenRules.Bowl, PrepStation("食材台")).Id;
            PrepDo(local, GridAction.TakeCounter, PrepStation("食材台").Id, extraBowl);
            var spare = Grid.Stations.Values.Select(value => (GridStation)value).Single(s => Grid.Item(s.ItemId)?.DefinitionId == GridKitchenRules.Bowl && Grid.Item(s.ItemId).Contents.Count == 0);
            long remoteBowl = spare.ItemId; PrepDo(remote, GridAction.TakeCounter, spare.Id, remoteBowl);
            PrepDo(local, GridAction.Move, cell: new GridCell(1, 9)); PrepDo(remote, GridAction.Move, cell: new GridCell(1, 9));
            var localRequest = local.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.PortionSoup, target: stove.Id, expected: extraBowl, expectedTarget: pot, process: batch.Id);
            var remoteRequest = remoteView.CreateGridCommand(GridAction.PortionSoup, target: stove.Id, expected: remoteBowl, expectedTarget: pot, process: batch.Id);
            var duplicate = CopyReplayRequest(remoteRequest); long localCommand = localRequest.CommandId, remoteCommand = remoteRequest.CommandId;
            var first = local.CallProduct(localRequest); var second = remote.CallProduct(remoteRequest);
            using (var response = (GridCommandResponse)Finish(first, 500)) Assert.That(response.Error, Is.Zero);
            using (var response = (GridCommandResponse)Finish(second, 500)) Assert.That(response.Error, Is.Zero);
            for (int i = 0; i < 30 && (Grid.Actor(local.MemberId).LastResultCommandId != localCommand || Grid.Actor(remote.MemberId).LastResultCommandId != remoteCommand); ++i) AdvanceTicks(1);
            Assert.That(Grid.Item(extraBowl).Contents.Count + Grid.Item(remoteBowl).Contents.Count, Is.EqualTo(1)); Assert.That(Grid.AutoProcesses, Is.Empty);
            long[] produced = Grid.Items.Values.Select(value => (GridItem)value).Where(i => i.DefinitionId == GridKitchenRules.Soup).Select(i => i.Id).ToArray();
            using (var response = (GridCommandResponse)Finish(remote.CallProduct(duplicate), 500)) Assert.That(response.Status, Is.EqualTo((int)GridResultStatus.Executed).Or.EqualTo((int)GridResultStatus.Rejected));
            Assert.That(Grid.Items.Values.Select(value => (GridItem)value).Where(i => i.DefinitionId == GridKitchenRules.Soup).Select(i => i.Id), Is.EqualTo(produced));
            Assert.That(Grid.Item(pot).Contents, Is.Empty); Assert.That(Grid.Item(pot).AutoProcessId, Is.Zero);
        }
    }
}
