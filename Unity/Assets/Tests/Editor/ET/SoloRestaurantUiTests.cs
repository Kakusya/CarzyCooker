#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
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
    public sealed class SoloRestaurantUiTests
    {
        private GameObject ownedRoot;
        private Init ownedInit;
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
                Assert.That(Time.realtimeSinceStartup-started,Is.LessThan(seconds),reason+"; "+Presentation?.Failure);
                yield return null;
            }
        }
        private IEnumerator StartLauncher(bool waitReady=true)
        {
            Assert.That(Init.Instance,Is.Null,"Do not replace another active ET entry");
            Assert.That(FiberManager.Instance,Is.Null,"Do not replace another active World");
            RuntimeInstanceContext.Reset();Application.runInBackground=true;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Launcher.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return Wait(()=>GameEntry.RuntimeOwner is Component,"Owned GameEntry starts");
            ownedRoot=((Component)GameEntry.RuntimeOwner).transform.root.gameObject;
            yield return Wait(()=>Init.Instance!=null,"ET entry starts");ownedInit=Init.Instance;
            if(waitReady)yield return Wait(()=>Presentation?.Ready==true,"Real managed HUD ready");
        }
        private Button Button(string caption,bool prefix=false)=>Hud.GetComponentsInChildren<Button>(false)
            .Single(value=>prefix?value.GetComponentInChildren<Text>().text.StartsWith(caption):value.GetComponentInChildren<Text>().text==caption);
        private void Pointer(Button button)
        {
            Assert.That(ExecuteEvents.Execute(button.gameObject,new PointerEventData(UiEventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler),Is.True);
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
            Assert.That(Client.NextCommandId,Is.EqualTo(command),"One pointer click sends one command");
            yield return Wait(()=>Grid.LastCommandId==command && Grid.LastStatus==GridResultStatus.Executed,"Actual command executed");
        }

        [UnityTearDown]
        public IEnumerator CleanupOwnedLauncher()
        {
            if(ownedRoot!=null)
            {
                if(ownedInit!=null && Init.Instance==ownedInit)ownedInit.Shutdown();
                if(UnityGameFramework.Runtime.GameEntry.IsInitialized)UnityGameFramework.Runtime.GameEntry.Shutdown(UnityGameFramework.Runtime.ShutdownType.None);
                if(ownedRoot!=null)UnityEngine.Object.Destroy(ownedRoot);
                yield return null;
                RuntimeInstanceContext.Reset();ownedRoot=null;ownedInit=null;
            }
        }

        [Test]
        public void SoloRestaurantUiBindingsRealPrefabHasGeneratedFieldsAndOriginalCanvas()
        {
            GameObject asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab");
            Assert.That(asset,Is.Not.Null);var view=asset.GetComponent<MonoUIGridKitchen>();Assert.That(view,Is.Not.Null);
            foreach(Text text in new[]{view.PhaseText,view.ClockText,view.ScoreText,view.HandText,view.WorkText,view.StatusText,view.HintText,view.OrdersText,view.BuffText,view.SettlementText})
                Assert.That(text,Is.Not.Null,"Generated Text binding");
            foreach(RectTransform rect in new[]{view.OrderPanelRectTransform,view.BuffPanelRectTransform,view.SettlementPanelRectTransform,view.ControlPanelRectTransform,view.CameraDragAreaRectTransform,view.OrderActionsRectTransform,view.BuffActionsRectTransform,view.TargetsRectTransform})
                Assert.That(rect,Is.Not.Null,"Generated panel binding");
            Assert.That(asset.GetComponent<Canvas>().renderMode,Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(asset.GetComponent<GraphicRaycaster>(),Is.Not.Null);Assert.That(view.TargetTemplateButton.gameObject.activeSelf,Is.False);
            Assert.That(view.CameraDragAreaRectTransform.GetComponent<Image>().raycastTarget,Is.False);
            Assert.That(view.TargetsRectTransform.GetComponentInParent<ScrollRect>(true).content,Is.SameAs(view.TargetsRectTransform));
        }

        [UnityTest]
        public IEnumerator SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen()
        {
            yield return StartLauncher();GridPresentationComponent presentation=Presentation;
            UIGridKitchen original=presentation.Hud;Assert.That(original.Parent,Is.SameAs(((Scene)Client.IScene).GetComponent<UIComponent>()));
            Assert.That(Hud.UGFUIForm,Is.SameAs(original));Assert.That(Hud.PhaseText.text,Is.EqualTo("准备"));Assert.That(Hud.ScoreText.text,Does.Contain("0 分"));
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
            Assert.That(Hud.SettlementPanelRectTransform.gameObject.activeSelf,Is.True);Assert.That(Hud.SettlementText.text,Does.Contain("0 分 · 0 星"));
            long oldLevel=Grid.LevelId;yield return Command("下一关");long nextCommand=Client.NextCommandId;
            Assert.That(Grid.LevelId,Is.Not.EqualTo(oldLevel));Assert.That(Hud.PhaseText.text,Is.EqualTo("准备"));Assert.That(Grid.SelectedBuffId,Is.Zero);
            oldCallback();yield return null;
            Assert.That(Client.NextCommandId,Is.EqualTo(nextCommand),"Old order button callback cannot submit to the new level");Assert.That(Grid.LevelScore,Is.Zero);Assert.That(Grid.Orders,Is.Empty);
            Assert.That(Hud.OrdersText.text,Is.EqualTo("暂无待交付订单"));Assert.That(Hud.SettlementPanelRectTransform.gameObject.activeSelf,Is.False);
        }

        [UnityTest]
        public IEnumerator SoloRestaurantUiProjectionActualBakingSeparatesReceiptProgressAndCompletion()
        {
            yield return StartLauncher();yield return Command("拿 面包台 ·",true);yield return Command("放 烤箱 ·",true);
            long command=Client.NextCommandId+1;Pointer(Button("烘烤 烤箱 ·",true));
            Assert.That(Client.NextCommandId,Is.EqualTo(command));Assert.That(Grid.LastStatus,Is.EqualTo(GridResultStatus.Accepted));
            Assert.That(Grid.BakeProcesses,Is.Empty,"Receipt has not created a completed bake");
            yield return Wait(()=>Grid.BakeProcesses.Values.Any(value=>value.State==GridBakeState.Baking),"Actual bake started");
            long process=Grid.BakeProcesses.Values.Single().Id;Assert.That(Hud.HintText.text,Does.Contain("烘烤"));
            Assert.That(Grid.BakeProcesses[process].ProgressTicks,Is.LessThan(Grid.BakeProcesses[process].RequiredTicks));
            Assert.That(Grid.Items.Values.Any(value=>value.DefinitionId==GridKitchenRules.Toast),Is.False);
            yield return Command("暂停");int progress=Grid.BakeProcesses[process].ProgressTicks;string hint=Hud.HintText.text;
            yield return new WaitForSecondsRealtime(.25f);Assert.That(Grid.BakeProcesses[process].ProgressTicks,Is.EqualTo(progress));Assert.That(Hud.HintText.text,Is.EqualTo(hint));
            yield return Command("恢复");yield return Wait(()=>Grid.BakeProcesses[process].State==GridBakeState.Completed,"Actual bake completes");
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
