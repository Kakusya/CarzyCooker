using System;
using System.Linq;
using Game;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        private static void AddSupplyTargets(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            self.AddSupplyOverview(view, grid);
            if (grid.LevelPaused || grid.LevelPhase == LevelPhase.Completed) return;
            EntityRef<GridPresentationComponent> selfRef = self;
            long level = grid.LevelId, generation = grid.LevelGeneration, layout = grid.LayoutVersion;
            if (grid.LevelPhase is LevelPhase.Preparing or LevelPhase.Operating)
                foreach (SupplySourceProjection source in grid.SupplySources.Values.OrderBy(value => value.Id))
                {
                    int id = source.Id;
                    string name = Tables.Instance.DTSupplyConfig.Get(id).Name;
                    bool permitted = grid.ProductionPermit.ItemDefinitions.Contains(Tables.Instance.DTSupplyConfig.Get(id).MaterialDefinitionId);
                    view.AddTarget($"{(permitted ? "采购" : "受限采购")} {name} · 余 {source.RemainingPackages} 包", () =>
                    {
                        GridPresentationComponent current = selfRef;
                        current?.SendSupplyCommand(new SupplyIntent(SupplyAction.Purchase, source: id), level, generation);
                    });
                }
            foreach (SupplyShipmentProjection shipment in grid.SupplyShipments.Values.Where(value => value.Stage == ShipmentStage.Ready).OrderBy(value => value.Id))
            {
                var intent = new SupplyIntent(SupplyAction.Receive, station: shipment.ReceivingStationId, shipment: shipment.Id, layout: layout);
                string name = Tables.Instance.DTSupplyConfig.Get(shipment.SourceId).Name;
                string slot = grid.Stations.TryGetValue(shipment.ReceivingStationId, out var receiving) && receiving.ItemId != 0 ? " · 收货台满" : "";
                view.AddTarget("接收 " + name + slot, () => { GridPresentationComponent current = selfRef; current?.SendSupplyCommand(intent, level, generation); });
            }
            if (!grid.Actors.TryGetValue(grid.MemberId, out var actor) || actor.HandItemId != 0) return;
            foreach (GridStationProjection station in grid.Stations.Values)
            {
                if (!grid.Items.TryGetValue(station.ItemId, out var package) || package.Contents.Count == 0 ||
                    !Tables.Instance.DTSupplyConfig.DataList.Any(value => value.PackageDefinitionId == package.DefinitionId)) continue;
                long material = package.Contents[0];
                var intent = new SupplyIntent(SupplyAction.TakePortion, station: station.Id, package: package.Id, content: material, layout: layout);
                view.AddTarget("拆一份 " + grid.Items[material].Name + " · " + station.Name + $"（余 {package.Contents.Count} 份）",
                    () => { GridPresentationComponent current = selfRef; current?.SendSupplyCommand(intent, level, generation); });
            }
        }

        public static void SendSupplyCommand(this GridPresentationComponent self, SupplyIntent intent, long expectedLevel, long expectedGeneration)
        {
            var owner = self.GridCommandOwner(); if (owner == null) return;
            EntityRef<GridPresentationComponent> selfRef = self;
            owner.Run(async context =>
            {
                ClientSessionComponent client = ((Scene)self.IScene).GetComponent<ClientSessionComponent>();
                GridClientComponent grid = client.GetComponent<GridClientComponent>(); EntityRef<GridClientComponent> gridRef = grid;
                if (grid.LevelId != expectedLevel || grid.LevelGeneration != expectedGeneration) return;
                SupplyCommandRequest request = grid.CreateSupplyCommand(intent);
                self.RememberAdviceRequest(grid, request.CommandId, intent.StationId);
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
