using System;
using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private ClientSessionComponent StartDevelopmentLevel(int levelId)
        {
            RuntimeInstanceContext.Reset();
            var config = RuntimeInstanceConfig.Create("development-fixture", "test", "solo",
                testDestination: RuntimeTestDestination.Run, testLevelId: levelId);
            RuntimeInstanceContext.Prepare(config);
            var fiber = CreateFiber();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeDevelopmentTest();
            Pump(16);
            ClientSessionComponent client = host.Client;
            Assert.That(client.Stage, Is.EqualTo(ProductClientStage.Joined));
            Assert.That(client.GetComponent<GridClientComponent>().Confirmed, Is.True);
            return client;
        }

        [Test, Category("DevelopmentTestLevels")]
        public void DevelopmentTestEnvironmentRoleUnknownTargetAndNormalIntentStayDistinct()
        {
            RuntimeInstanceConfig.ValidateDevelopmentAccess(RuntimeInstanceRole.Solo, true, false);
            RuntimeInstanceConfig.ValidateDevelopmentAccess(RuntimeInstanceRole.Solo, false, true);
            Assert.Throws<InvalidOperationException>(() => RuntimeInstanceConfig.ValidateDevelopmentAccess(RuntimeInstanceRole.Solo, false, false));
            foreach (RuntimeInstanceRole role in new[] { RuntimeInstanceRole.Host, RuntimeInstanceRole.Client, RuntimeInstanceRole.Legacy })
            {
                Assert.Throws<InvalidOperationException>(() => RuntimeInstanceConfig.ValidateDevelopmentAccess(role, true, true));
            }
            Assert.Throws<InvalidOperationException>(() => RuntimeInstanceConfig.Create("dev", "test", "host", testDestination: RuntimeTestDestination.Run, testLevelId: 101));
            var normal = RuntimeInstanceConfig.Create("dev", "test", "solo", testLevelId: 101);
            Assert.That(normal.IsDevelopmentTest, Is.False, "Remembered selection alone never starts a test");
            Assert.Throws<InvalidOperationException>(() => DevelopmentTestCatalog.Resolve(int.MaxValue));
            var rows = (System.Collections.Generic.IList<DRLevelConfig>)Tables.Instance.DTLevelConfig.DataList;
            var missing = RestaurantChapters.First(0, RuntimeInstanceRole.Solo, ProductMenuVariant.Cafe);
            int index = rows.IndexOf(missing);
            rows.RemoveAt(index);
            try
            {
                var unavailable = DevelopmentTestCatalog.Entries().Single(value => value.Name == "咖啡制作");
                Assert.That(unavailable.LevelId, Is.Zero);
                Assert.That(unavailable.Failure, Is.Not.Null.And.Not.Empty);
                Assert.Throws<InvalidOperationException>(() => DevelopmentTestCatalog.Resolve(missing.Id));
            }
            finally
            {
                rows.Insert(index, missing);
            }
            var config = RuntimeInstanceConfig.Create("dev-invalid", "test", "solo", testDestination: RuntimeTestDestination.Run, testLevelId: int.MaxValue);
            RuntimeInstanceContext.Prepare(config);
            var fiber = CreateFiber();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            Assert.Throws<InvalidOperationException>(() => host.InitializeDevelopmentTest());
            Assert.That((Scene)host.HostScene, Is.Null);
            Assert.That((Scene)host.ClientScene, Is.Null);
        }

        [Test, Category("DevelopmentTestLevels")]
        public void DevelopmentTestEveryExposedLevelStartsExactReadyBaselineAndUsesRealGridArbitration()
        {
            var entries = DevelopmentTestCatalog.Entries();
            Assert.That(entries.Where(value => value.Failure != null).Select(value => value.Name + ":" + value.Failure), Is.Empty);
            Assert.That(entries.Select(value => value.LevelId).Distinct().Count(), Is.EqualTo(entries.Count));
            Assert.That(entries.Count(value => value.Group == DevelopmentTestGroup.Chapter), Is.EqualTo(Tables.Instance.DTRestaurantChapterConfig.DataList.Count * 3));
            Assert.That(entries.Any(value => value.Group == DevelopmentTestGroup.Feature), Is.True);
            foreach (DevelopmentTestLevel entry in entries)
            {
                var client = StartDevelopmentLevel(entry.LevelId);
                var view = client.GetComponent<GridClientComponent>();
                var definition = DevelopmentTestCatalog.Resolve(entry.LevelId);
                Assert.That(view.LevelConfigId, Is.EqualTo(entry.LevelId), entry.Name);
                Assert.That(CurrentLevel.Phase, Is.EqualTo(LevelPhase.Preparing));
                var initial = RestaurantUnlockValues.Initial(definition);
                var expectedPermit = new ProductionPermit();
                ProductionPermissionRules.Rebuild(expectedPermit,
                    definition.AllowedDishIds.Concat(definition.AllowedDrinkIds).Where(initial.DishIds.Contains), initial.StationIds);
                Assert.That(CurrentLevel.ProductionPermit.DishIds, Is.EquivalentTo(expectedPermit.DishIds));
                var layout = Tables.Instance.DTInitialLayoutConfig.DataList.Where(row => ProductMenuCatalog.Includes(definition.MenuVariant, row.MenuVariant)).ToArray();
                Assert.That(Grid.Stations.Values.Select(station => ((GridStation)station).InitialLayoutId), Is.EquivalentTo(layout.Select(row => row.Id)));
                foreach (var row in layout.Where(row => row.InitialItemDefinitionId != 0))
                {
                    GridStation station = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.InitialLayoutId == row.Id);
                    Assert.That(((GridItem)Grid.Items[station.ItemId]).DefinitionId, Is.EqualTo(row.InitialItemDefinitionId));
                }
                var sources = Tables.Instance.DTSupplyConfig.DataList.Where(row => ProductMenuCatalog.Includes(definition.MenuVariant, row.MenuVariant)).ToArray();
                Assert.That(view.SupplySources.Keys, Is.EquivalentTo(sources.Select(row => row.Id)));
                foreach (var source in sources)
                {
                    Assert.That(view.SupplySources[source.Id].RemainingPackages, Is.EqualTo(source.AvailablePackages));
                }
                Assert.That(((Scene)host.HostScene).GetComponent<NetComponent>(), Is.Null);
                Assert.That(host.ListenEndpoint, Is.Null);
                Assert.That(((Match)((HostSessionComponent)host.Authority).Match).GetComponent<SoloSaveBinding>(), Is.Null);
                Assert.That(CurrentLevel.EligibleProfiles, Is.Empty);
                var bowl = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Name == "碗台");
                long item = bowl.ItemId;
                int count = Grid.Items.Count;
                GridSubmit(client, 1, GridAction.TakeCounter, target: bowl.Id, expected: item);
                AdvanceTicks(100); PumpMessages(8);
                Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(item).And.Not.Zero, entry.Name);
                Assert.That(bowl.ItemId, Is.Zero);
                Assert.That(Grid.Items.Count, Is.EqualTo(count), "Taking a finite item never replenishes it");
                GridSubmit(client, 2, GridAction.TakeCounter, target: bowl.Id);
                AdvanceTicks(5); PumpMessages(8);
                Assert.That(view.LastCommandId, Is.EqualTo(2));
                Assert.That(view.LastStatus, Is.EqualTo(GridResultStatus.Rejected));
                Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(item));
                TestContext.WriteLine("Development test start exercised: " + entry.Name + " / " + entry.LevelId);
                host.StopProduct();
            }
        }
    }
}
