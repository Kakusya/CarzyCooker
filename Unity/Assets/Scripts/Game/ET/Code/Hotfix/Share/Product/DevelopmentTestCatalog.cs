#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;
using Game;

namespace ET
{
    public static class DevelopmentTestCatalog
    {
        public static List<DevelopmentTestLevel> Entries()
        {
            var result = new List<DevelopmentTestLevel>();
            foreach (var chapter in Tables.Instance.DTRestaurantChapterConfig.DataList.OrderBy(value => value.Id))
            {
                var seen = new HashSet<int>();
                int id = chapter.SoloFirstLevelId;
                while (id != 0 && seen.Add(id))
                {
                    var level = Tables.Instance.DTLevelConfig.GetOrDefault(id);
                    var entry = new DevelopmentTestLevel { LevelId = id, Name = chapter.Name + " · 第 " + (level?.ChapterStage.ToString() ?? "?") + " 小关", Group = DevelopmentTestGroup.Chapter };
                    if (level == null || level.ChapterId != chapter.SoloChapterId || level.MenuVariant != chapter.MenuVariant)
                    {
                        entry.Failure = "InvalidDevelopmentChapterRoute";
                    }
                    else
                    {
                        entry.Failure = ValidateStart(level);
                    }
                    result.Add(entry);
                    if (level == null || level.IsChapterTerminal)
                    {
                        break;
                    }
                    id = level.NextLevelConfigId;
                }
            }
            Add("基础操作", ProductMenuVariant.Basic);
            Add("饮品基础", ProductMenuVariant.Drink);
            Add("沙拉制作", ProductMenuVariant.Salad);
            Add("经营成长", ProductMenuVariant.Growth);
            Add("热餐制作", ProductMenuVariant.Hot);
            Add("甜品制作", ProductMenuVariant.Dessert);
            Add("咖啡制作", ProductMenuVariant.Cafe);
            Add("蒸食制作", ProductMenuVariant.Steamed);
            Add("茶饮制作", ProductMenuVariant.Tea);
            return result;

            void Add(string name, ProductMenuVariant menu)
            {
                try
                {
                    var level = RestaurantChapters.First(0, RuntimeInstanceRole.Solo, menu);
                    result.Add(new DevelopmentTestLevel { LevelId = level.Id, Name = name, Group = DevelopmentTestGroup.Feature, Failure = ValidateStart(level) });
                }
                catch (InvalidOperationException error)
                {
                    result.Add(new DevelopmentTestLevel { Name = name, Group = DevelopmentTestGroup.Feature, Failure = error.Message });
                }
            }
        }

        private static string ValidateStart(DRLevelConfig level)
        {
            try
            {
                var unlocked = RestaurantUnlockValues.Initial(level);
                RestaurantUnlockValues.Validate(unlocked);
                var permit = new ProductionPermit();
                var menu = level.AllowedDishIds.Concat(level.AllowedDrinkIds).ToArray();
                ProductionPermissionRules.Rebuild(permit, menu.Where(unlocked.DishIds.Contains), unlocked.StationIds);
                if (permit.DishIds.Count == 0 || RestaurantChapters.Find(level) != null && !permit.DishIds.SetEquals(menu))
                {
                    return "DevelopmentTestMissingProductionPermission";
                }
                var layout = Tables.Instance.DTInitialLayoutConfig.DataList.Where(row => ProductMenuCatalog.Includes(level.MenuVariant, row.MenuVariant)).ToArray();
                GridStationConfig.ValidateInitialLayout(Tables.Instance.DTStationConfig.DataList, layout,
                    Tables.Instance.DTGridItemConfig.DataList, new[] { new GridCell(level.SpawnX, level.SpawnY) });
                GridDishConfig.ValidateMenu(menu, Tables.Instance.DTDishConfig.DataList, Tables.Instance.DTDishStageConfig.DataList,
                    Tables.Instance.DTGridItemConfig.DataList, Tables.Instance.DTGridManualRecipeConfig.DataList,
                    Tables.Instance.DTGridAutoRecipeConfig.DataList, Tables.Instance.DTGridBakeRecipeConfig.DataList,
                    Tables.Instance.DTStationConfig.DataList, layout,
                    Tables.Instance.DTSupplyConfig.DataList.Where(row => ProductMenuCatalog.Includes(level.MenuVariant, row.MenuVariant)));
                if (Tables.Instance.DTCustomerFlowConfig.DataList.Count(row => row.LevelConfigId == level.Id) != 1)
                {
                    return "DevelopmentTestMissingCustomerFlow";
                }
                return null;
            }
            catch (InvalidOperationException error)
            {
                return error.Message;
            }
        }

        public static DRLevelConfig Resolve(int levelId)
        {
            DevelopmentTestLevel entry = Entries().SingleOrDefault(value => value.LevelId == levelId);
            if (entry == null || entry.Failure != null)
            {
                throw new InvalidOperationException(entry?.Failure ?? "UnknownDevelopmentTestLevel");
            }
            return Tables.Instance.DTLevelConfig.Get(levelId);
        }
    }
}
#endif
