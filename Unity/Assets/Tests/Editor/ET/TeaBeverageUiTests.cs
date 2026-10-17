#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private IEnumerator TeaUiJourney(bool cooperative)
        {
            if (cooperative)
            {
                yield return StartProgressHostRoom(); yield return StartPreviewAndConfirm(EntryButton("开启茶饮餐厅"));
            }
            else
            {
                yield return StartModeLauncher(); CreateSaveUiRepository(Runtime, out _, out _); Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Original tea selector");
                yield return StartPreviewAndConfirm(SelectionButton("新开茶饮餐厅"));
            }
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Ordinary tea kitchen opens");
            long before = Client.NextCommandId; Pointer(Button("菜谱 珍珠奶茶"));
            string guide = string.Join("\n", Hud.GetComponentsInChildren<Text>(false).Select(text => text.text));
            Assert.That(guide, Does.Contain("显式接水").And.Contain("红茶叶").And.Contain("牛奶").And.Contain("糖浆").And.Contain("熟珍珠").And.Contain("冰最后投入"));
            Pointer(Button("关闭菜谱")); Assert.That(Client.NextCommandId, Is.EqualTo(before));
            yield return Command("直接拿泡茶桶"); yield return Command("接水 → 取水台热水档");
            long barrel = Grid.Actors[Client.MemberId].HandItemId;
            Assert.That(Grid.Items[Grid.Items[barrel].Contents.Single()].DefinitionId, Is.EqualTo(GridKitchenRules.HotWater));
            yield return Command("放 泡茶桶 ·", true); yield return Command("拿 红茶叶台 ·", true); yield return Command("添加 泡茶桶 ·", true);
            yield return Command("泡茶 泡茶桶 ·", true);
            yield return Wait(() => Grid.AutoProcesses.Values.Any(batch => batch.RequiredCapability == GridStationCapability.BrewTea && batch.State == GridAutoState.Ready), "Real tea batch ready");
            Assert.That(Grid.AutoProcesses.Values.Single().RemainingPortions, Is.EqualTo(3));
            CoopUiRemote remote = null;
            if (cooperative)
            {
                Presentation.CloseGridHud(); ((GridKitchenBoard)Presentation.Board).Visible = false;
                remote = new CoopUiRemote(Runtime.ListenEndpoint); ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "Independent KCP teammate receives brewed tea");
            }
            try
            {
                var view = remote?.Grid ?? Grid; var client = remote?.Client ?? Client; var presentation = remote?.Presentation ?? Presentation;
                yield return HotUiCommand(remote, "拿 热茶杯台 ·", true); long hotCup = view.Actors[client.MemberId].HandItemId;
                yield return HotUiCommand(remote, "取一份 泡茶桶 ·", true); yield return HotUiCommand(remote, "放 热茶杯台 ·", true);
                yield return HotUiCommand(remote, "直接拿调饮壶"); yield return HotUiCommand(remote, "取一份 泡茶桶 ·", true);
                yield return HotUiCommand(remote, "放 调饮台 ·", true);
                yield return HotUiCommand(remote, "拿 牛奶台 ·", true); yield return HotUiCommand(remote, "添加 调饮台 ·", true);
                yield return HotUiCommand(remote, "拿 糖浆台 ·", true); yield return HotUiCommand(remote, "添加 调饮台 ·", true);
                yield return HotUiCommand(remote, "调饮 调饮台 ·", true);
                yield return HotUiWait(remote, () => view.AutoProcesses.Values.Any(batch => batch.RequiredCapability == GridStationCapability.MixTea && batch.State == GridAutoState.Ready), "Real milk tea base ready");
                yield return HotUiCommand(remote, "拿 生珍珠台 ·", true); yield return HotUiCommand(remote, "放 煮珍珠台 ·", true);
                yield return HotUiCommand(remote, "煮珍珠 煮珍珠台 ·", true);
                yield return HotUiWait(remote, () => view.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.CookedPearls), "Real cooked pearls ready");
                yield return HotUiCommand(remote, "拿 煮珍珠台 ·", true); yield return HotUiCommand(remote, "添加 小料罐台 ·", true);
                yield return HotUiCommand(remote, "拿 冷饮杯台 ·", true); long icedCup = view.Actors[client.MemberId].HandItemId;
                yield return HotUiCommand(remote, "取一份 调饮台 ·", true); yield return HotUiCommand(remote, "放 冷饮杯台 ·", true);
                yield return HotUiCommand(remote, "拿 冰块台 ·", true); yield return HotUiCommand(remote, "添加 冷饮杯台 ·", true);
                yield return HotUiCommand(remote, "拿 冷饮杯台 ·", true); yield return HotUiCommand(remote, "混合 冰鲜奶茶");
                yield return HotUiWait(remote, () => view.Items[icedCup].Contents.Count == 1 && view.Items[view.Items[icedCup].Contents[0]].DefinitionId == GridKitchenRules.IcedMilkTea, "Real iced milk tea formed");
                yield return HotUiCommand(remote, "放 冷饮杯台 ·", true);
                yield return HotUiBuyRaw(remote, 24, "透明冷饮杯"); long pearlCup = view.Actors[client.MemberId].HandItemId;
                yield return HotUiCommand(remote, "放 红茶叶台 ·", true);
                long cupPackage = view.Stations.Values.Single(station => station.Name == "交接台").ItemId;
                yield return HotUiCommand(remote, "拿 交接台 ·", true); yield return HotUiCommand(remote, "放 牛奶台 ·", true);
                Assert.That(view.Items[cupPackage].Contents.Count, Is.EqualTo(1));
                yield return HotUiBuyRaw(remote, 22, "冰块"); yield return HotUiCommand(remote, "放 糖浆台 ·", true);
                long icePackage = view.Stations.Values.Single(station => station.Name == "交接台").ItemId;
                Pointer(ClaimUiButton(remote?.Hud ?? Hud, "查看详情"));
                yield return HotUiCommand(remote, "取出 熟珍珠 · 小料罐台"); yield return HotUiCommand(remote, "添加 红茶叶台 ·", true);
                yield return HotUiCommand(remote, "拿 红茶叶台 ·", true); yield return HotUiCommand(remote, "取一份 调饮台 ·", true);
                yield return HotUiCommand(remote, "放 红茶叶台 ·", true); yield return HotUiCommand(remote, "拿 糖浆台 ·", true);
                yield return HotUiCommand(remote, "添加 红茶叶台 ·", true); yield return HotUiCommand(remote, "拿 红茶叶台 ·", true);
                Assert.That(view.Items[pearlCup].Contents.Select(id => view.Items[id].DefinitionId), Is.EqualTo(new[] { 80, 82, 83 }));
                yield return HotUiCommand(remote, "混合 珍珠奶茶"); yield return HotUiCommand(remote, "停止混合");
                var retired = CaptureButtonCallback(ClaimUiButton(remote?.Hud ?? Hud, "继续混合"));
                presentation.CloseGridHud(); yield return null; presentation.OpenGridHud();
                yield return HotUiWait(remote, () => presentation.Ready, "Original tea HUD reopens from managed pool");
                before = client.NextCommandId; retired(); Assert.That(client.NextCommandId, Is.EqualTo(before));
                yield return HotUiCommand(remote, "继续混合");
                yield return HotUiWait(remote, () => view.Items[pearlCup].Contents.Count == 1 && view.Items[view.Items[pearlCup].Contents[0]].DefinitionId == GridKitchenRules.PearlMilkTea, "Real pearl milk tea formed");
                yield return HotUiCommand(remote, "放 红茶叶台 ·", true);
                Assert.That(view.AutoProcesses.Values.Any(batch => batch.RequiredCapability == GridStationCapability.MixTea), Is.False);
                Assert.That(view.AutoProcesses.Values.Single(batch => batch.RequiredCapability == GridStationCapability.BrewTea).RemainingPortions, Is.EqualTo(1));
                if (!cooperative)
                {
                    // 同一次普通入口旅程验证混杯的原设施按钮，使用实际剩余原料和第三只有限冷杯。
                    yield return HotUiCommand(remote, "拿 交接台 ·", true); yield return HotUiCommand(remote, "放 生珍珠台 ·", true);
                    yield return HotUiCommand(remote, "直接拿调饮壶"); yield return HotUiCommand(remote, "取一份 泡茶桶 ·", true);
                    yield return HotUiCommand(remote, "放 调饮台 ·", true);
                    yield return HotUiBuyRaw(remote, 23, "瓶装牛奶"); yield return HotUiCommand(remote, "添加 调饮台 ·", true);
                    yield return HotUiDiscardPackage(remote);
                    yield return HotUiBuyRaw(remote, 21, "糖浆"); yield return HotUiCommand(remote, "添加 调饮台 ·", true);
                    yield return HotUiCommand(remote, "调饮 调饮台 ·", true);
                    yield return HotUiWait(remote, () => view.AutoProcesses.Values.Any(batch => batch.RequiredCapability == GridStationCapability.MixTea && batch.State == GridAutoState.Ready), "Real additional finite base for facility check");
                    yield return HotUiCommand(remote, "拆一份 透明冷饮杯", true); long cleanupCup = view.Actors[client.MemberId].HandItemId;
                    yield return HotUiCommand(remote, "取一份 调饮台 ·", true); yield return HotUiCommand(remote, "放 冰块台 ·", true);
                    yield return HotUiCommand(remote, "拆一份 冰块", true); long cleanupIce = view.Actors[client.MemberId].HandItemId;
                    yield return HotUiCommand(remote, "添加 冰块台 ·", true); yield return HotUiCommand(remote, "拿 冰块台 ·", true);
                    long cleanupLiquid = view.Items[cleanupCup].Contents.First();
                    yield return HotUiCommand(remote, "倒液 → 水池／洗碗台");
                    Assert.That(view.Items[cleanupCup].Contents, Is.EqualTo(new[] { cleanupIce })); Assert.That(view.Items.ContainsKey(cleanupLiquid), Is.False);
                    yield return HotUiCommand(remote, "投垃圾 → 垃圾桶");
                    Assert.That(view.Items[cleanupCup].Contents, Is.Empty); Assert.That(view.Items.ContainsKey(cleanupIce), Is.False);
                    yield return HotUiCommand(remote, "放 冰块台 ·", true);
                    Assert.That(view.SupplySources[23].RemainingPackages, Is.EqualTo(4)); Assert.That(view.SupplySources[21].RemainingPackages, Is.EqualTo(4));
                }
                yield return HotUiSwitch(remote, true); yield return ContentUiPump(UnlockUiStartOperating(), remote); yield return HotUiSwitch(remote, false);
                string[] names = { "热红茶", "冰鲜奶茶", "珍珠奶茶" }; string[] sources = { "热茶杯台", "冷饮杯台", "红茶叶台" };
                long[] cups = { hotCup, icedCup, pearlCup };
                for (int index = 0; index < names.Length; ++index)
                {
                    int dish = 13 + index; string name = names[index]; long cup = cups[index];
                    yield return HotUiWait(remote, () => view.Orders.Values.Any(value => value.State == OrderState.Waiting && value.DishId == dish), "Actual customer orders " + name, 45);
                    var order = view.Orders.Values.First(value => value.State == OrderState.Waiting && value.DishId == dish); string table = view.Stations[order.TableStationId].Name;
                    yield return HotUiCommand(remote, "拿 " + sources[index] + " ·", true); yield return HotUiCommand(remote, "放 茶饮贴单台 ·", true);
                    yield return HotUiCommand(remote, "贴单 → " + table); Assert.That(view.Items[cup].BoundOrderId, Is.EqualTo(order.Id));
                    yield return HotUiCommand(remote, "拿 茶饮贴单台 ·", true); yield return HotUiCommand(remote, "前往 " + table);
                    Assert.That(view.Orders[order.Id].State, Is.EqualTo(OrderState.Waiting), name +
                        $" after actual table approach; tick={view.Tick}; deadline={order.DeadlineTick}; phase={view.LevelPhase}; elapsed={view.LevelElapsedTicks}");
                    yield return HotUiCommand(remote, "交付 " + name + " → " + table);
                    Assert.That(view.Orders[order.Id].State, Is.EqualTo(OrderState.Served)); Assert.That(view.Items.ContainsKey(cup), Is.False);
                    yield return HotUiWait(remote, () => view.FrontWorks.Values.Any(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available), "Tea customer leaves before original clearing", 20);
                    long clear = view.FrontWorks.Values.Single(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available).Id;
                    yield return HotUiCommand(remote, "清桌 " + table, true);
                    yield return HotUiWait(remote, () => view.FrontWorks[clear].State == FrontWorkState.Completed, "Original clearing completes", 20);
                    Assert.That(view.FrontWorks.Values.Any(work => work.Kind == FrontWorkKind.Wash && cups.Contains(work.ItemId)), Is.False);
                }
                Assert.That(view.ServedOrders, Is.EqualTo(3)); Assert.That(view.LevelScore, Is.EqualTo(30));
                Assert.That(view.Items[cupPackage].Contents.Count, Is.EqualTo(cooperative ? 1 : 0)); Assert.That(view.Items[icePackage].Contents.Count, Is.EqualTo(cooperative ? 1 : 0));
                Assert.That(view.SupplySources[24].RemainingPackages, Is.EqualTo(4)); Assert.That(view.SupplySources[22].RemainingPackages, Is.EqualTo(4));
            }
            finally { remote?.Dispose(); }
        }

        [UnityTest, Category("TeaBeverageUi"), Timeout(420000)]
        public IEnumerator TeaUiSoloOriginalLauncherThreeRealDrinksBindingAndThirtyPoints() => TeaUiJourney(false);

        [UnityTest, Category("TeaBeverageUi"), Timeout(420000)]
        public IEnumerator TeaUiCooperativeHostBrewsKcpMakesThreeRealDrinksAndDeliversThirtyPoints() => TeaUiJourney(true);
    }
}
#endif
