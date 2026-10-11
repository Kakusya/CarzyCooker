#if !DOTNET
using System;

namespace ET
{
    public static class ProductMessages
    {
        // 独立非池化 DTO；输入/响应仍显式 Dispose，不与任何接收者共享可写消息。
        public static T Create<T>(Action<T> configure) where T : MessageObject
        {
            T message = (T)ObjectPool.Instance.Fetch(typeof(T), false);
            configure(message);
            return message;
        }
    }
    [FriendOf(typeof(LocalDelivery))]
    [FriendOf(typeof(RemoteDelivery))]
    public static class ProductDelivery
    {
        public static void Send(HostPeer peer, MessageObject message)
        {
            LocalDelivery local = peer.GetComponent<LocalDelivery>();
            ClientSessionComponent client = local?.Client ?? default;
            if (client != null)
            {
                ProcessInnerSender sender = peer.Root().GetComponent<ProcessInnerSender>();
                sender.Send(client.GetActorId(), (IMessage)message);
                return; // 本地消息由接收 handler 释放。
            }
            RemoteDelivery remote = peer.GetComponent<RemoteDelivery>();
            Session session = remote?.Session ?? default;
            try
            {
                if (session != null)
                {
                    session.Send((IMessage)message);
                }
            }
            finally
            {
                message.Dispose(); // Session.Send 已完成序列化，不负责回收输入。
            }
        }
    }
}
#endif
