#if !DOTNET
using System.Linq;

namespace ET
{
    [EntitySystemOf(typeof(KitchenSupply))]
    [FriendOf(typeof(KitchenSupply))]
    public static partial class KitchenSupplySystem
    {
        [EntitySystem]
        private static void Awake(this KitchenSupply self)
        {
            self.RemainingPackages.Clear(); self.Commands.Clear();
            foreach (DRSupplyConfig source in Tables.Instance.DTSupplyConfig.DataList)
                self.RemainingPackages.Add(source.Id, source.AvailablePackages);
        }
    }

    [EntitySystemOf(typeof(SupplyShipment))]
    public static partial class SupplyShipmentSystem
    {
        [EntitySystem]
        private static void Awake(this SupplyShipment self) { }
    }

    [FriendOf(typeof(KitchenSupply))]
    [FriendOf(typeof(SupplyShipment))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(Match))]
    public static partial class GridKitchenSystem
    {
        private static bool IsSupplyInteraction(GridAction action) => action is GridAction.ReceiveSupply or GridAction.TakeSupplyPortion;

        public static GridStation SupplyReceivingStation(this GridKitchenComponent kitchen, int sourceId)
        {
            DRSupplyConfig source = Tables.Instance.DTSupplyConfig.GetOrDefault(sourceId);
            return source == null ? null : kitchen.Stations.Values.Select(value => (GridStation)value)
                .SingleOrDefault(station => station.InitialLayoutId == source.ReceivingLayoutId);
        }

        public static string QueueSupplyCommand(this GridKitchenComponent kitchen, HostPeer peer, ProductCommandContext context, SupplyIntent intent)
        {
            Level level = kitchen.ProductLevel(); Match match = kitchen.Match;
            if (level == null || kitchen.Stopped || peer == null || !kitchen.HasBinding(peer.MemberId, peer.Generation)) return "EntryClosed";
            string error = context.Validate(match.MatchId, kitchen.WorldId, kitchen.WorldGeneration, level.Id, level.Generation, peer.Generation);
            if (error != null) return error;
            error = peer.CheckProductCommand(context, ProductCommandKind.Supply, intent.Equals(peer.LastSupplyCommand), out bool replay);
            if (error != null || replay) return error;
            if (intent.Action < SupplyAction.Purchase || intent.Action > SupplyAction.TakePortion) return "InvalidCommand";
            if (level.Paused) return "Paused";
            if (level.Phase == LevelPhase.Completed) return "LevelCompleted";
            if (intent.Action == SupplyAction.Purchase && level.Phase == LevelPhase.Draining) return "WrongPhase";
            peer.RecordProductCommand(context, ProductCommandKind.Supply); peer.LastSupplyCommand = intent;
            kitchen.GetComponent<KitchenSupply>().Commands.Enqueue(new SupplyEnvelope(context, intent, peer));
            return null;
        }

        public static void AdvanceSupplyCommands(this GridKitchenComponent kitchen, Level level)
        {
            KitchenSupply supply = kitchen.GetComponent<KitchenSupply>();
            while (supply.Commands.Count > 0)
            {
                SupplyEnvelope command = supply.Commands.Dequeue(); HostPeer peer = command.Peer;
                string error = peer == null || !kitchen.HasBinding(peer.MemberId, command.Context.BindingGeneration) ? "EntryClosed"
                    : command.Context.Validate(((Match)kitchen.Match).MatchId, kitchen.WorldId, kitchen.WorldGeneration, level.Id, level.Generation, peer.Generation);
                if (error == null) error = level.Paused ? "Paused" : level.Phase == LevelPhase.Completed ? "LevelCompleted" : null;
                SupplyIntent intent = command.Intent;
                if (error == null && intent.Action == SupplyAction.Purchase)
                {
                    DRSupplyConfig source = Tables.Instance.DTSupplyConfig.GetOrDefault(intent.SourceId);
                    GridStation receiving = kitchen.SupplyReceivingStation(intent.SourceId);
                    error = level.Phase == LevelPhase.Draining ? "WrongPhase" : source == null || receiving == null ? "InvalidSupply"
                        : intent.Quantity <= 0 ? "InvalidQuantity" : supply.RemainingPackages[source.Id] < intent.Quantity ? "SupplyExhausted" : null;
                    if (error == null)
                    {
                        supply.RemainingPackages[source.Id] -= intent.Quantity;
                        for (int i = 0; i < intent.Quantity; ++i)
                        {
                            SupplyShipment shipment = supply.AddChild<SupplyShipment>();
                            shipment.SourceId = source.Id; shipment.MaterialDefinitionId = source.MaterialDefinitionId;
                            shipment.PackageDefinitionId = source.PackageDefinitionId; shipment.PackageUnits = source.PackageUnits;
                            shipment.ReceivingStationId = receiving.Id; shipment.RequiredTicks = source.TransitTicks;
                            shipment.ProgressTicks = 0; shipment.PackageItemId = 0;
                            shipment.StartedTick = kitchen.Tick + 1; shipment.Stage = ShipmentStage.InTransit;
                        }
                    }
                }
                else if (error == null)
                {
                    GridActor actor = kitchen.Actor(peer.MemberId);
                    GridStation station = kitchen.Station(intent.StationId);
                    error = actor == null ? "EntryClosed" : station == null ? "UnknownCounter" : null;
                    if (error == null)
                    {
                        var gridIntent = new GridIntent(command.Context.CommandId,
                            intent.Action == SupplyAction.Receive ? GridAction.ReceiveSupply : GridAction.TakeSupplyPortion,
                            station.Cell, station.Id, intent.ContentItemId, intent.PackageId, intent.ShipmentId,
                            expectedLayoutVersion: intent.LayoutVersion);
                        if (kitchen.Incoming.TryGetValue(peer.MemberId, out var replaced))
                            kitchen.Result(actor, replaced.CommandId, GridResultStatus.Cancelled, "Replaced");
                        actor.LastReceivedCommandId = gridIntent.CommandId; actor.LastReceivedIntent = gridIntent;
                        kitchen.Incoming[peer.MemberId] = gridIntent;
                        kitchen.Dirty = true;
                        continue;
                    }
                }
                peer?.RecordProductResult(command.Context.CommandId, error == null ? GridResultStatus.Executed : GridResultStatus.Rejected, error);
                kitchen.Dirty = true;
            }
        }

        public static void CancelSupplyCommands(this GridKitchenComponent kitchen, string reason)
        {
            KitchenSupply supply = kitchen.GetComponent<KitchenSupply>();
            if (supply == null) return;
            while (supply.Commands.Count > 0)
            {
                SupplyEnvelope command = supply.Commands.Dequeue();
                command.Peer?.RecordProductResult(command.Context.CommandId, GridResultStatus.Cancelled, reason);
            }
        }

        private static void AdvanceSupplyShipments(this GridKitchenComponent kitchen)
        {
            KitchenSupply supply = kitchen.GetComponent<KitchenSupply>();
            foreach (SupplyShipment shipment in supply.Children.Values.OfType<SupplyShipment>())
            {
                if (shipment.Stage != ShipmentStage.InTransit || shipment.StartedTick == kitchen.Tick) continue;
                ++shipment.ProgressTicks;
                if (shipment.ProgressTicks >= shipment.RequiredTicks) shipment.Stage = ShipmentStage.Ready;
                kitchen.Dirty = true;
            }
        }

        private static string SupplyAtStation(this GridKitchenComponent kitchen, GridActor actor, GridStation station, GridIntent intent)
        {
            if (intent.ExpectedLayoutVersion != kitchen.LayoutVersion) return "LayoutChanged";
            if (intent.Action == GridAction.ReceiveSupply)
            {
                SupplyShipment shipment = kitchen.GetComponent<KitchenSupply>().GetChild<SupplyShipment>(intent.ExpectedProcessId);
                if (shipment == null || shipment.ReceivingStationId != station.Id) return "ShipmentChanged";
                if (shipment.Stage == ShipmentStage.Received) return "AlreadyReceived";
                if (shipment.Stage != ShipmentStage.Ready) return "ShipmentNotReady";
                if (station.Kind != GridStationKind.Counter || station.ItemSlotCapacity != 1) return "InvalidReceivingStation";
                if (station.ItemId != 0) return "ReceivingFull";
                GridItem package = kitchen.CreateGridItem(shipment.PackageDefinitionId, GridItemLocation.Counter, station.Id, station.Cell);
                for (int i = 0; i < shipment.PackageUnits; ++i)
                {
                    GridItem material = kitchen.CreateGridItem(shipment.MaterialDefinitionId, GridItemLocation.Container, package.Id, station.Cell);
                    package.Contents.Add(material.Id);
                }
                station.ItemId = package.Id; shipment.PackageItemId = package.Id; shipment.Stage = ShipmentStage.Received;
                return null;
            }
            if (actor.HandItemId != 0) return "HandFull";
            GridItem source = kitchen.Item(station.ItemId), content = kitchen.Item(intent.ExpectedItemId);
            if (source == null || source.Id != intent.ExpectedTargetItemId || source.Location != GridItemLocation.Counter || source.HolderId != station.Id ||
                content == null || !source.Contents.Contains(content.Id) || content.Location != GridItemLocation.Container || content.HolderId != source.Id) return "ContentsChanged";
            DRSupplyConfig definition = Tables.Instance.DTSupplyConfig.DataList.SingleOrDefault(row => row.PackageDefinitionId == source.DefinitionId);
            if (definition == null || content.DefinitionId != definition.MaterialDefinitionId) return "RequiresSupplyPackage";
            if (kitchen.ItemWorking(source.Id) || kitchen.ItemWorking(content.Id)) return "WorkBusy";
            kitchen.PauseWork(actor);
            source.Contents.Remove(content.Id); content.Location = GridItemLocation.Hand; content.HolderId = actor.MemberId;
            content.Cell = actor.Cell; actor.HandItemId = content.Id;
            return null;
        }
    }
}
#endif
