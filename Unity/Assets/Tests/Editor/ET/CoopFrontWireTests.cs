using System;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void CoopFrontStart()
        {
            BuffDo(Buff.Candidates[0]);
            LevelDo(LevelControlAction.Start);
        }

        private (int Error, string Message, int Status, long Command, int Rpc) CoopFrontCall(ClientSessionComponent client, IRequest request)
        {
            var task = client.GetAsyncOwner().AsyncScope.RunAsync(context => client.CallProductValueAsync(context, request, value =>
            {
                if (value is ServeOrderResponse serve) return (serve.Error, serve.Message, serve.Status, serve.CommandId, serve.RpcId);
                var front = (FrontWorkResponse)value;
                return (front.Error, front.Message, front.Status, front.CommandId, front.RpcId);
            }));
            return Finish(task, 500);
        }

        private void CoopFrontDo(ClientSessionComponent client, FrontWorkAction action, FrontWork work, string rejection = null, long? serial = null)
        {
            var projection = client.GetComponent<GridClientComponent>();
            FrontWorkRequest request = projection.CreateFrontWorkCommand(action, work.Id, serial ?? work.ClaimSerial);
            long command = request.CommandId;
            var receipt = CoopFrontCall(client, request);
            Assert.That(receipt.Error, Is.Zero, receipt.Message);
            Assert.That(receipt.Command, Is.EqualTo(command));
            Assert.That(receipt.Rpc, Is.GreaterThan(0));
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(projection.LastCommandId, Is.EqualTo(command));
            Assert.That(projection.LastStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), projection.LastReason);
            Assert.That(projection.LastReason, Is.EqualTo(rejection));
        }

        private Order CoopFrontAsk(ClientSessionComponent client, int sequence = 1)
        {
            Customer customer = InitialSeatedCustomer(sequence);
            FrontWork ask = CurrentLevel.GetChild<FrontWork>(customer.AskWorkId);
            CoopFrontDo(client, FrontWorkAction.Claim, ask);
            FrontWait(() => ask.State == FrontWorkState.Completed);
            Assert.That(customer.Phase, Is.EqualTo(CustomerPhase.Ordered));
            return CurrentLevel.GetChild<Order>(customer.OrderId);
        }

        private long CoopFrontMeal(ClientSessionComponent local, ClientSessionComponent remote, int dish)
        {
            if (dish == 1)
            {
                GridAutoProcess batch = CoopSoup(local, remote);
                AdvanceTicks(batch.RequiredTicks); PumpMessages(8);
                GridStation source = PrepStation("碗台"), stove = PrepStation("灶台");
                long bowl = source.ItemId;
                PrepDo(remote, GridAction.TakeCounter, source.Id, bowl);
                PrepDo(remote, GridAction.PortionSoup, stove.Id, bowl, batch.PotItemId, batch.Id);
                return bowl;
            }
            long toast = DishBake(local);
            GridStation oven = PrepStation("烤箱"), plate = PrepStation("盘台");
            PrepDo(local, GridAction.TakeCounter, oven.Id, toast);
            PrepDo(local, GridAction.AddToContainer, plate.Id, toast, plate.ItemId);
            long item = plate.ItemId;
            PrepDo(remote, GridAction.TakeCounter, plate.Id, item);
            return item;
        }

        private (ProductCommandContext Context, ServeOrderIntent Intent) CoopFrontServe(ClientSessionComponent client, Order order, long item, string rejection = null)
        {
            GridStation table = Grid.Station(order.TableStationId);
            PrepDo(client, GridAction.Move, cell: table.StationApproaches().First(Grid.IsWalkable));
            var view = client.GetComponent<GridClientComponent>();
            var request = view.CreateServeOrder(order.Id, order.CustomerId, table.Id, item);
            var context = new ProductCommandContext(request.MatchId, request.WorldId, request.WorldGeneration, request.LevelId, request.LevelGeneration, request.BindingGeneration, request.CommandId);
            var intent = new ServeOrderIntent(order.Id, order.CustomerId, table.Id, item);
            var receipt = CoopFrontCall(client, request);
            Assert.That(receipt.Error, Is.Zero, receipt.Message);
            Assert.That(receipt.Command, Is.EqualTo(context.CommandId));
            Assert.That(receipt.Rpc, Is.GreaterThan(0));
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(view.LastStatus, Is.EqualTo(rejection == null ? GridResultStatus.Executed : GridResultStatus.Rejected), view.LastReason);
            Assert.That(view.LastReason, Is.EqualTo(rejection));
            return (context, intent);
        }

        private ServeOrderRequest CoopFrontReplay(ProductCommandContext context, ServeOrderIntent intent, long? item = null)
            => ProductMessages.Create<ServeOrderRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = context.MatchId; value.WorldId = context.WorldId; value.WorldGeneration = context.WorldGeneration;
                value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration; value.BindingGeneration = context.BindingGeneration; value.CommandId = context.CommandId;
                value.OrderId = intent.OrderId; value.CustomerId = intent.CustomerId; value.TableStationId = intent.TableStationId; value.ItemId = item ?? intent.ItemId;
            });

        [TestCase(1)]
        [TestCase(2)]
        public void CoopFrontWireRealOwnedKcpAskAndServeBothDishesOnce(int dish)
        {
            ConfigureFrontFlow(interval: 100000, wait: 2000, firstArrival: 100000);
            StartHost(); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            long meal = CoopFrontMeal(local, remote, dish), food = Grid.Item(meal).Contents.Single();
            GridStation handoff = PrepStation("交接台");
            PrepDo(remote, GridAction.PlaceCounter, handoff.Id, meal);
            CoopFrontStart(); FrontRole.NextScheduleTick = 100000;
            Order order = CoopFrontAsk(remote, dish);
            PrepDo(remote, GridAction.TakeCounter, handoff.Id, meal);
            var served = CoopFrontServe(remote, order, meal);
            Assert.That(order.State, Is.EqualTo(OrderState.Served)); Assert.That(CurrentLevel.Score, Is.EqualTo(order.Score));
            Assert.That(Grid.Item(food), Is.Null); Assert.That(Grid.Item(meal).Contents, Is.Empty);
            Assert.That(Grid.Item(meal).Location, Is.EqualTo(GridItemLocation.Table));
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.Zero);
            var replay = CoopFrontCall(remote, CoopFrontReplay(served.Context, served.Intent));
            Assert.That(replay.Error, Is.Zero); Assert.That(replay.Status, Is.EqualTo((int)GridResultStatus.Executed));
            var conflict = CoopFrontCall(remote, CoopFrontReplay(served.Context, served.Intent, meal + 1));
            Assert.That(conflict.Message, Is.EqualTo("CommandConflict"));
            AdvanceTicks(1); PumpMessages(8);
            Assert.That(CurrentLevel.Score, Is.EqualTo(order.Score));
            foreach (ClientSessionComponent observer in new[] { local, remote })
            {
                var projection = observer.GetComponent<GridClientComponent>();
                Assert.That(projection.LevelScore, Is.EqualTo(order.Score));
                Assert.That(projection.Orders[order.Id].State, Is.EqualTo(OrderState.Served));
                Assert.That(projection.Items.ContainsKey(food), Is.False);
                Assert.That(projection.Items[meal].Location, Is.EqualTo(GridItemLocation.Table));
            }
        }

        [TestCase("dish")]
        [TestCase("customer")]
        public void CoopFrontWireWrongDishOrCustomerKeepsOriginalMealAndScore(string invalid)
        {
            ConfigureFrontFlow(interval: 100000, wait: 2000, firstArrival: 100000);
            StartHost(); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            long meal = CoopFrontMeal(local, remote, invalid == "dish" ? 2 : 1);
            GridStation handoff = PrepStation("交接台"); PrepDo(remote, GridAction.PlaceCounter, handoff.Id, meal);
            CoopFrontStart(); FrontRole.NextScheduleTick = 100000;
            Order order = CoopFrontAsk(remote); PrepDo(remote, GridAction.TakeCounter, handoff.Id, meal);
            long[] contents = Grid.Item(meal).Contents.ToArray();
            if (invalid == "dish") CoopFrontServe(remote, order, meal, "WrongDish");
            else
            {
                GridStation table = Grid.Station(order.TableStationId);
                PrepDo(remote, GridAction.Move, cell: table.StationApproaches().First(Grid.IsWalkable));
                var view = remote.GetComponent<GridClientComponent>();
                var receipt = CoopFrontCall(remote, view.CreateServeOrder(order.Id, order.CustomerId + 1, table.Id, meal));
                Assert.That(receipt.Error, Is.Zero); AdvanceTicks(1); PumpMessages(8);
                Assert.That(view.LastReason, Is.EqualTo("OrderChanged"));
            }
            Assert.That(CurrentLevel.Score, Is.Zero); Assert.That(order.State, Is.EqualTo(OrderState.Waiting));
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(meal));
            Assert.That(Grid.Item(meal).Contents, Is.EqualTo(contents));
            Assert.That(Grid.Station(order.TableStationId).ItemId, Is.Zero);
            Assert.That(remote.Stage, Is.EqualTo(ProductClientStage.Joined));
        }

        [Test]
        public void CoopFrontWireOldRequestCannotServeNewCustomerAtSameActualTable()
        {
            ConfigureFrontFlow(interval: 100000, wait: 120, firstArrival: 100000);
            StartHost(); var remote = CoopRemote(); ClientSessionComponent local = host.Client;
            long meal = CoopFrontMeal(local, remote, 1); var handoff = PrepStation("交接台");
            PrepDo(remote, GridAction.PlaceCounter, handoff.Id, meal);
            CoopFrontStart(); FrontRole.NextScheduleTick = 100000;
            Order old = CoopFrontAsk(remote); PrepDo(remote, GridAction.TakeCounter, handoff.Id, meal);
            GridStation table = Grid.Station(old.TableStationId);
            PrepDo(remote, GridAction.Move, cell: table.StationApproaches().First(Grid.IsWalkable));
            var view = remote.GetComponent<GridClientComponent>();
            ServeOrderRequest delayed = view.CreateServeOrder(old.Id, old.CustomerId, table.Id, meal);
            Customer previous = CurrentLevel.GetChild<Customer>(old.CustomerId);
            FrontWait(() => previous.Phase == CustomerPhase.Gone);
            Assert.That(old.State, Is.EqualTo(OrderState.Expired));
            Order next = CoopFrontAsk(local, 2);
            Assert.That(next.TableStationId, Is.EqualTo(table.Id)); Assert.That(next.CustomerId, Is.Not.EqualTo(old.CustomerId));
            var receipt = CoopFrontCall(remote, delayed);
            Assert.That(receipt.Error, Is.Zero); AdvanceTicks(1); PumpMessages(8);
            Assert.That(view.LastReason, Is.EqualTo("OrderChanged"));
            Assert.That(next.State, Is.EqualTo(OrderState.Waiting)); Assert.That(CurrentLevel.Score, Is.Zero);
            Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(meal));
            Assert.That(Grid.Item(meal).Contents.Count, Is.EqualTo(1)); Assert.That(table.ItemId, Is.Zero);
        }

        [TestCase("serve-submit")]
        [TestCase("front-submit")]
        [TestCase("companion-request")]
        [TestCase("companion-result")]
        public void CoopFrontWireInternalMessagesCannotEnterAuthority(string kind)
        {
            StartHost(); var remote = CoopRemote();
            MessageObject message = kind switch
            {
                "serve-submit" => ProductMessages.Create<ServeOrderSubmitRequest>(_ => { }),
                "front-submit" => ProductMessages.Create<FrontWorkSubmitRequest>(_ => { }),
                "companion-request" => ProductMessages.Create<CompanionWorkRequest>(_ => { }),
                _ => ProductMessages.Create<CompanionWorkResult>(_ => { })
            };
            ProductRuntimeComponent runtime = remote.Runtime; Session session = remote.Session;
            session.Send((IMessage)message); message.Dispose(); Pump(150);
            Assert.That(remote.Stage, Is.EqualTo(ProductClientStage.Stopped));
            Assert.That(session.IsDisposed, Is.True); Assert.That(runtime.Failure, Is.Not.Null.And.Not.Empty);
            Assert.That(CurrentLevel.Score, Is.Zero); Assert.That(CurrentLevel.Children.Values.OfType<Order>(), Is.Empty);
            Assert.That(((ClientSessionComponent)host.Client).Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That(host.Root().GetComponent<ProcessInnerSender>().IsStopped, Is.False);
        }
    }
}
