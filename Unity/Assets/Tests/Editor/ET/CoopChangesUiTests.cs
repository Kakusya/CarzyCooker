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
        [UnityTest]
        [NUnit.Framework.Category("PerFrameIntegration")]
        [Category("RestaurantArtRegression")]
        public IEnumerator CoopMemberResumeUiRealDisconnectManualButtonReopenAndRetiredInput()
        {
            yield return StartCoopHostLauncher(); ((Scene)Client.IScene).RemoveComponent<GridPresentationComponent>(); yield return null;
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Remote managed HUD before loss");
                Button RemoteButton(string caption) => remote.Hud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith(caption));
                var oldTake = CaptureButtonCallback(RemoteButton("拿 碗台 ·"));
                long command = remote.Client.NextCommandId + 1; Pointer(RemoteButton("拿 碗台 ·"));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == command && remote.Grid.LastStatus == GridResultStatus.Executed, "Real remote takes bowl");
                int member = remote.Client.MemberId; long generation = remote.Client.BindingGeneration;
                long bowl = remote.Grid.Actors[member].HandItemId;
                ProductRuntimeComponent runtime = remote.Client.Runtime;
                ((Session)remote.Client.Session).Dispose();
                yield return WaitCoopUi(remote, () => runtime.Stage == ProductStage.Disconnected, "Real Session close retains running intent");
                Assert.That(remote.Presentation.Ready, Is.False); Assert.That(remote.Grid.Confirmed, Is.False);
                oldTake(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Button stableResume=RemoteButton("手动恢复");
                for(int frame=0;frame<12;frame++)
                {
                    remote.Tick();yield return null;
                    Assert.That(RemoteButton("手动恢复"),Is.SameAs(stableResume),"Stable lost frames keep managed button identity");
                    Assert.That(remote.Presentation.Ready,Is.False);
                }
                ProductResumeIntent intent=runtime.GetComponent<ProductResumeIntent>();
                long originalAttempt=intent.AttemptId;
                intent.Feedback="恢复页同状态反馈更新";remote.Presentation.RefreshGridHud();
                Assert.That(remote.Hud.HintText.text,Is.EqualTo(intent.Feedback));
                Assert.That(intent.AttemptId,Is.EqualTo(originalAttempt));
                var retiredAttempt=CaptureButtonCallback(RemoteButton("手动恢复"));
                ++intent.AttemptId;remote.Presentation.RefreshGridHud();
                Assert.That(RemoteButton("手动恢复"),Is.Not.SameAs(stableResume));
                retiredAttempt();Assert.That(runtime.Stage,Is.EqualTo(ProductStage.Disconnected),"Previous attempt callback is no longer eligible");
                var oldResume = CaptureButtonCallback(RemoteButton("手动恢复")); int mono = remote.Hud.GetInstanceID();
                remote.Presentation.CloseGridHud(); oldResume(); Assert.That(runtime.Stage, Is.EqualTo(ProductStage.Disconnected));
                yield return null; remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => ((UIGridKitchen)remote.Presentation.Hud)?.Available == true, "Managed disconnected HUD reopens");
                oldResume(); Assert.That(runtime.Stage, Is.EqualTo(ProductStage.Disconnected));
                Pointer(RemoteButton("手动恢复"));
                Assert.That(remote.Grid.Confirmed, Is.False);
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.Confirmed, "Real Resume has confirmation projection and view");
                Assert.That(remote.Hud.GetInstanceID(), Is.EqualTo(mono)); Assert.That(remote.Client.MemberId, Is.EqualTo(member));
                Assert.That(remote.Client.BindingGeneration, Is.GreaterThan(generation)); Assert.That(remote.Grid.Actors[member].HandItemId, Is.Zero);
                Assert.That(remote.Grid.Items[bowl].Location, Is.EqualTo(GridItemLocation.Ground));
                long fresh = remote.Client.NextCommandId; oldTake(); oldResume(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(fresh));
                ((Session)remote.Client.Session).Dispose();
                yield return WaitCoopUi(remote, () => runtime.Stage == ProductStage.Disconnected, "Second manual loss");
                Pointer(RemoteButton("结束本地恢复"));
                Assert.That(remote.Hud.HintText.text, Does.Contain("未向 Host 确认 Leave"));
                Assert.That(Client.Stage, Is.EqualTo(ProductClientStage.Joined));
            }
        }

        [UnityTest]
        public IEnumerator CoopRenovationUiOrdinaryHostChoiceClientReadOnlyAndManagedContinue()
        {
            yield return StartCoopHostLauncher();
            string buff = Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(buff); yield return Command("开始营业");
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Ordinary Host settlement prepares renovations", 90);
            Assert.That(Grid.RenovationCandidates, Is.EqualTo(new[] { 5, 6 }));
            string choice = Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("装修 ")).GetComponentInChildren<Text>().text;
            var choose = CaptureButtonCallback(Button(choice));
            yield return Command(choice);
            int selected = Grid.SelectedRenovationId; long level = Grid.LevelId;
            Assert.That(selected, Is.GreaterThan(0));
            // 合法选择之后才制造一次晚到落物，检查真实 Continue 的共同失败投影。
            HostSessionComponent authority = Runtime.Authority; Match match = authority.Match;
            RestaurantRuntime restaurant = match.Restaurant; GridKitchenComponent kitchen = restaurant.Kitchen;
            GridStation source = kitchen.Stations.Values.Select(value => (GridStation)value).Single(station => station.Name == "橙子台");
            GridItem blocked = kitchen.Item(source.ItemId); GridCell previousCell = blocked.Cell;
            source.ItemId = 0; blocked.HolderId = 0; blocked.Location = GridItemLocation.Ground;
            var reward = Tables.Instance.DTRenovationConfig.Get(selected); blocked.Cell = new GridCell(reward.InstallX, reward.InstallY);
            long failed = Client.NextCommandId + 1; Pointer(Button("下一关"));
            yield return Wait(() => Grid.LastCommandId == failed && Grid.LastStatus == GridResultStatus.Rejected, "Late drop rejects whole Continue");
            Assert.That(Grid.LevelId, Is.EqualTo(level)); Assert.That(Hud.SettlementText.text, Does.Contain("安装位置仍有角色或落物"));
            ((Scene)Client.IScene).RemoveComponent<GridPresentationComponent>(); yield return null;
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Late Client sees Host choice");
                Assert.That(remote.Grid.SelectedRenovationId, Is.EqualTo(selected)); Assert.That(remote.Hud.SettlementText.text, Does.Contain("已选装修"));
                Assert.That(remote.Hud.SettlementText.text, Does.Contain("安装位置仍有角色或落物"));
                Assert.That(remote.Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text.StartsWith("装修 ") || button.GetComponentInChildren<Text>().text == "下一关"), Is.False);
                long last = Client.NextCommandId; choose(); Assert.That(Client.NextCommandId, Is.EqualTo(last));
                remote.Presentation.CloseGridHud(); ((Scene)Client.IScene).AddComponent<GridPresentationComponent>();
                yield return WaitCoopUi(remote, () => Presentation?.Ready == true, "Host controls reopen through owner");
                source.ItemId = blocked.Id; blocked.HolderId = source.Id; blocked.Location = GridItemLocation.Counter; blocked.Cell = previousCell;
                yield return Command("下一关");
                yield return WaitCoopUi(remote, () => remote.Grid.LevelId != level && remote.Grid.RenovationApplied.ContainsKey(selected), "Remote sees installed station in next level");
                long station = Grid.RenovationApplied[selected].Single(); Assert.That(Grid.Stations[station].ItemId, Is.Zero);
                Assert.That(remote.Grid.Stations.ContainsKey(station), Is.True);
                yield return RenovationUiMoveStation(Grid.Stations[station].Name, new GridCell(2, 15));
                yield return WaitCoopUi(remote, () => remote.Grid.Stations[station].Cell == new GridCell(2, 15), "Client sees ordinary map movement of new station");
                choose(); Assert.That(Grid.RenovationApplied[selected].Count, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator CoopDrinkUiOrdinaryPurchaseSplitPreparationBindingDeliveryAndClear()
        {
            yield return StartCoopHostLauncher();
            Assert.That(Grid.LevelConfigId, Is.EqualTo(3));
            string buff = Hud.GetComponentsInChildren<Button>(false).First(button => button.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(buff);
            ((Scene)Client.IScene).RemoveComponent<GridPresentationComponent>(); yield return null;
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Ordinary drink Client HUD");
                Button RemoteButton(string caption)
                {
                    var buttons = remote.Hud.GetComponentsInChildren<Button>(false);
                    var matches = buttons.Where(button => button.GetComponentInChildren<Text>().text.StartsWith(caption)).ToArray();
                    Assert.That(matches.Length, Is.EqualTo(1), "Missing remote action: " + caption + "; available=" + string.Join(" | ", buttons.Select(button => button.GetComponentInChildren<Text>().text)));
                    return matches[0];
                }
                IEnumerator Do(string caption)
                {
                    long command = remote.Client.NextCommandId + 1; Pointer(RemoteButton(caption));
                    yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == command && remote.Grid.LastStatus == GridResultStatus.Executed, caption);
                }
                yield return Do("采购 橙子");
                yield return WaitCoopUi(remote, () => remote.Grid.SupplyShipments.Values.Any(shipment => shipment.SourceId == 4 && shipment.Stage == ShipmentStage.Ready), "Purchased oranges arrive");
                yield return Do("接收 橙子"); yield return Do("拆一份 橙子");
                long orange = remote.Grid.Actors[remote.Client.MemberId].HandItemId;
                yield return Do("放 切配台 ·");
                var chop = Grid.Stations.Values.Single(station => station.Name == "切配台");
                var cutting = Client.CallProduct(Grid.CreateGridCommand(GridAction.StartStationWork, target: chop.Id, expected: orange, expectedTarget: orange));
                yield return WaitCoopUi(remote, () => cutting.Status != Cysharp.Threading.Tasks.UniTaskStatus.Pending, "Host local cutting receipt");
                using (var response = (MessageObject)cutting.GetAwaiter().GetResult()) Assert.That(((IResponse)response).Error, Is.Zero);
                var receiving = remote.Grid.Stations.Values.Single(station => station.ItemId != 0 &&
                    Tables.Instance.DTSupplyConfig.DataList.Any(source => source.PackageDefinitionId == remote.Grid.Items[station.ItemId].DefinitionId));
                yield return Do("拿 " + receiving.Name + " ·"); yield return Do("放 贴单台 ·");
                yield return WaitCoopUi(remote, () => remote.Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.OrangeChunks), "Host chops purchased remote orange");
                yield return Do("拿 切配台 ·"); yield return Do("添加 果汁壶台 ·");
                yield return Do("拿 果汁壶台 ·"); yield return Do("放 榨汁机 ·"); yield return Do("榨汁 榨汁机 ·");
                yield return Do("拿 贴单台 ·"); yield return Do("投垃圾 → 垃圾桶");
                yield return Do("投垃圾 → 垃圾桶");
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.Zero, "Discard solids then the empty package before receiving cups");
                yield return Do("采购 空杯");
                yield return WaitCoopUi(remote, () => remote.Grid.SupplyShipments.Values.Any(shipment => shipment.SourceId == 5 && shipment.Stage == ShipmentStage.Ready), "Purchased cups arrive");
                yield return Do("接收 空杯"); yield return Do("拆一份 一次性杯");
                long cup = remote.Grid.Actors[remote.Client.MemberId].HandItemId;
                yield return WaitCoopUi(remote, () => remote.Grid.AutoProcesses.Values.Any(batch => batch.State == GridAutoState.Ready), "Real remote juicer completes");
                yield return Do("盛一份 榨汁机 ·"); yield return Do("放 贴单台 ·");
                var starting = Client.CallProduct(Grid.CreateLevelCommand(LevelControlAction.Start));
                yield return WaitCoopUi(remote, () => starting.Status != Cysharp.Threading.Tasks.UniTaskStatus.Pending, "Host local opening receipt");
                using (var response = (MessageObject)starting.GetAwaiter().GetResult()) Assert.That(((IResponse)response).Error, Is.Zero);
                yield return WaitCoopUi(remote, () => remote.Grid.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == 3), "Ordinary drink order", 90);
                var order = remote.Grid.Orders.Values.First(order => order.State == OrderState.Waiting && order.DishId == 3);
                string table = remote.Grid.Stations[order.TableStationId].Name;
                yield return Do("贴单 → " + table); Assert.That(remote.Grid.Items[cup].BoundOrderId, Is.EqualTo(order.Id));
                yield return Do("拿 贴单台 ·"); yield return Do("前往 " + table); yield return Do("交付 鲜榨橙汁 → " + table);
                Assert.That(remote.Grid.Items.ContainsKey(cup), Is.False); Assert.That(remote.Grid.ServedOrders, Is.EqualTo(1));
                yield return WaitCoopUi(remote, () => remote.Grid.FrontWorks.Values.Any(work => work.CustomerId == order.CustomerId && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available), "Drink customer leaves empty table", 25);
                yield return Do("清桌 " + table);
                yield return WaitCoopUi(remote, () => remote.Grid.Tables[order.TableStationId].State == FrontTableState.Free, "Remote completes drink table cleanup", 25);
                Assert.That(remote.Grid.FrontWorks.Values.Any(work => work.Kind == FrontWorkKind.Wash && work.ItemId == cup), Is.False);
                Assert.That(remote.Grid.SupplySources[4].RemainingPackages, Is.EqualTo(4)); Assert.That(remote.Grid.SupplySources[5].RemainingPackages, Is.EqualTo(4));
            }
        }
    }
}
#endif
