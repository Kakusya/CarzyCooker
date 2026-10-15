#if !DOTNET
using Game;

namespace ET
{
    [EntitySystemOf(typeof(CooperativeClaimState))]
    [FriendOf(typeof(CooperativeClaimState))]
    [FriendOf(typeof(Match))]
    public static partial class CooperativeClaimStateSystem
    {
        [EntitySystem]
        private static void Awake(this CooperativeClaimState self)
        {
            self.Results.Clear();
            self.CloseReceipts.Clear();
            self.NextPulse = 0;
            self.CloseDeadline = 0;
            self.PulsePending = self.Closing = self.CloseReady = false;
        }

        [EntitySystem]
        private static void Update(this CooperativeClaimState self)
        {
            long now = TimeInfo.Instance.FrameTime;
            if (self.PulsePending || now < self.NextPulse)
            {
                return;
            }
            self.NextPulse = now + 1000;
            self.PulsePending = true;
            HostSessionComponent authority = ((Scene)self.IScene).GetComponent<HostSessionComponent>();
            self.Root().GetComponent<ProcessInnerSender>().Send(authority.GetActorId(), ProductMessages.Create<ProgressPulse>(message =>
            {
                message.MatchId = self.GetParent<Match>().MatchId;
                message.StateId = self.Id;
                message.StateInstanceId = self.InstanceId;
            }));
        }

        [EntitySystem]
        private static void Destroy(this CooperativeClaimState self)
        {
            self.Results.Clear();
            self.CloseReceipts.Clear();
        }
    }

    [EntitySystemOf(typeof(CooperativeSaveResult))]
    [FriendOf(typeof(CooperativeSaveResult))]
    public static partial class CooperativeSaveResultSystem
    {
        [EntitySystem]
        private static void Awake(this CooperativeSaveResult self)
        {
            self.ResultId = self.WorldId = null;
            self.LevelId = self.LevelGeneration = 0;
            self.UnsignedSuccess = self.SignedSuccess = null;
            self.State = SoloSaveState.Ready;
            self.Failure = null;
            self.Preparing = self.HostSaveRequired = self.HostSaveCommitted = false;
            self.HostSavedReceipt = null;
            self.Claims.Clear();
            self.PendingControl = null;
        }

        [EntitySystem]
        private static void Destroy(this CooperativeSaveResult self)
        {
            self.GetComponent<AsyncOwnerComponent>()?.AsyncScope?.Stop();
            self.UnsignedSuccess = self.SignedSuccess = null;
            self.HostSavedReceipt = null;
            self.PendingControl = null;
            self.Claims.Clear();
        }
    }

    [EntitySystemOf(typeof(CooperativeClaim))]
    [FriendOf(typeof(CooperativeClaim))]
    public static partial class CooperativeClaimSystem
    {
        [EntitySystem]
        private static void Awake(this CooperativeClaim self)
        {
            self.TicketId = self.ProfileId = self.KeyId = null;
            self.Document = null;
            self.AckedBindings.Clear();
        }

        [EntitySystem]
        private static void Destroy(this CooperativeClaim self)
        {
            self.Document = null;
            self.AckedBindings.Clear();
        }
    }

    [EntitySystemOf(typeof(CooperativeCloseReceipt))]
    [FriendOf(typeof(CooperativeCloseReceipt))]
    public static partial class CooperativeCloseReceiptSystem
    {
        [EntitySystem]
        private static void Awake(this CooperativeCloseReceipt self)
        {
            self.ReceiptId = self.ProfileId = null;
            self.Preparing = false;
            self.Document = null;
            self.AckedBindings.Clear();
        }

        [EntitySystem]
        private static void Destroy(this CooperativeCloseReceipt self)
        {
            self.GetComponent<AsyncOwnerComponent>()?.AsyncScope?.Stop();
            self.Document = null;
            self.AckedBindings.Clear();
        }
    }

    [EntitySystemOf(typeof(ClientProgressDelivery))]
    [FriendOf(typeof(ClientProgressDelivery))]
    public static partial class ClientProgressDeliverySystem
    {
        [EntitySystem]
        private static void Awake(this ClientProgressDelivery self)
        {
            self.Pending.Clear();
            self.PendingIds.Clear();
            self.Running = false;
            self.Failure = null;
        }

        [EntitySystem]
        private static void Destroy(this ClientProgressDelivery self)
        {
            self.GetComponent<AsyncOwnerComponent>()?.AsyncScope?.Stop();
            self.Pending.Clear();
            self.PendingIds.Clear();
            self.Running = false;
        }
    }
}
#endif
