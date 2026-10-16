#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(GridActor))]
    [FriendOf(typeof(GridStation))]
    [FriendOf(typeof(GridItem))]
    [FriendOf(typeof(GridManualProcess))]
    public static partial class GridKitchenSystem
    {
        public static void ValidateGridDefinitions(IEnumerable<DRGridItemConfig> itemRows, IEnumerable<DRGridManualRecipeConfig> recipeRows)
        {
            var items = itemRows.ToDictionary(row => row.Id);
            var recipes = recipeRows.ToArray();
            foreach (int id in new[] { GridKitchenRules.Tomato, GridKitchenRules.TomatoChunks, GridKitchenRules.Egg, GridKitchenRules.EggLiquid, GridKitchenRules.Bowl })
                if (!items.ContainsKey(id)) throw new InvalidOperationException($"Missing grid item definition {id}");
            foreach (DRGridItemConfig item in items.Values)
                if (string.IsNullOrWhiteSpace(item.Name) || item.Kind is not (GridItemKind.Material or GridItemKind.Container) ||
                    (item.Kind == GridItemKind.Material && (item.ContainerCapacity != 0 || item.AcceptedDefinitionIds.Count != 0)) ||
                    (item.Kind == GridItemKind.Container && (item.ContainerCapacity <= 0 || item.AcceptedDefinitionIds.Count == 0)) ||
                    !ProductMenuCatalog.IsValid(item.MenuVariant) || item.Disposable && (item.Kind != GridItemKind.Container || item.ContainerCapacity != (item.Id == GridKitchenRules.HotDrinkCup ? 3 : 1)) ||
                    item.AllowsEmptyContainers && (item.Kind != GridItemKind.Container || !item.Discardable) ||
                    item.AcceptedDefinitionIds.Any(id => !items.ContainsKey(id) || items[id].Kind != GridItemKind.Material &&
                        !(item.AllowsEmptyContainers && items[id].Kind == GridItemKind.Container && items[id].Disposable)) ||
                    item.ExclusiveDefinitionIds.Any(id => item.Kind != GridItemKind.Container || !item.AcceptedDefinitionIds.Contains(id)))
                    throw new InvalidOperationException($"Invalid grid item definition {item.Id}");
            foreach (DRGridManualRecipeConfig recipe in recipes)
            {
                if (recipe.Mode == GridManualMode.Assembly)
                {
                    GridAssemblyConfig.Validate(recipe, items);
                    continue;
                }
                if (recipe.RequiredTicks <= 0 || recipe.Mode is not (GridManualMode.Chop or GridManualMode.Beat) ||
                    !items.ContainsKey(recipe.InputDefinitionId) || !items.ContainsKey(recipe.OutputDefinitionId) ||
                    items[recipe.InputDefinitionId].Kind != GridItemKind.Material || items[recipe.OutputDefinitionId].Kind != GridItemKind.Material ||
                    recipe.InputDefinitionId == recipe.OutputDefinitionId || recipe.InputDefinitionIds.Count != 0 ||
                    recipe.InputCounts.Count != 0 || recipe.ContainerDefinitionId != 0 || recipe.LastInputDefinitionId != 0)
                {
                    throw new InvalidOperationException($"Invalid grid recipe {recipe.Id}");
                }
            }
            if (recipes.Count(r => r.Mode == GridManualMode.Chop && r.InputDefinitionId == GridKitchenRules.Tomato && r.OutputDefinitionId == GridKitchenRules.TomatoChunks) != 1 ||
                recipes.Count(r => r.Mode == GridManualMode.Beat && r.InputDefinitionId == GridKitchenRules.Egg && r.OutputDefinitionId == GridKitchenRules.EggLiquid) != 1 ||
                items[GridKitchenRules.Bowl].Kind != GridItemKind.Container || !items[GridKitchenRules.Bowl].AcceptedDefinitionIds.Contains(GridKitchenRules.Egg) ||
                !items[GridKitchenRules.Bowl].AcceptedDefinitionIds.Contains(GridKitchenRules.EggLiquid))
                throw new InvalidOperationException("Missing or incompatible grid prep recipes / bowl");
        }

        private static GridItem CreateGridItem(this GridKitchenComponent self, int definition, GridItemLocation location, long holder, GridCell cell)
        {
            DRGridItemConfig row = Tables.Instance.DTGridItemConfig.Get(definition);
            GridItem item = self.AddChild<GridItem>();
            item.DefinitionId = definition; item.Name = row.Name; item.Location = location;
            item.HolderId = holder; item.Cell = cell; item.AutoProcessId = 0; item.Contents.Clear();
            item.BakeProcessId = 0;
            self.Items.Add(item.Id, item);
            return item;
        }

        public static GridManualProcess Process(this GridKitchenComponent self, long id) => self.Processes.TryGetValue(id, out var value) ? value : null;
        public static GridManualProcess InputProcess(this GridKitchenComponent self, long input) => self.Processes.Values
            .Select(value => (GridManualProcess)value).FirstOrDefault(value => value.InputItemId == input && value.State != GridWorkState.Completed);
        private static bool ItemWorking(this GridKitchenComponent self, long item) => (self.Item(item)?.BakeProcessId ?? 0) != 0 ||
            self.AutoProcess(self.Item(item)?.AutoProcessId ?? 0) is { LockContainer: true, State: GridAutoState.Cooking } || self.Processes.Values
            .Select(value => (GridManualProcess)value).Any(p => p.State == GridWorkState.Working && (p.InputItemId == item || p.TargetItemId == item));

        private static void PauseWork(this GridKitchenComponent self, GridActor actor)
        {
            GridManualProcess process = self.Process(actor.WorkId);
            actor.WorkId = 0;
            if (process == null || process.State != GridWorkState.Working || process.WorkerMemberId != actor.MemberId || process.WorkerGeneration != actor.Generation) return;
            self.PauseManualProcess(process);
        }

        private static string AddToContainer(this GridKitchenComponent self, GridActor actor, GridStation station, GridIntent intent)
        {
            GridItem input = self.Item(actor.HandItemId), target = self.Item(station.ItemId);
            if (input == null || input.Id != intent.ExpectedItemId || input.Location != GridItemLocation.Hand || input.HolderId != actor.MemberId ||
                target == null || target.Id != intent.ExpectedTargetItemId || target.Location != GridItemLocation.Counter || target.HolderId != station.Id) return "TargetChanged";
            if (self.ItemWorking(target.Id) || self.ItemWorking(input.Id)) return "WorkBusy";
            if (self.InputProcess(target.Id) is { } reserved && Tables.Instance.DTGridManualRecipeConfig.Get(reserved.RecipeId).Mode == GridManualMode.Assembly)
            {
                return "ProcessLocked";
            }
            if (target.Hygiene != ItemHygiene.Clean || input.Hygiene != ItemHygiene.Clean) return "DirtyUtensil";
            if (target.AutoProcessId != 0) return "ProcessLocked";
            DRGridItemConfig source = Tables.Instance.DTGridItemConfig.Get(input.DefinitionId), bowl = Tables.Instance.DTGridItemConfig.Get(target.DefinitionId);
            if (source.Kind == GridItemKind.Container) return "NestedContainer";
            if (bowl.Kind != GridItemKind.Container || !bowl.AcceptedDefinitionIds.Contains(input.DefinitionId)) return "IncompatibleMaterial";
            if (!GridAssemblyConfig.AllowsContainerContents(target.DefinitionId,
                target.Contents.Select(id => self.Item(id).DefinitionId).Append(input.DefinitionId).ToArray()))
            {
                return "ContainerFull";
            }
            if (target.Contents.Count >= bowl.ContainerCapacity) return "ContainerFull";
            if (target.DefinitionId is GridKitchenRules.Pot or GridKitchenRules.JuiceJug or GridKitchenRules.MixingBowl or GridKitchenRules.CoffeeReceiver or GridKitchenRules.MilkPitcher)
            {
                string error = self.CheckPotAddition(target, input.DefinitionId);
                if (error != null) return error;
            }
            string permission = self.CheckProductionInput(target.DefinitionId, input.DefinitionId);
            if (permission != null)
            {
                return permission;
            }
            if (target.DefinitionId == GridKitchenRules.HotDrinkCup)
            {
                string addition = self.CheckDrinkBaseAddition(target, input.DefinitionId);
                if (addition != null)
                {
                    return addition;
                }
            }
            self.PauseWork(actor);
            actor.HandItemId = 0; input.Location = GridItemLocation.Container; input.HolderId = target.Id; target.Contents.Add(input.Id);
            return null;
        }

        private static string StartWork(this GridKitchenComponent self, GridActor actor, GridStation station, GridIntent intent)
        {
            GridManualMode mode = intent.Action == GridAction.StartStationWork ? GridManualMode.Chop : GridManualMode.Beat;
            GridItem target = self.Item(mode == GridManualMode.Chop ? station?.ItemId ?? 0 : actor.HandItemId);
            if (target == null || target.Id != intent.ExpectedTargetItemId) return "TargetChanged";
            if (target.Hygiene != ItemHygiene.Clean) return "DirtyUtensil";
            GridItem input;
            if (mode == GridManualMode.Chop)
            {
                if ((station.Capabilities & GridStationCapability.Chop) == 0) return "RequiresChoppingStation";
                if (actor.HandItemId != 0) return "HandFull";
                if (target.Location != GridItemLocation.Counter || target.HolderId != station.Id) return "TargetChanged";
                input = target;
            }
            else
            {
                if (target.Location != GridItemLocation.Hand || target.HolderId != actor.MemberId || target.DefinitionId != GridKitchenRules.Bowl || target.Contents.Count != 1) return "RequiresHeldEggBowl";
                input = self.Item(target.Contents[0]);
                if (input == null || input.Location != GridItemLocation.Container || input.HolderId != target.Id) return "TargetChanged";
            }
            if (input == null || input.Id != intent.ExpectedItemId) return "TargetChanged";
            DRGridManualRecipeConfig recipe = Tables.Instance.DTGridManualRecipeConfig.DataList.FirstOrDefault(r => r.Mode == mode && r.InputDefinitionId == input.DefinitionId);
            if (recipe == null) return "NoRecipe";
            if (mode == GridManualMode.Beat && !Tables.Instance.DTGridItemConfig.Get(target.DefinitionId).AcceptedDefinitionIds.Contains(recipe.OutputDefinitionId)) return "IncompatibleMaterial";
            GridManualProcess process = self.InputProcess(input.Id);
            bool continuing = process != null;
            if (!continuing)
            {
                string permission = self.CheckNewRecipePermission(self.ProductLevel()?.ProductionPermit.ManualRecipes.Contains(recipe.Id) ?? true,
                    mode == GridManualMode.Chop ? station : null);
                if (permission != null)
                {
                    return permission;
                }
            }
            if ((process?.Id ?? 0) != intent.ExpectedProcessId) return "ProcessChanged";
            if (actor.WorkId != 0 || process?.State == GridWorkState.Working || (mode == GridManualMode.Chop && self.ItemWorking(target.Id))) return "WorkBusy";
            if (process == null)
            {
                process = self.AddChild<GridManualProcess>();
                process.RecipeId = recipe.Id; process.InputItemId = input.Id; process.TargetItemId = target.Id;
                process.RequiredTicks = self.FrozenManualTicks(recipe, mode == GridManualMode.Chop ? station : null); process.ProgressTicks = 0; process.OutputItemId = 0;
                self.Processes.Add(process.Id, process);
            }
            if (process.RecipeId != recipe.Id || process.TargetItemId != target.Id) return "ProcessChanged";
            process.StationId = mode == GridManualMode.Chop ? station.Id : 0;
            process.WorkerMemberId = actor.MemberId; process.WorkerGeneration = actor.Generation;
            process.State = GridWorkState.Working; process.StartedTick = self.Tick; actor.WorkId = process.Id;
            process.CompletionQueued = false; process.CompletionReceived = false;
            self.PublishKitchenStage(process, KitchenProcessKind.Manual, continuing ? KitchenWorkStage.Resumed : KitchenWorkStage.Started, ++process.TransitionSerial);
            return null;
        }

        private static string StopWork(this GridKitchenComponent self, GridActor actor, GridIntent intent)
        {
            GridManualProcess process = self.Process(actor.WorkId);
            if (process == null || process.Id != intent.ExpectedProcessId || process.InputItemId != intent.ExpectedItemId ||
                process.TargetItemId != intent.ExpectedTargetItemId || process.WorkerMemberId != actor.MemberId || process.WorkerGeneration != actor.Generation) return "ProcessChanged";
            self.PauseWork(actor); return null;
        }

        private static void AdvanceWork(this GridKitchenComponent self)
        {
            self.ManualStepBuffer.Clear();
            foreach(var reference in self.Processes.Values)self.ManualStepBuffer.Add(reference);
            foreach (var reference in self.ManualStepBuffer)
            {
                GridManualProcess process=reference;
                if(process==null)
                {
                    continue;
                }
                if (process.State != GridWorkState.Working) continue;
                GridActor actor = self.Actor(process.WorkerMemberId);
                GridItem input = self.Item(process.InputItemId), target = self.Item(process.TargetItemId);
                GridStation station = self.Station(process.StationId);
                DRGridManualRecipeConfig recipe = Tables.Instance.DTGridManualRecipeConfig.Get(process.RecipeId);
                bool valid = actor != null && actor.Generation == process.WorkerGeneration && actor.WorkId == process.Id &&
                    self.HasBinding(actor.MemberId, actor.Generation) && actor.Motion == GridMotion.Standing && input != null && target != null;
                if (recipe.Mode == GridManualMode.Assembly)
                {
                    valid &= target != null && target.Id == input?.Id && actor?.HandItemId == target.Id &&
                        target.Location == GridItemLocation.Hand && target.HolderId == actor?.MemberId && self.AssemblyContentsMatch(target, recipe);
                }
                else if (process.StationId != 0)
                    valid &= actor != null && station != null && (station.Capabilities & GridStationCapability.Chop) != 0 && station.ItemId == process.InputItemId &&
                        input?.Location == GridItemLocation.Counter && input?.HolderId == station.Id && actor?.HandItemId == 0 && self.AtStation(actor, station);
                else valid &= actor?.HandItemId == process.TargetItemId && target?.Location == GridItemLocation.Hand && target?.HolderId == actor?.MemberId &&
                    target?.Contents.Count == 1 && target.Contents[0] == process.InputItemId && input?.Location == GridItemLocation.Container && input?.HolderId == target.Id;
                if (!valid)
                {
                    if (actor != null && actor.WorkId == process.Id) actor.WorkId = 0;
                    self.PauseManualProcess(process);
                    continue;
                }
                if (process.StartedTick == self.Tick) continue;
                if (process.ProgressTicks < process.RequiredTicks) ++process.ProgressTicks;
                self.Dirty = true;
                if (process.ProgressTicks < process.RequiredTicks) continue;
                if (!process.CompletionReceived)
                {
                    if (!process.CompletionQueued)
                    {
                        self.RequestKitchenCompletion(process, KitchenProcessKind.Manual, process.TransitionSerial);
                        process.CompletionQueued = true;
                    }
                    continue;
                }
                if (recipe.Mode == GridManualMode.Assembly)
                {
                    self.CompleteAssembly(process, actor, target, recipe);
                    continue;
                }
                GridItem output = self.CreateGridItem(recipe.OutputDefinitionId, input.Location, input.HolderId, input.Cell);
                if (station != null) station.ItemId = output.Id;
                else { target.Contents.Clear(); target.Contents.Add(output.Id); }
                self.Items.Remove(input.Id); input.Dispose();
                process.OutputItemId = output.Id; process.State = GridWorkState.Completed;
                process.WorkerMemberId = 0; process.WorkerGeneration = 0; actor.WorkId = 0;
                process.CompletionQueued = false; process.CompletionReceived = false;
                self.PublishKitchenStage(process, KitchenProcessKind.Manual, KitchenWorkStage.Finished, ++process.TransitionSerial, output.Id);
            }
            self.ManualStepBuffer.Clear();
        }
    }

    [EntitySystemOf(typeof(GridManualProcess))]
    [FriendOf(typeof(GridManualProcess))]
    public static partial class GridManualProcessSystem
    {
        [EntitySystem] private static void Awake(this GridManualProcess self)
        {
            self.TransitionSerial = 0; self.CompletionQueued = false; self.CompletionReceived = false;
        }
    }
}
#endif
