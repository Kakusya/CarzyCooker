using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(RestaurantUnlocks))]
    public sealed partial class ProductSessionTests
    {
        private void StartGrowth(bool cooperative = false)
        {
            var config = RuntimeInstanceConfig.Create("unlock-test", "test", cooperative ? "host" : "solo", cooperative ? "127.0.0.1:0" : null);
            RuntimeInstanceContext.Prepare(config); var fiber = CreateFiber(); UseOptionalBuffFixture();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeProduct(menuVariant: (int)ProductMenuVariant.Growth); Pump(12);
            Assert.That(host.Stage, Is.Not.EqualTo(ProductStage.Failed), host.Failure);
        }

        [Test]
        [Category("RestaurantUnlockCore")]
        public void RestaurantUnlockConfigurationKeepsLegacyMenusAndSharesOnlyLegalProductionClosure()
        {
            GridDishConfig.ValidateAllMenus();
            RestaurantConfig.ValidateLevelDefinitions(Tables.Instance.DTLevelConfig.DataList, GridKitchenSystem.IsInitialGridWalkable);
            RenovationConfig.ValidateAll();
            var first = Tables.Instance.DTLevelConfig.Get(701);
            var initial = RestaurantUnlockValues.Initial(first); var permit = new ProductionPermit();
            ProductionPermissionRules.Rebuild(permit, first.AllowedDishIds.Concat(first.AllowedDrinkIds).Intersect(initial.DishIds), initial.StationIds);
            Assert.That(permit.DishIds, Is.EquivalentTo(new[] { 1, 2, 4, 5 }));
            ProductionPermissionRules.Rebuild(permit, new[] { 3 }, initial.StationIds);
            Assert.That(permit.DishIds, Is.Empty, "Recipe unlock cannot bypass locked required equipment");
            ProductionPermissionRules.Rebuild(permit, new[] { 3 }, initial.StationIds.Append(10));
            Assert.That(permit.DishIds, Does.Contain(3));
            var third = Tables.Instance.DTLevelConfig.Get(703);
            ProductionPermissionRules.Rebuild(permit, third.AllowedDishIds, initial.StationIds.Append(10));
            Assert.That(permit.DishIds, Is.EquivalentTo(new[] { 4 }));
            Assert.That(permit.ItemDefinitions, Does.Contain(GridKitchenRules.Tomato));
            Assert.That(permit.ManualRecipes, Does.Contain(1));
            Assert.That(permit.AutoRecipes, Is.Empty);
            Assert.That(ProductionPermissionRules.AllowsInput(permit, GridKitchenRules.Plate, GridKitchenRules.TomatoChunks), Is.True);
            Assert.That(ProductionPermissionRules.AllowsInput(permit, GridKitchenRules.Pot, GridKitchenRules.TomatoChunks), Is.False);
            Assert.That(ProductionPermissionRules.AllowsInput(permit, GridKitchenRules.Plate, 26), Is.False);
            var old = RestaurantUnlockValues.Initial(Tables.Instance.DTLevelConfig.Get(201));
            Assert.That(old.DishIds, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(old.StationIds, Does.Contain(10));
        }

        [Test]
        [Category("RestaurantUnlockCore")]
        public void RestaurantUnlockRealRenovationMailboxGrantsOnceKeepsOwnerAcrossLevelAndDoesNotCreateItemsOrEquipment()
        {
            StartGrowth();
            var unlocked = Restaurant.GetComponent<RestaurantUnlocks>();
            Assert.That(unlocked.DishIds.Contains(3), Is.False);
            Assert.That(unlocked.StationIds.Contains(10), Is.False);
            Assert.That(CurrentLevel.AllowedDishIds, Is.EqualTo(new[] { 1, 2, 4, 5 }));
            var items = Grid.Items.Keys.OrderBy(id => id).ToArray(); var stations = Grid.Stations.Keys.OrderBy(id => id).ToArray();
            RenovationComplete(); Assert.That(Renovation.Candidates, Does.Contain(7));
            var request = RenovationView.CreateRenovationChoice(7, RenovationView.RenovationChoiceVersion);
            var replay = CopyReplayRequest(request);
            using (var response = RenovationReply(request))
            {
                Assert.That(response.Error, Is.Zero, response.Message);
            }
            AdvanceTicks(1); PumpMessages(10);
            Assert.That(unlocked.DishIds.Contains(3), Is.True);
            Assert.That(unlocked.StationIds.Contains(10), Is.True);
            Assert.That(CurrentLevel.AllowedDishIds, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            long revision = unlocked.PermissionVersion;
            using (var response = RenovationReply(replay))
            {
                Assert.That(response.Status, Is.EqualTo((int)GridResultStatus.Executed));
            }
            Assert.That(unlocked.PermissionVersion, Is.EqualTo(revision));
            LevelDo(LevelControlAction.Continue);
            Assert.That(CurrentLevel.ConfigId, Is.EqualTo(702));
            Assert.That(Restaurant.GetComponent<RestaurantUnlocks>(), Is.SameAs(unlocked));
            Assert.That(unlocked.DishIds.Count(id => id == 3), Is.EqualTo(1));
            Assert.That(Renovation.AppliedStations[7], Is.Empty);
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Assert.That(Grid.Stations.Keys.OrderBy(id => id), Is.EqualTo(stations));
        }
    }
}
