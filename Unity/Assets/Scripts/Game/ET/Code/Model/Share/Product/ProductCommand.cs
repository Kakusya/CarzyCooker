#if !DOTNET
using System;

namespace ET
{
    public enum ProductCommandKind { Grid = 0, Level = 1, Layout = 2, FrontWork = 3, Serve = 4, Buff = 5, Supply = 6, Renovation = 7, Room = 8, Baseline = 9, CompanionPreference = 10, OrderIntent = 11, TargetPing = 12, SupplyRequest = 13 }

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
