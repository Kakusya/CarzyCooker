using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static string FinishChapterCaption(GridClientComponent grid) => grid.Root().GetComponent<ProductRuntimeComponent>().Config.IsDevelopmentTest
            ? "完成测试并返回列表" : grid.CooperativeSave
            ? "保存完成并返回房间" : IsCooperativeRestaurant(grid) ? "完成大关并返回房间"
            : grid.SaveEnabled ? "保存完成并返回选档" : "结束试玩并返回选档";

        private static string ContinueChapterCaption(GridClientComponent grid) => !grid.SaveEnabled
            ? "下一关" : grid.SaveState == SoloSaveState.Committed ? "继续下一关" : "保存并继续";

        // 只从已提交投影派生提示；不裁决、选择装修、保存文件或创建下一关。
        private static string SettlementNextStep(GridClientComponent grid, bool terminal)
        {
            if (grid.SaveState == SoloSaveState.Writing)
            {
                return "等待保存完成，当前结算保留";
            }
            if (!grid.CanControlRestaurant)
            {
                return terminal ? "等待 Host 确认返回房间" : "等待 Host 处理装修并继续";
            }
            if (!terminal && !RenovationReadyForContinue(grid))
            {
                return "等待装修候选准备，不会自动前进";
            }
            if (!terminal && grid.SaveEnabled && grid.RenovationCandidates.Count != 0 &&
                grid.SelectedRenovationId == 0 && !grid.RenovationSelectionFinalized)
            {
                return grid.CooperativeSave ? "选择装修或本关不选装修" : "先查看并确认一项装修";
            }
            string caption = terminal ? FinishChapterCaption(grid) : ContinueChapterCaption(grid);
            if (grid.SaveState == SoloSaveState.Failed)
            {
                return "保存失败，重试“" + caption + "”";
            }
            return !terminal && !grid.SaveEnabled && grid.SelectedRenovationId == 0 && grid.RenovationCandidates.Count > 0
                ? "可选择一项装修，或点击“" + caption + "”" : "点击“" + caption + "”";
        }

        private static string SettlementRecordStatus(GridClientComponent grid)
        {
            if (grid.Root().GetComponent<ProductRuntimeComponent>().Config.IsDevelopmentTest)
            {
                return "测试运行；不计正式进度";
            }
            if (!grid.SaveEnabled)
            {
                return IsCooperativeRestaurant(grid) ? grid.CooperativeSave ? "合作结算；个人领取另查凭证" : "内存合作；个人领取另查凭证"
                    : "内存试玩；尚未写入记录";
            }
            return grid.SaveState switch
            {
                SoloSaveState.Writing => "保存中；尚未确认提交",
                SoloSaveState.Failed => "保存失败；原结算保留",
                SoloSaveState.Committed => IsCooperativeRestaurant(grid) ? "记录已提交；个人领取另查凭证" : "记录已提交",
                _ => "尚未保存；不会自动写入记录"
            };
        }

        private static string SettlementMenuChanges(GridClientComponent grid, DRLevelConfig next)
        {
            var current = Tables.Instance.DTLevelConfig.Get(grid.LevelConfigId);
            int[] currentMenu = current.AllowedDishIds.Concat(current.AllowedDrinkIds).ToArray();
            int[] nextMenu = next.AllowedDishIds.Concat(next.AllowedDrinkIds).ToArray();
            string Names(int[] ids) => ids.Length == 0 ? "无" : string.Join("、", ids.Select(id => Tables.Instance.DTDishConfig.Get(id).Name));
            return "下一关菜单：" + Names(nextMenu) + "\n新增：" + Names(nextMenu.Except(currentMenu).ToArray()) +
                "\n移出：" + Names(currentMenu.Except(nextMenu).ToArray()) +
                "\n具体生产仍需原解锁、设备和材料资格。旧物和已开始作业沿原规则保留，不补发物资。";
        }

        private static void RefreshSettlementGuidance(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (grid.LevelPhase != LevelPhase.Completed)
            {
                if (self.OverviewPage == GridOverviewPage.Settlement)
                {
                    self.OverviewPage = GridOverviewPage.None;
                }
                return;
            }
            bool terminal = Tables.Instance.DTLevelConfig.Get(grid.LevelConfigId).IsChapterTerminal;
            string action = SettlementNextStep(grid, terminal);
            // 原装修/保存说明留在详情，摘要突出结果、记录事实及唯一下一步。
            string details = view.SettlementText.text + "\n下一步：" + action;
            string destination;
            if (terminal)
            {
                destination = grid.Root().GetComponent<ProductRuntimeComponent>().Config.IsDevelopmentTest
                    ? "测试终点：返回测试列表，无下一关"
                    : IsCooperativeRestaurant(grid) ? "大关终点：返回房间，无下一关" : "大关终点：返回选档，无下一关";
                details += "\n" + destination + "；可以另开独立餐厅。零星不阻止完成。";
            }
            else
            {
                var next = Tables.Instance.DTLevelConfig.GetOrDefault(grid.NextLevelConfigId);
                destination = next == null ? "下一关定义缺失，原继续请求会拒绝" : "下一关：" +
                    (next.ChapterStage > 0 ? "第 " + next.ChapterStage + " 关" : "关卡 " + next.Id) + " · 菜单见结算详情";
                details += "\n" + (next == null ? destination : SettlementMenuChanges(grid, next)) + "\n零星仍可继续，不自动选择下关加成或奖励。";
            }
            if (IsCooperativeRestaurant(grid))
            {
                details += "\nHost控制餐厅继续；结算、文件提交和个人领取是不同状态。个人已领取/可领取/关闭以“合作进度领取”的实际查询为准。";
            }
            view.SettlementText.text = (terminal ? "大关完成" : "本关完成") + $" · {grid.LevelScore} 分 · {grid.LevelStars} 星\n成功 {grid.ServedOrders} 单 · 未满足 {grid.UnfulfilledOrders} 单\n" +
                SettlementRecordStatus(grid) + "\n下一步：" + action + "\n" + destination;
            if (grid.SelectedRenovationId != 0)
            {
                var reward = Tables.Instance.DTRenovationConfig.Get(grid.SelectedRenovationId);
                view.SettlementText.text += "\n已选装修：" + reward.Name;
                if (reward.RewardKind == RenovationRewardKind.UnlockDefinitions)
                {
                    view.SettlementText.text += "，定义已解锁，不补发食材或设备";
                }
                if (grid.SelectedUpgradeStationId != 0 && grid.Stations.TryGetValue(grid.SelectedUpgradeStationId, out var target))
                {
                    view.SettlementText.text += $"\n目标：{target.Name} ({target.Cell.X},{target.Cell.Y}) · 等级 {grid.PendingUpgradeExpectedLevel}→1";
                }
            }
            if (grid.RenovationFailure != null)
            {
                view.SettlementText.text += "\n装修未安装：" + RenovationFailureDescription(grid.RenovationFailure);
            }
            if (self.OverviewPage == GridOverviewPage.Settlement)
            {
                view.SetInformation("结算详情 · 只读", details, true, false);
            }
        }

        private static void AddSettlementGuidance(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (grid.LevelPhase != LevelPhase.Completed)
            {
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            view.AddControl(self.OverviewPage == GridOverviewPage.Settlement ? "关闭结算详情" : "结算详情", () =>
            {
                GridPresentationComponent current = original;
                if (current == null || !current.Ready)
                {
                    return;
                }
                bool close = current.OverviewPage == GridOverviewPage.Settlement;
                current.ClearOrderNavigation();
                current.ResetItemLocator();
                current.ResetSupplyOverview();
                current.ResetFrontWorkboard();
                current.ClearStationInspection();
                current.ClearRenovationPreview();
                current.RecipeVisible = false;
                current.DetailsVisible = false;
                current.CompanionChat = CompanionChatTopic.None;
                current.OverviewPage = close ? GridOverviewPage.None : GridOverviewPage.Settlement;
                current.RefreshGridHud();
            });
        }
    }
}
