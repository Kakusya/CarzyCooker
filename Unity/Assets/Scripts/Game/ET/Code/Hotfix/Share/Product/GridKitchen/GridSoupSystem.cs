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
    [FriendOf(typeof(GridAutoProcess))]
    public static partial class GridKitchenSystem
    {
        public static void ValidateGridSoupDefinitions(IEnumerable<DRGridItemConfig> itemRows, IEnumerable<DRGridAutoRecipeConfig> recipeRows)
        {
            var items = itemRows.ToDictionary(row => row.Id);
            var recipes = recipeRows.ToArray();
            if (!items.TryGetValue(GridKitchenRules.Pot, out var pot) || pot.Kind != 2 ||
                !items.ContainsKey(GridKitchenRules.Soup) || !items.TryGetValue(GridKitchenRules.Bowl, out var bowl) ||
                !bowl.AcceptedDefinitionIds.Contains(GridKitchenRules.Soup))
                throw new InvalidOperationException("Missing soup / pot / compatible bowl definitions");
            foreach (var recipe in recipes)
            {
                if (!items.TryGetValue(recipe.ContainerDefinitionId, out var container) || container.Kind != 2 ||
                    recipe.RequiredTicks <= 0 || recipe.OutputPortions <= 0 || recipe.InputDefinitionIds.Count == 0 ||
                    recipe.InputDefinitionIds.Count != recipe.InputCounts.Count || recipe.InputCounts.Any(count => count <= 0) ||
                    recipe.InputDefinitionIds.Distinct().Count() != recipe.InputDefinitionIds.Count ||
                    recipe.InputCounts.Sum() > container.ContainerCapacity ||
                    recipe.InputDefinitionIds.Any(id => !items.TryGetValue(id, out var item) || item.Kind != 1 || !container.AcceptedDefinitionIds.Contains(id)) ||
                    !items.TryGetValue(recipe.OutputDefinitionId, out var output) || output.Kind != 1 ||
                    !items.TryGetValue(recipe.ServingContainerDefinitionId, out var serving) || serving.Kind != 2 || !serving.AcceptedDefinitionIds.Contains(output.Id))
                    throw new InvalidOperationException($"Invalid soup recipe {recipe.Id}");
            }
            if (recipes.Count(recipe => recipe.ContainerDefinitionId == GridKitchenRules.Pot &&
                recipe.OutputDefinitionId == GridKitchenRules.Soup && recipe.ServingContainerDefinitionId == GridKitchenRules.Bowl &&
                recipe.InputDefinitionIds.Count == 2 && recipe.InputDefinitionIds.Contains(GridKitchenRules.TomatoChunks) &&
                recipe.InputDefinitionIds.Contains(GridKitchenRules.EggLiquid)) != 1)
                throw new InvalidOperationException("Missing unique tomato egg soup recipe");
        }

        public static GridAutoProcess AutoProcess(this GridKitchenComponent self, long id)
            => self.AutoProcesses.TryGetValue(id, out var value) ? value : null;

        private static bool RecipeMatches(DRGridAutoRecipeConfig recipe, IReadOnlyList<int> definitions, bool complete)
        {
            if (definitions.Any(id => !recipe.InputDefinitionIds.Contains(id))) return false;
            for (int i = 0; i < recipe.InputDefinitionIds.Count; ++i)
            {
                int count = definitions.Count(id => id == recipe.InputDefinitionIds[i]);
                if (count > recipe.InputCounts[i] || (complete && count != recipe.InputCounts[i])) return false;
            }
            return true;
        }

        private static string CheckPotAddition(this GridKitchenComponent self, GridItem pot, int definition)
        {
            if (pot.AutoProcessId != 0) return "ProcessLocked";
            var definitions = new List<int>();
            foreach (long id in pot.Contents)
            {
                GridItem item = self.Item(id);
                if (item == null || item.Location != GridItemLocation.Container || item.HolderId != pot.Id) return "TargetChanged";
                definitions.Add(item.DefinitionId);
            }
            definitions.Add(definition);
            return Tables.Instance.DTGridAutoRecipeConfig.DataList.Any(recipe => recipe.ContainerDefinitionId == pot.DefinitionId && RecipeMatches(recipe, definitions, false))
                ? null : "IncompatibleMaterial";
        }

        private static void RetireCompletedPrep(this GridKitchenComponent self, long target)
        {
            foreach (var process in self.Processes.Values.Select(value => (GridManualProcess)value).Where(process => process.TargetItemId == target && process.State == GridWorkState.Completed).ToArray())
            {
                self.Processes.Remove(process.Id);
                process.Dispose();
            }
        }

        private static string PourContents(this GridKitchenComponent self, GridActor actor, GridStation station, GridIntent intent)
        {
            GridItem bowl = self.Item(actor.HandItemId), pot = self.Item(station.ItemId);
            if (bowl == null || bowl.Id != intent.ExpectedItemId || bowl.DefinitionId != GridKitchenRules.Bowl || bowl.Location != GridItemLocation.Hand || bowl.HolderId != actor.MemberId ||
                pot == null || pot.Id != intent.ExpectedTargetItemId || pot.DefinitionId != GridKitchenRules.Pot || pot.Location != GridItemLocation.Counter || pot.HolderId != station.Id) return "TargetChanged";
            if (self.ItemWorking(bowl.Id)) return "WorkBusy";
            if (bowl.Hygiene != ItemHygiene.Clean || pot.Hygiene != ItemHygiene.Clean) return "DirtyUtensil";
            if (bowl.Contents.Count != 1) return "RequiresEggLiquidBowl";
            GridItem liquid = self.Item(bowl.Contents[0]);
            if (liquid == null || liquid.DefinitionId != GridKitchenRules.EggLiquid || liquid.Location != GridItemLocation.Container || liquid.HolderId != bowl.Id) return "RequiresEggLiquidBowl";
            if (pot.AutoProcessId != 0) return "ProcessLocked";
            if (pot.Contents.Count >= Tables.Instance.DTGridItemConfig.Get(pot.DefinitionId).ContainerCapacity) return "ContainerFull";
            string error = self.CheckPotAddition(pot, liquid.DefinitionId);
            if (error != null) return error;
            self.PauseWork(actor);
            bowl.Contents.Clear(); pot.Contents.Add(liquid.Id); liquid.HolderId = pot.Id;
            self.RetireCompletedPrep(bowl.Id);
            return null;
        }

        private static string StartAutoWork(this GridKitchenComponent self, GridActor actor, GridStation station, GridIntent intent)
        {
            GridItem pot = self.Item(station.ItemId);
            if (actor.HandItemId != 0) return "HandFull";
            if (pot == null || pot.Id != intent.ExpectedItemId || pot.Id != intent.ExpectedTargetItemId || pot.DefinitionId != GridKitchenRules.Pot || pot.Location != GridItemLocation.Counter || pot.HolderId != station.Id) return "TargetChanged";
            if (pot.Hygiene != ItemHygiene.Clean) return "DirtyUtensil";
            if (pot.AutoProcessId != intent.ExpectedProcessId) return "ProcessChanged";
            if (pot.AutoProcessId != 0) return "ProcessLocked";
            if ((station.Capabilities & GridStationCapability.Cook) == 0) return "RequiresStove";
            if (station.AutoProcessId != 0) return "StationBusy";
            var inputs = pot.Contents.Select(self.Item).ToArray();
            if (inputs.Any(item => item == null || item.Location != GridItemLocation.Container || item.HolderId != pot.Id)) return "TargetChanged";
            var definitions = inputs.Select(item => item.DefinitionId).ToArray();
            var recipes = Tables.Instance.DTGridAutoRecipeConfig.DataList.Where(recipe => recipe.ContainerDefinitionId == pot.DefinitionId && RecipeMatches(recipe, definitions, true)).ToArray();
            if (recipes.Length != 1) return "NoRecipe";
            DRGridAutoRecipeConfig recipe = recipes[0];
            self.PauseWork(actor);
            var process = self.AddChild<GridAutoProcess>();
            process.RecipeId = recipe.Id; process.PotItemId = pot.Id; process.StartStationId = station.Id; process.StartedTick = self.Tick;
            process.RequiredTicks = self.FrozenCookingTicks(recipe); process.OutputDefinitionId = recipe.OutputDefinitionId;
            process.TotalPortions = recipe.OutputPortions; process.RemainingPortions = 0;
            process.ServingContainerDefinitionId = recipe.ServingContainerDefinitionId; process.State = GridAutoState.Cooking;
            process.InputItemIds.AddRange(pot.Contents);
            self.AutoProcesses.Add(process.Id, process); pot.AutoProcessId = process.Id; station.AutoProcessId = process.Id;
            self.PublishKitchenStage(process, KitchenProcessKind.Soup, KitchenWorkStage.Started, ++process.TransitionSerial);
            return null;
        }

        private static void AdvanceAutoWork(this GridKitchenComponent self)
        {
            foreach (var process in self.AutoProcesses.Values.Select(value => (GridAutoProcess)value))
            {
                if (process.State != GridAutoState.Cooking || process.StartedTick == self.Tick) continue;
                if (process.ProgressTicks < process.RequiredTicks) ++process.ProgressTicks;
                self.Dirty = true;
                if (process.ProgressTicks < process.RequiredTicks) continue;
                if (!process.CompletionReceived)
                {
                    if (!process.CompletionQueued)
                    {
                        self.RequestKitchenCompletion(process, KitchenProcessKind.Soup, process.TransitionSerial);
                        process.CompletionQueued = true;
                    }
                    continue;
                }
                process.State = GridAutoState.Ready; process.RemainingPortions = process.TotalPortions;
                GridStation station = self.Station(process.StartStationId);
                if (station != null && station.AutoProcessId == process.Id) station.AutoProcessId = 0;
                process.CompletionQueued = false; process.CompletionReceived = false;
                self.PublishKitchenStage(process, KitchenProcessKind.Soup, KitchenWorkStage.Finished, ++process.TransitionSerial);
            }
        }

        private static string PortionSoup(this GridKitchenComponent self, GridActor actor, GridStation station, GridIntent intent)
        {
            GridItem bowl = self.Item(actor.HandItemId), pot = self.Item(station.ItemId);
            if (bowl == null || bowl.Id != intent.ExpectedItemId || bowl.Location != GridItemLocation.Hand || bowl.HolderId != actor.MemberId ||
                pot == null || pot.Id != intent.ExpectedTargetItemId || pot.DefinitionId != GridKitchenRules.Pot || pot.Location != GridItemLocation.Counter || pot.HolderId != station.Id) return "TargetChanged";
            GridAutoProcess process = self.AutoProcess(pot.AutoProcessId);
            if (process == null || process.Id != intent.ExpectedProcessId || process.PotItemId != pot.Id) return "ProcessChanged";
            if (process.State != GridAutoState.Ready) return "NotReady";
            if (process.RemainingPortions <= 0) return "EmptyBatch";
            if (bowl.Hygiene != ItemHygiene.Clean || pot.Hygiene != ItemHygiene.Clean) return "DirtyUtensil";
            if (bowl.DefinitionId != process.ServingContainerDefinitionId || !Tables.Instance.DTGridItemConfig.Get(bowl.DefinitionId).AcceptedDefinitionIds.Contains(process.OutputDefinitionId)) return "IncompatibleContainer";
            if (bowl.Contents.Count != 0) return "ContainerFull";
            if (!pot.Contents.SequenceEqual(process.InputItemIds) || process.InputItemIds.Any(id => self.Item(id) == null || self.Item(id).HolderId != pot.Id)) return "TargetChanged";
            self.PauseWork(actor);
            GridItem soup = self.CreateGridItem(process.OutputDefinitionId, GridItemLocation.Container, bowl.Id, bowl.Cell);
            bowl.Contents.Add(soup.Id); --process.RemainingPortions;
            if (process.RemainingPortions == 0)
            {
                foreach (long id in process.InputItemIds) { GridItem input = self.Item(id); self.Items.Remove(id); input.Dispose(); }
                pot.Contents.Clear(); pot.AutoProcessId = 0;
                self.AutoProcesses.Remove(process.Id); process.Dispose();
            }
            return null;
        }
    }

    [EntitySystemOf(typeof(GridAutoProcess))]
    [FriendOf(typeof(GridAutoProcess))]
    public static partial class GridAutoProcessSystem
    {
        [EntitySystem]
        private static void Awake(this GridAutoProcess self)
        {
            self.RecipeId = 0; self.RequiredTicks = 0; self.ProgressTicks = 0; self.OutputDefinitionId = 0;
            self.TransitionSerial = 1; self.CompletionQueued = false; self.CompletionReceived = false;
            self.TotalPortions = 0; self.RemainingPortions = 0; self.ServingContainerDefinitionId = 0;
            self.PotItemId = 0; self.StartStationId = 0; self.StartedTick = 0; self.State = default; self.InputItemIds.Clear();
        }
    }
}
#endif
