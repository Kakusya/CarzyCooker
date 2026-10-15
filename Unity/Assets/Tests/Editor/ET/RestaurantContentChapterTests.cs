using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void ContentRoomStart(int chapter)
        {
            using var reply = RoomReply(host.Client, ((ClientSessionComponent)host.Client).CreateRoomCommand(ProductRoomAction.StartRestaurant,
                (int)Tables.Instance.DTRestaurantChapterConfig.Get(chapter).MenuVariant, chapter));
            Assert.That(reply.Error, Is.Zero, reply.Message); Assert.That(reply.Status, Is.EqualTo((int)GridResultStatus.Executed)); Pump(20);
        }

        private ClientSessionComponent StartContentChapter(bool cooperative, int chapter)
        {
            if (cooperative)
            {
                StartRoomFixture(); UseOptionalBuffFixture(); var remote = JoinRoomFixture();
                using (var denied = RoomReply(remote, remote.CreateRoomCommand(ProductRoomAction.StartRestaurant, (int)Tables.Instance.DTRestaurantChapterConfig.Get(chapter).MenuVariant, chapter)))
                {
                    Assert.That(denied.Message, Is.EqualTo("HostControlOnly"));
                }
                ContentRoomStart(chapter);
                PumpUntil(() => remote.GetComponent<GridClientComponent>().Confirmed, "Real KCP receives selected content chapter", 150, true);
                return remote;
            }
            var config = RuntimeInstanceConfig.Create("content-chapter-test", "solo", "solo");
            RuntimeInstanceContext.Reset(); RuntimeInstanceContext.Prepare(config); var fiber = CreateFiber(); UseOptionalBuffFixture();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeProduct(menuVariant: (int)Tables.Instance.DTRestaurantChapterConfig.Get(chapter).MenuVariant, contentChapterId: chapter); Pump(15);
            return null;
        }

        private void ContentComplete()
        {
            if (CurrentLevel.Phase == LevelPhase.Preparing)
            {
                LevelDo(LevelControlAction.Start);
            }
            for (int tick = 0; tick < 3000 && CurrentLevel.Phase != LevelPhase.Completed; ++tick)
            {
                AdvanceTicks(1); PumpMessages(3);
            }
            Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Completed), "Actual bounded chapter flow naturally drains");
            if (!CurrentLevel.IsChapterTerminal)
            {
                FrontWait(() => Renovation.Prepared, 8);
            }
        }

        [Test, Category("RestaurantContentChapterCore")]
        public void RestaurantContentChapterConfigHasTwoThreeStageRoutesInBothModesWithoutChangingOldReferences()
        {
            RestaurantChapters.ValidateAll();
            RestaurantConfig.ValidateLevelDefinitions(Tables.Instance.DTLevelConfig.DataList, GridKitchenSystem.IsInitialGridWalkable);
            GridDishConfig.ValidateAllMenus();
            FrontConfig.Validate(Tables.Instance.DTCustomerFlowConfig.DataList, Tables.Instance.DTOrderConfig.DataList, Tables.Instance.DTTableConfig.DataList);
            foreach (var chapter in Tables.Instance.DTRestaurantChapterConfig.DataList)
            {
                foreach (var role in new[] { RuntimeInstanceRole.Solo, RuntimeInstanceRole.Host })
                {
                    var first = RestaurantChapters.First(chapter.Id, role, (int)chapter.MenuVariant);
                    Assert.That(RestaurantConfig.ChapterLevelCount(first), Is.EqualTo(3));
                    Assert.That(RestaurantChapters.Find(first).Id, Is.EqualTo(chapter.Id));
                }
            }
            foreach (int first in new[] { 101, 201, 301, 401, 501, 601, 701, 801 })
            {
                Assert.That(Tables.Instance.DTLevelConfig.Get(first).NextLevelConfigId, Is.EqualTo(first + 1));
                Assert.That(Tables.Instance.DTLevelConfig.Get(first + 1).NextLevelConfigId, Is.EqualTo(first + 2));
                Assert.That(Tables.Instance.DTLevelConfig.Get(first + 2).NextLevelConfigId, Is.Zero);
            }
            Assert.That(RestaurantChapters.Find(Tables.Instance.DTLevelConfig.Get(101)), Is.Null);
            Assert.Throws<InvalidOperationException>(() => RestaurantChapters.First(99, RuntimeInstanceRole.Solo, 3));
            Assert.Throws<InvalidOperationException>(() => RestaurantChapters.First(1, RuntimeInstanceRole.Solo, 0));
        }

        [TestCase(false, 1)]
        [TestCase(false, 2)]
        [TestCase(true, 1)]
        [TestCase(true, 2)]
        [Category("RestaurantContentChapterCore")]
        public void RestaurantContentChapterRealLocalAndKcpSelectThreeLevelsAndReplayAsAnIndependentKitchen(bool cooperative, int chapter)
        {
            var remote = StartContentChapter(cooperative, chapter); var local = (ClientSessionComponent)host.Client;
            var content = Tables.Instance.DTRestaurantChapterConfig.Get(chapter);
            int first = cooperative ? content.CooperativeFirstLevelId : content.SoloFirstLevelId;
            string world = Grid.WorldId; var items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            var orderedDishes = new HashSet<int>();
            for (int stage = 1; stage <= 3; ++stage)
            {
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(first + stage - 1));
                Assert.That(RestaurantChapters.Find(Tables.Instance.DTLevelConfig.Get(CurrentLevel.ConfigId)).Id, Is.EqualTo(chapter));
                Assert.That(CurrentLevel.AllowedDishIds, Is.EquivalentTo(Tables.Instance.DTLevelConfig.Get(CurrentLevel.ConfigId).AllowedDishIds
                    .Concat(Tables.Instance.DTLevelConfig.Get(CurrentLevel.ConfigId).AllowedDrinkIds)));
                ContentComplete();
                foreach (var order in CurrentLevel.Children.Values.OfType<Order>())
                {
                    orderedDishes.Add(order.DishId);
                }
                Assert.That(CurrentLevel.Stars, Is.Zero);
                if (stage < 3)
                {
                    if (stage == 1)
                    {
                        RenovationChoose(8);
                    }
                    LevelDo(LevelControlAction.Continue); Pump(15);
                    Assert.That(Grid.WorldId, Is.EqualTo(world)); Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
                }
            }
            using (var rejected = LevelReply(local, RenovationView.CreateLevelCommand(LevelControlAction.Continue)))
            {
                Assert.That(rejected.Message, Is.EqualTo("ChapterCompleted"));
            }
            Assert.That(CurrentLevel.NextLevelConfigId, Is.Zero); Assert.That(Renovation.Candidates, Is.Empty);
            if (chapter == 2)
            {
                Assert.That(new[] { 5, 6, 7, 8, 9, 10, 11 }.All(orderedDishes.Contains), Is.True,
                    "The three actual stages issue orders for the new salad, hot dishes, dessert and all three coffees");
            }
            var old = RenovationView.CreateLevelCommand(LevelControlAction.Pause);
            if (cooperative)
            {
                var match = (Match)((HostSessionComponent)host.Authority).Match; var session = (Session)remote.Session;
                using (var reply = LevelReply(local, RenovationView.CreateLevelCommand(LevelControlAction.FinishChapter)))
                {
                    Assert.That(reply.Error, Is.Zero, reply.Message);
                }
                AdvanceTicks(1); PumpUntil(() => (RestaurantRuntime)match.Restaurant == null && !remote.GetComponent<ProductRoomClient>().Current.HasRestaurant,
                    "Actual terminal action returns same room", 150, true);
                ContentRoomStart(chapter == 1 ? 2 : 1); Pump(20);
                Assert.That((Session)remote.Session, Is.SameAs(session)); Assert.That(((ProcessInnerSender)host.Sender).IsStopped, Is.False);
                using var stale = LevelReply(local, old); Assert.That(stale.Message, Is.EqualTo("WrongWorld"));
            }
            else
            {
                old.Dispose(); host.StopProduct(); StartContentChapter(false, chapter == 1 ? 2 : 1);
            }
            Assert.That(Grid.WorldId, Is.Not.EqualTo(world)); Assert.That(Grid.Width, Is.EqualTo(7));
            Assert.That(Grid.Items.Keys.Intersect(items), Is.Empty); Assert.That(Renovation.AppliedStations, Is.Empty);
        }
    }
}
