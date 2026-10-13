#if !DOTNET
using System;
using System.Collections.Generic;

namespace ET
{
    public readonly struct RenovationChoiceIntent : IEquatable<RenovationChoiceIntent>
    {
        public readonly long ChoiceVersion;
        public readonly int DefinitionId;
        public RenovationChoiceIntent(long version, int definition) { ChoiceVersion = version; DefinitionId = definition; }
        public bool Equals(RenovationChoiceIntent other) => ChoiceVersion == other.ChoiceVersion && DefinitionId == other.DefinitionId;
    }

    public readonly struct RenovationChoiceEnvelope
    {
        public readonly ProductCommandContext Context;
        public readonly RenovationChoiceIntent Intent;
        private readonly EntityRef<HostPeer> peer;
        public HostPeer Peer => peer;
        public RenovationChoiceEnvelope(ProductCommandContext context, RenovationChoiceIntent intent, HostPeer owner)
        { Context = context; Intent = intent; peer = owner; }
    }

    [ComponentOf(typeof(RestaurantRuntime))]
    public sealed class RestaurantRenovation : Entity, IAwake
    {
        // 当前成功结果的固定候选与选择；只在下一次自然结算刷新。
        public long ResultLevelId, ResultLevelGeneration, ChoiceVersion;
        public int SelectedDefinitionId;
        public string SelectedName;
        public IReadOnlyList<int> Candidates;
        public IReadOnlyList<GridStationPlacement> PendingPlacements;
        // 已获得奖励的唯一应用记录；实体/布局仍由持续 Kitchen 持有。
        public readonly Dictionary<int, IReadOnlyList<long>> AppliedStations = new();
        public readonly Queue<RenovationChoiceEnvelope> Choices = new();
        public readonly Queue<ProductTransitionIdentity> Preparations = new();
        public long RequestedLevelId, RequestedLevelGeneration, PreparationSerial;
        public bool Prepared, PreparationQueued, NotifyPrepared;
    }
}
#endif
