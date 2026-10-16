#if !DOTNET
using System;

namespace ET
{
    public readonly struct GridStationLayoutIntent : IEquatable<GridStationLayoutIntent>
    {
        public readonly long StationId, ExpectedLayoutVersion;
        public readonly GridCell Cell;
        public readonly GridOrientation Orientation;
        public GridStationLayoutIntent(long station, long version, GridCell cell, GridOrientation orientation)
        { StationId = station; ExpectedLayoutVersion = version; Cell = cell; Orientation = orientation; }
        public bool Equals(GridStationLayoutIntent other) => StationId == other.StationId && ExpectedLayoutVersion == other.ExpectedLayoutVersion &&
            Cell == other.Cell && Orientation == other.Orientation;
    }

    public readonly struct GridStationLayoutEnvelope
    {
        public readonly ProductCommandContext Context;
        public readonly GridStationLayoutIntent Intent;
        public readonly EntityRef<HostPeer> Peer;
        public GridStationLayoutEnvelope(ProductCommandContext context, GridStationLayoutIntent intent, HostPeer peer)
        { Context = context; Intent = intent; Peer = peer; }
    }
}
#endif
