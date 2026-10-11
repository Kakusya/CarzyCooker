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
    public static partial class GridKitchenSystem
    {
        [EntitySystem]
        private static void Awake(this GridKitchenComponent self)
        {
            self.WorldId = Guid.NewGuid().ToString("N");
            self.WorldGeneration = self.InstanceId;
            self.Tick = 0;
            self.Version = 0;
            self.LastFrameTime = TimeInfo.Instance.FrameTime;
            self.Accumulator = 0;
            self.Stopped = false;
            self.Dirty = true;
            self.Blocked.Clear(); self.Actors.Clear(); self.Stations.Clear(); self.Items.Clear();
            self.MemberChanges.Clear(); self.Incoming.Clear(); self.Results.Clear(); self.PendingResults.Clear();
            self.AddStation(new GridCell(1, 11), "备料台", "番茄");
            self.AddStation(new GridCell(3, 11), "碗台", "碗");
            self.AddStation(new GridCell(5, 11), "食材台", "鸡蛋");
            self.AddStation(new GridCell(3, 8), "交接台", null);
            self.AddStation(new GridCell(5, 8), "空台面", null);
        }

        private static void AddStation(this GridKitchenComponent self, GridCell cell, string name, string itemName)
        {
            GridStation station = self.AddChild<GridStation>();
            station.Cell = cell; station.Name = name; station.ItemId = 0;
            self.Stations.Add(station.Id, station);
            self.Blocked.Add(cell);
            if (itemName == null) return;
            GridItem item = self.AddChild<GridItem>();
            item.Name = itemName; item.Location = GridItemLocation.Counter;
            item.HolderId = station.Id; item.Cell = cell; item.Contents.Clear();
            if (itemName == "碗") item.Contents.Add("占位食材");
            station.ItemId = item.Id;
            self.Items.Add(item.Id, item);
        }

        [EntitySystem]
        private static void Update(this GridKitchenComponent self)
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
        private static void Destroy(this GridKitchenComponent self) => self.StopGrid();

        public static void StopGrid(this GridKitchenComponent self)
        {
            if (self.Stopped) return;
            self.Stopped = true;
            foreach (GridActor actor in self.OrderedActors())
                if (actor.HasIntent) self.Result(actor, actor.ActiveIntent.CommandId, GridResultStatus.Cancelled, "WorldStopped");
            self.Incoming.Clear(); self.MemberChanges.Clear();
        }

        public static bool IsWalkable(this GridKitchenComponent self, GridCell cell) =>
            cell.X > 0 && cell.X < GridKitchenRules.Width - 1 && cell.Y > 0 && cell.Y < GridKitchenRules.Height - 1 && !self.Blocked.Contains(cell);

        public static GridActor Actor(this GridKitchenComponent self, int memberId) => self.Actors.TryGetValue(memberId, out EntityRef<GridActor> value) ? value : null;
        public static GridStation Station(this GridKitchenComponent self, long id) => self.Stations.TryGetValue(id, out EntityRef<GridStation> value) ? value : null;
        public static GridItem Item(this GridKitchenComponent self, long id) => self.Items.TryGetValue(id, out EntityRef<GridItem> value) ? value : null;
        private static GridActor[] OrderedActors(this GridKitchenComponent self) => self.Actors.OrderBy(pair => pair.Key).Select(pair => (GridActor)pair.Value).Where(actor => actor != null).ToArray();

        public static bool HasBinding(this GridKitchenComponent self, int memberId, long generation)
        {
            Participant member = self.GetParent<Match>().GetChild<Participant>(memberId);
            HostPeer peer = member?.BoundPeer ?? default;
            return member != null && member.Online && member.BindingGeneration == generation && peer != null && peer.Generation == generation && peer.EntryOpen && !peer.Closing;
        }

        public static void QueueGridMember(this GridKitchenComponent self, int memberId, long generation, bool joined)
        {
            if (!self.Stopped) self.MemberChanges.Enqueue(new GridMemberChange(memberId, generation, joined));
        }

        private static void ApplyMembers(this GridKitchenComponent self)
        {
            while (self.MemberChanges.Count > 0)
            {
                GridMemberChange change = self.MemberChanges.Dequeue();
                GridActor actor = self.Actor(change.MemberId);
                if (!change.Joined)
                {
                    if (actor == null || actor.Generation != change.Generation) continue;
                    if (actor.HasIntent) self.Result(actor, actor.ActiveIntent.CommandId, GridResultStatus.Cancelled, "MemberLeft");
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
                for (int y = 10; y > 0 && !spawned; --y)
                for (int x = 1; x < GridKitchenRules.Width - 1 && !spawned; ++x)
                {
                    GridCell cell = new(x, y);
                    if (!self.IsWalkable(cell) || !self.CanStop(cell, null)) continue;
                    actor = self.AddChild<GridActor>();
                    actor.MemberId = change.MemberId; actor.Generation = change.Generation;
                    actor.Cell = cell; actor.NextCell = cell; actor.Goal = cell; actor.StopCell = cell;
                    actor.HasStopReservation = true; actor.Motion = GridMotion.Standing;
                    self.Actors.Add(actor.MemberId, actor);
                    spawned = true; self.Dirty = true;
                }
                if (!spawned) self.Results.Add(new GridResultValue(change.MemberId, change.Generation, 0, GridResultStatus.Rejected, "MapFull"));
            }
        }

        public static string SubmitGridIntent(this GridKitchenComponent self, int memberId, long generation, GridIntent intent)
        {
            GridActor actor = self.Actor(memberId);
            if (self.Stopped || !self.HasBinding(memberId, generation) || actor == null || actor.Generation != generation) return "EntryClosed";
            if (intent.CommandId <= 0 || intent.Action < GridAction.Move || intent.Action > GridAction.PickupDrop) return "InvalidCommand";
            if (intent.CommandId <= actor.LastReceivedCommandId)
                return intent.CommandId == actor.LastReceivedCommandId && intent.Equals(actor.LastReceivedIntent) ? null : "ExpiredCommand";
            if (intent.Action == GridAction.Move && !self.IsWalkable(intent.Cell)) return "BlockedCell";
            if (intent.Action is GridAction.TakeCounter or GridAction.PlaceCounter && self.Station(intent.TargetId) == null) return "UnknownCounter";
            if (intent.Action == GridAction.PickupDrop && self.Item(intent.TargetId) == null) return "UnknownItem";
            // 接收元数据不移动角色/物品；世界修改留给固定 Tick。
            actor.LastReceivedCommandId = intent.CommandId; actor.LastReceivedIntent = intent;
            if (self.Incoming.TryGetValue(memberId, out GridIntent replaced))
                self.Result(actor, replaced.CommandId, GridResultStatus.Cancelled, "Replaced");
            self.Incoming[memberId] = intent;
            return null;
        }

        public static void AdvanceGridTick(this GridKitchenComponent self)
        {
            if (self.Stopped) return;
            ++self.Tick;
            self.ApplyMembers();
            foreach (GridActor actor in self.OrderedActors())
            {
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
                    self.Result(actor, actor.ActiveIntent.CommandId, GridResultStatus.Executed, null);
                else self.ExecuteInteraction(actor);
                actor.HasIntent = false; actor.Motion = GridMotion.Standing;
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
            GridCell goal = intent.Cell;
            if (intent.Action is GridAction.TakeCounter or GridAction.PlaceCounter)
            {
                GridStation station = self.Station(intent.TargetId);
                if (station == null) { self.Result(actor, intent.CommandId, GridResultStatus.Rejected, "UnknownCounter"); return; }
                List<GridCell> shortest = null;
                foreach (GridCell adjacent in Neighbours(station.Cell))
                {
                    List<GridCell> path = self.FindGridPath(actor.Cell, adjacent);
                    if (path != null && (shortest == null || path.Count < shortest.Count)) { shortest = path; goal = adjacent; }
                }
                if (shortest == null) { self.Result(actor, intent.CommandId, GridResultStatus.Rejected, "Unreachable"); return; }
            }
            if (intent.Action == GridAction.PickupDrop) goal = actor.Cell;
            if (!self.PlanRoute(actor, goal)) { self.Result(actor, intent.CommandId, GridResultStatus.Rejected, "Unreachable"); return; }
            if (actor.HasIntent)
                self.Result(actor, actor.ActiveIntent.CommandId, GridResultStatus.Cancelled, "Replaced");
            actor.ActiveIntent = intent; actor.HasIntent = true;
            self.Result(actor, intent.CommandId, GridResultStatus.PendingApproach, null);
        }

        private static bool PlanRoute(this GridKitchenComponent self, GridActor actor, GridCell goal)
        {
            List<GridCell> route = self.FindGridPath(actor.Cell, goal);
            if (route == null) return false;
            int stopIndex = route.Count - 1;
            while (stopIndex >= 0 && !self.CanStop(route[stopIndex], actor)) --stopIndex;
            if (stopIndex < 0)
            {
                // 改目标时当前格可能只是满员格的经过点，保留原合法停点。
                route = actor.HasStopReservation ? self.FindGridPath(actor.Cell, actor.StopCell) : null;
                if (route == null) return false;
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
            foreach (GridActor other in self.OrderedActors())
            {
                if (other == except) continue;
                if (other.HasStopReservation && other.StopCell == cell || other.Motion != GridMotion.Transit && other.Cell == cell) ++count;
            }
            return count < GridKitchenRules.StandingCapacity;
        }

        private static void BeginStep(this GridKitchenComponent self, GridActor actor, GridCell next)
        {
            actor.NextCell = next; actor.StepStartTick = self.Tick; actor.Motion = GridMotion.Transit;
            actor.Facing = Direction(actor.Cell, next); self.Dirty = true;
        }

        private static int Direction(GridCell from, GridCell to) => to.X > from.X ? 1 : to.X < from.X ? 3 : to.Y > from.Y ? 0 : 2;
        public static int GridDistance(GridCell a, GridCell b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        public static GridCell[] Neighbours(GridCell cell) => new[] { new GridCell(cell.X, cell.Y + 1), new GridCell(cell.X + 1, cell.Y), new GridCell(cell.X, cell.Y - 1), new GridCell(cell.X - 1, cell.Y) };

        public static List<GridCell> FindGridPath(this GridKitchenComponent self, GridCell from, GridCell to)
        {
            if (!self.IsWalkable(from) || !self.IsWalkable(to)) return null;
            var queue = new Queue<GridCell>(); var previous = new Dictionary<GridCell, GridCell>();
            queue.Enqueue(from); previous.Add(from, from);
            while (queue.Count > 0)
            {
                GridCell cell = queue.Dequeue();
                if (cell == to)
                {
                    var result = new List<GridCell> { cell };
                    while (cell != from) { cell = previous[cell]; result.Add(cell); }
                    result.Reverse(); return result;
                }
                foreach (GridCell next in Neighbours(cell))
                {
                    if (!self.IsWalkable(next) || previous.ContainsKey(next)) continue;
                    previous.Add(next, cell); queue.Enqueue(next);
                }
            }
            return null;
        }

        private static void ExecuteInteraction(this GridKitchenComponent self, GridActor actor)
        {
            GridIntent intent = actor.ActiveIntent;
            string error = null;
            if (intent.Action == GridAction.PickupDrop)
            {
                GridItem item = self.Item(intent.TargetId);
                if (item == null || item.Location != GridItemLocation.Ground || item.Cell != actor.Cell) error = "PickupRequiresSameCell";
                else if (actor.HandItemId != 0) error = "HandFull";
                else { item.Location = GridItemLocation.Hand; item.HolderId = actor.MemberId; actor.HandItemId = item.Id; }
            }
            else
            {
                GridStation station = self.Station(intent.TargetId);
                if (station == null || GridDistance(actor.Cell, station.Cell) != 1) error = "CounterNotAdjacent";
                else
                {
                    actor.Facing = Direction(actor.Cell, station.Cell);
                    if (intent.Action == GridAction.TakeCounter)
                    {
                        GridItem item = self.Item(station.ItemId);
                        if (actor.HandItemId != 0 || item == null || item.Id != intent.ExpectedItemId || item.Location != GridItemLocation.Counter || item.HolderId != station.Id) error = "TargetChanged";
                        else { station.ItemId = 0; actor.HandItemId = item.Id; item.Location = GridItemLocation.Hand; item.HolderId = actor.MemberId; }
                    }
                    else
                    {
                        GridItem item = self.Item(actor.HandItemId);
                        if (station.ItemId != 0 || item == null || item.Id != intent.ExpectedItemId || item.Location != GridItemLocation.Hand || item.HolderId != actor.MemberId) error = "TargetChanged";
                        else { actor.HandItemId = 0; station.ItemId = item.Id; item.Location = GridItemLocation.Counter; item.HolderId = station.Id; item.Cell = station.Cell; }
                    }
                }
            }
            self.Result(actor, intent.CommandId, error == null ? GridResultStatus.Executed : GridResultStatus.Rejected, error);
            self.Dirty = true;
        }

        private static void Result(this GridKitchenComponent self, GridActor actor, long commandId, GridResultStatus status, string reason)
        {
            if (commandId >= actor.LastResultCommandId)
            { actor.LastResultCommandId = commandId; actor.LastResultStatus = status; actor.LastResultReason = reason; }
            self.Results.Add(new GridResultValue(actor.MemberId, actor.Generation, commandId, status, reason));
            self.PendingResults.Add(new GridResultValue(actor.MemberId, actor.Generation, commandId, status, reason));
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
            self.StepStartTick = 0; self.Motion = GridMotion.Standing; self.Facing = 2; self.HandItemId = 0;
            self.Goal = default; self.StopCell = default; self.HasStopReservation = false;
            self.Path.Clear(); self.PathIndex = 0; self.ActiveIntent = default; self.HasIntent = false;
            self.LastReceivedCommandId = 0; self.LastReceivedIntent = default;
            self.LastResultCommandId = 0; self.LastResultStatus = default; self.LastResultReason = null;
        }
    }

    [EntitySystemOf(typeof(GridStation))]
    public static partial class GridStationSystem { [EntitySystem] private static void Awake(this GridStation self) { } }
    [EntitySystemOf(typeof(GridItem))]
    public static partial class GridItemSystem { [EntitySystem] private static void Awake(this GridItem self) { } }
}
#endif
