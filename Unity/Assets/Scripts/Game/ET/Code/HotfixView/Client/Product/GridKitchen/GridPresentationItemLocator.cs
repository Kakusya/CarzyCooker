using System;
using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void ResetItemLocator(this GridPresentationComponent self)
        {
            self.ItemLocatorQuery = self.ItemLocatorInput = "";
            self.ItemLocatorWorld = null;
            self.ItemLocatorGeneration = self.ItemLocatorSelectedId = 0;
            self.ItemLocatorKind = null;
            self.ItemLocatorDetailsVisible = false;
            if (self.ActionGroup == GridActionGroup.Items)
            {
                self.ActionGroup = GridActionGroup.All;
            }
        }

        private static void CloseItemLocator(this GridPresentationComponent self)
        {
            if (self.OverviewPage == GridOverviewPage.Items)
            {
                self.OverviewPage = GridOverviewPage.None;
                self.ResetItemLocator();
            }
        }

        private static void OpenItemLocator(this GridPresentationComponent self, GridClientComponent grid, long selectedId = 0)
        {
            self.ClearOrderNavigation();
            self.ResetItemLocator();
            self.OverviewPage = GridOverviewPage.Items;
            self.ItemLocatorSelectedId = selectedId;
            self.ItemLocatorDetailsVisible = selectedId == 0;
            self.ItemLocatorWorld = grid.WorldId;
            self.ItemLocatorGeneration = grid.WorldGeneration;
            self.ActionGroup = GridActionGroup.Items;
            self.RecipeVisible = self.DetailsVisible = false;
            self.CompanionChat = CompanionChatTopic.None;
            self.RefreshGridHud();
        }

        private static bool LocatorMatches(this GridPresentationComponent self, GridItemProjection item)
        {
            return (self.ItemLocatorKind == null || Tables.Instance.DTGridItemConfig.Get(item.DefinitionId).Kind == self.ItemLocatorKind) &&
                (string.IsNullOrWhiteSpace(self.ItemLocatorQuery) || item.Name.IndexOf(self.ItemLocatorQuery.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool LocatorOuter(GridClientComponent grid, long id, out GridItemProjection outer)
        {
            for (int depth = 0; depth < grid.Items.Count; ++depth)
            {
                if (!grid.Items.TryGetValue(id, out outer))
                {
                    return false;
                }
                if (outer.Location != GridItemLocation.Container)
                {
                    return true;
                }
                id = outer.HolderId;
            }
            outer = default;
            return false;
        }

        public static GridCell? ItemLocatorHighlight(this GridPresentationComponent self, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Items || grid.WorldId != self.ItemLocatorWorld ||
                grid.WorldGeneration != self.ItemLocatorGeneration || !LocatorOuter(grid, self.ItemLocatorSelectedId, out var outer))
            {
                return null;
            }
            if (outer.Location == GridItemLocation.Hand)
            {
                return grid.Actors.TryGetValue((int)outer.HolderId, out var actor) ? actor.Cell : null;
            }
            if (outer.Location == GridItemLocation.Ground)
            {
                return outer.Cell;
            }
            return grid.Stations.TryGetValue(outer.HolderId, out var station) ? station.Cell : null;
        }

        private static string LocatorPosition(GridClientComponent grid, GridItemProjection item)
        {
            if (item.Location == GridItemLocation.Container)
            {
                return grid.Items.TryGetValue(item.HolderId, out var holder) ? holder.Name + "内" : "容器已变化";
            }
            if (item.Location == GridItemLocation.Hand)
            {
                return "玩家 " + item.HolderId + " 手持";
            }
            string location = item.Location switch
            {
                GridItemLocation.Ground => "地面",
                GridItemLocation.DirtyPool => "脏餐具池",
                GridItemLocation.CleanPool => "净餐具池",
                GridItemLocation.Table => "餐桌",
                _ => "台面"
            };
            return location + (grid.Stations.TryGetValue(item.HolderId, out var station) ? " · " + station.Name : "");
        }

        private static void RefreshItemLocator(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Items)
            {
                return;
            }
            string summary = $"当前结果 {grid.Items.Values.Count(self.LocatorMatches)} 个物品实例\n外壳和内容分别计数；在途货不属于实物。";
            string details;
            if (self.ItemLocatorSelectedId == 0)
            {
                details = summary + "\n按名称和类别筛选，再选择结果。\n这里只查看，不拾取、不移动角色。";
            }
            else if (!grid.Items.TryGetValue(self.ItemLocatorSelectedId, out var item) || !LocatorOuter(grid, item.Id, out var outer))
            {
                details = "选中物品已消失或宿主已变化，请重新选择。\n不沿用旧位置，也不会自动选择其他物品。";
            }
            else
            {
                bool locked = grid.AutoProcesses.Values.Any(process => process.PotItemId == outer.Id &&
                    process.State == GridAutoState.Cooking && process.LockContainer);
                bool working = outer.BakeProcessId != 0 || grid.Processes.Values.Any(process => process.State != GridWorkState.Completed &&
                    (process.InputItemId == outer.Id || process.TargetItemId == outer.Id));
                string state = locked ? "加工锁定，不能取用输入" : working ? "实际加工中，操作以原按钮为准" : "不代表已经可拿取，仍由原操作校验";
                GridCell? cell = self.ItemLocatorHighlight(grid);
                details = grid.ItemDescription(item.Id) + "\n直接位置：" + LocatorPosition(grid, item) +
                    "\n外层宿主：" + LocatorPosition(grid, outer) + (cell.HasValue ? $" ({cell.Value.X},{cell.Value.Y})" : " · 当前不可定位") +
                    "\n" + (item.Hygiene == ItemHygiene.Clean ? "干净" : "脏污") + " · " + state +
                    "\n" + summary + "\n高亮当前宿主，不移动角色。";
            }
            string target = "厨房物品查找";
            if (self.ItemLocatorSelectedId != 0)
            {
                if (grid.Items.TryGetValue(self.ItemLocatorSelectedId, out var chosen) && LocatorOuter(grid, chosen.Id, out var host))
                {
                    GridCell? cell = self.ItemLocatorHighlight(grid);
                    target = chosen.Name + " · " + LocatorPosition(grid, host) + (cell.HasValue ? $" ({cell.Value.X},{cell.Value.Y})" : "");
                }
                else
                {
                    target = "物品已消失，请重新选择";
                }
            }
            view.SetInformation(target, details, self.ItemLocatorDetailsVisible, self.OrdersVisible);
        }

        private static void AddItemLocator(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Items)
            {
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            view.ShowEntryAddress(self.ItemLocatorInput, 80);
            view.AddControl("查找物品", () =>
            {
                GridPresentationComponent current = original;
                if (current != null)
                {
                    current.ItemLocatorInput = view.EntryAddress;
                    current.ItemLocatorQuery = current.ItemLocatorInput;
                    current.ItemLocatorSelectedId = 0;
                    current.ItemLocatorDetailsVisible = true;
                    current.RefreshGridHud();
                }
            });
            AddKind("全部类别", null);
            AddKind("只看材料", GridItemKind.Material);
            AddKind("只看容器", GridItemKind.Container);
            view.AddTarget("关闭物品查找", () =>
            {
                GridPresentationComponent current = original;
                current?.CloseItemLocator();
                current?.RefreshGridHud();
            }, group: GridActionGroup.Items);
            int index = 0;
            foreach (var item in grid.Items.Values.Where(self.LocatorMatches).OrderBy(value => value.Id))
            {
                long id = item.Id;
                view.AddTarget($"物品 {++index} · {item.Name} · {LocatorPosition(grid, item)}", () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null && grid.Items.ContainsKey(id))
                    {
                        current.ItemLocatorSelectedId = id;
                        current.ItemLocatorDetailsVisible = false;
                        current.RefreshGridHud();
                    }
                }, group: GridActionGroup.Items);
            }
            void AddKind(string caption, GridItemKind? kind)
            {
                view.AddTarget(caption, () =>
                {
                    GridPresentationComponent current = original;
                    if (current != null)
                    {
                        current.ItemLocatorKind = kind;
                        current.ItemLocatorSelectedId = 0;
                        current.ItemLocatorDetailsVisible = true;
                        current.RefreshGridHud();
                    }
                }, group: GridActionGroup.Items);
            }
        }
    }
}
