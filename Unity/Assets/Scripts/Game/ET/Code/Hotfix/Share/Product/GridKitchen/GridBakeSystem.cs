#if !DOTNET
using System.Linq;

namespace ET
{
    [FriendOf(typeof(GridBakeProcess))]
    public static partial class GridKitchenSystem
    {
        public static GridBakeProcess BakeProcess(this GridKitchenComponent self, long id) => self.BakeProcesses.TryGetValue(id, out var value) ? value : null;

        private static string StartBake(this GridKitchenComponent self, GridActor actor, GridStation station, GridIntent intent)
        {
            GridItem input = self.Item(station.ItemId);
            if (actor.HandItemId != 0) return "HandFull";
            if (input == null || input.Id != intent.ExpectedItemId || input.Id != intent.ExpectedTargetItemId ||
                input.Location != GridItemLocation.Counter || input.HolderId != station.Id) return "TargetChanged";
            if ((station.Capabilities & GridStationCapability.Bake) == 0) return "RequiresOven";
            if (station.BakeProcessId != 0 || input.BakeProcessId != 0 || self.ItemWorking(input.Id)) return "WorkBusy";
            if (intent.ExpectedProcessId != 0) return "ProcessChanged";
            var recipes = Tables.Instance.DTGridBakeRecipeConfig.DataList.Where(row => row.InputDefinitionId == input.DefinitionId &&
                (station.Capabilities & (GridStationCapability)row.StationCapability) == (GridStationCapability)row.StationCapability).ToArray();
            if (recipes.Length != 1) return "NoRecipe";
            var recipe = recipes[0]; self.PauseWork(actor);
            GridBakeProcess process = self.AddChild<GridBakeProcess>();
            process.RecipeId = recipe.Id; process.StationId = station.Id; process.InputItemId = input.Id;
            process.OutputDefinitionId = recipe.OutputDefinitionId; process.RequiredTicks = self.FrozenBakingTicks(recipe, station);
            process.ProgressTicks = 0; process.State = GridBakeState.Baking; process.StartedTick = self.Tick;
            self.BakeProcesses.Add(process.Id, process); station.BakeProcessId = process.Id; input.BakeProcessId = process.Id;
            self.PublishKitchenStage(process, KitchenProcessKind.Bake, KitchenWorkStage.Started, ++process.TransitionSerial);
            return null;
        }

        private static void AdvanceBakeWork(this GridKitchenComponent self)
        {
            foreach (GridBakeProcess process in self.BakeProcesses.Values.Select(value => (GridBakeProcess)value))
            {
                if (process.State != GridBakeState.Baking || process.StartedTick == self.Tick) continue;
                GridStation station = self.Station(process.StationId); GridItem input = self.Item(process.InputItemId);
                if (station == null || input == null || station.BakeProcessId != process.Id || input.BakeProcessId != process.Id ||
                    station.ItemId != input.Id || input.HolderId != station.Id || input.Location != GridItemLocation.Counter) continue;
                if (process.ProgressTicks < process.RequiredTicks) ++process.ProgressTicks;
                self.Dirty = true;
                if (process.ProgressTicks < process.RequiredTicks) continue;
                if (!process.CompletionReceived)
                {
                    if (!process.CompletionQueued)
                    {
                        self.RequestKitchenCompletion(process, KitchenProcessKind.Bake, process.TransitionSerial);
                        process.CompletionQueued = true;
                    }
                    continue;
                }
                GridItem output = self.CreateGridItem(process.OutputDefinitionId, GridItemLocation.Counter, station.Id, station.Cell);
                station.ItemId = output.Id; station.BakeProcessId = 0;
                self.Items.Remove(input.Id); input.Dispose();
                process.OutputItemId = output.Id; process.State = GridBakeState.Completed;
                process.CompletionQueued = false; process.CompletionReceived = false;
                self.PublishKitchenStage(process, KitchenProcessKind.Bake, KitchenWorkStage.Finished, ++process.TransitionSerial, output.Id);
            }
        }

        public static int IdentifyDish(this GridKitchenComponent self, long containerId)
        {
            GridItem container = self.Item(containerId);
            if (container == null || container.Contents.Count != 1) return 0;
            GridItem content = self.Item(container.Contents[0]);
            if (content == null || content.Location != GridItemLocation.Container || content.HolderId != container.Id) return 0;
            return Tables.Instance.DTDishConfig.DataList.FirstOrDefault(row => row.ContainerDefinitionId == container.DefinitionId &&
                row.ContentDefinitionId == content.DefinitionId)?.Id ?? 0;
        }
    }

    [EntitySystemOf(typeof(GridBakeProcess))]
    [FriendOf(typeof(GridBakeProcess))]
    public static partial class GridBakeProcessSystem
    {
        [EntitySystem]
        private static void Awake(this GridBakeProcess self)
        {
            self.RecipeId = 0; self.RequiredTicks = 0; self.ProgressTicks = 0; self.OutputDefinitionId = 0;
            self.TransitionSerial = 1; self.CompletionQueued = false; self.CompletionReceived = false;
            self.StationId = 0; self.InputItemId = 0; self.OutputItemId = 0; self.StartedTick = 0; self.State = default;
        }
    }
}
#endif
