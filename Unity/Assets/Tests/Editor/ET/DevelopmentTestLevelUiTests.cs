#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using ET.Client;
using Game;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    [FriendOf(typeof(GridBakeProcess))]
    [FriendOf(typeof(FrontLevelState))]
    public sealed partial class SoloRestaurantUiTests
    {
        private RestaurantRuntime TestRestaurant => ((Match)((HostSessionComponent)Runtime.Authority).Match).Restaurant;

        private IEnumerator EnterDevelopmentTest(string caption, int levelId)
        {
            Assert.That(Entry.Page, Is.EqualTo(ProductEntryPage.TestCatalog));
            Assert.That((Scene)Runtime.HostScene, Is.Null);
            Assert.That((Scene)Runtime.ClientScene, Is.Null);
            yield return ReachAndClick(EntryButton(caption));
            Assert.That(Entry.SelectedTestLevelId, Is.EqualTo(levelId));
            var previous = Runtime;
            yield return ReachAndClick(EntryButton("进入测试"));
            yield return Wait(() => Runtime != previous && Presentation?.Ready == true && Grid.Confirmed, "Selected temporary world is ready");
            ownedInit = Init.Instance;
            Assert.That(Runtime.Config.IsDevelopmentTest, Is.True);
            Assert.That(Grid.LevelConfigId, Is.EqualTo(levelId));
            Assert.That(Grid.LevelPhase, Is.EqualTo(LevelPhase.Preparing));
            Assert.That(Grid.SaveEnabled, Is.False);
            Assert.That(Runtime.ListenEndpoint, Is.Null);
            Hud.ApplyLocalFontSize(ProductFontSize.Largest);
            Assert.That(Hud.ScoreText.text, Does.Contain("测试"));
            Assert.That(Hud.GetComponentsInChildren<Button>(false).Select(button => button.GetComponentInChildren<Text>().text),
                Has.None.Contains("保存并继续").And.None.Contains("领取").And.None.Contains("存档与授权"));
        }

        private IEnumerator ReturnDevelopmentCatalog()
        {
            var previous = Runtime;
            yield return ReachAndClick(Button("返回测试列表"));
            yield return Wait(() => Runtime != previous && Entry?.Page == ProductEntryPage.TestCatalog && ((UIGridKitchen)Entry.Hud)?.Available == true,
                "Old world exits before the catalog reopens");
            ownedInit = Init.Instance;
            EntryHud.ApplyLocalFontSize(ProductFontSize.Largest);
            Assert.That(previous.IsDisposed, Is.True);
            Assert.That((Scene)Runtime.HostScene, Is.Null);
        }

        [UnityTest, Category("DevelopmentTestUi")]
        public IEnumerator DevelopmentTestUiOrdinaryLauncherRestartPendingWorkRetireCallbacksAndReenterIndependentMenu()
        {
            yield return StartModeLauncher();
            yield return ReachAndClick(EntryButton("测试关卡"));
            EntryHud.ApplyLocalFontSize(ProductFontSize.Largest);
            yield return EnterDevelopmentTest("章节 · " + Tables.Instance.DTRestaurantChapterConfig.Get(1).Name + " · 第 2 小关", 1102);
            Hud.ApplyLocalFontSize(ProductFontSize.Largest);
            yield return ReadableChooseBuff();
            yield return ReadableCommand("开始营业");
            yield return Wait(() => Grid.Orders.Values.Any(order => order.State == OrderState.Waiting), "A real customer places an order", 35);
            yield return ReadableCommand("拿 面包台 ·", true);
            yield return ReadableCommand("放 烤箱 ·", true);
            yield return ReadableCommand("烘烤 烤箱 ·", true);
            ((GridBakeProcess)((GridKitchenComponent)TestRestaurant.Kitchen).BakeProcesses.Values.Single()).RequiredTicks = 10000;
            yield return ReadableCommand("拿 碗台 ·", true);
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Not.Zero);
            Assert.That(Grid.Orders.Values.Any(order => order.State == OrderState.Waiting), Is.True);
            Assert.That(Grid.BakeProcesses.Count, Is.EqualTo(1));
            var previous = Runtime;
            string world = Grid.WorldId;
            var retired = CaptureButtonCallback(Button("重开当前测试"));
            yield return ReachAndClick(Button("重开当前测试"));
            retired();
            yield return Wait(() => Runtime != previous && Presentation?.Ready == true, "Restart creates one new ready world");
            ownedInit = Init.Instance;
            Assert.That(previous.IsDisposed, Is.True);
            Assert.That(Grid.WorldId, Is.Not.EqualTo(world));
            Assert.That(Grid.LevelConfigId, Is.EqualTo(1102));
            Assert.That(Grid.LevelPhase, Is.EqualTo(LevelPhase.Preparing));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Zero);
            Assert.That(Grid.BakeProcesses, Is.Empty);
            Assert.That(Grid.Orders, Is.Empty);
            Assert.That(Grid.LevelScore, Is.Zero);
            retired();
            yield return ReadableCommand("拿 碗台 ·", true);
            yield return ReturnDevelopmentCatalog();
            Assert.That(Entry.SelectedTestLevelId, Is.EqualTo(1102));
            yield return EnterDevelopmentTest("功能 · 咖啡制作", 1401);
            string source = Tables.Instance.DTSupplyConfig.Get(14).Name;
            yield return ReadableCommand("采购 " + source, true);
            yield return Wait(() => Grid.SupplyShipments.Values.Any(shipment => shipment.SourceId == 14 && shipment.Stage == ShipmentStage.Ready), "Finite beans arrive");
            yield return ReadableCommand("接收 " + source, true);
            yield return ReadableCommand("拆一份 咖啡豆", true);
            yield return ReadableCommand("放 磨豆机 ·", true);
            yield return ReadableCommand("拿 交接台 ·", true);
            yield return ReadableCommand("投垃圾 → 垃圾桶");
            yield return ReadableCommand("研磨 磨豆机 ·", true);
            yield return Wait(() => Grid.Items[Grid.Stations.Values.Single(station => station.Name == "磨豆机").ItemId].DefinitionId == 53,
                "Independent coffee menu really grinds finite beans");
            Assert.That(Grid.SupplySources[14].RemainingPackages, Is.EqualTo(4));
            var coffee = Runtime;
            yield return ReachAndClick(Button("返回主菜单"));
            yield return Wait(() => Runtime != coffee && Entry?.Page == ProductEntryPage.Home && ((UIGridKitchen)Entry.Hud)?.Available == true, "Return to normal home");
            ownedInit = Init.Instance;
            Assert.That(Runtime.Config.IsDevelopmentTest, Is.False);
            yield return ReachAndClick(EntryButton("测试关卡"));
            Assert.That(Entry.SelectedTestLevelId, Is.EqualTo(1401));
            yield return ReachAndClick(EntryButton("返回开始页"));
            CreateSaveUiRepository(Runtime, out _, out _);
            yield return ReachAndClick(EntryButton("开始新游戏"));
            yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Normal new game after tests");
            yield return ReachAndClick(SelectionButton("确认开始"));
            yield return Wait(() => Presentation?.Ready == true, "Normal chapter remains usable");
            Assert.That(Grid.LevelConfigId, Is.EqualTo(1101));
            Assert.That(Grid.SaveEnabled, Is.True);
            Assert.That(Runtime.Config.IsDevelopmentTest, Is.False);
            yield return ReadableCommand("拿 碗台 ·", true);
        }

        [UnityTest, Category("DevelopmentTestUi")]
        public IEnumerator DevelopmentTestUiExitDuringActualHudLoadAndInvalidInitializationReturnWithoutOrphanViews()
        {
            yield return StartModeLauncher();
            yield return ReachAndClick(EntryButton("测试关卡"));
            var retired = CaptureButtonCallback(EntryButton("进入测试"));
            var previous = Runtime;
            yield return ReachAndClick(EntryButton("进入测试"));
            yield return Wait(() => Runtime != previous && Presentation?.HudLoading == true && !Presentation.Ready, "Actual temporary HUD loading window");
            ownedInit = Init.Instance;
            var stopped = Runtime;
            var pending = Presentation;
            stopped.StopProduct();
            yield return null;
            yield return null;
            Assert.That(pending.IsDisposed, Is.True);
            Assert.That(UnityEngine.Object.FindObjectsByType<MonoUIGridKitchen>(UnityEngine.FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None), Is.Empty);
            retired();
            Assert.That((Scene)stopped.HostScene, Is.Null);
            Assert.That(ProductBootstrapBridge.SwitchInitialization(stopped.Config, RuntimeInstanceConfig.Create(System.Guid.NewGuid().ToString("N"), "invalid-test", "solo",
                testDestination: RuntimeTestDestination.Run, testLevelId: int.MaxValue)), Is.True);
            yield return Wait(() => Runtime != stopped && Entry?.Page == ProductEntryPage.TestCatalog && ((UIGridKitchen)Entry.Hud)?.Available == true,
                "Invalid target returns visibly to the catalog");
            ownedInit = Init.Instance;
            Assert.That(Entry.Failure, Does.Contain("UnknownDevelopmentTestLevel"));
            Assert.That((Scene)Runtime.HostScene, Is.Null);
            Assert.That((Scene)Runtime.ClientScene, Is.Null);
            yield return EnterDevelopmentTest("功能 · 基础操作", 101);
            yield return ReadableCommand("拿 碗台 ·", true);
        }

        private IEnumerator CompleteShortDevelopmentOpening()
        {
            // The owned runtime timer is shortened; production config and actual Start/drain/result paths are unchanged.
            Level level = TestRestaurant.CurrentLevel;
            level.OperatingTicks = 1;
            level.GetComponent<FrontLevelState>().NextArrivalOperatingTick = 100000;
            yield return ReadableChooseBuff();
            yield return ReadableCommand("开始营业");
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Real temporary settlement completes");
            Assert.That(Hud.ScoreText.text, Does.Contain("测试"));
            Assert.That(Grid.SaveEnabled, Is.False);
        }

        [UnityTest, Category("DevelopmentTestUi")]
        public IEnumerator DevelopmentTestUiSuccessContinueTerminalExitPreserveFormalRecordAndTicketThenRestoreOldSave()
        {
            yield return StartModeLauncher();
            var repository = CreateSaveUiRepository(Runtime, out string directory, out string profile);
            var header = SelectionHeader(profile);
            repository.Commit(header, SelectionFixture());
            string ticket = UiAuthorizedTicket(repository, header.SlotId);
            string[] files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).OrderBy(path => path).ToArray();
            byte[][] contents = files.Select(File.ReadAllBytes).ToArray();
            var claims = ProgressClaimFiles.Browse(repository);
            yield return ReachAndClick(EntryButton("测试关卡"));
            yield return EnterDevelopmentTest("功能 · 基础操作", 101);
            Runtime.Root().RemoveComponent<SoloSaveRepository>();
            repository = Runtime.Root().AddComponent<SoloSaveRepository, string, string>(directory, profile);
            Assert.Throws<System.InvalidOperationException>(() => ((Match)((HostSessionComponent)Runtime.Authority).Match).BindSoloSave(profile, null, null));
            Assert.Throws<System.InvalidOperationException>(() => repository.Commit(header, SelectionFixture()));
            var claimRequest = ProgressRepositoryRequest.Create();
            claimRequest.Operation = (int)ProgressRepositoryOperation.Claim;
            claimRequest.Id = ticket;
            var claim = repository.GetAsyncOwner().AsyncScope.RunAsync(context => context.CallLocalValueAsync(
                Runtime.Root().GetComponent<ProcessInnerSender>(), repository.GetActorId(), claimRequest,
                response => response.Error)).AsTask();
            yield return Wait(() => claim.IsCompleted, "Actual local claim request reaches the temporary-root rejection boundary");
            Assert.That(claim.GetAwaiter().GetResult(), Is.EqualTo(ProductError.InvalidRequest));
            var exportRequest = SaveAuthorizationRequest.Create();
            exportRequest.Action = (int)SaveAuthorizationAction.ExportImport;
            exportRequest.SlotId = header.SlotId;
            var export = repository.GetAsyncOwner().AsyncScope.RunAsync(context => context.CallLocalValueAsync(
                Runtime.Root().GetComponent<ProcessInnerSender>(), repository.GetActorId(), exportRequest,
                response => response.Error)).AsTask();
            yield return Wait(() => export.IsCompleted, "Actual local authorization request is rejected");
            Assert.That(export.GetAwaiter().GetResult(), Is.EqualTo(ProductError.InvalidRequest));
            yield return CompleteShortDevelopmentOpening();
            yield return Wait(() => Grid.RenovationChoiceVersion > 0, "Real temporary renovation offers are ready");
            long first = Grid.LevelId;
            yield return ReadableCommand("下一关");
            yield return Wait(() => Grid.LevelId != first && Grid.LevelPhase == LevelPhase.Preparing, "Temporary progression uses the real next-level rule");
            Assert.That(Grid.LevelConfigId, Is.EqualTo(102));
            Assert.That(Grid.SaveEnabled, Is.False);
            yield return ReturnDevelopmentCatalog();
            yield return EnterDevelopmentTest("章节 · " + Tables.Instance.DTRestaurantChapterConfig.Get(1).Name + " · 第 3 小关", 1103);
            yield return CompleteShortDevelopmentOpening();
            yield return ReadableCommand("完成测试并返回列表");
            yield return Wait(() => Entry?.Page == ProductEntryPage.TestCatalog && ((UIGridKitchen)Entry.Hud)?.Available == true, "Terminal completion returns to the test catalog");
            ownedInit = Init.Instance;
            Runtime.Root().RemoveComponent<SoloSaveRepository>();
            repository = Runtime.Root().AddComponent<SoloSaveRepository, string, string>(directory, profile);
            Assert.That(Directory.GetFiles(directory, "*", SearchOption.AllDirectories).OrderBy(path => path), Is.EqualTo(files));
            for (int index = 0; index < files.Length; ++index)
            {
                Assert.That(File.ReadAllBytes(files[index]), Is.EqualTo(contents[index]), Path.GetFileName(files[index]));
            }
            Assert.That(repository.Browse().Single().ResultId, Is.EqualTo(header.ResultId));
            Assert.That(ProgressClaimFiles.Browse(repository).Select(value => value.TicketId), Is.EqualTo(claims.Select(value => value.TicketId)));
            Assert.That(ProgressClaimFiles.Browse(repository).Single(value => value.TicketId == ticket).State,
                Is.EqualTo(claims.Single(value => value.TicketId == ticket).State));
            yield return ReachAndClick(EntryButton("返回开始页"));
            yield return ReachAndClick(EntryButton("继续游戏"));
            yield return Wait(() => Selection?.RecordState == SoloRecordReadState.Ready, "Normal record browser still sees the original file");
            yield return InspectAndContinueSave(header.SlotId);
            yield return Wait(() => Presentation?.Ready == true, "Normal old record restores after test success and completion");
            Assert.That(Grid.LevelConfigId, Is.EqualTo(header.NextLevelId));
            Assert.That(Grid.SaveEnabled, Is.True);
            Assert.That(Runtime.Config.IsDevelopmentTest, Is.False);
            Assert.That(Grid.Items.Count, Is.EqualTo(SelectionFixture().Items.Length));
            yield return ReadableCommand("拿 碗台 ·", true);
        }
    }
}
#endif
