using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private StationLayoutResponse LayoutReply(ClientSessionComponent client, StationLayoutRequest request)
        {
            var task = client.CallProduct(request); PumpMessages(8);
            Assert.That(task.Status, Is.Not.EqualTo(UniTaskStatus.Pending));
            return (StationLayoutResponse)task.GetAwaiter().GetResult();
        }

        private void LayoutDo(GridStation station, GridCell cell, int orientation = 0, string rejection = null, long? version = null)
        {
            ClientSessionComponent client = host.Client;
            using (var receipt = LayoutReply(client, client.GetComponent<GridClientComponent>().CreateStationLayout(station.Id, cell, orientation, version)))
                Assert.That(receipt.Error, Is.Zero, receipt.Message);
            AdvanceTicks(1); PumpMessages(8);
            HostPeer peer = ((LocalMessageComponent)client.Local).Peer;
            Assert.That(peer.LastCommandStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), peer.LastCommandReason);
            Assert.That(peer.LastCommandReason, Is.EqualTo(rejection));
        }

        private static StationLayoutRequest CopyLayout(StationLayoutRequest source) => ProductMessages.Create<StationLayoutRequest>(value =>
        {
            value.ProtocolVersion=source.ProtocolVersion;value.MatchId=source.MatchId;value.WorldId=source.WorldId;value.WorldGeneration=source.WorldGeneration;
            value.BindingGeneration=source.BindingGeneration;value.LevelId=source.LevelId;value.LevelGeneration=source.LevelGeneration;value.CommandId=source.CommandId;
            value.StationId=source.StationId;value.ExpectedLayoutVersion=source.ExpectedLayoutVersion;value.X=source.X;value.Y=source.Y;value.Orientation=source.Orientation;
        });

        [Test]
        public void SoloStationLayoutRealMailboxRotatesOnceAndReplaysOnlySamePayload()
        {
            StartSolo(); ClientSessionComponent client = host.Client; var projection = client.GetComponent<GridClientComponent>();
            GridStation oven = PrepStation("烤箱"); long version = Grid.LayoutVersion, tick = Grid.Tick;
            var request=projection.CreateStationLayout(oven.Id,oven.Cell,1);
            var queuedReplay=CopyLayout(request);var finalReplay=CopyLayout(request);var conflict=CopyLayout(request);conflict.Orientation=2;
            using(var receipt=LayoutReply(client,request)) Assert.That(receipt.Status,Is.EqualTo((int)GridResultStatus.Accepted));
            using(var receipt=LayoutReply(client,queuedReplay)) Assert.That(receipt.Status,Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(Grid.LayoutCommands.Count,Is.EqualTo(1)); Assert.That(Grid.Tick,Is.EqualTo(tick)); Assert.That(oven.Orientation,Is.Zero);
            AdvanceTicks(1);PumpMessages(8);
            Assert.That(oven.Orientation,Is.EqualTo(1));Assert.That(Grid.LayoutVersion,Is.EqualTo(version+1));
            Assert.That(Grid.Blocked.Contains(new GridCell(4,14)),Is.True);Assert.That(Grid.Blocked.Contains(new GridCell(5,15)),Is.False);
            using(var receipt=LayoutReply(client,finalReplay)) Assert.That(receipt.Status,Is.EqualTo((int)GridResultStatus.Executed));
            using(var receipt=LayoutReply(client,conflict)) Assert.That(receipt.Message,Is.EqualTo("CommandConflict"));
            Assert.That(Grid.LayoutCommands,Is.Empty);Assert.That(projection.LayoutVersion,Is.EqualTo(version+1));
            Assert.That(projection.Stations[oven.Id].Orientation,Is.EqualTo(1));
        }

        [TestCase(0,12)]
        [TestCase(3,11)]
        [TestCase(3,1)]
        public void SoloStationLayoutRejectsBoundaryOverlapAndEntranceWithoutMutation(int x,int y)
        {
            StartSolo(); GridStation station=PrepStation("备料台");long item=station.ItemId,version=Grid.LayoutVersion;
            var cell=station.Cell;var blocked=Grid.Blocked.ToArray();
            LayoutDo(station,new GridCell(x,y),rejection:"LayoutBlocked");
            Assert.That(station.Cell,Is.EqualTo(cell));Assert.That(station.ItemId,Is.EqualTo(item));Assert.That(Grid.Item(item).HolderId,Is.EqualTo(station.Id));
            Assert.That(Grid.LayoutVersion,Is.EqualTo(version));Assert.That(Grid.Blocked,Is.EquivalentTo(blocked));
        }

        [Test]
        public void SoloStationLayoutRejectsClosingLastEntrancePath()
        {
            var layouts=(System.Collections.Generic.List<DRInitialLayoutConfig>)Tables.Instance.DTInitialLayoutConfig.DataList;
            try
            {
                foreach(int x in new[]{1,2,4,5}) layouts.Add(InitialStation(100+x,1,x,2));
                StartSolo();var station=PrepStation("交接台");long version=Grid.LayoutVersion;
                Assert.That(Grid.FindGridPath(GridStationConfig.Entrance(),new GridCell(1,10)),Is.Not.Null);
                LayoutDo(station,new GridCell(3,2),rejection:"LayoutBlocked");
                Assert.That(Grid.LayoutVersion,Is.EqualTo(version));Assert.That(station.Cell,Is.EqualTo(new GridCell(3,8)));
                Assert.That(Grid.FindGridPath(GridStationConfig.Entrance(),new GridCell(1,10)),Is.Not.Null);
            }
            finally { layouts.RemoveAll(row=>row.Id>100); }
        }

        [Test]
        public void SoloStationLayoutRejectsRoleAndRealDisconnectedDrop()
        {
            StartSolo();ClientSessionComponent client=host.Client;GridStation station=PrepStation("备料台");
            LayoutDo(station,Grid.Actor(client.MemberId).Cell,rejection:"LayoutOccupied");
            ClientSessionComponent other=GridJoin();int member=other.MemberId;GridStation bowl=PrepStation("碗台");long item=bowl.ItemId;
            PrepDo(other,GridAction.TakeCounter,bowl.Id,item);GridCell dropCell=Grid.Actor(member).Cell;
            using(var receipt=(ProductLeaveResponse)Finish(other.CallProduct(ProductMessages.Create<ProductLeaveRequest>(value=>value.MatchId=other.MatchId)),100))
                Assert.That(receipt.Error,Is.Zero);
            AdvanceTicks(1);PumpMessages(5);
            Assert.That(Grid.Actor(member),Is.Null);Assert.That(Grid.Item(item).Location,Is.EqualTo(GridItemLocation.Ground));
            long version=Grid.LayoutVersion;
            LayoutDo(station,dropCell,rejection:"LayoutOccupied");
            Assert.That(Grid.Item(item).Cell,Is.EqualTo(dropCell));Assert.That(Grid.LayoutVersion,Is.EqualTo(version));
        }

        [Test]
        public void SoloStationLayoutRechecksVersionAfterQueuedWinnerAndRejectsPausedOrOperating()
        {
            StartSolo();ClientSessionComponent client=host.Client;var projection=client.GetComponent<GridClientComponent>();
            GridStation station=PrepStation("备料台");long version=Grid.LayoutVersion;
            using(var receipt=LayoutReply(client,projection.CreateStationLayout(station.Id,new GridCell(2,12),0)))Assert.That(receipt.Error,Is.Zero);
            using(var receipt=LayoutReply(client,projection.CreateStationLayout(station.Id,new GridCell(4,12),0)))Assert.That(receipt.Error,Is.Zero);
            AdvanceTicks(1);PumpMessages(8);
            Assert.That(station.Cell,Is.EqualTo(new GridCell(2,12)));Assert.That(Grid.LayoutVersion,Is.EqualTo(version+1));
            Assert.That(projection.LastReason,Is.EqualTo("LayoutChanged"));
            LevelDo(LevelControlAction.Pause);LayoutDo(station,new GridCell(4,12),rejection:"Paused");
            Assert.That(station.Cell,Is.EqualTo(new GridCell(2,12)));
            LevelDo(LevelControlAction.Resume);LevelDo(LevelControlAction.Start);LayoutDo(station,new GridCell(4,12),rejection:"WrongPhase");
            Assert.That(Grid.LayoutVersion,Is.EqualTo(version+1));Assert.That(station.Cell,Is.EqualTo(new GridCell(2,12)));
        }

        [Test]
        public void SoloStationLayoutCancelsFutureRouteButPreservesCurrentStep()
        {
            StartSolo();ClientSessionComponent client=host.Client;GridActor actor=Grid.Actor(client.MemberId);
            using(var receipt=SoloReply(client,client.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.Move,new GridCell(4,10))))Assert.That(receipt.Error,Is.Zero);
            AdvanceTicks(2);PumpMessages(5);Assert.That(actor.Motion,Is.EqualTo(GridMotion.Transit));
            var next=actor.NextCell;var oldCell=actor.Cell;long started=actor.StepStartTick;
            LayoutDo(PrepStation("交接台"),new GridCell(3,12));
            Assert.That(actor.Cell,Is.EqualTo(oldCell));Assert.That(actor.NextCell,Is.EqualTo(next));Assert.That(actor.StepStartTick,Is.EqualTo(started));
            Assert.That(actor.Path,Is.Empty);Assert.That(actor.HasIntent,Is.False);
            AdvanceTicks(GridKitchenRules.StepTicks);Assert.That(actor.Cell,Is.EqualTo(next));Assert.That(actor.Motion,Is.EqualTo(GridMotion.Standing));
            Assert.That(Grid.Results.Any(result=>result.Status==GridResultStatus.Cancelled && result.Reason=="LayoutChanged"),Is.True);
        }

        [Test]
        public void SoloStationPreserveMovedChoppingRetainsMaterialProgressAndReleasesOldClaim()
        {
            StartSolo();ClientSessionComponent client=host.Client;GridStation source=PrepStation("备料台"),station=PrepStation("切配台");long tomato=source.ItemId;
            PrepDo(client,GridAction.TakeCounter,source.Id,tomato);PrepDo(client,GridAction.PlaceCounter,station.Id,tomato);
            PrepDo(client,GridAction.StartStationWork,station.Id,tomato,tomato);AdvanceTicks(3);
            GridManualProcess process=Grid.InputProcess(tomato);int progress=process.ProgressTicks;long processId=process.Id,stationId=station.Id;
            Assert.That(process.State,Is.EqualTo(GridWorkState.Working));
            LayoutDo(station,new GridCell(5,7));
            Assert.That(station.Id,Is.EqualTo(stationId));Assert.That(station.ItemId,Is.EqualTo(tomato));Assert.That(Grid.Item(tomato).Cell,Is.EqualTo(station.Cell));
            Assert.That(process.Id,Is.EqualTo(processId));Assert.That(process.ProgressTicks,Is.EqualTo(progress));Assert.That(process.State,Is.EqualTo(GridWorkState.Paused));
            Assert.That(process.WorkerMemberId,Is.Zero);Assert.That(Grid.Actor(client.MemberId).WorkId,Is.Zero);
            PrepDo(client,GridAction.StartStationWork,station.Id,tomato,tomato,processId);
            Assert.That(process.ProgressTicks,Is.GreaterThanOrEqualTo(progress));Assert.That(process.Id,Is.EqualTo(processId));
            AdvanceTicks(process.RequiredTicks);PumpMessages(5);
            Assert.That(process.State,Is.EqualTo(GridWorkState.Completed));Assert.That(station.ItemId,Is.EqualTo(process.OutputItemId));
            Assert.That(Grid.Items.Values.Count(value=>((GridItem)value).DefinitionId==GridKitchenRules.TomatoChunks),Is.EqualTo(1));
        }

        [Test]
        public void SoloStationPreserveCookingStoveKeepsOriginalPotContentsAndBatch()
        {
            StartSolo();ClientSessionComponent client=host.Client;long pot=SoupFillPot(client);GridAutoProcess process=SoupBegin(client,pot);
            GridStation station=PrepStation("灶台");var contents=Grid.Item(pot).Contents.ToArray();var allItems=Grid.Items.Keys.ToArray();
            long processId=process.Id;int progress=process.ProgressTicks;
            LayoutDo(station,new GridCell(1,7),1);
            Assert.That(station.ItemId,Is.EqualTo(pot));Assert.That(station.AutoProcessId,Is.EqualTo(processId));Assert.That(process.StartStationId,Is.EqualTo(station.Id));
            Assert.That(process.ProgressTicks,Is.EqualTo(progress+1));Assert.That(Grid.Item(pot).Contents,Is.EqualTo(contents));Assert.That(Grid.Items.Keys,Is.EquivalentTo(allItems));
            Assert.That(Grid.Item(pot).Cell,Is.EqualTo(station.Cell));Assert.That(contents.All(id=>Grid.Item(id).HolderId==pot && Grid.Item(id).Cell==station.Cell),Is.True);
            AdvanceTicks(2);Assert.That(process.ProgressTicks,Is.EqualTo(progress+3));Assert.That(process.Id,Is.EqualTo(processId));
        }
    }
}
