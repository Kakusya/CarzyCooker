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
        [UnityTest, Category("EntryExperience"), Category("RenovationPreviewLocal"), Timeout(180000)]
        public IEnumerator EntryRenovationPreviewSoloInstallCancelUpgradeConfirmAndFrozenOldWorkUseOriginalRequests()
        {
            yield return StartLauncher();
            yield return Command("拿 备料台 ·", true); yield return Command("放 切配台 ·", true); yield return Command("切配 切配台 ·", true);
            yield return Wait(() => Grid.Processes.Values.Any(work => work.State == GridWorkState.Working && work.ProgressTicks > 0), "Original old chopping actually starts before buff or upgrade");
            var old = Grid.Processes.Values.Single(work => work.State == GridWorkState.Working);
            long station = old.StationId;
            Assert.That(old.RequiredTicks, Is.EqualTo(60));
            yield return Command("停止 切配台 ·", true);
            Assert.That(Grid.Processes[old.Id].State, Is.EqualTo(GridWorkState.Paused));
            Match match = ((HostSessionComponent)Runtime.Authority).Match;
            ((Level)((RestaurantRuntime)match.Restaurant).CurrentLevel).OperatingTicks = 100; match.ProductKitchen().Dirty = true;
            yield return UnlockUiStartOperating();
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Real paused kitchen survives natural front closure", 70);
            int stations = Grid.Stations.Count;
            long command = Client.NextCommandId, version = Grid.RenovationChoiceVersion;
            int[] candidates = Grid.RenovationCandidates.ToArray();
            Pointer(Button("装修 新增台面"));
            Assert.That(Hud.DetailsText.text, Does.Contain("新增工位").And.Contain("安装锚点").And.Contain("完整验证"));
            Assert.That(Grid.SelectedRenovationId, Is.Zero); Assert.That(Grid.Stations.Count, Is.EqualTo(stations));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("取消装修预览"));
            Assert.That(Grid.RenovationCandidates, Is.EqualTo(candidates)); Assert.That(Grid.RenovationChoiceVersion, Is.EqualTo(version));
            Pointer(Button("装修 升级切配台", true));
            Assert.That(Hud.DetailsText.text, Does.Contain("基础耗时").And.Contain("3.00 → 2.25 秒").And.Contain("原已开始作业保持冻结耗时"));
            var retired = CaptureButtonCallback(Button("确认此装修"));
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Preview HUD pool reopen leaves no selected reward");
            retired();
            Assert.That(Client.NextCommandId, Is.EqualTo(command)); Assert.That(Grid.SelectedRenovationId, Is.Zero);
            Assert.That(Presentation.RenovationPreviewId, Is.Zero);
            Pointer(Button("装修 升级切配台", true));
            var confirm = Button("确认此装修"); var oldConfirm = CaptureButtonCallback(confirm);
            Pointer(confirm); oldConfirm();
            Assert.That(Client.NextCommandId, Is.EqualTo(command + 1), "Only the explicit current confirmation submits one physical request");
            yield return Wait(() => Grid.SelectedRenovationId == 3 && Grid.SelectedUpgradeStationId == station, "Original choice commits before continuation");
            Assert.That(Grid.Stations[station].UpgradeLevel, Is.Zero);
            Assert.That(Grid.Processes[old.Id].RequiredTicks, Is.EqualTo(60));
            yield return Command("下一关");
            Assert.That(Grid.Stations[station].UpgradeLevel, Is.EqualTo(1));
            Assert.That(Grid.Stations.Count, Is.EqualTo(stations));
            Assert.That(Grid.Processes[old.Id].RequiredTicks, Is.EqualTo(60));
            command = Client.NextCommandId; oldConfirm();
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Old target level and previous choice context cannot confirm again");
            Pointer(Button("全部动作"));
            yield return Command("继续切配 切配台 ·", true);
            Assert.That(Grid.Processes[old.Id].RequiredTicks, Is.EqualTo(60));
            yield return Wait(() => Grid.Processes[old.Id].State == GridWorkState.Completed, "Old work finishes using its original frozen duration");
            yield return Command("拿 切配台 ·", true); yield return Command("放 备料台 ·", true);
            yield return Command("采购 番茄", true);
            yield return Wait(() => Grid.SupplyShipments.Values.Any(shipment => shipment.Stage == ShipmentStage.Ready), "Original finite shipment arrives");
            yield return Command("接收 番茄", true); yield return Command("拆一份 番茄", true);
            long fresh = Grid.Actors[Client.MemberId].HandItemId;
            yield return Command("放 切配台 ·", true); yield return Command("切配 切配台 ·", true);
            var next = Grid.Processes.Values.Single(work => work.InputItemId == fresh);
            Assert.That(next.RequiredTicks, Is.EqualTo(45), "Actual new work agrees with the 2.25 second preview without a new-level buff");
            Assert.That(Grid.SelectedBuffId, Is.Zero);
        }

        [UnityTest, Category("EntryExperience"), Category("RenovationPreviewLocal"), Timeout(120000)]
        public IEnumerator EntryRenovationPreviewUpgradeAndExpansionSameCandidateSetCancelThenConfirmUpgrade()
        {
            yield return StartProgressHostRoom(); yield return StartPreviewAndConfirm(EntryButton("开启经营成长餐厅"));
            yield return Wait(() => Presentation?.Ready == true, "Ordinary Host growth menu before shared candidate comparison");
            Match match = ((HostSessionComponent)Runtime.Authority).Match;
            RestaurantRuntime restaurant = match.Restaurant;
            ((Level)restaurant.CurrentLevel).OperatingTicks = 100; ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
            yield return ExpansionUiNaturalSettlement();
            int[] candidates = Grid.RenovationCandidates.ToArray();
            long version = Grid.RenovationChoiceVersion, command = Client.NextCommandId;
            Pointer(Button("装修 高效灶台", true));
            long station = Presentation.RenovationPreviewStationId;
            Assert.That(Hud.DetailsText.text, Does.Contain("升级原工位").And.Contain("基础耗时").And.Contain("不含本关加成"));
            Pointer(Button("取消装修预览"));
            Pointer(Button("装修 扩建单客桌"));
            Assert.That(Hud.DetailsText.text, Does.Contain("7×18 → 9×18"));
            var staleExpansion = CaptureButtonCallback(Button("确认此装修"));
            Pointer(Button("取消装修预览"));
            Assert.That(Grid.RenovationCandidates, Is.EqualTo(candidates)); Assert.That(Grid.RenovationChoiceVersion, Is.EqualTo(version));
            Assert.That(Client.NextCommandId, Is.EqualTo(command)); Assert.That(Grid.SelectedRenovationId, Is.Zero);
            yield return Command("装修 高效灶台", true);
            Assert.That(Grid.SelectedRenovationId, Is.EqualTo(9)); Assert.That(Grid.SelectedUpgradeStationId, Is.EqualTo(station));
            command = Client.NextCommandId; staleExpansion();
            Assert.That(Client.NextCommandId, Is.EqualTo(command)); Assert.That(Grid.Width, Is.EqualTo(7));
            yield return UnlockUiNext(true);
            Assert.That(Grid.Stations[station].UpgradeLevel, Is.EqualTo(1));
            Assert.That(Grid.Width, Is.EqualTo(7)); Assert.That(Grid.Tables.Count, Is.EqualTo(2), "Cancelled expansion never installs incidentally");
        }

        [UnityTest, Category("EntryExperience"), Timeout(180000)]
        [Category("SettlementNetwork")]
        public IEnumerator EntryRenovationPreviewActualKcpReadOnlyUnlockAndExpansionStaySeparateUntilHostConfirmation()
        {
            yield return StartProgressHostRoom(); yield return StartPreviewAndConfirm(EntryButton("开启经营成长餐厅"));
            yield return Wait(() => Presentation?.Ready == true, "Ordinary growth room starts");
            Match match = ((HostSessionComponent)Runtime.Authority).Match;
            RestaurantRuntime restaurant = match.Restaurant;
            ((Level)restaurant.CurrentLevel).OperatingTicks = 100; ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
            yield return ExpansionUiNaturalSettlement();
            long command = Client.NextCommandId;
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            Pointer(Button("装修 扩建单客桌"));
            Assert.That(Hud.DetailsText.text, Does.Contain("7×18 → 9×18").And.Contain("新增桌位").And.Contain("整体安装"));
            Pointer(Button("取消装修预览"));
            Pointer(Button("装修 解锁现榨菜单"));
            Assert.That(Hud.DetailsText.text, Does.Contain("解锁菜品").And.Contain("解锁设备").And.Contain("不附赠食材或设备"));
            Pointer(Button("取消装修预览"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command)); Assert.That(Grid.SelectedRenovationId, Is.Zero);
            Assert.That(Grid.Width, Is.EqualTo(7)); Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Retain Host owner while remote reads shared candidates");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Actual KCP reward preview reader");
                long remoteCommand = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "查看装修 扩建单客桌"));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("7×18 → 9×18").And.Contain("实际选择由Host确认"));
                Assert.That(remote.Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "确认此装修"), Is.False);
                remote.Presentation.SendRenovationChoice(8, remote.Grid.RenovationChoiceVersion, remote.Grid.LevelId, remote.Grid.LevelGeneration);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(remoteCommand));
                Assert.That(remote.Grid.SelectedRenovationId, Is.Zero);
                var staleCancel = CaptureButtonCallback(ClaimUiButton(remote.Hud, "取消装修预览"));
                var choice = Client.CallProduct(Grid.CreateRenovationChoice(7, Grid.RenovationChoiceVersion)).AsTask();
                yield return WaitCoopUi(remote, () => choice.IsCompleted && remote.Grid.SelectedRenovationId == 7 && remote.Presentation.OverviewPage != GridOverviewPage.Renovation,
                    "Actual Host local request commits another candidate and invalidates the remote preview");
                using (var receipt = (ChooseRenovationResponse)choice.GetAwaiter().GetResult())
                {
                    Assert.That(receipt.Error, Is.Zero);
                }
                staleCancel();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(remoteCommand));
                Assert.That(remote.Grid.UnlockedDishIds, Does.Contain(3)); Assert.That(remote.Grid.Width, Is.EqualTo(7));
                Assert.That(remote.Hud.SettlementText.text, Does.Contain("已选装修：解锁现榨菜单").And.Contain("定义已解锁，不补发食材或设备"));
                remote.KeepMapHeadlessWhileHostOwnsDisplay();
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Remote releases original UI before Host reopens");
                Presentation.OpenGridHud(); yield return WaitCoopUi(remote, () => Presentation.Ready, "Original Host owner reopens");
                yield return ContentUiPump(UnlockUiNext(true), remote);
                Assert.That(Grid.AllowedDishIds, Does.Contain(3));
                ((Level)restaurant.CurrentLevel).OperatingTicks = 100; ((GridKitchenComponent)restaurant.Kitchen).Dirty = true;
                yield return ContentUiPump(ExpansionUiNaturalSettlement(), remote);
                yield return ContentUiPump(Command("装修 扩建单客桌"), remote);
                Assert.That(Grid.Width, Is.EqualTo(7), "Confirmed expansion still waits for actual next-level installation");
                yield return ContentUiPump(UnlockUiNext(true), remote);
                yield return WaitCoopUi(remote, () => remote.Grid.Width == 9 && remote.Grid.Tables.Count == 3, "Actual whole expansion reaches KCP projection");
                Assert.That(Grid.Width, Is.EqualTo(9)); Assert.That(Grid.Tables.Count, Is.EqualTo(3));
                Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(remoteCommand));
            }
        }
    }
}
#endif
