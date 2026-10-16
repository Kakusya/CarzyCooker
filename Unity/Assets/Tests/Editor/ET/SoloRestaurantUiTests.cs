#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ET.Client;
using Game;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UiEventSystem=UnityEngine.EventSystems.EventSystem;
using GameEntry=Game.GameEntry;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private GameObject ownedRoot;
        private Init ownedInit;
        private readonly List<string> ownedSaveUiDirectories = new();
        private ProductRuntimeComponent Runtime=>ProductBootstrapBridge.Root as ProductRuntimeComponent;
        private ClientSessionComponent Client=>Runtime==null?null:(ClientSessionComponent)Runtime.Client;
        private GridClientComponent Grid=>Client?.GetComponent<GridClientComponent>();
        private GridPresentationComponent Presentation=>Client==null?null:((Scene)Client.IScene).GetComponent<GridPresentationComponent>();
        private MonoUIGridKitchen Hud=>((UIGridKitchen)Presentation.Hud).View;
        private MonoGridKitchenBoard Board=>((GridKitchenBoard)Presentation.Board).View;

        private IEnumerator Wait(Func<bool> condition,string reason,float seconds=12)
        {
            float started=Time.realtimeSinceStartup;
            while(!condition())
            {
                Assert.That(Time.realtimeSinceStartup-started,Is.LessThan(seconds),reason+"; "+Presentation?.Failure+
                    $"; phase={Grid?.LevelPhase}, tick={Grid?.Tick}, operating={Grid?.LevelElapsedTicks}, customers={Grid?.Customers.Count}, works={Grid?.FrontWorks.Count}; last={Grid?.LastCommandId}/{Grid?.LastStatus}/{Grid?.LastReason}" +
                    (Grid?.Actors.ContainsKey(Grid.MemberId) == true ? $"; actor={Grid.Actors[Grid.MemberId].Cell}/{Grid.Actors[Grid.MemberId].Motion}, goal={Grid.Actors[Grid.MemberId].Goal}" : ""));
                yield return null;
            }
        }
        private IEnumerator StartLauncher(bool waitReady=true, bool chooseGame=true, ProductMenuVariant menuVariant=0)
        {
            Assert.That(Init.Instance,Is.Null,"Do not replace another active ET entry");
            Assert.That(FiberManager.Instance,Is.Null,"Do not replace another active World");
            RuntimeInstanceContext.Reset();Application.runInBackground=true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Launcher.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return Wait(()=>GameEntry.RuntimeOwner is Component,"Owned GameEntry starts");
            ownedRoot=((Component)GameEntry.RuntimeOwner).transform.root.gameObject;
            yield return Wait(()=>Init.Instance!=null,"ET entry starts");ownedInit=Init.Instance;
            yield return Wait(() => Runtime?.Root().GetComponent<ProductEntryPresentation>() is ProductEntryPresentation entry &&
                ((UIGridKitchen)entry.Hud)?.Available == true, "Ordinary managed mode menu ready");
            var modeEntry = Runtime.Root().GetComponent<ProductEntryPresentation>();
            Pointer(((UIGridKitchen)modeEntry.Hud).View.GetComponentsInChildren<Button>(false).Single(value => value.GetComponentInChildren<Text>().text == "单机餐厅"));
            yield return Wait(() => Runtime?.Root().GetComponent<SoloSavePresentation>() is SoloSavePresentation selection &&
                ((UIGridKitchen)selection.Hud)?.Available == true, "Real managed save selector ready");
            if (!chooseGame) yield break;
            var selector = Runtime.Root().GetComponent<SoloSavePresentation>();
            // This existing suite exercises the retained two-food configuration; drink tests opt into the new menu.
            selector.NewMenuVariant = menuVariant;
            var menu = ((UIGridKitchen)selector.Hud).View;
            StartPreviewAndConfirm(menu.GetComponentsInChildren<Button>(false).Single(value => value.GetComponentInChildren<Text>().text == "内存试玩"));
            if(waitReady)yield return Wait(()=>Presentation?.Ready==true,"Real managed HUD ready");
        }
        private Button Button(string caption,bool prefix=false)
        {
            Button result = Hud.GetComponentsInChildren<Button>(false)
                .SingleOrDefault(value=>prefix?value.GetComponentInChildren<Text>().text.StartsWith(caption):value.GetComponentInChildren<Text>().text==caption);
            Assert.That(result, Is.Not.Null, "Missing actual button: " + caption + "; phase=" + Grid?.LevelPhase + "; group=" + Presentation?.ActionGroup);
            return result;
        }
        private void Pointer(Button button)
        {
            Assert.That(ExecuteEvents.Execute(button.gameObject,new PointerEventData(UiEventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler),Is.True);
        }
        private void StartPreviewAndConfirm(Button start)
        {
            MonoUIGridKitchen view = start.GetComponentInParent<MonoUIGridKitchen>();
            Pointer(start);
            Pointer(view.GetComponentsInChildren<Button>(false)
                .Single(button => button.GetComponentInChildren<Text>().text == "确认开始"));
        }
        private UnityAction CaptureButtonCallback(Button button)
        {
            var prepare=typeof(UnityEventBase).GetMethod("PrepareInvoke",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(prepare,Is.Not.Null);
            foreach(object call in (IEnumerable)prepare.Invoke(button.onClick,null))
            {
                var field=call.GetType().GetField("Delegate",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
                if(field?.GetValue(call) is UnityAction callback)return callback;
            }
            Assert.Fail("Actual runtime button callback missing");return null;
        }
        private IEnumerator Command(string caption,bool prefix=false)
        {
            long command=Client.NextCommandId+1;Pointer(Button(caption,prefix));
            if (caption.StartsWith("装修 "))
            {
                // 既有业务旅程也沿新的真实预览/确认入口，只产生一个原装修命令。
                Assert.That(Client.NextCommandId, Is.EqualTo(command - 1), "Opening renovation preview is read-only");
                Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Renovation));
                Pointer(Button("确认此装修"));
            }
            Assert.That(Client.NextCommandId,Is.EqualTo(command),"One pointer click sends one command");
            yield return Wait(()=>Grid.LastCommandId==command && Grid.LastStatus==GridResultStatus.Executed,"Actual command executed: " + caption + "; expected=" + command);
        }

        [UnityTest]
        [Category("AndroidSolo")]
        [Category("EntrySettlement")]
        [Category("EntryAdvice")]
        public IEnumerator SoloSaveUiOneClickCloseAndReopenKeepsResultOwnedCommit()
        {
            yield return StartLauncher();
            ProductRuntimeComponent runtime = Runtime;
            var repository = CreateSaveUiRepository(runtime, out string directory, out string profile);
            try
            {
                Match match = ((HostSessionComponent)runtime.Authority).Match; match.BindSoloSave(profile, null, null);
                GridKitchenComponent kitchen = match.ProductKitchen(); kitchen.Dirty = true;
                yield return Wait(() => Grid.SaveEnabled, "Save mode arrives through independent snapshot");
                string buff = Hud.GetComponentsInChildren<Button>(false).First(value => value.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
                yield return Command(buff); yield return Command("开始营业");
                yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Actual natural save result", 75);
                yield return Wait(() => Grid.RenovationLevelId == Grid.LevelId && Grid.RenovationChoiceVersion > 0, "Actual save renovation preparation ready");
                Assert.That(Hud.SettlementText.text, Does.Contain("尚未保存").And.Contain("先查看并确认一项装修"));
                long inspectCommand = Client.NextCommandId;
                Pointer(Button("结算详情"));
                Assert.That(Hud.DetailsText.text, Does.Contain("下一关菜单：").And.Contain("新增：").And.Contain("移出："));
                Assert.That(Client.NextCommandId, Is.EqualTo(inspectCommand));
                yield return Command("装修 新增台面");
                string actualPath = repository.DirectoryPath, blocker = System.IO.Path.Combine(actualPath, "blocked");
                System.IO.File.WriteAllText(blocker, "owned UI failure fixture"); repository.DirectoryPath = blocker;
                long failedLevel = Grid.LevelId; Pointer(Button("保存并继续"));
                yield return Wait(() => Grid.SaveState == SoloSaveState.Failed, "Actual save failure stays settled");
                Assert.That(Grid.LevelId, Is.EqualTo(failedLevel)); Assert.That(Grid.LevelPhase, Is.EqualTo(LevelPhase.Completed));
                Assert.That(Hud.SettlementText.text, Does.Contain("保存失败").And.Contain("原结算保留").And.Contain("重试“保存并继续”")); string resultId = Grid.SaveResultId;
                inspectCommand = Client.NextCommandId;
                Pointer(Button("查看处理说明"));
                Assert.That(Hud.DetailsText.text, Does.Contain("成功检查点尚未保存").And.Contain("明确选择重试"));
                Pointer(Button("查看保存状态"));
                Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.Settlement));
                Assert.That(Client.NextCommandId, Is.EqualTo(inspectCommand));
                Assert.That(Grid.SaveState, Is.EqualTo(SoloSaveState.Failed));
                Assert.That(System.IO.File.ReadAllText(blocker), Is.EqualTo("owned UI failure fixture"));
                repository.DirectoryPath = actualPath; Assert.That(repository.Browse(), Is.Empty);
                Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud(); yield return Wait(() => Presentation.Ready, "Failed save HUD reopen");
                Assert.That(Grid.SaveResultId, Is.EqualTo(resultId)); Assert.That(Hud.SettlementText.text, Does.Contain("保存失败"));
                inspectCommand = Client.NextCommandId;
                Pointer(Button("结算详情"));
                var oldDetails = CaptureButtonCallback(Button("关闭结算详情"));
                Pointer(Button("关闭结算详情"));
                Assert.That(Client.NextCommandId, Is.EqualTo(inspectCommand));
                long command = Client.NextCommandId, oldLevel = Grid.LevelId; int mono = Hud.GetInstanceID();
                Button save = Button("保存并继续"); Pointer(save); Pointer(save);
                Assert.That(Client.NextCommandId, Is.EqualTo(command + 1), "Repeated same-frame pointers issue one physical save command");
                Presentation.CloseGridHud();
                yield return Wait(() => Grid.LevelId != oldLevel && Grid.LevelPhase == LevelPhase.Preparing, "Closing display owner keeps Match-owned save and Continue", 12);
                Assert.That(repository.Browse().Length, Is.EqualTo(1)); Assert.That(Grid.Items.Count, Is.EqualTo(7)); Assert.That(Grid.Stations.Count, Is.EqualTo(17));
                Presentation.OpenGridHud(); yield return Wait(() => Presentation.Ready, "Managed saved HUD reopen");
                Assert.That(Hud.GetInstanceID(), Is.EqualTo(mono)); Assert.That(Grid.LastSavedSlotId, Is.EqualTo(repository.Browse().Single().SlotId));
                Assert.That(Hud.HintText.text, Does.Contain("检查点"));
                command = Client.NextCommandId; oldDetails();
                Assert.That(Client.NextCommandId, Is.EqualTo(command));
                Assert.That(Presentation.OverviewPage, Is.EqualTo(GridOverviewPage.None));
            }
            finally
            {
                runtime.StopProduct(); repository.Dispose(); RemoveOwnSaveUiFiles(directory);
            }
        }

        private static void RemoveOwnSaveUiFiles(string directory)
        {
            string parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Temp/solo-save-ui-tests"));
            Assert.That(System.IO.Path.GetDirectoryName(directory), Is.EqualTo(parent));
            if (!System.IO.Directory.Exists(directory))
            {
                return;
            }
            foreach (string file in System.IO.Directory.EnumerateFiles(directory, "*", System.IO.SearchOption.AllDirectories)) System.IO.File.Delete(file);
            foreach (string child in System.IO.Directory.EnumerateDirectories(directory, "*", System.IO.SearchOption.AllDirectories).OrderByDescending(s => s.Length)) System.IO.Directory.Delete(child);
            System.IO.Directory.Delete(directory);
        }

        private SoloSaveRepository CreateSaveUiRepository(ProductRuntimeComponent runtime, out string directory, out string profile)
        {
            Assert.That(runtime, Is.SameAs(Runtime), "Only replace this Launcher's owned repository");
            directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Temp/solo-save-ui-tests", Guid.NewGuid().ToString("N")));
            profile = Guid.NewGuid().ToString("N");
            // 先记录测试专属目录，初始化失败时也由本夹具收尾，不触碰产品存档。
            ownedSaveUiDirectories.Add(directory);
            runtime.Root().GetComponent<SoloSavePresentation>()?.CloseSoloSelection();
            runtime.Root().RemoveComponent<SoloSaveRepository>();
            return runtime.Root().AddComponent<SoloSaveRepository, string, string>(directory, profile);
        }

        [UnityTearDown]
        public IEnumerator CleanupOwnedLauncher()
        {
            try
            {
                foreach (CoopUiRemote remote in ownedCoopUiRemotes) { remote.Dispose(); }
                ownedCoopUiRemotes.Clear();
                if(ownedRoot!=null)
                {
                    if(ownedInit!=null && Init.Instance==ownedInit)ownedInit.Shutdown();
                    if(UnityGameFramework.Runtime.GameEntry.IsInitialized)UnityGameFramework.Runtime.GameEntry.Shutdown(UnityGameFramework.Runtime.ShutdownType.None);
                    if(ownedRoot!=null)UnityEngine.Object.Destroy(ownedRoot);
                    yield return null;
                    RuntimeInstanceContext.Reset();ownedRoot=null;ownedInit=null;
                }
            }
            finally
            {
                foreach (string directory in ownedSaveUiDirectories)
                {
                    RemoveOwnSaveUiFiles(directory);
                }
                ownedSaveUiDirectories.Clear();
            }
        }

        [Test]
        public void SoloRestaurantUiBindingsRealPrefabHasGeneratedFieldsAndOriginalCanvas()
        {
            GameObject asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab");
            Assert.That(asset,Is.Not.Null);var view=asset.GetComponent<MonoUIGridKitchen>();Assert.That(view,Is.Not.Null);
            foreach(Text text in new[]{view.PhaseText,view.ClockText,view.ScoreText,view.HandText,view.WorkText,view.StatusText,view.HintText,view.OrdersText,view.BuffText,view.SettlementText,view.TargetText,view.DetailsText})
                Assert.That(text,Is.Not.Null,"Generated Text binding");
            foreach(RectTransform rect in new[]{view.OrderPanelRectTransform,view.BuffPanelRectTransform,view.SettlementPanelRectTransform,view.ControlPanelRectTransform,view.CameraDragAreaRectTransform,view.OrderActionsRectTransform,view.BuffActionsRectTransform,view.TargetsRectTransform,view.PrimaryActionsRectTransform,view.DetailsPanelRectTransform})
                Assert.That(rect,Is.Not.Null,"Generated panel binding");
            Assert.That(asset.GetComponent<Canvas>().renderMode,Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(asset.GetComponent<GraphicRaycaster>(),Is.Not.Null);Assert.That(view.TargetTemplateButton.gameObject.activeSelf,Is.False);
            Assert.That(view.CameraDragAreaRectTransform.GetComponent<Image>().raycastTarget,Is.False);
            Assert.That(view.TargetsRectTransform.GetComponentInParent<ScrollRect>(true).content,Is.SameAs(view.TargetsRectTransform));
            Assert.That(view.DetailsPanelRectTransform.GetComponent<ScrollRect>().content,Is.SameAs(view.DetailsText.rectTransform));
            Assert.That(view.DetailsPanelRectTransform.gameObject.activeSelf,Is.False);
            Assert.That(view.TargetText.raycastTarget,Is.False);
        }

        [UnityTest]
        public IEnumerator SoloRenovationUiRealZeroStarChoicesReopenOnceInstallAndOldCallbacks()
        {
            yield return StartLauncher();
            string buff = Hud.GetComponentsInChildren<Button>(false).First(value => value.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(buff); yield return Command("开始营业");
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Actual zero-star renovation result", 75);
            yield return Wait(() => Grid.RenovationLevelId == Grid.LevelId && Grid.RenovationChoiceVersion > 0, "Actual renovation preparation delivered");
            Assert.That(Grid.LevelStars, Is.Zero); Assert.That(Grid.RenovationCandidates, Is.EqualTo(new[] { 1, 2, 3, 4 }));
            long version = Grid.RenovationChoiceVersion; int[] candidates = Grid.RenovationCandidates.ToArray();
            UnityAction retired = CaptureButtonCallback(Button("装修 新增台面")); int mono = Hud.GetInstanceID();
            long before = Client.NextCommandId; Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Managed renovation HUD reopen");
            Assert.That(Hud.GetInstanceID(), Is.EqualTo(mono)); retired(); Assert.That(Client.NextCommandId, Is.EqualTo(before));
            Assert.That(Grid.RenovationChoiceVersion, Is.EqualTo(version)); Assert.That(Grid.RenovationCandidates, Is.EqualTo(candidates));
            UnityAction otherChoice = CaptureButtonCallback(Button("装修 新增切配台"));
            yield return Command("装修 新增台面");
            Assert.That(Grid.SelectedRenovationId, Is.EqualTo(1)); Assert.That(Grid.Stations.Count, Is.EqualTo(16));
            Assert.That(Hud.SettlementText.text, Does.Contain("已选装修：新增台面"));
            before = Client.NextCommandId; otherChoice(); Assert.That(Client.NextCommandId, Is.EqualTo(before), "Retired choice cannot submit another direction");
            long oldLevel = Grid.LevelId; long[] items = Grid.Items.Keys.OrderBy(value => value).ToArray();
            yield return Command("下一关"); Assert.That(Grid.LevelId, Is.Not.EqualTo(oldLevel));
            long station = Grid.RenovationApplied[1].Single(); Assert.That(Grid.Stations[station].Name, Is.EqualTo("新增台面"));
            Assert.That(Grid.Stations[station].ItemId, Is.Zero); Assert.That(Grid.Stations.Count, Is.EqualTo(17));
            Assert.That(Grid.Items.Keys.OrderBy(value => value), Is.EqualTo(items));
            before = Client.NextCommandId; otherChoice(); retired(); yield return null;
            Assert.That(Client.NextCommandId, Is.EqualTo(before)); Assert.That(Hud.HintText.text, Does.Contain("已装修：新增台面"));
        }

        private IEnumerator RenovationUiMoveStation(string name, GridCell cell)
        {
            long before = Client.NextCommandId; Pointer(Button("调整 " + name));
            Assert.That(Client.NextCommandId, Is.EqualTo(before), "Selecting a station sends no business command");
            Vector2 point = Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(cell));
            Assert.That(Board.InputScreenRect.Contains(point), Is.True);
            Presentation.ProcessGridPointer(point, true, true, false); Presentation.ProcessGridPointer(point, false, false, true);
            long command = before + 1; Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Executed, "Actual map layout movement");
            Pointer(Button("结束调整"));
        }

        [UnityTest]
        public IEnumerator SoloRenovationUiActualBlockedAdditionsKeepUpgradeFeedbackAndCanContinue()
        {
            yield return StartLauncher();
            yield return RenovationUiMoveStation("切配台", new GridCell(2, 13));
            yield return RenovationUiMoveStation("交接台", new GridCell(4, 12));
            string buff = Hud.GetComponentsInChildren<Button>(false).First(value => value.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(buff); yield return Command("开始营业");
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Actual empty renovation result", 75);
            yield return Wait(() => Grid.RenovationLevelId == Grid.LevelId && Grid.RenovationChoiceVersion > 0, "Actual empty candidates prepared");
            Assert.That(Grid.RenovationCandidates, Is.EqualTo(new[] { 3, 4 })); Assert.That(Hud.SettlementText.text, Does.Contain("可选择一项装修"));
            Assert.That(Hud.GetComponentsInChildren<Button>(false).Count(value => value.GetComponentInChildren<Text>().text.StartsWith("装修 升级")), Is.EqualTo(2));
            long[] items = Grid.Items.Keys.OrderBy(value => value).ToArray(); yield return Command("下一关");
            Assert.That(Grid.Stations.Count, Is.EqualTo(16)); Assert.That(Grid.RenovationApplied, Is.Empty);
            Assert.That(Grid.Items.Keys.OrderBy(value => value), Is.EqualTo(items));
        }

        [UnityTest]
        public IEnumerator SoloSupplyUiActualPurchaseReceiptPortionsAndEmptyShellWithManagedReopen()
        {
            yield return StartLauncher(); int source = Tables.Instance.DTSupplyConfig.DataList.Single(value => value.MaterialDefinitionId == GridKitchenRules.Tomato).Id;
            UnityAction oldPurchase = CaptureButtonCallback(Button("采购 番茄", true));
            yield return Command("采购 番茄", true);
            Assert.That(Grid.SupplySources[source].RemainingPackages, Is.EqualTo(4)); Assert.That(Grid.SupplyShipments.Count, Is.EqualTo(1));
            long shipment = Grid.SupplyShipments.Values.Single().Id;
            Assert.That(Grid.SupplyShipments[shipment].Stage, Is.EqualTo(ShipmentStage.InTransit));
            Assert.That(Hud.HintText.text, Does.Contain("运输"));
            int mono = Hud.GetInstanceID(); long before = Client.NextCommandId;
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Managed supply HUD reopen");
            oldPurchase(); Assert.That(Client.NextCommandId, Is.EqualTo(before)); Assert.That(Grid.SupplyShipments.Count, Is.EqualTo(1));
            Assert.That(Hud.GetInstanceID(), Is.EqualTo(mono));
            yield return Wait(() => Grid.SupplyShipments[shipment].Stage == ShipmentStage.Ready, "Actual shipment arrives on business ticks");
            Assert.That(Hud.HintText.text, Does.Contain("待收货"));
            yield return Command("接收 番茄", true); long package = Grid.SupplyShipments[shipment].PackageItemId;
            long[] contents = Grid.Items[package].Contents.ToArray(); Assert.That(contents.Length, Is.EqualTo(2));
            yield return Command("拆一份 番茄", true);
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(contents[0])); Assert.That(Grid.Items[package].Contents, Is.EqualTo(new[] { contents[1] }));
            yield return Command("放 烤箱 ·", true); yield return Command("拆一份 番茄", true);
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(contents[1])); yield return Command("放 切配台 ·", true);
            yield return Command("拿 交接台 ·", true); Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(package));
            Assert.That(Grid.Items[package].Contents, Is.Empty); yield return Command("投垃圾 → 垃圾桶");
            Assert.That(Grid.Items.ContainsKey(package), Is.False);
            Assert.That(Grid.Items.ContainsKey(contents[0]) && Grid.Items.ContainsKey(contents[1]), Is.True);
            Assert.That(Grid.SupplySources[source].RemainingPackages, Is.EqualTo(4)); Assert.That(Grid.SupplyShipments.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SoloSupplyUiOldLevelPurchaseCallbackCannotWriteNextKitchen()
        {
            yield return StartLauncher(); UnityAction oldPurchase = CaptureButtonCallback(Button("采购 鸡蛋", true));
            string candidate = Hud.GetComponentsInChildren<Button>(false).First(value => value.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(candidate); yield return Command("开始营业");
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Actual natural completion before supply callback check", 75);
            long oldLevel = Grid.LevelId; yield return Command("下一关"); long command = Client.NextCommandId;
            Assert.That(Grid.LevelId, Is.Not.EqualTo(oldLevel)); oldPurchase(); yield return null;
            Assert.That(Client.NextCommandId, Is.EqualTo(command)); Assert.That(Grid.SupplyShipments, Is.Empty);
            Assert.That(Grid.SupplySources.Values.All(value => value.RemainingPackages == 5), Is.True);
        }

        [UnityTest]
        public IEnumerator SoloCleanupUiActualSinkButtonsApproachWrongTrashAndManagedReopen()
        {
            yield return StartLauncher(); yield return Command("拿 碗台 ·", true);
            long bowl = Grid.Actors[Client.MemberId].HandItemId;
            var sink = Grid.Stations.Values.Single(value => value.Kind == GridStationKind.Wash);
            var trash = Grid.Stations.Values.Single(value => value.Kind == GridStationKind.Trash);
            GridCell far = new(1, 16); Vector2 point = Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(far));
            Assert.That(Board.InputScreenRect.Contains(point), Is.True);
            long command = Client.NextCommandId + 1;
            Presentation.ProcessGridPointer(point, true, true, false); Presentation.ProcessGridPointer(point, false, false, true);
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Executed, "Real map movement to far cell");
            Button fill = Button("接水 → " + sink.Name); UnityAction oldFill = CaptureButtonCallback(fill);
            command = Client.NextCommandId + 1; Pointer(fill);
            Assert.That(Client.NextCommandId, Is.EqualTo(command)); Assert.That(Grid.LastStatus, Is.EqualTo(GridResultStatus.Accepted));
            Assert.That(Grid.Items[bowl].Contents, Is.Empty, "Receipt cannot pretend the water is already filled");
            yield return null; Assert.That(Grid.Items[bowl].Contents, Is.Empty, "The distant character must actually approach");
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Executed, "Real sink arrival and water fill");
            long water = Grid.Items[bowl].Contents.Single();
            Assert.That(sink.Approaches.Contains(Grid.Actors[Client.MemberId].Cell), Is.True);
            Assert.That(Hud.HandText.text, Does.Contain("水"));
            point = Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(trash.Cell));
            Assert.That(Board.InputScreenRect.Contains(point), Is.True);
            command = Client.NextCommandId + 1;
            Presentation.ProcessGridPointer(point, true, true, false); Presentation.ProcessGridPointer(point, false, false, true);
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Rejected, "Real wrong-facility rejection");
            Assert.That(Grid.LastReason, Is.EqualTo("RequiresSolid")); Assert.That(Hud.StatusText.text, Does.Contain("垃圾桶只能接收固体"));
            Assert.That(Grid.Items[bowl].Contents, Is.EqualTo(new[] { water }));
            yield return Command("倒液 → " + sink.Name);
            Assert.That(Grid.Items.ContainsKey(water), Is.False); Assert.That(Grid.Items[bowl].Contents, Is.Empty);
            int pooledView = Hud.GetInstanceID(); long before = Client.NextCommandId;
            Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Actual managed HUD reopen");
            oldFill(); Assert.That(Client.NextCommandId, Is.EqualTo(before), "Retired owner callback cannot refill the same pooled HUD");
            Assert.That(Hud.GetInstanceID(), Is.EqualTo(pooledView));
            yield return Command("接水 → " + sink.Name);
            Assert.That(Grid.Items[bowl].Contents.Single(), Is.Not.EqualTo(water));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.EqualTo(bowl));
            var header = Hud.PhaseText.rectTransform.parent as RectTransform; var canvas = Hud.GetComponent<Canvas>().rootCanvas;
            point = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, header.TransformPoint(header.rect.center));
            before = Client.NextCommandId;
            Presentation.ProcessGridPointer(point, true, true, false); Presentation.ProcessGridPointer(point, false, false, true);
            Assert.That(Client.NextCommandId, Is.EqualTo(before), "HUD pointer cannot leak into a map command");
        }

        [UnityTest]
        [NUnit.Framework.Category("PerFrameIntegration")]
        [Category("RestaurantArtRegression")]
        [Category("ProductSemanticEnums")]
        [Category("AndroidSolo")]
        public IEnumerator SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen()
        {
            yield return StartLauncher();GridPresentationComponent presentation=Presentation;
            UIGridKitchen original=presentation.Hud;Assert.That(original.Parent,Is.SameAs(((Scene)Client.IScene).GetComponent<UIComponent>()));
            Assert.That(Hud.UGFUIForm,Is.SameAs(original));Assert.That(Hud.PhaseText.text,Is.EqualTo("准备 · 第 1/3 关"));Assert.That(Hud.ScoreText.text,Does.Contain("0 分"));
            foreach(GridCell corner in new[]{new GridCell(0,Grid.KitchenBottom),new GridCell(Grid.Width-1,Grid.KitchenBottom),new GridCell(0,Grid.Height-1),new GridCell(Grid.Width-1,Grid.Height-1)})
                Assert.That(Board.InputScreenRect.Contains(Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(corner))),Is.True,"Kitchen fits the actual unobstructed map region");
            int seed=Grid.BuffSeed;int[] candidates=Grid.BuffCandidates.ToArray();long command=Client.NextCommandId;
            Pointer(Button("隐藏候选加成"));yield return null;Assert.That(Hud.BuffActionsRectTransform.GetComponentsInChildren<Button>(false),Is.Empty);
            Pointer(Button("查看候选加成"));yield return null;
            Assert.That(Grid.BuffSeed,Is.EqualTo(seed));Assert.That(Grid.BuffCandidates,Is.EqualTo(candidates));Assert.That(Client.NextCommandId,Is.EqualTo(command));

            Vector2 point=Board.InputScreenRect.center;Vector3 before=Board.BoardCamera.transform.position;
            presentation.ProcessGridPointer(point,true,true,false);presentation.ProcessGridPointer(point+new Vector2(0,300),false,true,false);presentation.ProcessGridPointer(point+new Vector2(0,300),false,false,true);
            Assert.That(Client.NextCommandId,Is.EqualTo(command),"Dragging sends no action");Assert.That(Board.BoardCamera.transform.position.y,Is.Not.EqualTo(before.y));
            var header=Hud.PhaseText.rectTransform.parent as RectTransform;Canvas canvas=Hud.GetComponent<Canvas>().rootCanvas;
            Vector2 blocked=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,header.TransformPoint(header.rect.center));
            presentation.ProcessGridPointer(blocked,true,true,false);presentation.ProcessGridPointer(blocked,false,false,true);
            Assert.That(Client.NextCommandId,Is.EqualTo(command),"HUD pointer cannot pass through");Assert.That(presentation.PointerConsumed,Is.True);
            GridCell destination=new(2,6);Vector3 screen=Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(destination));
            Assert.That(Board.InputScreenRect.Contains(screen),Is.True);Assert.That(Board.ScreenToCell(screen),Is.EqualTo(destination),"Current camera coordinates after drag");
            presentation.ProcessGridPointer(screen,true,true,false);presentation.ProcessGridPointer(screen,false,false,true);
            Assert.That(Client.NextCommandId,Is.EqualTo(++command));yield return Wait(()=>Grid.LastStatus==GridResultStatus.Executed && Grid.Actors[Client.MemberId].Cell==destination,"Real board move reaches current target");

            Pointer(Button("拿 碗台 ·",true));Assert.That(Client.NextCommandId,Is.EqualTo(++command));
            Assert.That(Grid.LastStatus,Is.EqualTo(GridResultStatus.Accepted));Assert.That(Grid.Actors[Client.MemberId].HandItemId,Is.Zero,"Receipt is not a pickup");
            Assert.That(Hud.HandText.text,Does.Contain("空手"));yield return Wait(()=>Grid.LastStatus==GridResultStatus.Executed && Grid.Actors[Client.MemberId].HandItemId!=0,"Actual held bowl");
            long bowl=Grid.Actors[Client.MemberId].HandItemId;Assert.That(Hud.HandText.text,Does.Contain("碗（空）"));
            int pooledView=Hud.GetInstanceID();
            for(int cycle=0;cycle<2;++cycle)
            {
                Button retired=Button(cycle==0?"放 交接台 ·":"拿 交接台 ·",true);UnityAction retiredCallback=CaptureButtonCallback(retired);UIGridKitchen closing=presentation.Hud;
                presentation.CloseGridHud();retired.onClick.Invoke();Assert.That(Client.NextCommandId,Is.EqualTo(command),"Closed subscription cannot send");
                Assert.That(closing.IsDisposed,Is.True);Assert.That(presentation.Ready,Is.False);
                yield return null;presentation.OpenGridHud();yield return Wait(()=>presentation.Ready,"Managed reopen");
                retiredCallback();Assert.That(Client.NextCommandId,Is.EqualTo(command),"Retired owner callback cannot act on the reopened form");
                Assert.That(Hud.GetInstanceID(),Is.EqualTo(pooledView),"GF reused the original pooled Mono");
                Assert.That(Grid.BuffSeed,Is.EqualTo(seed));Assert.That(Grid.BuffCandidates,Is.EqualTo(candidates),"Managed reopen does not redraw");
                yield return Command(cycle==0?"放 交接台 ·":"拿 交接台 ·",true);++command;
            }
            Assert.That(Grid.Actors[Client.MemberId].HandItemId,Is.EqualTo(bowl));Assert.That(Client.NextCommandId,Is.EqualTo(command));
        }

        [UnityTest]
        public IEnumerator SoloRestaurantUiProjectionRealOrderRejectionPauseAndOldLevelButton()
        {
            yield return StartLauncher();yield return Command("拿 碗台 ·",true);long bowl=Grid.Actors[Client.MemberId].HandItemId;
            string candidate=Hud.GetComponentsInChildren<Button>(false).First(value=>value.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(candidate);yield return Command("开始营业");
            yield return Wait(()=>Grid.FrontWorks.Values.Any(value=>value.Kind==FrontWorkKind.Ask && value.State==FrontWorkState.Working && value.Progress>0),"Actual partner visible work");
            Assert.That(Hud.WorkText.text,Does.Contain("询问"));Assert.That(Hud.BuffText.text,Does.Contain(Tables.Instance.DTBuffConfig.Get(Grid.SelectedBuffId).Name));
            yield return Command("暂停");long tick=Grid.Tick;string clock=Hud.ClockText.text,work=Hud.WorkText.text;
            yield return new WaitForSecondsRealtime(.25f);Assert.That(Grid.Tick,Is.EqualTo(tick));Assert.That(Hud.ClockText.text,Is.EqualTo(clock));Assert.That(Hud.WorkText.text,Is.EqualTo(work));
            Assert.That(Hud.PhaseText.text,Does.Contain("暂停"));yield return Command("恢复");
            yield return Wait(()=>Grid.Orders.Values.Any(value=>value.State==OrderState.Waiting && value.DishId==1),"Actual soup order");
            GridOrderProjection order=Grid.Orders.Values.First(value=>value.State==OrderState.Waiting && value.DishId==1);string table=Grid.Stations[order.TableStationId].Name;
            Assert.That(Hud.OrdersText.text,Does.Contain(table));Assert.That(Hud.OrdersText.text,Does.Contain("番茄蛋花汤"));
            yield return Command("前往 "+table);Button serve=Button("交付 番茄蛋花汤 → "+table);UnityAction oldCallback=CaptureButtonCallback(serve);
            long command=Client.NextCommandId+1;Pointer(serve);Assert.That(Client.NextCommandId,Is.EqualTo(command));
            Assert.That(Grid.LastStatus,Is.EqualTo(GridResultStatus.Accepted));Assert.That(Grid.Actors[Client.MemberId].HandItemId,Is.EqualTo(bowl));
            yield return Wait(()=>Grid.LastCommandId==command && Grid.LastStatus==GridResultStatus.Rejected,"Empty bowl rejected");
            Assert.That(Grid.LastReason,Is.EqualTo("WrongDish"));Assert.That(Hud.StatusText.text,Does.Contain("手中菜品与订单不符"));
            Assert.That(Grid.Actors[Client.MemberId].HandItemId,Is.EqualTo(bowl));Assert.That(Grid.LevelScore,Is.Zero);Assert.That(Grid.Orders[order.Id].State,Is.EqualTo(OrderState.Waiting));
            yield return Wait(()=>Grid.LevelPhase==LevelPhase.Completed,"Actual zero-star natural front drain",45);
            yield return Wait(()=>Grid.RenovationLevelId==Grid.LevelId && Grid.RenovationChoiceVersion>0,"Actual next-level control ready");
            Assert.That(Hud.SettlementPanelRectTransform.gameObject.activeSelf,Is.True);Assert.That(Hud.SettlementText.text,Does.Contain("0 分 · 0 星"));
            long oldLevel=Grid.LevelId;yield return Command("下一关");long nextCommand=Client.NextCommandId;
            Assert.That(Grid.LevelId,Is.Not.EqualTo(oldLevel));Assert.That(Hud.PhaseText.text,Is.EqualTo("准备"));Assert.That(Grid.SelectedBuffId,Is.Zero);
            oldCallback();yield return null;
            Assert.That(Client.NextCommandId,Is.EqualTo(nextCommand),"Old order button callback cannot submit to the new level");Assert.That(Grid.LevelScore,Is.Zero);Assert.That(Grid.Orders,Is.Empty);
            Assert.That(Hud.OrdersText.text,Is.EqualTo("暂无待交付订单"));Assert.That(Hud.SettlementPanelRectTransform.gameObject.activeSelf,Is.False);
        }

        [UnityTest]
        [Category("RestaurantArtRegression")]
        public IEnumerator SoloRestaurantUiInformationHierarchyTargetDetailsAndPrimaryActionUseCurrentProjection()
        {
            yield return StartLauncher();
            yield return Command("拿 碗台 ·",true);
            long bowl=Grid.Actors[Client.MemberId].HandItemId;
            long station=Grid.Stations.Values.Single(value=>value.Name=="碗台").Id;
            Assert.That(Presentation.SelectedStationId,Is.EqualTo(station));
            Assert.That(Hud.HandText.text,Does.Contain("碗（空）"));
            Assert.That(Hud.TargetText.text,Does.Contain("碗台").And.Contain("空"));
            Button place=Button("放 碗台 ·",true);
            Assert.That(place.transform.parent,Is.SameAs(Hud.PrimaryActionsRectTransform));
            long command=Client.NextCommandId;
            Pointer(Button("查看详情"));yield return null;
            Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf,Is.True);
            Assert.That(Hud.DetailsText.text,Does.Contain("内容：空"));
            Assert.That(Client.NextCommandId,Is.EqualTo(command));
            var oldClose=CaptureButtonCallback(Button("收起详情"));
            int pooled=Hud.GetInstanceID();
            Presentation.CloseGridHud();yield return null;Presentation.OpenGridHud();
            yield return Wait(()=>Presentation.Ready,"Reopen actual selected-target HUD");
            Assert.That(Hud.GetInstanceID(),Is.EqualTo(pooled));
            oldClose();yield return null;
            Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf,Is.True,"Retired callback cannot close new owner details");
            Assert.That(Client.NextCommandId,Is.EqualTo(command));
            Pointer(Button("收起详情"));yield return null;
            yield return Command("放 碗台 ·",true);
            Assert.That(Grid.Actors[Client.MemberId].HandItemId,Is.Zero);
            Assert.That(Grid.Stations[station].ItemId,Is.EqualTo(bowl));
            Assert.That(Hud.TargetText.text,Does.Contain("碗（空）"));
            Assert.That(Client.NextCommandId,Is.EqualTo(command+1));
        }

        [UnityTest]
        [NUnit.Framework.Category("PerFrameIntegration")]
        [Category("RestaurantArtRegression")]
        public IEnumerator SoloRestaurantUiProjectionActualBakingSeparatesReceiptProgressAndCompletion()
        {
            yield return StartLauncher();yield return Command("拿 面包台 ·",true);yield return Command("放 烤箱 ·",true);
            long command=Client.NextCommandId+1;Pointer(Button("烘烤 烤箱 ·",true));
            Assert.That(Client.NextCommandId,Is.EqualTo(command));Assert.That(Grid.LastStatus,Is.EqualTo(GridResultStatus.Accepted));
            Assert.That(Grid.BakeProcesses,Is.Empty,"Receipt has not created a completed bake");
            yield return Wait(()=>Grid.BakeProcesses.Values.Any(value=>value.State==GridBakeState.Baking),"Actual bake started");
            yield return Wait(()=>Hud.WorkText.text.Contains("烤制已开始"),"Explicit work-start message displayed");
            long process=Grid.BakeProcesses.Values.Single().Id;Assert.That(Hud.HintText.text,Does.Contain("烘烤"));
            Assert.That(Hud.TargetText.text,Does.Contain("烤箱"));
            Assert.That(Hud.PrimaryActionsRectTransform.GetComponentsInChildren<Button>(false).Length,Is.GreaterThan(0),"Selected station action remains reachable");
            long beforeDetails=Client.NextCommandId;
            Pointer(Button("查看详情"));yield return null;
            Assert.That(Client.NextCommandId,Is.EqualTo(beforeDetails),"Reading details cannot send a domain command");
            Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf,Is.True);
            Assert.That(Hud.DetailsText.text,Does.Contain("烤箱").Or.Contain("烘烤"));
            Assert.That(Hud.DetailsText.text,Does.Contain("面包片"));
            Pointer(Button("收起详情"));yield return null;
            Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf,Is.False);
            Assert.That(Client.NextCommandId,Is.EqualTo(beforeDetails));
            Assert.That(Grid.BakeProcesses[process].ProgressTicks,Is.LessThan(Grid.BakeProcesses[process].RequiredTicks));
            Assert.That(Grid.Items.Values.Any(value=>value.DefinitionId==GridKitchenRules.Toast),Is.False);
            yield return Command("暂停");int progress=Grid.BakeProcesses[process].ProgressTicks;string hint=Hud.HintText.text;
            yield return new WaitForSecondsRealtime(.25f);Assert.That(Grid.BakeProcesses[process].ProgressTicks,Is.EqualTo(progress));Assert.That(Hud.HintText.text,Is.EqualTo(hint));
            yield return Command("恢复");yield return Wait(()=>Grid.BakeProcesses[process].State==GridBakeState.Completed,"Actual bake completes");
            yield return Wait(()=>Hud.WorkText.text.Contains("烤制已完成"),"Explicit work-finish message displayed");
            Assert.That(Hud.HintText.text,Does.Contain("完成"));Assert.That(Grid.Items.Values.Count(value=>value.DefinitionId==GridKitchenRules.Toast),Is.EqualTo(1));
            Assert.That(Grid.LevelScore,Is.Zero,"Processing completion is not order scoring");
        }

        [UnityTest]
        public IEnumerator SoloRestaurantUiLifecycleExitDuringActualInitialLoadDoesNotShowOrphanHud()
        {
            yield return StartLauncher(false);yield return Wait(()=>Presentation?.HudLoading==true && !Presentation.Ready,"Actual UI loading window");
            GridPresentationComponent pending=Presentation;Runtime.StopProduct();
            yield return null;yield return null;yield return null;
            Assert.That(pending.IsDisposed,Is.True);Assert.That(Client,Is.Null);
            Assert.That(UnityEngine.Object.FindObjectsByType<MonoUIGridKitchen>(FindObjectsInactive.Exclude,FindObjectsSortMode.None),Is.Empty,"No orphan HUD after owner exits");
        }
    }
}
#endif
