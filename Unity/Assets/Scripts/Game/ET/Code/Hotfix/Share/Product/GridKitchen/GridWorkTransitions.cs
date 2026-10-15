#if !DOTNET
namespace ET
{
    [FriendOf(typeof(GridManualProcess))]
    [FriendOf(typeof(GridAutoProcess))]
    [FriendOf(typeof(GridBakeProcess))]
    public static partial class GridKitchenSystem
    {
        private static void RequestKitchenCompletion(this GridKitchenComponent self, Entity work, KitchenProcessKind kind, long serial)
        {
            HostSessionComponent authority = ((Scene)self.IScene).GetComponent<HostSessionComponent>();
            var message = ProductMessages.Create<KitchenWorkTransition>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = ((Match)self.Match).MatchId;
                value.WorldId = self.WorldId; value.WorldGeneration = self.WorldGeneration;
                // 持续厨房作业按世界及原实例完成，不携旧角色的 Level 作为授权。
                value.SourceId = work.Id; value.SourceInstanceId = work.InstanceId; value.TransitionSerial = serial;
                value.ProcessKind = (int)kind; value.Action = (int)KitchenTransitionAction.Complete;
            });
            self.Root().GetComponent<ProcessInnerSender>().Send(authority.GetActorId(), message);
        }

        private static void AdvanceKitchenTransitions(this GridKitchenComponent self)
        {
            while (self.WorkTransitions.Count > 0)
            {
                KitchenWorkTransitionValue value = self.WorkTransitions.Dequeue();
                ProductTransitionIdentity identity = value.Identity;
                if (identity.WorldId != self.WorldId || identity.WorldGeneration != self.WorldGeneration)
                {
                    continue;
                }
                switch (value.Kind)
                {
                    case KitchenProcessKind.Manual:
                        GridManualProcess manual = self.Process(identity.SourceId);
                        if (manual != null && manual.InstanceId == identity.SourceInstanceId && manual.TransitionSerial == identity.Serial &&
                            manual.State == GridWorkState.Working && manual.CompletionQueued && manual.ProgressTicks >= manual.RequiredTicks)
                        {
                            manual.CompletionReceived = true;
                        }
                        break;
                    case KitchenProcessKind.Soup:
                    case KitchenProcessKind.Juice:
                    case KitchenProcessKind.Mix:
                    case KitchenProcessKind.Beverage:
                        GridAutoProcess soup = self.AutoProcess(identity.SourceId);
                        if (soup != null && soup.InstanceId == identity.SourceInstanceId && soup.TransitionSerial == identity.Serial &&
                            soup.State == GridAutoState.Cooking && soup.CompletionQueued && soup.ProgressTicks >= soup.RequiredTicks &&
                            AutoWorkKind(soup.RequiredCapability) == value.Kind)
                        {
                            soup.CompletionReceived = true;
                        }
                        break;
                    case KitchenProcessKind.Bake:
                        GridBakeProcess bake = self.BakeProcess(identity.SourceId);
                        if (bake != null && bake.InstanceId == identity.SourceInstanceId && bake.TransitionSerial == identity.Serial &&
                            bake.State == GridBakeState.Baking && bake.CompletionQueued && bake.ProgressTicks >= bake.RequiredTicks)
                        {
                            bake.CompletionReceived = true;
                        }
                        break;
                }
            }
        }

        private static void PublishKitchenStage(this GridKitchenComponent self, Entity work, KitchenProcessKind kind, KitchenWorkStage stage, long serial, long output = 0)
        {
            self.WorkOutcomes.Add(new KitchenWorkOutcomeValue(kind, stage, work, serial, output));
            self.Dirty = true;
        }

        private static void PauseManualProcess(this GridKitchenComponent self, GridManualProcess process)
        {
            if (process.State != GridWorkState.Working)
            {
                return;
            }
            process.State = GridWorkState.Paused; process.WorkerMemberId = 0; process.WorkerGeneration = 0;
            process.CompletionQueued = false; process.CompletionReceived = false;
            self.PublishKitchenStage(process, KitchenProcessKind.Manual, KitchenWorkStage.Paused, ++process.TransitionSerial);
        }
    }
}
#endif
