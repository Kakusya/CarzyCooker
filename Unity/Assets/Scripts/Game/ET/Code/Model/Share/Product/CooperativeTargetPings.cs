#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    public enum TargetPingAction { Set = 1, Clear = 2 }
    public enum TargetPingKind { Station = 1, Item = 2, Table = 3 }

    public readonly struct TargetPingValue
    {
        public readonly int MemberId;
        public readonly long BindingGeneration, TargetId, Serial, ExpiresTick;
        public readonly TargetPingKind Kind;
        public TargetPingValue(int member, long binding, TargetPingKind kind, long target, long serial, long expiry)
        {
            MemberId = member; BindingGeneration = binding; Kind = kind; TargetId = target; Serial = serial; ExpiresTick = expiry;
        }
    }

    public readonly struct TargetPingCommand : System.IEquatable<TargetPingCommand>
    {
        public readonly TargetPingAction Action;
        public readonly TargetPingKind Kind;
        public readonly long TargetId;
        public TargetPingCommand(TargetPingAction action, TargetPingKind kind, long target)
        {
            Action = action; Kind = kind; TargetId = target;
        }
        public bool Equals(TargetPingCommand other) => Action == other.Action && Kind == other.Kind && TargetId == other.TargetId;
    }

    [ComponentOf(typeof(Level))]
    public sealed class CooperativeTargetPings : Entity, IAwake
    {
        public long NextSerial;
        public readonly Dictionary<int, TargetPingValue> Members = new();
    }
}
#endif
