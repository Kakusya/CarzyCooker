#if !DOTNET
using System.Linq;

namespace ET
{
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(Level))]
    public static partial class RestaurantRuntimeSystem
    {
        private static void RequestLevelTransition(this RestaurantRuntime self, Level level, LevelTransitionAction action)
        {
            level.TransitionQueued = true; GridKitchenComponent kitchen = self.Kitchen;
            self.Root().GetComponent<ProcessInnerSender>().Send(((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId(), ProductMessages.Create<LevelTransition>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = ((Match)self.Match).MatchId;
                value.WorldId = kitchen.WorldId; value.WorldGeneration = kitchen.WorldGeneration; value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                value.SourceId = level.Id; value.SourceInstanceId = level.InstanceId; value.TransitionSerial = level.TransitionSerial; value.Action = (int)action;
            }));
        }

        private static void AdvanceLevelTransitions(this RestaurantRuntime self, Level level)
        {
            if (level.Paused) return;
            GridKitchenComponent kitchen = self.Kitchen;
            while (self.Transitions.Count > 0)
            {
                LevelTransitionValue value = self.Transitions.Dequeue(); ProductTransitionIdentity identity = value.Identity;
                if (identity.LevelId != level.Id || identity.LevelGeneration != level.Generation || identity.SourceId != level.Id ||
                    identity.SourceInstanceId != level.InstanceId || identity.Serial != level.TransitionSerial || !level.TransitionQueued) continue;
                if (value.Action == LevelTransitionAction.Drain && level.Phase == LevelPhase.Operating && level.ElapsedOperatingTicks >= level.OperatingTicks)
                {
                    level.Phase = LevelPhase.Draining; level.DrainingStartedTick = kitchen.Tick;
                }
                else if (value.Action == LevelTransitionAction.Complete && level.Phase == LevelPhase.Draining && kitchen.Tick > level.DrainingStartedTick)
                {
                    if (level.Children.Values.OfType<ILevelFrontActivity>().Any(activity => activity.BlocksLevelCompletion))
                    { level.TransitionQueued = false; ++level.TransitionSerial; continue; }
                    kitchen.CancelLevelIntents("LevelCompleted"); kitchen.PauseLevelWorkers();
                    level.Stars = level.StarThresholds.Count(threshold => level.Score >= threshold);
                    level.Phase = LevelPhase.Completed; ++level.CompletionSerial; self.PrepareRenovationChoices(level);
                }
                else continue;
                level.TransitionQueued = false; ++level.TransitionSerial; self.PublishLevelPhase(level);
            }
        }

        private static void PublishLevelPhase(this RestaurantRuntime self, Level level)
        {
            self.PhaseOutcomes.Add(new LevelPhaseOutcomeValue(level.Id, level.Generation, level.Phase, level.Paused));
            ((GridKitchenComponent)self.Kitchen).Dirty = true;
        }
    }
}
#endif
