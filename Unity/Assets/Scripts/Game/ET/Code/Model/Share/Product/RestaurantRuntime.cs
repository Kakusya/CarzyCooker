#if !DOTNET
using System;
using System.Collections.Generic;

namespace ET
{
    public enum LevelPhase { Preparing = 1, Operating = 2, Draining = 3, Completed = 4 }
    public enum LevelControlAction { Start = 1, Pause = 2, Resume = 3, Continue = 4, SaveAndContinue = 5, FinishChapter = 6, SkipRenovation = 7 }

    public readonly struct LevelControlIntent : IEquatable<LevelControlIntent>
    {
        public readonly LevelControlAction Action;
        public readonly int NextLevelConfigId;
        public LevelControlIntent(LevelControlAction action, int next = 0) { Action = action; NextLevelConfigId = next; }
        public bool Equals(LevelControlIntent other) => Action == other.Action && NextLevelConfigId == other.NextLevelConfigId;
        public override bool Equals(object other) => other is LevelControlIntent value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Action, NextLevelConfigId);
    }

    public readonly struct LevelControlEnvelope
    {
        public readonly ProductCommandContext Context;
        public readonly LevelControlIntent Intent;
        public readonly EntityRef<HostPeer> Peer;
        public LevelControlEnvelope(ProductCommandContext context, LevelControlIntent intent, HostPeer peer)
        { Context = context; Intent = intent; Peer = peer; }
    }

    // 后续 Customer / FrontWork 以自身真实状态参与收尾，不维护额外 pending 计数。
    public interface ILevelFrontActivity { bool BlocksLevelCompletion { get; } }

    [ChildOf(typeof(Match))]
    public sealed class RestaurantRuntime : Entity, IAwake<DRLevelConfig>, IUpdate, IDestroy
    {
        public EntityRef<Match> Match;
        public EntityRef<GridKitchenComponent> Kitchen;
        public EntityRef<Level> CurrentLevel;
        public long NextLevelGeneration;
        public long LastFrameTime;
        public long Accumulator;
        public bool Stopped;
        public ProductMenuVariant MenuVariant;
        public readonly Queue<LevelControlEnvelope> Controls = new();
        public readonly Queue<LevelTransitionValue> Transitions = new();
        public readonly Queue<ProductTransitionIdentity> FinishedTransitions = new();
        public readonly List<LevelPhaseOutcomeValue> PhaseOutcomes = new();
    }

    [ChildOf(typeof(RestaurantRuntime))]
    public sealed class Level : Entity, IAwake<DRLevelConfig, long>
    {
        public int ConfigId;
        public int NextLevelConfigId;
        public long Generation;
        public LevelPhase Phase;
        public bool Paused;
        public int OperatingTicks;
        public int ElapsedOperatingTicks;
        public int Score;
        public int Stars;
        public int ServedOrders, UnfulfilledOrders;
        public bool IsChapterTerminal;
        public int[] StarThresholds;
        public int[] AllowedDishIds;
        public readonly ProductionPermit ProductionPermit = new();
        public int[] AllowedRenovationIds;
        public ProductMenuVariant MenuVariant;
        public GridCell SpawnCell;
        public readonly Dictionary<long, long> DrinkBindings = new();
        public long DrainingStartedTick;
        public long CompletionSerial;
        public long TransitionSerial;
        public bool TransitionQueued;
        public LevelControlEnvelope? PreparationControl;
        public readonly Dictionary<string, string> EligibleProfiles = new();
        public EntityRef<CooperativeSaveResult> ClaimResult;
    }
}
#endif
