#if !DOTNET
using System;
using System.Linq;
using Game;

namespace ET
{
    public static class RestaurantChapters
    {
        public static DRLevelConfig First(int contentChapterId, RuntimeInstanceRole role, int menuVariant)
        {
            if (role is not (RuntimeInstanceRole.Solo or RuntimeInstanceRole.Host))
            {
                throw new InvalidOperationException("HostOrSoloOnly");
            }
            if (contentChapterId == 0)
            {
                return role == RuntimeInstanceRole.Solo ? Tables.Instance.DTLevelConfig.DataList.Single(row =>
                    row.ChapterId == 1 && row.ChapterStage == 1 && row.MenuVariant == menuVariant) : RestaurantConfig.FirstCooperativeLevel(menuVariant);
            }
            var chapter = Tables.Instance.DTRestaurantChapterConfig.GetOrDefault(contentChapterId);
            if (chapter == null || (int)chapter.MenuVariant != menuVariant)
            {
                throw new InvalidOperationException("InvalidContentChapter");
            }
            return Tables.Instance.DTLevelConfig.Get(role == RuntimeInstanceRole.Solo ? chapter.SoloFirstLevelId : chapter.CooperativeFirstLevelId);
        }

        public static DRRestaurantChapterConfig Find(DRLevelConfig level)
        {
            if (level == null)
            {
                return null;
            }
            return Tables.Instance.DTRestaurantChapterConfig.DataList.FirstOrDefault(chapter =>
                chapter.SoloChapterId == level.ChapterId || chapter.CooperativeChapterId == level.ChapterId);
        }

        public static string Label(int levelId)
        {
            var level = Tables.Instance.DTLevelConfig.GetOrDefault(levelId); var chapter = Find(level);
            return chapter == null ? "关" + levelId : chapter.Name + " · 第 " + level.ChapterStage + "/3 关";
        }

        public static void ValidateAll()
        {
            var chapters = Tables.Instance.DTRestaurantChapterConfig.DataList;
            if (chapters.Count != 2 || chapters.Select(row => row.Id).Distinct().Count() != chapters.Count ||
                chapters.SelectMany(row => new[] { row.SoloChapterId, row.CooperativeChapterId }).Distinct().Count() != chapters.Count * 2)
            {
                throw new InvalidOperationException("InvalidContentChapters");
            }
            foreach (var chapter in chapters)
            {
                if (chapter.Id <= 0 || string.IsNullOrWhiteSpace(chapter.Name) || !ProductMenuCatalog.IsValid((int)chapter.MenuVariant) ||
                    chapter.SoloChapterId <= 2 || chapter.CooperativeChapterId <= 2)
                {
                    throw new InvalidOperationException("InvalidContentChapter");
                }
                ValidateRoute(chapter, chapter.SoloChapterId, chapter.SoloFirstLevelId);
                ValidateRoute(chapter, chapter.CooperativeChapterId, chapter.CooperativeFirstLevelId);
            }
        }

        private static void ValidateRoute(DRRestaurantChapterConfig chapter, int chain, int firstId)
        {
            var levels = Tables.Instance.DTLevelConfig.DataList.Where(row => row.ChapterId == chain && row.MenuVariant == (int)chapter.MenuVariant)
                .OrderBy(row => row.ChapterStage).ToArray();
            if (levels.Length != 3 || levels[0].Id != firstId)
            {
                throw new InvalidOperationException("InvalidContentChapterRoute");
            }
            var unlocked = RestaurantUnlockValues.Initial(levels[0]); var permit = new ProductionPermit();
            for (int index = 0; index < levels.Length; ++index)
            {
                var level = levels[index];
                var menu = level.AllowedDishIds.Concat(level.AllowedDrinkIds).ToArray();
                ProductionPermissionRules.Rebuild(permit, menu.Where(unlocked.DishIds.Contains), unlocked.StationIds);
                if (level.ChapterStage != index + 1 || level.IsChapterTerminal != (index == 2) ||
                    level.NextLevelConfigId != (index == 2 ? 0 : levels[index + 1].Id) || !permit.DishIds.SetEquals(menu))
                {
                    throw new InvalidOperationException("InvalidContentChapterProduction");
                }
            }
        }
    }
}
#endif
