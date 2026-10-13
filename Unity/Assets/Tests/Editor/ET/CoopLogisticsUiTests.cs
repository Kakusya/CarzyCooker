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
        public IEnumerator CoopLogisticsLifetimeUiRemotePurchaseReceivePortionsAndFacilitiesUseManagedOwners()
        {
            yield return StartCoopHostLauncher(); ((Scene)Client.IScene).RemoveComponent<GridPresentationComponent>(); yield return null;
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Remote logistics HUD ready");
                Button RemoteButton(string caption) => remote.Hud.GetComponentsInChildren<Button>(false).Single(value => value.GetComponentInChildren<Text>().text.StartsWith(caption));
                IEnumerator Do(string caption)
                {
                    long command = remote.Client.NextCommandId + 1; Pointer(RemoteButton(caption));
                    Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                    yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == command && remote.Grid.LastStatus == GridResultStatus.Executed, caption);
                }
                int source = Tables.Instance.DTSupplyConfig.DataList.Single(value => value.MaterialDefinitionId == GridKitchenRules.Tomato).Id;
                Button purchase = RemoteButton("采购 番茄"); var oldPurchase = CaptureButtonCallback(purchase);
                long command = remote.Client.NextCommandId + 1; int mono = remote.Hud.GetInstanceID(); Pointer(purchase);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command)); remote.Presentation.CloseGridHud();
                oldPurchase(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(command)); yield return null; remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.SupplyShipments.Values.Any(value => value.SourceId == source && value.Stage == ShipmentStage.Ready), "Committed remote purchase survives HUD close");
                Assert.That(remote.Hud.GetInstanceID(), Is.EqualTo(mono)); Assert.That(remote.Grid.SupplySources[source].RemainingPackages, Is.EqualTo(4));
                oldPurchase(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                var shipment = remote.Grid.SupplyShipments.Values.Single(value => value.SourceId == source);
                Button receive = RemoteButton("接收 番茄"); var oldReceive = CaptureButtonCallback(receive); ++command; Pointer(receive);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command)); Assert.That(remote.Grid.SupplyShipments[shipment.Id].PackageItemId, Is.Zero, "Receipt is not material arrival");
                remote.Presentation.CloseGridHud(); oldReceive(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                yield return null; remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.SupplyShipments[shipment.Id].Stage == ShipmentStage.Received, "Real receipt continues to current receiving station");
                long package = remote.Grid.SupplyShipments[shipment.Id].PackageItemId; long[] materials = remote.Grid.Items[package].Contents.ToArray();
                oldReceive(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                foreach (long material in materials)
                {
                    yield return Do("拆一份 番茄"); Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.EqualTo(material));
                    yield return Do("投垃圾 → 垃圾桶"); Assert.That(remote.Grid.Items.ContainsKey(material), Is.False);
                }
                string station = remote.Grid.Stations[shipment.ReceivingStationId].Name;
                yield return Do("拿 " + station + " ·"); Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.EqualTo(package));
                yield return Do("投垃圾 → 垃圾桶"); Assert.That(remote.Grid.Items.ContainsKey(package), Is.False);
                Assert.That(remote.Grid.SupplySources[source].RemainingPackages, Is.EqualTo(4));
                yield return Do("拿 碗台 ·"); long bowl = remote.Grid.Actors[remote.Client.MemberId].HandItemId;
                yield return Do("接水 → 水池"); long water = remote.Grid.Items[bowl].Contents.Single();
                Assert.That(remote.Grid.Items[water].DefinitionId, Is.EqualTo(GridKitchenRules.Water));
                yield return Do("倒液 → 水池"); Assert.That(remote.Grid.Items.ContainsKey(water), Is.False);
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.EqualTo(bowl)); Assert.That(remote.Grid.Items[bowl].Contents, Is.Empty);
                Assert.That(remote.Client.Stage, Is.EqualTo(ProductClientStage.Joined));
            }
        }
    }
}
#endif
