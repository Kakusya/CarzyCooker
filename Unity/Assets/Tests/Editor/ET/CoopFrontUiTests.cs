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
        private IEnumerator CoopFrontUiPrepareSoup()
        {
            string buff = Hud.GetComponentsInChildren<Button>(false).First(value => value.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(buff);
            yield return Command("拿 备料台 ·", true); yield return Command("放 切配台 ·", true); yield return Command("切配 切配台 ·", true);
            yield return Wait(() => Grid.Processes.Values.Any(value => value.State == GridWorkState.Completed), "Real Host chopping");
            yield return Command("拿 切配台 ·", true); yield return Command("添加 灶台 ·", true);
            yield return Command("拿 食材台 ·", true); yield return Command("添加 碗台 ·", true); yield return Command("拿 碗台 ·", true); yield return Command("开始打蛋");
            yield return Wait(() => Grid.Items.Values.Any(value => value.DefinitionId == GridKitchenRules.EggLiquid), "Real Host egg work");
            yield return Command("倒蛋液 灶台 ·", true); yield return Command("放 碗台 ·", true); yield return Command("煮汤 灶台 ·", true);
            yield return Wait(() => Grid.AutoProcesses.Values.Any(value => value.State == GridAutoState.Ready), "Real Host soup ready");
            yield return Command("拿 碗台 ·", true); yield return Command("盛一份 灶台 ·", true); yield return Command("放 碗台 ·", true);
        }

        [UnityTest]
        [Category("RestaurantArtFinal")]
        public IEnumerator CoopFrontProjectionUiRemoteServeAndClearSurviveManagedReopenWithoutRetiredCallbacks()
        {
            yield return StartCoopHostLauncher(); yield return CoopFrontUiPrepareSoup();
            long bowl = Grid.Stations.Values.Single(value => value.Name == "碗台").ItemId;
            yield return Command("开始营业");
            ((Scene)Client.IScene).RemoveComponent<GridPresentationComponent>(); yield return null;
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Remote actual front HUD ready");
                Button RemoteButton(string caption) => remote.Hud.GetComponentsInChildren<Button>(false).Single(value => value.GetComponentInChildren<Text>().text.StartsWith(caption));
                IEnumerator Do(string caption)
                {
                    long command = remote.Client.NextCommandId + 1; Pointer(RemoteButton(caption));
                    Assert.That(remote.Client.NextCommandId, Is.EqualTo(command), "One actual remote pointer request");
                    yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == command && remote.Grid.LastStatus == GridResultStatus.Executed, caption);
                }
                yield return Do("拿 碗台 ·"); Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.EqualTo(bowl));
                yield return WaitCoopUi(remote, () => remote.Grid.Orders.Values.Any(value => value.State == OrderState.Waiting && value.DishId == 1), "Real customer soup order opens");
                var order = remote.Grid.Orders.Values.First(value => value.State == OrderState.Waiting && value.DishId == 1);
                string table = remote.Grid.Stations[order.TableStationId].Name;
                yield return Do("前往 " + table);
                Button serve = RemoteButton("交付 番茄蛋花汤 → " + table); var oldServe = CaptureButtonCallback(serve);
                long servedCommand = remote.Client.NextCommandId + 1; int mono = remote.Hud.GetInstanceID();
                Pointer(serve); Assert.That(remote.Client.NextCommandId, Is.EqualTo(servedCommand)); remote.Presentation.CloseGridHud();
                oldServe(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(servedCommand)); yield return null; remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.Orders[order.Id].State == OrderState.Served, "Accepted remote delivery survives display close");
                Assert.That(remote.Hud.GetInstanceID(), Is.EqualTo(mono)); Assert.That(remote.Grid.LevelScore, Is.EqualTo(order.Score));
                Assert.That(remote.Grid.Items[bowl].Location, Is.EqualTo(GridItemLocation.Table)); Assert.That(remote.Grid.Items[bowl].Contents, Is.Empty);
                oldServe(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(servedCommand));
                yield return WaitCoopUi(remote, () => remote.Grid.FrontWorks.Values.Any(value => value.Kind == FrontWorkKind.Clear && value.ItemId == bowl && value.State == FrontWorkState.Available), "Actual customer leaves same original bowl");
                Button clear = RemoteButton("清桌 " + table); var oldClear = CaptureButtonCallback(clear);
                long clearCommand = remote.Client.NextCommandId + 1; Pointer(clear); Assert.That(remote.Client.NextCommandId, Is.EqualTo(clearCommand));
                remote.Presentation.CloseGridHud(); oldClear(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(clearCommand));
                yield return null; remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.Items[bowl].Location is GridItemLocation.DirtyPool or GridItemLocation.CleanPool, "Remote clear continues after managed reopen");
                oldClear(); Assert.That(remote.Client.NextCommandId, Is.EqualTo(clearCommand));
                yield return WaitCoopUi(remote, () => remote.Grid.Items[bowl].Location == GridItemLocation.CleanPool, "Companion washes original remote-served bowl", 35);
                Assert.That(remote.Grid.Items[bowl].Hygiene, Is.EqualTo(ItemHygiene.Clean));
                Assert.That(remote.Grid.Stations[remote.Grid.Items[bowl].HolderId].PoolItemIds.Count(value => value == bowl), Is.EqualTo(1));
                Assert.That(remote.Client.Stage, Is.EqualTo(ProductClientStage.Joined));
            }
        }
    }
}
#endif
