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
            self.LayoutStationId=0;self.LayoutLevelId=0;self.LayoutLevelGeneration=0;
            self.BuffCandidatesVisible=true;
            self.HudLoading=false;self.HudLoadEpoch=0;
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
                    self.OpenGridHud();
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
            if(grid==null || !grid.Confirmed || client.Stage!=ProductClientStage.Joined || board?.View==null || hud?.View==null || !hud.Available)return;
            if(!self.Ready)
            {
                try
                {
                    board.View.Configure(grid.Width,grid.Height,grid.KitchenBottom);Canvas.ForceUpdateCanvases();board.View.SetHudViewport(hud.View.MapViewport());self.Ready=true;
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
            board.View.SetHudViewport(hud.View.MapViewport());
            float tick=grid.Tick+Mathf.Clamp(TimeInfo.Instance.FrameTime-grid.ReceivedFrameTime,0,grid.TickMilliseconds*grid.StepTicks)/(float)grid.TickMilliseconds;
            if (grid.LevelPaused || grid.LevelPhase == LevelPhase.Completed) tick = grid.Tick;
            board.View.Render(grid.Actors.Values,grid.Stations.Values,grid.Items.Values,grid.AutoProcesses.Values,grid.BakeProcesses.Values,
                grid.Companions.Values,grid.Customers.Values,grid.FrontWorks.Values,grid.MemberId,tick,grid.StepTicks,self.Highlight);
            if(grid.LastCommandId>=self.HighlightCommand && grid.LastStatus is GridResultStatus.Rejected or GridResultStatus.Cancelled)self.Highlight=null;
            if (grid.LastCommandKind == ProductCommandKind.Level) self.Highlight = null;
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
                self.PointerConsumed=!board.View.InputScreenRect.Contains(point);
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
            if(self.LayoutStationId!=0)
            {
                if(grid.LevelId!=self.LayoutLevelId || grid.LevelGeneration!=self.LayoutLevelGeneration ||
                    grid.LevelPhase!=LevelPhase.Preparing || grid.LevelPaused || !grid.Stations.TryGetValue(self.LayoutStationId,out GridStationProjection chosen))
                { self.LayoutStationId=0;self.RefreshGridHud();return; }
                self.SendStationLayout(chosen.Id,cell,chosen.Orientation,grid.LayoutVersion,self.LayoutLevelId,self.LayoutLevelGeneration);
                return;
            }
            foreach(GridStationProjection station in grid.Stations.Values)
                if(station.Footprint.Contains(cell)){self.ChooseGridCounter(station.Id);return;}
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
            var owner=self.GridCommandOwner();if(owner==null)return;
            EntityRef<GridPresentationComponent> selfRef=self;
            owner.Run(async context=>
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

        public static void SendLevelControl(this GridPresentationComponent self, LevelControlAction action, long expectedLevel, long expectedGeneration)
        {
            var owner=self.GridCommandOwner();if(owner==null)return;
            EntityRef<GridPresentationComponent> selfRef = self;
            owner.Run(async context =>
            {
                ClientSessionComponent client = ((Scene)self.IScene).GetComponent<ClientSessionComponent>();
                GridClientComponent grid = client.GetComponent<GridClientComponent>(); EntityRef<GridClientComponent> gridRef = grid;
                if(grid.LevelId!=expectedLevel || grid.LevelGeneration!=expectedGeneration)return;
                LevelCommandRequest request = grid.CreateLevelCommand(action);
                request.LevelId = expectedLevel; request.LevelGeneration = expectedGeneration;
                long command = request.CommandId, generation = grid.WorldGeneration, currentLevel = grid.LevelId;
                try
                {
                    string error = await client.CallProductValueAsync(context, request, response => response.Error == 0 ? null : response.Message);
                    context.ThrowIfStopped(); self = selfRef; grid = gridRef;
                    if (self == null || grid == null || !self.Ready || grid.WorldGeneration != generation || grid.LevelId != currentLevel || grid.LastCommandId != command) return;
                    if (error != null) { grid.LastStatus = GridResultStatus.Rejected; grid.LastReason = error; self.RefreshGridHud(); }
                }
                catch (OperationCanceledException) { }
                catch (Exception exception) { self = selfRef; if (self != null) { self.Failure = exception.Message; self.RefreshGridHud(); } }
            });
        }

        public static void RefreshGridHud(this GridPresentationComponent self)
        {
            UIGridKitchen hud=self.Hud;if(hud?.View==null)return;
            GridClientComponent grid=((Scene)self.IScene).GetComponent<ClientSessionComponent>().GetComponent<GridClientComponent>();
            MonoUIGridKitchen view=hud.View;
            self.RefreshRestaurantPanels(view,grid);
            string state=grid.LastStatus switch {GridResultStatus.Accepted=>"已发送",GridResultStatus.PendingApproach=>"正在接近",GridResultStatus.Executed=>"动作已执行",GridResultStatus.Cancelled=>"已取消",GridResultStatus.Rejected=>"无法操作",_=>"就绪"};
            string reason=grid.LastReason switch {"HandFull"=>"手中已有物品", "TargetChanged"=>"台面或物品已变化", "PickupRequiresSameCell"=>"需要站在物品所在格", "BlockedCell"=>"此格不可行走", "Unreachable"=>"无法到达", "CounterNotAdjacent"=>"需要靠近台面", "WorkBusy"=>"正在工作，先停止或等待队友", "ContainerFull"=>"容器已满", "IncompatibleMaterial"=>"材料不兼容", "NestedContainer"=>"容器不能套容器", "NoRecipe"=>"没有对应配方", "ProcessChanged"=>"作业或批次已变化", "RequiresHeldEggBowl"=>"需要手持含鸡蛋的碗", "RequiresEggLiquidBowl"=>"需要碗中有一份打好的蛋液", "ProcessLocked"=>"这锅已开始加工，不能改动材料", "NotReady"=>"汤还没煮好", "RequiresStove"=>"需要把锅放在灶台上启动", "StationBusy"=>"灶台仍在加工上一锅", "RequiresEmptyBowl"=>"需要手持空碗", _=>grid.LastReason==null?"":grid.LastReason};
            reason = grid.LastReason switch { "Paused" => "已暂停，请先恢复", "WrongLevel" => "关卡已变化", "LevelCompleted" => "本关已结束", "WrongPhase" => "当前阶段不能执行", "MissingNextLevel" => "下一关配置缺失", "InvalidSpawn" => "下一关出生位不可用", _ => reason };
            reason = grid.LastReason switch { "LayoutChanged" => "布局已变化，请按当前位置重试", "LayoutBlocked" => "占地越界、重叠或通路被堵住", "LayoutOccupied" => "此处有角色或落物", "LayoutRelationshipsChanged" => "工位物品或作业关系已变化", "InvalidLayout" => "布局参数无效", _ => reason };
            if (grid.LastReason == "RequiresOven") reason="需要兼容烤箱";
            reason=grid.LastReason switch{"RequiresTablePosition"=>"请先走到餐桌旁","WrongDish"=>"手中菜品与订单不符","DirtyUtensil"=>"餐具需要先洗净",
                "OrderChanged" or "OrderEnded"=>"订单已结束或顾客已更换","ItemChanged"=>"手持物已变化","TableBusy"=>"餐桌暂时不能交付","RequiresFrontAction"=>"请使用前厅工作或交付按钮",_=>reason};
            reason=grid.LastReason switch {"BuffChoiceRequired"=>"请先选择本关加成","BuffLocked"=>"本关加成已锁定","InvalidBuffChoice"=>"此加成不在本关候选中","DrawChanged"=>"候选已变化",_=>reason};
            string phase = grid.LevelPhase switch { LevelPhase.Preparing => "准备", LevelPhase.Operating => "营业", LevelPhase.Draining => "收尾", LevelPhase.Completed => $"结算 {grid.LevelScore}分 {grid.LevelStars}星", _ => "" };
            view.StatusText.text=(grid.LevelId==0?"":phase+(grid.LevelPaused?"（暂停）":"")+" · ")+$"玩家 {grid.MemberId} · {state} {reason}";
            if(!grid.Actors.TryGetValue(grid.MemberId,out GridActorProjection own))return;
            string hand=own.HandItemId!=0 ? grid.ItemDescription(own.HandItemId):"空手";
            view.HandText.text=$"手持：{hand}";
            view.HintText.text=string.Join(" · ",grid.Processes.Values.Select(GridClientSystem.WorkDescription).Concat(grid.AutoProcesses.Values.Select(GridClientSystem.AutoWorkDescription)).Concat(grid.BakeProcesses.Values.Select(GridClientSystem.BakeWorkDescription)));
            if(view.HintText.text.Length==0)view.HintText.text="点击目标拿放／切配 · 拖动查看餐厅";
            if (grid.LevelId != 0) view.HintText.text += " · 试玩进度仅保留本次运行";
            if(grid.SelectedBuffId!=0)view.HintText.text+=" · 本关加成："+Tables.Instance.DTBuffConfig.Get(grid.SelectedBuffId).Name;
            if(self.LayoutStationId!=0 && (grid.LevelId!=self.LayoutLevelId || grid.LevelGeneration!=self.LayoutLevelGeneration || grid.LevelPhase!=LevelPhase.Preparing || grid.LevelPaused)) self.LayoutStationId=0;
            if(self.LayoutStationId!=0 && grid.Stations.TryGetValue(self.LayoutStationId,out GridStationProjection selected)) view.HintText.text=$"调整 {selected.Name}：点击目标格移动，或点旋转";
            GridItemProjection[] drops=grid.Items.Values.Where(value=>value.Location==GridItemLocation.Ground && value.Cell==own.Cell).OrderBy(value=>value.Id).ToArray();
            string targetsKey=$"{own.HandItemId}:{own.WorkId}:"+string.Join(",",drops.Select(value=>value.Id))+"|"+
                string.Join(";",grid.Stations.Values.OrderBy(v=>v.Id).Select(v=>$"{v.Id}:{v.ItemId}"))+"|"+
                string.Join(";",grid.Items.Values.OrderBy(v=>v.Id).Select(v=>$"{v.Id}:{string.Join(",",v.Contents)}"))+"|"+
                string.Join(";",grid.Processes.Values.OrderBy(v=>v.Id).Select(v=>$"{v.Id}:{v.State}:{v.WorkerMemberId}:{v.StationId}"))+"|"+
                string.Join(";",grid.AutoProcesses.Values.OrderBy(v=>v.Id).Select(v=>$"{v.Id}:{v.State}:{v.RemainingPortions}"));
            targetsKey += $"|{grid.LevelId}:{grid.LevelGeneration}:{grid.LevelPhase}:{grid.LevelPaused}";
            targetsKey += $"|{grid.LayoutVersion}:{self.LayoutStationId}";
            targetsKey+=$"|{grid.BuffDrawVersion}:{grid.SelectedBuffId}:{self.BuffCandidatesVisible}:"+string.Join(",",grid.BuffCandidates);
            targetsKey += "|"+string.Join(";",grid.BakeProcesses.Values.OrderBy(value=>value.Id).Select(value=>$"{value.Id}:{value.State}"));
            targetsKey += "|"+string.Join(";",grid.FrontWorks.Values.OrderBy(value=>value.Id).Select(value=>$"{value.Id}:{value.State}:{value.ClaimSerial}:{value.WorkerId}"));
            targetsKey += "|"+string.Join(";",grid.Orders.Values.OrderBy(value=>value.Id).Select(value=>$"{value.Id}:{value.State}"));
            targetsKey += "|"+string.Join(";",grid.Stations.Values.OrderBy(value=>value.Id).Select(value=>$"{value.Id}:{string.Join(",",value.PoolItemIds)}"));
            if(self.HudTargetsKey==targetsKey)return;
            self.HudTargetsKey=targetsKey;view.ClearTargets();
            EntityRef<GridClientComponent> currentProjection=grid;EntityRef<UIGridKitchen> currentHud=hud;
            string inputWorld=grid.WorldId;long inputWorldGeneration=grid.WorldGeneration,inputBinding=grid.BindingGeneration,inputLevel=grid.LevelId,inputLevelGeneration=grid.LevelGeneration;
            view.SetInputGuard(()=>
            {
                GridClientComponent projection=currentProjection;UIGridKitchen originalHud=currentHud;
                return originalHud?.Available==true && projection?.Confirmed==true && projection.WorldId==inputWorld && projection.WorldGeneration==inputWorldGeneration &&
                    projection.BindingGeneration==inputBinding && projection.LevelId==inputLevel && projection.LevelGeneration==inputLevelGeneration;
            });
            EntityRef<GridPresentationComponent> selfRef=self;
            if(self.LayoutStationId!=0 && grid.Stations.TryGetValue(self.LayoutStationId,out GridStationProjection adjusting))
            {
                long layout=grid.LayoutVersion,level=grid.LevelId,generation=grid.LevelGeneration;
                view.AddTarget("旋转 "+adjusting.Name,()=>{GridPresentationComponent current=selfRef;current?.SendStationLayout(adjusting.Id,adjusting.Cell,(adjusting.Orientation+1)%4,layout,level,generation);});
                view.AddTarget("结束调整",()=>{GridPresentationComponent current=selfRef;if(current!=null){current.LayoutStationId=0;current.RefreshGridHud();}});
            }
            if (grid.LevelId != 0)
            {
                long level = grid.LevelId, levelGeneration = grid.LevelGeneration;
                if (grid.LevelPhase == LevelPhase.Completed)
                    view.AddControl("下一关", () => { GridPresentationComponent current = selfRef; current?.SendLevelControl(LevelControlAction.Continue, level, levelGeneration); });
                else
                {
                    if (grid.LevelPhase == LevelPhase.Preparing && !grid.LevelPaused && (!grid.RequireBuffChoice || grid.SelectedBuffId!=0))
                        view.AddControl("开始营业", () => { GridPresentationComponent current = selfRef; current?.SendLevelControl(LevelControlAction.Start, level, levelGeneration); });
                    bool paused = grid.LevelPaused;
                    view.AddControl(paused ? "恢复" : "暂停", () => { GridPresentationComponent current = selfRef; current?.SendLevelControl(paused ? LevelControlAction.Resume : LevelControlAction.Pause, level, levelGeneration); });
                }
            }
            if(grid.LevelPhase==LevelPhase.Preparing && grid.BuffCandidates.Count>0)
            {
                bool shown=self.BuffCandidatesVisible;
                view.AddControl(shown?"隐藏候选加成":"查看候选加成",()=>{GridPresentationComponent current=selfRef;if(current!=null){current.BuffCandidatesVisible=!shown;current.RefreshGridHud();}});
                if(shown && !grid.LevelPaused && grid.SelectedBuffId==0)
                    foreach(int candidate in grid.BuffCandidates)
                    {
                        DRBuffConfig definition=Tables.Instance.DTBuffConfig.Get(candidate);long level=grid.LevelId,generation=grid.LevelGeneration,draw=grid.BuffDrawVersion;
                        view.AddBuffAction("选择 "+definition.Name+" · 煮汤耗时 "+definition.DurationNumerator+"/"+definition.DurationDenominator,
                            ()=>{GridPresentationComponent current=selfRef;current?.SendBuffChoice(candidate,draw,level,generation);});
                    }
            }
            if (!grid.LevelPaused && grid.LevelPhase is LevelPhase.Operating or LevelPhase.Draining)
            {
                long level=grid.LevelId,generation=grid.LevelGeneration;
                if (own.FrontWorkId != 0 && grid.FrontWorks.TryGetValue(own.FrontWorkId, out GridFrontWorkProjection currentWork))
                    view.AddTarget("停手前厅工作",()=>{GridPresentationComponent current=selfRef;current?.SendFrontWork(FrontWorkAction.Stop,currentWork.Id,currentWork.ClaimSerial,level,generation);});
                else if (own.HandItemId == 0 && own.WorkId == 0)
                    foreach (GridFrontWorkProjection work in grid.FrontWorks.Values.Where(value=>value.State==FrontWorkState.Available).OrderBy(value=>value.Id))
                    {
                        string action=work.Kind switch {FrontWorkKind.Ask=>"询问",FrontWorkKind.Clear=>"清桌",_=>"洗碗"};
                        string target=grid.Stations.TryGetValue(work.StationId,out GridStationProjection station)?station.Name:"前厅";
                        view.AddTarget(action+" "+target,()=>{GridPresentationComponent current=selfRef;current?.SendFrontWork(FrontWorkAction.Claim,work.Id,work.ClaimSerial,level,generation);});
                    }
            }
            if(!grid.LevelPaused && grid.LevelPhase is LevelPhase.Operating or LevelPhase.Draining)
                foreach(GridOrderProjection order in grid.Orders.Values.Where(value=>value.State==OrderState.Waiting).OrderBy(value=>value.Id))
                {
                    if(!grid.Stations.TryGetValue(order.TableStationId,out GridStationProjection table))continue;
                    GridCell approach=table.Approaches.First(cell=>!grid.Stations.Values.Any(value=>value.Footprint.Contains(cell)));
                    view.AddOrderAction("前往 "+table.Name,()=>{GridPresentationComponent current=selfRef;current?.SendGridAction(GridAction.Move,approach);});
                    if(own.HandItemId!=0)
                    {
                        long item=own.HandItemId,level=grid.LevelId,generation=grid.LevelGeneration;
                        string dish=Tables.Instance.DTDishConfig.Get(order.DishId).Name;
                        view.AddOrderAction("交付 "+dish+" → "+table.Name,()=>{GridPresentationComponent current=selfRef;current?.SendServeOrder(order.Id,order.CustomerId,order.TableStationId,item,level,generation);});
                    }
                }
            foreach(GridStationProjection station in grid.Stations.Values.OrderBy(value=>value.Id))
            {
                long id=station.Id; GridIntent intent=grid.CounterIntent(id);
                if(grid.LevelPhase==LevelPhase.Preparing && !grid.LevelPaused)
                {
                    long level=grid.LevelId,generation=grid.LevelGeneration;
                    view.AddTarget("调整 "+station.Name,()=>{GridPresentationComponent current=selfRef;current?.ChooseLayoutStation(id,level,generation);});
                }
                if(station.Kind==GridStationKind.CleanPool && own.HandItemId==0)
                    foreach(long item in station.PoolItemIds)
                        view.AddTarget("取回 "+grid.ItemDescription(item),()=>{GridPresentationComponent current=selfRef;current?.SendGridAction(GridAction.TakeCounter,station.Cell,id,item);});
                if(station.Kind is GridStationKind.Table or GridStationKind.Wash or GridStationKind.DirtyPool or GridStationKind.CleanPool) continue;
                string action=intent.Action switch {GridAction.PlaceCounter=>"放",GridAction.AddToContainer=>"添加",GridAction.StartStationWork=>intent.ExpectedProcessId==0?"切配":"继续切配",GridAction.StopWork=>"停止",GridAction.PourContents=>"倒蛋液",GridAction.StartAutoWork=>"煮汤",GridAction.PortionSoup=>"盛一份",GridAction.StartBake=>"烘烤",_=>"拿"};
                view.AddTarget($"{action} {station.Name} · {grid.ItemDescription(station.ItemId)}",()=>{GridPresentationComponent current=selfRef;current?.SendGridIntent(intent);});
                if (own.HandItemId == 0 && grid.Items.TryGetValue(station.ItemId, out GridItemProjection potItem) && potItem.DefinitionId == GridKitchenRules.Pot && intent.Action == GridAction.StartAutoWork)
                {
                    long potId = potItem.Id;
                    view.AddTarget("直接拿锅",()=>{GridPresentationComponent current=selfRef;current?.SendGridAction(GridAction.TakeCounter,station.Cell,id,potId);});
                }
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
        [UGFUIFormSystem] private static void UGFUIFormOnClose(this UIGridKitchen self,bool isShutdown)
        {
            self.GetComponent<AsyncOwnerComponent>()?.AsyncScope?.Stop();self.View?.ReleaseHud();
            ((Scene)self.IScene).GetComponent<GridPresentationComponent>()?.GridHudClosed(self);
        }
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
