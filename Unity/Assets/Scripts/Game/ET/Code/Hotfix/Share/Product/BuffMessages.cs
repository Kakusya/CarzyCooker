#if !DOTNET
using Cysharp.Threading.Tasks;
namespace ET
{
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class BuffPeerHandler : ProductActorRequestHandler<HostPeer,ChooseBuffRequest,ChooseBuffResponse>
    {
        protected override async UniTask Run(HostPeer peer,ChooseBuffRequest request,ChooseBuffResponse response)
        {
            response.CommandId=request.CommandId;
            if(!peer.EntryOpen){response.Error=ProductError.PeerClosed;response.Status=(int)GridResultStatus.Rejected;return;}
            if(!peer.CanControlRestaurant()){response.Error=ProductError.InvalidRequest;response.Message="HostControlOnly";response.Status=(int)GridResultStatus.Rejected;return;}
            var inner=ProductMessages.Create<ChooseBuffSubmitRequest>(value=>
            {
                value.PeerId=peer.Id;value.PeerInstanceId=peer.InstanceId;value.PeerGeneration=peer.Generation;value.ProtocolVersion=request.ProtocolVersion;
                value.MatchId=request.MatchId;value.WorldId=request.WorldId;value.WorldGeneration=request.WorldGeneration;value.BindingGeneration=request.BindingGeneration;
                value.LevelId=request.LevelId;value.LevelGeneration=request.LevelGeneration;value.CommandId=request.CommandId;value.DrawVersion=request.DrawVersion;value.ChoiceDefId=request.ChoiceDefId;
            });
            HostSessionComponent authority=peer.Authority;
            using var reply=(ChooseBuffSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(),inner,false);
            response.Error=reply.Error;response.Message=reply.Message;response.Status=reply.Status;
        }
    }
    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(HostSessionComponent))]
    public sealed class BuffSubmitHandler : ProductActorRequestHandler<Entity,ChooseBuffSubmitRequest,ChooseBuffSubmitResponse>
    {
        protected override UniTask Run(Entity target,ChooseBuffSubmitRequest request,ChooseBuffSubmitResponse response)
        {
            response.CommandId=request.CommandId;
            if(target is not HostSessionComponent authority){response.Error=ProductError.InvalidRequest;response.Message="InternalOnly";response.Status=(int)GridResultStatus.Rejected;return UniTask.CompletedTask;}
            HostPeer peer=authority.GetChild<HostPeer>(request.PeerId);Match match=authority.Match;
            string error=request.ProtocolVersion!=ProductSessionRules.ProtocolVersion?"IncompatibleProtocol"
                :peer==null || peer.InstanceId!=request.PeerInstanceId || peer.Generation!=request.PeerGeneration || peer.Generation!=request.BindingGeneration || !peer.EntryOpen || peer.Closing?"EntryClosed"
                :match.ProductKitchen().QueueBuffChoice(peer,new ProductCommandContext(request.MatchId,request.WorldId,request.WorldGeneration,request.LevelId,request.LevelGeneration,request.BindingGeneration,request.CommandId),new BuffChoiceIntent(request.DrawVersion,request.ChoiceDefId));
            response.Error=error==null?0:ProductError.InvalidRequest;response.Message=error??peer?.LastCommandReason;
            response.Status=(int)(error==null?peer.LastCommandStatus:GridResultStatus.Rejected);return UniTask.CompletedTask;
        }
    }
    [FriendOf(typeof(GridClientComponent))]
    public static class BuffClientCommands
    {
        public static ChooseBuffRequest CreateBuffChoice(this GridClientComponent self,int choice,long drawVersion)
        {
            ClientSessionComponent client=self.GetParent<ClientSessionComponent>();
            ProductCommandContext context=client.CreateProductCommandContext(self.WorldId,self.WorldGeneration,self.LevelId,self.LevelGeneration);
            self.LastCommandId=context.CommandId;self.LastCommandKind=ProductCommandKind.Buff;self.LastStatus=GridResultStatus.Accepted;self.LastReason=null;
            return ProductMessages.Create<ChooseBuffRequest>(value=>
            {
                value.ProtocolVersion=ProductSessionRules.ProtocolVersion;value.MatchId=context.MatchId;value.WorldId=context.WorldId;value.WorldGeneration=context.WorldGeneration;
                value.BindingGeneration=context.BindingGeneration;value.LevelId=context.LevelId;value.LevelGeneration=context.LevelGeneration;value.CommandId=context.CommandId;
                value.DrawVersion=drawVersion;value.ChoiceDefId=choice;
            });
        }
    }
}
#endif
