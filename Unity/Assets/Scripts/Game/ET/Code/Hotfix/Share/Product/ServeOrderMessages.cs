#if !DOTNET
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class ServePeerHandler : ProductActorRequestHandler<HostPeer,ServeOrderRequest,ServeOrderResponse>
    {
        protected override async UniTask Run(HostPeer peer,ServeOrderRequest request,ServeOrderResponse response)
        {
            response.CommandId=request.CommandId;
            if(!peer.EntryOpen){response.Error=ProductError.PeerClosed;response.Status=(int)GridResultStatus.Rejected;return;}
            var inner=ProductMessages.Create<ServeOrderSubmitRequest>(value=>
            {
                value.PeerId=peer.Id;value.PeerInstanceId=peer.InstanceId;value.PeerGeneration=peer.Generation;value.ProtocolVersion=request.ProtocolVersion;
                value.MatchId=request.MatchId;value.WorldId=request.WorldId;value.WorldGeneration=request.WorldGeneration;value.BindingGeneration=request.BindingGeneration;
                value.LevelId=request.LevelId;value.LevelGeneration=request.LevelGeneration;value.CommandId=request.CommandId;
                value.OrderId=request.OrderId;value.CustomerId=request.CustomerId;value.TableStationId=request.TableStationId;value.ItemId=request.ItemId;
            });
            HostSessionComponent authority=peer.Authority;
            using var result=(ServeOrderSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(),inner,false);
            response.Error=result.Error;response.Message=result.Message;response.Status=result.Status;
        }
    }
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    public sealed class ServeSubmitHandler : ProductActorRequestHandler<Entity,ServeOrderSubmitRequest,ServeOrderSubmitResponse>
    {
        protected override UniTask Run(Entity target,ServeOrderSubmitRequest request,ServeOrderSubmitResponse response)
        {
            response.CommandId=request.CommandId;
            if(target is not HostSessionComponent authority){response.Error=ProductError.InvalidRequest;response.Message="InternalOnly";response.Status=(int)GridResultStatus.Rejected;return UniTask.CompletedTask;}
            HostPeer peer=authority.GetChild<HostPeer>(request.PeerId);Match match=authority.Match;
            string error=request.ProtocolVersion!=ProductSessionRules.ProtocolVersion?"IncompatibleProtocol"
                :peer==null || peer.InstanceId!=request.PeerInstanceId || peer.Generation!=request.PeerGeneration || peer.Generation!=request.BindingGeneration || !peer.EntryOpen || peer.Closing?"EntryClosed"
                :match.ProductKitchen().QueueServe(peer,new ProductCommandContext(request.MatchId,request.WorldId,request.WorldGeneration,request.LevelId,request.LevelGeneration,request.BindingGeneration,request.CommandId),
                    new ServeOrderIntent(request.OrderId,request.CustomerId,request.TableStationId,request.ItemId));
            response.Error=error==null?0:ProductError.InvalidRequest;response.Message=error??peer?.LastCommandReason;
            response.Status=(int)(error==null?peer.LastCommandStatus:GridResultStatus.Rejected);return UniTask.CompletedTask;
        }
    }
    [FriendOf(typeof(GridClientComponent))]
    public static class ServeClientCommands
    {
        public static ServeOrderRequest CreateServeOrder(this GridClientComponent self,long order,long customer,long table,long item)
        {
            ClientSessionComponent client=self.GetParent<ClientSessionComponent>();
            ProductCommandContext context=client.CreateProductCommandContext(self.WorldId,self.WorldGeneration,self.LevelId,self.LevelGeneration);
            self.LastCommandId=context.CommandId;self.LastCommandKind=ProductCommandKind.Serve;self.LastStatus=GridResultStatus.Accepted;self.LastReason=null;
            return ProductMessages.Create<ServeOrderRequest>(value=>
            {
                value.ProtocolVersion=ProductSessionRules.ProtocolVersion;value.MatchId=context.MatchId;value.WorldId=context.WorldId;value.WorldGeneration=context.WorldGeneration;
                value.BindingGeneration=context.BindingGeneration;value.LevelId=context.LevelId;value.LevelGeneration=context.LevelGeneration;value.CommandId=context.CommandId;
                value.OrderId=order;value.CustomerId=customer;value.TableStationId=table;value.ItemId=item;
            });
        }
    }
}
#endif
