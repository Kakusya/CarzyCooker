#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    // 沿用 IMHandler/MessageDispatcher/MailBox，只补本产品输入与晚到答复的所有权边界。
    [FriendOf(typeof(ProcessInnerSender))]
    public abstract class ProductActorRequestHandler<T, TRequest, TResponse> : HandlerObject, IMHandler
        where T : Entity
        where TRequest : MessageObject, IRequest
        where TResponse : MessageObject, IResponse
    {
        protected abstract UniTask Run(T entity, TRequest request, TResponse response);

        public async UniTask Handle(Entity entity, Address fromAddress, MessageObject actorMessage)
        {
            var request = (TRequest)actorMessage;
            int rpcId = request.RpcId;
            EntityRef<ProcessInnerSender> senderRef = entity.Root().GetComponent<ProcessInnerSender>();
            var response = (TResponse)ObjectPool.Instance.Fetch(typeof(TResponse), false);
            try
            {
                await Run((T)entity, request, response);
            }
            catch (RpcException e)
            {
                response.Error = e.Error;
                response.Message = e.Message;
            }
            catch (Exception e)
            {
                response.Error = ErrorCore.ERR_RpcFail;
                response.Message = e.Message;
            }
            finally
            {
                request.Dispose();
            }
            response.RpcId = rpcId;
            ProcessInnerSender sender = senderRef;
            if (sender == null || sender.IsStopped)
            {
                response.Dispose();
                return;
            }
            sender.Reply(fromAddress, response);
        }

        public Type GetRequestType() => typeof(TRequest);
        public Type GetResponseType() => typeof(TResponse);
    }
}
#endif
