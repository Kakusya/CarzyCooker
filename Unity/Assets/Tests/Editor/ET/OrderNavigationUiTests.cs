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
        private static string OrderNavigationCaption(GridClientComponent grid, GridOrderProjection order) =>
            "制作 · " + grid.Stations[order.TableStationId].Name + " · " + Tables.Instance.DTDishConfig.Get(order.DishId).Name;

        [UnityTest, Category("EntryExperience"), Timeout(150000)]
        public IEnumerator EntryOrderNavigationSoloChoosesSameDishReadsItemsAndExpiresWithoutRetargeting()
        {
            yield return StartModeLauncher();
            var runtime = Runtime;
            var repository = CreateSaveUiRepository(runtime, out string directory, out _);
            try
            {
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Normal content selector");
                yield return StartPreviewAndConfirm(SelectionButton("新开大关 1 · 家常开张"));
                yield return Wait(() => Presentation?.Ready == true && Grid.BuffDrawVersion > 0, "Actual first content chapter");
                yield return UnlockUiStartOperating();
                yield return Wait(() => Grid.Orders.Values.Count(order => order.State == OrderState.Waiting && order.DishId == 1) == 2,
                    "Two actual customers order the same soup", 30);
                yield return Command("暂停");
                var orders = Grid.Orders.Values.Where(order => order.State == OrderState.Waiting && order.DishId == 1).OrderBy(order => order.Id).ToArray();
                long command = Client.NextCommandId;
                int items = Grid.Items.Count;
                Pointer(Button(OrderNavigationCaption(Grid, orders[0])));
                Assert.That(Presentation.RecipeOrderId, Is.EqualTo(orders[0].Id));
                Pointer(Button(OrderNavigationCaption(Grid, orders[1])));
                Assert.That(Presentation.RecipeOrderId, Is.EqualTo(orders[1].Id));
                Assert.That(Hud.TargetText.text, Does.Contain(Grid.Stations[orders[1].TableStationId].Name));
                Assert.That(Hud.TargetText.text, Does.Contain("顾客"));
                Assert.That(Hud.DetailsText.text, Does.Contain("只读目标"));
                Pointer(Button("订单：完整菜谱"));
                Assert.That(Hud.DetailsText.text, Does.Contain("备料").And.Contain("交付"));
                Pointer(Button("订单：制作缺口"));
                var itemLink = Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("订单：物品 "));
                Pointer(itemLink);
                Assert.That(Presentation.RecipeItemId, Is.Not.Zero);
                Assert.That(Hud.DetailsText.text, Does.Contain("这里只查看"));
                Pointer(Button("返回订单制作"));
                Assert.That(Presentation.RecipeItemId, Is.Zero);
                Pointer(Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("订单：查看 ")));
                Assert.That(Hud.DetailsText.text, Does.Contain("位置"));
                Pointer(Button("返回订单制作"));
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                Assert.That(Grid.Items.Count, Is.EqualTo(items));
                Assert.That(Grid.Items.Values.All(item => item.BoundOrderId == 0), Is.True);
                var retired = CaptureButtonCallback(Button(OrderNavigationCaption(Grid, orders[1])));
                Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
                yield return Wait(() => Presentation.Ready, "Managed order navigation reopens");
                retired();
                Assert.That(Presentation.RecipeOrderId, Is.Zero);
                Pointer(Button("全部动作"));
                yield return Command("恢复");
                var tomatoSource = Grid.Stations.Values.Single(station => Grid.Items.TryGetValue(station.ItemId, out var item) && item.DefinitionId == GridKitchenRules.Tomato);
                yield return Command("拿 " + tomatoSource.Name + " ·", true);
                yield return Command("放 切配台 ·", true);
                yield return Command("切配 切配台 ·", true);
                yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.TomatoChunks), "Original real cutting finishes");
                yield return Command("暂停");
                Assert.That(Grid.Orders[orders[1].Id].State, Is.EqualTo(OrderState.Waiting));
                Pointer(Button(OrderNavigationCaption(Grid, orders[1])));
                Assert.That(Hud.DetailsText.text, Does.Not.Contain("切配番茄"), "Actual prepared input is not requested again");
                var previousOrderButton = CaptureButtonCallback(Button(OrderNavigationCaption(Grid, orders[1])));
                yield return Command("恢复");
                yield return Wait(() => Grid.Orders[orders[1].Id].State != OrderState.Waiting && Presentation.RecipeOrderId == 0,
                    "Actual selected order expires and does not retarget", 30);
                Assert.That(Presentation.OrderNavigationFeedback, Does.Contain("已结束"));
                command = Client.NextCommandId;
                previousOrderButton();
                Assert.That(Presentation.RecipeOrderId, Is.Zero);
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
            }
            finally
            {
                runtime.StopProduct(); repository.Dispose(); RemoveOwnSaveUiFiles(directory);
            }
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntryOrderNavigationActualKcpReadsSpecificCustomerAndReturnsToOriginalPickup()
        {
            yield return StartProgressHostRoom();
            yield return StartPreviewAndConfirm(EntryButton("开启大关 1 · 家常开张"));
            yield return Wait(() => Presentation?.Ready == true && Grid.BuffDrawVersion > 0, "Host content chapter ready");
            yield return UnlockUiStartOperating();
            yield return Wait(() => Grid.Orders.Values.Count(order => order.State == OrderState.Waiting && order.DishId == 1) == 2,
                "Two cooperative soup orders", 30);
            yield return Command("暂停");
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases display");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Orders.Values.Count(order => order.State == OrderState.Waiting) == 2,
                    "Actual KCP projection carries both orders");
                var order = remote.Grid.Orders.Values.OrderBy(value => value.Id).Last();
                long command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, OrderNavigationCaption(remote.Grid, order)));
                Assert.That(remote.Presentation.RecipeOrderId, Is.EqualTo(order.Id));
                Assert.That(remote.Hud.TargetText.text, Does.Contain(remote.Grid.Stations[order.TableStationId].Name));
                Pointer(ClaimUiButton(remote.Hud, "关闭订单导航"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                remote.Presentation.CloseGridHud();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Client releases display");
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Host returns to original controls");
                yield return ContentUiPump(Command("恢复"), remote);
                yield return ContentUiPump(Command("拿 碗台 ·", true), remote);
                Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Not.Zero);
            }
        }
    }

    public sealed partial class ProductSessionTests
    {
        [Test, Category("EntryExperience")]
        public void EntryOrderGuideActualBoundDrinkKeepsOriginalOrderWhenAnotherIsSelected()
        {
            DrinkOnlyCustomers(); StartSolo(liveFront: true, liveDrinks: true);
            var batch = DrinkBegin(); long cup = DrinkFirstCup(batch);
            var label = PrepStation("贴单台");
            PrepDo(host.Client, GridAction.PlaceCounter, label.Id, cup);
            LevelDo(LevelControlAction.Start);
            FrontWait(() => CurrentLevel.Children.Values.OfType<Order>().Count(order => order.State == OrderState.Waiting) == 2, 400);
            var orders = CurrentLevel.Children.Values.OfType<Order>().Where(order => order.State == OrderState.Waiting).OrderBy(order => order.Id).ToArray();
            PrepDo(host.Client, GridAction.BindDrink, label.Id, cup, cup, order: orders[1].Id);
            var client = (ClientSessionComponent)host.Client;
            var view = client.GetComponent<GridClientComponent>();
            long command = client.NextCommandId;
            string need = GridRecipeGuide.CurrentNeed(view, 3, orders[0].Id);
            Assert.That(need, Does.Contain("此杯已贴另一有效订单").And.Contain(Grid.Station(orders[1].TableStationId).Name));
            Assert.That(CurrentLevel.DrinkBindings[cup], Is.EqualTo(orders[1].Id));
            Assert.That(view.Items[cup].BoundOrderId, Is.EqualTo(orders[1].Id));
            Assert.That(client.NextCommandId, Is.EqualTo(command));
            FrontWait(() => orders[0].State != OrderState.Waiting, 1600);
            Assert.That(GridRecipeGuide.CurrentNeed(view, 3, orders[0].Id), Does.Contain("所选订单已结束"));
            Assert.That(client.NextCommandId, Is.EqualTo(command));
        }
    }
}
#endif
