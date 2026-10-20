using System;
using System.Linq;
using Game;
using NUnit.Framework;
#if UNITY_ET_VIEW && UNITY_EDITOR
using ET.Client;
#endif

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void StartRemainingBusiness(ProductMenuVariant menu, bool cooperative, int buff = 0)
        {
            if (buff != 0)
            {
                foreach (var level in Tables.Instance.DTLevelConfig.DataList.ToArray())
                {
                    if (level.MenuVariant == menu)
                    {
                        InstallBuffLevel(BuffLevel(level, pool: new[] { buff }, count: 1));
                    }
                }
            }
            UseOptionalBuffFixture();
            var config = RuntimeInstanceConfig.Create("remaining-business", "test", cooperative ? "host" : "solo", cooperative ? "127.0.0.1:0" : null);
            RuntimeInstanceContext.Prepare(config);
            var fiber = CreateFiber();
            host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
            host.InitializeProduct(menuVariant: menu);
            Pump(15);
            Assert.That(host.Stage, Is.Not.EqualTo(ProductStage.Failed), host.Failure);
            FrontWait(() => Buff.Prepared, 8);
        }

        [Test, Category("RemainingBusiness")]
        [Category("ProductSemanticEnums")]
        public void RemainingBusinessNewMenuBuffsAllHaveARealAllowedRecipeAndLegacyPoolsStayStable()
        {
            BuffConfig.ValidateAll(); StationUpgradeConfig.ValidateAll();
            foreach (var level in Tables.Instance.DTLevelConfig.DataList)
            {
                if (level.MenuVariant <= ProductMenuVariant.Drink)
                {
                    Assert.That(level.AllowedBuffIds, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
                    continue;
                }
                var permit = new ProductionPermit();
                ProductionPermissionRules.Rebuild(permit, level.AllowedDishIds.Concat(level.AllowedDrinkIds), Tables.Instance.DTStationConfig.DataList.Select(row => row.Id));
                Assert.That(level.AllowedBuffIds, Is.Not.Empty, "Required choice remains playable: " + level.Id);
                foreach (int id in level.AllowedBuffIds)
                {
                    var effect = Tables.Instance.DTBuffConfig.Get(id);
                    Assert.That(permit.AutoRecipes.Contains(effect.BatchRecipeId) || permit.ManualRecipes.Contains(effect.ManualRecipeId) ||
                        permit.BakeRecipes.Contains(effect.BakeRecipeId), Is.True, "Irrelevant effect " + id + " in level " + level.Id);
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("RemainingBusiness")]
        public void RemainingBusinessMixBuffAndUpgradeActuallyFreezeNewBatchAndProjectToBothClients(bool cooperative)
        {
            StartRemainingBusiness(ProductMenuVariant.Dessert, cooperative, 13);
            var client = (ClientSessionComponent)host.Client;
            var remote = cooperative ? CoopRemote() : client;
            var old = DessertMix(client, false);
            Assert.That(old.RequiredTicks, Is.EqualTo(80));
            BuffDo(13);
            Assert.That(old.RequiredTicks, Is.EqualTo(80), "Choosing an effect cannot rewrite an existing batch");
            ContentComplete(); var mixer = PrepStation("搅拌机"); UpgradeChoose(12, mixer);
            long levelId = CurrentLevel.Id;
            LevelDo(LevelControlAction.Continue);
            PumpUntil(() => CurrentLevel.Id != levelId, "Install real mixer upgrade", 180, true);
            Assert.That(PrepStation("搅拌机"), Is.SameAs(mixer)); Assert.That(mixer.UpgradeLevel, Is.EqualTo(1));
            BuffDo(13);
            var mold1 = PrepStation("模具台"); PrepDo(client, GridAction.TakeCounter, mold1.Id, mold1.ItemId); DessertPortion(client);
            PrepDo(client, GridAction.PlaceCounter, mold1.Id, Grid.Actor(client.MemberId).HandItemId);
            var mold2 = PrepStation("第二模具台"); PrepDo(client, GridAction.TakeCounter, mold2.Id, mold2.ItemId); DessertPortion(client);
            PrepDo(client, GridAction.PlaceCounter, mold2.Id, Grid.Actor(client.MemberId).HandItemId);
            BeverageRaw(client, 12, "搅拌机"); BeverageRaw(client, 13, "搅拌机");
            var eggs = PermissionOrder(client, 2); PermissionSupply(client, ReceiveIntent(eggs));
            var receiving = Grid.Station(eggs.ReceivingStationId); var package = Grid.Item(receiving.ItemId); long egg = package.Contents[0];
            PermissionSupply(client, PortionIntent(receiving, package.Id, egg));
            PrepDo(client, GridAction.AddToContainer, mixer.Id, egg, mixer.ItemId);
            PrepDo(client, GridAction.StartMix, mixer.Id, mixer.ItemId, mixer.ItemId);
            var next = Grid.AutoProcess(Grid.Item(mixer.ItemId).AutoProcessId);
            Assert.That(next.RequiredTicks, Is.EqualTo(45), "80 * 3/4 effect * 3/4 upgrade");
            var view = remote.GetComponent<GridClientComponent>();
            PumpUntil(() => view.AutoProcesses.ContainsKey(next.Id) && view.Stations[mixer.Id].UpgradeLevel == 1, "Independent actual effect and station projection", 180, true);
            Assert.That(view.AutoProcesses[next.Id].RequiredTicks, Is.EqualTo(45));
            LevelDo(LevelControlAction.Pause); int progress = next.ProgressTicks; AdvanceTicks(10);
            Assert.That(next.ProgressTicks, Is.EqualTo(progress)); LevelDo(LevelControlAction.Resume);
        }

        [Test, Category("RemainingBusinessMenuEligibility")]
        public void RemainingBusinessGrowthBuffPoolExcludesLockedDrinkAndKeepsItsDrawAfterProjectionRefresh()
        {
            StartRemainingBusiness(ProductMenuVariant.Growth, false);
            Assert.That(CurrentLevel.AllowedDishIds, Has.No.Member(3));
            Assert.That(Buff.AllowedIds, Has.No.Member(6).And.No.Member(9), "Orange chopping and juicing cannot benefit the locked drink branch");
            Assert.That(Buff.AllowedIds, Is.Not.Empty);
            var before = Buff.Candidates.ToArray(); long version = Buff.DrawVersion;
            Grid.Dirty = true; AdvanceTicks(4); Pump(12);
            Assert.That(Buff.Candidates, Is.EqualTo(before)); Assert.That(Buff.DrawVersion, Is.EqualTo(version));
            foreach (int id in Buff.Candidates)
            {
                Assert.That(BuffConfig.CanAffectMenu(Tables.Instance.DTBuffConfig.Get(id), CurrentLevel.ProductionPermit), Is.True);
            }
        }

        [TestCase(ProductMenuVariant.Growth, 3, "灶台", false)]
        [TestCase(ProductMenuVariant.Growth, 4, "榨汁机", false)]
        [TestCase(ProductMenuVariant.Hot, 5, "煎台", false)]
        [TestCase(ProductMenuVariant.Dessert, 6, "搅拌机", false)]
        [TestCase(ProductMenuVariant.Cafe, 7, "磨豆机", false)]
        [TestCase(ProductMenuVariant.Cafe, 8, "咖啡机", false)]
        [TestCase(ProductMenuVariant.Cafe, 9, "蒸汽机", false)]
        [TestCase(ProductMenuVariant.Cafe, 9, "蒸汽机", true)]
        [Category("RemainingBusiness")]
        [Category("ProductSemanticEnums")]
        public void RemainingBusinessNewUpgradeRealSelectionAndActualFileRestoreKeepItemsAndSingleLevel(ProductMenuVariant menu, int upgrade, string stationName, bool cooperative)
        {
            StartRemainingBusiness(menu, cooperative);
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative)
                {
                    ProveFixtureProfile(host.Client, repository);
                }
                ContentComplete(); var station = PrepStation(stationName);
                Assert.That(Renovation.Candidates, Does.Contain(upgrade + 6));
                long item = station.ItemId; GridCell cell = station.Cell;
                UpgradeChoose(upgrade + 6, station);
                Assert.That(station.UpgradeLevel, Is.Zero, "A choice is not an installation");
                var header = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                var payload = repository.Select(header.SlotId, out header);
                int count = payload.Items.Length;
                Assert.That(payload.PendingRenovationId, Is.EqualTo(upgrade + 6));
                host.StopProduct(); repository.Dispose(); StartSavedFixture(header, payload); Pump(15);
                var restored = PrepStation(stationName);
                Assert.That(restored.UpgradeLevel, Is.EqualTo(1)); Assert.That(restored.UpgradeDefinitionId, Is.EqualTo(upgrade));
                Assert.That(restored.Cell, Is.EqualTo(cell)); Assert.That(Grid.Items.Count, Is.EqualTo(count));
                Assert.That(restored.ItemId == 0, Is.EqualTo(item == 0));
                Assert.That(RenovationView.Stations[restored.Id].UpgradeLevel, Is.EqualTo(1));
            }
            finally
            {
                repository.Dispose(); RemoveOwnSaveTestDirectory(directory);
            }
        }

        [TestCase(7, 15, false)]
        [TestCase(8, 16, false)]
        [TestCase(9, 17, false)]
        [TestCase(9, 18, true)]
        [Category("RemainingBusiness")]
        public void RemainingBusinessCoffeeDevicesActuallyUseNewBuffAndInstalledUpgradeForTheirModes(int upgrade, int buff, bool cooperative)
        {
            StartRemainingBusiness(ProductMenuVariant.Cafe, cooperative, buff);
            var client = (ClientSessionComponent)host.Client;
            var observer = cooperative ? CoopRemote() : client;
            var definition = Tables.Instance.DTStationUpgradeConfig.Get(upgrade);
            var station = Grid.Stations.Values.Select(value => (GridStation)value).Single(value => value.Kind == definition.StationKind);
            ContentComplete(); UpgradeChoose(upgrade + 6, station); LevelDo(LevelControlAction.Continue); BuffDo(buff);
            BeverageBases(client);
            var grinding = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single(value => value.RecipeId == 6);
            Assert.That(grinding.RequiredTicks, Is.EqualTo(upgrade == 7 ? 23 : 40));
            foreach (var value in Grid.AutoProcesses.Values)
            {
                GridAutoProcess work = value;
                int expected = work.RecipeId == 4 ? (upgrade == 8 ? 45 : 80) :
                    upgrade == 9 ? (work.RecipeId == (buff == 17 ? 5 : 6) ? 34 : 45) : 60;
                Assert.That(work.RequiredTicks, Is.EqualTo(expected), "Real coffee mode " + work.RecipeId);
            }
            var view = observer.GetComponent<GridClientComponent>();
            PumpUntil(() => view.SelectedBuffId == buff && view.AutoProcesses.Count == Grid.AutoProcesses.Count, "Current independent coffee processing projection", 180, true);
            foreach (var work in view.AutoProcesses.Values)
            {
                Assert.That(work.RequiredTicks, Is.EqualTo(Grid.AutoProcess(work.Id).RequiredTicks));
            }
        }

#if UNITY_ET_VIEW && UNITY_EDITOR
        [Test, Category("RemainingBusiness")]
        public void RemainingBusinessGuideKeepsBoundOrderAndExplainsExpiredBindingWithoutSendingCommands()
        {
            StartBeverage(); var client = (ClientSessionComponent)host.Client; BeverageBases(client);
            var source = PrepStation("切配台"); long cupId = source.ItemId;
            PrepDo(client, GridAction.TakeCounter, source.Id, cupId); BeveragePortion(client, "咖啡机");
            var view = client.GetComponent<GridClientComponent>(); var cup = view.Items[cupId];
            var tables = view.Stations.Values.Where(station => station.Kind == GridStationKind.Table).OrderBy(station => station.Id).ToArray();
            // 提示是纯投影函数：输入两个订单的值，验证绑定选择，不伪造权威交付。
            using var first = GridOrderState.Create(); first.OrderId = 101; first.DishId = 9; first.TableStationId = tables[0].Id; first.State = (int)OrderState.Waiting;
            using var second = GridOrderState.Create(); second.OrderId = 102; second.DishId = 9; second.TableStationId = tables[1].Id; second.State = (int)OrderState.Waiting;
            view.Orders.Clear(); view.Orders.Add(101, new GridOrderProjection(first)); view.Orders.Add(102, new GridOrderProjection(second));
            view.Items[cupId] = new GridItemProjection(cup.Id, cup.Name, cup.Location, cup.HolderId, cup.Cell, cup.Contents, cup.DefinitionId,
                cup.AutoProcessId, cup.BakeProcessId, cup.Hygiene, cup.Form, cup.Discardable, 102);
            long command = client.NextCommandId;
            Assert.That(GridRecipeGuide.CurrentNeed(view, 9), Does.Contain(tables[1].Name).And.Not.Contain("再拿起"));
            view.Orders.Remove(102);
            Assert.That(GridRecipeGuide.CurrentNeed(view, 9), Does.Contain("原订单已失效").And.Contain("解绑"));
            Assert.That(client.NextCommandId, Is.EqualTo(command)); Assert.That(Grid.IdentifyDish(cupId), Is.EqualTo(9));
        }

        [Test, Category("RemainingBusiness"), Category("RecipeStepRegression")]
        public void RemainingBusinessGuideDoesNotOfferLockedSoupInputsAsSaladMaterials()
        {
            StartGrowth(); var client = (ClientSessionComponent)host.Client;
            long pot = SoupFillPot(client); SoupBegin(client, pot); Pump(10);
            var view = client.GetComponent<GridClientComponent>();
            Assert.That(GridRecipeGuide.CurrentNeed(view, 4), Does.Contain("番茄"));
            Assert.That(GridRecipeGuide.CurrentNeed(view, 4), Does.Not.Contain("切配生菜"));
            var step = GridRecipeGuide.CurrentStep(view, 4);
            Assert.That(step.Description, Does.Contain("番茄"));
            Assert.That(Grid.Item(pot).Contents, Has.No.Member(step.ItemId), "A locked soup input is never offered as the locator's free material");
        }

        [Test, Category("RemainingBusiness"), Category("RecipeStepRegression")]
        public void RemainingBusinessRecipeGuideUsesPartialRealMixAndCookedMoldInsteadOfRepeatingMaterials()
        {
            StartDessert(); var client = (ClientSessionComponent)host.Client; var view = client.GetComponent<GridClientComponent>();
            DessertAddSupply(client, 12);
            string partial = GridRecipeGuide.CurrentNeed(view, 8);
            Assert.That(partial, Does.Contain("鸡蛋 ×1").And.Contain("牛奶 ×1").And.Not.Contain("预拌粉 ×1"));
            var egg = PrepStation("食材台"); PrepDo(client, GridAction.TakeCounter, egg.Id, egg.ItemId);
            var mixer = PrepStation("搅拌机"); PrepDo(client, GridAction.AddToContainer, mixer.Id, Grid.Actor(client.MemberId).HandItemId, mixer.ItemId);
            DessertAddSupply(client, 13); PrepDo(client, GridAction.StartMix, mixer.Id, mixer.ItemId, mixer.ItemId);
            var batch = Grid.AutoProcess(Grid.Item(mixer.ItemId).AutoProcessId); AdvanceTicks(batch.RequiredTicks + 3); Pump(15);
            var mold = PrepStation("模具台"); PrepDo(client, GridAction.TakeCounter, mold.Id, mold.ItemId); DessertPortion(client);
            var baking = DessertBeginBake(client); AdvanceTicks(baking.RequiredTicks + 3); Pump(15);
            long command = client.NextCommandId; int items = Grid.Items.Count;
            Assert.That(GridRecipeGuide.CurrentNeed(view, 8), Does.Contain("脱模").And.Not.Contain("缺少"));
            Assert.That(client.NextCommandId, Is.EqualTo(command)); Assert.That(Grid.Items.Count, Is.EqualTo(items));
        }
#endif
    }
}
