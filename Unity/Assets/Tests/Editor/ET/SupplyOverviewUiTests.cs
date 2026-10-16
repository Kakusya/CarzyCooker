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
        private IEnumerator SupplyOverviewReject(string caption, string reason)
        {
            long command = Client.NextCommandId + 1;
            Pointer(Button(caption, true));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Rejected, "Original supply rejection: " + caption);
            Assert.That(Grid.LastReason, Is.EqualTo(reason));
        }

        [UnityTest, Category("EntryExperience"), Category("EntryAdvice")]
        public IEnumerator EntrySupplyOverviewSoloSeparatesQuotaTransitPackageAndRejectsFullReceiverWithoutDuplication()
        {
            yield return StartLauncher();
            long command = Client.NextCommandId;
            Pointer(Button("补货总览"));
            Pointer(Button("来源 · 番茄补货"));
            Assert.That(Hud.DetailsText.text, Does.Contain("外部配额：5/5").And.Contain("现存关联包装 0"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Command("采购 番茄", true);
            var shipment = Grid.SupplyShipments.Values.Single();
            Pointer(Button("货物 1 ·", true));
            Assert.That(Hud.DetailsText.text, Does.Contain("外部配额：4/5").And.Contain("运输中 1"));
            var oldPurchase = CaptureButtonCallback(Button("采购 番茄", true));
            yield return Command("暂停");
            int progress = Grid.SupplyShipments[shipment.Id].ProgressTicks;
            command = Client.NextCommandId;
            oldPurchase();
            yield return null; yield return null;
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.SupplyShipments[shipment.Id].ProgressTicks, Is.EqualTo(progress));
            Assert.That(Hud.DetailsText.text, Does.Contain("运输冻结"));
            yield return Command("恢复");
            yield return Wait(() => Grid.SupplyShipments[shipment.Id].Stage == ShipmentStage.Ready, "Original transport reaches ready state");
            Pointer(Button("全部动作"));
            yield return Command("拿 碗台 ·", true);
            string receiver = Grid.Stations[shipment.ReceivingStationId].Name;
            yield return Command("放 " + receiver + " ·", true);
            Assert.That(Hud.DetailsText.text, Does.Contain("收货台已满"));
            var oldReceive = CaptureButtonCallback(Button("接收 番茄", true));
            yield return SupplyOverviewReject("接收 番茄", "ReceivingFull");
            Assert.That(Grid.SupplyShipments[shipment.Id].Stage, Is.EqualTo(ShipmentStage.Ready));
            Assert.That(Grid.SupplyShipments[shipment.Id].PackageItemId, Is.Zero);
            command = Client.NextCommandId;
            long[] rejectedItems = Grid.Items.Keys.OrderBy(value => value).ToArray();
            Pointer(Button("查看处理说明"));
            Assert.That(Hud.DetailsText.text, Does.Contain("供应、货物或收货条件").And.Contain("自行选择原接收动作"));
            var retiredAdvice = CaptureButtonCallback(Button("查看供应与收货"));
            Pointer(Button("查看供应与收货"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Supply));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.Items.Keys.OrderBy(value => value), Is.EqualTo(rejectedItems));
            Assert.That(Grid.SupplySources[shipment.SourceId].RemainingPackages, Is.EqualTo(4));
            Assert.That(Grid.SupplyShipments[shipment.Id].Stage, Is.EqualTo(ShipmentStage.Ready));
            Pointer(Button("来源 · 番茄补货"));
            Pointer(Button("全部动作"));
            yield return Command("拿 " + receiver + " ·", true);
            yield return Command("放 碗台 ·", true);
            yield return Command("接收 番茄", true);
            long package = Grid.SupplyShipments[shipment.Id].PackageItemId;
            long[] portions = Grid.Items[package].Contents.ToArray();
            Assert.That(Hud.DetailsText.text, Does.Contain("已接收记录 1 条").And.Contain("现存关联包装 1 个 · 包内材料 2 份"));
            command = Client.NextCommandId;
            oldReceive();
            retiredAdvice();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Supply));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.SupplyShipments[shipment.Id].PackageItemId, Is.EqualTo(package));
            yield return Command("拆一份 番茄", true);
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(portions[0]));
            Assert.That(Grid.Items[package].Contents, Is.EqualTo(new[] { portions[1] }));
            Assert.That(Hud.DetailsText.text, Does.Contain("包内材料 1 份"));
            yield return Command("放 切配台 ·", true);
            command = Client.NextCommandId;
            Pointer(Button("实物包装 ·", true));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Items));
            Assert.That(Presentation.ItemLocatorSelectedId, Is.EqualTo(package));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("补货动作"));
            Pointer(Button("补货总览")); Pointer(Button("来源 · 番茄补货"));
            var retiredRow = CaptureButtonCallback(Button("货物 1 ·", true));
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Supply overview managed reopen");
            retiredRow();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(Presentation.SupplyOverviewSourceId, Is.Zero);
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("全部动作"));
            yield return Command("切配 切配台 ·", true);
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.TomatoChunks), "Original production works after supply overview");
        }

        [UnityTest, Category("EntryExperience"), Timeout(150000)]
        public IEnumerator EntrySupplyOverviewExhaustedSourceRealDrainingAndNextLevelKeepFactsButClearSelection()
        {
            yield return StartLauncher();
            Pointer(Button("补货总览")); Pointer(Button("来源 · 鸡蛋补货"));
            int source = Tables.Instance.DTSupplyConfig.DataList.Single(row => row.MaterialDefinitionId == GridKitchenRules.Egg).Id;
            for (int index = 0; index < 5; ++index)
            {
                yield return Command("采购 鸡蛋", true);
            }
            Assert.That(Grid.SupplySources[source].RemainingPackages, Is.Zero);
            Assert.That(Grid.SupplyShipments.Count, Is.EqualTo(5));
            Assert.That(Hud.DetailsText.text, Does.Contain("配额已用尽").And.Contain("现存关联包装 0"));
            yield return SupplyOverviewReject("采购 鸡蛋", "SupplyExhausted");
            long[] shipments = Grid.SupplyShipments.Keys.OrderBy(id => id).ToArray();
            Match match = ((HostSessionComponent)Runtime.Authority).Match;
            RestaurantRuntime restaurant = match.Restaurant;
            Level level = restaurant.CurrentLevel;
            // 本用例只缩短自身运行轮次，让真实顾客仍在订单期间进入Draining；不改表或伪造阶段。
            level.OperatingTicks = 100;
            match.ProductKitchen().Dirty = true;
            Pointer(Button("全部动作"));
            yield return UnlockUiStartOperating();
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Draining, "Actual short fixture round enters draining with customers active", 20);
            Assert.That(Hud.DetailsText.text, Does.Contain("不能新采购"));
            Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text.StartsWith("采购 ")), Is.False);
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Actual customer closure completes", 60);
            Assert.That(Hud.DetailsText.text, Does.Contain("已结算"));
            long command = Client.NextCommandId;
            var retiredRow = CaptureButtonCallback(Button("货物 1 ·", true));
            Pointer(Button("来源 · 鸡蛋补货"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            long previous = Grid.LevelId;
            yield return Command("下一关");
            Assert.That(Grid.LevelId, Is.Not.EqualTo(previous));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(Presentation.SupplyOverviewSourceId, Is.Zero);
            Assert.That(Grid.SupplyShipments.Keys.OrderBy(id => id), Is.EqualTo(shipments));
            Assert.That(Grid.SupplySources[source].RemainingPackages, Is.Zero);
            command = Client.NextCommandId;
            retiredRow();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
        }

        [UnityTest, Category("EntryExperience")]
        public IEnumerator EntrySupplyOverviewActualKcpPurchaseReceiveSplitAndPackageLookupUseOneOriginalRequest()
        {
            yield return StartCoopHostLauncher();
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases UI with owner retained");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "KCP supply viewer ready");
                Pointer(ClaimUiButton(remote.Hud, "补货总览")); Pointer(ClaimUiButton(remote.Hud, "来源 · 番茄补货"));
                long command = remote.Client.NextCommandId;
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("外部配额：5/5"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                yield return AssemblyUiCommand(remote, "采购 番茄", true);
                var shipment = remote.Grid.SupplyShipments.Values.Single();
                yield return WaitCoopUi(remote, () => remote.Grid.SupplyShipments[shipment.Id].Stage == ShipmentStage.Ready, "Real remote purchase reaches ready");
                Pointer(ClaimUiButton(remote.Hud, "货物 1 ·", true));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("待接收 1").And.Contain("现存关联包装 0"));
                var retiredRow = CaptureButtonCallback(ClaimUiButton(remote.Hud, "货物 1 ·", true));
                remote.Presentation.CloseGridHud(); yield return null; remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Remote supply HUD pool reopen");
                retiredRow();
                Assert.That(remote.Presentation.SupplyOverviewShipmentId, Is.Zero);
                Pointer(ClaimUiButton(remote.Hud, "补货总览")); Pointer(ClaimUiButton(remote.Hud, "来源 · 番茄补货"));
                command = remote.Client.NextCommandId;
                yield return AssemblyUiCommand(remote, "接收 番茄", true);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command + 1));
                long package = remote.Grid.SupplyShipments[shipment.Id].PackageItemId;
                long first = remote.Grid.Items[package].Contents[0];
                yield return AssemblyUiCommand(remote, "拆一份 番茄", true);
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.EqualTo(first));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("现存关联包装 1 个 · 包内材料 1 份"));
                Assert.That(remote.Grid.SupplySources[shipment.SourceId].RemainingPackages, Is.EqualTo(4));
                command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "实物包装 ·", true));
                Assert.That(remote.Presentation.ItemLocatorSelectedId, Is.EqualTo(package));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
            }
        }
    }
}
#endif
