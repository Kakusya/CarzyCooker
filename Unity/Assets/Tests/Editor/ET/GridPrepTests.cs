using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(GridManualProcess))]
    public sealed partial class ProductSessionTests
    {
        private GridStation PrepStation(string name) => Grid.Stations.Values.Select(v => (GridStation)v).Single(v => v.Name == name);
        private GridManualProcess PrepProcess(long input) => Grid.InputProcess(input);

        private long PrepSend(ClientSessionComponent client, GridAction action, long target=0, long input=0, long expectedTarget=0, long process=0, GridCell cell=default, long order = 0)
        {
            var projection = client.GetComponent<GridClientComponent>();
            var request = projection.CreateGridCommand(action, cell, target, input, expectedTarget, process, expectedOrderId: order);
            long command = request.CommandId;
            using var response = (GridCommandResponse)Finish(client.CallProduct(request), 300);
            Assert.That(response.Error, Is.Zero, response.Message);
            return command;
        }

        private void PrepDo(ClientSessionComponent client, GridAction action, long target=0, long input=0, long expectedTarget=0, long process=0, GridCell cell=default, string rejection=null, long order = 0)
        {
            long command = PrepSend(client,action,target,input,expectedTarget,process,cell,order);
            GridActor actor = Grid.Actor(client.MemberId);
            for(int i=0; i<350 && (actor.LastResultCommandId!=command || actor.LastResultStatus==GridResultStatus.PendingApproach); ++i) Pump();
            Assert.That(actor.LastResultCommandId, Is.EqualTo(command));
            Assert.That(actor.LastResultStatus, Is.EqualTo(rejection==null?GridResultStatus.Executed:GridResultStatus.Rejected),actor.LastResultReason);
            Assert.That(actor.LastResultReason, Is.EqualTo(rejection));
            if ((LocalMessageComponent)client.Local != null)
            {
                PumpMessages(8);
                var result = ProductOutcomeTestObserver.Received.Single(value => value.Value.Kind == ProductCommandKind.Grid &&
                    value.Value.Context.CommandId == command && value.Value.Context.BindingGeneration == client.BindingGeneration &&
                    value.Value.Context.WorldId == Grid.WorldId && value.Value.Context.WorldGeneration == Grid.WorldGeneration);
                Assert.That(result.Value.Status, Is.EqualTo(actor.LastResultStatus)); Assert.That(result.Value.Reason, Is.EqualTo(rejection));
            }
        }

        private long PrepEggBowl(ClientSessionComponent client)
        {
            GridStation food=PrepStation("食材台"), bowl=PrepStation("碗台");
            long egg=food.ItemId, bowlId=bowl.ItemId;
            PrepDo(client,GridAction.TakeCounter,food.Id,egg);
            PrepDo(client,GridAction.AddToContainer,bowl.Id,egg,bowlId);
            PrepDo(client,GridAction.TakeCounter,bowl.Id,bowlId);
            return bowlId;
        }

        private void PrepTick(int count)
        {
            for(int i=0;i<count;++i)
            {
                PumpMessages(3); Grid.AdvanceGridTick();
            }
        }

        [Test]
        [Category("ProductSemanticEnums")]
        public void GridPrepContainerDefinitionsAndUniqueContents()
        {
            GridKitchenSystem.ValidateGridDefinitions(Tables.Instance.DTGridItemConfig.DataList,Tables.Instance.DTGridManualRecipeConfig.DataList);
            Assert.Throws<InvalidOperationException>(()=>GridKitchenSystem.ValidateGridDefinitions(Tables.Instance.DTGridItemConfig.DataList.Where(v=>v.Id!=GridKitchenRules.Egg),Tables.Instance.DTGridManualRecipeConfig.DataList));
            Assert.Throws<InvalidOperationException>(()=>GridKitchenSystem.ValidateGridDefinitions(Tables.Instance.DTGridItemConfig.DataList,Tables.Instance.DTGridManualRecipeConfig.DataList.Where(v=>v.Mode!=GridManualMode.Beat)));
            StartHost();
            ClientSessionComponent client=host.Client, other=GridJoin();
            GridStation bowl=PrepStation("碗台"), handoff=PrepStation("交接台");
            long bowlId=bowl.ItemId; Assert.That(Grid.Item(bowlId).Contents,Is.Empty);
            long carried=PrepEggBowl(client), egg=Grid.Item(carried).Contents.Single();
            Assert.That(carried,Is.EqualTo(bowlId)); Assert.That(Grid.Item(egg).Location,Is.EqualTo(GridItemLocation.Container));
            Assert.That(Grid.Item(egg).HolderId,Is.EqualTo(bowlId)); Assert.That(Grid.Item(egg).Parent,Is.SameAs(Grid));
            Assert.That(((Match)Grid.Match).Children.Values.OfType<Participant>().Count(),Is.EqualTo(2));
            Assert.That(Grid.Items.Values.Count(v=>((GridItem)v).Id==egg),Is.EqualTo(1));
            PrepDo(client,GridAction.PlaceCounter,handoff.Id,bowlId);
            PrepDo(other,GridAction.TakeCounter,handoff.Id,bowlId);
            Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.Zero);
            Assert.That(Grid.Actor(other.MemberId).HandItemId,Is.EqualTo(bowlId));
            Assert.That(Grid.Item(bowlId).Contents,Is.EqualTo(new[]{egg}));
            Assert.That(Grid.Item(egg).HolderId,Is.EqualTo(bowlId));
        }

        [Test]
        public void GridPrepContainerRejectionsDoNotTransfer()
        {
            StartHost(); ClientSessionComponent client=host.Client;
            GridStation tomato=PrepStation("备料台"), bowl=PrepStation("碗台"), food=PrepStation("食材台"), chopping=PrepStation("切配台");
            long tomatoId=tomato.ItemId, bowlId=bowl.ItemId, eggId=food.ItemId;
            PrepDo(client,GridAction.TakeCounter,tomato.Id,tomatoId);
            PrepDo(client,GridAction.AddToContainer,bowl.Id,tomatoId,bowlId,rejection:"IncompatibleMaterial");
            Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.EqualTo(tomatoId)); Assert.That(Grid.Item(bowlId).Contents,Is.Empty);
            PrepDo(client,GridAction.PlaceCounter,chopping.Id,tomatoId);
            PrepDo(client,GridAction.TakeCounter,food.Id,eggId);
            PrepDo(client,GridAction.AddToContainer,bowl.Id,eggId,bowlId+1,rejection:"TargetChanged");
            Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.EqualTo(eggId)); Assert.That(Grid.Item(bowlId).Contents,Is.Empty);
            PrepDo(client,GridAction.AddToContainer,bowl.Id,eggId,bowlId);
            var replay=GridRequest(client,client.GetComponent<GridClientComponent>().LastCommandId,GridAction.AddToContainer,target:bowl.Id,expected:eggId);
            replay.ExpectedTargetItemId=bowlId;
            using(var response=(GridCommandResponse)Finish(client.CallProduct(replay),300))Assert.That(response.Error,Is.Zero);
            Assert.That(Grid.Item(bowlId).Contents,Is.EqualTo(new[]{eggId}),"Replayed add cannot duplicate contents");
            Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.Zero);
            // 第二份材料只作为拒绝用例的初态；行为依旧经过真实消息和 Tick。
            GridItem extra=Grid.AddChild<GridItem>(); extra.DefinitionId=GridKitchenRules.Egg; extra.Name="鸡蛋";
            extra.Location=GridItemLocation.Counter;extra.HolderId=food.Id;extra.Cell=food.Cell;extra.Contents.Clear();
            Grid.Items.Add(extra.Id,extra);food.ItemId=extra.Id;Grid.Dirty=true;PrepTick(1);Pump(8);
            PrepDo(client,GridAction.TakeCounter,food.Id,extra.Id);
            PrepDo(client,GridAction.AddToContainer,bowl.Id,extra.Id,bowlId,rejection:"ContainerFull");
            Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.EqualTo(extra.Id)); Assert.That(Grid.Item(bowlId).Contents,Is.EqualTo(new[]{eggId}));
            PrepDo(client,GridAction.PlaceCounter,food.Id,extra.Id);
            PrepDo(client,GridAction.TakeCounter,bowl.Id,bowlId);
            PrepDo(client,GridAction.AddToContainer,chopping.Id,bowlId,tomatoId,rejection:"NestedContainer");
            Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.EqualTo(bowlId)); Assert.That(chopping.ItemId,Is.EqualTo(tomatoId));
        }

        [Test]
        [Category("ProductSemanticEnums")]
        public void GridPrepWorkChoppingStopsResumesMovesAndProducesOnce()
        {
            StartHost();ClientSessionComponent first=host.Client, second=GridJoin();
            GridStation source=PrepStation("备料台"), chopping=PrepStation("切配台");long tomato=source.ItemId;
            PrepDo(first,GridAction.TakeCounter,source.Id,tomato);PrepDo(first,GridAction.PlaceCounter,chopping.Id,tomato);
            PrepDo(second,GridAction.Move,cell:new GridCell(5,9));
            PrepDo(first,GridAction.StartStationWork,chopping.Id,tomato,tomato);
            GridManualProcess work=PrepProcess(tomato); Assert.That(work.Parent,Is.SameAs(Grid));
            Assert.That(work.ProgressTicks,Is.LessThan(3)); Assert.That(chopping.ItemId,Is.EqualTo(tomato));
            PrepDo(second,GridAction.StartStationWork,chopping.Id,tomato,tomato,work.Id,rejection:"WorkBusy");
            Assert.That(work.WorkerMemberId,Is.EqualTo(first.MemberId));
            PrepTick(12);int before=work.ProgressTicks;
            using (var denied = (GridCommandResponse)FinishMessages(first.CallProduct(GridRequest(first,1000,GridAction.Move,new GridCell(0,0))), "blocked move receipt"))
                Assert.That(denied.Message, Is.EqualTo("BlockedCell"));
            Assert.That(work.State,Is.EqualTo(GridWorkState.Working));
            PrepDo(first,GridAction.StartStationWork,chopping.Id,tomato,tomato,work.Id,rejection:"WorkBusy");
            Assert.That(work.ProgressTicks,Is.GreaterThanOrEqualTo(before));
            PrepDo(first,GridAction.StopWork,input:tomato,expectedTarget:tomato,process:work.Id);
            int paused=work.ProgressTicks;PrepTick(10);Assert.That(work.ProgressTicks,Is.EqualTo(paused));
            PrepDo(first,GridAction.TakeCounter,chopping.Id,tomato);
            GridStation another=Grid.AddChild<GridStation>();another.Name="测试切配台";another.Kind=GridStationKind.Chopping;another.Cell=new GridCell(1,7);another.ItemId=0;
            DRStationConfig definition=Tables.Instance.DTStationConfig.Get(chopping.DefinitionId);
            another.DefinitionId=definition.Id;another.Orientation=0;another.ItemSlotCapacity=definition.ItemSlotCapacity;another.Capabilities=(GridStationCapability)definition.Capabilities;
            Grid.Stations.Add(another.Id,another);Grid.Blocked.Add(another.Cell);Grid.Dirty=true;
            PrepDo(first,GridAction.PlaceCounter,another.Id,tomato);
            PrepDo(first,GridAction.StartStationWork,another.Id,tomato,tomato,work.Id);
            Assert.That(work.StationId,Is.EqualTo(another.Id));Assert.That(work.ProgressTicks,Is.EqualTo(paused));
            PrepDo(first,GridAction.Move,cell:new GridCell(2,7));
            paused=work.ProgressTicks;Assert.That(work.State,Is.EqualTo(GridWorkState.Paused));PrepTick(10);Assert.That(work.ProgressTicks,Is.EqualTo(paused));
            PrepDo(second,GridAction.StartStationWork,another.Id,tomato,tomato,work.Id);
            Assert.That(work.WorkerMemberId,Is.EqualTo(second.MemberId));
            PrepDo(first,GridAction.StopWork,input:tomato,expectedTarget:tomato,process:work.Id,rejection:"ProcessChanged");
            PrepTick(work.RequiredTicks);Assert.That(work.State,Is.EqualTo(GridWorkState.Completed));
            Assert.That(Grid.Item(tomato),Is.Null);Assert.That(another.ItemId,Is.EqualTo(work.OutputItemId));
            Assert.That(Grid.Item(work.OutputItemId).DefinitionId,Is.EqualTo(GridKitchenRules.TomatoChunks));
            PrepTick(65);Assert.That(Grid.Items.Values.Count(v=>((GridItem)v).DefinitionId==GridKitchenRules.TomatoChunks),Is.EqualTo(1));
            PrepDo(first,GridAction.TakeCounter,another.Id,work.OutputItemId);
        }

        [Test]
        [Category("ProductSemanticEnums")]
        public void GridPrepWorkHeldBowlHandoffDisconnectLeaveAndNewWorld()
        {
            StartHost();ClientSessionComponent first=host.Client, second=GridJoin();
            long bowl=PrepEggBowl(first),egg=Grid.Item(bowl).Contents.Single();
            PrepDo(first,GridAction.StartHeldWork,bowl,egg,bowl);GridManualProcess work=PrepProcess(egg);
            PrepTick(10);PrepDo(first,GridAction.StopWork,input:egg,expectedTarget:bowl,process:work.Id);
            int paused=work.ProgressTicks;PrepTick(10);Assert.That(work.ProgressTicks,Is.EqualTo(paused));
            GridStation handoff=PrepStation("交接台");PrepDo(first,GridAction.PlaceCounter,handoff.Id,bowl);
            PrepDo(second,GridAction.TakeCounter,handoff.Id,bowl);PrepDo(second,GridAction.StartHeldWork,bowl,egg,bowl,work.Id);
            Assert.That(work.WorkerMemberId,Is.EqualTo(second.MemberId));
            GridActor actor=Grid.Actor(second.MemberId);GridCell drop=actor.Cell;
            HostPeer peer=((LocalMessageComponent)second.Local).Peer;long oldGeneration=peer.Generation;
            var retiredRequest = GridRetiredBindingRequest(second, GridAction.StartHeldWork, drop, bowl, egg, bowl, work.Id);
            peer.CloseEntry();peer.CloseEntry();Pump(12);
            paused=work.ProgressTicks;Assert.That(work.State,Is.EqualTo(GridWorkState.Paused));Assert.That(Grid.Item(bowl).Cell,Is.EqualTo(drop));
            Assert.That(Grid.Item(egg).HolderId,Is.EqualTo(bowl));Assert.That(Grid.Item(bowl).Location,Is.EqualTo(GridItemLocation.Ground));
            GridRejectRetiredBinding(retiredRequest);
            PrepTick(10);Assert.That(work.ProgressTicks,Is.EqualTo(paused));
            PrepDo(first,GridAction.Move,cell:drop);PrepDo(first,GridAction.PickupDrop,bowl);
            PrepDo(first,GridAction.StartHeldWork,bowl,egg,bowl,work.Id);PrepTick(work.RequiredTicks);
            Assert.That(work.State,Is.EqualTo(GridWorkState.Completed));Assert.That(Grid.Item(egg),Is.Null);
            long liquid=Grid.Item(bowl).Contents.Single();Assert.That(liquid,Is.EqualTo(work.OutputItemId));Assert.That(liquid,Is.Not.EqualTo(egg));
            Assert.That(Grid.Item(liquid).DefinitionId,Is.EqualTo(GridKitchenRules.EggLiquid));Assert.That(Grid.Actor(first.MemberId).HandItemId,Is.EqualTo(bowl));
            first.LeaveProduct().Forget();Pump(20);Assert.That(Grid.Actor(first.MemberId),Is.Null);Assert.That(Grid.Item(bowl).Location,Is.EqualTo(GridItemLocation.Ground));
            ClientSessionComponent last=GridJoin();GridStation tomatoes=PrepStation("备料台"),chopping=PrepStation("切配台");long tomato=tomatoes.ItemId;
            PrepDo(last,GridAction.TakeCounter,tomatoes.Id,tomato);PrepDo(last,GridAction.PlaceCounter,chopping.Id,tomato);
            PrepDo(last,GridAction.StartStationWork,chopping.Id,tomato,tomato);
            GridManualProcess unfinished=PrepProcess(tomato);Assert.That(unfinished.State,Is.EqualTo(GridWorkState.Working));
            EntityRef<GridManualProcess> oldWork=unfinished;
            string oldWorld=Grid.WorldId;EntityRef<GridKitchenComponent> oldGrid=Grid;host.StopProduct();Pump(4);Assert.That((GridKitchenComponent)oldGrid,Is.Null);
            Assert.That((GridManualProcess)oldWork,Is.Null);
            Game.RuntimeInstanceContext.Reset();StartHost();Assert.That(Grid.WorldId,Is.Not.EqualTo(oldWorld));Assert.That(Grid.Processes,Is.Empty);
            Assert.That(Grid.Items.Values.Count(v=>((GridItem)v).DefinitionId==GridKitchenRules.Egg),Is.EqualTo(1));
            Assert.That(Grid.Item(PrepStation("碗台").ItemId).Contents,Is.Empty);
        }

        [Test]
        [Category("ProductSemanticEnums")]
        public void GridPrepWireLocalKcpDuplicateAndIndependentVersionedGraph()
        {
            StartHost();ClientSessionComponent local=host.Client,remote=CreateRemoteClient();remote.JoinProduct().Forget();Pump(100);
            Assert.That(NetworkHelper.ToIPEndPoint(host.ListenEndpoint).Port,Is.GreaterThan(0));
            ClientSessionComponent incompatible=CreateRemoteClient();
            using(var response=(ProductJoinResponse)Finish(incompatible.CallProduct(ProductMessages.Create<ProductJoinRequest>(v=>v.ProtocolVersion=2)),300))
                Assert.That(response.Error,Is.EqualTo(ProductError.IncompatibleProtocol));
            Assert.That(Grid.Actors.Count,Is.EqualTo(2),"Version 2 join cannot create a member");
            long bowl=PrepEggBowl(remote),egg=Grid.Item(bowl).Contents.Single();
            var projection=remote.GetComponent<GridClientComponent>();
            var request=projection.CreateGridCommand(GridAction.StartHeldWork,target:bowl,expected:egg,expectedTarget:bowl);
            long command=request.CommandId;
            using(var response=(GridCommandResponse)Finish(remote.CallProduct(request),300))Assert.That(response.Error,Is.Zero);
            Pump(10);GridManualProcess work=PrepProcess(egg);long id=work.Id;int progress=work.ProgressTicks;
            var replay=GridRequest(remote,command,GridAction.StartHeldWork,target:bowl,expected:egg);replay.ExpectedTargetItemId=bowl;
            using(var response=(GridCommandResponse)Finish(remote.CallProduct(replay),300))Assert.That(response.Error,Is.Zero);
            Assert.That(Grid.Processes.Count,Is.EqualTo(1));Assert.That(work.Id,Is.EqualTo(id));Assert.That(work.ProgressTicks,Is.GreaterThanOrEqualTo(progress));
            HostPeer localPeer=((LocalMessageComponent)local.Local).Peer;
            var localProjection=local.GetComponent<GridClientComponent>();
            using(var snapshot=GridBroadcast.CreateSnapshot(Grid,localPeer))
            {
                snapshot.Version=localProjection.Version+1;localProjection.ApplyGridSnapshot(snapshot);
                snapshot.Items.Single(v=>v.ItemId==bowl).Contents.Clear();snapshot.Processes[0].ProgressTicks=999;
                Assert.That(localProjection.Items[bowl].Contents,Is.EqualTo(new[]{egg}));Assert.That(localProjection.Processes[id].ProgressTicks,Is.LessThan(999));
            }
            // 新实际版本重新广播，覆盖上面的独立复制探测。
            PrepTick(2);Pump(10);Assert.That(projection.Version,Is.EqualTo(localProjection.Version));
            Assert.That(projection.Processes[id].ProgressTicks,Is.EqualTo(localProjection.Processes[id].ProgressTicks));
            Assert.That(projection.Items[bowl].Contents,Is.EqualTo(localProjection.Items[bowl].Contents));
            Assert.That(projection.Items[bowl].Contents,Is.Not.SameAs(localProjection.Items[bowl].Contents));
            var old=GridRequest(remote,command+1,GridAction.Move,new GridCell(2,9));old.ProtocolVersion=2;
            using(var response=(GridCommandResponse)Finish(remote.CallProduct(old),300))Assert.That(response.Message,Is.EqualTo("IncompatibleProtocol"));
            PrepTick(work.RequiredTicks);Pump(12);Assert.That(work.State,Is.EqualTo(GridWorkState.Completed));
            Assert.That(projection.Items[bowl].Contents.Single(),Is.EqualTo(work.OutputItemId));
            Assert.That(Grid.Items.Values.Count(v=>((GridItem)v).DefinitionId==GridKitchenRules.EggLiquid),Is.EqualTo(1));
        }
    }
}
