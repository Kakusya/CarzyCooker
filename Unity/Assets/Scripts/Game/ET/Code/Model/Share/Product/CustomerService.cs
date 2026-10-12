#if !DOTNET
using System.Collections.Generic;
using System;

namespace ET
{
    public enum CustomerPhase { Entering, Queued, WalkingToSeat, WaitingAsk, Ordered, Eating, Leaving, Gone }
    public enum OrderState { Waiting, Served, Expired }
    public enum FrontTableState { Free, Reserved, Occupied, Dirty }
    public enum ItemHygiene { Clean, Dirty }

    public readonly struct ServeOrderIntent : IEquatable<ServeOrderIntent>
    {
        public readonly long OrderId, CustomerId, TableStationId, ItemId;
        public ServeOrderIntent(long order, long customer, long table, long item)
        { OrderId=order;CustomerId=customer;TableStationId=table;ItemId=item; }
        public bool Equals(ServeOrderIntent other) => OrderId==other.OrderId && CustomerId==other.CustomerId && TableStationId==other.TableStationId && ItemId==other.ItemId;
    }
    public readonly struct ServeOrderEnvelope
    {
        public readonly ProductCommandContext Context;
        public readonly ServeOrderIntent Intent;
        public readonly EntityRef<HostPeer> Peer;
        public ServeOrderEnvelope(ProductCommandContext context, ServeOrderIntent intent, HostPeer peer) { Context=context;Intent=intent;Peer=peer; }
    }

    [ComponentOf(typeof(Level))]
    public sealed class FrontLevelState : Entity, IAwake<DRCustomerFlowConfig>
    {
        public int FlowDefinitionId;
        public int NextArrivalOperatingTick;
        public int NextCustomerSequence;
        public int SkippedArrivals;
        public readonly Queue<long> Waiting = new();
        public readonly Queue<ServeOrderEnvelope> Serves = new();
    }

    [ComponentOf(typeof(GridStation))]
    public sealed class FrontTable : Entity, IAwake<DRTableConfig>
    {
        public int DefinitionId;
        public GridCell SeatOffset;
        public long CustomerId;
        public FrontTableState State;
    }

    [ChildOf(typeof(Level))]
    public sealed class Customer : Entity, IAwake<DRCustomerFlowConfig, long, int>, ILevelFrontActivity
    {
        public int FlowDefinitionId;
        public int OrderDefinitionId;
        public int ArrivalSequence;
        public long LevelGeneration;
        public CustomerPhase Phase;
        public long TableStationId;
        public long AskWorkId;
        public long OrderId;
        public GridCell Cell;
        public GridCell NextCell;
        public GridMotion Motion;
        public long StepStartTick;
        public readonly List<GridCell> Path = new();
        public int PathIndex;
        public int RemainingEatTicks;
        public long PhaseEnteredTick;
        public bool BlocksLevelCompletion => Phase != CustomerPhase.Gone;
    }

    [ChildOf(typeof(Level))]
    public sealed class Order : Entity, IAwake<DROrderConfig, Customer, long>
    {
        public int DefinitionId;
        public int DishId;
        public int Score;
        public long CustomerId;
        public long TableStationId;
        public long LevelGeneration;
        public long DeadlineTick;
        public long ServedTick;
        public long ServedItemId;
        public OrderState State;
    }
}
#endif
