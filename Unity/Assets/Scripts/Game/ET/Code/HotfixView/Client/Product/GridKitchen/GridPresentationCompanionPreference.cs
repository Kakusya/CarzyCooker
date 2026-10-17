using System;
using Game;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static string CompanionPreferenceCaption(CompanionWorkPreference preference) => preference switch
        {
            CompanionWorkPreference.AskFirst => "询问优先",
            CompanionWorkPreference.WashFirst => "洗碗优先",
            _ => "自动（询问优先）"
        };

        private static void AddCompanionPreferenceTargets(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (grid.Companions.Count == 0 || !grid.CanControlRestaurant || grid.LevelPaused ||
                grid.LevelPhase is not (LevelPhase.Preparing or LevelPhase.Operating or LevelPhase.Draining))
            {
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            long level = grid.LevelId, generation = grid.LevelGeneration;
            foreach (var preference in new[] { CompanionWorkPreference.Automatic, CompanionWorkPreference.AskFirst, CompanionWorkPreference.WashFirst })
            {
                view.AddTarget("岗位：" + CompanionPreferenceCaption(preference) + (preference == grid.CompanionPreference ? " · 当前" : ""), () =>
                {
                    GridPresentationComponent current = original;
                    current?.SendCompanionPreference(preference, level, generation);
                }, group: GridActionGroup.Companions);
            }
        }

        public static void SendCompanionPreference(this GridPresentationComponent self, CompanionWorkPreference preference, long level, long levelGeneration)
        {
            var owner = self.GridCommandOwner();
            if (owner == null)
            {
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            owner.Run(async context =>
            {
                var client = ((Scene)self.IScene).GetComponent<ClientSessionComponent>();
                var grid = client?.GetComponent<GridClientComponent>();
                if (grid?.Confirmed != true || !grid.CanControlRestaurant || grid.LevelPaused || grid.LevelId != level || grid.LevelGeneration != levelGeneration ||
                    grid.LevelPhase is not (LevelPhase.Preparing or LevelPhase.Operating or LevelPhase.Draining))
                {
                    return;
                }
                EntityRef<GridClientComponent> projection = grid;
                var request = grid.CreateCompanionPreference(preference);
                long command = request.CommandId, binding = grid.BindingGeneration, worldGeneration = grid.WorldGeneration;
                string world = grid.WorldId;
                try
                {
                    string error = await client.CallProductValueAsync(context, request, response => response.Error == 0 ? null : response.Message);
                    context.ThrowIfStopped();
                    GridPresentationComponent current = original;
                    grid = projection;
                    if (current == null || !current.Ready || grid == null || grid.WorldId != world || grid.WorldGeneration != worldGeneration ||
                        grid.BindingGeneration != binding || grid.LevelId != level || grid.LevelGeneration != levelGeneration || grid.LastCommandId != command)
                    {
                        return;
                    }
                    if (error != null)
                    {
                        grid.LastStatus = GridResultStatus.Rejected;
                        grid.LastReason = error;
                        current.RefreshGridHud();
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception error)
                {
                    GridPresentationComponent current = original;
                    if (current != null)
                    {
                        current.Failure = error.Message;
                        current.RefreshGridHud();
                    }
                }
            });
        }
    }
}
