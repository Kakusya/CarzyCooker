using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UIEventSystem = UnityEngine.EventSystems.EventSystem;

namespace ET.Client
{
    [EntitySystemOf(typeof(GridPresentationComponent))]
    [FriendOf(typeof(GridPresentationComponent))]
    [FriendOf(typeof(GridClientComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    public static partial class GridPresentationSystem
    {
        public const float DragThresholdPixels = 14;
        [EntitySystem]
        private static void Awake(this GridPresentationComponent self)
        {
            self.Ready=false;self.Failure=null;self.HudVersion=0;self.HudTargetsKey=null;self.Board=default;self.Hud=default;
            self.PointerHeld=false;self.PointerDragged=false;self.PointerConsumed=false;self.Highlight=null;self.HighlightCommand=0;
            EntityRef<GridPresentationComponent> selfRef=self;
            self.GetAsyncOwner().Run(async context=>
            {
                try
                {
                    Scene scene=(Scene)self.IScene;
                    UIComponent ui=scene.GetComponent<UIComponent>()??scene.AddComponent<UIComponent>();
                    GFEntityComponent entities=scene.GetComponent<GFEntityComponent>()??scene.AddComponent<GFEntityComponent>();
                    GridKitchenBoard board=await entities.AddGFEntityComponentAsync<GridKitchenBoard>(UGFEntityId.GridKitchenBoard);
                    context.ThrowIfStopped();self=selfRef;if(self==null){board?.Dispose();return;}
                    self.Board=board;
                    UIGridKitchen hud=await ui.AddUIFormComponentAsync<UIGridKitchen>(UGFUIFormId.UIGridKitchen);
                    context.ThrowIfStopped();self=selfRef;if(self==null){hud?.Dispose();return;}
                    self.Hud=hud;hud.View.ConfigureHud();self.TryGridReady();
                }
                catch(OperationCanceledException) { }
                catch(Exception exception)
                {
                    self=selfRef;if(self!=null){self.Failure=exception.Message;Log.Warning($"Grid presentation failed: {exception.Message}");}
                }
            });
        }

        public static void TryGridReady(this GridPresentationComponent self)
        {
            if(self.Failure!=null)return;
            Scene scene=(Scene)self.IScene;
            ClientSessionComponent client=scene.GetComponent<ClientSessionComponent>();
            GridClientComponent grid=client?.GetComponent<GridClientComponent>();
            GridKitchenBoard board=self.Board;UIGridKitchen hud=self.Hud;
            if(grid==null || !grid.Confirmed || client.Stage!=ProductClientStage.Joined || board?.View==null || hud?.View==null)return;
            if(!self.Ready)
            {
                try
                {
                    board.View.Configure(grid.Width,grid.Height,grid.KitchenBottom);self.Ready=true;
                    Log.Info($"GridReady world={grid.WorldId} generation={grid.WorldGeneration} member={client.MemberId}");
                }
                catch(Exception exception){self.Failure=exception.Message;Log.Warning($"Grid view initialization failed: {exception.Message}");}
            }
        }

        [EntitySystem]
        private static void Update(this GridPresentationComponent self)
        {
            self.TryGridReady();
            if(!self.Ready)return;
            ClientSessionComponent client=((Scene)self.IScene).GetComponent<ClientSessionComponent>();
            GridClientComponent grid=client.GetComponent<GridClientComponent>();
            if(client.Stage!=ProductClientStage.Joined || !grid.Confirmed){self.Ready=false;return;}
            GridKitchenBoard board=self.Board;UIGridKitchen hud=self.Hud;
            if(board?.View==null || hud?.View==null){self.Ready=false;return;}
            float tick=grid.Tick+Mathf.Clamp(TimeInfo.Instance.FrameTime-grid.ReceivedFrameTime,0,grid.TickMilliseconds*grid.StepTicks)/(float)grid.TickMilliseconds;
            board.View.Render(grid.Actors.Values,grid.Stations.Values,grid.Items.Values,grid.MemberId,tick,grid.StepTicks,self.Highlight);
            if(grid.LastCommandId>=self.HighlightCommand && grid.LastStatus is GridResultStatus.Rejected or GridResultStatus.Cancelled)self.Highlight=null;
            if(self.HudVersion!=grid.Version){self.HudVersion=grid.Version;self.RefreshGridHud();}
            var touch=Touchscreen.current?.primaryTouch;
            if(touch!=null && (touch.press.isPressed || touch.press.wasReleasedThisFrame))
                self.ProcessGridPointer(touch.position.ReadValue(),touch.press.wasPressedThisFrame,touch.press.isPressed,touch.press.wasReleasedThisFrame);
            else if(Mouse.current!=null)
                self.ProcessGridPointer(Mouse.current.position.ReadValue(),Mouse.current.leftButton.wasPressedThisFrame,Mouse.current.leftButton.isPressed,Mouse.current.leftButton.wasReleasedThisFrame);
        }

        public static void ProcessGridPointer(this GridPresentationComponent self, Vector2 point, bool down, bool held, bool up)
        {
            if(!self.Ready)return;
            GridKitchenBoard board=self.Board;
            if(down)
            {
                self.PointerHeld=true;self.PointerDragged=false;self.PointerStart=point;self.PointerLast=point;
                self.PointerConsumed=!board.View.BoardCamera.pixelRect.Contains(point);
                if(UIEventSystem.current!=null)
                {
                    var hits=new List<RaycastResult>();
                    UIEventSystem.current.RaycastAll(new PointerEventData(UIEventSystem.current){position=point},hits);
                    self.PointerConsumed |= hits.Count>0;
                }
            }
            if(!self.PointerHeld)return;
            if(!self.PointerConsumed && (held||up))
            {
                if(Mathf.Abs(point.y-self.PointerStart.y)>=DragThresholdPixels)self.PointerDragged=true;
                if(self.PointerDragged)board.View.Pan(point.y-self.PointerLast.y);
                self.PointerLast=point;
            }
            if(up)
            {
                self.PointerHeld=false;
                if(!self.PointerConsumed && !self.PointerDragged)self.ClickGridCell(board.View.ScreenToCell(point));
            }
        }

        public static void ClickGridCell(this GridPresentationComponent self, GridCell cell)
        {
            if(!self.Ready)return;
            GridClientComponent grid=((Scene)self.IScene).GetComponent<ClientSessionComponent>().GetComponent<GridClientComponent>();
            foreach(GridStationProjection station in grid.Stations.Values)
                if(station.Cell==cell){self.ChooseGridCounter(station.Id);return;}
            if(!grid.Actors.TryGetValue(grid.MemberId,out GridActorProjection own))return;
            GridItemProjection[] drops=grid.Items.Values.Where(item=>item.Location==GridItemLocation.Ground && item.Cell==cell && cell==own.Cell).ToArray();
            if(drops.Length==1){self.SendGridAction(GridAction.PickupDrop,cell,drops[0].Id);return;}
            if(drops.Length>1){self.RefreshGridHud();return;}
            if(cell.X<=0 || cell.X>=grid.Width-1 || cell.Y<=0 || cell.Y>=grid.Height-1)return;
            self.SendGridAction(GridAction.Move,cell);
        }

        public static void ChooseGridCounter(this GridPresentationComponent self, long target)
        {
            if(!self.Ready)return;
            GridClientComponent grid=((Scene)self.IScene).GetComponent<ClientSessionComponent>().GetComponent<GridClientComponent>();
            if(!grid.Stations.TryGetValue(target,out GridStationProjection station) || !grid.Actors.TryGetValue(grid.MemberId,out GridActorProjection own))return;
            self.SendGridIntent(grid.CounterIntent(target));
        }

        public static void SendGridIntent(this GridPresentationComponent self, GridIntent intent) =>
            self.SendGridAction(intent.Action, intent.Cell, intent.TargetId, intent.ExpectedItemId, intent.ExpectedTargetItemId, intent.ExpectedProcessId);

        public static void SendGridAction(this GridPresentationComponent self, GridAction action, GridCell cell=default, long target=0, long expected=0, long expectedTarget=0, long process=0)
        {
            if(!self.Ready)return;
            EntityRef<GridPresentationComponent> selfRef=self;
            self.GetAsyncOwner().Run(async context=>
            {
                ClientSessionComponent client=((Scene)self.IScene).GetComponent<ClientSessionComponent>();
                GridClientComponent grid=client.GetComponent<GridClientComponent>();
                EntityRef<GridClientComponent> gridRef=grid;
                GridCommandRequest request=grid.CreateGridCommand(action,cell,target,expected,expectedTarget,process);
                long command=request.CommandId, generation=grid.WorldGeneration;
                self.Highlight=cell;self.HighlightCommand=command;
                try
                {
                    string rejection=await client.CallProductValueAsync(context,request,response=>response.Error==0?null:response.Message);
                    context.ThrowIfStopped();self=selfRef;grid=gridRef;
                    if(self==null || !self.Ready || grid==null || grid.WorldGeneration!=generation || grid.LastCommandId!=command)return;
                    if(rejection!=null){grid.LastStatus=GridResultStatus.Rejected;grid.LastReason=rejection;self.Highlight=null;self.RefreshGridHud();}
                }
                catch(OperationCanceledException){ }
                catch(Exception exception)
                {self=selfRef;if(self!=null && self.HighlightCommand==command){self.Highlight=null;self.Failure=exception.Message;self.RefreshGridHud();}}
            });
        }

        public static void RefreshGridHud(this GridPresentationComponent self)
        {
            UIGridKitchen hud=self.Hud;if(hud?.View==null)return;
            GridClientComponent grid=((Scene)self.IScene).GetComponent<ClientSessionComponent>().GetComponent<GridClientComponent>();
            MonoUIGridKitchen view=hud.View;
            string state=grid.LastStatus switch {GridResultStatus.Accepted=>"已发送",GridResultStatus.PendingApproach=>"正在接近",GridResultStatus.Executed=>"动作已执行",GridResultStatus.Cancelled=>"已取消",GridResultStatus.Rejected=>"无法操作",_=>"就绪"};
            string reason=grid.LastReason switch {"HandFull"=>"手中已有物品", "TargetChanged"=>"台面或物品已变化", "PickupRequiresSameCell"=>"需要站在物品所在格", "BlockedCell"=>"此格不可行走", "Unreachable"=>"无法到达", "CounterNotAdjacent"=>"需要靠近台面", "WorkBusy"=>"正在工作，先停止或等待队友", "ContainerFull"=>"碗已满", "IncompatibleMaterial"=>"材料不兼容", "NestedContainer"=>"容器不能套容器", "NoRecipe"=>"没有对应配方", "ProcessChanged"=>"作业或执行者已变化", "RequiresHeldEggBowl"=>"需要手持含鸡蛋的碗",_=>grid.LastReason==null?"":grid.LastReason};
            view.StatusText.text=$"玩家 {grid.MemberId} · {state} {reason}";
            if(!grid.Actors.TryGetValue(grid.MemberId,out GridActorProjection own))return;
            string hand=own.HandItemId!=0 ? grid.ItemDescription(own.HandItemId):"空手";
            view.HandText.text=$"手持：{hand}";
            view.HintText.text=string.Join(" · ",grid.Processes.Values.Select(GridClientSystem.WorkDescription));
            if(view.HintText.text.Length==0)view.HintText.text="点击目标拿放／切配 · 拖动查看餐厅";
            GridItemProjection[] drops=grid.Items.Values.Where(value=>value.Location==GridItemLocation.Ground && value.Cell==own.Cell).OrderBy(value=>value.Id).ToArray();
            string targetsKey=$"{own.HandItemId}:{own.WorkId}:"+string.Join(",",drops.Select(value=>value.Id))+"|"+
                string.Join(";",grid.Stations.Values.OrderBy(v=>v.Id).Select(v=>$"{v.Id}:{v.ItemId}"))+"|"+
                string.Join(";",grid.Items.Values.OrderBy(v=>v.Id).Select(v=>$"{v.Id}:{string.Join(",",v.Contents)}"))+"|"+
                string.Join(";",grid.Processes.Values.OrderBy(v=>v.Id).Select(v=>$"{v.Id}:{v.State}:{v.WorkerMemberId}:{v.StationId}"));
            if(self.HudTargetsKey==targetsKey)return;
            self.HudTargetsKey=targetsKey;view.ClearTargets();
            EntityRef<GridPresentationComponent> selfRef=self;
            foreach(GridStationProjection station in grid.Stations.Values.OrderBy(value=>value.Id))
            {
                long id=station.Id; GridIntent intent=grid.CounterIntent(id);
                string action=intent.Action switch {GridAction.PlaceCounter=>"放",GridAction.AddToContainer=>"添加",GridAction.StartStationWork=>intent.ExpectedProcessId==0?"切配":"继续切配",GridAction.StopWork=>"停止",_=>"拿"};
                view.AddTarget($"{action} {station.Name} · {grid.ItemDescription(station.ItemId)}",()=>{GridPresentationComponent current=selfRef;current?.SendGridIntent(intent);});
                GridProcessProjection paused=grid.Processes.Values.FirstOrDefault(v=>v.InputItemId==station.ItemId && v.State==GridWorkState.Paused);
                if(own.HandItemId==0 && paused.Id!=0)
                {
                    long item=station.ItemId;
                    view.AddTarget("拿取暂停材料",()=>{GridPresentationComponent current=selfRef;current?.SendGridAction(GridAction.TakeCounter,station.Cell,id,item);});
                }
            }
            if(own.HandItemId!=0 && grid.Items.TryGetValue(own.HandItemId,out GridItemProjection bowl) && bowl.DefinitionId==GridKitchenRules.Bowl && bowl.Contents.Count==1 &&
                grid.Items.TryGetValue(bowl.Contents[0],out GridItemProjection egg) && egg.DefinitionId==GridKitchenRules.Egg)
            {
                GridProcessProjection work=grid.Processes.Values.FirstOrDefault(v=>v.InputItemId==egg.Id && v.State!=GridWorkState.Completed);
                var intent=new GridIntent(0,own.WorkId==work.Id && work.Id!=0?GridAction.StopWork:GridAction.StartHeldWork,own.Cell,bowl.Id,egg.Id,bowl.Id,work.Id);
                view.AddTarget(intent.Action==GridAction.StopWork?"停止打蛋":work.Id==0?"开始打蛋":"继续打蛋",()=>{GridPresentationComponent current=selfRef;current?.SendGridIntent(intent);});
            }
            int dropIndex=0;
            foreach(GridItemProjection item in drops)
            {
                long id=item.Id;
                view.AddTarget($"拾 {item.Name} {++dropIndex}",()=>{GridPresentationComponent current=selfRef;current?.SendGridAction(GridAction.PickupDrop,own.Cell,id);});
            }
        }

        [EntitySystem]
        private static void Destroy(this GridPresentationComponent self)
        {
            self.Ready=false;self.Highlight=null;self.PointerHeld=false;
            Scene scene=self.Parent as Scene;
            scene?.GetComponent<UIComponent>()?.RemoveComponent<UIGridKitchen>();
            scene?.GetComponent<GFEntityComponent>()?.RemoveComponent<GridKitchenBoard>();
            self.Board=default;self.Hud=default;
        }
    }

    [EntitySystemOf(typeof(GridKitchenBoard))]
    public static partial class GridKitchenBoardSystem
    {
        [EntitySystem] private static void Awake(this GridKitchenBoard self) { }
        [UGFEntitySystem] private static void UGFEntityOnHide(this GridKitchenBoard self,bool isShutdown)=>self.View?.ReleaseBoard();
    }
    [EntitySystemOf(typeof(UIGridKitchen))]
    public static partial class UIGridKitchenSystem
    {
        [EntitySystem] private static void Awake(this UIGridKitchen self) { }
        [UGFUIFormSystem] private static void UGFUIFormOnClose(this UIGridKitchen self,bool isShutdown)=>self.View?.ReleaseHud();
    }
    [Event(SceneType.ProductClient)]
    public sealed class GridJoinedView : AEvent<Scene,ProductClientJoined>
    {
        protected override UniTask Run(Scene scene,ProductClientJoined args)
        {if(scene.GetComponent<GridPresentationComponent>()==null)scene.AddComponent<GridPresentationComponent>();return UniTask.CompletedTask;}
    }
    [Event(SceneType.ProductClient)]
    public sealed class GridProjectionView : AEvent<Scene,GridProjectionApplied>
    {
        protected override UniTask Run(Scene scene,GridProjectionApplied args)
        {GridPresentationComponent view=scene.GetComponent<GridPresentationComponent>();view?.TryGridReady();view?.RefreshGridHud();return UniTask.CompletedTask;}
    }
    [Event(SceneType.ProductClient)]
    public sealed class GridLeftView : AEvent<Scene,ProductClientLeft>
    {
        protected override UniTask Run(Scene scene,ProductClientLeft args)
        {scene.RemoveComponent<GridPresentationComponent>();return UniTask.CompletedTask;}
    }
}
