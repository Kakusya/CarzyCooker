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
        private IEnumerator HotUiCommand(CoopUiRemote remote, string caption, bool prefix = false)
        {
            var hud = remote?.Hud ?? Hud;
            int count = hud.GetComponentsInChildren<Button>(false).Count(button => prefix
                ? button.GetComponentInChildren<Text>().text.StartsWith(caption)
                : button.GetComponentInChildren<Text>().text == caption);
            Assert.That(count, Is.EqualTo(1), "Current hot-food UI action: " + caption);
            yield return AssemblyUiCommand(remote, caption, prefix);
        }

        private IEnumerator HotUiWait(CoopUiRemote remote, System.Func<bool> condition, string reason, float seconds = 20)
        {
            if (remote == null)
            {
                yield return Wait(condition, reason, seconds);
            }
            else
            {
                yield return WaitCoopUi(remote, condition, reason, seconds);
            }
        }

        private IEnumerator HotUiBuyRaw(CoopUiRemote remote, int source, string name, int remaining = 4)
        {
            var view = remote?.Grid ?? Grid;
            string supply = Tables.Instance.DTSupplyConfig.Get(source).Name;
            yield return HotUiCommand(remote, "采购 " + supply, true);
            yield return HotUiWait(remote, () => view.SupplyShipments.Values.Any(shipment => shipment.SourceId == source && shipment.Stage == ShipmentStage.Ready), "Real finite hot food shipment arrives");
            yield return HotUiCommand(remote, "接收 " + supply, true);
            yield return HotUiCommand(remote, "拆一份 " + name, true);
            Assert.That(view.SupplySources[source].RemainingPackages, Is.EqualTo(remaining));
        }

        private IEnumerator HotUiDiscardPackage(CoopUiRemote remote)
        {
            var view = remote?.Grid ?? Grid;
            long package = view.Stations.Values.Single(station => station.Name == "交接台").ItemId;
            Assert.That(view.Items[package].Contents, Is.Empty);
            yield return HotUiCommand(remote, "拿 交接台 ·", true);
            yield return HotUiCommand(remote, "投垃圾 → 垃圾桶");
            Assert.That(view.Items.ContainsKey(package), Is.False);
        }

        private IEnumerator HotUiSwitch(CoopUiRemote remote, bool hostView)
        {
            if (remote == null)
            {
                yield break;
            }
            if (hostView)
            {
                remote.Presentation.CloseGridHud();
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation?.Ready == true, "Host managed controls reopen");
            }
            else
            {
                Presentation.CloseGridHud();
                remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Teammate managed controls reopen");
            }
        }

        private IEnumerator HotUiJourney(bool cooperative, bool breakfast)
        {
            if (cooperative)
            {
                yield return StartProgressHostRoom();
                StartPreviewAndConfirm(EntryButton("开启热餐餐厅"));
            }
            else
            {
                yield return StartModeLauncher();
                CreateSaveUiRepository(Runtime, out _, out _);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Ordinary Solo hot menu selection");
                StartPreviewAndConfirm(SelectionButton("新开热餐餐厅"));
            }
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Ordinary hot restaurant opens");
            Assert.That(Grid.AllowedDishIds, Is.EqualTo(new[] { 6, 7 }));
            ContentUiReadRecipe("经典牛肉汉堡");
            ContentUiReadRecipe("香肠鸡蛋早餐");
            if (breakfast)
            {
                yield return Command("拿 食材台 ·", true);
                yield return Command("放 煎台 ·", true);
                yield return Command("煎制 煎台 ·", true);
                yield return Wait(() => Grid.Stations.Values.Any(station => station.Name == "煎台" && Grid.Items[station.ItemId].DefinitionId == 37), "Original egg really becomes a fried egg");
                yield return Command("拿 煎台 ·", true);
                yield return Command("添加 盘台 ·", true);
                yield return HotUiBuyRaw(null, 11, "生香肠");
                yield return Command("放 煎台 ·", true);
                yield return HotUiDiscardPackage(null);
                yield return Command("拿 面包台 ·", true);
                yield return Command("放 烤箱 ·", true);
            }
            else
            {
                yield return Command("拿 牛肉饼台 ·", true);
                yield return Command("放 煎台 ·", true);
            }
            CoopUiRemote remote = null;
            if (cooperative)
            {
                Presentation.CloseGridHud();
                remote = new CoopUiRemote(Runtime.ListenEndpoint);
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "Independent real KCP cook joins before parallel preparation");
            }
            try
            {
                var view = remote?.Grid ?? Grid;
                var client = remote?.Client ?? Client;
                if (breakfast)
                {
                    if (remote != null)
                    {
                        long movement = remote.Client.NextCommandId + 1;
                        remote.Presentation.ClickGridCell(new GridCell(4, 14));
                        yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == movement && remote.Grid.LastStatus == GridResultStatus.Executed &&
                            remote.Grid.Actors[remote.Client.MemberId].Cell == new GridCell(4, 14), "Teammate moves to the original oven through actual map input");
                    }
                }
                else
                {
                    yield return HotUiBuyRaw(remote, 9, "汉堡面包");
                    if (remote == null)
                    {
                        yield return Command("添加 盘台 ·", true);
                        yield return HotUiDiscardPackage(null);
                    }
                }
                yield return HotUiSwitch(remote, true);
                yield return ContentUiPump(Command("煎制 煎台 ·", true), remote);
                yield return HotUiSwitch(remote, false);
                var frying = view.Stations.Values.Single(station => station.Name == "煎台");
                Assert.That(view.BakeProcesses[frying.BakeProcessId].State, Is.EqualTo(GridBakeState.Baking));
                if (breakfast)
                {
                    yield return HotUiCommand(remote, "烘烤 烤箱 ·", true);
                    var oven = view.Stations.Values.Single(station => station.Name == "烤箱");
                    Assert.That(view.BakeProcesses.Values.Count(process => process.State == GridBakeState.Baking), Is.EqualTo(2), "Frying and original toast baking really overlap");
                    yield return HotUiWait(remote, () => view.Items[view.Stations[oven.Id].ItemId].DefinitionId == 9, "Original toast branch completes");
                    yield return HotUiCommand(remote, "拿 烤箱 ·", true);
                    yield return HotUiCommand(remote, "添加 盘台 ·", true);
                }
                else if (remote != null)
                {
                    yield return HotUiCommand(remote, "添加 盘台 ·", true);
                    yield return HotUiDiscardPackage(remote);
                }
                int cooked = breakfast ? 36 : 34;
                yield return HotUiWait(remote, () => view.Items[view.Stations[frying.Id].ItemId].DefinitionId == cooked, "Correct distinct cooked ingredient is actually available");
                yield return HotUiCommand(remote, "拿 煎台 ·", true);
                yield return HotUiCommand(remote, "添加 盘台 ·", true);
                long plate = view.Stations.Values.Single(station => station.Name == "盘台").ItemId;
                Assert.That(view.Items[plate].Contents.Select(id => view.Items[id].DefinitionId), Is.EquivalentTo(breakfast ? new[] { 9, 37, 36 } : new[] { 32, 34 }));
                yield return HotUiCommand(remote, "拿 盘台 ·", true);
                string name = breakfast ? "香肠鸡蛋早餐" : "经典牛肉汉堡";
                int definition = breakfast ? 39 : 38;
                int dish = breakfast ? 7 : 6;
                yield return HotUiCommand(remote, "组装 " + name);
                yield return HotUiWait(remote, () => view.Items[plate].Contents.Count == 1 && view.Items[view.Items[plate].Contents[0]].DefinitionId == definition, "Actual hot dish assembly consumes exact components once");
                yield return HotUiSwitch(remote, true);
                yield return ContentUiPump(UnlockUiStartOperating(), remote);
                yield return HotUiSwitch(remote, false);
                yield return HotUiWait(remote, () => view.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == dish), "Ordinary customer asks for the actual prepared hot dish", 25);
                var order = view.Orders.Values.First(value => value.State == OrderState.Waiting && value.DishId == dish);
                string table = view.Stations[order.TableStationId].Name;
                yield return HotUiCommand(remote, "前往 " + table);
                yield return HotUiCommand(remote, "交付 " + name + " → " + table);
                Assert.That(view.Orders[order.Id].State, Is.EqualTo(OrderState.Served));
                Assert.That(view.LevelScore, Is.EqualTo(10));
                Assert.That(view.Actors[client.MemberId].HandItemId, Is.Zero);
                yield return HotUiWait(remote, () => view.FrontWorks.Values.Any(work => work.StationId == order.TableStationId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available), "Actual hot meal customer leaves", 20);
                yield return HotUiCommand(remote, "清桌 " + table, true);
                yield return HotUiWait(remote, () => view.Items.TryGetValue(plate, out var carrier) && carrier.Location == GridItemLocation.CleanPool, "Original hot-food plate is washed after player clears", 40);
                Assert.That(view.Items[plate].Hygiene, Is.EqualTo(ItemHygiene.Clean));
                yield return HotUiWait(remote, () => view.LevelPhase == LevelPhase.Completed, "Real hot service naturally drains", 100);
                Assert.That(view.ServedOrders, Is.EqualTo(1));
            }
            finally
            {
                remote?.Dispose();
            }
        }

        [UnityTest, Category("HotFoodUi"), Timeout(300000)]
        public IEnumerator HotFoodUiSoloActualBurgerSupplyAssemblyDeliveryAndOriginalPlateWash() => HotUiJourney(false, false);

        [UnityTest, Category("HotFoodUi"), Timeout(300000)]
        public IEnumerator HotFoodUiSoloActualBreakfastThreeBranchesDeliveryAndOriginalPlateWash() => HotUiJourney(false, true);

        [UnityTest, Category("HotFoodUi"), Timeout(300000)]
        public IEnumerator HotFoodUiCooperativeActualBurgerHostFryingAndKcpAssemblyDelivery() => HotUiJourney(true, false);

        [UnityTest, Category("HotFoodUi"), Timeout(300000)]
        public IEnumerator HotFoodUiCooperativeActualBreakfastParallelFryingAndKcpToastAssemblyDelivery() => HotUiJourney(true, true);
    }
}
#endif
