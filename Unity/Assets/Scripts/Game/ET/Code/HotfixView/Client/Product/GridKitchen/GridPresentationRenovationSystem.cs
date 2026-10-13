using System;
using System.Linq;
using Game;

namespace ET.Client
{
    [FriendOf(typeof(ProductRuntimeComponent))]
    public static partial class GridPresentationSystem
    {
        private static bool RenovationReadyForContinue(GridClientComponent grid)
        {
            ProductRuntimeComponent runtime = grid.Root().GetComponent<ProductRuntimeComponent>();
            return runtime?.Config.Role != RuntimeInstanceRole.Solo ||
                grid.RenovationLevelId == grid.LevelId && grid.RenovationLevelGeneration == grid.LevelGeneration && grid.RenovationChoiceVersion > 0;
        }
        private static void RefreshRenovationStatus(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (grid.RenovationApplied.Count > 0)
                view.HintText.text += " · 已装修：" + string.Join("、",grid.RenovationApplied.Keys.OrderBy(value=>value).Select(id=>Tables.Instance.DTRenovationConfig.Get(id).Name));
            if (grid.LevelPhase != LevelPhase.Completed) return;
            if (!RenovationReadyForContinue(grid))
            {
                view.SettlementText.text = $"本关完成\n{grid.LevelScore} 分 · {grid.LevelStars} 星\n正在准备装修候选";
                return;
            }
            if (grid.RenovationLevelId != grid.LevelId || grid.RenovationLevelGeneration != grid.LevelGeneration) return;
            if (grid.SelectedRenovationId != 0)
            {
                view.SettlementText.text += "\n已选装修：" + Tables.Instance.DTRenovationConfig.Get(grid.SelectedRenovationId).Name + "，下一关安装";
                return;
            }
            if (grid.RenovationCandidates.Count == 0) { view.SettlementText.text += "\n当前没有可选装修，可以继续"; return; }
            view.SettlementText.text += grid.SaveEnabled ? "\n选择一项装修后保存并继续" : "\n可选择一项装修，或直接继续";
        }

        private static void AddRenovationTargets(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (grid.LevelPhase != LevelPhase.Completed || grid.RenovationLevelId != grid.LevelId || grid.RenovationLevelGeneration != grid.LevelGeneration ||
                grid.SelectedRenovationId != 0) return;
            EntityRef<GridPresentationComponent> selfRef = self;
            long level = grid.LevelId, generation = grid.LevelGeneration, version = grid.RenovationChoiceVersion;
            foreach (int id in grid.RenovationCandidates)
            {
                string name = Tables.Instance.DTRenovationConfig.Get(id).Name;
                view.AddTarget("装修 " + name, () => { GridPresentationComponent current = selfRef; current?.SendRenovationChoice(id, version, level, generation); });
            }
        }

        public static void SendRenovationChoice(this GridPresentationComponent self, int definition, long choiceVersion, long expectedLevel, long expectedGeneration)
        {
            var owner = self.GridCommandOwner(); if (owner == null) return;
            EntityRef<GridPresentationComponent> selfRef = self;
            owner.Run(async context =>
            {
                ClientSessionComponent client = ((Scene)self.IScene).GetComponent<ClientSessionComponent>();
                GridClientComponent grid = client.GetComponent<GridClientComponent>(); EntityRef<GridClientComponent> gridRef = grid;
                if (grid.LevelId != expectedLevel || grid.LevelGeneration != expectedGeneration || grid.RenovationChoiceVersion != choiceVersion ||
                    grid.SelectedRenovationId != 0 || grid.LevelPhase != LevelPhase.Completed) return;
                ChooseRenovationRequest request = grid.CreateRenovationChoice(definition, choiceVersion);
                long command = request.CommandId, worldGeneration = grid.WorldGeneration;
                try
                {
                    string error = await client.CallProductValueAsync(context, request, response => response.Error == 0 ? null : response.Message);
                    context.ThrowIfStopped(); self = selfRef; grid = gridRef;
                    if (self == null || !self.Ready || grid == null || grid.WorldGeneration != worldGeneration || grid.LevelId != expectedLevel ||
                        grid.LevelGeneration != expectedGeneration || grid.LastCommandId != command) return;
                    if (error != null) { grid.LastStatus = GridResultStatus.Rejected; grid.LastReason = error; self.RefreshGridHud(); }
                }
                catch (OperationCanceledException) { }
                catch (Exception exception) { self = selfRef; if (self != null) { self.Failure = exception.Message; self.RefreshGridHud(); } }
            });
        }
    }
}
