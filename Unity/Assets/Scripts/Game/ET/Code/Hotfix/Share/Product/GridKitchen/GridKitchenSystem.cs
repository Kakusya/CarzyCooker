#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    [EntitySystemOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(GridActor))]
    [FriendOf(typeof(GridStation))]
    [FriendOf(typeof(GridItem))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(Level))]
    public static partial class GridKitchenSystem
    {
        [EntitySystem]
        private static void Awake(this GridKitchenComponent self)
        {
            self.Restaurant = self.Parent as RestaurantRuntime;
            RestaurantRuntime restaurant = self.Restaurant;
            self.Match = restaurant == null ? self.Parent as Match : restaurant.Match;
            if ((Match)self.Match == null) throw new InvalidOperationException("Kitchen requires a Match or Restaurant owner");
            self.WorldId = Guid.NewGuid().ToString("N");
            self.WorldGeneration = self.InstanceId;
            self.Tick = 0;
            self.Version = 0;
            self.LayoutVersion = 1;
            self.Width = GridKitchenRules.Width; self.Height = GridKitchenRules.Height;
            self.LastFrameTime = TimeInfo.Instance.FrameTime;
            self.Accumulator = 0;
            self.Stopped = false;
            self.Dirty = true;
            ValidateGridDefinitions(Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTGridManualRecipeConfig.DataList);
            ValidateGridSoupDefinitions(Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTGridAutoRecipeConfig.DataList);
            ValidateCleanupDefinitions(Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTStationConfig.DataList);
            GridDishConfig.ValidateAllMenus();
            self.Blocked.Clear(); self.Actors.Clear(); self.Stations.Clear(); self.Items.Clear(); self.Processes.Clear(); self.AutoProcesses.Clear();
            self.BakeProcesses.Clear();
            self.MemberChanges.Clear(); self.Incoming.Clear(); self.Results.Clear(); self.PendingResults.Clear();
            self.LayoutCommands.Clear();
            self.WorkTransitions.Clear(); self.WorkOutcomes.Clear();
            self.ActorStepBuffer.Clear();self.ManualStepBuffer.Clear();
            self.ActorRouteBuffer.Clear();Array.Clear(self.SearchQueue,0,self.SearchQueue.Length);Array.Clear(self.SearchPrevious,0,self.SearchPrevious.Length);
            if (!self.RestoreKitchenBaseline())
            {
                ProductMenuVariant variant = restaurant?.MenuVariant ?? ProductMenuVariant.Basic;
                var layout = Tables.Instance.DTInitialLayoutConfig.DataList.Where(row => ProductMenuCatalog.Includes(variant, row.MenuVariant)).ToArray();
                self.Blocked.UnionWith(GridStationConfig.ValidateInitialLayout(Tables.Instance.DTStationConfig.DataList, layout,
                    Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTLevelConfig.DataList.Where(row => row.MenuVariant == variant).Select(row => new GridCell(row.SpawnX, row.SpawnY))));
                foreach (var initial in layout) self.AddConfiguredStation(initial);
                self.AddComponent<KitchenSupply>();
            }
        }

        public static HashSet<GridCell> ValidateStationConfiguration()
        {
            var tables = Tables.Instance;
            var blocked = new HashSet<GridCell>();
            // 独立菜单可以复用同一格；只有同一运行菜单内的设备互斥。
            foreach (var menu in tables.DTLevelConfig.DataList.GroupBy(row => row.MenuVariant))
            {
                blocked.UnionWith(GridStationConfig.ValidateInitialLayout(tables.DTStationConfig.DataList,
                    tables.DTInitialLayoutConfig.DataList.Where(row => ProductMenuCatalog.Includes(menu.Key, row.MenuVariant)),
                    tables.DTGridItemConfig.DataList, menu.Select(row => new GridCell(row.SpawnX, row.SpawnY))));
            }
            return blocked;
        }

        public static bool IsInitialGridWalkable(GridCell cell) => GridStationGeometry.Inside(cell) && !ValidateStationConfiguration().Contains(cell);

        private static void AddConfiguredStation(this GridKitchenComponent self, DRInitialLayoutConfig initial)
        {
            DRStationConfig definition = Tables.Instance.DTStationConfig.Get(initial.StationDefinitionId);
            GridStation station = self.AddChild<GridStation>();
            station.InitialLayoutId = initial.Id;
            station.DefinitionId = definition.Id; station.Orientation = initial.Orientation;
            station.ItemSlotCapacity = definition.ItemSlotCapacity; station.Capabilities = (GridStationCapability)definition.Capabilities;
            station.Cell = new GridCell(initial.X, initial.Y); station.Name = initial.Name; station.ItemId = 0; station.Kind = (GridStationKind)definition.Kind;
            self.Stations.Add(station.Id, station);
            if (initial.InitialItemDefinitionId == 0) return;
            GridItem item = self.CreateGridItem(initial.InitialItemDefinitionId, GridItemLocation.Counter, station.Id, station.Cell);
            station.ItemId = item.Id;
        }

        public static bool AtStation(this GridKitchenComponent self, GridActor actor, GridStation station) =>
            self.IsWalkable(actor.Cell) && station.StationApproaches().Contains(actor.Cell);

        // 仅旧 Host 样机由 HostSession 调用；正式 Solo 由 RestaurantRuntime 唯一驱动。
        public static void AdvanceLegacyGridClock(this GridKitchenComponent self)
        {
            long now = TimeInfo.Instance.FrameTime;
            // Editor 阻塞后有界追赶；每一步仍使用相同业务时长。
            self.Accumulator += Math.Max(0, Math.Min(250, now - self.LastFrameTime));
            self.LastFrameTime = now;
            while (!self.Stopped && self.Accumulator >= GridKitchenRules.TickMilliseconds)
            {
                self.Accumulator -= GridKitchenRules.TickMilliseconds;
                self.AdvanceGridTick();
            }
        }

        [EntitySystem]
        private static void Destroy(this GridKitchenComponent self)
        {
            self.StopGrid();self.ActorStepBuffer.Clear();self.ManualStepBuffer.Clear();
            self.ActorRouteBuffer.Clear();Array.Clear(self.SearchQueue,0,self.SearchQueue.Length);Array.Clear(self.SearchPrevious,0,self.SearchPrevious.Length);
        }

        public static void StopGrid(this GridKitchenComponent self)
        {
            if (self.Stopped) return;
            self.Stopped = true;
            self.CancelSupplyCommands("WorldStopped");
            foreach (GridActor actor in self.OrderedActors())
            {
                self.PauseWork(actor);
                if (actor.HasIntent) self.Result(actor, actor.ActiveIntent.CommandId, GridResultStatus.Cancelled, "WorldStopped", actor.ActiveIntent.OriginKind);
            }
            self.Incoming.Clear(); self.MemberChanges.Clear();
            self.LayoutCommands.Clear();
        }

        public static bool IsWalkable(this GridKitchenComponent self, GridCell cell) =>
            cell.X > 0 && cell.X < self.Width - 1 && cell.Y > 0 && cell.Y < self.Height - 1 && !self.Blocked.Contains(cell);

        public static GridActor Actor(this GridKitchenComponent self, int memberId) => self.Actors.TryGetValue(memberId, out EntityRef<GridActor> value) ? value : null;
        public static GridStation Station(this GridKitchenComponent self, long id) => self.Stations.TryGetValue(id, out EntityRef<GridStation> value) ? value : null;
        public static GridItem Item(this GridKitchenComponent self, long id) => self.Items.TryGetValue(id, out EntityRef<GridItem> value) ? value : null;
        private static GridActor[] OrderedActors(this GridKitchenComponent self) => self.Actors.OrderBy(pair => pair.Key).Select(pair => (GridActor)pair.Value).Where(actor => actor != null).ToArray();

        public static bool HasBinding(this GridKitchenComponent self, int memberId, long generation)
        {
            Participant member = ((Match)self.Match).GetChild<Participant>(memberId);
            HostPeer peer = member?.BoundPeer ?? default;
            return member != null && member.Online && member.BindingGeneration == generation && peer != null && peer.Generation == generation && peer.EntryOpen && !peer.Closing;
        }

        public static void QueueGridMember(this GridKitchenComponent self, int memberId, long generation, bool joined)
        {
            if (!self.Stopped) self.MemberChanges.Enqueue(new GridMemberChange(memberId, generation, joined));
        }

        public static void ApplyGridMembers(this GridKitchenComponent self)
        {
            while (self.MemberChanges.Count > 0)
            {
                GridMemberChange change = self.MemberChanges.Dequeue();
                GridActor actor = self.Actor(change.MemberId);
                if (!change.Joined)
                {
                    if (actor == null || actor.Generation != change.Generation) continue;
                    self.PauseWork(actor);
                    self.ReleaseActorFrontWork(actor);
                    if (actor.HasIntent) self.Result(actor, actor.ActiveIntent.CommandId, GridResultStatus.Cancelled, "MemberLeft", actor.ActiveIntent.OriginKind);
                    GridItem held = self.Item(actor.HandItemId);
                    if (held != null)
                    {
                        held.Location = GridItemLocation.Ground; held.HolderId = 0; held.Cell = actor.Cell;
                        actor.HandItemId = 0;
                    }
                    self.Incoming.Remove(change.MemberId); self.Actors.Remove(change.MemberId);
                    actor.Dispose(); self.Dirty = true;
                    continue;
                }
                if (!self.HasBinding(change.MemberId, change.Generation) || actor != null) continue;
                bool spawned = false;
                Level level = self.ProductLevel();
                for (int y = level?.SpawnCell.Y ?? 10; y > 0 && !spawned; --y)
                for (int x = 1; x < self.Width - 1 && !spawned; ++x)
                {
                    GridCell cell = new(x, y);
                    if (level != null && y == level.SpawnCell.Y && x < level.SpawnCell.X) continue;
                    if (!self.IsWalkable(cell) || !self.CanStop(cell, null)) continue;
                    actor = level == null ? self.AddChild<GridActor>() : level.AddChild<GridActor>();
                    actor.MemberId = change.MemberId; actor.Generation = change.Generation;
                    actor.Cell = cell; actor.NextCell = cell; actor.Goal = cell; actor.StopCell = cell;
                    actor.HasStopReservation = true; actor.Motion = GridMotion.Standing;
                    self.Actors.Add(actor.MemberId, actor);
                    spawned = true; self.Dirty = true;
                }
                if (!spawned) self.Results.Add(new GridResultValue(change.MemberId, change.Generation, 0, GridResultStatus.Rejected, "MapFull"));
            }
        }

        internal static string SubmitGridIntent(this GridKitchenComponent self, int memberId, long generation, GridIntent intent)
        {
            GridActor actor = self.Actor(memberId);
            if (self.Stopped || !self.HasBinding(memberId, generation) || actor == null || actor.Generation != generation) return "EntryClosed";
            if (intent.CommandId <= 0 || intent.Action < GridAction.Move || intent.Action > GridAction.PortionTeaBase) return "InvalidCommand";
            if (IsDrinkAction(intent.Action) && !ProductMenuCatalog.SupportsDrinks(((RestaurantRuntime)self.Restaurant)?.MenuVariant ?? ProductMenuVariant.Basic)) return "DrinkMenuRequired";
            Match match = self.Match;
            Participant member = match.GetChild<Participant>(memberId);
            HostPeer peer = member.BoundPeer;
            Level level = self.ProductLevel();
            var context = new ProductCommandContext(match.MatchId, self.WorldId, self.WorldGeneration, level?.Id ?? 0, level?.Generation ?? 0, generation, intent.CommandId);
            string prior = peer.CheckProductCommand(context, ProductCommandKind.Grid, intent.Equals(peer.LastGridCommand), out bool replay);
            if (prior != null || replay) return prior;
            if (level != null && level.Paused) return "Paused";
            if (level != null && level.Phase == LevelPhase.Completed) return "LevelCompleted";
            if (intent.Action == GridAction.Move && !self.IsWalkable(intent.Cell)) return "BlockedCell";
            if (RequiresGridStation(intent.Action) && self.Station(intent.TargetId) == null) return "UnknownCounter";
            if (intent.Action == GridAction.PickupDrop && self.Item(intent.TargetId) == null) return "UnknownItem";
            // 接收元数据不移动角色/物品；世界修改留给固定 Tick。
            actor.LastReceivedCommandId = intent.CommandId; actor.LastReceivedIntent = intent;
            if (self.Incoming.TryGetValue(memberId, out GridIntent replaced))
                self.Result(actor, replaced.CommandId, GridResultStatus.Cancelled, "Replaced", replaced.OriginKind);
            peer.RecordProductCommand(context, ProductCommandKind.Grid);
            peer.LastGridCommand = intent;
            self.Incoming[memberId] = intent;
            return null;
        }

        public static void AdvanceGridTick(this GridKitchenComponent self)
        {
            if (self.Stopped) return;
            ++self.Tick;
            self.ApplyGridMembers();
            self.ActorStepBuffer.Clear();
            foreach(var pair in self.Actors)self.ActorStepBuffer.Add(pair);
            self.ActorStepBuffer.Sort(static (left,right)=>left.Key.CompareTo(right.Key));
            foreach(var entry in self.ActorStepBuffer)
            {
                GridActor actor=entry.Value;
                if(actor==null)
                {
                    continue;
                }
                if (!self.HasBinding(actor.MemberId, actor.Generation)) continue;
                if (actor.Motion == GridMotion.Transit)
                {
                    self.Dirty = true;
                    if (self.Tick - actor.StepStartTick < GridKitchenRules.StepTicks) continue;
                    actor.Cell = actor.NextCell;
                    actor.Motion = GridMotion.Standing;
                    ++actor.PathIndex;
                }
                if (self.Incoming.TryGetValue(actor.MemberId, out GridIntent intent))
                {
                    self.Incoming.Remove(actor.MemberId);
                    self.InstallIntent(actor, intent);
                }
                if (!actor.HasIntent) continue;
                if (actor.PathIndex < actor.Path.Count)
                {
                    self.BeginStep(actor, actor.Path[actor.PathIndex]);
                    continue;
                }
                if (actor.Cell != actor.Goal)
                {
                    actor.Motion = GridMotion.Waiting;
                    self.PlanRoute(actor, actor.Goal);
                    if (actor.Path.Count > 0) self.BeginStep(actor, actor.Path[0]);
                    continue;
                }
                if (actor.ActiveIntent.Action == GridAction.Move)
                    self.Result(actor, actor.ActiveIntent.CommandId, GridResultStatus.Executed, null, actor.ActiveIntent.OriginKind);
                else self.ExecuteInteraction(actor);
                actor.HasIntent = false; actor.Motion = GridMotion.Standing;
                self.Dirty = true;
            }
            self.ActorStepBuffer.Clear();
            self.AdvanceKitchenTransitions();
            self.AdvanceWork();
            self.AdvanceAutoWork();
            self.AdvanceBakeWork();
            self.AdvanceSupplyShipments();
            if ((RestaurantRuntime)self.Restaurant == null) self.CommitGridState();
        }

        public static void CommitGridState(this GridKitchenComponent self)
        {
            if (self.ProductLevel()?.PruneOrderIntents((Match)self.Match, self.Tick) == true)
            {
                self.Dirty = true;
            }
            if (self.PruneTargetPings())
            {
                self.Dirty = true;
            }
            if (self.PruneSupplyRequests())
            {
                self.Dirty = true;
            }
            if (self.Dirty)
            {
                ++self.Version;
                self.Dirty = false;
                self.SendCommitted();
                EventSystem.Instance.Publish((Scene)self.IScene, new GridWorldCommitted { Version = self.Version });
            }
        }

        private static void InstallIntent(this GridKitchenComponent self, GridActor actor, GridIntent intent)
        {
            if (intent.Action == GridAction.TakeContainerContent)
            {
                string error = self.CheckContainerExtraction(actor, self.Station(intent.TargetId), intent, out _, out _);
                if (error != null)
                {
                    self.Result(actor, intent.CommandId, GridResultStatus.Rejected, error, intent.OriginKind);
                    return;
                }
            }
            if (IsCleanupAction(intent.Action))
            {
                // 不让自动接近先暂停正在参与的输入，再把它当成合法垃圾。
                string error = self.CheckCleanup(actor, self.Station(intent.TargetId), intent, false, out _, out _, out _);
                if (error != null) { self.Result(actor, intent.CommandId, GridResultStatus.Rejected, error, intent.OriginKind); return; }
            }
            // 冲突开始在改路线前拒绝，避免先移动再因依赖失效暂停原工作。
            if (intent.Action is GridAction.StartStationWork or GridAction.StartHeldWork or GridAction.StartAssembly && actor.WorkId != 0)
            { self.Result(actor, intent.CommandId, GridResultStatus.Rejected, "WorkBusy", intent.OriginKind); return; }
            GridCell goal = intent.Cell;
            if (RequiresGridStation(intent.Action))
            {
                GridStation station = self.Station(intent.TargetId);
                if (station == null) { self.Result(actor, intent.CommandId, GridResultStatus.Rejected, "UnknownCounter", intent.OriginKind); return; }
                int shortest = int.MaxValue;
                foreach (GridCell adjacent in station.StationApproaches())
                {
                    int length=self.GridPathLength(actor.Cell,adjacent);
                    if(length>=0 && length<shortest)
                    {
                        shortest=length;goal=adjacent;
                    }
                }
                if (shortest == int.MaxValue) { self.Result(actor, intent.CommandId, GridResultStatus.Rejected, "Unreachable", intent.OriginKind); return; }
            }
            if (intent.Action is GridAction.PickupDrop or GridAction.StartHeldWork or GridAction.StartAssembly or GridAction.StopWork) goal = actor.Cell;
            if (!self.PlanRoute(actor, goal)) { self.Result(actor, intent.CommandId, GridResultStatus.Rejected, "Unreachable", intent.OriginKind); return; }
            self.ReleaseActorFrontWork(actor, false);
            if (intent.Action == GridAction.Move) self.PauseWork(actor);
            if (actor.HasIntent)
                self.Result(actor, actor.ActiveIntent.CommandId, GridResultStatus.Cancelled, "Replaced", actor.ActiveIntent.OriginKind);
            actor.ActiveIntent = intent; actor.HasIntent = true;
            self.Result(actor, intent.CommandId, GridResultStatus.PendingApproach, null, intent.OriginKind);
        }

        private static bool PlanRoute(this GridKitchenComponent self, GridActor actor, GridCell goal)
        {
            List<GridCell> route=self.ActorRouteBuffer;
            if(!self.FindGridPathInto(actor.Cell,goal,route))
            {
                return false;
            }
            int stopIndex = route.Count - 1;
            while (stopIndex >= 0 && !self.CanStop(route[stopIndex], actor)) --stopIndex;
            if (stopIndex < 0)
            {
                // 改目标时当前格可能只是满员格的经过点，保留原合法停点。
                if(!actor.HasStopReservation || !self.FindGridPathInto(actor.Cell,actor.StopCell,route))
                {
                    return false;
                }
                stopIndex = route.Count - 1;
            }
            actor.Goal = goal; actor.StopCell = route[stopIndex]; actor.HasStopReservation = true;
            actor.Path.Clear();
            for (int i = 1; i <= stopIndex; ++i) actor.Path.Add(route[i]);
            actor.PathIndex = 0;
            actor.Motion = actor.Path.Count == 0 && actor.Cell != goal ? GridMotion.Waiting : GridMotion.Standing;
            self.Dirty = true;
            return true;
        }

        public static bool CanStop(this GridKitchenComponent self, GridCell cell, GridActor except)
        {
            int count = 0;
            foreach (var reference in self.Actors.Values)
            {
                GridActor other=reference;
                if (other == null || other == except)
                {
                    continue;
                }
                if (other.HasStopReservation && other.StopCell == cell || other.Motion != GridMotion.Transit && other.Cell == cell) ++count;
            }
            return count < GridKitchenRules.StandingCapacity;
        }

        private static void BeginStep(this GridKitchenComponent self, GridActor actor, GridCell next)
        {
            actor.NextCell = next; actor.StepStartTick = self.Tick; actor.Motion = GridMotion.Transit;
            actor.Facing = Direction(actor.Cell, next); self.Dirty = true;
        }

        private static GridFacing Direction(GridCell from, GridCell to) => to.X > from.X ? GridFacing.Right : to.X < from.X ? GridFacing.Left : to.Y > from.Y ? GridFacing.Up : GridFacing.Down;
        public static int GridDistance(GridCell a, GridCell b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        public static GridCell[] Neighbours(GridCell cell) => GridStationGeometry.Neighbours(cell);

        public static List<GridCell> FindGridPath(this GridKitchenComponent self, GridCell from, GridCell to)
        {
            if(!self.SearchGridPath(from,to))
            {
                return null;
            }
            var result=new List<GridCell>(self.SearchResultLength(from,to));
            self.CopySearchPath(from,to,result);return result;
        }

        private static bool FindGridPathInto(this GridKitchenComponent self,GridCell from,GridCell to,List<GridCell> destination)
        {
            if(!self.SearchGridPath(from,to))
            {
                return false;
            }
            self.CopySearchPath(from,to,destination);return true;
        }

        private static int GridPathLength(this GridKitchenComponent self,GridCell from,GridCell to)
        {
            return self.SearchGridPath(from,to)?self.SearchResultLength(from,to):-1;
        }

        private static int SearchResultLength(this GridKitchenComponent self,GridCell from,GridCell to)
        {
            int first=from.Y*self.Width+from.X,current=to.Y*self.Width+to.X,length=1;
            while(current!=first)
            {
                current=self.SearchPrevious[current];++length;
            }
            return length;
        }

        private static void CopySearchPath(this GridKitchenComponent self,GridCell from,GridCell to,List<GridCell> destination)
        {
            // 搜索和复制之间没有 await、消息分发或观察事件；调用者只得到自己的路径。
            int first=from.Y*self.Width+from.X,current=to.Y*self.Width+to.X;
            destination.Clear();
            while(true)
            {
                destination.Add(new GridCell(current%self.Width,current/self.Width));
                if(current==first)
                {
                    break;
                }
                current=self.SearchPrevious[current];
            }
            destination.Reverse();
        }

        private static bool SearchGridPath(this GridKitchenComponent self,GridCell from,GridCell to)
        {
            if(!self.IsWalkable(from) || !self.IsWalkable(to))
            {
                return false;
            }
            Array.Fill(self.SearchPrevious,-1);
            int head=0,tail=0,first=from.Y*self.Width+from.X,target=to.Y*self.Width+to.X;
            self.SearchQueue[tail++]=first;self.SearchPrevious[first]=first;
            while(head<tail)
            {
                int current=self.SearchQueue[head++];
                if(current==target)
                {
                    return true;
                }
                int x=current%self.Width,y=current/self.Width;
                for(int direction=0;direction<4;direction++)
                {
                    // 保留上、右、下、左的确定顺序；GridCell 是值类型，不申请邻格数组。
                    GridCell next=direction switch{0=>new GridCell(x,y+1),1=>new GridCell(x+1,y),2=>new GridCell(x,y-1),_=>new GridCell(x-1,y)};
                    if(!self.IsWalkable(next))
                    {
                        continue;
                    }
                    int nextIndex=next.Y*self.Width+next.X;
                    if(self.SearchPrevious[nextIndex]!=-1)
                    {
                        continue;
                    }
                    self.SearchPrevious[nextIndex]=current;self.SearchQueue[tail++]=nextIndex;
                }
            }
            return false;
        }

        private static void ExecuteInteraction(this GridKitchenComponent self, GridActor actor)
        {
            GridIntent intent = actor.ActiveIntent;
            string error = null;
            if (intent.Action == GridAction.StartAssembly)
            {
                error = self.StartAssembly(actor, intent);
            }
            else if (intent.Action == GridAction.StartHeldWork) error = self.StartWork(actor, null, intent);
            else if (intent.Action == GridAction.StopWork) error = self.StopWork(actor, intent);
            else if (intent.Action == GridAction.PickupDrop)
            {
                GridItem item = self.Item(intent.TargetId);
                if (item == null || item.Location != GridItemLocation.Ground || item.Cell != actor.Cell) error = "PickupRequiresSameCell";
                else if (actor.HandItemId != 0) error = "HandFull";
                else { self.PauseWork(actor); item.Location = GridItemLocation.Hand; item.HolderId = actor.MemberId; actor.HandItemId = item.Id; }
            }
            else
            {
                GridStation station = self.Station(intent.TargetId);
                if (station == null || !self.AtStation(actor, station)) error = "CounterNotAdjacent";
                else if (IsCleanupAction(intent.Action)) error = self.CleanupAtFacility(actor, station, intent);
                else if (IsSupplyInteraction(intent.Action)) error = self.SupplyAtStation(actor, station, intent);
                else if (station.Kind == GridStationKind.CleanPool && intent.Action == GridAction.TakeCounter) error = self.TakeCleanUtensil(actor,station,intent);
                else if (station.Kind is GridStationKind.Table or GridStationKind.Wash or GridStationKind.DirtyPool or GridStationKind.CleanPool or GridStationKind.Trash) error = "RequiresFrontAction";
                else
                {
                    actor.Facing = Direction(actor.Cell, station.Cell);
                    if (intent.Action == GridAction.TakeCounter)
                    {
                        GridItem item = self.Item(station.ItemId);
                        if (actor.HandItemId != 0 || item == null || item.Id != intent.ExpectedItemId || item.Location != GridItemLocation.Counter || item.HolderId != station.Id) error = "TargetChanged";
                        else if (self.AutoProcess(item.AutoProcessId) is GridAutoProcess locked && locked.LockContainer && locked.State == GridAutoState.Cooking) error = "ProcessLocked";
                        else if (self.ItemWorking(item.Id)) error = "WorkBusy";
                        else { self.PauseWork(actor); station.ItemId = 0; actor.HandItemId = item.Id; item.Location = GridItemLocation.Hand; item.HolderId = actor.MemberId; }
                    }
                    else if (intent.Action == GridAction.AddToContainer) error = self.AddToContainer(actor, station, intent);
                    else if (intent.Action == GridAction.TakeContainerContent) error = self.TakeContainerContent(actor, station, intent);
                    else if (intent.Action == GridAction.StartStationWork) error = self.StartWork(actor, station, intent);
                    else if (intent.Action == GridAction.PourContents) error = self.PourContents(actor, station, intent);
                    else if (intent.Action is GridAction.StartAutoWork or GridAction.StartJuice or GridAction.StartMix or GridAction.StartExtract or GridAction.HeatMilk or GridAction.FoamMilk or GridAction.StartBrewTea or GridAction.StartMixTea) error = self.StartAutoWork(actor, station, intent);
                    else if (intent.Action is GridAction.PortionSoup or GridAction.PortionJuice or GridAction.PortionBatter or GridAction.PortionDrinkBase or GridAction.PortionTeaBase) error = self.PortionSoup(actor, station, intent);
                    else if (intent.Action is GridAction.BindDrink or GridAction.UnbindDrink) error = self.BindDrink(actor, station, intent);
                    else if (intent.Action == GridAction.Unmold) error = self.Unmold(actor, station, intent);
                    else if (intent.Action is GridAction.StartBake or GridAction.StartFry or GridAction.StartGrind or GridAction.StartSteam or GridAction.StartBoilPearls) error = self.StartBake(actor, station, intent);
                    else
                    {
                        GridItem item = self.Item(actor.HandItemId);
                        if (station.ItemId != 0 || item == null || item.Id != intent.ExpectedItemId || item.Location != GridItemLocation.Hand || item.HolderId != actor.MemberId) error = "TargetChanged";
                        else { self.PauseWork(actor); actor.HandItemId = 0; station.ItemId = item.Id; item.Location = GridItemLocation.Counter; item.HolderId = station.Id; item.Cell = station.Cell; }
                    }
                }
            }
            self.Result(actor, intent.CommandId, error == null ? GridResultStatus.Executed : GridResultStatus.Rejected, error, intent.OriginKind);
            self.Dirty = true;
        }

        private static void Result(this GridKitchenComponent self, GridActor actor, long commandId, GridResultStatus status, string reason, ProductCommandKind kind = ProductCommandKind.Grid)
        {
            if (commandId <= 0) return;
            if (commandId >= actor.LastResultCommandId)
            { actor.LastResultCommandId = commandId; actor.LastResultStatus = status; actor.LastResultReason = reason; }
            Level level = self.ProductLevel();
            var value = new GridResultValue(actor.MemberId, actor.Generation, commandId, status, reason, level?.Id ?? 0, level?.Generation ?? 0);
            self.Results.Add(value);
            if (kind == ProductCommandKind.Grid) self.PendingResults.Add(value);
            Participant member = ((Match)self.Match).GetChild<Participant>(actor.MemberId);
            HostPeer peer = member?.BoundPeer ?? default;
            if (peer != null && peer.Generation == actor.Generation)
            {
                var context = new ProductCommandContext(((Match)self.Match).MatchId, self.WorldId, self.WorldGeneration,
                    level?.Id ?? 0, level?.Generation ?? 0, actor.Generation, commandId);
                peer.RecordProductResult(context, kind, status, reason);
            }
            if (self.Results.Count > 64) self.Results.RemoveAt(0);
            self.Dirty = true;
        }
    }

    [EntitySystemOf(typeof(GridActor))]
    [FriendOf(typeof(GridActor))]
    public static partial class GridActorSystem
    {
        [EntitySystem]
        private static void Awake(this GridActor self)
        {
            self.MemberId = 0; self.Generation = 0; self.Cell = default; self.NextCell = default;
            self.StepStartTick = 0; self.Motion = GridMotion.Standing; self.Facing = GridFacing.Down; self.HandItemId = 0; self.WorkId = 0;
            self.FrontWorkId = 0; self.FrontClaimSerial = 0;
            self.Goal = default; self.StopCell = default; self.HasStopReservation = false;
            self.Path.Clear(); self.PathIndex = 0; self.ActiveIntent = default; self.HasIntent = false;
            self.LastReceivedCommandId = 0; self.LastReceivedIntent = default;
            self.LastResultCommandId = 0; self.LastResultStatus = default; self.LastResultReason = null;
        }
    }

    [EntitySystemOf(typeof(GridStation))]
    [FriendOf(typeof(GridStation))]
    public static partial class GridStationSystem
    {
        [EntitySystem]
        private static void Awake(this GridStation self) { self.InitialLayoutId = 0; self.UpgradeDefinitionId = self.UpgradeLevel = 0; self.PoolItemIds.Clear(); }
    }
    [EntitySystemOf(typeof(GridItem))]
    [FriendOf(typeof(GridItem))]
    public static partial class GridItemSystem
    {
        [EntitySystem]
        private static void Awake(this GridItem self) => self.Hygiene = ItemHygiene.Clean;
    }
}
#endif
