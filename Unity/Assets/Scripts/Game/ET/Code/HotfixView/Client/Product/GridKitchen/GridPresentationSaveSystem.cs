namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void RefreshSaveStatus(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (!grid.SaveEnabled || grid.LevelPhase != LevelPhase.Completed) return;
            bool completed = Tables.Instance.DTLevelConfig.Get(grid.LevelConfigId).IsChapterTerminal;
            view.SettlementText.text += grid.SaveState switch
            {
                SoloSaveState.Writing => completed ? "\n正在保存大关完成记录…" : "\n正在保存检查点…",
                SoloSaveState.Failed => "\n保存失败，本次结算保留，请重试",
                SoloSaveState.Committed => completed ? "\n完成记录已保存，正在返回选档" : "\n检查点已保存，可重试继续下一关",
                _ => completed ? "\n只新建完成记录，不覆盖已有记录" : "\n只新建检查点，不覆盖已有记录"
            };
        }
    }
}
