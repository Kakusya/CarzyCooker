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
        private IEnumerator DessertUiJourney(bool cooperative)
        {
            if (cooperative)
            {
                yield return StartProgressHostRoom(); Pointer(EntryButton("开启甜品餐厅"));
            }
            else
            {
                yield return StartModeLauncher(); CreateSaveUiRepository(Runtime, out _, out _);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Ordinary dessert selection");
                Pointer(SelectionButton("新开甜品餐厅"));
            }
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Actual dessert restaurant opens");
            Assert.That(Grid.AllowedDishIds, Is.EqualTo(new[] { 8 }));
            long command = Client.NextCommandId;
            Pointer(Button("菜谱 原味小蛋糕"));
            string recipe = string.Join("\n", Hud.GetComponentsInChildren<Text>(false).Select(text => text.text));
            Assert.That(recipe, Does.Contain("预拌粉").And.Contain("鸡蛋").And.Contain("牛奶").And.Contain("2 份").And.Contain("脱模"));
            Pointer(Button("关闭菜谱")); Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.Items.Values.Any(item => item.DefinitionId is 46 or 48), Is.False);
            yield return HotUiBuyRaw(null, 12, "甜点预拌粉");
            yield return Command("添加 搅拌机 ·", true); yield return HotUiDiscardPackage(null);
            yield return Command("拿 食材台 ·", true); yield return Command("添加 搅拌机 ·", true);
            yield return HotUiBuyRaw(null, 13, "瓶装牛奶");
            yield return Command("添加 搅拌机 ·", true); yield return HotUiDiscardPackage(null);
            yield return Command("搅拌 搅拌机 ·", true);
            CoopUiRemote remote = null;
            if (cooperative)
            {
                Presentation.CloseGridHud(); remote = new CoopUiRemote(Runtime.ListenEndpoint); ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "Actual independent KCP pastry cook joins");
            }
            try
            {
                var view = remote?.Grid ?? Grid; var client = remote?.Client ?? Client;
                var mixer = view.Stations.Values.Single(station => station.Name == "搅拌机");
                yield return HotUiWait(remote, () => view.AutoProcesses.Values.Any(batch => batch.State == GridAutoState.Ready), "Real mixing completes");
                Assert.That(view.AutoProcesses.Values.Single().RemainingPortions, Is.EqualTo(2));
                long mold = view.Stations.Values.Single(station => station.Name == "模具台").ItemId;
                long plate = view.Stations.Values.Single(station => station.Name == "甜品盘台").ItemId;
                yield return HotUiCommand(remote, "拿 模具台 ·", true);
                yield return HotUiCommand(remote, "装模 搅拌机 ·", true);
                Assert.That(view.AutoProcesses.Values.Single().RemainingPortions, Is.EqualTo(1));
                yield return HotUiCommand(remote, "放 烤箱 ·", true); yield return HotUiCommand(remote, "烘烤 烤箱 ·", true);
                Assert.That(view.BakeProcesses.Values.Single(work => work.State == GridBakeState.Baking).CarrierItemId, Is.EqualTo(mold));
                yield return HotUiWait(remote, () => view.Items[mold].Contents.Count == 1 && view.Items[view.Items[mold].Contents[0]].DefinitionId == 48, "Original mold contains its actually baked cake");
                long firstCake = view.Items[mold].Contents.Single();
                yield return HotUiCommand(remote, "拿 烤箱 ·", true); yield return HotUiCommand(remote, "脱模 甜品盘台 ·", true);
                Assert.That(view.Items[plate].Contents, Is.EqualTo(new[] { firstCake }));
                Assert.That(view.Actors[client.MemberId].HandItemId, Is.EqualTo(mold));
                Assert.That(view.Items[mold].Contents, Is.Empty);
                yield return HotUiCommand(remote, "装模 搅拌机 ·", true);
                Assert.That(view.AutoProcesses, Is.Empty);
                Assert.That(view.Items[mixer.ItemId].Contents, Is.Empty);
                Assert.That(view.Items[view.Items[mold].Contents.Single()].DefinitionId, Is.EqualTo(46));
                yield return HotUiCommand(remote, "放 烤箱 ·", true); yield return HotUiCommand(remote, "烘烤 烤箱 ·", true);
                yield return HotUiSwitch(remote, true); yield return ContentUiPump(UnlockUiStartOperating(), remote);
                yield return HotUiSwitch(remote, false);
                yield return HotUiCommand(remote, "拿 甜品盘台 ·", true);
                yield return HotUiWait(remote, () => view.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == 8), "Real customer orders S04", 25);
                var order = view.Orders.Values.First(value => value.State == OrderState.Waiting && value.DishId == 8);
                string table = view.Stations[order.TableStationId].Name;
                yield return HotUiCommand(remote, "前往 " + table); yield return HotUiCommand(remote, "交付 原味小蛋糕 → " + table);
                Assert.That(view.Orders[order.Id].State, Is.EqualTo(OrderState.Served));
                Assert.That(view.LevelScore, Is.EqualTo(10)); Assert.That(view.Items.ContainsKey(firstCake), Is.False);
                Assert.That(view.Items[mold].Contents.Count, Is.EqualTo(1));
                Assert.That(view.Items[view.Items[mold].Contents.Single()].DefinitionId, Is.EqualTo(48), "The same mold has really baked its second portion");
                Assert.That(view.Items.Values.Count(item => item.DefinitionId == 47), Is.EqualTo(2));
                yield return HotUiWait(remote, () => view.FrontWorks.Values.Any(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available), "S04 customer leaves", 20);
                yield return HotUiCommand(remote, "清桌 " + table, true);
                yield return HotUiWait(remote, () => view.Items.TryGetValue(plate, out var item) && item.Location == GridItemLocation.CleanPool, "Player clears and partner washes original dessert plate", 40);
                Assert.That(view.Items[plate].Hygiene, Is.EqualTo(ItemHygiene.Clean));
                yield return HotUiWait(remote, () => view.LevelPhase == LevelPhase.Completed, "Dessert service naturally completes", 100);
                Assert.That(view.ServedOrders, Is.EqualTo(1));
            }
            finally
            {
                remote?.Dispose();
            }
        }

        [UnityTest, Category("DessertUi"), Timeout(420000)]
        public IEnumerator DessertUiSoloActualThreeIngredientsBakeUnmoldDeliverySameMoldReuseAndPlateWash() => DessertUiJourney(false);

        [UnityTest, Category("DessertUi"), Timeout(420000)]
        public IEnumerator DessertUiCooperativeActualHostMixAndKcpMoldBakeUnmoldDeliveryReuseAndWash() => DessertUiJourney(true);
    }
}
#endif
