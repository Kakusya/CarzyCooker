#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    [FriendOf(typeof(FrontWorkCommands))]
    [FriendOf(typeof(FrontWork))]
    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(CompanionGrowth))]
    [FriendOf(typeof(Companion))]
    [FriendOf(typeof(ProcessInnerSender))]
    public static partial class GridKitchenSystem
    {
        public static string QueuePlayerFrontWork(this GridKitchenComponent self, HostPeer peer, ProductCommandContext context, FrontWorkIntent intent)
        {
            Level level = self.ProductLevel(); Match match = self.Match;
            if (level == null || self.Stopped || peer == null || !self.HasBinding(peer.MemberId, peer.Generation)) return "EntryClosed";
            string error = context.Validate(match.MatchId, self.WorldId, self.WorldGeneration, level.Id, level.Generation, peer.Generation);
            if (error != null) return error;
            string prior = peer.CheckProductCommand(context, ProductCommandKind.FrontWork, intent.Equals(peer.LastFrontWorkCommand), out bool replay);
            if (prior != null || replay) return prior;
            error = FrontCommandGate(level, intent);
            if (error != null) return error;
            peer.RecordProductCommand(context, ProductCommandKind.FrontWork); peer.LastFrontWorkCommand = intent;
            level.GetComponent<FrontWorkCommands>().Pending.Enqueue(new FrontWorkEnvelope(context, intent, peer));
            return null;
        }

        public static string QueueCompanionFrontWork(this GridKitchenComponent self, CompanionRole role, ProductCommandContext context, FrontWorkIntent intent)
        {
            Level level = self.ProductLevel(); Match match = self.Match;
            if (level == null || self.Stopped || role == null || role.Parent != level || role.LevelGeneration != level.Generation) return "RoleChanged";
            string error = context.Validate(match.MatchId, self.WorldId, self.WorldGeneration, level.Id, level.Generation, role.LevelGeneration);
            if (error != null) return error;
            if (context.CommandId < role.LastSubmittedRequestId) return "ExpiredCommand";
            if (context.CommandId == role.LastSubmittedRequestId)
                return intent.Equals(role.LastSubmittedIntent) ? null : "CommandConflict";
            error = FrontCommandGate(level, intent); if (error != null) return error;
            role.LastSubmittedRequestId = context.CommandId; role.LastSubmittedIntent = intent;
            role.LastResultStatus = GridResultStatus.Accepted; role.LastWorkReason = null;
            level.GetComponent<FrontWorkCommands>().Pending.Enqueue(new FrontWorkEnvelope(intent, role, context.CommandId));
            return null;
        }

        private static string FrontCommandGate(Level level, FrontWorkIntent intent) => level.Paused ? "Paused"
            : level.Phase != LevelPhase.Operating && level.Phase != LevelPhase.Draining ? "WrongPhase"
            : intent.Action < FrontWorkAction.Claim || intent.Action > FrontWorkAction.Stop || intent.WorkId <= 0 || intent.ClaimSerial < 0 ||
                intent.Action == FrontWorkAction.Stop && intent.ClaimSerial == 0 ? "InvalidCommand" : null;

        public static void AdvanceFrontWork(this GridKitchenComponent self, Level level)
        {
            if (level.Phase != LevelPhase.Operating && level.Phase != LevelPhase.Draining) return;
            FrontWorkCommands commands = level.GetComponent<FrontWorkCommands>();
            commands.StepCommands.Clear();
            int pendingCount=commands.Pending.Count;
            for(int i=0;i<pendingCount;i++)commands.StepCommands.Add(commands.Pending.Dequeue());
            foreach (FrontWorkEnvelope command in commands.StepCommands)
            {
                if(command.Intent.Action==FrontWorkAction.Stop)
                {
                    self.ApplyQueuedFront(level,command);
                }
            }
            self.AdvanceFrontCreations(level, commands);
            self.AdvanceFrontTransitions(level, commands);
            commands.MovementRoles.Clear();commands.WorkStepBuffer.Clear();
            foreach(Entity child in level.Children.Values)
            {
                if(child is CompanionRole role)
                {
                    commands.MovementRoles.Add(role);
                }
            }
            commands.MovementRoles.Sort(static (left,right)=>((CompanionRole)left).Id.CompareTo(((CompanionRole)right).Id));
            foreach(var reference in commands.MovementRoles)
            {
                CompanionRole role=reference;
                if(role!=null)
                {
                    self.AdvanceCompanionMovement(level,role);
                }
            }
            foreach(Entity child in level.Children.Values)
            {
                if(child is FrontWork work)
                {
                    commands.WorkStepBuffer.Add(work);
                }
            }
            commands.WorkStepBuffer.Sort(static (left,right)=>((FrontWork)left).Id.CompareTo(((FrontWork)right).Id));
            foreach (var reference in commands.WorkStepBuffer)
            {
                FrontWork work=reference;
                if(work==null)
                {
                    continue;
                }
                if (work.State != FrontWorkState.Approaching && work.State != FrontWorkState.Working) continue;
                if (self.CheckFrontTarget(level, work) != null) { self.ReleaseFrontClaim(level, work, FrontWorkState.Cancelled, true); continue; }
                GridCell cell; GridMotion motion; bool valid;
                if (work.WorkerKind == FrontWorkerKind.Player)
                {
                    GridActor actor = self.Actor((int)work.WorkerId);
                    valid = actor != null && actor.InstanceId == work.WorkerInstanceId && actor.Generation == work.WorkerGeneration &&
                        self.HasBinding(actor.MemberId, actor.Generation) && actor.FrontWorkId == work.Id && actor.FrontClaimSerial == work.ClaimSerial && actor.HandItemId == 0;
                    cell = actor?.Cell ?? default; motion = actor?.Motion ?? GridMotion.Standing;
                }
                else
                {
                    CompanionRole role = level.GetChild<CompanionRole>(work.WorkerId);
                    valid = role != null && role.InstanceId == work.WorkerInstanceId && role.LevelGeneration == work.WorkerGeneration &&
                        role.WorkId == work.Id && role.ClaimSerial == work.ClaimSerial;
                    cell = role?.Cell ?? default; motion = role?.Motion ?? GridMotion.Standing;
                }
                if (!valid) { self.ReleaseFrontClaim(level, work, FrontWorkState.Available, true); continue; }
                if (motion != GridMotion.Standing || cell != work.OperationCell)
                {
                    if (work.State == FrontWorkState.Working) self.ReleaseFrontClaim(level, work, FrontWorkState.Available, true);
                    continue;
                }
                if (work.State == FrontWorkState.Approaching)
                {
                    if (!work.TransitionQueued) self.RequestFrontTransition(level, work, FrontWorkTransitionAction.Start);
                    if (!work.StartReceived) continue;
                    work.State = FrontWorkState.Working; work.StartedTick = self.Tick;
                    work.TransitionQueued = work.StartReceived = false; ++work.TransitionSerial;
                    self.PublishFrontStage(level, work, FrontWorkStage.Started); continue;
                }
                if (work.StartedTick == self.Tick) continue;
                work.Progress = Math.Min(1, work.Progress + 1d / work.FrozenTicks); self.Dirty = true;
                if (work.Progress < 1 - 1e-9) continue;
                if (!work.TransitionQueued) self.RequestFrontTransition(level, work, FrontWorkTransitionAction.Complete);
                if (!work.CompletionReceived) continue;
                string error = self.CompleteFrontTarget(level, work);
                if (error != null) continue;
                CompanionRole finisher = work.WorkerKind == FrontWorkerKind.Companion ? level.GetChild<CompanionRole>(work.WorkerId) : null;
                long request = work.WorkerCommandId, serial = work.ClaimSerial;
                if (finisher != null && work.Kind is FrontWorkKind.Ask or FrontWorkKind.Wash)
                {
                    Companion identity = finisher.Companion;
                    CompanionGrowth growth = level.GetComponent<CompanionGrowth>();
                    growth.CompletedByCompanion.TryGetValue(identity.Id, out int count); growth.CompletedByCompanion[identity.Id] = count + 1;
                }
                work.SuccessorPending = work.Kind == FrontWorkKind.Clear && work.ItemId != 0;
                self.ReleaseFrontClaim(level, work, FrontWorkState.Completed, true);
                self.NotifyFrontFinished(level, work);
                if (finisher != null) NotifyFrontResult(finisher, request, work.Id, serial, GridResultStatus.Executed, null, CompanionNotificationKind.Finished);
            }
            foreach (FrontWorkEnvelope command in commands.StepCommands)
            {
                if(command.Intent.Action==FrontWorkAction.Claim)
                {
                    self.ApplyQueuedFront(level,command);
                }
            }
            commands.StepCommands.Clear();commands.MovementRoles.Clear();commands.WorkStepBuffer.Clear();
        }

        private static void ApplyQueuedFront(this GridKitchenComponent self, Level level, FrontWorkEnvelope command)
        {
            HostPeer peer = command.Peer; CompanionRole role = command.Role;
            string error = command.Kind == FrontWorkerKind.Player
                ? peer == null || !peer.EntryOpen || !self.HasBinding(peer.MemberId, command.Context.BindingGeneration) ? "EntryClosed"
                    : command.Context.Validate(((Match)self.Match).MatchId, self.WorldId, self.WorldGeneration, level.Id, level.Generation, peer.Generation)
                : role == null || role.Parent != level || role.LevelGeneration != level.Generation ? "RoleChanged" : null;
            if (error == null) error = self.ApplyFrontCommand(level, command);
            GridResultStatus status = error == null ? GridResultStatus.Executed : GridResultStatus.Rejected;
            if (peer != null) peer.RecordProductResult(command.Context, ProductCommandKind.FrontWork, status, error);
            if (role != null)
            {
                role.LastCommittedRequestId = command.RequestId; role.LastResultStatus = status; role.LastWorkReason = error;
                FrontWork work = level.GetChild<FrontWork>(command.Intent.WorkId);
                NotifyFrontResult(role, command.RequestId, command.Intent.WorkId, work?.ClaimSerial ?? 0, status, error, CompanionNotificationKind.RequestResult);
            }
            self.Dirty = true;
        }

        private static string ApplyFrontCommand(this GridKitchenComponent self, Level level, FrontWorkEnvelope command)
        {
            string error = FrontCommandGate(level, command.Intent); if (error != null) return error;
            FrontWork work = level.GetChild<FrontWork>(command.Intent.WorkId);
            if (work == null) return "UnknownWork";
            HostPeer peer = command.Peer; CompanionRole role = command.Role;
            GridActor actor = peer == null ? null : self.Actor(peer.MemberId);
            long worker = command.Kind == FrontWorkerKind.Player ? peer.MemberId : role.Id;
            long instance = command.Kind == FrontWorkerKind.Player ? actor?.InstanceId ?? 0 : role.InstanceId;
            if (command.Intent.Action == FrontWorkAction.Stop)
            {
                if (work.WorkerKind != command.Kind || work.WorkerId != worker || work.WorkerInstanceId != instance || work.ClaimSerial != command.Intent.ClaimSerial)
                    return "ClaimChanged";
                self.ReleaseFrontClaim(level, work, FrontWorkState.Available, true); return null;
            }
            if (work.State is FrontWorkState.Approaching or FrontWorkState.Working) return "WorkBusy";
            if (work.ClaimSerial != command.Intent.ClaimSerial) return "ClaimChanged";
            error = self.CheckFrontTarget(level, work); if (error != null) return error;
            if (work.BaseTicks <= 0) return "InvalidWork";
            if (command.Kind == FrontWorkerKind.Companion && work.Kind == FrontWorkKind.Clear) return "PlayerWorkOnly";
            if (actor != null && (actor.FrontWorkId != 0 || actor.WorkId != 0 || actor.HandItemId != 0 || actor.Motion != GridMotion.Standing || actor.HasIntent) ||
                role != null && (role.WorkId != 0 || role.Motion != GridMotion.Standing)) return "WorkerBusy";
            if (command.Kind == FrontWorkerKind.Player && actor == null) return "EntryClosed";
            if (level.Children.Values.OfType<FrontWork>().Any(value => value.Id != work.Id && value.StationId == work.StationId &&
                value.State is FrontWorkState.Approaching or FrontWorkState.Working)) return "StationBusy";
            GridCell start = actor?.Cell ?? role.Cell; GridStation station = self.Station(work.StationId);
            int best = int.MaxValue; GridCell operation = default;
            foreach (GridCell approach in station.StationApproaches())
            {
                if (actor != null && !self.CanStop(approach, actor)) continue;
                if (level.Children.Values.OfType<FrontWork>().Any(value => value.Id != work.Id && value.OperationCell == approach &&
                    value.State is FrontWorkState.Approaching or FrontWorkState.Working)) continue;
                int length=self.GridPathLength(start,approach);
                if(length>=0 && length<best)
                {
                    best=length;operation=approach;
                }
            }
            if (best == int.MaxValue) return "Unreachable";
            if (work.ClaimSerial == long.MaxValue) return "InvalidCommand";
            if (actor != null && !self.PlanRoute(actor, operation)) return "Unreachable";
            if(role!=null && !self.FindGridPathInto(start,operation,role.Path))
            {
                return "Unreachable";
            }
            ++work.ClaimSerial; work.WorkerKind = command.Kind; work.WorkerId = worker; work.WorkerInstanceId = instance;
            work.WorkerGeneration = actor?.Generation ?? role.LevelGeneration; work.WorkerCommandId = command.RequestId;
            work.OperationCell = operation; work.State = FrontWorkState.Approaching; work.FrozenTicks = work.BaseTicks; work.StartedTick = self.Tick;
            if (role != null && work.Kind == FrontWorkKind.Wash)
            {
                Companion identity = role.Companion; CompanionGrowth growth = level.GetComponent<CompanionGrowth>();
                if (growth.CompletedByCompanion.TryGetValue(identity.Id, out int count) && count >= 3)
                    work.FrozenTicks = Math.Max(1, work.BaseTicks / 2 + work.BaseTicks % 2);
            }
            if (actor != null) { actor.FrontWorkId = work.Id; actor.FrontClaimSerial = work.ClaimSerial; actor.ActiveIntent = new GridIntent(0, GridAction.Move, operation); actor.HasIntent = true; }
            else
            {
                role.WorkId = work.Id; role.ClaimSerial = work.ClaimSerial; role.Path.RemoveAt(0); role.PathIndex = 0;
            }
            ++work.TransitionSerial; work.TransitionQueued = work.StartReceived = work.CompletionReceived = false;
            self.PublishFrontStage(level, work, FrontWorkStage.Claimed);
            return null;
        }

        private static void AdvanceCompanionMovement(this GridKitchenComponent self, Level level, CompanionRole role)
        {
            if (role.Motion == GridMotion.Transit)
            {
                if (self.Tick - role.StepStartTick < GridKitchenRules.StepTicks) return;
                role.Cell = role.NextCell; role.Motion = GridMotion.Standing; ++role.PathIndex; self.Dirty = true;
            }
            if (role.PathIndex >= role.Path.Count) return;
            GridCell next = role.Path[role.PathIndex];
            if (!self.IsWalkable(next))
            {
                FrontWork work = level.GetChild<FrontWork>(role.WorkId);
                if (work != null) self.ReleaseFrontClaim(level, work, FrontWorkState.Available, true);
                return;
            }
            role.NextCell = next; role.StepStartTick = self.Tick; role.Motion = GridMotion.Transit; self.Dirty = true;
        }

        private static void ReleaseFrontClaim(this GridKitchenComponent self, Level level, FrontWork work, FrontWorkState state, bool cancelRoute)
        {
            if (work.WorkerKind == FrontWorkerKind.Player)
            {
                GridActor actor = self.Actor((int)work.WorkerId);
                if (actor != null && actor.InstanceId == work.WorkerInstanceId && actor.FrontWorkId == work.Id && actor.FrontClaimSerial == work.ClaimSerial)
                {
                    actor.FrontWorkId = 0; actor.FrontClaimSerial = 0;
                    if (cancelRoute && actor.HasIntent && actor.ActiveIntent.CommandId == 0)
                    { actor.HasIntent = false; actor.Path.Clear(); actor.PathIndex = 0; actor.Goal = actor.Motion == GridMotion.Transit ? actor.NextCell : actor.Cell; actor.StopCell = actor.Goal; }
                }
            }
            else if (work.WorkerKind == FrontWorkerKind.Companion)
            {
                CompanionRole role = level.GetChild<CompanionRole>(work.WorkerId);
                if (role != null && role.InstanceId == work.WorkerInstanceId && role.WorkId == work.Id && role.ClaimSerial == work.ClaimSerial)
                { role.WorkId = 0; role.ClaimSerial = 0; if (cancelRoute) { role.Path.Clear(); role.PathIndex = 0; } }
            }
            work.State = state; work.WorkerKind = FrontWorkerKind.None; work.WorkerId = 0; work.WorkerInstanceId = 0;
            work.WorkerGeneration = 0; work.WorkerCommandId = 0; self.Dirty = true;
            ++work.TransitionSerial; work.TransitionQueued = work.StartReceived = work.CompletionReceived = false;
            self.PublishFrontStage(level, work, state == FrontWorkState.Completed ? FrontWorkStage.Finished
                : state == FrontWorkState.Cancelled ? FrontWorkStage.Cancelled : FrontWorkStage.Released);
        }

        public static void ReleaseActorFrontWork(this GridKitchenComponent self, GridActor actor, bool cancelRoute = true)
        {
            Level level = self.ProductLevel(); FrontWork work = level?.GetChild<FrontWork>(actor.FrontWorkId);
            if (work != null && work.WorkerKind == FrontWorkerKind.Player && work.WorkerInstanceId == actor.InstanceId)
                self.ReleaseFrontClaim(level, work, FrontWorkState.Available, cancelRoute);
        }

        public static void PauseFrontWork(this GridKitchenComponent self, Level level, bool all = false)
        {
            FrontWorkCommands commands = level.GetComponent<FrontWorkCommands>();
            while (commands.Pending.Count > 0)
            {
                FrontWorkEnvelope command = commands.Pending.Dequeue(); HostPeer peer = command.Peer; CompanionRole role = command.Role;
                peer?.RecordProductResult(command.Context, ProductCommandKind.FrontWork, GridResultStatus.Cancelled, "Paused");
                if (role != null)
                { role.LastCommittedRequestId = command.RequestId; role.LastResultStatus = GridResultStatus.Cancelled; role.LastWorkReason = "Paused"; NotifyFrontResult(role, command.RequestId, command.Intent.WorkId, 0, GridResultStatus.Cancelled, "Paused", CompanionNotificationKind.RequestResult); }
            }
            foreach (FrontWork work in level.Children.Values.OfType<FrontWork>().Where(value => value.State == FrontWorkState.Approaching || all && value.State == FrontWorkState.Working).ToArray())
                self.ReleaseFrontClaim(level, work, FrontWorkState.Available, true);
            self.Dirty = true;
        }

        private static void NotifyFrontResult(CompanionRole role, long request, long work, long serial, GridResultStatus status, string reason, CompanionNotificationKind kind)
        {
            ProcessInnerSender sender = role.Root().GetComponent<ProcessInnerSender>();
            if (sender == null || sender.IsStopped) return;
            sender.Send(role.GetActorId(), ProductMessages.Create<CompanionWorkResult>(message =>
            { message.RoleId = role.Id; message.RoleInstanceId = role.InstanceId; message.LevelGeneration = role.LevelGeneration;
                message.RequestId = request; message.WorkId = work; message.ClaimSerial = serial; message.Status = (int)status; message.Reason = reason; message.EventKind = (int)kind; }));
        }
    }
}
#endif
