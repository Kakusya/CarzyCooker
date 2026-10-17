using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(CooperativeOrderIntents))]
    public sealed partial class ProductSessionTests
    {
        private CooperativeOrderIntents IntentState => CurrentLevel.GetComponent<CooperativeOrderIntents>();

        private ClientSessionComponent IntentFixture()
        {
            var rows = (System.Collections.Generic.List<DRCustomerFlowConfig>)Tables.Instance.DTCustomerFlowConfig.DataList;
            var map = (System.Collections.Generic.Dictionary<int, DRCustomerFlowConfig>)Tables.Instance.DTCustomerFlowConfig.DataMap;
            int first = RestaurantConfig.FirstCooperativeLevel(ProductMenuVariant.Basic).Id;
            var original = rows.Single(value => value.LevelConfigId == first);
            var buffer = new Luban.ByteBuf();
            foreach (int value in new[] { original.Id, original.LevelConfigId, 100000, 100000, original.QueueCapacity, 4000,
                original.EatTicks, original.EntranceX, original.EntranceY, original.QueueX, original.QueueY })
            {
                buffer.WriteInt(value);
            }
            buffer.WriteSize(original.OrderConfigIds.Count);
            foreach (int id in original.OrderConfigIds)
            {
                buffer.WriteInt(id);
            }
            var replacement = new DRCustomerFlowConfig(buffer);
            rows[rows.IndexOf(original)] = replacement; map[replacement.Id] = replacement;
            StartHost();
            var remote = CoopRemote();
            CoopFrontStart(); FrontRole.NextScheduleTick = 100000;
            return remote;
        }

        private OrderIntentResponse IntentReply(ClientSessionComponent client, OrderIntentRequest request)
            => (OrderIntentResponse)Finish(client.CallProduct(request), 500);

        private void IntentDo(ClientSessionComponent client, OrderIntentAction action, Order order = null, string rejected = null)
        {
            using var reply = IntentReply(client, client.GetComponent<GridClientComponent>().CreateOrderIntent(action, order?.Id ?? 0, order?.CustomerId ?? 0));
            Assert.That(reply.Error == 0, Is.EqualTo(rejected == null), reply.Message);
            Assert.That(reply.Message, Is.EqualTo(rejected));
            if (rejected == null)
            {
                Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed));
            }
            Pump(12);
        }

        [Test, Category("OrderIntents")]
        public void EntryOrderIntentActualKcpTwoMembersSameOrderThirdCooksServesOnceAndCopiesIndependentValues()
        {
            var remote = IntentFixture();
            ClientSessionComponent local = host.Client;
            var third = CoopRemote();
            Order order = CoopFrontAsk(remote);
            IntentDo(local, OrderIntentAction.Set, order);
            IntentDo(remote, OrderIntentAction.Set, order);
            Assert.That(IntentState.Members.Count, Is.EqualTo(2));
            var remoteGrid = remote.GetComponent<GridClientComponent>();
            PumpUntil(() => remoteGrid.OrderIntents.Count == 2, "Actual KCP receives both non-exclusive intents", 100, true);
            var peer = (HostPeer)((Match)Grid.Match).GetChild<Participant>(remote.MemberId).BoundPeer;
            using (var dto = GridBroadcast.CreateSnapshot(Grid, peer))
            {
                dto.Version = remoteGrid.Version + 1;
                remoteGrid.ApplyGridSnapshot(dto);
                dto.OrderIntents[0].OrderId = -1;
                dto.OrderIntents.Clear();
            }
            Assert.That(remoteGrid.OrderIntents.All(value => value.OrderId == order.Id), Is.True);
            Assert.That(remoteGrid.OrderIntents.Count, Is.EqualTo(2));
            long bowl = CoopFrontMeal(local, third, 1);
            Assert.That(Grid.IdentifyDish(bowl), Is.EqualTo(order.DishId));
            Assert.That(IntentState.Members.Count, Is.EqualTo(2), "Real chopping, egg work, cooking and portioning do not reserve this order");
            CoopFrontServe(third, order, bowl);
            Assert.That(order.State, Is.EqualTo(OrderState.Served));
            Assert.That(CurrentLevel.Score, Is.EqualTo(order.Score));
            Assert.That(CurrentLevel.ServedOrders, Is.EqualTo(1));
            Assert.That(IntentState.Members, Is.Empty);
            PumpUntil(() => remoteGrid.OrderIntents.Count == 0, "Both intents cleared after original third-member delivery", 100, true);
            IntentDo(local, OrderIntentAction.Set, order, "OrderEnded");
            Assert.That(CurrentLevel.ServedOrders, Is.EqualTo(1));
        }

        [Test, Category("OrderIntents")]
        public void EntryOrderIntentActualReplaceCancelPauseDisconnectResumeSameTableExpiryAndNextLevelRejectOldIdentity()
        {
            var remote = IntentFixture();
            ClientSessionComponent local = host.Client;
            Order first = CoopFrontAsk(local);
            Customer secondCustomer = InitialSeatedCustomer(2, 12);
            FrontWork secondAsk = CurrentLevel.GetChild<FrontWork>(secondCustomer.AskWorkId);
            CoopFrontDo(remote, FrontWorkAction.Claim, secondAsk);
            FrontWait(() => secondAsk.State == FrontWorkState.Completed);
            Order second = CurrentLevel.GetChild<Order>(secondCustomer.OrderId);
            IntentDo(local, OrderIntentAction.Set, first);
            IntentDo(remote, OrderIntentAction.Set, first);
            IntentDo(local, OrderIntentAction.Set, second);
            Assert.That(IntentState.Members[local.MemberId].OrderId, Is.EqualTo(second.Id));
            Assert.That(IntentState.Members[remote.MemberId].OrderId, Is.EqualTo(first.Id));
            IntentDo(local, OrderIntentAction.Clear);
            Assert.That(IntentState.Members.Keys, Is.EqualTo(new[] { remote.MemberId }));
            LevelDo(LevelControlAction.Pause);
            IntentDo(remote, OrderIntentAction.Clear, rejected: "Paused");
            Assert.That(IntentState.Members.Count, Is.EqualTo(1));
            LevelDo(LevelControlAction.Resume);
            long binding = remote.BindingGeneration;
            var staleBinding = remote.GetComponent<GridClientComponent>().CreateOrderIntent(OrderIntentAction.Set, second.Id, second.CustomerId);
            ProductRuntimeComponent remoteRuntime = remote.Runtime;
            ((Session)remote.Session).Dispose();
            Pump(240);
            Assert.That(IntentState.Members, Is.Empty);
            var resume = remoteRuntime.ResumeProduct();
            PumpUntil(() => resume.Status != UniTaskStatus.Pending, "Actual KCP resume for intents", 500, true);
            resume.GetAwaiter().GetResult();
            remote = remoteRuntime.Client;
            Assert.That(remote.BindingGeneration, Is.GreaterThan(binding));
            Assert.That(IntentState.Members, Is.Empty);
            using (var reply = IntentReply(remote, staleBinding))
            {
                Assert.That(reply.Error, Is.Not.Zero);
            }
            IntentDo(remote, OrderIntentAction.Set, first);
            var oldLevel = local.GetComponent<GridClientComponent>().CreateOrderIntent(OrderIntentAction.Set, first.Id, first.CustomerId);
            first.DeadlineTick = Grid.Tick + 5;
            FrontWait(() => first.State == OrderState.Expired, 3000);
            Assert.That(IntentState.Members, Is.Empty);
            // Reuse the actual original table only after its natural exit and clear work.
            FrontRole.NextScheduleTick = 0;
            FrontWait(() => Grid.Station(first.TableStationId).GetComponent<FrontTable>().State == FrontTableState.Free, 2000);
            Order replacement = CoopFrontAsk(local, 1);
            Assert.That(replacement.Id, Is.Not.EqualTo(first.Id));
            IntentDo(remote, OrderIntentAction.Set, first, "OrderEnded");
            IntentDo(remote, OrderIntentAction.Set, replacement);
            using (var leave = (ProductLeaveResponse)Finish(remote.CallProduct(ProductMessages.Create<ProductLeaveRequest>(value => value.MatchId = remote.MatchId)), 500))
            {
                Assert.That(leave.Error, Is.Zero);
            }
            Pump(20);
            Assert.That(IntentState.Members, Is.Empty);
            IntentDo(local, OrderIntentAction.Set, replacement);
            FrontWait(() => CurrentLevel.Phase == LevelPhase.Completed, 5000);
            Assert.That(IntentState.Members, Is.Empty);
            FrontWait(() => Renovation.Prepared, 20);
            if (!Renovation.SelectionFinalized)
            {
                RenovationChoose(Renovation.Candidates.First());
            }
            LevelDo(LevelControlAction.Continue);
            Assert.That(IntentState.Members, Is.Empty);
            using var denied = IntentReply(local, oldLevel);
            Assert.That(denied.Message, Is.EqualTo("WrongLevel"));
        }
    }
}
