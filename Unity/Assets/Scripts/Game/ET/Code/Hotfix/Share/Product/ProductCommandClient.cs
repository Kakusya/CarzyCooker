#if !DOTNET
using System;

namespace ET
{
    // 编号原语不调用客户端投影，保持会话与 GridClient 的依赖方向。
    [FriendOf(typeof(ClientSessionComponent))]
    public static class ProductCommandClient
    {
        public static ProductCommandContext CreateProductCommandContext(this ClientSessionComponent self,
            string worldId, long worldGeneration, long levelId = 0, long levelGeneration = 0)
        {
            if (self.Stage != ProductClientStage.Joined) throw new InvalidOperationException("Product is not joined");
            long command = checked(++self.NextCommandId);
            return new ProductCommandContext(self.MatchId, worldId, worldGeneration, levelId, levelGeneration,
                self.BindingGeneration, command);
        }
    }
}
#endif
