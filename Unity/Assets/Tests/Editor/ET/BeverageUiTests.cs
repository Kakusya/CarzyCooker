#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private IEnumerator BeverageUiJourney(bool cooperative)
        {
            if (cooperative)
            {
                yield return StartProgressHostRoom(); yield return StartPreviewAndConfirm(EntryButton("开启咖啡餐厅"));
            }
            else
            {
                yield return StartModeLauncher(); CreateSaveUiRepository(Runtime, out _, out _);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Ordinary coffee menu selection");
                yield return StartPreviewAndConfirm(SelectionButton("新开咖啡餐厅"));
            }
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Actual coffee restaurant opens");
            Assert.That(Grid.AllowedDishIds, Is.EqualTo(new[] { 9, 10, 11 }));
            ContentUiReadRecipe("浓缩咖啡"); ContentUiReadRecipe("热拿铁"); ContentUiReadRecipe("卡布奇诺");
            yield return HotUiBuyRaw(null, 14, "咖啡豆");
            yield return Command("放 磨豆机 ·", true); yield return HotUiDiscardPackage(null);
            yield return Command("研磨 磨豆机 ·", true);
            yield return Wait(() => Grid.Items[Grid.Stations.Values.Single(station => station.Name == "磨豆机").ItemId].DefinitionId == 53, "Real raw beans become grounds");
            yield return Command("拿 磨豆机 ·", true); yield return Command("添加 咖啡机 ·", true);
            yield return HotUiBuyRaw(null, 13, "瓶装牛奶");
            yield return Command("添加 蒸汽机 ·", true); yield return HotUiDiscardPackage(null);
            CoopUiRemote remote = null;
            if (cooperative)
            {
                Presentation.CloseGridHud(); remote = new CoopUiRemote(Runtime.ListenEndpoint); ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "Independent KCP barista joins");
                long movement = remote.Client.NextCommandId + 1;
                remote.Presentation.ClickGridCell(new GridCell(4, 13));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == movement && remote.Grid.LastStatus == GridResultStatus.Executed &&
                    remote.Grid.Actors[remote.Client.MemberId].Cell == new GridCell(4, 13), "KCP barista approaches steam machine through map input");
            }
            try
            {
                var view = remote?.Grid ?? Grid; var client = remote?.Client ?? Client;
                yield return HotUiSwitch(remote, true); yield return ContentUiPump(Command("萃取 咖啡机 ·", true), remote);
                yield return HotUiSwitch(remote, false); yield return HotUiCommand(remote, "热奶 蒸汽机 ·", true);
                Assert.That(view.AutoProcesses.Values.Count(work => work.State == GridAutoState.Cooking), Is.EqualTo(2), "Actual coffee and hot milk run concurrently");
                yield return HotUiWait(remote, () => view.AutoProcesses.Values.Count(work => work.State == GridAutoState.Ready) == 2, "Both independent bases finish");
                long firstCup = view.Stations.Values.Single(station => station.Name == "热饮杯台").ItemId;
                yield return HotUiCommand(remote, "拿 热饮杯台 ·", true); yield return HotUiCommand(remote, "放 切配台 ·", true);
                yield return HotUiCommand(remote, "拿 蒸汽机 ·", true); yield return HotUiCommand(remote, "放 热饮杯台 ·", true);
                yield return HotUiCommand(remote, "拿 第二奶缸台 ·", true); yield return HotUiCommand(remote, "放 蒸汽机 ·", true);
                yield return HotUiBuyRaw(remote, 13, "瓶装牛奶", remaining: 3);
                yield return HotUiCommand(remote, "添加 蒸汽机 ·", true); yield return HotUiDiscardPackage(remote);
                yield return HotUiCommand(remote, "打泡 蒸汽机 ·", true);
                yield return HotUiWait(remote, () => view.AutoProcesses.Values.Any(work => work.RequiredCapability == GridStationCapability.FoamMilk && work.State == GridAutoState.Ready), "Explicit foam mode produces foam");
                yield return HotUiCommand(remote, "拿 切配台 ·", true); yield return HotUiCommand(remote, "取一份 咖啡机 ·", true);
                Assert.That(view.Items[firstCup].Contents.Select(id => view.Items[id].DefinitionId), Is.EqualTo(new[] { 56 }));
                yield return HotUiCommand(remote, "放 第二奶缸台 ·", true);
                yield return HotUiBuyRaw(remote, 15, "热饮杯"); long latte = view.Actors[client.MemberId].HandItemId;
                yield return HotUiCommand(remote, "取一份 咖啡机 ·", true);
                yield return HotUiCommand(remote, "取一份 热饮杯台 ·", true); yield return HotUiCommand(remote, "取一份 热饮杯台 ·", true);
                Assert.That(view.Items[latte].Contents.Select(id => view.Items[id].DefinitionId), Is.EqualTo(new[] { 56, 57, 57 }));
                yield return HotUiCommand(remote, "混合 热拿铁");
                yield return HotUiWait(remote, () => view.Items[latte].Contents.Count == 1 && view.Items[view.Items[latte].Contents[0]].DefinitionId == 60, "Exact latte portions become one finished drink");
                yield return HotUiCommand(remote, "放 切配台 ·", true);
                yield return HotUiCommand(remote, "拆一份 热饮杯", true); long cappuccino = view.Actors[client.MemberId].HandItemId;
                yield return HotUiCommand(remote, "取一份 咖啡机 ·", true); yield return HotUiCommand(remote, "取一份 热饮杯台 ·", true);
                Assert.That(view.Items[cappuccino].Contents.Select(id => view.Items[id].DefinitionId), Is.EqualTo(new[] { 56, 57 }));
                yield return HotUiCommand(remote, "取一份 蒸汽机 ·", true);
                Assert.That(view.Items[cappuccino].Contents.Select(id => view.Items[id].DefinitionId), Is.EqualTo(new[] { 56, 57, 58 }), "Foam is really added after coffee and milk");
                yield return HotUiCommand(remote, "混合 卡布奇诺");
                yield return HotUiWait(remote, () => view.Items[cappuccino].Contents.Count == 1 && view.Items[view.Items[cappuccino].Contents[0]].DefinitionId == 61, "Cappuccino finishes after foam");
                yield return HotUiCommand(remote, "放 磨豆机 ·", true);
                yield return HotUiSwitch(remote, true); yield return ContentUiPump(UnlockUiStartOperating(), remote); yield return HotUiSwitch(remote, false);
                var names = new[] { "浓缩咖啡", "热拿铁", "卡布奇诺" }; var sources = new[] { "第二奶缸台", "切配台", "磨豆机" };
                var cups = new[] { firstCup, latte, cappuccino };
                for (int index = 0; index < names.Length; ++index)
                {
                    int dish = index + 9; string name = names[index]; long cup = cups[index];
                    yield return HotUiWait(remote, () => view.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == dish), "Actual customer orders " + name, 30);
                    var order = view.Orders.Values.First(value => value.State == OrderState.Waiting && value.DishId == dish); string table = view.Stations[order.TableStationId].Name;
                    yield return HotUiCommand(remote, "拿 " + sources[index] + " ·", true); yield return HotUiCommand(remote, "放 贴单台 ·", true);
                    yield return HotUiCommand(remote, "贴单 → " + table); Assert.That(view.Items[cup].BoundOrderId, Is.EqualTo(order.Id));
                    yield return HotUiCommand(remote, "拿 贴单台 ·", true); yield return HotUiCommand(remote, "前往 " + table);
                    yield return HotUiCommand(remote, "交付 " + name + " → " + table);
                    Assert.That(view.Orders[order.Id].State, Is.EqualTo(OrderState.Served));
                    Assert.That(view.LevelScore, Is.EqualTo((index + 1) * 10)); Assert.That(view.Items.ContainsKey(cup), Is.False);
                    yield return HotUiWait(remote, () => view.FrontWorks.Values.Any(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available), "Coffee customer leaves before player clears", 20);
                    long clear = view.FrontWorks.Values.Single(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available).Id;
                    yield return HotUiCommand(remote, "清桌 " + table, true);
                    yield return HotUiWait(remote, () => view.FrontWorks[clear].State == FrontWorkState.Completed, "Player finishes clearing before taking the next cup", 20);
                }
                Assert.That(view.ServedOrders, Is.EqualTo(3)); Assert.That(view.LevelScore, Is.EqualTo(30));
            }
            finally
            {
                remote?.Dispose();
            }
        }

        [UnityTest, Category("BeverageUi"), Timeout(600000)]
        public IEnumerator BeverageUiSoloActualThreeDrinksFiniteBasesFinalFoamBindingAndThirtyPointDelivery() => BeverageUiJourney(false);

        [UnityTest, Category("BeverageUi"), Timeout(600000)]
        public IEnumerator BeverageUiCooperativeActualParallelHostExtractionKcpMilkAndThreeDrinksDelivery() => BeverageUiJourney(true);
    }
}
#endif
