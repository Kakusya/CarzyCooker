#if !DOTNET
using System;
using System.Linq;

namespace ET
{
    [FriendOf(typeof(GridAutoProcess))]
    [FriendOf(typeof(GridManualProcess))]
    public static partial class GridKitchenSystem
    {
        public static string QueueStationLayout(this GridKitchenComponent self, HostPeer peer,
            ProductCommandContext context, GridStationLayoutIntent intent)
        {
            Level level = self.ProductLevel(); Match match = self.Match;
            if (level == null) return "SoloRestaurantOnly";
            if (self.Stopped || peer == null || !self.HasBinding(peer.MemberId, peer.Generation)) return "EntryClosed";
            if (!peer.CanControlRestaurant()) return "HostControlOnly";
            string identity = context.Validate(match.MatchId, self.WorldId, self.WorldGeneration, level.Id, level.Generation, peer.Generation);
            if (identity != null) return identity;
            string prior = peer.CheckProductCommand(context, ProductCommandKind.Layout, intent.Equals(peer.LastLayoutCommand), out bool replay);
            if (prior != null || replay) return prior;
            if (intent.ExpectedLayoutVersion <= 0 || intent.Orientation < 0 || intent.Orientation > 3) return "InvalidLayout";
            peer.RecordProductCommand(context, ProductCommandKind.Layout); peer.LastLayoutCommand = intent;
            self.LayoutCommands.Enqueue(new GridStationLayoutEnvelope(context, intent, peer));
            return null;
        }

        public static void AdvanceStationLayouts(this GridKitchenComponent self)
        {
            while (self.LayoutCommands.Count > 0)
            {
                var command = self.LayoutCommands.Dequeue(); HostPeer peer = command.Peer;
                Level level = self.ProductLevel(); Match match = self.Match;
                string error = peer == null || !self.HasBinding(peer.MemberId, command.Context.BindingGeneration) ? "EntryClosed"
                    : command.Context.Validate(match.MatchId, self.WorldId, self.WorldGeneration, level.Id, level.Generation, peer.Generation);
                if (error == null && !peer.CanControlRestaurant()) error = "HostControlOnly";
                if (error == null) error = self.ApplyStationLayout(level, command.Intent);
                peer?.RecordProductResult(command.Context, ProductCommandKind.Layout, error == null ? GridResultStatus.Executed : GridResultStatus.Rejected, error);
                self.Dirty = true;
            }
        }

        private static string ApplyStationLayout(this GridKitchenComponent self, Level level, GridStationLayoutIntent intent)
        {
            if (level.Paused) return "Paused";
            if (level.Phase != LevelPhase.Preparing) return "WrongPhase";
            if (intent.ExpectedLayoutVersion != self.LayoutVersion) return "LayoutChanged";
            GridStation station = self.Station(intent.StationId);
            if (station == null) return "UnknownCounter";
            if (self.LayoutVersion == long.MaxValue) return "InvalidLayout";
            var definition = Tables.Instance.DTStationConfig.Get(station.DefinitionId);
            var footprint = GridStationGeometry.Cells(definition.FootprintOffsets, intent.Cell, intent.Orientation);
            if (self.OrderedActors().Any(actor => footprint.Contains(actor.Cell) ||
                    actor.Motion == GridMotion.Transit && footprint.Contains(actor.NextCell)) ||
                self.Items.Values.Select(value => (GridItem)value).Any(item => item.Location == GridItemLocation.Ground && footprint.Contains(item.Cell)))
                return "LayoutOccupied";
            var item = self.Item(station.ItemId);
            if(station.PoolItemIds.Count>station.ItemSlotCapacity || station.PoolItemIds.Distinct().Count()!=station.PoolItemIds.Count ||
                station.PoolItemIds.Any(id=>self.Item(id)==null || self.Item(id).HolderId!=station.Id || self.Item(id).Contents.Count!=0 ||
                    self.Item(id).Location!=(station.Kind==GridStationKind.CleanPool?GridItemLocation.CleanPool:GridItemLocation.DirtyPool)))
                return "LayoutRelationshipsChanged";
            if (station.ItemId != 0 && (item == null || item.Location != GridItemLocation.Counter || item.HolderId != station.Id ||
                item.Contents.Any(id => self.Item(id) == null || self.Item(id).Location != GridItemLocation.Container || self.Item(id).HolderId != item.Id)))
                return "LayoutRelationshipsChanged";
            GridAutoProcess batch = self.AutoProcess(station.AutoProcessId);
            if (batch is { LockContainer: true, State: GridAutoState.Cooking }) return "ProcessLocked";
            if (station.AutoProcessId != 0 && (batch == null || batch.StartStationId != station.Id)) return "LayoutRelationshipsChanged";
            GridBakeProcess bake = self.BakeProcess(station.BakeProcessId);
            if (station.BakeProcessId != 0 && (bake == null || bake.StationId != station.Id || bake.InputItemId != station.ItemId || item?.BakeProcessId != bake.Id))
                return "LayoutRelationshipsChanged";
            var placements = self.Stations.Values.Select(value => (GridStation)value).Select(value => new GridStationPlacement(value.DefinitionId,
                value.Id == station.Id ? intent.Cell : value.Cell, value.Id == station.Id ? intent.Orientation : value.Orientation));
            System.Collections.Generic.HashSet<GridCell> terrain;
            try
            {
                terrain = GridStationConfig.ValidateTerrain(GridStationConfig.ValidateDefinitions(Tables.Instance.DTStationConfig.DataList), placements,
                    Tables.Instance.DTLevelConfig.DataList.Select(row => new GridCell(row.SpawnX, row.SpawnY))
                        .Concat(self.OrderedActors().Select(actor => actor.Cell))
                        .Concat(self.OrderedActors().Where(actor => actor.Motion == GridMotion.Transit).Select(actor => actor.NextCell)), self.Width, self.Height);
            }
            catch (InvalidOperationException) { return "LayoutBlocked"; }
            string frontError = null; self.CheckFrontLayout(terrain, footprint, intent, ref frontError);
            if (frontError != null) return frontError;
            // 完整预检后只提交原对象的空间值；关系与进度不重建。
            station.Cell = intent.Cell; station.Orientation = intent.Orientation;
            foreach(long id in station.PoolItemIds)self.Item(id).Cell=station.Cell;
            if (item != null)
            {
                item.Cell = station.Cell;
                foreach (long id in item.Contents) self.Item(id).Cell = station.Cell;
            }
            self.Blocked.Clear(); self.Blocked.UnionWith(terrain); ++self.LayoutVersion;
            self.CancelLevelIntents("LayoutChanged");
            foreach (GridActor actor in self.OrderedActors())
            {
                GridManualProcess work = self.Process(actor.WorkId);
                if (work != null && work.StationId == station.Id && !self.AtStation(actor, station)) self.PauseWork(actor);
            }
            self.Dirty = true;
            return null;
        }

        // 前厅接入后加入实际角色位置、目标关系与可达性，不创建第二份地图。
        static partial void CheckFrontLayout(this GridKitchenComponent self, System.Collections.Generic.HashSet<GridCell> terrain, GridCell[] footprint, GridStationLayoutIntent intent, ref string error);
    }
}
#endif
