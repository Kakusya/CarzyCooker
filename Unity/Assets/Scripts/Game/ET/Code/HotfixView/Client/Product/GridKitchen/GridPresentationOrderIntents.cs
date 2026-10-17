using System;
using Game;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        public static void SendOrderIntent(this GridPresentationComponent self, OrderIntentAction action, long orderId, long customerId, long level, long levelGeneration)
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
                    grid.LevelPhase is not (LevelPhase.Operating or LevelPhase.Draining))
                {
                    return;
                }
                EntityRef<GridClientComponent> projection = grid;
                var request = grid.CreateOrderIntent(action, orderId, customerId);
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
