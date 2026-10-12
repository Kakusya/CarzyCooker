#if !DOTNET
using System;

namespace ET
{
    public enum ProductCommandKind { Grid, Level, Layout, FrontWork, Serve, Buff }

    // 值身份，不持 Entity、消息或任何可写载荷。
    public readonly struct ProductCommandContext : IEquatable<ProductCommandContext>
    {
        public readonly string MatchId, WorldId;
        public readonly long WorldGeneration, LevelId, LevelGeneration, BindingGeneration, CommandId;

        public ProductCommandContext(string match, string world, long worldGeneration,
            long levelId, long levelGeneration, long binding, long command)
        {
            MatchId = match; WorldId = world; WorldGeneration = worldGeneration;
            LevelId = levelId; LevelGeneration = levelGeneration;
            BindingGeneration = binding; CommandId = command;
        }

        public string Validate(string match, string world, long worldGeneration, long levelId, long levelGeneration, long binding)
        {
            if (CommandId <= 0) return "InvalidCommand";
            if (MatchId != match) return "WrongMatch";
            if (WorldId != world || WorldGeneration != worldGeneration) return "WrongWorld";
            if (LevelId != levelId || LevelGeneration != levelGeneration) return "WrongLevel";
            if (BindingGeneration != binding) return "EntryClosed";
            return null;
        }

        public bool Equals(ProductCommandContext other)
            => MatchId == other.MatchId && WorldId == other.WorldId && WorldGeneration == other.WorldGeneration
                && LevelId == other.LevelId && LevelGeneration == other.LevelGeneration
                && BindingGeneration == other.BindingGeneration && CommandId == other.CommandId;
    }
}
#endif
