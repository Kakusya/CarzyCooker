#if !DOTNET
using System;
using System.Linq;
namespace ET
{
    [EntitySystemOf(typeof(LevelBuff))]
    [FriendOf(typeof(LevelBuff))]
    public static partial class LevelBuffSystem
    {
        [EntitySystem]
        private static void Awake(this LevelBuff self,DRLevelConfig definition,int seed)
        {
            BuffConfig.ValidateLevel(definition);
            self.Seed=seed;self.DrawVersion=0;self.SelectedId=0;self.RequireChoice=definition.RequireBuffChoice;
            self.AllowedIds = definition.AllowedBuffIds.ToArray(); self.CandidateCount = definition.CandidateCount;
            self.Candidates=Array.Empty<int>();self.Choices.Clear();
            self.Preparations.Clear();
            self.PreparationSerial = 1; self.Prepared = self.PreparationQueued = self.NotifyPrepared = false;
        }
    }
    [FriendOf(typeof(LevelBuff))]
    [FriendOf(typeof(HostPeer))]
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(Level))]
    [FriendOf(typeof(Match))]
    public static class BuffCommands
    {
        public static string QueueBuffChoice(this GridKitchenComponent kitchen,HostPeer peer,ProductCommandContext context,BuffChoiceIntent intent)
        {
            Level level=kitchen.ProductLevel();Match match=kitchen.Match;
            if(level==null || kitchen.Stopped || peer==null || !kitchen.HasBinding(peer.MemberId,peer.Generation))return "EntryClosed";
            if(!peer.CanControlRestaurant())return "HostControlOnly";
            string error=context.Validate(match.MatchId,kitchen.WorldId,kitchen.WorldGeneration,level.Id,level.Generation,peer.Generation);
            if(error!=null)return error;
            error=peer.CheckProductCommand(context,ProductCommandKind.Buff,intent.Equals(peer.LastBuffCommand),out bool replay);
            if(error!=null || replay)return error;
            if(level.Paused)return "Paused";
            if(level.Phase!=LevelPhase.Preparing)return "WrongPhase";
            peer.RecordProductCommand(context,ProductCommandKind.Buff);peer.LastBuffCommand=intent;
            level.GetComponent<LevelBuff>().Choices.Enqueue(new BuffChoiceEnvelope(context,intent,peer));return null;
        }
        public static void AdvanceBuffChoices(this GridKitchenComponent kitchen,Level level)
        {
            LevelBuff buff=level.GetComponent<LevelBuff>();
            while(buff.Choices.Count>0)
            {
                BuffChoiceEnvelope command=buff.Choices.Dequeue();HostPeer peer=command.Peer;
                string error=peer==null || !peer.EntryOpen || !kitchen.HasBinding(peer.MemberId,command.Context.BindingGeneration)?"EntryClosed"
                    :command.Context.Validate(((Match)kitchen.Match).MatchId,kitchen.WorldId,kitchen.WorldGeneration,level.Id,level.Generation,peer.Generation);
                if(error==null && !peer.CanControlRestaurant())error="HostControlOnly";
                if(error==null)error=level.Paused?"Paused":level.Phase!=LevelPhase.Preparing?"WrongPhase"
                    :!buff.Prepared?"BuffNotReady":command.Intent.DrawVersion!=buff.DrawVersion?"DrawChanged":buff.SelectedId!=0?"BuffLocked"
                    :!buff.Candidates.Contains(command.Intent.ChoiceId)?"InvalidBuffChoice":null;
                if(error==null)buff.SelectedId=command.Intent.ChoiceId;
                peer?.RecordProductResult(command.Context, ProductCommandKind.Buff,error==null?GridResultStatus.Executed:GridResultStatus.Rejected,error);kitchen.Dirty=true;
            }
        }
        public static void CancelBuffChoices(this GridKitchenComponent kitchen,Level level,string reason)
        {
            LevelBuff buff=level.GetComponent<LevelBuff>();
            while(buff.Choices.Count>0){BuffChoiceEnvelope command=buff.Choices.Dequeue();HostPeer peer=command.Peer;peer?.RecordProductResult(command.Context, ProductCommandKind.Buff,GridResultStatus.Cancelled,reason);}
            kitchen.Dirty=true;
        }
    }
    [FriendOf(typeof(LevelBuff))]
    public static partial class GridKitchenSystem
    {
        private static int FrozenCookingTicks(this GridKitchenComponent kitchen,DRGridAutoRecipeConfig recipe)
        {
            LevelBuff buff=kitchen.ProductLevel()?.GetComponent<LevelBuff>();
            DRBuffConfig choice=buff==null || buff.SelectedId==0?null:Tables.Instance.DTBuffConfig.Get(buff.SelectedId);
            return choice==null || choice.BatchRecipeId!=recipe.Id?recipe.RequiredTicks:BuffConfig.ScaleTicks(recipe.RequiredTicks,choice.DurationNumerator,choice.DurationDenominator);
        }
    }
    [FriendOf(typeof(LevelBuff))]
    public static partial class RestaurantRuntimeSystem
    {
        private static void AdvanceBuffPreparation(this RestaurantRuntime self, Level level)
        {
            if (level.Paused || level.Phase != LevelPhase.Preparing) return;
            LevelBuff buff = level.GetComponent<LevelBuff>(); GridKitchenComponent kitchen = self.Kitchen;
            while (buff.Preparations.Count > 0)
            {
                ProductTransitionIdentity identity = buff.Preparations.Dequeue();
                if (identity.LevelId != level.Id || identity.LevelGeneration != level.Generation || identity.SourceId != buff.Id ||
                    identity.SourceInstanceId != buff.InstanceId || identity.Serial != buff.PreparationSerial || !buff.PreparationQueued || buff.Prepared) continue;
                buff.Candidates = BuffConfig.Draw(buff.AllowedIds, buff.CandidateCount, buff.Seed);
                buff.DrawVersion = 1; buff.Prepared = true; buff.PreparationQueued = false; ++buff.PreparationSerial;
                buff.NotifyPrepared = true; kitchen.Dirty = true;
            }
            if (buff.Prepared || buff.PreparationQueued) return;
            buff.PreparationQueued = true;
            self.Root().GetComponent<ProcessInnerSender>().Send(((Scene)self.IScene).GetComponent<HostSessionComponent>().GetActorId(), ProductMessages.Create<BuffPrepare>(value =>
            {
                value.ProtocolVersion = ProductSessionRules.ProtocolVersion; value.MatchId = ((Match)self.Match).MatchId;
                value.WorldId = kitchen.WorldId; value.WorldGeneration = kitchen.WorldGeneration; value.LevelId = level.Id; value.LevelGeneration = level.Generation;
                value.SourceId = buff.Id; value.SourceInstanceId = buff.InstanceId; value.TransitionSerial = buff.PreparationSerial;
            }));
        }
        static partial void CheckStartAdditions(this RestaurantRuntime self,Level level,ref string error)
        {
            LevelBuff buff=level.GetComponent<LevelBuff>();
            if(!buff.Prepared)error="BuffNotReady";
            else if(buff.RequireChoice && buff.SelectedId==0)error="BuffChoiceRequired";
        }
    }
}
#endif
