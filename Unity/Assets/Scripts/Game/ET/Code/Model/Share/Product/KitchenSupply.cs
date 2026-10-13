#if !DOTNET
using System;
using System.Collections.Generic;

namespace ET
{
    public enum SupplyAction { Purchase = 1, Receive = 2, TakePortion = 3 }
    public enum ShipmentStage { InTransit = 1, Ready = 2, Received = 3 }

    public readonly struct SupplyIntent : IEquatable<SupplyIntent>
    {
        public readonly SupplyAction Action;
        public readonly int SourceId, Quantity;
        public readonly long StationId, ShipmentId, PackageId, ContentItemId, LayoutVersion;

        public SupplyIntent(SupplyAction action, int source = 0, int quantity = 1, long station = 0,
            long shipment = 0, long package = 0, long content = 0, long layout = 0)
        {
            Action = action; SourceId = source; Quantity = quantity; StationId = station;
            ShipmentId = shipment; PackageId = package; ContentItemId = content; LayoutVersion = layout;
        }

        public bool Equals(SupplyIntent other) => Action == other.Action && SourceId == other.SourceId && Quantity == other.Quantity &&
            StationId == other.StationId && ShipmentId == other.ShipmentId && PackageId == other.PackageId &&
            ContentItemId == other.ContentItemId && LayoutVersion == other.LayoutVersion;
    }

    public readonly struct SupplyEnvelope
    {
        public readonly ProductCommandContext Context;
        public readonly SupplyIntent Intent;
        private readonly EntityRef<HostPeer> peer;
        public HostPeer Peer => peer;
        public SupplyEnvelope(ProductCommandContext context, SupplyIntent intent, HostPeer owner)
        { Context = context; Intent = intent; peer = owner; }
    }

    [ComponentOf(typeof(GridKitchenComponent))]
    public sealed class KitchenSupply : Entity, IAwake
    {
        // 外部剩余包数只在预留时减少；实物材料只由 GridItem.Contents 表达。
        public readonly Dictionary<int, int> RemainingPackages = new();
        public readonly Queue<SupplyEnvelope> Commands = new();
        public readonly Queue<ShipmentTransitionValue> Transitions = new();
        public readonly List<ShipmentOutcomeValue> Outcomes = new();
    }

    [ChildOf(typeof(KitchenSupply))]
    public sealed class SupplyShipment : Entity, IAwake
    {
        // 采购时冻结定义与业务 Tick 耗时；跨关保持同一个货物实例。
        public int SourceId, MaterialDefinitionId, PackageDefinitionId, PackageUnits, RequiredTicks, ProgressTicks;
        public long ReceivingStationId, PackageItemId, StartedTick;
        public ShipmentStage Stage;
        public long TransitionSerial;
        public bool ReadyQueued;
    }

    public readonly struct SupplySourceProjection
    {
        public readonly int Id, RemainingPackages;
        public readonly long StationId;
        public SupplySourceProjection(int id, int remaining, long station)
        { Id = id; RemainingPackages = remaining; StationId = station; }
    }

    public readonly struct SupplyShipmentProjection
    {
        public readonly long Id, ReceivingStationId, PackageItemId;
        public readonly int SourceId, RequiredTicks, ProgressTicks;
        public readonly ShipmentStage Stage;
        public SupplyShipmentProjection(GridSupplyShipmentState value)
        {
            Id = value.ShipmentId; SourceId = value.SourceId; ReceivingStationId = value.ReceivingStationId;
            PackageItemId = value.PackageItemId; RequiredTicks = value.RequiredTicks; ProgressTicks = value.ProgressTicks;
            Stage = (ShipmentStage)value.Stage;
        }
    }
}
#endif
