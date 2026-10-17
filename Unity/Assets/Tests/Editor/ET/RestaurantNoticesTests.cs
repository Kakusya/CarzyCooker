using System.Linq;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(RestaurantNoticesComponent))]
    public sealed partial class ProductSessionTests
    {
        private RestaurantNoticesComponent Notices => ((Scene)((ClientSessionComponent)host.Client).IScene).GetComponent<RestaurantNoticesComponent>();

        [Test, Category("RestaurantNotices")]
        public void EntryNoticesActualSameStepSoupBakeAndShipmentsAllAppearOnceAndStayBounded()
        {
            StartSolo();
            ClientSessionComponent client = host.Client;
            var soup = SoupBegin(client, SoupFillPot(client));
            soup.RequiredTicks = 10000;
            var bake = DishBegin(client);
            bake.RequiredTicks = 10000;
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id, quantity: 2));
            var cargo = Supply.Children.Values.OfType<SupplyShipment>().ToArray();
            soup.RequiredTicks = soup.ProgressTicks + 1;
            bake.RequiredTicks = bake.ProgressTicks + 1;
            foreach (var shipment in cargo)
            {
                shipment.RequiredTicks = shipment.ProgressTicks + 1;
            }
            long command = client.NextCommandId;
            AdvanceTicks(4); PumpMessages(8);
            Assert.That(soup.State, Is.EqualTo(GridAutoState.Ready));
            Assert.That(bake.State, Is.EqualTo(GridBakeState.Completed));
            Assert.That(cargo.All(value => value.Stage == ShipmentStage.Ready), Is.True);
            Assert.That(Notices.Entries.Count(value => value.TargetId == soup.Id || value.TargetId == bake.Id), Is.EqualTo(2));
            Assert.That(Notices.Entries.Count(value => cargo.Any(shipment => shipment.Id == value.TargetId)), Is.EqualTo(2));
            Assert.That(Notices.Entries.Single(value => value.TargetId == soup.Id).ItemId, Is.EqualTo(soup.PotItemId));
            var retained = Notices.Entries.ToArray();
            EventSystem.Instance.Publish((Scene)client.IScene, new KitchenWorkOutcomeApplied { Value = SupplyView.LastKitchenOutcome });
            EventSystem.Instance.Publish((Scene)client.IScene, new ShipmentOutcomeApplied { Value = SupplyView.LastShipmentOutcome });
            PumpMessages(8);
            Assert.That(Notices.Entries, Is.EqualTo(retained), "Repeated observations of pooled-message values cannot replay an outcome");
            Assert.That(client.NextCommandId, Is.EqualTo(command), "Observation sends no domain commands");
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id, quantity: 3));
            int other = Tables.Instance.DTSupplyConfig.DataList.First(value => value.Id != TomatoSupply.Id).Id;
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: other, quantity: 5));
            AdvanceTicks(250); PumpMessages(8);
            Assert.That(Notices.Entries.Count, Is.EqualTo(RestaurantNoticesSystem.MaximumNotices));
            Assert.That(Notices.Entries.Select(value => value.Serial).Distinct().Count(), Is.EqualTo(8));
            Assert.That(Notices.NextNotice, Is.GreaterThan(8));
            Assert.That(Notices.Entries[0].Serial, Is.EqualTo(Notices.NextNotice - 7));
        }

        [Test, Category("RestaurantNotices")]
        public void EntryNoticesActualDeadlinesPauseExpiryPreferencesAndFirstProjectionDoNotReplay()
        {
            StartSolo(liveFront: true);
            var repository = TestSaveRepository(out string directory);
            try
            {
                PreferencesWrite(repository, new ProductPreferencesValue());
                LevelDo(LevelControlAction.Start);
                var order = WaitActualOrder(1);
                FrontWait(() => Notices.Entries.Any(value => value.TargetId == order.Id && value.Kind == RestaurantNoticeKind.Deadline), 1500);
                Assert.That(order.State, Is.EqualTo(OrderState.Waiting));
                LevelDo(LevelControlAction.Pause);
                long tick = Grid.Tick, serial = Notices.NextNotice;
                AdvanceTicks(60); PumpMessages(8);
                Assert.That(Grid.Tick, Is.EqualTo(tick));
                Assert.That(Notices.NextNotice, Is.EqualTo(serial));
                PreferencesWrite(repository, new ProductPreferencesValue { Notices = ProductNoticeOptions.None });
                Assert.That(Notices.Entries, Is.Empty);
                PreferencesWrite(repository, new ProductPreferencesValue());
                Assert.That(Notices.Entries, Is.Empty, "Enabling does not replay an already-near order");
                LevelDo(LevelControlAction.Resume);
                FrontWait(() => order.State == OrderState.Expired, 300);
                Assert.That(Notices.Entries.Any(value => value.TargetId == order.Id), Is.False);
                PreferencesWrite(repository, new ProductPreferencesValue { Notices = ProductNoticeOptions.None });
                var shipment = SupplyOrderReady(TomatoSupply.Id);
                Assert.That(Notices.Entries, Is.Empty);
                PreferencesWrite(repository, new ProductPreferencesValue());
                Assert.That(Notices.Entries, Is.Empty);
                // A recreated observer starts at the currently installed facts, as a first connection does.
                var scene = (Scene)((ClientSessionComponent)host.Client).IScene;
                scene.RemoveComponent<RestaurantNoticesComponent>();
                Grid.Dirty = true; AdvanceTicks(1); PumpMessages(8);
                Assert.That(Notices.BaselineReady, Is.True);
                Assert.That(Notices.Entries, Is.Empty);
                Assert.That(Notices.InitialReadyShipments.Contains(shipment.Id), Is.True);
                Assert.That(Notices.ShipmentSerial, Is.EqualTo(SupplyView.LastShipmentOutcomeSerial));
            }
            finally
            {
                repository.Dispose();
                RemoveOwnSaveTestDirectory(directory);
            }
        }

        [Test, Category("RestaurantNotices")]
        public void EntryNoticesActualWorkAndShipmentContinueAcrossLevelAndPausedCompletion()
        {
            StartSolo();
            ClientSessionComponent client = host.Client;
            long pot = SoupFillPot(client);
            LevelDo(LevelControlAction.Start);
            PrepDo(client, GridAction.Move, cell: new GridCell(1, 9));
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks - 8);
            var soup = SoupBegin(client, pot);
            soup.RequiredTicks = 10000;
            SupplyDo(new SupplyIntent(SupplyAction.Purchase, source: TomatoSupply.Id));
            var shipment = Supply.Children.Values.OfType<SupplyShipment>().Single();
            shipment.RequiredTicks = 10000;
            AdvanceTicks(CurrentLevel.OperatingTicks - CurrentLevel.ElapsedOperatingTicks + 2);
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed));
            FrontWait(() => Renovation.Prepared, 8);
            RenovationChoose(Renovation.Candidates.First());
            LevelDo(LevelControlAction.Continue);
            Assert.That(Notices.LevelId, Is.EqualTo(CurrentLevel.Id));
            soup.RequiredTicks = soup.ProgressTicks + 1;
            shipment.RequiredTicks = shipment.ProgressTicks + 1;
            LevelDo(LevelControlAction.Pause);
            AdvanceTicks(20); PumpMessages(8);
            Assert.That(soup.State, Is.EqualTo(GridAutoState.Cooking));
            Assert.That(shipment.Stage, Is.EqualTo(ShipmentStage.InTransit));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(4); PumpMessages(8);
            Assert.That(Notices.Entries.Count(value => value.TargetId == soup.Id), Is.EqualTo(1));
            Assert.That(Notices.Entries.Count(value => value.TargetId == shipment.Id), Is.EqualTo(1));
        }
    }
}
