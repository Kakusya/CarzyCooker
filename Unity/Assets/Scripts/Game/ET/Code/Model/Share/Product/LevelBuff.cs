#if !DOTNET
using System;
using System.Collections.Generic;
namespace ET
{
    public readonly struct BuffChoiceIntent : IEquatable<BuffChoiceIntent>
    {
        public readonly long DrawVersion;
        public readonly int ChoiceId;
        public BuffChoiceIntent(long draw,int choice){DrawVersion=draw;ChoiceId=choice;}
        public bool Equals(BuffChoiceIntent other)=>DrawVersion==other.DrawVersion && ChoiceId==other.ChoiceId;
        public override bool Equals(object other)=>other is BuffChoiceIntent value && Equals(value);
        public override int GetHashCode()=>HashCode.Combine(DrawVersion,ChoiceId);
    }
    public readonly struct BuffChoiceEnvelope
    {
        public readonly ProductCommandContext Context;
        public readonly BuffChoiceIntent Intent;
        public readonly EntityRef<HostPeer> Peer;
        public BuffChoiceEnvelope(ProductCommandContext context,BuffChoiceIntent intent,HostPeer peer){Context=context;Intent=intent;Peer=peer;}
    }
    [ComponentOf(typeof(Level))]
    public sealed class LevelBuff : Entity, IAwake<DRLevelConfig,int>
    {
        public int Seed;
        public long DrawVersion;
        public int SelectedId;
        public bool RequireChoice;
        public int[] Candidates;
        public int[] AllowedIds;
        public int CandidateCount;
        public long PreparationSerial;
        public bool Prepared, PreparationQueued, NotifyPrepared;
        public readonly Queue<BuffChoiceEnvelope> Choices=new();
        public readonly Queue<ProductTransitionIdentity> Preparations = new();
    }
}
#endif
