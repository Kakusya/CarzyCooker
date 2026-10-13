#if !DOTNET
using Cysharp.Threading.Tasks;

namespace ET
{
    [FriendOf(typeof(GridClientComponent))]
    public static class ProductOutcomeConsumer
    {
        public static bool Matches(GridClientComponent view, string match, string world, long generation, long level, long levelGeneration,
            int member, long binding, long version)
        {
            return view != null && view.Confirmed && view.MatchId == match && view.WorldId == world && view.WorldGeneration == generation &&
                view.LevelId == level && view.LevelGeneration == levelGeneration && view.MemberId == member &&
                view.BindingGeneration == binding && version <= view.Version;
        }
    }

    [MessageHandler(SceneType.ProductClient)]
    [FriendOf(typeof(GridClientComponent))]
    public sealed class ProductCommandOutcomeHandler : MessageHandler<ClientSessionComponent, ProductCommandOutcome>
    {
        protected override UniTask Run(ClientSessionComponent client, ProductCommandOutcome message)
        {
            try
            {
                GridClientComponent view = client.GetComponent<GridClientComponent>();
                if (!ProductOutcomeConsumer.Matches(view, message.MatchId, message.WorldId, message.WorldGeneration, message.LevelId, message.LevelGeneration,
                    message.MemberId, message.BindingGeneration, message.Version) || message.OutcomeSerial <= view.LastProductOutcomeSerial)
                {
                    return UniTask.CompletedTask;
                }
                var context = new ProductCommandContext(message.MatchId, message.WorldId, message.WorldGeneration, message.OriginLevelId,
                    message.OriginLevelGeneration, message.BindingGeneration, message.CommandId);
                bool changedLevel = message.LevelId != message.OriginLevelId || message.LevelGeneration != message.OriginLevelGeneration;
                var value = new ProductCommandOutcomeValue(context, (ProductCommandKind)message.Kind, (GridResultStatus)message.Status, message.Reason, changedLevel);
                view.LastProductOutcomeSerial = message.OutcomeSerial;
                view.LastProductOutcome = value;
                if (message.CommandId >= view.LastCommandId)
                {
                    view.LastCommandId = message.CommandId; view.LastCommandKind = value.Kind;
                    view.LastStatus = value.Status; view.LastReason = value.Reason;
                }
                EventSystem.Instance.Publish((Scene)client.IScene, new ProductCommandOutcomeApplied { Value = value });
                EventSystem.Instance.Publish((Scene)client.IScene, new GridProjectionApplied { Version = view.Version });
                return UniTask.CompletedTask;
            }
            finally
            {
                message.Dispose();
            }
        }
    }

    [MessageHandler(SceneType.ProductClient)]
    [FriendOf(typeof(GridClientComponent))]
    public sealed class KitchenWorkOutcomeHandler : MessageHandler<ClientSessionComponent, KitchenWorkOutcome>
    {
        protected override UniTask Run(ClientSessionComponent client, KitchenWorkOutcome message)
        {
            try
            {
                GridClientComponent view = client.GetComponent<GridClientComponent>();
                if (!ProductOutcomeConsumer.Matches(view, message.MatchId, message.WorldId, message.WorldGeneration, message.LevelId, message.LevelGeneration,
                    message.MemberId, message.BindingGeneration, message.Version) || message.OutcomeSerial <= view.LastKitchenOutcomeSerial)
                {
                    return UniTask.CompletedTask;
                }
                var value = new KitchenWorkOutcomeValue((KitchenProcessKind)message.ProcessKind, (KitchenWorkStage)message.Stage,
                    message.WorkId, message.WorkInstanceId, message.TransitionSerial, message.OutputItemId);
                view.LastKitchenOutcomeSerial = message.OutcomeSerial;
                view.LastKitchenOutcome = value;
                EventSystem.Instance.Publish((Scene)client.IScene, new KitchenWorkOutcomeApplied { Value = value });
                return UniTask.CompletedTask;
            }
            finally
            {
                message.Dispose();
            }
        }
    }
    [MessageHandler(SceneType.ProductClient)]
    [FriendOf(typeof(GridClientComponent))]
    public sealed class ShipmentOutcomeHandler : MessageHandler<ClientSessionComponent, ShipmentOutcome>
    {
        protected override UniTask Run(ClientSessionComponent client, ShipmentOutcome message)
        {
            try
            {
                GridClientComponent view = client.GetComponent<GridClientComponent>();
                if (!ProductOutcomeConsumer.Matches(view, message.MatchId, message.WorldId, message.WorldGeneration, message.LevelId, message.LevelGeneration,
                    message.MemberId, message.BindingGeneration, message.Version) || message.OutcomeSerial <= view.LastShipmentOutcomeSerial)
                    return UniTask.CompletedTask;
                var value = new ShipmentOutcomeValue(message.ShipmentId, message.ShipmentInstanceId, message.TransitionSerial, (ShipmentStage)message.Stage);
                view.LastShipmentOutcomeSerial = message.OutcomeSerial; view.LastShipmentOutcome = value;
                EventSystem.Instance.Publish((Scene)client.IScene, new ShipmentOutcomeApplied { Value = value });
                return UniTask.CompletedTask;
            }
            finally { message.Dispose(); }
        }
    }
    [MessageHandler(SceneType.ProductClient)]
    [FriendOf(typeof(GridClientComponent))]
    public sealed class FrontWorkOutcomeHandler : MessageHandler<ClientSessionComponent, FrontWorkOutcome>
    {
        protected override UniTask Run(ClientSessionComponent client, FrontWorkOutcome message)
        {
            try
            {
                GridClientComponent view = client.GetComponent<GridClientComponent>();
                if (!ProductOutcomeConsumer.Matches(view, message.MatchId, message.WorldId, message.WorldGeneration, message.LevelId, message.LevelGeneration,
                    message.MemberId, message.BindingGeneration, message.Version) || message.OutcomeSerial <= view.LastFrontWorkOutcomeSerial)
                    return UniTask.CompletedTask;
                var value = new FrontWorkOutcomeValue(message.WorkId, message.WorkInstanceId, message.ClaimSerial, (FrontWorkKind)message.Kind, (FrontWorkStage)message.Stage);
                view.LastFrontWorkOutcomeSerial = message.OutcomeSerial; view.LastFrontWorkOutcome = value;
                EventSystem.Instance.Publish((Scene)client.IScene, new FrontWorkOutcomeApplied { Value = value });
                return UniTask.CompletedTask;
            }
            finally { message.Dispose(); }
        }
    }
    [MessageHandler(SceneType.ProductClient)]
    [FriendOf(typeof(GridClientComponent))]
    public sealed class BuffPreparedHandler : MessageHandler<ClientSessionComponent, BuffPrepared>
    {
        protected override UniTask Run(ClientSessionComponent client, BuffPrepared message)
        {
            try
            {
                GridClientComponent view = client.GetComponent<GridClientComponent>();
                if (!ProductOutcomeConsumer.Matches(view, message.MatchId, message.WorldId, message.WorldGeneration, message.LevelId, message.LevelGeneration,
                    message.MemberId, message.BindingGeneration, message.Version) || message.OutcomeSerial <= view.LastBuffPreparedSerial || message.DrawVersion != view.BuffDrawVersion)
                    return UniTask.CompletedTask;
                var value = new BuffPreparedValue(message.LevelId, message.LevelGeneration, message.DrawVersion);
                view.LastBuffPreparedSerial = message.OutcomeSerial; view.LastBuffPrepared = value;
                EventSystem.Instance.Publish((Scene)client.IScene, new BuffPreparedApplied { Value = value });
                return UniTask.CompletedTask;
            }
            finally { message.Dispose(); }
        }
    }
    [MessageHandler(SceneType.ProductClient)]
    [FriendOf(typeof(GridClientComponent))]
    public sealed class RenovationPreparedHandler : MessageHandler<ClientSessionComponent, RenovationPrepared>
    {
        protected override UniTask Run(ClientSessionComponent client, RenovationPrepared message)
        {
            try
            {
                GridClientComponent view = client.GetComponent<GridClientComponent>();
                if (!ProductOutcomeConsumer.Matches(view, message.MatchId, message.WorldId, message.WorldGeneration, message.LevelId, message.LevelGeneration,
                    message.MemberId, message.BindingGeneration, message.Version) || message.OutcomeSerial <= view.LastRenovationPreparedSerial ||
                    message.ChoiceVersion != view.RenovationChoiceVersion || message.LevelId != view.RenovationLevelId) return UniTask.CompletedTask;
                var value = new RenovationPreparedValue(message.LevelId, message.LevelGeneration, message.ChoiceVersion);
                view.LastRenovationPreparedSerial = message.OutcomeSerial; view.LastRenovationPrepared = value;
                EventSystem.Instance.Publish((Scene)client.IScene, new RenovationPreparedApplied { Value = value });
                return UniTask.CompletedTask;
            }
            finally { message.Dispose(); }
        }
    }
    [MessageHandler(SceneType.ProductClient)]
    [FriendOf(typeof(GridClientComponent))]
    public sealed class LevelPhaseChangedHandler : MessageHandler<ClientSessionComponent, LevelPhaseChanged>
    {
        protected override UniTask Run(ClientSessionComponent client, LevelPhaseChanged message)
        {
            try
            {
                GridClientComponent view = client.GetComponent<GridClientComponent>();
                if (!ProductOutcomeConsumer.Matches(view, message.MatchId, message.WorldId, message.WorldGeneration, message.LevelId, message.LevelGeneration,
                    message.MemberId, message.BindingGeneration, message.Version) || message.OutcomeSerial <= view.LastLevelPhaseSerial) return UniTask.CompletedTask;
                var value = new LevelPhaseOutcomeValue(message.LevelId, message.LevelGeneration, (LevelPhase)message.Phase, message.Paused);
                view.LastLevelPhaseSerial = message.OutcomeSerial; view.LastLevelPhase = value;
                EventSystem.Instance.Publish((Scene)client.IScene, new LevelPhaseChangedApplied { Value = value });
                return UniTask.CompletedTask;
            }
            finally { message.Dispose(); }
        }
    }
}
#endif
