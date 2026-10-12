using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private long PrepareServingMeal(int dish)
        {
            ClientSessionComponent client=host.Client;
            if(dish==1)
            {
                long pot=SoupFillPot(client);GridAutoProcess batch=SoupBegin(client,pot);AdvanceTicks(batch.RequiredTicks);
                GridStation source=PrepStation("碗台"),stove=PrepStation("灶台");long bowl=source.ItemId;
                PrepDo(client,GridAction.TakeCounter,source.Id,bowl);PrepDo(client,GridAction.PortionSoup,stove.Id,bowl,pot,batch.Id);
                return bowl;
            }
            long toast=DishBake(client);GridStation oven=PrepStation("烤箱"),plateSource=PrepStation("盘台");long plate=plateSource.ItemId;
            PrepDo(client,GridAction.TakeCounter,oven.Id,toast);PrepDo(client,GridAction.AddToContainer,plateSource.Id,toast,plate);
            PrepDo(client,GridAction.TakeCounter,plateSource.Id,plate);return plate;
        }

        private Order WaitActualOrder(int dish)
        {
            FrontWait(()=>CurrentLevel.Children.Values.OfType<Order>().Any(value=>value.State==OrderState.Waiting && value.DishId==dish),300);
            return CurrentLevel.Children.Values.OfType<Order>().First(value=>value.State==OrderState.Waiting && value.DishId==dish);
        }

        private ServeOrderResponse ServeReply(ServeOrderRequest request)
        {
            var task=((ClientSessionComponent)host.Client).CallProduct(request);PumpMessages(8);
            Assert.That(task.Status,Is.Not.EqualTo(UniTaskStatus.Pending));return (ServeOrderResponse)task.GetAwaiter().GetResult();
        }

        private void ServeDo(Order order,long item,string rejection=null,bool move=true)
        {
            ClientSessionComponent client=host.Client;GridStation table=Grid.Station(order.TableStationId);
            if(move)PrepDo(client,GridAction.Move,cell:table.StationApproaches().First(Grid.IsWalkable));
            using(var reply=ServeReply(client.GetComponent<GridClientComponent>().CreateServeOrder(order.Id,order.CustomerId,table.Id,item)))Assert.That(reply.Error,Is.Zero,reply.Message);
            AdvanceTicks(1);PumpMessages(8);
            Assert.That(FrontPeer.LastCommandStatus,Is.EqualTo(rejection==null?GridResultStatus.Executed:GridResultStatus.Rejected),FrontPeer.LastCommandReason);
            Assert.That(FrontPeer.LastCommandReason,Is.EqualTo(rejection));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void SoloCustomerServeBothRealDishesConsumeFoodOnceAndKeepOriginalUtensil(int dish)
        {
            StartSolo(liveFront:true);long item=PrepareServingMeal(dish);long food=Grid.Item(item).Contents.Single();
            LevelDo(LevelControlAction.Start);Order order=WaitActualOrder(dish);GridStation table=Grid.Station(order.TableStationId);
            ServeDo(order,item);
            Customer customer=CurrentLevel.GetChild<Customer>(order.CustomerId);ClientSessionComponent client=host.Client;
            Assert.That(order.State,Is.EqualTo(OrderState.Served));Assert.That(order.ServedItemId,Is.EqualTo(item));Assert.That(CurrentLevel.Score,Is.EqualTo(order.Score));
            Assert.That(Grid.Item(food),Is.Null);Assert.That(Grid.Item(item).Contents,Is.Empty);Assert.That(Grid.Item(item).Parent,Is.SameAs(Grid));
            Assert.That(Grid.Item(item).Location,Is.EqualTo(GridItemLocation.Table));Assert.That(table.ItemId,Is.EqualTo(item));
            Assert.That(customer.Phase,Is.EqualTo(CustomerPhase.Eating));Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.Zero);
            var projection=client.GetComponent<GridClientComponent>();Assert.That(projection.LevelScore,Is.EqualTo(order.Score));
            Assert.That(projection.Orders[order.Id].State,Is.EqualTo(OrderState.Served));Assert.That(projection.Items[item].Location,Is.EqualTo(GridItemLocation.Table));
            Assert.That(projection.Items.ContainsKey(food),Is.False);
            ServeDo(order,item,"OrderEnded",move:false);
            Assert.That(CurrentLevel.Score,Is.EqualTo(order.Score));Assert.That(Grid.Item(item),Is.Not.Null);
        }

        [Test]
        public void SoloCustomerServeLatestReplayAndPayloadConflictNeverConsumeOrScoreTwice()
        {
            StartSolo(liveFront:true);long bowl=PrepareServingMeal(1);LevelDo(LevelControlAction.Start);Order order=WaitActualOrder(1);
            ClientSessionComponent client=host.Client;var projection=client.GetComponent<GridClientComponent>();GridStation table=Grid.Station(order.TableStationId);
            PrepDo(client,GridAction.Move,cell:table.StationApproaches().First(Grid.IsWalkable));
            var request=projection.CreateServeOrder(order.Id,order.CustomerId,table.Id,bowl);
            long command=request.CommandId;ProductCommandContext context=new(request.MatchId,request.WorldId,request.WorldGeneration,request.LevelId,request.LevelGeneration,request.BindingGeneration,command);
            using(var reply=ServeReply(request))Assert.That(reply.Status,Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(order.State,Is.EqualTo(OrderState.Waiting));Assert.That(CurrentLevel.Score,Is.Zero);
            AdvanceTicks(1);PumpMessages(8);
            ServeOrderRequest Replay(long item)=>ProductMessages.Create<ServeOrderRequest>(value=>
            {value.ProtocolVersion=ProductSessionRules.ProtocolVersion;value.MatchId=context.MatchId;value.WorldId=context.WorldId;value.WorldGeneration=context.WorldGeneration;
                value.LevelId=context.LevelId;value.LevelGeneration=context.LevelGeneration;value.BindingGeneration=context.BindingGeneration;value.CommandId=context.CommandId;
                value.OrderId=order.Id;value.CustomerId=order.CustomerId;value.TableStationId=table.Id;value.ItemId=item;});
            using(var reply=ServeReply(Replay(bowl)))Assert.That(reply.Status,Is.EqualTo((int)GridResultStatus.Executed));
            using(var reply=ServeReply(Replay(bowl+1)))Assert.That(reply.Message,Is.EqualTo("CommandConflict"));
            AdvanceTicks(1);Assert.That(CurrentLevel.Score,Is.EqualTo(order.Score));Assert.That(table.ItemId,Is.EqualTo(bowl));
        }

        [TestCase("dish")]
        [TestCase("position")]
        [TestCase("dirty")]
        [TestCase("customer")]
        public void SoloCustomerServeInvalidPayloadAndPositionKeepFoodOrderAndScore(string invalid)
        {
            StartSolo(liveFront:true);long item=PrepareServingMeal(invalid=="dish"?2:1);LevelDo(LevelControlAction.Start);Order order=WaitActualOrder(1);
            GridItem utensil=Grid.Item(item);long food=utensil.Contents.Single();GridStation table=Grid.Station(order.TableStationId);
            if(invalid=="dirty")utensil.Hygiene=ItemHygiene.Dirty;
            if(invalid=="customer")
            {
                PrepDo(host.Client,GridAction.Move,cell:table.StationApproaches().First(Grid.IsWalkable));
                var projection=((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
                using(var reply=ServeReply(projection.CreateServeOrder(order.Id,order.CustomerId+1,table.Id,item)))Assert.That(reply.Error,Is.Zero);
                AdvanceTicks(1);PumpMessages(8);Assert.That(FrontPeer.LastCommandReason,Is.EqualTo("OrderChanged"));
            }
            else ServeDo(order,item,invalid=="dish"?"WrongDish":invalid=="dirty"?"DirtyUtensil":"RequiresTablePosition",move:invalid!="position");
            Assert.That(utensil.Contents,Is.EqualTo(new[]{food}));Assert.That(Grid.Item(food),Is.Not.Null);
            Assert.That(Grid.Actor(((ClientSessionComponent)host.Client).MemberId).HandItemId,Is.EqualTo(item));
            Assert.That(table.ItemId,Is.Zero);Assert.That(order.State,Is.EqualTo(OrderState.Waiting));Assert.That(CurrentLevel.Score,Is.Zero);
        }

        [Test]
        public void SoloCustomerServeStaleOrderCannotServeNextCustomerAtSameTable()
        {
            ConfigureFrontFlow(interval:100000,wait:80);StartSolo(liveFront:true);long bowl=PrepareServingMeal(1);
            LevelDo(LevelControlAction.Start);Order old=WaitActualOrder(1);Customer departed=CurrentLevel.GetChild<Customer>(old.CustomerId);
            FrontWait(()=>departed.Phase==CustomerPhase.Gone);GridStation table=Grid.Station(old.TableStationId);
            var state=CurrentLevel.GetComponent<FrontLevelState>();state.NextArrivalOperatingTick=CurrentLevel.ElapsedOperatingTicks+1;
            FrontWait(()=>CurrentLevel.Children.Values.OfType<Order>().Any(value=>value.State==OrderState.Waiting && value.Id!=old.Id));
            Order next=CurrentLevel.Children.Values.OfType<Order>().First(value=>value.State==OrderState.Waiting);
            Assert.That(next.TableStationId,Is.EqualTo(table.Id));ServeDo(old,bowl,"OrderChanged");
            Assert.That(next.State,Is.EqualTo(OrderState.Waiting));Assert.That(Grid.Item(bowl).Contents.Count,Is.EqualTo(1));Assert.That(CurrentLevel.Score,Is.Zero);
        }

        [TestCase("add")]
        [TestCase("beat")]
        [TestCase("pour")]
        [TestCase("portion")]
        [TestCase("cook")]
        public void SoloCustomerUtensilsDirtyContainersCannotPreparePourCookOrPortion(string operation)
        {
            StartSolo();ClientSessionComponent client=host.Client;GridStation bowl=PrepStation("碗台"),stove=PrepStation("灶台");
            long bowlId=bowl.ItemId,pot=stove.ItemId;
            if(operation=="add")
            {
                GridStation source=PrepStation("食材台");long egg=source.ItemId;PrepDo(client,GridAction.TakeCounter,source.Id,egg);
                Grid.Item(bowlId).Hygiene=ItemHygiene.Dirty;
                PrepDo(client,GridAction.AddToContainer,bowl.Id,egg,bowlId,rejection:"DirtyUtensil");
                Assert.That(Grid.Item(bowlId).Contents,Is.Empty);Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.EqualTo(egg));
            }
            else if(operation is "beat" or "pour")
            {
                bowlId=PrepEggBowl(client);
                if(operation=="pour")
                {
                    PrepDo(client,GridAction.StartHeldWork,bowlId,Grid.Item(bowlId).Contents.Single(),bowlId);AdvanceTicks(60);
                }
                long content=Grid.Item(bowlId).Contents.Single();Grid.Item(bowlId).Hygiene=ItemHygiene.Dirty;
                if(operation=="beat")PrepDo(client,GridAction.StartHeldWork,bowlId,content,bowlId,rejection:"DirtyUtensil");
                else PrepDo(client,GridAction.PourContents,stove.Id,bowlId,pot,rejection:"DirtyUtensil");
                Assert.That(Grid.Item(bowlId).Contents,Is.EqualTo(new[]{content}));Assert.That(Grid.Item(pot).Contents,Is.Empty);
            }
            else
            {
                pot=SoupFillPot(client);
                if(operation=="cook")
                {
                    long[] ingredients=Grid.Item(pot).Contents.ToArray();Grid.Item(pot).Hygiene=ItemHygiene.Dirty;
                    PrepDo(client,GridAction.StartAutoWork,stove.Id,pot,pot,rejection:"DirtyUtensil");
                    Assert.That(Grid.Item(pot).Contents,Is.EqualTo(ingredients));Assert.That(Grid.Item(pot).AutoProcessId,Is.Zero);
                }
                else
                {
                    GridAutoProcess batch=SoupBegin(client,pot);AdvanceTicks(batch.RequiredTicks);
                    bowlId=bowl.ItemId;PrepDo(client,GridAction.TakeCounter,bowl.Id,bowlId);Grid.Item(bowlId).Hygiene=ItemHygiene.Dirty;
                    PrepDo(client,GridAction.PortionSoup,stove.Id,bowlId,pot,batch.Id,rejection:"DirtyUtensil");
                    Assert.That(Grid.Item(bowlId).Contents,Is.Empty);Assert.That(batch.RemainingPortions,Is.EqualTo(2));
                }
            }
            Assert.That(CurrentLevel.Score,Is.Zero);
        }

        [Test]
        public void SoloCustomerServePauseCancelsAcceptedUnexecutedDeliveryAndResumeCannotReviveIt()
        {
            ConfigureFrontFlow(interval:100000);StartSolo(liveFront:true);long bowl=PrepareServingMeal(1);LevelDo(LevelControlAction.Start);
            Order order=WaitActualOrder(1);GridStation table=Grid.Station(order.TableStationId);ClientSessionComponent client=host.Client;
            PrepDo(client,GridAction.Move,cell:table.StationApproaches().First(Grid.IsWalkable));long food=Grid.Item(bowl).Contents.Single();
            using(var reply=ServeReply(client.GetComponent<GridClientComponent>().CreateServeOrder(order.Id,order.CustomerId,table.Id,bowl)))
                Assert.That(reply.Status,Is.EqualTo((int)GridResultStatus.Accepted));
            Assert.That(CurrentLevel.GetComponent<FrontLevelState>().Serves.Count,Is.EqualTo(1));
            LevelDo(LevelControlAction.Pause);AdvanceTicks(30);
            Assert.That(CurrentLevel.GetComponent<FrontLevelState>().Serves,Is.Empty);Assert.That(order.State,Is.EqualTo(OrderState.Waiting));
            Assert.That(Grid.Item(bowl).Contents,Is.EqualTo(new[]{food}));Assert.That(CurrentLevel.Score,Is.Zero);
            LevelDo(LevelControlAction.Resume);AdvanceTicks(1);
            Assert.That(order.State,Is.EqualTo(OrderState.Waiting));Assert.That(Grid.Actor(client.MemberId).HandItemId,Is.EqualTo(bowl));
        }

        [Test]
        public void SoloCustomerUtensilsActualEatClearWashAndTakePreserveSameBowlAndStock()
        {
            ConfigureFrontFlow(interval:100000);StartSolo(liveFront:true);long bowl=PrepareServingMeal(1);
            long[] utensils=Grid.Items.Values.Select(value=>(GridItem)value).Where(value=>value.DefinitionId is 5 or 10).Select(value=>value.Id).OrderBy(value=>value).ToArray();
            LevelDo(LevelControlAction.Start);Order order=WaitActualOrder(1);ServeDo(order,bowl);
            Customer customer=CurrentLevel.GetChild<Customer>(order.CustomerId);GridStation table=Grid.Station(order.TableStationId);
            AdvanceTicks(59);Assert.That(customer.Phase,Is.EqualTo(CustomerPhase.Eating));Assert.That(table.GetComponent<FrontTable>().State,Is.EqualTo(FrontTableState.Occupied));
            Assert.That(table.ItemId,Is.EqualTo(bowl));FrontWait(()=>customer.Phase==CustomerPhase.Gone);
            Assert.That(Grid.Item(bowl).Hygiene,Is.EqualTo(ItemHygiene.Dirty));Assert.That(table.GetComponent<FrontTable>().State,Is.EqualTo(FrontTableState.Dirty));
            FrontWork clear=CurrentLevel.Children.Values.OfType<FrontWork>().Single(value=>value.Kind==FrontWorkKind.Clear && value.ItemId==bowl);
            FrontPlayerDo(FrontWorkAction.Claim,clear);FrontWait(()=>clear.State==FrontWorkState.Completed);
            Assert.That(table.ItemId,Is.Zero);Assert.That(table.GetComponent<FrontTable>().State,Is.EqualTo(FrontTableState.Free));
            FrontWork wash=CurrentLevel.Children.Values.OfType<FrontWork>().Single(value=>value.Kind==FrontWorkKind.Wash && value.ItemId==bowl);
            Assert.That(Grid.Item(bowl).Location,Is.EqualTo(GridItemLocation.DirtyPool));FrontWait(()=>wash.State==FrontWorkState.Completed);
            GridStation clean=Grid.Station(Grid.Item(bowl).HolderId);Assert.That(clean.Kind,Is.EqualTo(GridStationKind.CleanPool));
            PrepDo(host.Client,GridAction.TakeCounter,clean.Id,bowl);
            Assert.That(Grid.Actor(((ClientSessionComponent)host.Client).MemberId).HandItemId,Is.EqualTo(bowl));Assert.That(clean.PoolItemIds,Is.Empty);
            Assert.That(Grid.Item(bowl).Hygiene,Is.EqualTo(ItemHygiene.Clean));Assert.That(Grid.Item(bowl).Contents,Is.Empty);
            Assert.That(Grid.Items.Values.Select(value=>(GridItem)value).Where(value=>value.DefinitionId is 5 or 10).Select(value=>value.Id).OrderBy(value=>value),Is.EqualTo(utensils));
            GridItem pot=Grid.Item(PrepStation("灶台").ItemId);PrepDo(host.Client,GridAction.PortionSoup,PrepStation("灶台").Id,bowl,pot.Id,pot.AutoProcessId);
            Assert.That(Grid.IdentifyDish(bowl),Is.EqualTo(1));
        }

        [Test]
        public void SoloCustomerDrainLastActualWashBlocksCompletionAndNextLevelKeepsCleanPool()
        {
            ConfigureFrontFlow(interval:100000);StartSolo(liveFront:true);long bowl=PrepareServingMeal(1);LevelDo(LevelControlAction.Start);
            Order order=WaitActualOrder(1);ServeDo(order,bowl);Customer customer=CurrentLevel.GetChild<Customer>(order.CustomerId);
            FrontWait(()=>customer.Phase==CustomerPhase.Gone);
            AdvanceTicks(CurrentLevel.OperatingTicks-CurrentLevel.ElapsedOperatingTicks);
            Assert.That(CurrentLevel.Phase,Is.EqualTo(LevelPhase.Draining));
            FrontWork clear=CurrentLevel.Children.Values.OfType<FrontWork>().Single(value=>value.Kind==FrontWorkKind.Clear && value.ItemId==bowl);
            FrontPlayerDo(FrontWorkAction.Claim,clear);FrontWait(()=>clear.State==FrontWorkState.Completed);
            FrontWork wash=CurrentLevel.Children.Values.OfType<FrontWork>().Single(value=>value.Kind==FrontWorkKind.Wash && value.ItemId==bowl);
            FrontWait(()=>wash.State==FrontWorkState.Working);AdvanceTicks(7);double progress=wash.Progress;
            Assert.That(CurrentLevel.Phase,Is.EqualTo(LevelPhase.Draining));LevelDo(LevelControlAction.Pause);AdvanceTicks(100);
            Assert.That(wash.Progress,Is.EqualTo(progress));Assert.That(CurrentLevel.Phase,Is.EqualTo(LevelPhase.Draining));
            LevelDo(LevelControlAction.Resume);FrontWait(()=>CurrentLevel.Phase==LevelPhase.Completed);
            Assert.That(wash.State,Is.EqualTo(FrontWorkState.Completed));Assert.That(Grid.Item(bowl).Hygiene,Is.EqualTo(ItemHygiene.Clean));
            Assert.That(CurrentLevel.Score,Is.EqualTo(10));Assert.That(CurrentLevel.Stars,Is.EqualTo(1));
            GridStation pool=Grid.Station(Grid.Item(bowl).HolderId);long poolId=pool.Id;LevelDo(LevelControlAction.Continue);
            Assert.That(Grid.Item(bowl).HolderId,Is.EqualTo(poolId));Assert.That(pool.PoolItemIds,Is.EqualTo(new[]{bowl}));
            Assert.That(CurrentLevel.GetComponent<CompanionGrowth>().CompletedByCompanion,Is.Empty);
            LayoutDo(pool,new GridCell(4,6));Assert.That(Grid.Item(bowl).Cell,Is.EqualTo(pool.Cell));Assert.That(pool.PoolItemIds,Is.EqualTo(new[]{bowl}));
            PrepDo(host.Client,GridAction.TakeCounter,pool.Id,bowl);Assert.That(Grid.Item(bowl).Location,Is.EqualTo(GridItemLocation.Hand));
        }
    }
}
