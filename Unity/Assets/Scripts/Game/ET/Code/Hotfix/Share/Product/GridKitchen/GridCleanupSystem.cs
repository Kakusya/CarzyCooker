#if !DOTNET
using System;
using System.Linq;

namespace ET
{
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(GridActor))]
    [FriendOf(typeof(GridStation))]
    [FriendOf(typeof(GridItem))]
    [FriendOf(typeof(GridManualProcess))]
    [FriendOf(typeof(GridAutoProcess))]
    [FriendOf(typeof(GridBakeProcess))]
    [FriendOf(typeof(FrontWork))]
    public static partial class GridKitchenSystem
    {
        private static bool IsCleanupAction(GridAction action) => action is GridAction.FillWater or GridAction.DrainLiquid or GridAction.DiscardSolid;

        private static GridManualProcess[] CleanupPrep(this GridKitchenComponent self, GridItem source)
        {
            var ids = source.Contents.Append(source.Id).ToHashSet();
            return self.Processes.Values.Select(value => (GridManualProcess)value)
                .Where(value => ids.Contains(value.InputItemId) || ids.Contains(value.TargetItemId) || ids.Contains(value.OutputItemId)).ToArray();
        }

        private static GridBakeProcess[] CleanupBakes(this GridKitchenComponent self, GridItem source)
        {
            var ids = source.Contents.Append(source.Id).ToHashSet();
            return self.BakeProcesses.Values.Select(value => (GridBakeProcess)value)
                .Where(value => ids.Contains(value.InputItemId) || ids.Contains(value.OutputItemId)).ToArray();
        }

        private static string CheckCleanup(this GridKitchenComponent self, GridActor actor, GridStation station, GridIntent intent,
            bool requireArrival, out GridItem source, out GridItem[] contents, out GridAutoProcess batch)
        {
            source = self.Item(actor.HandItemId); contents = Array.Empty<GridItem>(); batch = null;
            if (station == null) return "UnknownCounter";
            var capability = intent.Action == GridAction.FillWater ? GridStationCapability.FillWater
                : intent.Action == GridAction.DrainLiquid ? GridStationCapability.DrainLiquid : GridStationCapability.Trash;
            if ((station.Capabilities & capability) == 0) return intent.Action == GridAction.DiscardSolid ? "RequiresTrash" : "RequiresSink";
            if (intent.ExpectedLayoutVersion <= 0 || intent.ExpectedLayoutVersion != self.LayoutVersion) return "LayoutChanged";
            if (requireArrival && !self.AtStation(actor, station)) return "CounterNotAdjacent";
            if (source == null || source.Id != intent.ExpectedItemId || source.Location != GridItemLocation.Hand || source.HolderId != actor.MemberId)
                return "TargetChanged";
            if (!source.Contents.SequenceEqual(intent.ExpectedContentIds)) return "ContentsChanged";
            long sourceId = source.Id;
            contents = source.Contents.Select(self.Item).ToArray();
            if (contents.Any(item => item == null || item.Location != GridItemLocation.Container || item.HolderId != sourceId) ||
                source.Contents.Distinct().Count() != source.Contents.Count) return "ContentsChanged";
            if (contents.Any(item => Tables.Instance.DTGridItemConfig.Get(item.DefinitionId).Kind != GridItemKind.Material || item.Contents.Count != 0)) return "NestedContainer";
            var prep = self.CleanupPrep(source);
            var baking = self.CleanupBakes(source);
            if (prep.Any(value => value.State == GridWorkState.Working || value.WorkerMemberId != 0)) return "WorkBusy";
            if (source.BakeProcessId != 0 || baking.Any(value => value.State == GridBakeState.Baking) || contents.Any(item => item.BakeProcessId != 0 || item.AutoProcessId != 0)) return "ProcessLocked";
            batch = self.AutoProcess(source.AutoProcessId);
            long expectedProcess = source.AutoProcessId != 0 ? source.AutoProcessId
                : prep.Select(value => value.Id).Concat(baking.Select(value => value.Id)).DefaultIfEmpty().Max();
            if (expectedProcess != intent.ExpectedProcessId) return "ProcessChanged";
            if (source.AutoProcessId != 0 && (batch == null || batch.PotItemId != source.Id || batch.State != GridAutoState.Ready)) return "ProcessLocked";
            if (batch != null && (!batch.InputItemIds.SequenceEqual(source.Contents) || batch.RemainingPortions <= 0 ||
                batch.RemainingPortions != intent.ExpectedRemainingPortions)) return "BatchChanged";
            if (batch == null && intent.ExpectedRemainingPortions != 0) return "BatchChanged";
            Level level = self.ProductLevel();
            if (station.ItemId != 0 || level != null && level.Children.Values.OfType<FrontWork>().Any(work => work.StationId == station.Id &&
                work.State is FrontWorkState.Approaching or FrontWorkState.Working)) return "StationBusy";
            var definition = Tables.Instance.DTGridItemConfig.Get(source.DefinitionId);
            if (intent.Action == GridAction.FillWater)
            {
                if (definition.Kind != GridItemKind.Container) return "RequiresContainer";
                if (source.Hygiene != ItemHygiene.Clean) return "DirtyUtensil";
                if (source.Contents.Count != 0 || batch != null) return "ContainerFull";
                var supply = Tables.Instance.DTStationConfig.Get(station.DefinitionId);
                if (definition.ContainerCapacity < supply.WaterUnits) return "ContainerFull";
                if (!definition.AcceptedDefinitionIds.Contains(supply.WaterDefinitionId)) return "IncompatibleContainer";
                return null;
            }
            GridMaterialForm requiredForm = (intent.Action == GridAction.DrainLiquid ? GridMaterialForm.Liquid : GridMaterialForm.Solid);
            if (batch != null)
                return Tables.Instance.DTGridItemConfig.Get(batch.OutputDefinitionId).Form != requiredForm
                    ? intent.Action == GridAction.DrainLiquid ? "RequiresLiquid" : "RequiresSolid" : null;
            if (definition.Kind == GridItemKind.Material)
            {
                if (source.Contents.Count != 0) return "ContentsChanged";
                if (definition.Form != requiredForm) return intent.Action == GridAction.DrainLiquid ? "RequiresLiquid" : "RequiresSolid";
                return definition.Discardable ? null : "ProtectedItem";
            }
            if (contents.Length == 0)
                return intent.Action == GridAction.DiscardSolid && definition.Discardable ? null : "ProtectedContainer";
            var forms = contents.Select(item => Tables.Instance.DTGridItemConfig.Get(item.DefinitionId).Form).Distinct().ToArray();
            if (forms.Length != 1) return "MixedContents";
            if (forms[0] != requiredForm) return intent.Action == GridAction.DrainLiquid ? "RequiresLiquid" : "RequiresSolid";
            return contents.Any(item => !Tables.Instance.DTGridItemConfig.Get(item.DefinitionId).Discardable) ? "ProtectedItem" : null;
        }

        private static string CleanupAtFacility(this GridKitchenComponent self, GridActor actor, GridStation station, GridIntent intent)
        {
            string error = self.CheckCleanup(actor, station, intent, true, out var source, out var contents, out var batch);
            if (error != null) return error;
            self.PauseWork(actor);
            foreach (var process in self.CleanupPrep(source))
            {
                foreach (var current in self.OrderedActors()) if (current.WorkId == process.Id) current.WorkId = 0;
                self.Processes.Remove(process.Id); process.Dispose();
            }
            foreach (var process in self.CleanupBakes(source))
            {
                var oven = self.Station(process.StationId);
                if (oven != null && oven.BakeProcessId == process.Id) oven.BakeProcessId = 0;
                self.BakeProcesses.Remove(process.Id); process.Dispose();
            }
            if (intent.Action == GridAction.FillWater)
            {
                var supply = Tables.Instance.DTStationConfig.Get(station.DefinitionId);
                var water = self.CreateGridItem(supply.WaterDefinitionId, GridItemLocation.Container, source.Id, source.Cell);
                source.Contents.Add(water.Id);
                return null;
            }
            if (batch != null)
            {
                var start = self.Station(batch.StartStationId);
                if (start != null && start.AutoProcessId == batch.Id) start.AutoProcessId = 0;
                source.AutoProcessId = 0; batch.RemainingPortions = 0;
                self.AutoProcesses.Remove(batch.Id); batch.Dispose();
            }
            foreach (var item in contents) { self.Items.Remove(item.Id); item.Dispose(); }
            self.ProductLevel()?.DrinkBindings.Remove(source.Id);
            source.Contents.Clear();
            var definition = Tables.Instance.DTGridItemConfig.Get(source.DefinitionId);
            if (definition.Kind == GridItemKind.Material || contents.Length == 0 && batch == null && definition.Discardable)
            {
                actor.HandItemId = 0; self.Items.Remove(source.Id); source.Dispose();
            }
            return null;
        }
    }
}
#endif
