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
}
#endif
