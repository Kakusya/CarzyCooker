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
            self.Seed=seed;self.DrawVersion=1;self.SelectedId=0;self.RequireChoice=definition.RequireBuffChoice;
            self.Candidates=BuffConfig.Draw(definition.AllowedBuffIds,definition.CandidateCount,seed);self.Choices.Clear();
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
                    :command.Intent.DrawVersion!=buff.DrawVersion?"DrawChanged":buff.SelectedId!=0?"BuffLocked"
                    :!buff.Candidates.Contains(command.Intent.ChoiceId)?"InvalidBuffChoice":null;
                if(error==null)buff.SelectedId=command.Intent.ChoiceId;
                peer?.RecordProductResult(command.Context.CommandId,error==null?GridResultStatus.Executed:GridResultStatus.Rejected,error);kitchen.Dirty=true;
            }
        }
        public static void CancelBuffChoices(this GridKitchenComponent kitchen,Level level,string reason)
        {
            LevelBuff buff=level.GetComponent<LevelBuff>();
            while(buff.Choices.Count>0){BuffChoiceEnvelope command=buff.Choices.Dequeue();HostPeer peer=command.Peer;peer?.RecordProductResult(command.Context.CommandId,GridResultStatus.Cancelled,reason);}
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
        static partial void CheckStartAdditions(this RestaurantRuntime self,Level level,ref string error)
        {
            LevelBuff buff=level.GetComponent<LevelBuff>();
            if(buff.RequireChoice && buff.SelectedId==0)error="BuffChoiceRequired";
        }
    }
}
#endif
