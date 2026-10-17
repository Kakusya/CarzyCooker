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
        [UnityTest, Category("RestaurantNoticesUi"), Timeout(120000)]
        public IEnumerator EntryNoticesActualSoloTargetConsumptionSettingsAndHudPoolRemainReadOnly()
        {
            yield return StartLauncher();
            CreateSaveUiRepository(Runtime, out _, out _);
            yield return Command("拿 面包台 ·", true);
            yield return Command("放 烤箱 ·", true);
            yield return Command("烘烤 烤箱 ·", true);
            var notices = ((Scene)Client.IScene).GetComponent<RestaurantNoticesComponent>();
            yield return Wait(() => notices.Entries.Any(value => value.Kind == RestaurantNoticeKind.Processing), "Actual bake finished reminder");
            var baked = notices.Entries.Last(value => value.Kind == RestaurantNoticeKind.Processing);
            long command = Client.NextCommandId;
            Pointer(Button("提醒 #", true));
            Assert.That(Hud.DetailsText.text, Does.Contain("仅列最近8条").And.Contain("已完工"));
            var oldRow = CaptureButtonCallback(Button("查看提醒 #" + baked.Serial + " · 完工"));
            Pointer(Button("查看提醒 #" + baked.Serial + " · 完工"));
            Assert.That(Presentation.ItemLocatorSelectedId, Is.EqualTo(baked.ItemId));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("全部动作"));
            yield return Command("拿 烤箱 ·", true);
            yield return Command("投垃圾 → 垃圾桶");
            Assert.That(Grid.Items.ContainsKey(baked.ItemId), Is.False);
            Pointer(Button("提醒 #", true));
            Pointer(Button("查看提醒 #" + baked.Serial + " · 完工"));
            Assert.That(Hud.DetailsText.text, Does.Contain("原目标已变化"));
            int count = notices.Entries.Count;
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Reminder HUD pool reuse");
            command = Client.NextCommandId;
            oldRow();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(notices.Entries.Count, Is.EqualTo(count));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("全部动作"));
            yield return Command("采购 番茄", true);
            yield return Wait(() => notices.Entries.Any(value => value.Kind == RestaurantNoticeKind.Arrival), "Actual cargo arrival reminder");
            var arrival = notices.Entries.Last(value => value.Kind == RestaurantNoticeKind.Arrival);
            command = Client.NextCommandId;
            Pointer(Button("提醒 #", true));
            Pointer(Button("查看提醒 #" + arrival.Serial + " · 到货"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Supply));
            Assert.That(Presentation.SupplyOverviewShipmentId, Is.EqualTo(arrival.TargetId));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Command("接收 番茄", true);
            Assert.That(Grid.SupplyShipments[arrival.TargetId].Stage, Is.EqualTo(ShipmentStage.Received));
            Pointer(Button("本机设置"));
            yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "Local reminder options read");
            Pointer(Button("到货提醒：开"));
            Pointer(Button("保存本机设置"));
            yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "Actual settings commit updates observer");
            Assert.That(notices.Entries.Any(value => value.Kind == RestaurantNoticeKind.Arrival), Is.False);
            Pointer(Button("取消本机设置"));
        }

        [UnityTest, Category("RestaurantNoticesUi"), Timeout(120000)]
        public IEnumerator EntryNoticesActualKcpFirstBaselineLiveArrivalDisconnectAndResumeDoNotReplay()
        {
            yield return StartCoopHostLauncher();
            CreateSaveUiRepository(Runtime, out _, out _);
            yield return Command("采购 番茄", true);
            yield return Wait(() => Grid.SupplyShipments.Values.Any(value => value.Stage == ShipmentStage.Ready), "Host cargo already ready before joining");
            Presentation.CloseGridHud();
            ((GridKitchenBoard)Presentation.Board).Visible = false;
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases original HUD");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Actual KCP client first projection");
                var notices = ((Scene)remote.Client.IScene).GetComponent<RestaurantNoticesComponent>();
                Assert.That(notices.BaselineReady, Is.True);
                Assert.That(notices.Entries, Is.Empty, "First snapshot cannot turn existing ready cargo into a new arrival");
                Pointer(ClaimUiButton(remote.Hud, "采购 鸡蛋", true));
                yield return WaitCoopUi(remote, () => notices.Entries.Any(value => value.Kind == RestaurantNoticeKind.Arrival), "Actual KCP command and later independent arrival outcome");
                var arrival = notices.Entries.Last(value => value.Kind == RestaurantNoticeKind.Arrival);
                long command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "提醒 #", true));
                var oldRow = CaptureButtonCallback(ClaimUiButton(remote.Hud, "查看提醒 #" + arrival.Serial + " · 到货"));
                Pointer(ClaimUiButton(remote.Hud, "查看提醒 #" + arrival.Serial + " · 到货"));
                Assert.That(remote.Presentation.SupplyOverviewShipmentId, Is.EqualTo(arrival.TargetId));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                long binding = remote.Client.BindingGeneration;
                var runtime = (ProductRuntimeComponent)remote.Client.Runtime;
                ((Session)remote.Client.Session).Dispose();
                yield return WaitCoopUi(remote, () => runtime.Stage == ProductStage.Disconnected, "Real KCP disconnect");
                Pointer(ClaimUiButton(remote.Hud, "手动恢复"));
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.Confirmed && remote.Client.BindingGeneration > binding, "Real manual recovery new baseline");
                notices = ((Scene)remote.Client.IScene).GetComponent<RestaurantNoticesComponent>();
                Assert.That(notices.Entries, Is.Empty, "Recovery shows no historical arrival");
                command = remote.Client.NextCommandId;
                var restoredPage = remote.Presentation.OverviewPage;
                long restoredTarget = remote.Presentation.SupplyOverviewShipmentId;
                oldRow();
                Assert.That(remote.Presentation.OverviewPage, Is.EqualTo(restoredPage));
                Assert.That(remote.Presentation.SupplyOverviewShipmentId, Is.EqualTo(restoredTarget));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
            }
        }
    }
}
#endif
