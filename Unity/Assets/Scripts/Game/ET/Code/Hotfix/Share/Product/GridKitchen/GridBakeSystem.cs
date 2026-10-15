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
            if (actor.HandItemId != 0)
            {
                return "HandFull";
            }
            GridItem slot = self.Item(station.ItemId);
            GridItem input = self.Item(intent.ExpectedItemId);
            if (slot == null || slot.Id != intent.ExpectedTargetItemId || slot.Location != GridItemLocation.Counter || slot.HolderId != station.Id || input == null)
            {
                return "TargetChanged";
            }
            GridItem carrier = slot.Id == input.Id ? null : slot;
            if (carrier == null ? input.Location != GridItemLocation.Counter || input.HolderId != station.Id :
                input.Location != GridItemLocation.Container || input.HolderId != carrier.Id || carrier.Contents.Count != 1 ||
                carrier.Contents[0] != input.Id || !carrier.Contents.SequenceEqual(intent.ExpectedContentIds))
            {
                return "TargetChanged";
            }
            GridStationCapability capability = intent.Action switch
            {
                GridAction.StartGrind => GridStationCapability.Grind,
                GridAction.StartFry => GridStationCapability.Fry,
                _ => GridStationCapability.Bake
            };
            if ((station.Capabilities & capability) == 0)
            {
                return capability switch
                {
                    GridStationCapability.Grind => "RequiresGrinder",
                    GridStationCapability.Fry => "RequiresFryingStation",
                    _ => "RequiresOven"
                };
            }
            if (station.BakeProcessId != 0 || input.BakeProcessId != 0 || self.ItemWorking(input.Id) || self.ItemWorking(slot.Id))
            {
                return "WorkBusy";
            }
            if (intent.ExpectedProcessId != 0)
            {
                return "ProcessChanged";
            }
            if (input.Hygiene != ItemHygiene.Clean || slot.Hygiene != ItemHygiene.Clean)
            {
                return "DirtyUtensil";
            }
            var recipes = Tables.Instance.DTGridBakeRecipeConfig.DataList.Where(row => row.InputDefinitionId == input.DefinitionId &&
                row.StationCapability == (int)capability && row.CarrierDefinitionId == (carrier?.DefinitionId ?? 0)).ToArray();
            if (recipes.Length != 1)
            {
                return "NoRecipe";
            }
            var recipe = recipes[0];
            string permission = self.CheckNewRecipePermission(self.ProductLevel()?.ProductionPermit.BakeRecipes.Contains(recipe.Id) ?? true, station);
            if (permission != null)
            {
                return permission;
            }
            self.PauseWork(actor);
            GridBakeProcess process = self.AddChild<GridBakeProcess>();
            process.RecipeId = recipe.Id; process.StationId = station.Id; process.InputItemId = input.Id;
            process.CarrierItemId = carrier?.Id ?? 0;
            process.OutputDefinitionId = recipe.OutputDefinitionId; process.RequiredTicks = self.FrozenBakingTicks(recipe, station);
            process.ProgressTicks = 0; process.State = GridBakeState.Baking; process.StartedTick = self.Tick;
            self.BakeProcesses.Add(process.Id, process); station.BakeProcessId = process.Id; input.BakeProcessId = process.Id;
            if (carrier != null)
            {
                carrier.BakeProcessId = process.Id;
            }
            self.PublishKitchenStage(process, KitchenProcessKind.Bake, KitchenWorkStage.Started, ++process.TransitionSerial);
            return null;
        }

        private static void AdvanceBakeWork(this GridKitchenComponent self)
        {
            foreach (var reference in self.BakeProcesses.Values)
            {
                GridBakeProcess process = reference;
                if (process.State != GridBakeState.Baking || process.StartedTick == self.Tick)
                {
                    continue;
                }
                GridStation station = self.Station(process.StationId);
                GridItem input = self.Item(process.InputItemId);
                GridItem carrier = self.Item(process.CarrierItemId);
                if (station == null || input == null || station.BakeProcessId != process.Id || input.BakeProcessId != process.Id)
                {
                    continue;
                }
                if (process.CarrierItemId == 0)
                {
                    if (station.ItemId != input.Id || input.HolderId != station.Id || input.Location != GridItemLocation.Counter)
                    {
                        continue;
                    }
                }
                else if (carrier == null || station.ItemId != carrier.Id || carrier.HolderId != station.Id || carrier.Location != GridItemLocation.Counter ||
                    carrier.BakeProcessId != process.Id || carrier.Contents.Count != 1 || carrier.Contents[0] != input.Id ||
                    input.Location != GridItemLocation.Container || input.HolderId != carrier.Id)
                {
                    continue;
                }
                if (process.ProgressTicks < process.RequiredTicks)
                {
                    ++process.ProgressTicks;
                }
                self.Dirty = true;
                if (process.ProgressTicks < process.RequiredTicks)
                {
                    continue;
                }
                if (!process.CompletionReceived)
                {
                    if (!process.CompletionQueued)
                    {
                        self.RequestKitchenCompletion(process, KitchenProcessKind.Bake, process.TransitionSerial);
                        process.CompletionQueued = true;
                    }
                    continue;
                }
                GridItem output;
                if (carrier == null)
                {
                    output = self.CreateGridItem(process.OutputDefinitionId, GridItemLocation.Counter, station.Id, station.Cell);
                    station.ItemId = output.Id;
                }
                else
                {
                    output = self.CreateGridItem(process.OutputDefinitionId, GridItemLocation.Container, carrier.Id, carrier.Cell);
                    carrier.Contents[0] = output.Id;
                    carrier.BakeProcessId = 0;
                }
                station.BakeProcessId = 0;
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
            self.StationId = 0; self.InputItemId = 0; self.OutputItemId = 0; self.CarrierItemId = 0; self.StartedTick = 0; self.State = default;
        }
    }
}
#endif
