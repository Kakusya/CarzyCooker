#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using ET.Client;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("EntryExperience"), Category("EntryServiceRecap"), Timeout(180000)]
        public IEnumerator EntryServiceOverviewSoloActualDeliveryExpiryAndFinalScoreRemainReadOnly()
        {
            yield return StartLauncher();
            yield return CoopFrontUiPrepareSoup();
            var restaurant = (RestaurantRuntime)((Match)((HostSessionComponent)Runtime.Authority).Match).Restaurant;
            var level = (Level)restaurant.CurrentLevel;
            level.OperatingTicks = 100;
            ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
            long command = Client.NextCommandId;
            Pointer(Button("营业总览"));
            Assert.That(Hud.DetailsText.text, Does.Contain("尚未开店").And.Contain("暂无待交付订单"));
            Pointer(Button("总览：客流"));
            Assert.That(Hud.DetailsText.text, Does.Contain("队列占用 0/").And.Contain("不是超时订单"));
            var table = Grid.Stations[Grid.Tables.Keys.First()];
            try
            {
                // A controlled current projection checks the display distinction; admission remains the original Host rule.
                Grid.Stations[table.Id] = new GridStationProjection(table.Id, table.Cell, table.Name, Grid.Items.Keys.First(),
                    table.Kind, table.AutoProcessId, table.DefinitionId, table.Orientation, table.ItemSlotCapacity,
                    table.Capabilities, table.Footprint, table.Approaches, table.BakeProcessId, table.PoolItemIds,
                    table.WaterDefinitionId, table.WaterUnits, table.UpgradeDefinitionId, table.UpgradeLevel);
                Presentation.RefreshGridHud();
                Assert.That(Hud.DetailsText.text, Does.Contain("空桌被物占用 1"));
            }
            finally
            {
                Grid.Stations[table.Id] = table;
                Presentation.RefreshGridHud();
            }
            Pointer(Button("总览：得分"));
            Assert.That(Hud.DetailsText.text, Does.Contain("未结算").And.Contain("1 星门槛"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("关闭营业总览"));
            yield return Command("开始营业");
            yield return Wait(() => Grid.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == 1), "Actual soup order");
            var order = Grid.Orders.Values.First(order => order.State == OrderState.Waiting && order.DishId == 1);
            yield return Command("暂停");
            command = Client.NextCommandId;
            Pointer(Button("营业总览"));
            Pointer(Button("需求 · 番茄蛋花汤"));
            Assert.That(Hud.DetailsText.text, Does.Contain("番茄蛋花汤：待交付 1 单").And.Contain("已暂停"));
            string frozen = Hud.DetailsText.text;
            yield return null;
            yield return null;
            Assert.That(Hud.DetailsText.text, Is.EqualTo(frozen));
            var row = Hud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith("总览订单 ·"));
            var retired = CaptureButtonCallback(row);
            Pointer(row);
            Assert.That(Presentation.RecipeOrderId, Is.EqualTo(order.Id));
            Pointer(Button("返回营业总览"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Service));
            Pointer(Button("关闭营业总览"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Command("恢复");
            yield return Command("拿 碗台 ·", true);
            string tableName = Grid.Stations[order.TableStationId].Name;
            yield return Command("前往 " + tableName);
            yield return Command("交付 番茄蛋花汤 → " + tableName);
            Assert.That(Grid.Orders[order.Id].State, Is.EqualTo(OrderState.Served));
            Assert.That(GridPresentationSystem.ServiceDemandCount(Grid, 1), Is.Zero);
            Assert.That(Hud.ScoreText.text, Does.Contain("参考").And.Contain("未结算"));
            Assert.That(level.Stars, Is.Zero, "Reading and scoring references do not commit final stars");
            Pointer(Button("营业总览"));
            Pointer(Button("总览：得分"));
            Assert.That(Hud.DetailsText.text, Does.Contain("当前已提交 " + Grid.LevelScore + " 分"));
            command = Client.NextCommandId;
            retired();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Service));
            float closingStarted = UnityEngine.Time.realtimeSinceStartup;
            while (Grid.LevelPhase != LevelPhase.Completed || Grid.RenovationChoiceVersion == 0)
            {
                Assert.That(UnityEngine.Time.realtimeSinceStartup - closingStarted, Is.LessThan(65),
                    "Real front closure requires explicit player clear work, not an increased timeout");
                var clear = Grid.FrontWorks.Values.FirstOrDefault(work => work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available);
                if (clear.Id != 0 && Grid.Actors[Client.MemberId].FrontWorkId == 0)
                {
                    yield return Command("清桌 " + Grid.Stations[clear.StationId].Name);
                }
                else
                {
                    yield return null;
                }
            }
            Assert.That(Grid.UnfulfilledOrders, Is.GreaterThan(0));
            Assert.That(Grid.LevelScore, Is.EqualTo(order.Score), "Expired orders do not subtract score");
            Assert.That(Grid.LevelStars, Is.EqualTo(GridPresentationSystem.ServiceReferenceStars(Grid)));
            Assert.That(Hud.DetailsText.text, Does.Contain("最终结算"));
            Assert.That(Hud.ScoreText.text, Does.Not.Contain("未结算"));
            Pointer(Button("总览：原结算"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Settlement));
            command = Client.NextCommandId;
            Pointer(Button("本关复盘"));
            Assert.That(Hud.DetailsText.text, Does.Contain("番茄蛋花汤：已交付 1 单").And.Contain("贡献 10 分")
                .And.Contain("盘装吐司：已交付 0 单").And.Not.Contain("结果尚未完整"));
            Assert.That(Hud.DetailsText.text, Does.Contain($"订单合计：已交付 {Grid.ServedOrders} 单 · 超时 {Grid.UnfulfilledOrders} 单 · 贡献 {Grid.LevelScore} 分"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            var oldRecapReturn = CaptureButtonCallback(Button("复盘：返回结算"));
            Pointer(Button("复盘：返回结算"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Settlement));
            Pointer(Button("营业总览"));
            Pointer(Button("总览：得分"));
            Pointer(Button("总览：原结算"));
            Pointer(Button("返回营业总览"));
            Pointer(Button("关闭营业总览"));
            Presentation.CloseGridHud();
            yield return null;
            Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Service overview HUD pool reuse");
            command = Client.NextCommandId;
            retired();
            oldRecapReturn();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
        }

        [UnityTest, Category("EntryExperience"), Category("EntryServiceRecap"), Timeout(180000)]
        public IEnumerator EntryServiceOverviewActualFullQueueZeroScoreAndNextLevelRetireOldRows()
        {
            yield return StartLauncher();
            var restaurant = (RestaurantRuntime)((Match)((HostSessionComponent)Runtime.Authority).Match).Restaurant;
            var level = (Level)restaurant.CurrentLevel;
            level.OperatingTicks = 300;
            ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
            yield return UnlockUiStartOperating();
            Pointer(Button("营业总览"));
            Pointer(Button("总览：客流"));
            yield return Wait(() => Grid.SkippedArrivals > 0, "Actual bounded queue skips while occupied/dirty tables remain unavailable", 40);
            var flow = Tables.Instance.DTCustomerFlowConfig.DataList.Single(row => row.LevelConfigId == Grid.LevelConfigId);
            Assert.That(Hud.DetailsText.text, Does.Contain($"队列占用 {Grid.WaitingCustomers.Count}/{flow.QueueCapacity}"));
            Assert.That(Hud.DetailsText.text, Does.Contain($"本关满队列跳过来客 {Grid.SkippedArrivals}"));
            Assert.That(Grid.WaitingCustomers.Count, Is.LessThanOrEqualTo(flow.QueueCapacity));
            Assert.That(Grid.LevelScore, Is.Zero);
            var oldRow = CaptureButtonCallback(Button("总览：客流"));
            float closing = UnityEngine.Time.realtimeSinceStartup;
            while (Grid.LevelPhase != LevelPhase.Completed || Grid.RenovationChoiceVersion == 0)
            {
                Assert.That(UnityEngine.Time.realtimeSinceStartup - closing, Is.LessThan(80), "Queue customers naturally expire/leave and actual clear work completes");
                var clear = Grid.FrontWorks.Values.FirstOrDefault(work => work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available);
                if (clear.Id != 0 && Grid.Actors[Client.MemberId].FrontWorkId == 0)
                {
                    yield return Command("清桌 " + Grid.Stations[clear.StationId].Name);
                }
                else
                {
                    yield return null;
                }
            }
            Pointer(Button("总览：得分"));
            Assert.That(Hud.DetailsText.text, Does.Contain("最终结算 0 星"));
            Assert.That(Grid.LevelScore, Is.Zero);
            Assert.That(Grid.UnfulfilledOrders, Is.GreaterThan(0));
            long readCommand = Client.NextCommandId;
            Pointer(Button("本关复盘"));
            Assert.That(Hud.DetailsText.text, Does.Contain("本关结果：0 分 · 0 星").And.Contain("已交付 0 单")
                .And.Contain($"超时 {Grid.UnfulfilledOrders} 单 · 贡献 0 分").And.Not.Contain("结果尚未完整"));
            var oldRecapReturn = CaptureButtonCallback(Button("复盘：返回结算"));
            Pointer(Button("复盘：返回结算"));
            Assert.That(Client.NextCommandId, Is.EqualTo(readCommand));
            Pointer(Button("关闭结算详情"));
            yield return Command("下一关");
            long command = Client.NextCommandId;
            oldRow();
            oldRecapReturn();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Pointer(Button("营业总览"));
            Assert.That(Hud.DetailsText.text, Does.Contain("尚未开店").And.Contain("暂无待交付订单"));
        }

        [UnityTest, Category("EntryExperience"), Category("EntryServiceRecap"), Timeout(180000)]
        public IEnumerator EntryServiceOverviewActualKcpIdleViewerReceivesHostDeliveryAndReopensWithoutCommands()
        {
            yield return StartCoopHostLauncher();
            var restaurant = (RestaurantRuntime)((Match)((HostSessionComponent)Runtime.Authority).Match).Restaurant;
            ((Level)restaurant.CurrentLevel).OperatingTicks = 100;
            yield return CoopFrontUiPrepareSoup();
            yield return Command("拿 碗台 ·", true);
            long bowl = Grid.Actors[Client.MemberId].HandItemId;
            yield return Command("开始营业");
            yield return Wait(() => Grid.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == 1), "Actual cooperative soup order");
            var order = Grid.Orders.Values.First(order => order.State == OrderState.Waiting && order.DishId == 1);
            yield return Command("前往 " + Grid.Stations[order.TableStationId].Name);
            yield return Command("暂停");
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases singleton HUD while keeping local client");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Real KCP overview ready");
                long command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "营业总览"));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("已暂停").And.Contain("番茄蛋花汤：待交付 1 单"));
                string frozen = remote.Hud.DetailsText.text;
                yield return null;
                Assert.That(remote.Hud.DetailsText.text, Is.EqualTo(frozen));
                var resume = Client.CallProduct(Grid.CreateLevelCommand(LevelControlAction.Resume)).AsTask();
                yield return WaitCoopUi(remote, () => resume.IsCompleted && !remote.Grid.LevelPaused, "Host resumes by the actual local mailbox while remote only reads");
                using (var response = (LevelCommandResponse)resume.GetAwaiter().GetResult())
                {
                    Assert.That(response.Error, Is.Zero);
                }
                var serve = Client.CallProduct(Grid.CreateServeOrder(order.Id, order.CustomerId, order.TableStationId, bowl)).AsTask();
                yield return WaitCoopUi(remote, () => serve.IsCompleted && remote.Grid.Orders[order.Id].State == OrderState.Served,
                    "Idle remote receives actual Host delivery");
                using (var response = (ServeOrderResponse)serve.GetAwaiter().GetResult())
                {
                    Assert.That(response.Error, Is.Zero);
                }
                Assert.That(remote.Grid.LevelScore, Is.EqualTo(order.Score));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("暂无待交付订单"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                var oldTab = CaptureButtonCallback(ClaimUiButton(remote.Hud, "总览：客流"));
                remote.Presentation.CloseGridHud();
                yield return null;
                remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Remote managed overview reopens");
                oldTab();
                Assert.That(remote.Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                float closing = UnityEngine.Time.realtimeSinceStartup;
                while (remote.Grid.LevelPhase != LevelPhase.Completed || remote.Grid.RenovationChoiceVersion == 0)
                {
                    Assert.That(UnityEngine.Time.realtimeSinceStartup - closing, Is.LessThan(65), "Actual cooperative closure");
                    remote.Tick();
                    var clear = remote.Grid.FrontWorks.Values.FirstOrDefault(work => work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available);
                    if (clear.Id != 0 && remote.Grid.Actors[remote.Client.MemberId].FrontWorkId == 0)
                    {
                        Pointer(ClaimUiButton(remote.Hud, "清桌 " + remote.Grid.Stations[clear.StationId].Name));
                        long clearCommand = remote.Client.NextCommandId;
                        yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == clearCommand && remote.Grid.LastStatus == GridResultStatus.Executed, "Actual remote clear allows natural completion");
                    }
                    yield return null;
                }
                command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "本关复盘"));
                string recap = remote.Hud.DetailsText.text;
                Assert.That(recap, Does.Contain("本关结果：10 分").And.Not.Contain("结果尚未完整"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                remote.Presentation.CloseGridHud();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Remote releases recap HUD");
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Host reads same committed recap");
                long hostCommand = Client.NextCommandId;
                Pointer(Button("本关复盘"));
                Assert.That(Hud.DetailsText.text, Is.EqualTo(recap));
                Assert.That(Client.NextCommandId, Is.EqualTo(hostCommand));
            }
        }
    }

    public sealed partial class ProductSessionTests
    {
        [Test, Category("EntryExperience")]
        public void EntryServiceOverviewBoundDrinkIsStillDemandAndExpiredOrdersDisappear()
        {
            DrinkOnlyCustomers();
            StartSolo(liveFront: true, liveDrinks: true);
            var batch = DrinkBegin();
            long cup = DrinkFirstCup(batch);
            var label = PrepStation("贴单台");
            PrepDo(host.Client, GridAction.PlaceCounter, label.Id, cup);
            LevelDo(LevelControlAction.Start);
            FrontWait(() => CurrentLevel.Children.Values.OfType<Order>().Count(order => order.State == OrderState.Waiting) == 2, 400);
            var orders = CurrentLevel.Children.Values.OfType<Order>().Where(order => order.State == OrderState.Waiting).OrderBy(order => order.Id).ToArray();
            PrepDo(host.Client, GridAction.BindDrink, label.Id, cup, cup, order: orders[1].Id);
            var client = (ClientSessionComponent)host.Client;
            var view = client.GetComponent<GridClientComponent>();
            long command = client.NextCommandId;
            Assert.That(GridPresentationSystem.ServiceDemandCount(view, 3), Is.EqualTo(2));
            Assert.That(view.Items[cup].BoundOrderId, Is.EqualTo(orders[1].Id));
            Assert.That(GridPresentationSystem.ServiceReferenceStars(view), Is.Zero);
            Assert.That(client.NextCommandId, Is.EqualTo(command));
            FrontWait(() => orders[0].State != OrderState.Waiting, 1600);
            Assert.That(GridPresentationSystem.ServiceDemandCount(view, 3), Is.EqualTo(CurrentLevel.Children.Values.OfType<Order>()
                .Count(order => order.State == OrderState.Waiting && order.DishId == 3)));
            Assert.That(view.LevelScore, Is.Zero);
            Assert.That(CurrentLevel.Stars, Is.Zero);
        }

        [Test, Category("EntryExperience")]
        public void EntryServiceOverviewAllReferenceThresholdsKeepAuthoritativeScoreAndStarsUntouched()
        {
            StartSolo(liveFront: true);
            var client = (ClientSessionComponent)host.Client;
            var view = client.GetComponent<GridClientComponent>();
            var definition = Tables.Instance.DTLevelConfig.Get(view.LevelConfigId);
            int score = CurrentLevel.Score;
            int stars = CurrentLevel.Stars;
            try
            {
                for (int index = 0; index < definition.StarThresholds.Count; ++index)
                {
                    view.LevelScore = definition.StarThresholds[index] - 1;
                    Assert.That(GridPresentationSystem.ServiceReferenceStars(view), Is.EqualTo(index));
                    view.LevelScore = definition.StarThresholds[index];
                    Assert.That(GridPresentationSystem.ServiceReferenceStars(view), Is.EqualTo(index + 1));
                }
                Assert.That(CurrentLevel.Score, Is.EqualTo(score));
                Assert.That(CurrentLevel.Stars, Is.EqualTo(stars));
            }
            finally
            {
                view.LevelScore = score;
            }
        }
    }
}
#endif
