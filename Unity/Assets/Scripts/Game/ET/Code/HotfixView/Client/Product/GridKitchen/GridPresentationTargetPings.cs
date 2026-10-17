using System;
using Game;
using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        public static GridCell? TargetPingCell(GridClientComponent grid, TargetPingValue ping)
        {
            if (ping.Kind != TargetPingKind.Item)
            {
                return grid.Stations.TryGetValue(ping.TargetId, out var station) ? station.Cell : null;
            }
            if (!LocatorOuter(grid, ping.TargetId, out var outer))
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
            return grid.Stations.TryGetValue(outer.HolderId, out var holder) ? holder.Cell : null;
        }

        private static string TargetPingName(GridClientComponent grid, TargetPingValue ping)
            => ping.Kind == TargetPingKind.Item ? grid.Items.TryGetValue(ping.TargetId, out var item) ? item.Name : "物品已变化"
                : grid.Stations.TryGetValue(ping.TargetId, out var station) ? station.Name : "工位已变化";

        private static void RenderTargetPings(this GridPresentationComponent self, GridClientComponent grid, MonoGridKitchenBoard board)
        {
            board.BeginTargetPings();
            foreach (var ping in grid.TargetPings)
            {
                var cell = TargetPingCell(grid, ping);
                if (cell.HasValue)
                {
                    board.ShowTargetPing(ping.MemberId, cell.Value, TargetPingName(grid, ping));
                }
            }
            board.EndTargetPings();
        }

        private static void ClearTargetPingVisuals(this GridPresentationComponent self)
        {
            GridKitchenBoard board = self.Board;
            board?.View?.ClearTargetPings();
        }

        private static void AddTargetPingTargets(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            EntityRef<GridPresentationComponent> original = self;
            long level = grid.LevelId, generation = grid.LevelGeneration;
            bool canSend = !grid.LevelPaused && grid.LevelPhase is LevelPhase.Preparing or LevelPhase.Operating or LevelPhase.Draining;
            long selectedItem = self.OverviewPage == GridOverviewPage.Items ? self.ItemLocatorSelectedId
                : self.RecipeOrderId != 0 ? self.RecipeItemId : 0;
            if (canSend && selectedItem != 0 && grid.Items.TryGetValue(selectedItem, out var item))
            {
                view.AddTarget("标记物品 · " + item.Name, () =>
                {
                    GridPresentationComponent current = original;
                    current?.SendTargetPing(TargetPingAction.Set, TargetPingKind.Item, selectedItem, level, generation);
                }, group: self.ActionGroup);
            }
            else if (canSend && self.SelectedStationId != 0 && grid.Stations.TryGetValue(self.SelectedStationId, out var station))
            {
                bool table = grid.Tables.ContainsKey(station.Id);
                view.AddTarget((table ? "标记餐桌 · " : "标记工位 · ") + station.Name, () =>
                {
                    GridPresentationComponent current = original;
                    current?.SendTargetPing(TargetPingAction.Set, table ? TargetPingKind.Table : TargetPingKind.Station, station.Id, level, generation);
                }, group: self.ActionGroup);
            }
            if (canSend && grid.TargetPings.Any(value => value.MemberId == grid.MemberId))
            {
                view.AddTarget("取消我的目标标记", () =>
                {
                    GridPresentationComponent current = original;
                    current?.SendTargetPing(TargetPingAction.Clear, TargetPingKind.Station, 0, level, generation);
                }, group: self.ActionGroup);
            }
            foreach (var ping in grid.TargetPings)
            {
                view.AddTarget("查看标记 · 成员 " + ping.MemberId + " · " + TargetPingName(grid, ping), () =>
                {
                    GridPresentationComponent current = original;
                    if (current == null || !grid.TargetPings.Any(value => value.MemberId == ping.MemberId && value.Serial == ping.Serial))
                    {
                        return;
                    }
                    if (ping.Kind == TargetPingKind.Item)
                    {
                        current.OpenItemLocator(grid, ping.TargetId);
                    }
                    else if (grid.Stations.ContainsKey(ping.TargetId))
                    {
                        current.OverviewPage = GridOverviewPage.None;
                        current.SelectedStationId = ping.TargetId;
                        current.SelectedLevelId = grid.LevelId; current.SelectedLevelGeneration = grid.LevelGeneration;
                        current.RecipeVisible = false; current.DetailsVisible = true;
                        current.RefreshGridHud();
                    }
                }, group: self.ActionGroup);
            }
        }

        public static void SendTargetPing(this GridPresentationComponent self, TargetPingAction action, TargetPingKind kind, long targetId, long level, long levelGeneration)
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
                if (grid?.Confirmed != true || grid.LevelPaused || grid.LevelId != level || grid.LevelGeneration != levelGeneration ||
                    grid.LevelPhase is not (LevelPhase.Preparing or LevelPhase.Operating or LevelPhase.Draining))
                {
                    return;
                }
                EntityRef<GridClientComponent> projection = grid;
                var request = grid.CreateTargetPing(action, kind, targetId);
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
