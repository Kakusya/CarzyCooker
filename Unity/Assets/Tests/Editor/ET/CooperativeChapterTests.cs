using System;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        [Test]
        [Category("CooperativeChapter")]
        public void CooperativeChapterConfigDefinesFiniteChainsAndPreservesLegacyReferences()
        {
            var rows = Tables.Instance.DTLevelConfig.DataList;
            RestaurantConfig.ValidateLevelDefinitions(rows, GridKitchenSystem.IsInitialGridWalkable);
            foreach (ProductMenuVariant variant in new[] { ProductMenuVariant.Basic, ProductMenuVariant.Drink })
            {
                var first = RestaurantConfig.FirstCooperativeLevel(variant);
                var chain = rows.Where(row => row.ChapterId == first.ChapterId && row.MenuVariant == variant).OrderBy(row => row.ChapterStage).ToArray();
                Assert.That(chain.Length, Is.EqualTo(3));
                Assert.That(RestaurantConfig.ChapterLevelCount(first), Is.EqualTo(chain.Length));
                Assert.That(chain.Select(row => row.IsChapterTerminal), Is.EqualTo(new[] { false, false, true }));
                Assert.That(chain.Last().AllowedRenovationIds, Is.Empty);
                Assert.That(chain.Last().NextLevelConfigId, Is.Zero);
                Assert.Throws<InvalidOperationException>(() => RestaurantConfig.ValidateLevelDefinitions(rows.Where(row => row.Id != chain[1].Id), GridKitchenSystem.IsInitialGridWalkable));
                Assert.Throws<InvalidOperationException>(() => RestaurantConfig.ValidateLevelDefinitions(rows.Where(row => row.Id != chain[2].Id), GridKitchenSystem.IsInitialGridWalkable));
            }
            Assert.That(Tables.Instance.DTLevelConfig.Get(2).NextLevelConfigId, Is.EqualTo(2));
            Assert.That(Tables.Instance.DTLevelConfig.Get(4).NextLevelConfigId, Is.EqualTo(4));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("CooperativeChapter")]
        public void CooperativeChapterThreeNaturalZeroStarLevelsProjectSameTerminalWithoutFourth(bool drinks)
        {
            StartHost(liveDrinks: drinks);
            var remote = CoopRemote();
            var view = remote.GetComponent<GridClientComponent>();
            GridKitchenComponent kitchen = Grid;
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            for (int stage = 1; stage <= 3; ++stage)
            {
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo((drinks ? 400 : 300) + stage));
                if (!Buff.Prepared)
                {
                    FrontWait(() => Buff.Prepared, 8);
                }
                BuffDo(Buff.Candidates.First());
                ChapterNaturalComplete();
                Pump(30);
                Assert.That(view.LevelPhase, Is.EqualTo(LevelPhase.Completed));
                Assert.That(view.LevelId, Is.EqualTo(CurrentLevel.Id));
                Assert.That(view.LevelScore, Is.Zero);
                Assert.That(view.LevelStars, Is.Zero);
                Assert.That(view.UnfulfilledOrders, Is.EqualTo(CurrentLevel.UnfulfilledOrders));
                Assert.That(view.SaveEnabled, Is.False);
                Assert.That((GridKitchenComponent)Restaurant.Kitchen, Is.SameAs(kitchen));
                Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
                if (stage < 3)
                {
                    FrontWait(() => Renovation.Prepared, 8);
                    LevelDo(LevelControlAction.Continue);
                }
            }
            var terminal = CurrentLevel;
            using (var denied = LevelReply(host.Client, RenovationView.CreateLevelCommand(LevelControlAction.Continue)))
            {
                Assert.That(denied.Message, Is.EqualTo("ChapterCompleted"));
            }
            AdvanceTicks(20);
            Pump(30);
            Assert.That(CurrentLevel, Is.SameAs(terminal));
            Assert.That(terminal.IsChapterTerminal, Is.True);
            Assert.That(Renovation.Candidates, Is.Empty);
            Assert.That(Renovation.SelectedDefinitionId, Is.Zero);
        }

        [Test]
        [Category("CooperativeChapter")]
        public void CooperativeChapterHostFinishReplaysOnceKeepsRoomAndRejectsOldWorld()
        {
            StartRoomFixture();
            var remote = JoinRoomFixture();
            RoomDo(ProductRoomAction.StartRestaurant);
            Pump(30);
            var local = (ClientSessionComponent)host.Client;
            using (var early = LevelReply(local, RenovationView.CreateLevelCommand(LevelControlAction.FinishChapter)))
            {
                Assert.That(early.Error, Is.Zero);
            }
            AdvanceTicks(1);
            PumpMessages(8);
            Assert.That(RenovationPeer.LastCommandReason, Is.EqualTo("WrongPhase"));
            for (int stage = 1; stage <= 3; ++stage)
            {
                FrontWait(() => Buff.Prepared, 8);
                BuffDo(Buff.Candidates.First());
                ChapterNaturalComplete();
                if (stage < 3)
                {
                    FrontWait(() => Renovation.Prepared, 8);
                    LevelDo(LevelControlAction.Continue);
                }
            }
            Pump(30);
            var remoteGrid = remote.GetComponent<GridClientComponent>();
            using (var denied = (LevelCommandResponse)Finish(remote.CallProduct(remoteGrid.CreateLevelCommand(LevelControlAction.FinishChapter)), 500))
            {
                Assert.That(denied.Message, Is.EqualTo("HostControlOnly"));
            }
            var oldWorld = remoteGrid.CreateLevelCommand(LevelControlAction.Pause);
            var request = RenovationView.CreateLevelCommand(LevelControlAction.FinishChapter);
            var duplicate = CopyReplayRequest(request);
            var late = CopyReplayRequest(request);
            var restaurant = Restaurant;
            var match = (Match)((HostSessionComponent)host.Authority).Match;
            var sender = (ProcessInnerSender)host.Sender;
            var session = (Session)remote.Session;
            int member = remote.MemberId;
            string worldId = Grid.WorldId;
            long roomVersion = match.RoomVersion;
            using (var accepted = LevelReply(local, request))
            {
                Assert.That(accepted.Error, Is.Zero);
            }
            using (var repeated = LevelReply(local, duplicate))
            {
                Assert.That(repeated.Error, Is.Zero);
            }
            AdvanceTicks(1);
            PumpUntil(() => (RestaurantRuntime)match.Restaurant == null && !remote.GetComponent<ProductRoomClient>().Current.HasRestaurant,
                "Host finish retires only restaurant", 100, advanceTime: true);
            Assert.That(restaurant.IsDisposed, Is.True);
            Assert.That(sender.IsStopped, Is.False);
            Assert.That(remote.MemberId, Is.EqualTo(member));
            Assert.That((Session)remote.Session, Is.SameAs(session));
            Assert.That(match.RoomVersion, Is.EqualTo(roomVersion + 1));
            RoomDo(ProductRoomAction.StartRestaurant);
            Pump(30);
            Assert.That(Grid.WorldId, Is.Not.EqualTo(worldId));
            using (var stale = LevelReply(local, late))
            {
                Assert.That(stale.Message, Is.EqualTo("WrongWorld"));
            }
            using (var stale = (LevelCommandResponse)Finish(remote.CallProduct(oldWorld), 500))
            {
                Assert.That(stale.Message, Is.EqualTo("HostControlOnly"), "Remote level control is denied before checking world identity");
            }
            Assert.That(CurrentLevel.ConfigId, Is.EqualTo(301));
            Assert.That(match.Children.Values.OfType<RestaurantRuntime>().Count(), Is.EqualTo(1));
            Assert.That(local.GetComponent<ProductRoomClient>().Current.HasRestaurant, Is.True);
        }
    }
}
