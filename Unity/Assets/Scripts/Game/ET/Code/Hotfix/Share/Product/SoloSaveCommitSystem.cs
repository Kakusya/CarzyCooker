#if !DOTNET
using System;
using Cysharp.Threading.Tasks;
using Game;

namespace ET
{
    [EntitySystemOf(typeof(SoloSaveResult))]
    [FriendOf(typeof(SoloSaveResult))]
    [FriendOf(typeof(ProcessInnerSender))]
    public static partial class SoloSaveResultSystem
    {
        [EntitySystem]
        private static void Awake(this SoloSaveResult self, SoloSaveHeader header, SoloSavePayload payload, ProductCommandContext command)
        {
            self.ResultId = header.ResultId; self.SlotId = header.SlotId; self.Record = SoloSaveFile.Encode(header, payload);
            self.LevelId = command.LevelId; self.LevelGeneration = command.LevelGeneration;
            self.WorldId = command.WorldId; self.WorldGeneration = command.WorldGeneration;
            self.State = SoloSaveState.Ready; self.Failure = null; self.Waiter = default; self.Command = command;
        }

        public static void WriteSoloResult(this SoloSaveResult self, SoloSaveRepository repository, HostSessionComponent authority)
        {
            EntityRef<SoloSaveResult> original = self; EntityRef<HostSessionComponent> authorityRef = authority;
            EntityRef<ProcessInnerSender> senderRef = self.Root().GetComponent<ProcessInnerSender>();
            var repositoryActor = repository.GetActorId();
            self.State = SoloSaveState.Writing; self.Failure = null;
            self.GetAsyncOwner().Run(async context =>
            {
                int error = 0; string failure = null; byte[] header = null;
                try
                {
                    using var reply = (SoloSaveWriteResponse)await ((ProcessInnerSender)senderRef).Call(repositoryActor,
                        ProductMessages.Create<SoloSaveWriteRequest>(r => r.Record = self.Record), false);
                    error = reply.Error; failure = reply.Message; header = reply.Header == null ? null : (byte[])reply.Header.Clone();
                }
                catch (Exception e) { error = ProductError.InvalidRequest; failure = e.Message; }
                context.ThrowIfStopped(); self = original; authority = authorityRef; var sender = (ProcessInnerSender)senderRef;
                if (self == null || authority == null || sender == null || sender.IsStopped) return;
                // The result owns this observer; closing a HUD never cancels it.
                using var receipt = (SoloSaveCompletedResponse)await sender.Call(authority.GetActorId(),
                    ProductMessages.Create<SoloSaveCompletedRequest>(r =>
                    {
                        r.ResultEntityId = self.Id; r.ResultInstanceId = self.InstanceId;
                        r.WorldId = self.WorldId; r.WorldGeneration = self.WorldGeneration; r.LevelId = self.LevelId; r.LevelGeneration = self.LevelGeneration;
                        r.Error = error; r.Failure = failure; r.Header = header;
                    }), false);
            });
        }
    }

    [FriendOf(typeof(SoloSaveBinding))]
    [FriendOf(typeof(SoloSaveResult))]
    public static partial class RestaurantRuntimeSystem
    {
        private static bool SoloSavePending(this RestaurantRuntime self)
        {
            Match match = self.Match; SoloSaveResult result = match.GetComponent<SoloSaveBinding>()?.CurrentResult ?? default;
            return result?.State == SoloSaveState.Writing;
        }

        private static string BeginSoloSave(this RestaurantRuntime self, Level level, LevelControlEnvelope control)
        {
            Match match = self.Match; var binding = match.GetComponent<SoloSaveBinding>();
            if (binding == null) return "SaveModeRequired";
            if (level.Phase != LevelPhase.Completed) return "SuccessRequired";
            if (control.Intent.NextLevelConfigId != level.NextLevelConfigId) return "NextLevelChanged";
            var repository = self.Root().GetComponent<SoloSaveRepository>();
            if (repository == null || repository.SaveProfile() != binding.ProfileId) return "SaveRepositoryUnavailable";
            SoloSaveResult result = binding.CurrentResult;
            if (result == null || result.LevelId != level.Id || result.LevelGeneration != level.Generation)
            {
                SoloSavePayload payload;
                try { payload = SoloSaveCapture.Kitchen(self); }
                catch (InvalidOperationException e) { return e.Message; }
                var header = new SoloSaveHeader
                {
                    SlotId = Guid.NewGuid().ToString("N"), ProfileId = binding.ProfileId, ResultId = Guid.NewGuid().ToString("N"),
                    SourceSlotId = binding.SourceSlotId, SourceRevision = binding.SourceRevision, CompletedLevelId = level.ConfigId,
                    NextLevelId = level.NextLevelConfigId, Score = level.Score, Stars = level.Stars, SavedUtcTicks = DateTime.UtcNow.Ticks
                };
                try
                {
                    SoloSaveValidation.Payload(header, payload, binding.ProfileId);
                    result?.Dispose();
                    result = match.AddChild<SoloSaveResult, SoloSaveHeader, SoloSavePayload, ProductCommandContext>(header, payload, control.Context);
                    binding.CurrentResult = result;
                }
                catch (InvalidOperationException e) { return e.Message; }
            }
            result.Waiter = control.Peer; result.Command = control.Context;
            if (result.State == SoloSaveState.Committed)
                return self.ApplyLevelControl(level, new LevelControlIntent(LevelControlAction.Continue, level.NextLevelConfigId));
            if (result.State != SoloSaveState.Writing)
            {
                HostPeer peer = control.Peer; HostSessionComponent authority = peer.Authority;
                result.WriteSoloResult(repository, authority);
            }
            return null;
        }

        private static string CheckSoloSaveGate(this RestaurantRuntime self, Level level)
        {
            Match match = self.Match; var binding = match.GetComponent<SoloSaveBinding>();
            if (binding == null) return null;
            SoloSaveResult result = binding.CurrentResult;
            return result != null && result.LevelId == level.Id && result.LevelGeneration == level.Generation &&
                result.State == SoloSaveState.Committed ? null : "SaveRequired";
        }

        private static void AdvanceSoloSaveCompletions(this RestaurantRuntime self)
        {
            Match match = self.Match; var binding = match.GetComponent<SoloSaveBinding>();
            if (binding == null) return;
            GridKitchenComponent kitchen = self.Kitchen; Level level = self.CurrentLevel;
            while (binding.Completions.Count > 0)
            {
                var completion = binding.Completions.Dequeue(); SoloSaveResult result = completion.Result;
                if (result == null || (SoloSaveResult)binding.CurrentResult != result || result.State != SoloSaveState.Writing ||
                    result.LevelId != level.Id || result.LevelGeneration != level.Generation || result.WorldId != kitchen.WorldId ||
                    result.WorldGeneration != kitchen.WorldGeneration || level.Phase != LevelPhase.Completed) continue;
                result.State = completion.Committed ? SoloSaveState.Committed : SoloSaveState.Failed; result.Failure = completion.Failure;
                HostPeer peer = result.Waiter; kitchen.Dirty = true;
                if (peer == null || !kitchen.HasBinding(peer.MemberId, result.Command.BindingGeneration)) continue;
                if (completion.Committed)
                    self.Controls.Enqueue(new LevelControlEnvelope(result.Command, new LevelControlIntent(LevelControlAction.Continue, level.NextLevelConfigId), peer));
                else peer.RecordProductResult(result.Command, ProductCommandKind.Level, GridResultStatus.Rejected, "SaveFailed");
            }
        }

        private static void AdvanceSoloSaveSource(this RestaurantRuntime self)
        {
            Match match = self.Match; var binding = match.GetComponent<SoloSaveBinding>();
            SoloSaveResult result = binding?.CurrentResult ?? default;
            if (result?.State != SoloSaveState.Committed) return;
            binding.SourceSlotId = result.SlotId; binding.SourceRevision = 1;
        }

        private static void StopSoloSave(this RestaurantRuntime self)
        {
            Match match = self.Match; var binding = match?.GetComponent<SoloSaveBinding>();
            SoloSaveResult result = binding?.CurrentResult ?? default;
            result?.GetComponent<AsyncOwnerComponent>()?.AsyncScope?.Stop(); binding?.Completions.Clear();
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostSessionComponent))]
    [FriendOf(typeof(SoloSaveBinding))]
    [FriendOf(typeof(SoloSaveResult))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(Level))]
    public sealed class SoloSaveCompletedHandler : ProductActorRequestHandler<Entity, SoloSaveCompletedRequest, SoloSaveCompletedResponse>
    {
        protected override UniTask Run(Entity target, SoloSaveCompletedRequest request, SoloSaveCompletedResponse response)
        {
            if (target is not HostSessionComponent authority)
            { response.Error = ProductError.InvalidRequest; response.Message = "InternalOnly"; return UniTask.CompletedTask; }
            Match match = authority.Match; var binding = match.GetComponent<SoloSaveBinding>();
            SoloSaveResult result = binding?.CurrentResult ?? default;
            if (result == null || result.Id != request.ResultEntityId || result.InstanceId != request.ResultInstanceId ||
                result.WorldId != request.WorldId || result.WorldGeneration != request.WorldGeneration || result.LevelId != request.LevelId ||
                result.LevelGeneration != request.LevelGeneration || result.State != SoloSaveState.Writing)
            { response.Error = ProductError.InvalidRequest; response.Message = "StaleSaveResult"; return UniTask.CompletedTask; }
            bool committed = request.Error == 0; string failure = request.Failure;
            if (committed)
            {
                try
                {
                    if (request.Header == null) throw new InvalidOperationException("MissingCommittedHeader");
                    var header = (SoloSaveHeader)MemoryPackHelper.Deserialize(typeof(SoloSaveHeader), request.Header, 0, request.Header.Length);
                    SoloSaveValidation.Header(header, binding.ProfileId);
                    if (header.ResultId != result.ResultId || header.SlotId != result.SlotId) throw new InvalidOperationException("WrongSaveResult");
                }
                catch (Exception e) { committed = false; failure = e.Message; }
            }
            binding.Completions.Enqueue(new SoloSaveCompletion(result, committed, failure));
            return UniTask.CompletedTask;
        }
    }
}
#endif
