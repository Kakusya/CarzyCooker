#if !DOTNET
using System;
using Cysharp.Threading.Tasks;
using Game;

namespace ET
{
    [MessageHandler(SceneType.ProductClient)]
    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(ClientProgressDelivery))]
    public sealed class ProgressDeliveryHandler : MessageHandler<ClientSessionComponent, ProgressDelivery>
    {
        protected override UniTask Run(ClientSessionComponent client, ProgressDelivery message)
        {
            try
            {
                if (message.ProtocolVersion != ProductSessionRules.ProtocolVersion || message.MatchId != client.MatchId ||
                    message.BindingGeneration != client.BindingGeneration || (ProgressDeliveryKind)message.Kind is not (ProgressDeliveryKind.Ticket or ProgressDeliveryKind.NormalClose) ||
                    !SoloSaveValidation.IsIdentifier(message.Id) || message.Document == null || message.Document.Length > OfflineProfiles.MaxDocumentBytes)
                {
                    return UniTask.CompletedTask;
                }
                var delivery = client.GetComponent<ClientProgressDelivery>() ?? client.AddComponent<ClientProgressDelivery>();
                if (delivery.Pending.Count < 32 && delivery.PendingIds.Add(message.Id))
                {
                    delivery.Pending.Enqueue(new ProgressDeliveryValue(message.Id, message.MatchId, message.BindingGeneration, (ProgressDeliveryKind)message.Kind, (byte[])message.Document.Clone()));
                    delivery.RunProgressDelivery();
                }
            }
            finally { message.Dispose(); }
            return UniTask.CompletedTask;
        }
    }

    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(ProcessInnerSender))]
    [FriendOf(typeof(ProductRoomClient))]
    public static partial class ClientProgressDeliverySystem
    {
        public static void RunProgressDelivery(this ClientProgressDelivery self)
        {
            if (self.Running)
            {
                return;
            }
            self.Running = true;
            EntityRef<ClientProgressDelivery> original = self;
            EntityRef<ClientSessionComponent> clientRef = self.GetParent<ClientSessionComponent>();
            EntityRef<ProcessInnerSender> senderRef = self.Root().GetComponent<ProcessInnerSender>();
            var repository = self.Root().GetComponent<SoloSaveRepository>();
            self.GetAsyncOwner().Run(async context =>
            {
                while ((self = original) != null && self.Pending.Count > 0)
                {
                    var value = self.Pending.Dequeue();
                    string failure = null;
                    try
                    {
                        if (repository == null)
                        {
                            throw new InvalidOperationException("SaveRepositoryUnavailable");
                        }
                        ClientSessionComponent source = clientRef;
                        if (source == null || source.MatchId != value.MatchId || source.BindingGeneration != value.BindingGeneration)
                        {
                            throw new InvalidOperationException("EntryClosed");
                        }
                        byte[] expectedHost = source.GetComponent<ProductRoomClient>()?.Current.HostIdentity;
                        if (expectedHost == null)
                        {
                            throw new InvalidOperationException("HostIdentityRequired");
                        }
                        using var persisted = (ProgressRepositoryResponse)await ((ProcessInnerSender)senderRef).Call(repository.GetActorId(),
                            ProductMessages.Create<ProgressRepositoryRequest>(request =>
                            {
                                request.Operation = (int)(value.Kind == ProgressDeliveryKind.Ticket ? ProgressRepositoryOperation.PersistTicket : ProgressRepositoryOperation.PersistClose); request.MatchId = value.MatchId; request.Document = value.Document;
                                request.ExpectedHostIdentity = (byte[])expectedHost.Clone();
                            }), false);
                        failure = persisted.Error == 0 && persisted.Id == value.Id ? null : persisted.Message ?? "InvalidProgressDelivery";
                        context.ThrowIfStopped(); self = original;
                        ClientSessionComponent client = clientRef;
                        if (self == null || client == null)
                        {
                            return;
                        }
                        if (failure == null && client.MatchId == value.MatchId && client.BindingGeneration == value.BindingGeneration)
                        {
                            using var reply = (ProgressAckResponse)await client.CallProduct(ProductMessages.Create<ProgressAckRequest>(request =>
                            {
                                request.ProtocolVersion = ProductSessionRules.ProtocolVersion; request.MatchId = value.MatchId;
                                request.BindingGeneration = value.BindingGeneration; request.Kind = (int)value.Kind; request.Id = value.Id;
                            }));
                            if (reply.Error != 0)
                            {
                                failure = reply.Message;
                            }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception exception) { failure = exception.Message; }
                    context.ThrowIfStopped(); self = original;
                    if (self == null)
                    {
                        return;
                    }
                    self.Failure = failure;
                    self.PendingIds.Remove(value.Id);
                }
                self = original;
                if (self != null)
                {
                    self.Running = false;
                }
            });
        }
    }
}
#endif
