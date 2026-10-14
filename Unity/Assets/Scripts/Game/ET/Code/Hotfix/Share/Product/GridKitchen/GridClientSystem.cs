#if !DOTNET
using System;
using System.Collections.Generic;
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
            self.LastProductOutcomeSerial = 0; self.LastKitchenOutcomeSerial = 0;
            self.LastProductOutcome = default; self.LastKitchenOutcome = default;
            self.LastShipmentOutcomeSerial = 0; self.LastShipmentOutcome = default;
            self.LastFrontWorkOutcomeSerial = 0; self.LastFrontWorkOutcome = default;
            self.LastBuffPreparedSerial = 0; self.LastBuffPrepared = default;
            self.LastRenovationPreparedSerial = 0; self.LastRenovationPrepared = default;
            self.LastLevelPhaseSerial = 0; self.LastLevelPhase = default;
            self.LevelId = 0; self.LevelGeneration = 0;
            self.LayoutVersion = 0;
            self.AllowedDishIds = Array.Empty<int>();
            self.LevelPhase = default; self.LevelPaused = false; self.LevelOperatingTicks = 0; self.LevelElapsedTicks = 0;
            self.LevelScore = 0; self.LevelStars = 0; self.LevelConfigId = 0; self.NextLevelConfigId = 0; self.LastCommandKind = default;
            self.ServedOrders = self.UnfulfilledOrders = 0;
            self.Actors.Clear(); self.Stations.Clear(); self.Items.Clear(); self.Processes.Clear(); self.AutoProcesses.Clear();
            self.BakeProcesses.Clear();
            self.Companions.Clear(); self.FrontWorks.Clear(); self.Customers.Clear(); self.Orders.Clear(); self.Tables.Clear();
            self.WaitingCustomers = Array.Empty<long>(); self.SkippedArrivals = 0;
            self.BuffSeed=0;self.BuffDrawVersion=0;self.SelectedBuffId=0;self.RequireBuffChoice=false;self.BuffCandidates=Array.Empty<int>();
            self.SupplySources.Clear(); self.SupplyShipments.Clear();
            self.RenovationCandidates = Array.Empty<int>(); self.SelectedRenovationId = 0;
            self.RenovationFailure = null;
            self.SelectedUpgradeStationId = 0; self.PendingUpgradeExpectedLevel = 0; self.UpgradeTargets = Array.Empty<RenovationUpgradeTarget>();
            self.RenovationChoiceVersion = 0; self.RenovationLevelId = 0; self.RenovationLevelGeneration = 0; self.RenovationApplied.Clear();
            self.SaveEnabled = false; self.SaveResultId = null; self.SaveFailure = null; self.LastSavedSlotId = null; self.SaveState = default;
            self.CanControlRestaurant = false;
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
            if (self.LevelGeneration > 0 && (snapshot.LevelGeneration < self.LevelGeneration ||
                snapshot.LevelGeneration == self.LevelGeneration && snapshot.LevelId != self.LevelId)) return;
            // 先完整复制再安装；不保留 wire DTO、Host EntityRef 或消息集合。
            var actors = snapshot.Actors.ToDictionary(value => value.MemberId, value => new GridActorProjection(value.MemberId, value.Generation,
                new GridCell(value.X, value.Y), new GridCell(value.NextX, value.NextY), value.StepStartTick, (GridMotion)value.Motion,
                value.Facing, value.HandItemId, new GridCell(value.GoalX, value.GoalY),
                Array.AsReadOnly(value.Path.Select(cell => new GridCell(cell.X, cell.Y)).ToArray()), value.LastCommandId, (GridResultStatus)value.LastStatus, value.LastReason, value.WorkId, value.FrontWorkId, value.FrontClaimSerial));
            var stations = snapshot.Stations.ToDictionary(value => value.StationId, value => new GridStationProjection(value.StationId,
                new GridCell(value.X, value.Y), value.Name, value.ItemId, (GridStationKind)value.Kind, value.AutoProcessId,
                value.DefinitionId, value.Orientation, value.ItemSlotCapacity, (GridStationCapability)value.Capabilities,
                Array.AsReadOnly(value.Footprint.Select(cell => new GridCell(cell.X, cell.Y)).ToArray()),
                Array.AsReadOnly(value.Approaches.Select(cell => new GridCell(cell.X, cell.Y)).ToArray()), value.BakeProcessId, Array.AsReadOnly(value.PoolItemIds.ToArray()), value.WaterDefinitionId, value.WaterUnits, value.UpgradeDefinitionId, value.UpgradeLevel));
            var items = snapshot.Items.ToDictionary(value => value.ItemId, value => new GridItemProjection(value.ItemId, value.Name, (GridItemLocation)value.Location,
                value.HolderId, new GridCell(value.X, value.Y), Array.AsReadOnly(value.Contents.ToArray()), value.DefinitionId, value.AutoProcessId, value.BakeProcessId, (ItemHygiene)value.Hygiene, (GridMaterialForm)value.Form, value.Discardable, value.BoundOrderId));
            var processes = snapshot.Processes.ToDictionary(v => v.ProcessId, v => new GridProcessProjection(v.ProcessId, v.RecipeId, v.InputItemId, v.TargetItemId, v.StationId, v.RequiredTicks, v.ProgressTicks, (GridWorkState)v.State, v.WorkerMemberId, v.WorkerGeneration, v.OutputItemId));
            var automatic = snapshot.AutoProcesses.ToDictionary(value => value.ProcessId, value => new GridAutoProcessProjection(value.ProcessId, value.RecipeId,
                value.PotItemId, value.StartStationId, value.StartedTick, value.RequiredTicks, value.ProgressTicks, value.OutputDefinitionId,
                value.TotalPortions, value.RemainingPortions, value.ServingContainerDefinitionId, (GridAutoState)value.State, Array.AsReadOnly(value.InputItemIds.ToArray()), value.RequiredCapability, value.LockContainer));
            var baking = snapshot.BakeProcesses.ToDictionary(value => value.ProcessId, value => new GridBakeProjection(value.ProcessId, value.RecipeId,
                value.StationId, value.InputItemId, value.OutputDefinitionId, value.RequiredTicks, value.ProgressTicks, value.OutputItemId, (GridBakeState)value.State, value.StartedTick));
            var companions = snapshot.Companions.ToDictionary(value => value.RoleId, value => new GridCompanionProjection(value));
            var frontWorks = snapshot.FrontWorks.ToDictionary(value => value.WorkId, value => new GridFrontWorkProjection(value));
            var customers = snapshot.Customers.ToDictionary(value => value.CustomerId, value => new GridCustomerProjection(value));
            var orders = snapshot.Orders.ToDictionary(value => value.OrderId, value => new GridOrderProjection(value));
            var tables = snapshot.Tables.ToDictionary(value => value.StationId, value => new GridTableProjection(value));
            var waiting = Array.AsReadOnly(snapshot.WaitingCustomers.ToArray());
            var buffCandidates=Array.AsReadOnly(snapshot.BuffCandidates.ToArray());
            var supplySources = snapshot.SupplySources.ToDictionary(value => value.SourceId,
                value => new SupplySourceProjection(value.SourceId, value.RemainingPackages, value.ReceivingStationId));
            var supplyShipments = snapshot.SupplyShipments.ToDictionary(value => value.ShipmentId, value => new SupplyShipmentProjection(value));
            var renovationCandidates = Array.AsReadOnly(snapshot.RenovationCandidates.ToArray());
            var renovationApplied = snapshot.RenovationApplied.ToDictionary(value => value.DefinitionId,
                value => (IReadOnlyList<long>)Array.AsReadOnly(value.StationIds.ToArray()));
            self.MatchId = snapshot.MatchId; self.WorldId = snapshot.WorldId; self.WorldGeneration = snapshot.WorldGeneration;
            self.BindingGeneration = snapshot.BindingGeneration; self.MemberId = snapshot.MemberId; self.Version = snapshot.Version;
            self.LevelId = snapshot.LevelId; self.LevelGeneration = snapshot.LevelGeneration;
            self.LayoutVersion = snapshot.LayoutVersion;
            self.AllowedDishIds = Array.AsReadOnly(snapshot.AllowedDishIds.ToArray());
            self.LevelPhase = (LevelPhase)snapshot.LevelPhase; self.LevelPaused = snapshot.LevelPaused;
            self.LevelOperatingTicks = snapshot.LevelOperatingTicks; self.LevelElapsedTicks = snapshot.LevelElapsedTicks;
            self.LevelScore = snapshot.LevelScore; self.LevelStars = snapshot.LevelStars;
            self.ServedOrders = snapshot.ServedOrders; self.UnfulfilledOrders = snapshot.UnfulfilledOrders;
            self.LevelConfigId = snapshot.LevelConfigId; self.NextLevelConfigId = snapshot.NextLevelConfigId;
            self.Tick = snapshot.Tick; self.Width = snapshot.Width; self.Height = snapshot.Height; self.KitchenBottom = snapshot.KitchenBottom;
            self.TickMilliseconds = snapshot.TickMilliseconds; self.StepTicks = snapshot.StepTicks; self.ReceivedFrameTime = TimeInfo.Instance.FrameTime;
            self.Actors.Clear(); foreach (var value in actors) self.Actors.Add(value.Key, value.Value);
            self.Stations.Clear(); foreach (var value in stations) self.Stations.Add(value.Key, value.Value);
            self.Items.Clear(); foreach (var value in items) self.Items.Add(value.Key, value.Value);
            self.Processes.Clear(); foreach (var value in processes) self.Processes.Add(value.Key, value.Value);
            self.AutoProcesses.Clear(); foreach (var value in automatic) self.AutoProcesses.Add(value.Key, value.Value);
            self.BakeProcesses.Clear(); foreach (var value in baking) self.BakeProcesses.Add(value.Key, value.Value);
            self.Companions.Clear(); foreach (var value in companions) self.Companions.Add(value.Key, value.Value);
            self.FrontWorks.Clear(); foreach (var value in frontWorks) self.FrontWorks.Add(value.Key, value.Value);
            self.Customers.Clear(); foreach (var value in customers) self.Customers.Add(value.Key, value.Value);
            self.Orders.Clear(); foreach (var value in orders) self.Orders.Add(value.Key, value.Value);
            self.Tables.Clear(); foreach (var value in tables) self.Tables.Add(value.Key, value.Value);
            self.WaitingCustomers = waiting; self.SkippedArrivals = snapshot.SkippedArrivals;
            self.BuffSeed=snapshot.BuffSeed;self.BuffDrawVersion=snapshot.BuffDrawVersion;self.SelectedBuffId=snapshot.SelectedBuffId;self.RequireBuffChoice=snapshot.RequireBuffChoice;
            self.BuffCandidates=buffCandidates;
            self.SupplySources.Clear(); foreach (var value in supplySources) self.SupplySources.Add(value.Key, value.Value);
            self.SupplyShipments.Clear(); foreach (var value in supplyShipments) self.SupplyShipments.Add(value.Key, value.Value);
            self.RenovationCandidates = renovationCandidates; self.SelectedRenovationId = snapshot.SelectedRenovationId;
            self.SelectedUpgradeStationId = snapshot.SelectedUpgradeStationId; self.PendingUpgradeExpectedLevel = snapshot.PendingUpgradeExpectedLevel;
            self.UpgradeTargets = Array.AsReadOnly(snapshot.UpgradeTargets.Select(target => new RenovationUpgradeTarget(target.DefinitionId, target.StationId, target.ExpectedLevel)).ToArray());
            self.RenovationChoiceVersion = snapshot.RenovationChoiceVersion; self.RenovationLevelId = snapshot.RenovationLevelId;
            self.RenovationLevelGeneration = snapshot.RenovationLevelGeneration;
            self.RenovationFailure = snapshot.RenovationFailure;
            self.RenovationApplied.Clear(); foreach (var value in renovationApplied) self.RenovationApplied.Add(value.Key, value.Value);
            self.SaveEnabled = snapshot.SaveEnabled; self.SaveResultId = snapshot.SaveResultId; self.SaveFailure = snapshot.SaveFailure;
            self.SaveState = (SoloSaveState)snapshot.SaveState; self.LastSavedSlotId = snapshot.LastSavedSlotId;
            self.CanControlRestaurant = snapshot.CanControlRestaurant;
            if (actors.TryGetValue(self.MemberId, out GridActorProjection own) && own.LastCommandId >= self.LastCommandId)
            { self.LastCommandId = own.LastCommandId; self.LastStatus = own.LastStatus; self.LastReason = own.LastReason; self.NextCommandId = Math.Max(self.NextCommandId, own.LastCommandId); }
            if (snapshot.LastCommandId >= self.LastCommandId)
            { self.LastCommandId = snapshot.LastCommandId; self.LastCommandKind = (ProductCommandKind)snapshot.LastCommandKind; self.LastStatus = (GridResultStatus)snapshot.LastCommandStatus; self.LastReason = snapshot.LastCommandReason; }
            self.ConfirmGridProjection();
        }

        public static void ApplyGridResult(this GridClientComponent self, GridCommandResult result)
        {
            if (!self.Confirmed || result.MatchId != self.MatchId || result.WorldId != self.WorldId || result.WorldGeneration != self.WorldGeneration ||
                result.BindingGeneration != self.BindingGeneration || result.LevelId != self.LevelId || result.LevelGeneration != self.LevelGeneration ||
                result.Version > self.Version) return;
            if (result.CommandId >= self.LastCommandId)
            {
                self.LastCommandId = result.CommandId; self.LastStatus = (GridResultStatus)result.Status; self.LastReason = result.Reason;
            }
            if ((GridResultStatus)result.Status is GridResultStatus.Executed or GridResultStatus.Rejected or GridResultStatus.Cancelled)
            {
                var context = new ProductCommandContext(result.MatchId, result.WorldId, result.WorldGeneration,
                    result.LevelId, result.LevelGeneration, result.BindingGeneration, result.CommandId);
                EventSystem.Instance.Publish((Scene)self.IScene, new ProductCommandOutcomeApplied
                {
                    Value = new ProductCommandOutcomeValue(context, ProductCommandKind.Grid, (GridResultStatus)result.Status, result.Reason)
                });
            }
            EventSystem.Instance.Publish((Scene)self.IScene, new GridProjectionApplied { Version = self.Version });
        }

        public static GridCommandRequest CreateGridCommand(this GridClientComponent self, GridIntent intent) => self.CreateGridCommand(
            intent.Action, intent.Cell, intent.TargetId, intent.ExpectedItemId, intent.ExpectedTargetItemId, intent.ExpectedProcessId,
            intent.ExpectedContentIds, intent.ExpectedRemainingPortions, intent.ExpectedLayoutVersion, intent.ExpectedOrderId);

        public static GridCommandRequest CreateGridCommand(this GridClientComponent self, GridAction action, GridCell cell = default, long target = 0, long expected = 0, long expectedTarget = 0, long process = 0,
            IEnumerable<long> expectedContents = null, int expectedRemainingPortions = 0, long expectedLayoutVersion = 0, long expectedOrderId = 0)
        {
            if (!self.Confirmed) throw new InvalidOperationException("Grid projection is not ready");
            ClientSessionComponent client = self.GetParent<ClientSessionComponent>();
            ProductCommandContext context = client.CreateProductCommandContext(self.WorldId, self.WorldGeneration, self.LevelId, self.LevelGeneration);
            long command = context.CommandId;
            self.NextCommandId = command; // 原 HUD 的显示兼容值，不再分配另一条编号。
            self.LastCommandId = command; self.LastStatus = GridResultStatus.Accepted; self.LastReason = null;
            self.LastCommandKind = ProductCommandKind.Grid;
            return ProductMessages.Create<GridCommandRequest>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = self.MatchId; value.WorldId = self.WorldId;
                value.WorldGeneration = self.WorldGeneration; value.BindingGeneration = self.BindingGeneration; value.CommandId = command;
                value.Action = (int)action; value.X = cell.X; value.Y = cell.Y; value.TargetId = target; value.ExpectedItemId = expected; value.ExpectedTargetItemId = expectedTarget; value.ExpectedProcessId = process;
                value.LevelId = context.LevelId; value.LevelGeneration = context.LevelGeneration;
                if (expectedContents != null) value.ExpectedContentIds.AddRange(expectedContents);
                value.ExpectedRemainingPortions = expectedRemainingPortions; value.ExpectedLayoutVersion = expectedLayoutVersion;
                value.ExpectedOrderId = expectedOrderId;
            });
        }

        public static GridIntent CleanupIntent(this GridClientComponent self, long target, GridAction action)
        {
            var station = self.Stations[target]; var actor = self.Actors[self.MemberId];
            if (!self.Items.TryGetValue(actor.HandItemId, out var source))
                return new GridIntent(0, action, station.Cell, target, expectedLayoutVersion: self.LayoutVersion);
            var prep = self.Processes.Values.Where(value => value.InputItemId == source.Id || value.TargetItemId == source.Id || value.OutputItemId == source.Id ||
                source.Contents.Contains(value.InputItemId) || source.Contents.Contains(value.TargetItemId) || source.Contents.Contains(value.OutputItemId)).OrderByDescending(value => value.Id).FirstOrDefault();
            long baked = self.BakeProcesses.Values.Where(value => value.InputItemId == source.Id || value.OutputItemId == source.Id ||
                source.Contents.Contains(value.InputItemId) || source.Contents.Contains(value.OutputItemId)).Select(value => value.Id).DefaultIfEmpty().Max();
            int portions = self.AutoProcesses.TryGetValue(source.AutoProcessId, out var batch) ? batch.RemainingPortions : 0;
            return new GridIntent(0, action, station.Cell, target, source.Id, expectedProcessId: source.AutoProcessId != 0 ? source.AutoProcessId : Math.Max(prep.Id, baked),
                expectedContents: source.Contents, expectedRemainingPortions: portions, expectedLayoutVersion: self.LayoutVersion);
        }

        public static GridIntent CounterIntent(this GridClientComponent self, long target)
        {
            GridActorProjection actor = self.Actors[self.MemberId];
            GridStationProjection station = self.Stations[target];
            if ((station.Capabilities & GridStationCapability.Trash) != 0) return self.CleanupIntent(target, GridAction.DiscardSolid);
            if ((station.Capabilities & GridStationCapability.FillWater) != 0)
            {
                bool emptyContainer = self.Items.TryGetValue(actor.HandItemId, out var vessel) && vessel.Form == GridMaterialForm.Container && vessel.Contents.Count == 0 && vessel.AutoProcessId == 0;
                return self.CleanupIntent(target, emptyContainer ? GridAction.FillWater : GridAction.DrainLiquid);
            }
            if (actor.HandItemId == 0 && (station.Capabilities & GridStationCapability.Bake) != 0 &&
                self.Items.TryGetValue(station.ItemId, out GridItemProjection bakeInput) && bakeInput.BakeProcessId == 0 &&
                Tables.Instance.DTGridBakeRecipeConfig.DataList.Any(row => row.InputDefinitionId == bakeInput.DefinitionId))
                return new GridIntent(0, GridAction.StartBake, station.Cell, target, bakeInput.Id, bakeInput.Id);
            GridProcessProjection process = self.Processes.Values.FirstOrDefault(p => p.InputItemId == station.ItemId && p.State != GridWorkState.Completed);
            if (actor.WorkId != 0 && process.Id == actor.WorkId)
                return new GridIntent(0, GridAction.StopWork, station.Cell, target, process.InputItemId, process.TargetItemId, process.Id);
            if (actor.HandItemId != 0)
            {
                if (self.Items.TryGetValue(station.ItemId, out var jug) && jug.DefinitionId == GridKitchenRules.JuiceJug &&
                    self.Items.TryGetValue(actor.HandItemId, out var cup) && cup.DefinitionId == GridKitchenRules.DisposableCup && cup.Contents.Count == 0)
                    return new GridIntent(0, GridAction.PortionJuice, station.Cell, target, cup.Id, jug.Id, jug.AutoProcessId);
                if (self.Items.TryGetValue(station.ItemId, out GridItemProjection pot) && pot.DefinitionId == GridKitchenRules.Pot && self.Items.TryGetValue(actor.HandItemId, out GridItemProjection held) && held.DefinitionId == GridKitchenRules.Bowl)
                {
                    if (held.Contents.Count == 0)
                        return new GridIntent(0, GridAction.PortionSoup, station.Cell, target, held.Id, pot.Id, pot.AutoProcessId);
                    if (held.Contents.Count == 1 && self.Items.TryGetValue(held.Contents[0], out GridItemProjection liquid) && liquid.DefinitionId == GridKitchenRules.EggLiquid)
                        return new GridIntent(0, GridAction.PourContents, station.Cell, target, held.Id, pot.Id);
                }
                return new GridIntent(0, station.ItemId == 0 ? GridAction.PlaceCounter : GridAction.AddToContainer, station.Cell, target, actor.HandItemId, station.ItemId);
            }
            if (station.Kind == GridStationKind.Stove && self.Items.TryGetValue(station.ItemId, out GridItemProjection unstarted) && unstarted.DefinitionId == GridKitchenRules.Pot && unstarted.AutoProcessId == 0)
                return new GridIntent(0, GridAction.StartAutoWork, station.Cell, target, unstarted.Id, unstarted.Id);
            if (station.Kind == GridStationKind.Juicer && self.Items.TryGetValue(station.ItemId, out var juiceInput) && juiceInput.DefinitionId == GridKitchenRules.JuiceJug && juiceInput.AutoProcessId == 0)
                return new GridIntent(0, GridAction.StartJuice, station.Cell, target, juiceInput.Id, juiceInput.Id);
            if (station.Kind == GridStationKind.Chopping && self.Items.TryGetValue(station.ItemId, out GridItemProjection input) &&
                Tables.Instance.DTGridManualRecipeConfig.DataList.Any(r => r.Mode == 1 && r.InputDefinitionId == input.DefinitionId))
                return new GridIntent(0, GridAction.StartStationWork, station.Cell, target, input.Id, input.Id, process.Id);
            return new GridIntent(0, GridAction.TakeCounter, station.Cell, target, station.ItemId);
        }

        public static string ItemDescription(this GridClientComponent self, long id)
        {
            if (!self.Items.TryGetValue(id, out GridItemProjection item)) return "空";
            string description = item.Name + (Tables.Instance.DTGridItemConfig.Get(item.DefinitionId).Kind == 2 ? "（" + (item.Contents.Count == 0 ? "空" : string.Join("、", item.Contents.Select(value => self.Items.TryGetValue(value, out GridItemProjection material) ? material.Name : "?"))) + "）" : "");
            if(item.Hygiene==ItemHygiene.Dirty)description+=" · 脏";
            if (item.BoundOrderId != 0) description += " · 已贴单";
            if (self.AutoProcesses.TryGetValue(item.AutoProcessId, out GridAutoProcessProjection automatic)) description += " · " + AutoWorkDescription(automatic);
            if (self.BakeProcesses.TryGetValue(item.BakeProcessId, out GridBakeProjection baking)) description += " · " + BakeWorkDescription(baking);
            return description;
        }

        public static string AutoWorkDescription(GridAutoProcessProjection process)
            => process.State == GridAutoState.Cooking ? $"{(process.LockContainer ? "榨汁（锁壶）" : "煮制")} {process.ProgressTicks}/{process.RequiredTicks}" : $"完成 · 剩 {process.RemainingPortions} 份";

        public static string BakeWorkDescription(GridBakeProjection process) => process.State == GridBakeState.Baking
            ? $"烘烤 {process.ProgressTicks}/{process.RequiredTicks}" : "烘烤完成";

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
