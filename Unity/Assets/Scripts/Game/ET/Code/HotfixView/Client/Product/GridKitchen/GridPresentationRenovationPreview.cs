using System.Collections.Generic;
using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void ClearRenovationPreview(this GridPresentationComponent self)
        {
            self.RenovationPreviewId = self.RenovationPreviewExpectedLevel = 0;
            self.RenovationPreviewStationId = self.RenovationPreviewVersion = self.RenovationPreviewLevelId = self.RenovationPreviewGeneration = 0;
            if (self.OverviewPage == GridOverviewPage.Renovation)
            {
                self.OverviewPage = GridOverviewPage.None;
            }
        }

        private static bool RenovationPreviewIsCurrent(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (self.RenovationPreviewId == 0 || grid.LevelPhase != LevelPhase.Completed || grid.LevelId != self.RenovationPreviewLevelId ||
                grid.LevelGeneration != self.RenovationPreviewGeneration || grid.RenovationChoiceVersion != self.RenovationPreviewVersion ||
                grid.SelectedRenovationId != 0 || grid.RenovationSelectionFinalized || !grid.RenovationCandidates.Contains(self.RenovationPreviewId))
            {
                return false;
            }
            if (self.RenovationPreviewStationId == 0)
            {
                return Tables.Instance.DTRenovationConfig.Get(self.RenovationPreviewId).RewardKind != RenovationRewardKind.UpgradeStation;
            }
            return grid.Stations.TryGetValue(self.RenovationPreviewStationId, out var station) && station.UpgradeLevel == self.RenovationPreviewExpectedLevel &&
                grid.UpgradeTargets.Any(target => target.DefinitionId == self.RenovationPreviewId && target.StationId == station.Id && target.ExpectedLevel == station.UpgradeLevel);
        }

        private static void ObserveRenovationPreview(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (self.RenovationPreviewId != 0 && (self.OverviewPage != GridOverviewPage.Renovation || !self.RenovationPreviewIsCurrent(grid)))
            {
                self.ClearRenovationPreview();
            }
        }

        private static void OpenRenovationPreview(this GridPresentationComponent self, int definition, long station, int upgradeLevel, long version, long level, long generation)
        {
            GridClientComponent grid = ((Scene)self.IScene).GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
            if (!self.Ready || grid == null)
            {
                return;
            }
            self.ClearRenovationPreview();
            self.RenovationPreviewId = definition; self.RenovationPreviewStationId = station; self.RenovationPreviewExpectedLevel = upgradeLevel;
            self.RenovationPreviewVersion = version; self.RenovationPreviewLevelId = level; self.RenovationPreviewGeneration = generation;
            if (!self.RenovationPreviewIsCurrent(grid))
            {
                self.ClearRenovationPreview();
                return;
            }
            self.CloseItemLocator(); self.ClearOrderNavigation(); self.ClearStationInspection();
            self.OverviewPage = GridOverviewPage.Renovation;
            self.RecipeVisible = self.DetailsVisible = false; self.CompanionChat = CompanionChatTopic.None;
            self.ActionGroup = GridActionGroup.Layout;
            self.RefreshGridHud();
        }

        private static string UpgradePreview(GridClientComponent grid, GridStationProjection station, DRStationUpgradeConfig upgrade)
        {
            var lines = new List<string>();
            var old = station.UpgradeLevel == 0 ? null : Tables.Instance.DTStationUpgradeConfig.Get(station.UpgradeDefinitionId);
            void Compare(int basis, int output)
            {
                int before = StationUpgradeConfig.CombinedTicks(basis, 1, 1, old?.DurationNumerator ?? 1, old?.DurationDenominator ?? 1);
                int after = StationUpgradeConfig.CombinedTicks(basis, 1, 1, upgrade.DurationNumerator, upgrade.DurationDenominator);
                lines.Add(Tables.Instance.DTGridItemConfig.Get(output).Name + $"：{before * grid.TickMilliseconds / 1000f:0.00} → {after * grid.TickMilliseconds / 1000f:0.00} 秒");
            }
            if (upgrade.WorkKind == StationWorkKind.Chop)
            {
                foreach (var recipe in Tables.Instance.DTGridManualRecipeConfig.DataList.Where(recipe => recipe.Mode == GridManualMode.Chop))
                {
                    Compare(recipe.RequiredTicks, recipe.OutputDefinitionId);
                }
            }
            else if (upgrade.WorkKind == StationWorkKind.Batch)
            {
                foreach (var recipe in Tables.Instance.DTGridAutoRecipeConfig.DataList.Where(recipe =>
                    (station.Capabilities & recipe.StationCapability) == recipe.StationCapability))
                {
                    Compare(recipe.RequiredTicks, recipe.OutputDefinitionId);
                }
            }
            else if (upgrade.WorkKind == StationWorkKind.Bake)
            {
                foreach (var recipe in Tables.Instance.DTGridBakeRecipeConfig.DataList.Where(recipe =>
                    (station.Capabilities & recipe.StationCapability) == recipe.StationCapability))
                {
                    Compare(recipe.RequiredTicks, recipe.OutputDefinitionId);
                }
            }
            return "升级原工位：" + station.Name + $" ({station.Cell.X},{station.Cell.Y})\n等级 {station.UpgradeLevel} → {upgrade.TargetLevel}" +
                "\n配置匹配工序的基础耗时（不含本关加成）：\n" + string.Join("\n", lines) +
                "\n只影响安装后的新作业；原已开始作业保持冻结耗时。实际可生产工序仍取当前菜单许可。";
        }

        private static void RefreshRenovationPreview(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Renovation || !self.RenovationPreviewIsCurrent(grid))
            {
                return;
            }
            var definition = Tables.Instance.DTRenovationConfig.Get(self.RenovationPreviewId);
            string text = definition.RewardKind switch
            {
                RenovationRewardKind.InstallStation => "新增工位：" + Tables.Instance.DTStationConfig.Get(definition.StationDefinitionId).Name +
                    $" ×{definition.Quantity}\n安装锚点：({definition.InstallX},{definition.InstallY})\n下一关安装，不补原料；安装时仍要完整验证空间和通路。",
                RenovationRewardKind.UpgradeStation => UpgradePreview(grid, grid.Stations[self.RenovationPreviewStationId], Tables.Instance.DTStationUpgradeConfig.Get(definition.UpgradeDefinitionId)),
                RenovationRewardKind.UnlockDefinitions => "解锁菜品：" + string.Join("、", definition.UnlockDishIds.Select(id => Tables.Instance.DTDishConfig.Get(id).Name)) +
                    "\n解锁设备：" + string.Join("、", definition.UnlockStationIds.Select(id => Tables.Instance.DTStationConfig.Get(id).Name)) +
                    "\n只开放稳定定义，不附赠食材或设备；新生产仍取本关允许集。",
                RenovationRewardKind.ExpandSpace => $"扩建区域：{grid.Width}×{grid.Height} → {definition.ExpansionWidth}×{definition.ExpansionHeight}" +
                    "\n新增桌位：" + Tables.Instance.DTStationConfig.Get(definition.TableDefinitionId).Name +
                    $" ×{definition.Quantity}\n安装锚点：({definition.InstallX},{definition.InstallY})\n不补顾客或餐具；下一关完整预检后整体安装，预览不是安装保证。",
                _ => "当前奖励类型不可用，请返回候选"
            };
            text += grid.CanControlRestaurant ? "\n\n取消不会选定奖励，明确确认后才提交原选择。" : "\n\n仅查看，实际选择由Host确认。";
            view.SetInformation("装修预览：" + definition.Name, text, true, self.OrdersVisible);
        }

        private static void AddRenovationPreviewControls(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Renovation || !self.RenovationPreviewIsCurrent(grid))
            {
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            int definition = self.RenovationPreviewId, upgrade = self.RenovationPreviewExpectedLevel;
            long station = self.RenovationPreviewStationId, version = self.RenovationPreviewVersion, level = self.RenovationPreviewLevelId, generation = self.RenovationPreviewGeneration;
            view.AddControl("取消装修预览", () =>
            {
                GridPresentationComponent current = original;
                if (current != null)
                {
                    current.ClearRenovationPreview(); current.RefreshGridHud();
                }
            });
            if (grid.CanControlRestaurant)
            {
                view.AddControl("确认此装修", () =>
                {
                    GridPresentationComponent current = original;
                    GridClientComponent latest = ((Scene)current?.IScene)?.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
                    if (current == null || latest == null || !latest.CanControlRestaurant || !current.RenovationPreviewIsCurrent(latest) ||
                        current.RenovationPreviewId != definition || current.RenovationPreviewStationId != station || current.RenovationPreviewVersion != version)
                    {
                        return;
                    }
                    current.SendRenovationChoice(definition, version, level, generation, station, upgrade);
                    // 只关闭显示，不结束原UI owner或在途请求；提交效果仍等原快照。
                    current.ClearRenovationPreview(); current.RefreshGridHud();
                });
            }
        }
    }
}
