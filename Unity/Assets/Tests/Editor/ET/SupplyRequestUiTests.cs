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
        [UnityTest, Category("SupplyRequestsUi"), Timeout(150000)]
        public IEnumerator EntrySupplyRequestActualHostPublishesAndKcpMemberProcessesReceivesSplitsAndRecoversWithoutApproval()
        {
            yield return StartCoopHostLauncher();
            CreateSaveUiRepository(Runtime, out _, out _);
            int tomato = Grid.SupplySources.Values.Single(value => Tables.Instance.DTSupplyConfig.Get(value.Id).MaterialDefinitionId == GridKitchenRules.Tomato).Id;
            int quota = Grid.SupplySources[tomato].RemainingPackages;
            Pointer(Button("补货总览")); Pointer(Button("来源 · 番茄补货"));
            yield return Command("请求1包 · 番茄补货");
            var original = Grid.SupplyRequests.Single();
            Assert.That(Grid.SupplyShipments, Is.Empty);
            Assert.That(Grid.SupplySources[tomato].RemainingPackages, Is.EqualTo(quota));
            Assert.That(Hud.DetailsText.text, Does.Contain("待处理（尚未采购）"));
            var oldCancel = CaptureButtonCallback(Button("撤销我的补货请求"));
            var oldProcess = CaptureButtonCallback(Button("处理请求 · 成员 " + Client.MemberId + " ·", true));
            Pointer(Button("来源 · 鸡蛋补货"));
            yield return Command("替换我的请求 · 鸡蛋补货");
            Assert.That(Grid.SupplyRequests.Single().Id, Is.Not.EqualTo(original.Id));
            long command = Client.NextCommandId;
            oldCancel(); oldProcess();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.SupplyShipments, Is.Empty);
            yield return Command("撤销我的补货请求");
            Assert.That(Grid.SupplyRequests, Is.Empty);
            Pointer(Button("来源 · 番茄补货"));
            yield return Command("请求1包 · 番茄补货");
            long requestId = Grid.SupplyRequests.Single().Id;
            var retired = CaptureButtonCallback(Button("撤销我的补货请求"));
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Original managed supply request HUD reopens");
            command = Client.NextCommandId; retired();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.SupplyRequests.Single().Id, Is.EqualTo(requestId));
            Presentation.CloseGridHud(); ((GridKitchenBoard)Presentation.Board).Visible = false;
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases display for actual Client");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.SupplyRequests.Count == 1, "KCP sees Host pending request with zero purchases");
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                Pointer(ClaimUiButton(remote.Hud, "补货总览"));
                command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "查看请求 · 成员 " + Client.MemberId + " ·", true));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("待处理（尚未采购）"));
                IEnumerator Do(string caption, bool prefix = false)
                {
                    long expected = remote.Client.NextCommandId + 1;
                    Pointer(ClaimUiButton(remote.Hud, caption, prefix));
                    yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == expected && remote.Grid.LastStatus == GridResultStatus.Executed, caption);
                }
                var staleProcess = CaptureButtonCallback(ClaimUiButton(remote.Hud, "处理请求 · 成员 " + Client.MemberId + " ·", true));
                yield return Do("处理请求 · 成员 " + Client.MemberId + " ·", true);
                Assert.That(remote.Grid.SupplySources[tomato].RemainingPackages, Is.EqualTo(quota - 1));
                Assert.That(remote.Grid.SupplyShipments.Count, Is.EqualTo(1));
                Assert.That(remote.Grid.SupplyRequests.Single().State, Is.EqualTo(SupplyRequestState.Ordered));
                long shipment = remote.Grid.SupplyRequests.Single().ShipmentId;
                Assert.That(remote.Grid.SupplyShipments.ContainsKey(shipment), Is.True);
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("已下单（不代表已收货）"));
                command = remote.Client.NextCommandId; staleProcess();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Pointer(ClaimUiButton(remote.Hud, "查看请求 · 成员 " + Client.MemberId + " ·", true));
                Assert.That(remote.Presentation.SupplyOverviewShipmentId, Is.EqualTo(shipment));
                yield return WaitCoopUi(remote, () => remote.Grid.SupplyShipments[shipment].Stage == ShipmentStage.Ready, "Original shipment reaches ready state");
                yield return Do("接收 番茄", true);
                long package = remote.Grid.SupplyShipments[shipment].PackageItemId;
                var originalMaterials = remote.Grid.Items[package].Contents.ToArray();
                yield return Do("拆一份 番茄", true);
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.EqualTo(originalMaterials[0]));
                Assert.That(remote.Grid.Items[package].Contents, Is.EqualTo(originalMaterials.Skip(1).ToArray()));
                Pointer(ClaimUiButton(remote.Hud, "来源 · 鸡蛋补货"));
                yield return Do("请求1包 · 鸡蛋补货");
                var oldOwn = CaptureButtonCallback(ClaimUiButton(remote.Hud, "撤销我的补货请求"));
                long binding = remote.Client.BindingGeneration;
                ProductRuntimeComponent runtime = remote.Client.Runtime;
                ((Session)remote.Client.Session).Dispose();
                yield return WaitCoopUi(remote, () => runtime.Stage == ProductStage.Disconnected && Grid.SupplyRequests.All(value => value.MemberId != remote.Client.MemberId), "Only departed publisher hint is removed");
                Pointer(ClaimUiButton(remote.Hud, "手动恢复"));
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.Confirmed && remote.Client.BindingGeneration > binding, "Actual manual recovery has current request and shipment facts");
                Assert.That(remote.Grid.SupplyRequests.Count, Is.EqualTo(1));
                Assert.That(remote.Grid.SupplyRequests.Single().Id, Is.EqualTo(requestId));
                Assert.That(remote.Grid.SupplyShipments.Count, Is.EqualTo(1));
                Assert.That(remote.Grid.Items[package].Contents, Is.EqualTo(originalMaterials.Skip(1).ToArray()));
                command = remote.Client.NextCommandId; oldOwn(); staleProcess();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.SupplySources[tomato].RemainingPackages, Is.EqualTo(quota - 1));
            }
        }
    }
}
#endif
