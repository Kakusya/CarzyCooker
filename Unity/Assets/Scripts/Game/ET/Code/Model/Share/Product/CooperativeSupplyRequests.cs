#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    public enum SupplyRequestAction { Publish = 1, Cancel = 2 }
    public enum SupplyRequestState { Pending = 1, Ordered = 2 }

    public readonly struct SupplyRequestValue
    {
        public readonly long Id, Revision, BindingGeneration, ShipmentId;
        public readonly int MemberId, SourceId;
        public readonly SupplyRequestState State;
        public SupplyRequestValue(long id, long revision, int member, long binding, int source, SupplyRequestState state, long shipment = 0)
        {
            Id = id; Revision = revision; MemberId = member; BindingGeneration = binding; SourceId = source; State = state; ShipmentId = shipment;
        }
    }

    public readonly struct SupplyRequestCommand : System.IEquatable<SupplyRequestCommand>
    {
        public readonly SupplyRequestAction Action;
        public readonly int SourceId;
        public readonly long ExpectedId, ExpectedRevision;
        public SupplyRequestCommand(SupplyRequestAction action, int source, long expectedId, long revision)
        {
            Action = action; SourceId = source; ExpectedId = expectedId; ExpectedRevision = revision;
        }
        public bool Equals(SupplyRequestCommand other) => Action == other.Action && SourceId == other.SourceId && ExpectedId == other.ExpectedId && ExpectedRevision == other.ExpectedRevision;
    }

    [ComponentOf(typeof(Level))]
    public sealed class CooperativeSupplyRequests : Entity, IAwake
    {
        public long NextRevision;
        public readonly Dictionary<int, SupplyRequestValue> Members = new();
    }
}
#endif
