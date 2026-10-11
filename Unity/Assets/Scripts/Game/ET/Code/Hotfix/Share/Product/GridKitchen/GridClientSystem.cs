#if !DOTNET
using System;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace ET
{
    [EntitySystemOf(typeof(GridClientComponent))]
    [FriendOf(typeof(GridClientComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    public static partial class GridClientSystem
    {
        [EntitySystem]
        private static void Awake(this GridClientComponent self) => self.ResetGrid();
        [EntitySystem]
        private static void Destroy(this GridClientComponent self) => self.ResetGrid();

        public static void ResetGrid(this GridClientComponent self)
        {
            self.Confirmed = false; self.MatchId = null; self.WorldId = null; self.WorldGeneration = 0;
            self.BindingGeneration = 0; self.Version = 0; self.Tick = 0; self.MemberId = 0;
            self.NextCommandId = 0; self.LastCommandId = 0; self.LastStatus = default; self.LastReason = null;
            self.Actors.Clear(); self.Stations.Clear(); self.Items.Clear(); self.Processes.Clear();
        }

        public static void ConfirmGridProjection(this GridClientComponent self)
        {
            ClientSessionComponent client = self.GetParent<ClientSessionComponent>();
            self.Confirmed = client.Stage == ProductClientStage.Joined && self.MatchId == client.MatchId &&
                self.BindingGeneration == client.BindingGeneration && self.MemberId == client.MemberId &&
                self.Actors.TryGetValue(self.MemberId, out GridActorProjection actor) && actor.Generation == client.BindingGeneration;
            if (self.Confirmed) EventSystem.Instance.Publish((Scene)self.IScene, new GridProjectionApplied { Version = self.Version });
        }

        public static void ApplyGridSnapshot(this GridClientComponent self, GridWorldSnapshot snapshot)
        {
            ClientSessionComponent client = self.GetParent<ClientSessionComponent>();
            if (client.Stage is not (ProductClientStage.Joining or ProductClientStage.Joined) ||
                string.IsNullOrEmpty(snapshot.MatchId) || string.IsNullOrEmpty(snapshot.WorldId) || snapshot.WorldGeneration <= 0 ||
                snapshot.BindingGeneration <= 0 || snapshot.MemberId <= 0 || snapshot.Version <= 0 || snapshot.StepTicks <= 0 || snapshot.TickMilliseconds <= 0) return;
            if (client.Stage == ProductClientStage.Joined && (snapshot.MatchId != client.MatchId || snapshot.BindingGeneration != client.BindingGeneration || snapshot.MemberId != client.MemberId)) return;
            if (self.WorldId != null && (self.WorldId != snapshot.WorldId || self.WorldGeneration != snapshot.WorldGeneration || snapshot.Version <= self.Version)) return;
            // 先完整复制再安装；不保留 wire DTO、Host EntityRef 或消息集合。
            var actors = snapshot.Actors.ToDictionary(value => value.MemberId, value => new GridActorProjection(value.MemberId, value.Generation,
                new GridCell(value.X, value.Y), new GridCell(value.NextX, value.NextY), value.StepStartTick, (GridMotion)value.Motion,
                value.Facing, value.HandItemId, new GridCell(value.GoalX, value.GoalY),
                Array.AsReadOnly(value.Path.Select(cell => new GridCell(cell.X, cell.Y)).ToArray()), value.LastCommandId, (GridResultStatus)value.LastStatus, value.LastReason, value.WorkId));
            var stations = snapshot.Stations.ToDictionary(value => value.StationId, value => new GridStationProjection(value.StationId, new GridCell(value.X, value.Y), value.Name, value.ItemId, (GridStationKind)value.Kind));
            var items = snapshot.Items.ToDictionary(value => value.ItemId, value => new GridItemProjection(value.ItemId, value.Name, (GridItemLocation)value.Location,
                value.HolderId, new GridCell(value.X, value.Y), Array.AsReadOnly(value.Contents.ToArray()), value.DefinitionId));
            var processes = snapshot.Processes.ToDictionary(v => v.ProcessId, v => new GridProcessProjection(v.ProcessId, v.RecipeId, v.InputItemId, v.TargetItemId, v.StationId, v.RequiredTicks, v.ProgressTicks, (GridWorkState)v.State, v.WorkerMemberId, v.WorkerGeneration, v.OutputItemId));
            self.MatchId = snapshot.MatchId; self.WorldId = snapshot.WorldId; self.WorldGeneration = snapshot.WorldGeneration;
            self.BindingGeneration = snapshot.BindingGeneration; self.MemberId = snapshot.MemberId; self.Version = snapshot.Version;
            self.Tick = snapshot.Tick; self.Width = snapshot.Width; self.Height = snapshot.Height; self.KitchenBottom = snapshot.KitchenBottom;
            self.TickMilliseconds = snapshot.TickMilliseconds; self.StepTicks = snapshot.StepTicks; self.ReceivedFrameTime = TimeInfo.Instance.FrameTime;
            self.Actors.Clear(); foreach (var value in actors) self.Actors.Add(value.Key, value.Value);
            self.Stations.Clear(); foreach (var value in stations) self.Stations.Add(value.Key, value.Value);
            self.Items.Clear(); foreach (var value in items) self.Items.Add(value.Key, value.Value);
            self.Processes.Clear(); foreach (var value in processes) self.Processes.Add(value.Key, value.Value);
            if (actors.TryGetValue(self.MemberId, out GridActorProjection own) && own.LastCommandId >= self.LastCommandId)
            { self.LastCommandId = own.LastCommandId; self.LastStatus = own.LastStatus; self.LastReason = own.LastReason; self.NextCommandId = Math.Max(self.NextCommandId, own.LastCommandId); }
            self.ConfirmGridProjection();
        }

        public static void ApplyGridResult(this GridClientComponent self, GridCommandResult result)
        {
            if (!self.Confirmed || result.MatchId != self.MatchId || result.WorldId != self.WorldId || result.WorldGeneration != self.WorldGeneration ||
                result.BindingGeneration != self.BindingGeneration || result.Version < self.Version || result.CommandId < self.LastCommandId) return;
            self.LastCommandId = result.CommandId; self.LastStatus = (GridResultStatus)result.Status; self.LastReason = result.Reason;
            EventSystem.Instance.Publish((Scene)self.IScene, new GridProjectionApplied { Version = self.Version });
        }

        public static GridCommandRequest CreateGridCommand(this GridClientComponent self, GridAction action, GridCell cell = default, long target = 0, long expected = 0, long expectedTarget = 0, long process = 0)
        {
            if (!self.Confirmed) throw new InvalidOperationException("Grid projection is not ready");
            long command = checked(++self.NextCommandId);
            self.LastCommandId = command; self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            return ProductMessages.Create<GridCommandRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = self.MatchId; value.WorldId = self.WorldId;
                value.WorldGeneration = self.WorldGeneration; value.BindingGeneration = self.BindingGeneration; value.CommandId = command;
                value.Action = (int)action; value.X = cell.X; value.Y = cell.Y; value.TargetId = target; value.ExpectedItemId = expected; value.ExpectedTargetItemId = expectedTarget; value.ExpectedProcessId = process;
            });
        }

        public static GridIntent CounterIntent(this GridClientComponent self, long target)
        {
            GridActorProjection actor = self.Actors[self.MemberId];
            GridStationProjection station = self.Stations[target];
            GridProcessProjection process = self.Processes.Values.FirstOrDefault(p => p.InputItemId == station.ItemId && p.State != GridWorkState.Completed);
            if (actor.WorkId != 0 && process.Id == actor.WorkId)
                return new GridIntent(0, GridAction.StopWork, station.Cell, target, process.InputItemId, process.TargetItemId, process.Id);
            if (actor.HandItemId != 0)
                return new GridIntent(0, station.ItemId == 0 ? GridAction.PlaceCounter : GridAction.AddToContainer, station.Cell, target, actor.HandItemId, station.ItemId);
            if (station.Kind == GridStationKind.Chopping && self.Items.TryGetValue(station.ItemId, out GridItemProjection input) &&
                Tables.Instance.DTGridManualRecipeConfig.DataList.Any(r => r.Mode == 1 && r.InputDefinitionId == input.DefinitionId))
                return new GridIntent(0, GridAction.StartStationWork, station.Cell, target, input.Id, input.Id, process.Id);
            return new GridIntent(0, GridAction.TakeCounter, station.Cell, target, station.ItemId);
        }

        public static string ItemDescription(this GridClientComponent self, long id)
        {
            if (!self.Items.TryGetValue(id, out GridItemProjection item)) return "空";
            return item.Name + (item.DefinitionId == GridKitchenRules.Bowl ? "（" + (item.Contents.Count == 0 ? "空" : string.Join("、", item.Contents.Select(value => self.Items.TryGetValue(value, out GridItemProjection material) ? material.Name : "?"))) + "）" : "");
        }

        public static string WorkDescription(GridProcessProjection p)
        {
            if (p.Id == 0) return "";
            DRGridManualRecipeConfig recipe = Tables.Instance.DTGridManualRecipeConfig.Get(p.RecipeId);
            string name = recipe.Mode == 1 ? "切配" : "打蛋";
            return $"{name}：{(p.State == GridWorkState.Working ? "加工中" : p.State == GridWorkState.Paused ? "暂停" : "完成")} {p.ProgressTicks}/{p.RequiredTicks}";
        }
    }

    [MessageHandler(SceneType.ProductClient)]
    public sealed class GridSnapshotHandler : MessageHandler<ClientSessionComponent, GridWorldSnapshot>
    {
        protected override UniTask Run(ClientSessionComponent client, GridWorldSnapshot message)
        {
            try { client.GetComponent<GridClientComponent>()?.ApplyGridSnapshot(message); }
            finally { message.Dispose(); }
            return UniTask.CompletedTask;
        }
    }
    [MessageHandler(SceneType.ProductClient)]
    public sealed class GridResultHandler : MessageHandler<ClientSessionComponent, GridCommandResult>
    {
        protected override UniTask Run(ClientSessionComponent client, GridCommandResult message)
        {
            try { client.GetComponent<GridClientComponent>()?.ApplyGridResult(message); }
            finally { message.Dispose(); }
            return UniTask.CompletedTask;
        }
    }
}
#endif
