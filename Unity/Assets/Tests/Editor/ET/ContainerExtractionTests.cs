using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private (GridStation Station, long Bowl, long Egg) ExtractionEgg(ClientSessionComponent client)
        {
            GridStation source = PrepStation("食材台"), station = PrepStation("碗台");
            long egg = source.ItemId, bowl = station.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, egg);
            PrepDo(client, GridAction.AddToContainer, station.Id, egg, bowl);
            Pump(12);
            return (station, bowl, egg);
        }

        private GridCommandRequest ExtractionRequest(ClientSessionComponent client, GridStation station, long content)
        {
            var projection = client.GetComponent<GridClientComponent>();
            long container = projection.Stations[station.Id].ItemId;
            return projection.CreateGridCommand(GridAction.TakeContainerContent, target: station.Id, expected: content,
                expectedTarget: container, expectedContents: projection.Items[container].Contents,
                expectedLayoutVersion: projection.LayoutVersion);
        }

        private void ExtractionFinish(ClientSessionComponent client, GridCommandRequest request, string rejection = null)
        {
            long command = request.CommandId;
            using (var response = (GridCommandResponse)Finish(client.CallProduct(request), 500))
            {
                Assert.That(response.Error, Is.Zero, response.Message);
            }
            GridActor actor = Grid.Actor(client.MemberId);
            for (int i = 0; i < 400 && (actor.LastResultCommandId != command ||
                actor.LastResultStatus is GridResultStatus.Accepted or GridResultStatus.PendingApproach); ++i)
            {
                Pump();
            }
            Assert.That(actor.LastResultCommandId, Is.EqualTo(command));
            Assert.That(actor.LastResultStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), actor.LastResultReason);
            Assert.That(actor.LastResultReason, Is.EqualTo(rejection));
            Pump(12);
        }

        private static GridCommandRequest ExtractionCopy(GridCommandRequest source) =>
            ProductMessages.Create<GridCommandRequest>(copy =>
            {
                copy.ProtocolVersion = source.ProtocolVersion;
                copy.MatchId = source.MatchId; copy.WorldId = source.WorldId; copy.WorldGeneration = source.WorldGeneration;
                copy.LevelId = source.LevelId; copy.LevelGeneration = source.LevelGeneration;
                copy.BindingGeneration = source.BindingGeneration; copy.CommandId = source.CommandId;
                copy.Action = source.Action; copy.TargetId = source.TargetId;
                copy.ExpectedItemId = source.ExpectedItemId; copy.ExpectedTargetItemId = source.ExpectedTargetItemId;
                copy.ExpectedLayoutVersion = source.ExpectedLayoutVersion;
                copy.ExpectedContentIds.AddRange(source.ExpectedContentIds);
            });

        [TestCase(false)]
        [TestCase(true)]
        [Category("ContainerExtraction")]
        public void ContainerExtractionBothMenusKeepOriginalEggAndBowlAndReplayOnce(bool drinks)
        {
            StartSolo(liveDrinks: drinks);
            var client = (ClientSessionComponent)host.Client;
            var source = ExtractionEgg(client);
            int count = Grid.Items.Count;
            var request = ExtractionRequest(client, source.Station, source.Egg);
            var replay = ExtractionCopy(request);
            ExtractionFinish(client, request);
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(source.Egg));
            Assert.That(Grid.Item(source.Egg).Location, Is.EqualTo(GridItemLocation.Hand));
            Assert.That(Grid.Item(source.Egg).HolderId, Is.EqualTo(client.MemberId));
            Assert.That(source.Station.ItemId, Is.EqualTo(source.Bowl));
            Assert.That(Grid.Item(source.Bowl).Contents, Is.Empty);
            Assert.That(Grid.Items.Count, Is.EqualTo(count));
            using (var response = (GridCommandResponse)Finish(client.CallProduct(replay), 500))
            {
                Assert.That(response.Error, Is.Zero);
                Assert.That(response.Status, Is.EqualTo((int)GridResultStatus.Executed));
            }
            Assert.That(Grid.Items.Count, Is.EqualTo(count));
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(source.Egg));
        }

        [Test]
        [Category("ContainerExtraction")]
        public void ContainerExtractionRejectsFullHandStaleContentsAndUnknownActions()
        {
            StartSolo();
            var client = (ClientSessionComponent)host.Client;
            var source = ExtractionEgg(client);
            GridStation raw = PrepStation("备料台");
            long tomato = raw.ItemId;
            PrepDo(client, GridAction.TakeCounter, raw.Id, tomato);
            ExtractionFinish(client, ExtractionRequest(client, source.Station, source.Egg), "HandFull");
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(tomato));
            Assert.That(Grid.Item(source.Bowl).Contents, Is.EqualTo(new[] { source.Egg }));
            PrepDo(client, GridAction.PlaceCounter, raw.Id, tomato);
            var stale = ExtractionRequest(client, source.Station, source.Egg);
            stale.ExpectedContentIds.Clear();
            ExtractionFinish(client, stale, "ContentsChanged");
            Assert.That(Grid.Item(source.Bowl).Contents, Is.EqualTo(new[] { source.Egg }));
            var unknown = client.GetComponent<GridClientComponent>().CreateGridCommand((GridAction)1000);
            using var denied = (GridCommandResponse)Finish(client.CallProduct(unknown), 500);
            Assert.That(denied.Status, Is.EqualTo((int)GridResultStatus.Rejected));
            Assert.That(denied.Message, Is.EqualTo("InvalidCommand"));
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.Zero);
        }

        [Test]
        [Category("ContainerExtraction")]
        public void ContainerExtractionRejectsPausedHeldWorkAndLiquidWithoutBreakingProcess()
        {
            StartSolo();
            var client = (ClientSessionComponent)host.Client;
            long bowl = PrepEggBowl(client);
            long egg = Grid.Item(bowl).Contents.Single();
            PrepDo(client, GridAction.StartHeldWork, input: egg, expectedTarget: bowl);
            var work = Grid.InputProcess(egg);
            PrepDo(client, GridAction.StopWork, input: egg, expectedTarget: bowl, process: work.Id);
            GridStation station = PrepStation("碗台");
            PrepDo(client, GridAction.PlaceCounter, station.Id, bowl);
            int progress = work.ProgressTicks;
            ExtractionFinish(client, ExtractionRequest(client, station, egg), "ProcessLocked");
            Assert.That(work.ProgressTicks, Is.EqualTo(progress));
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { egg }));
            PrepDo(client, GridAction.TakeCounter, station.Id, bowl);
            PrepDo(client, GridAction.StartHeldWork, input: egg, expectedTarget: bowl, process: work.Id);
            AdvanceTicks(work.RequiredTicks + 1); Pump(20);
            long liquid = Grid.Item(bowl).Contents.Single();
            PrepDo(client, GridAction.PlaceCounter, station.Id, bowl);
            ExtractionFinish(client, ExtractionRequest(client, station, liquid), "RequiresSolidContent");
            Assert.That(Grid.Item(bowl).Contents, Is.EqualTo(new[] { liquid }));
        }

        [Test]
        [Category("ContainerExtraction")]
        [Category("ContainerExtractionFinal")]
        public void ContainerExtractionDoesNotDismantleCookingOrReadyBatch()
        {
            StartSolo();
            var client = (ClientSessionComponent)host.Client;
            long pot = SoupFillPot(client);
            GridStation stove = PrepStation("灶台");
            long solid = Grid.Item(pot).Contents.Single(id => Grid.Item(id).DefinitionId == GridKitchenRules.TomatoChunks);
            long liquid = Grid.Item(pot).Contents.Single(id => id != solid);
            ExtractionFinish(client, ExtractionRequest(client, stove, solid));
            Assert.That(Grid.Item(pot).Contents, Is.EqualTo(new[] { liquid }), "Other input stays in the original pot");
            Assert.That(Grid.Item(liquid).HolderId, Is.EqualTo(pot));
            PrepDo(client, GridAction.AddToContainer, stove.Id, solid, pot);
            var batch = SoupBegin(client, pot);
            ExtractionFinish(client, ExtractionRequest(client, stove, solid), "ProcessLocked");
            AdvanceTicks(batch.RequiredTicks + 1); Pump(20);
            ExtractionFinish(client, ExtractionRequest(client, stove, solid), "ProcessLocked");
            Assert.That(batch.RemainingPortions, Is.EqualTo(batch.TotalPortions));
            Assert.That(Grid.Item(pot).Contents.Contains(solid), Is.True);
        }

        [Test]
        [Category("ContainerExtraction")]
        public void ContainerExtractionRealKcpRaceMovesOneOriginalAndKeepsIndependentProjection()
        {
            StartHost();
            var local = (ClientSessionComponent)host.Client;
            var remote = CoopRemote();
            var source = ExtractionEgg(local);
            PrepDo(local, GridAction.Move, cell: new GridCell(1, 9));
            PrepDo(remote, GridAction.Move, cell: new GridCell(1, 9));
            var first = ExtractionRequest(local, source.Station, source.Egg);
            var second = ExtractionRequest(remote, source.Station, source.Egg);
            var call1 = local.CallProduct(first); var call2 = remote.CallProduct(second);
            using (var response = (GridCommandResponse)Finish(call1, 500)) Assert.That(response.Error, Is.Zero);
            using (var response = (GridCommandResponse)Finish(call2, 500)) Assert.That(response.Error, Is.Zero);
            Pump(350);
            Assert.That(new[] { Grid.Actor(local.MemberId), Grid.Actor(remote.MemberId) }.Count(actor => actor.HandItemId == source.Egg), Is.EqualTo(1));
            Assert.That(Grid.Item(source.Bowl).Contents, Is.Empty);
            Assert.That(source.Station.ItemId, Is.EqualTo(source.Bowl));
            var remoteView = remote.GetComponent<GridClientComponent>();
            Assert.That(remoteView.Items[source.Egg].Location, Is.EqualTo(GridItemLocation.Hand));
            Assert.That(remoteView.Items[source.Egg].HolderId, Is.EqualTo(Grid.Item(source.Egg).HolderId));
            Assert.That(remoteView.Items[source.Bowl].Contents, Is.Empty);
        }
    }
}
