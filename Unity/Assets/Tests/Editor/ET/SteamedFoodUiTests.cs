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
        private IEnumerator SteamedUiJourney(bool cooperative)
        {
            if (cooperative)
            {
                yield return StartProgressHostRoom(); yield return StartPreviewAndConfirm(EntryButton("开启蒸食餐厅"));
            }
            else
            {
                yield return StartModeLauncher(); CreateSaveUiRepository(Runtime, out _, out _);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Ordinary steamed selection");
                yield return StartPreviewAndConfirm(SelectionButton("新开蒸食餐厅"));
            }
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Original steamed kitchen entry");
            long before = Client.NextCommandId;
            Pointer(Button("菜谱 牛肉蒸饺"));
            string recipe = string.Join("\n", Hud.GetComponentsInChildren<Text>(false).Select(text => text.text));
            Assert.That(recipe, Does.Contain("牛肉末 ×1").And.Contain("洋葱块 ×1").And.Contain("饺子皮 ×1").And.Contain("蒸制").And.Contain("装盘"));
            Pointer(Button("关闭菜谱")); Assert.That(Client.NextCommandId, Is.EqualTo(before));
            yield return Command("拿 洋葱台 ·", true); yield return Command("放 切配台 ·", true); yield return Command("切配 切配台 ·", true);
            yield return Wait(() => Grid.Processes.Values.Any(work => work.OutputItemId != 0 && Grid.Items[work.OutputItemId].DefinitionId == 65), "Real onion chopping");
            yield return Command("拿 切配台 ·", true); yield return Command("添加 拌馅盆台 ·", true);
            yield return Command("拿 牛肉末台 ·", true); yield return Command("添加 拌馅盆台 ·", true);
            yield return Command("拿 拌馅盆台 ·", true); yield return Command("组装 牛肉洋葱馅");
            long bowl = Grid.Actors[Client.MemberId].HandItemId;
            yield return Wait(() => Grid.Items[bowl].Contents.Count == 1 && Grid.Items[Grid.Items[bowl].Contents[0]].DefinitionId == 68, "Real mixed filling");
            yield return Command("放 拌馅盆台 ·", true);
            CoopUiRemote remote = null;
            if (cooperative)
            {
                Presentation.CloseGridHud(); ((GridKitchenBoard)Presentation.Board).Visible = false;
                remote = new CoopUiRemote(Runtime.ListenEndpoint); ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "Actual KCP wrapper joins");
            }
            try
            {
                var view = remote?.Grid ?? Grid; var client = remote?.Client ?? Client;
                var presentation = remote?.Presentation ?? Presentation;
                yield return HotUiCommand(remote, "拿 饺子皮台 ·", true); yield return HotUiCommand(remote, "添加 拌馅盆台 ·", true);
                yield return HotUiCommand(remote, "拿 拌馅盆台 ·", true); yield return HotUiCommand(remote, "组装 未熟牛肉蒸饺");
                yield return HotUiCommand(remote, "停止组装");
                var retired = CaptureButtonCallback(ClaimUiButton(remote?.Hud ?? Hud, "继续组装"));
                presentation.CloseGridHud(); yield return null; presentation.OpenGridHud();
                yield return HotUiWait(remote, () => presentation.Ready, "Pooled original steamed HUD reopens");
                before = client.NextCommandId; retired(); Assert.That(client.NextCommandId, Is.EqualTo(before));
                yield return HotUiCommand(remote, "继续组装");
                yield return HotUiWait(remote, () => view.Items[bowl].Contents.Count == 1 && view.Items[view.Items[bowl].Contents[0]].DefinitionId == 69, "Real one wrapped dumpling");
                long raw = view.Items[bowl].Contents.Single();
                yield return HotUiCommand(remote, "放 拌馅盆台 ·", true);
                Pointer(ClaimUiButton(remote?.Hud ?? Hud, "查看详情"));
                yield return HotUiCommand(remote, "取出 未熟牛肉蒸饺 · 拌馅盆台");
                Assert.That(view.Actors[client.MemberId].HandItemId, Is.EqualTo(raw));
                yield return HotUiCommand(remote, "放 蒸锅 ·", true); yield return HotUiCommand(remote, "蒸制 蒸锅 ·", true);
                yield return HotUiWait(remote, () => view.Items.Values.Any(item => item.DefinitionId == 70), "Real steamed dumpling completion", 20);
                long cooked = view.Items.Values.Single(item => item.DefinitionId == 70).Id;
                yield return HotUiCommand(remote, "拿 蒸锅 ·", true); yield return HotUiCommand(remote, "添加 蒸食盘台 ·", true);
                long plate = view.Stations.Values.Single(station => station.Name == "蒸食盘台").ItemId;
                yield return HotUiSwitch(remote, true); yield return ContentUiPump(UnlockUiStartOperating(), remote); yield return HotUiSwitch(remote, false);
                yield return HotUiCommand(remote, "拿 蒸食盘台 ·", true);
                yield return HotUiWait(remote, () => view.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == 12), "Real F28 customer order", 25);
                var order = view.Orders.Values.First(value => value.State == OrderState.Waiting && value.DishId == 12);
                string table = view.Stations[order.TableStationId].Name;
                yield return HotUiCommand(remote, "前往 " + table); yield return HotUiCommand(remote, "交付 牛肉蒸饺 → " + table);
                Assert.That(view.LevelScore, Is.EqualTo(10)); Assert.That(view.ServedOrders, Is.EqualTo(1));
                Assert.That(view.Items.ContainsKey(cooked), Is.False); Assert.That(view.Items[bowl].Contents, Is.Empty);
                yield return HotUiWait(remote, () => view.FrontWorks.Values.Any(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available), "Customer leaves original plate", 20);
                yield return HotUiCommand(remote, "清桌 " + table, true);
                yield return HotUiWait(remote, () => view.Items.TryGetValue(plate, out var item) && item.Location == GridItemLocation.CleanPool, "Original steam plate washed", 40);
                Assert.That(view.Items[plate].Hygiene, Is.EqualTo(ItemHygiene.Clean));
                Assert.That(view.Items.Values.Count(item => item.DefinitionId == 71), Is.EqualTo(1));
            }
            finally { remote?.Dispose(); }
        }

        [UnityTest, Category("SteamedFoodUi"), Timeout(300000)]
        public IEnumerator SteamedUiSoloOriginalLauncherMixWrapSteamServeAndWash() => SteamedUiJourney(false);

        [UnityTest, Category("SteamedFoodUi"), Timeout(300000)]
        public IEnumerator SteamedUiCooperativeHostMixKcpWrapSteamServeAndWash() => SteamedUiJourney(true);
    }
}
#endif
