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
        [UnityTest, Category("OrderIntentsUi"), Timeout(180000)]
        public IEnumerator EntryOrderIntentActualHostAndKcpUiSameOrderOriginalDeliveryPooledAndRecoveryCallbacks()
        {
            yield return StartCoopHostLauncher();
            CreateSaveUiRepository(Runtime, out _, out _);
            yield return CoopFrontUiPrepareSoup();
            long bowl = Grid.Stations.Values.Single(value => value.Name == "碗台").ItemId;
            yield return Command("开始营业");
            yield return Wait(() => Grid.Orders.Values.Any(value => value.State == OrderState.Waiting && value.DishId == 1), "Actual soup customer opens an order");
            var order = Grid.Orders.Values.First(value => value.State == OrderState.Waiting && value.DishId == 1);
            long command = Client.NextCommandId;
            Pointer(Button(OrderNavigationCaption(Grid, order)));
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Reading remains read only");
            yield return Command("我准备制作此单（不独占）");
            Assert.That(Grid.OrderIntents.Single().MemberId, Is.EqualTo(Client.MemberId));
            Assert.That(Hud.DetailsText.text, Does.Contain("可多人"));
            Presentation.CloseGridHud();
            ((GridKitchenBoard)Presentation.Board).Visible = false;
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases managed HUD");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Orders.ContainsKey(order.Id), "Actual KCP shares target order");
                Pointer(ClaimUiButton(remote.Hud, OrderNavigationCaption(remote.Grid, remote.Grid.Orders[order.Id])));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("成员 " + Client.MemberId));
                IEnumerator Do(string caption, bool prefix = false)
                {
                    long expected = remote.Client.NextCommandId + 1;
                    Pointer(ClaimUiButton(remote.Hud, caption, prefix));
                    yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == expected && remote.Grid.LastStatus == GridResultStatus.Executed, caption);
                }
                yield return Do("我准备制作此单（不独占）");
                yield return WaitCoopUi(remote, () => Grid.OrderIntents.Count == 2 && remote.Grid.OrderIntents.Count == 2, "Both members select one order without exclusivity");
                yield return Do("撤销我的制作意向");
                Assert.That(remote.Grid.OrderIntents.Single().MemberId, Is.EqualTo(Client.MemberId));
                yield return Do("我准备制作此单（不独占）");
                var oldIntent = CaptureButtonCallback(ClaimUiButton(remote.Hud, "撤销我的制作意向"));
                command = remote.Client.NextCommandId;
                remote.Presentation.CloseGridHud(); yield return null; remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Pooled order HUD reopens");
                oldIntent();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.OrderIntents.Count, Is.EqualTo(2));
                yield return Do("拿 碗台 ·", true);
                string table = remote.Grid.Stations[order.TableStationId].Name;
                yield return Do("前往 " + table, true);
                yield return Do("交付 番茄蛋花汤 → " + table, true);
                yield return WaitCoopUi(remote, () => Grid.OrderIntents.Count == 0 && remote.Grid.OrderIntents.Count == 0, "Original delivery clears both intentions");
                Assert.That(remote.Grid.Orders[order.Id].State, Is.EqualTo(OrderState.Served));
                Assert.That(remote.Grid.LevelScore, Is.EqualTo(order.Score));
                Assert.That(remote.Grid.Items[bowl].Contents, Is.Empty);
                command = remote.Client.NextCommandId; oldIntent();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                yield return WaitCoopUi(remote, () => remote.Grid.Orders.Values.Any(value => value.State == OrderState.Waiting), "Another actual waiting customer", 30);
                var next = remote.Grid.Orders.Values.First(value => value.State == OrderState.Waiting);
                Pointer(ClaimUiButton(remote.Hud, OrderNavigationCaption(remote.Grid, next)));
                yield return Do("我准备制作此单（不独占）");
                var oldBindingButton = CaptureButtonCallback(ClaimUiButton(remote.Hud, "撤销我的制作意向"));
                long binding = remote.Client.BindingGeneration;
                ProductRuntimeComponent runtime = remote.Client.Runtime;
                ((Session)remote.Client.Session).Dispose();
                yield return WaitCoopUi(remote, () => runtime.Stage == ProductStage.Disconnected && Grid.OrderIntents.Count == 0, "Disconnect clears only the departed member intent");
                Pointer(ClaimUiButton(remote.Hud, "手动恢复"));
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.Confirmed && remote.Client.BindingGeneration > binding, "Actual manual recovery gets a new binding");
                Assert.That(remote.Grid.OrderIntents, Is.Empty);
                command = remote.Client.NextCommandId;
                oldBindingButton();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.OrderIntents, Is.Empty);
            }
        }
    }
}
#endif
