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
        private static Button FrontWorkboardRow(MonoUIGridKitchen hud, GridClientComponent grid, long workId)
        {
            int index = grid.FrontWorks.Values.OrderBy(value => value.Kind).ThenBy(value => value.Id)
                .Select((work, offset) => (work, offset)).Single(value => value.work.Id == workId).offset + 1;
            return ClaimUiButton(hud, "工作 " + index + " ·", true);
        }

        [UnityTest, Category("EntryExperience"), Timeout(150000)]
        public IEnumerator EntryFrontWorkboardSoloActualAskClearWashPauseAndNaturalDrainingKeepOriginalBowl()
        {
            yield return StartLauncher();
            yield return CoopFrontUiPrepareSoup();
            Match match = ((HostSessionComponent)Runtime.Authority).Match;
            ((Level)((RestaurantRuntime)match.Restaurant).CurrentLevel).OperatingTicks = 100;
            match.ProductKitchen().Dirty = true;
            yield return Command("开始营业");
            yield return Command("拿 碗台 ·", true);
            long bowl = Grid.Actors[Client.MemberId].HandItemId;
            long command = Client.NextCommandId;
            Pointer(Button("前厅工作看板"));
            Assert.That(Hud.DetailsText.text, Does.Contain("必要工作"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(bowl), "A full hand does not hide the read-only board");
            yield return Wait(() => Grid.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == 1), "Actual companion asks soup customer");
            var ask = Grid.FrontWorks.Values.First(work => work.Kind == FrontWorkKind.Ask);
            Pointer(FrontWorkboardRow(Hud, Grid, ask.Id));
            Assert.That(Hud.DetailsText.text, Does.Contain("询问").And.Contain("关联顾客"));
            var order = Grid.Orders.Values.First(order => order.State == OrderState.Waiting && order.DishId == 1);
            string table = Grid.Stations[order.TableStationId].Name;
            yield return Command("前往 " + table);
            yield return Command("交付 番茄蛋花汤 → " + table);
            Presentation.SendGridAction(GridAction.Move, new GridCell(1, 16));
            yield return Wait(() => Grid.Actors[Client.MemberId].Cell == new GridCell(1, 16) && Grid.Actors[Client.MemberId].Motion == GridMotion.Standing,
                "Original movement leaves clear work to approach from the kitchen");
            yield return Wait(() => Grid.FrontWorks.Values.Any(work => work.Kind == FrontWorkKind.Clear && work.ItemId == bowl && work.State == FrontWorkState.Available), "Customer naturally leaves original bowl");
            var clear = Grid.FrontWorks.Values.Single(work => work.Kind == FrontWorkKind.Clear && work.ItemId == bowl);
            Pointer(FrontWorkboardRow(Hud, Grid, clear.Id));
            Assert.That(Hud.DetailsText.text, Does.Contain("可认领").And.Contain("关联餐具"));
            var retiredClaim = CaptureButtonCallback(Button("清桌 " + table));
            yield return Command("清桌 " + table);
            Assert.That(Hud.DetailsText.text, Does.Contain("玩家 " + Client.MemberId).And.Contain("前往中"));
            Assert.That(Hud.DetailsText.text, Does.Contain("占用桌位").And.Contain("待清桌"));
            yield return Command("停手前厅工作");
            double retained = Grid.FrontWorks[clear.Id].Progress;
            Assert.That(Grid.FrontWorks[clear.Id].State, Is.EqualTo(FrontWorkState.Available));
            Assert.That(Hud.DetailsText.text, Does.Contain("可认领").And.Contain("无执行者"));
            yield return Wait(() => Grid.Actors[Client.MemberId].Motion == GridMotion.Standing, "Stopping approach finishes the original current grid step before another claim");
            yield return Command("清桌 " + table);
            Assert.That(Grid.FrontWorks[clear.Id].Progress, Is.GreaterThanOrEqualTo(retained));
            yield return Wait(() => Grid.FrontWorks[clear.Id].State == FrontWorkState.Completed, "Player really clears the original table");
            yield return Wait(() => Grid.FrontWorks.Values.Any(work => work.Kind == FrontWorkKind.Wash && work.ItemId == bowl && work.State == FrontWorkState.Working), "Companion actually washes the same bowl");
            var wash = Grid.FrontWorks.Values.Single(work => work.Kind == FrontWorkKind.Wash && work.ItemId == bowl);
            Pointer(FrontWorkboardRow(Hud, Grid, wash.Id));
            Assert.That(Hud.DetailsText.text, Does.Contain("小麦").And.Contain("执行中").And.Contain("实际进度").And.Contain("正在收尾"));
            Assert.That(Grid.LevelPhase, Is.EqualTo(LevelPhase.Draining));
            yield return Command("暂停");
            double progress = Grid.FrontWorks[wash.Id].Progress;
            command = Client.NextCommandId;
            Pointer(FrontWorkboardRow(Hud, Grid, wash.Id));
            yield return null; yield return null;
            Assert.That(Grid.FrontWorks[wash.Id].Progress, Is.EqualTo(progress));
            Assert.That(Hud.DetailsText.text, Does.Contain("已暂停"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Command("恢复");
            command = Client.NextCommandId;
            retiredClaim();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Actual front service closure completes", 60);
            Assert.That(Grid.Items[bowl].Location, Is.EqualTo(GridItemLocation.CleanPool));
            Assert.That(Grid.Items[bowl].Hygiene, Is.EqualTo(ItemHygiene.Clean));
            Assert.That(Grid.Items.Values.Count(item => item.Id == bowl), Is.EqualTo(1));
            Assert.That(Hud.DetailsText.text, Does.Contain("前厅已收口"));
            Assert.That(Grid.AutoProcesses.Values.Any(batch => batch.State == GridAutoState.Ready && batch.RemainingPortions > 0), Is.True,
                "Real front closure does not require draining the remaining kitchen soup batch");
            var retiredRow = CaptureButtonCallback(FrontWorkboardRow(Hud, Grid, wash.Id));
            yield return Command("下一关");
            command = Client.NextCommandId;
            retiredRow(); retiredClaim();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(Presentation.FrontWorkboardSelectedId, Is.Zero);
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("前厅工作看板"));
            var oldOpen = CaptureButtonCallback(Button("关闭前厅看板"));
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Front workboard managed pool reuse");
            oldOpen();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Pointer(Button("全部动作"));
            yield return Command("取回 " + Grid.ItemDescription(bowl));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(bowl));
        }

        [UnityTest, Category("EntryExperience"), Timeout(150000)]
        public IEnumerator EntryFrontWorkboardActualKcpClaimRaceAndCompanionOwnershipStayAuthoritative()
        {
            yield return StartCoopHostLauncher();
            yield return CoopFrontUiPrepareSoup();
            yield return Command("开始营业");
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Keep Host owner while remote receives the singleton HUD");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Real KCP front board ready");
                long command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "前厅工作看板"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                yield return WaitCoopUi(remote, () => remote.Grid.FrontWorks.Values.Any(work => work.Kind == FrontWorkKind.Ask && work.WorkerKind == FrontWorkerKind.Companion && work.State is FrontWorkState.Approaching or FrontWorkState.Working), "Actual companion ask ownership");
                var ask = remote.Grid.FrontWorks.Values.First(work => work.Kind == FrontWorkKind.Ask && work.WorkerKind == FrontWorkerKind.Companion);
                Pointer(FrontWorkboardRow(remote.Hud, remote.Grid, ask.Id));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("小麦"));
                remote.Presentation.SendFrontWork(FrontWorkAction.Claim, ask.Id, ask.ClaimSerial, remote.Grid.LevelId, remote.Grid.LevelGeneration);
                yield return WaitCoopUi(remote, () => remote.Grid.LastStatus == GridResultStatus.Rejected && remote.Grid.LastCommandId == command + 1, "Original authority rejects occupied companion task");
                Assert.That(remote.Grid.FrontWorks[ask.Id].WorkerKind, Is.EqualTo(FrontWorkerKind.Companion));
                Pointer(ClaimUiButton(remote.Hud, "全部动作"));
                yield return AssemblyUiCommand(remote, "拿 碗台 ·", true);
                long bowl = remote.Grid.Actors[remote.Client.MemberId].HandItemId;
                yield return WaitCoopUi(remote, () => remote.Grid.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == 1), "Actual remote soup customer");
                var order = remote.Grid.Orders.Values.First(order => order.State == OrderState.Waiting && order.DishId == 1);
                string table = remote.Grid.Stations[order.TableStationId].Name;
                yield return AssemblyUiCommand(remote, "前往 " + table);
                yield return AssemblyUiCommand(remote, "交付 番茄蛋花汤 → " + table);
                yield return WaitCoopUi(remote, () => remote.Grid.FrontWorks.Values.Any(work => work.Kind == FrontWorkKind.Clear && work.ItemId == bowl && work.State == FrontWorkState.Available), "Actual unclaimed clear available to both players");
                var clear = remote.Grid.FrontWorks.Values.Single(work => work.Kind == FrontWorkKind.Clear && work.ItemId == bowl);
                Pointer(FrontWorkboardRow(remote.Hud, remote.Grid, clear.Id));
                var staleClaim = CaptureButtonCallback(ClaimUiButton(remote.Hud, "清桌 " + table));
                long remoteCommand = remote.Client.NextCommandId + 1;
                // Host本地保留原LocalMessage请求，不为竞争创建第二份业务或强开同一单例UI。
                var localReceipt = Client.CallProduct(Grid.CreateFrontWorkCommand(FrontWorkAction.Claim, clear.Id, clear.ClaimSerial)).AsTask();
                long localCommand = Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "清桌 " + table));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(remoteCommand));
                yield return WaitCoopUi(remote, () => localReceipt.IsCompleted && Grid.LastCommandId == localCommand && remote.Grid.LastCommandId == remoteCommand &&
                    Grid.LastStatus is GridResultStatus.Executed or GridResultStatus.Rejected && remote.Grid.LastStatus is GridResultStatus.Executed or GridResultStatus.Rejected,
                    "Both original local and remote claim commands reach actual outcomes");
                using (var receipt = (FrontWorkResponse)localReceipt.GetAwaiter().GetResult())
                {
                    Assert.That(receipt.Error, Is.Zero, "Admission is separate from the business winner");
                }
                Assert.That(new[] { Grid.LastStatus, remote.Grid.LastStatus }.Count(status => status == GridResultStatus.Executed), Is.EqualTo(1));
                Assert.That(new[] { Grid.LastStatus, remote.Grid.LastStatus }.Count(status => status == GridResultStatus.Rejected), Is.EqualTo(1));
                Assert.That(remote.Grid.FrontWorks[clear.Id].ClaimSerial, Is.EqualTo(clear.ClaimSerial + 1));
                yield return WaitCoopUi(remote, () => remote.Grid.Items[bowl].Location == GridItemLocation.CleanPool, "Only one clear and one real wash return the same bowl", 35);
                command = remote.Client.NextCommandId;
                staleClaim();
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.FrontWorks.Values.Count(work => work.Kind == FrontWorkKind.Wash && work.ItemId == bowl), Is.EqualTo(1));
                var row = CaptureButtonCallback(FrontWorkboardRow(remote.Hud, remote.Grid, clear.Id));
                remote.Presentation.CloseGridHud(); yield return null; remote.Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready, "Remote front board pool reuse");
                row();
                Assert.That(remote.Presentation.FrontWorkboardSelectedId, Is.Zero);
                Assert.That(remote.Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                Assert.That(((ProcessInnerSender)Runtime.Sender).IsStopped, Is.False);
            }
        }
    }
}
#endif
