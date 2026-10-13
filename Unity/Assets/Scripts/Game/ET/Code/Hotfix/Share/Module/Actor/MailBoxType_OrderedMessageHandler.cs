using Cysharp.Threading.Tasks;

namespace ET
{
    [Invoke((long)MailBoxType.OrderedMessage)]
    [FriendOf(typeof(ProcessInnerSender))]
    public class MailBoxType_OrderedMessageHandler: AInvokeHandler<MailBoxInvoker>
    {
        public override void Handle(MailBoxInvoker args)
        {
            HandleInner(args).Forget();
        }

        private static async UniTask HandleInner(MailBoxInvoker args)
        {
            MailBoxComponent mailBoxComponent = args.MailBoxComponent;
            
            MessageObject messageObject = args.MessageObject;

            Fiber fiber = mailBoxComponent.Fiber();
            bool handedOff = false;
            try
            {
                if (fiber.IsDisposed)
                {
                    return;
                }
                EntityRef<ProcessInnerSender> senderRef = fiber.Root.GetComponent<ProcessInnerSender>();

                long instanceId = mailBoxComponent.InstanceId;
                using (await fiber.Root.GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.Mailbox, mailBoxComponent.ParentInstanceId))
                {
                    if (mailBoxComponent.InstanceId != instanceId)
                    {
                        if (messageObject is IRequest request)
                        {
                            IResponse resp = MessageHelper.CreateResponse(request.GetType(), request.RpcId, ErrorCore.ERR_NotFoundActor);
                            ProcessInnerSender sender = senderRef;
                            if (sender == null || sender.IsStopped)
                            {
                                ((MessageObject)resp).Dispose();
                            }
                            else
                            {
                                sender.Reply(args.FromAddress, resp);
                            }
                        }
                        return;
                    }
                    handedOff = true;
                    await MessageDispatcher.Instance.Handle(mailBoxComponent.Parent, args.FromAddress, messageObject);
                }
            }
            finally
            {
                if (!handedOff)
                {
                    messageObject.Dispose();
                }
            }
        }
    }
}
