#if !DOTNET
using System;
using System.Linq;

namespace ET
{
    [EntitySystemOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(Participant))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(GridActor))]
    [FriendOf(typeof(GridItem))]
    [FriendOf(typeof(LevelBuff))]
    public static partial class RestaurantRuntimeSystem
    {
        [EntitySystem]
        private static void Awake(this RestaurantRuntime self, DRLevelConfig first)
        {
            self.Match = self.GetParent<Match>(); self.Stopped = false; self.Controls.Clear();
            self.Transitions.Clear();
            self.FinishedTransitions.Clear();
            self.PhaseOutcomes.Clear();
            self.NextLevelGeneration = 0; self.LastFrameTime = TimeInfo.Instance.FrameTime; self.Accumulator = 0;
            self.AddComponent<RestaurantRenovation>();
            self.Kitchen = self.AddComponent<GridKitchenComponent>();
            self.InstallLevel(first);
        }

        private static void InstallLevel(this RestaurantRuntime self, DRLevelConfig definition)
        {
            self.CurrentLevel = self.AddChild<Level, DRLevelConfig, long>(definition, checked(++self.NextLevelGeneration));
            self.InstallLevelAdditions(self.CurrentLevel);
            self.PublishLevelPhase(self.CurrentLevel);
        }

        [EntitySystem]
        private static void Update(this RestaurantRuntime self)
        {
            long now = TimeInfo.Instance.FrameTime;
            self.Accumulator += Math.Max(0, Math.Min(250, now - self.LastFrameTime)); self.LastFrameTime = now;
            while (!self.Stopped && self.Accumulator >= GridKitchenRules.TickMilliseconds)
            {
                self.Accumulator -= GridKitchenRules.TickMilliseconds;
                self.AdvanceRestaurantStep();
            }
        }

        [EntitySystem]
        private static void Destroy(this RestaurantRuntime self) => self.StopRestaurant();

        public static void StopRestaurant(this RestaurantRuntime self)
        {
            if (self.Stopped) return;
            self.Stopped = true; self.Controls.Clear();
            self.CancelRenovationChoices("EntryClosed");
            self.StopSoloSave();
            Level level = self.CurrentLevel;
            if (level != null)
            {
                ((GridKitchenComponent)self.Kitchen)?.PauseFrontWork(level, true);
                ((GridKitchenComponent)self.Kitchen)?.CancelPendingServes(level, "EntryClosed");
                ((GridKitchenComponent)self.Kitchen)?.CancelBuffChoices(level, "EntryClosed");
                foreach (CompanionRole role in level.Children.Values.OfType<CompanionRole>()) role.GetComponent<AsyncOwnerComponent>()?.AsyncScope?.Stop();
            }
            ((GridKitchenComponent)self.Kitchen)?.StopGrid();
        }

        public static string QueueLevelControl(this RestaurantRuntime self, HostPeer peer, ProductCommandContext context, LevelControlIntent intent)
        {
            GridKitchenComponent kitchen = self.Kitchen; Level level = self.CurrentLevel; Match match = self.Match;
            if (self.Stopped || peer == null || !kitchen.HasBinding(peer.MemberId, peer.Generation)) return "EntryClosed";
            if (!peer.CanControlRestaurant()) return "HostControlOnly";
            string prior = peer.CheckProductCommand(context, ProductCommandKind.Level, intent.Equals(peer.LastLevelCommand), out bool replay);
            string identity = context.Validate(match.MatchId, kitchen.WorldId, kitchen.WorldGeneration, level.Id, level.Generation, peer.Generation);
            if (identity != null)
            {
                // The exact successful Continue may report its fact after it replaced its own Level.
                if (identity == "WrongLevel" && prior == null && replay && intent.Action is LevelControlAction.Continue or LevelControlAction.SaveAndContinue &&
                    (peer.LastCommandStatus == GridResultStatus.Executed || level.PreparationControl?.Context.Equals(context) == true)) return null;
                return identity;
            }
            if (prior != null || replay) return prior;
            if (intent.Action < LevelControlAction.Start || intent.Action > LevelControlAction.SaveAndContinue) return "InvalidCommand";
            if (intent.Action is LevelControlAction.Continue or LevelControlAction.SaveAndContinue && intent.NextLevelConfigId != level.NextLevelConfigId) return "NextLevelChanged";
            peer.RecordProductCommand(context, ProductCommandKind.Level); peer.LastLevelCommand = intent;
            self.Controls.Enqueue(new LevelControlEnvelope(context, intent, peer));
            return null;
        }

        public static void AdvanceRestaurantStep(this RestaurantRuntime self)
        {
            if (self.Stopped) return;
            GridKitchenComponent kitchen = self.Kitchen;
            long startingLevelId = ((Level)self.CurrentLevel).Id;
            kitchen.ApplyGridMembers();
            self.AdvanceBuffPreparation(self.CurrentLevel);
            self.AdvanceRenovationPreparation(self.CurrentLevel);
            self.AdvanceRenovationChoices(self.CurrentLevel);
            self.AdvanceSoloSaveCompletions();
            while (self.Controls.Count > 0)
            {
                LevelControlEnvelope control = self.Controls.Dequeue(); HostPeer peer = control.Peer; Level level = self.CurrentLevel; Match match = self.Match;
                string error = peer == null || !kitchen.HasBinding(peer.MemberId, control.Context.BindingGeneration) ? "EntryClosed"
                    : control.Context.Validate(match.MatchId, kitchen.WorldId, kitchen.WorldGeneration, level.Id, level.Generation, peer.Generation);
                if (error == null && !peer.CanControlRestaurant()) error = "HostControlOnly";
                if (error == null) error = control.Intent.Action == LevelControlAction.SaveAndContinue
                    ? self.BeginSoloSave(level, control) : self.ApplyLevelControl(level, control.Intent);
                var status = error == null ? control.Intent.Action == LevelControlAction.SaveAndContinue && self.SoloSavePending()
                    ? GridResultStatus.Accepted : GridResultStatus.Executed : GridResultStatus.Rejected;
                Level installed = self.CurrentLevel;
                if (status == GridResultStatus.Executed && installed != level && !installed.GetComponent<LevelBuff>().Prepared)
                {
                    installed.PreparationControl = control;
                    status = GridResultStatus.Accepted;
                }
                peer?.RecordProductResult(control.Context, ProductCommandKind.Level, status, error,
                    status == GridResultStatus.Executed && control.Intent.Action is LevelControlAction.Continue or LevelControlAction.SaveAndContinue);
                kitchen.Dirty = true;
            }
            Level current = self.CurrentLevel;
            if (current.PreparationControl is LevelControlEnvelope preparation && current.GetComponent<LevelBuff>().Prepared)
            {
                current.PreparationControl = null;
                HostPeer peer = preparation.Peer;
                if (peer != null && kitchen.HasBinding(peer.MemberId, preparation.Context.BindingGeneration))
                    peer.RecordProductResult(preparation.Context, ProductCommandKind.Level, GridResultStatus.Executed, null, true);
                kitchen.Dirty = true;
            }
            self.AdvanceLevelTransitions(current);
            kitchen.AdvanceBuffChoices(current);
            kitchen.AdvanceStationLayouts();
            kitchen.AdvanceSupplyCommands(current);
            if (!current.Paused && current.Phase != LevelPhase.Completed && current.Id == startingLevelId)
            {
                kitchen.AdvanceGridTick();
                self.AdvanceFront(current);
                if (current.Phase == LevelPhase.Operating)
                {
                    current.ElapsedOperatingTicks = Math.Min(current.OperatingTicks, current.ElapsedOperatingTicks + 1);
                    if (current.ElapsedOperatingTicks >= current.OperatingTicks && !current.TransitionQueued)
                        self.RequestLevelTransition(current, LevelTransitionAction.Drain);
                    kitchen.Dirty = true;
                }
                else if (current.Phase == LevelPhase.Draining && kitchen.Tick > current.DrainingStartedTick &&
                    !current.TransitionQueued && !current.Children.Values.OfType<ILevelFrontActivity>().Any(activity => activity.BlocksLevelCompletion))
                {
                    self.RequestLevelTransition(current, LevelTransitionAction.Complete);
                }
            }
            kitchen.CommitGridState();
        }

        private static string ApplyLevelControl(this RestaurantRuntime self, Level level, LevelControlIntent intent)
        {
            GridKitchenComponent kitchen = self.Kitchen;
            switch (intent.Action)
            {
                case LevelControlAction.Start:
                    if (level.Phase != LevelPhase.Preparing) return "WrongPhase";
                    if (level.Paused) return "Paused";
                    string gate = self.CheckStartGate(level);
                    if (gate != null) return gate;
                    level.Phase = LevelPhase.Operating; ++level.TransitionSerial; self.PublishLevelPhase(level); return null;
                case LevelControlAction.Pause:
                    if (level.Phase == LevelPhase.Completed) return "LevelCompleted";
                    if (level.Paused) return "AlreadyPaused";
                    kitchen.CancelLevelIntents("Paused"); self.PauseFront(level); level.Paused = true; self.PublishLevelPhase(level); return null;
                case LevelControlAction.Resume:
                    if (!level.Paused) return "NotPaused";
                    level.Paused = false; self.PublishLevelPhase(level); return null;
                case LevelControlAction.Continue:
                    if (level.Phase != LevelPhase.Completed) return "WrongPhase";
                    string saveGate = self.CheckSoloSaveGate(level);
                    if (saveGate != null) return saveGate;
                    if (intent.NextLevelConfigId != level.NextLevelConfigId) return "NextLevelChanged";
                    DRLevelConfig next = Tables.Instance.DTLevelConfig.GetOrDefault(level.NextLevelConfigId);
                    if (next == null) return "MissingNextLevel";
                    if (!kitchen.IsWalkable(new GridCell(next.SpawnX, next.SpawnY))) return "InvalidSpawn";
                    string nextError = self.CheckNextLevel(next);
                    if (nextError != null) return nextError;
                    string renovationError = self.CheckPendingRenovation();
                    if (renovationError != null) return renovationError;
                    kitchen.RetireLevelActors();
                    level.Dispose(); self.InstallLevel(next);
                    self.ApplyPendingRenovation();
                    Match match = self.Match;
                    foreach (Participant member in match.Children.Values.OfType<Participant>().Where(member => member.Online))
                        kitchen.QueueGridMember(member.MemberId, member.BindingGeneration, true);
                    kitchen.ApplyGridMembers();
                    self.AdvanceSoloSaveSource();
                    return null;
                default: return "InvalidCommand";
            }
        }

        // 在对应 change 中接入真实前厅／buff，不创建空服务或重复业务状态。
        static partial void AdvanceFront(this RestaurantRuntime self, Level level);
        static partial void PauseFront(this RestaurantRuntime self, Level level);
        static partial void InstallLevelAdditions(this RestaurantRuntime self, Level level);
        private static string CheckStartGate(this RestaurantRuntime self, Level level) { string error = null; self.CheckStartAdditions(level, ref error); return error; }
        private static string CheckNextLevel(this RestaurantRuntime self, DRLevelConfig next) { string error = null; self.CheckNextAdditions(next, ref error); return error; }
        static partial void CheckStartAdditions(this RestaurantRuntime self, Level level, ref string error);
        static partial void CheckNextAdditions(this RestaurantRuntime self, DRLevelConfig next, ref string error);
    }

    [EntitySystemOf(typeof(Level))]
    [FriendOf(typeof(Level))]
    public static partial class LevelSystem
    {
        [EntitySystem]
        private static void Awake(this Level self, DRLevelConfig definition, long generation)
        {
            self.ConfigId = definition.Id; self.NextLevelConfigId = definition.NextLevelConfigId; self.Generation = generation;
            self.Phase = LevelPhase.Preparing; self.Paused = false; self.OperatingTicks = definition.OperatingTicks;
            self.ElapsedOperatingTicks = 0; self.Score = 0; self.Stars = 0; self.DrainingStartedTick = 0;
            self.StarThresholds = definition.StarThresholds.ToArray(); self.SpawnCell = new GridCell(definition.SpawnX, definition.SpawnY);
            self.AllowedDishIds = definition.AllowedDishIds.ToArray();
            self.AllowedRenovationIds = definition.AllowedRenovationIds.ToArray();
            self.CompletionSerial = 1;
            self.TransitionSerial = 1; self.TransitionQueued = false;
            self.PreparationControl = null;
        }
    }
}
#endif
