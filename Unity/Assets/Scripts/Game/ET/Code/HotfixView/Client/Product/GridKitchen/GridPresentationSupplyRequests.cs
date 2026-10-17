using System;
using Game;
using System.Linq;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static string CooperativeSupplySummary(GridClientComponent grid, int sourceId)
        {
            var requests = grid.SupplyRequests.Where(value => sourceId == 0 || value.SourceId == sourceId).ToArray();
            if (requests.Length == 0)
            {
                return "协作补货请求：暂无。发布仅沟通，不采购。";
            }
            return "协作补货请求（每条1包）：\n" + string.Join("\n", requests.Select(value =>
                "成员 " + value.MemberId + " · " + Tables.Instance.DTSupplyConfig.Get(value.SourceId).Name + " · " +
                (value.State == SupplyRequestState.Pending ? "待处理（尚未采购）" : "已下单（不代表已收货）")));
        }

        private static void AddCooperativeSupplyTargets(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (self.OverviewPage != GridOverviewPage.Supply)
            {
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            long level = grid.LevelId, generation = grid.LevelGeneration;
            var own = grid.SupplyRequests.FirstOrDefault(value => value.MemberId == grid.MemberId);
            bool canChange = !grid.LevelPaused && grid.LevelPhase is LevelPhase.Preparing or LevelPhase.Operating;
            if (canChange && grid.SupplySources.ContainsKey(self.SupplyOverviewSourceId))
            {
                int source = self.SupplyOverviewSourceId;
                view.AddTarget((own.Id == 0 ? "请求1包 · " : "替换我的请求 · ") + Tables.Instance.DTSupplyConfig.Get(source).Name, () =>
                {
                    GridPresentationComponent current = original;
                    current?.SendSupplyRequest(SupplyRequestAction.Publish, source, own.Id, own.Revision, level, generation);
                });
            }
            if (canChange && own.Id != 0)
            {
                view.AddTarget("撤销我的补货请求", () =>
                {
                    GridPresentationComponent current = original;
                    current?.SendSupplyRequest(SupplyRequestAction.Cancel, 0, own.Id, own.Revision, level, generation);
                });
            }
            foreach (var request in grid.SupplyRequests.OrderBy(value => value.MemberId))
            {
                string name = Tables.Instance.DTSupplyConfig.Get(request.SourceId).Name;
                string state = request.State == SupplyRequestState.Pending ? "待处理" : "已下单";
                view.AddTarget("查看请求 · 成员 " + request.MemberId + " · " + name + " · " + state, () =>
                {
                    GridPresentationComponent current = original;
                    if (current == null || !grid.SupplyRequests.Any(value => value.Id == request.Id && value.Revision == request.Revision))
                    {
                        return;
                    }
                    current.SupplyOverviewSourceId = request.SourceId;
                    current.SupplyOverviewShipmentId = request.ShipmentId;
                    current.RefreshGridHud();
                });
                if (canChange && request.State == SupplyRequestState.Pending)
                {
                    var intent = new SupplyIntent(SupplyAction.Purchase, source: request.SourceId, cooperativeRequestId: request.Id, cooperativeRevision: request.Revision);
                    view.AddTarget("处理请求 · 成员 " + request.MemberId + " · " + name + "（采购1包）", () =>
                    {
                        GridPresentationComponent current = original;
                        current?.SendSupplyCommand(intent, level, generation);
                    });
                }
            }
        }

        public static void SendSupplyRequest(this GridPresentationComponent self, SupplyRequestAction action, int sourceId, long expectedId, long expectedRevision, long level, long levelGeneration)
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
                    grid.LevelPhase is not (LevelPhase.Preparing or LevelPhase.Operating))
                {
                    return;
                }
                EntityRef<GridClientComponent> projection = grid;
                var request = grid.CreateSupplyRequest(action, sourceId, expectedId, expectedRevision);
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
