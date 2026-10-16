#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private IEnumerator AdviceReject(Action send, string reason)
        {
            long command = Client.NextCommandId + 1;
            send();
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Rejected, "Actual advice rejection: " + reason);
            Assert.That(Grid.LastReason, Is.EqualTo(reason));
        }

        private void AdviceRead(string expected)
        {
            long command = Client.NextCommandId;
            long hand = Grid.Actors[Client.MemberId].HandItemId;
            long[] items = Grid.Items.Keys.OrderBy(id => id).ToArray();
            Pointer(Button("查看处理说明"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.ActionAdvice));
            Assert.That(Hud.DetailsText.text, Does.Contain(expected).And.Contain("不会重新执行原动作"));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(hand));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(items));
        }

        [UnityTest, Category("EntryAdvice"), Timeout(150000)]
        public IEnumerator EntryAdviceActualHandContainerPathTargetAndLockedRejectionsKeepMaterialsAndOriginalCommands()
        {
            yield return StartLauncher();
            yield return Command("拿 备料台 ·", true);
            yield return Command("放 切配台 ·", true);
            var chopping = Grid.Stations.Values.Single(station => (station.Capabilities & GridStationCapability.Chop) != 0);
            long tomato = Grid.Stations[chopping.Id].ItemId;
            yield return Command("拿 碗台 ·", true);
            long bowl = Grid.Actors[Client.MemberId].HandItemId;
            yield return AdviceReject(() => Presentation.SendGridAction(GridAction.StartStationWork, chopping.Cell, chopping.Id, tomato, tomato), "HandFull");
            AdviceRead("手槽或工作占用");
            var retiredItems = CaptureButtonCallback(Button("查看当前物品"));
            Pointer(Button("查看当前物品"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Items));
            long command = Client.NextCommandId;
            Pointer(Button("全部动作"));
            yield return Command("放 碗台 ·", true);
            retiredItems();
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            Assert.That(Client.NextCommandId, Is.EqualTo(command + 1));
            yield return Command("拿 碗台 ·", true);
            yield return AdviceReject(() => Presentation.SendGridAction(GridAction.StartHeldWork, expected: bowl, expectedTarget: bowl), "RequiresHeldEggBowl");
            AdviceRead("载具、内容或配方条件不匹配");
            command = Client.NextCommandId;
            Pointer(Button("查看本关菜谱"));
            Assert.That(Presentation.ActionGroup, Is.EqualTo(GridActionGroup.Recipes));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.Items[bowl].Contents, Is.Empty);
            Pointer(Button("全部动作"));
            yield return Command("放 碗台 ·", true);
            yield return AdviceReject(() => Presentation.SendGridAction(GridAction.Move, new GridCell(0, 0)), "BlockedCell");
            AdviceRead("当前位置或通路");
            Pointer(Button("关闭处理说明"));
            yield return AdviceReject(() => Presentation.SendGridAction(GridAction.TakeCounter, chopping.Cell, chopping.Id, tomato + 1), "TargetChanged");
            AdviceRead("原目标、订单或布局已经变化");
            Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "查看本次请求目标"), Is.False);
            Pointer(Button("关闭处理说明"));
            yield return Command("切配 切配台 ·", true);
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.TomatoChunks), "Original chop completes after rejected requests");
            yield return Command("拿 切配台 ·", true);
            yield return Command("添加 灶台 ·", true);
            yield return Command("拿 食材台 ·", true);
            yield return Command("添加 碗台 ·", true);
            yield return Command("拿 碗台 ·", true);
            yield return Command("开始打蛋");
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.EggLiquid), "Original egg work completes");
            yield return Command("倒蛋液 灶台 ·", true);
            yield return Command("放 碗台 ·", true);
            yield return Command("煮汤 灶台 ·", true);
            yield return Wait(() => Grid.AutoProcesses.Values.Any(process => process.State == GridAutoState.Ready), "Original soup batch completes");
            var stove = Grid.Stations.Values.Single(station => station.Name == "灶台");
            var batch = Grid.AutoProcesses.Values.Single();
            int portions = batch.RemainingPortions;
            yield return Command("拿 面包台 ·", true);
            long bread = Grid.Actors[Client.MemberId].HandItemId;
            yield return AdviceReject(() => Presentation.SendGridAction(GridAction.AddToContainer, stove.Cell, stove.Id, bread, stove.ItemId), "ProcessLocked");
            AdviceRead("加工、工位占用");
            command = Client.NextCommandId;
            Pointer(Button("查看本次请求目标"));
            Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Stations));
            Assert.That(Presentation.SelectedStationId, Is.EqualTo(stove.Id));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            Assert.That(Grid.AutoProcesses[batch.Id].RemainingPortions, Is.EqualTo(portions));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(bread));
            Pointer(Button("全部动作"));
            yield return Command("放 面包台 ·", true);
        }

        [UnityTest, Category("EntryAdvice"), Timeout(150000)]
        public IEnumerator EntryAdviceProductionPermissionUnknownDiagnosticLateOutcomeAndNextLevelRetireOldViews()
        {
            string previousClipboard = GUIUtility.systemCopyBuffer;
            try
            {
                yield return StartLauncher(menuVariant: ProductMenuVariant.Growth);
                Assert.That(Grid.LevelConfigId, Is.EqualTo(701));
                int source = Tables.Instance.DTSupplyConfig.DataList.Single(row => row.MaterialDefinitionId == GridKitchenRules.Orange).Id;
                int quota = Grid.SupplySources[source].RemainingPackages;
                long rejected = Client.NextCommandId + 1;
                Pointer(Button("全部动作"));
                yield return SupplyOverviewReject("受限采购 橙子", nameof(ProductionPermissionDecision.MenuRestricted));
                AdviceRead("菜单、设备或角色权限");
                long command = Client.NextCommandId;
                Pointer(Button("查看本关菜谱"));
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                Assert.That(Grid.AllowedDishIds, Has.No.Member(3));
                Assert.That(Grid.SupplySources[source].RemainingPackages, Is.EqualTo(quota));
                Assert.That(Grid.SupplyShipments, Is.Empty);
                Pointer(Button("全部动作"));
                yield return Command("拿 碗台 ·", true);
                command = Client.NextCommandId;
                long serial = Grid.LastProductOutcomeSerial + 1;
                // 只向真实本地消息入口投递较早命令的迟到结果，不改权威或投影字段。
                Runtime.Root().GetComponent<ProcessInnerSender>().Send(Client.GetActorId(), ProductMessages.Create<ProductCommandOutcome>(value =>
                {
                    value.MatchId = Grid.MatchId; value.WorldId = Grid.WorldId; value.WorldGeneration = Grid.WorldGeneration;
                    value.LevelId = value.OriginLevelId = Grid.LevelId;
                    value.LevelGeneration = value.OriginLevelGeneration = Grid.LevelGeneration;
                    value.MemberId = Grid.MemberId; value.BindingGeneration = Grid.BindingGeneration; value.Version = Grid.Version;
                    value.CommandId = rejected; value.Kind = (int)ProductCommandKind.Supply; value.Status = (int)GridResultStatus.Rejected;
                    value.Reason = "MenuRestricted"; value.OutcomeSerial = serial;
                }));
                yield return Wait(() => Grid.LastProductOutcomeSerial >= serial, "Actual ProductClient handler consumes late outcome");
                Assert.That(Grid.LastCommandId, Is.EqualTo(command));
                Assert.That(Grid.LastStatus, Is.EqualTo(GridResultStatus.Executed));
                Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "查看处理说明"), Is.False);
                yield return AdviceReject(() => Presentation.SendGridAction((GridAction)int.MaxValue), "InvalidCommand");
                AdviceRead("暂未识别该原因");
                Assert.That(Hud.DetailsText.text, Does.Contain("原始原因：InvalidCommand"));
                Assert.That(Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text == "查看当前物品"), Is.False);
                command = Client.NextCommandId;
                Pointer(Button("复制诊断信息"));
                Assert.That(GUIUtility.systemCopyBuffer, Does.Contain("Reason=InvalidCommand").And.Contain("World=" + Grid.WorldId).And.Contain("Command=" + command));
                Assert.That(GUIUtility.systemCopyBuffer, Does.Not.Contain("Credential").And.Not.Contain("PrivateKey"));
                var retiredCopy = CaptureButtonCallback(Button("复制诊断信息"));
                var retiredClose = CaptureButtonCallback(Button("关闭处理说明"));
                Pointer(Button("关闭处理说明"));
                AdviceRead("暂未识别该原因");
                GUIUtility.systemCopyBuffer = "entry-advice-owned-sentinel";
                retiredCopy(); retiredClose();
                Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo("entry-advice-owned-sentinel"));
                Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.ActionAdvice));
                int mono = Hud.GetInstanceID();
                Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
                yield return Wait(() => Presentation.Ready, "Advice managed HUD pool reopens");
                retiredCopy(); retiredClose();
                Assert.That(Hud.GetInstanceID(), Is.EqualTo(mono));
                Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
                Assert.That(Presentation.AdviceSubmittedCommand, Is.Zero);
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                Pointer(Button("全部动作"));
                Match match = ((HostSessionComponent)Runtime.Authority).Match;
                RestaurantRuntime restaurant = match.Restaurant;
                // 仅缩短本测试营业时长，仍经真实顾客和前厅自然收口。
                ((Level)restaurant.CurrentLevel).OperatingTicks = 100;
                match.ProductKitchen().Dirty = true;
                yield return UnlockUiStartOperating();
                yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Growth round naturally settles", 65);
                yield return Command("装修 解锁现榨菜单");
                yield return AdviceReject(() => Presentation.SendGridAction((GridAction)int.MaxValue), "InvalidCommand");
                AdviceRead("暂未识别该原因");
                retiredCopy = CaptureButtonCallback(Button("复制诊断信息"));
                long oldLevel = Grid.LevelId;
                yield return Command("下一关");
                Assert.That(Grid.LevelId, Is.Not.EqualTo(oldLevel));
                command = Client.NextCommandId;
                GUIUtility.systemCopyBuffer = "entry-advice-owned-sentinel";
                retiredCopy();
                Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo("entry-advice-owned-sentinel"));
                Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
                Assert.That(Presentation.AdviceSubmittedCommand, Is.Zero);
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
            }
            finally { GUIUtility.systemCopyBuffer = previousClipboard; }
        }

        [UnityTest, Category("EntryAdvice"), Timeout(120000)]
        public IEnumerator EntryAdviceHostAndActualKcpClientReadContainerRejectionThenRetireChangedContentWithoutRetry()
        {
            yield return StartCoopHostLauncher();
            yield return Command("拿 食材台 ·", true);
            long egg = Grid.Actors[Client.MemberId].HandItemId;
            var stove = Grid.Stations.Values.Single(station => station.Name == "灶台");
            yield return AdviceReject(() => Presentation.SendGridAction(GridAction.AddToContainer, stove.Cell, stove.Id, egg, stove.ItemId), "IncompatibleMaterial");
            AdviceRead("载具、内容或配方条件不匹配");
            long hostCommand = Client.NextCommandId;
            Pointer(Button("查看本关菜谱"));
            Assert.That(Client.NextCommandId, Is.EqualTo(hostCommand));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(egg));
            Presentation.SendGridAction(GridAction.Move, new GridCell(2, 10));
            yield return Wait(() => Grid.LastStatus == GridResultStatus.Executed && Grid.LastCommandId == hostCommand + 1, "Host moves aside through original input");
            Presentation.CloseGridHud();
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases the shared Editor HUD");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Actual KCP Client HUD ready");
                yield return AssemblyUiCommand(remote, "拿 备料台 ·", true);
                var bowlStation = remote.Grid.Stations.Values.Single(station => station.Name == "碗台");
                long bowl = bowlStation.ItemId, tomato = remote.Grid.Actors[remote.Client.MemberId].HandItemId;
                long command = remote.Client.NextCommandId + 1;
                remote.Presentation.SendGridAction(GridAction.AddToContainer, bowlStation.Cell, bowlStation.Id, tomato, bowl);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == command && remote.Grid.LastStatus == GridResultStatus.Rejected, "Real KCP incompatible container rejection");
                Assert.That(remote.Grid.LastReason, Is.EqualTo("IncompatibleMaterial"));
                Pointer(ClaimUiButton(remote.Hud, "查看处理说明"));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("载具、内容或配方条件不匹配"));
                var retiredTarget = CaptureButtonCallback(ClaimUiButton(remote.Hud, "查看本次请求目标"));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.Items[bowl].Contents, Is.Empty);
                var add = Client.CallProduct(Grid.CreateGridCommand(GridAction.AddToContainer, bowlStation.Cell, bowlStation.Id, egg, bowl));
                long hostAdd = Client.NextCommandId;
                yield return WaitCoopUi(remote, () => add.Status != UniTaskStatus.Pending, "Original Host local mailbox accepts egg addition");
                using (var response = (GridCommandResponse)add.GetAwaiter().GetResult()) { Assert.That(response.Error, Is.Zero, response.Message); }
                yield return WaitCoopUi(remote, () => Grid.LastCommandId == hostAdd && Grid.LastStatus == GridResultStatus.Executed && remote.Grid.Items[bowl].Contents.Contains(egg), "Actual other member changes contents of the same container");
                yield return WaitCoopUi(remote, () => remote.Presentation.OverviewPage == GridOverviewPage.None, "Old container advice clears after content changes");
                Assert.That(remote.Grid.Stations[bowlStation.Id].ItemId, Is.EqualTo(bowl));
                retiredTarget();
                Assert.That(remote.Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.EqualTo(tomato));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                Pointer(ClaimUiButton(remote.Hud, "全部动作"));
                yield return AssemblyUiCommand(remote, "放 备料台 ·", true);
                yield return AssemblyUiCommand(remote, "拿 碗台 ·", true);
                Assert.That(remote.Grid.Actors[remote.Client.MemberId].HandItemId, Is.EqualTo(bowl));
                Assert.That(remote.Grid.Items[bowl].Contents, Is.EqualTo(new[] { egg }));
                var pause = Client.CallProduct(Grid.CreateLevelCommand(LevelControlAction.Pause));
                yield return WaitCoopUi(remote, () => pause.Status != UniTaskStatus.Pending, "Only Host pauses the shared world");
                using (var response = (LevelCommandResponse)pause.GetAwaiter().GetResult()) { Assert.That(response.Error, Is.Zero); }
                yield return WaitCoopUi(remote, () => remote.Grid.LevelPaused, "Remote advice sees real paused state");
                command = remote.Client.NextCommandId + 1;
                remote.Presentation.SendGridAction(GridAction.Move, new GridCell(2, 10));
                yield return WaitCoopUi(remote, () => remote.Grid.LastCommandId == command && remote.Grid.LastStatus == GridResultStatus.Rejected, "Actual paused action is rejected");
                Assert.That(remote.Grid.LastReason, Is.EqualTo("Paused"));
                Pointer(ClaimUiButton(remote.Hud, "查看处理说明"));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("世界当前已暂停"));
                Pointer(ClaimUiButton(remote.Hud, "查看暂停与权限帮助"));
                Assert.That(remote.Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Help));
                Assert.That(remote.Hud.DetailsText.text, Does.Contain("已暂停").And.Contain("阅读不会自动恢复"));
                long tick = remote.Grid.Tick;
                var retiredHelp = CaptureButtonCallback(ClaimUiButton(remote.Hud, "帮助：前往菜谱"));
                Pointer(ClaimUiButton(remote.Hud, "查看处理说明"));
                retiredHelp();
                Assert.That(remote.Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.ActionAdvice));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.Tick, Is.EqualTo(tick));
                Pointer(ClaimUiButton(remote.Hud, "关闭处理说明"));
                var resume = Client.CallProduct(Grid.CreateLevelCommand(LevelControlAction.Resume));
                yield return WaitCoopUi(remote, () => resume.Status != UniTaskStatus.Pending, "Only Host explicitly resumes after reading");
                using (var response = (LevelCommandResponse)resume.GetAwaiter().GetResult()) { Assert.That(response.Error, Is.Zero); }
                yield return WaitCoopUi(remote, () => !remote.Grid.LevelPaused, "Original resume still commits");
            }
        }
    }
}
#endif
