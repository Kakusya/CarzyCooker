#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;
namespace ET
{
    public static class BuffConfig
    {
        public static int ScaleTicks(int ticks,int numerator,int denominator)
        {
            if(ticks<=0 || numerator<=0 || denominator<=0)throw new InvalidOperationException("Invalid buff duration");
            return checked((int)Math.Max(1,((long)ticks*numerator+denominator-1)/denominator));
        }
        public static void ValidateAll()
        {
            var definitions=Tables.Instance.DTBuffConfig.DataList;
            if(definitions.Select(value=>value.Id).Distinct().Count()!=definitions.Count)throw new InvalidOperationException("Duplicate buff");
            foreach(DRBuffConfig row in definitions)
            {
                DRGridAutoRecipeConfig recipe=Tables.Instance.DTGridAutoRecipeConfig.GetOrDefault(row.BatchRecipeId);
                if(row.Id<=0 || string.IsNullOrWhiteSpace(row.Name) || row.EffectKind!=1 || row.DurationNumerator<=0 || row.DurationDenominator<=0 || recipe==null)
                    throw new InvalidOperationException("Invalid buff definition: "+row.Id);
                ScaleTicks(recipe.RequiredTicks,row.DurationNumerator,row.DurationDenominator);
            }
            foreach(DRLevelConfig level in Tables.Instance.DTLevelConfig.DataList)ValidateLevel(level);
        }
        public static void ValidateLevel(DRLevelConfig level)
        {
            if(level.CandidateCount<0 || level.AllowedBuffIds.Distinct().Count()!=level.AllowedBuffIds.Count ||
                level.AllowedBuffIds.Any(id=>Tables.Instance.DTBuffConfig.GetOrDefault(id)==null) ||
                level.RequireBuffChoice && (level.AllowedBuffIds.Count==0 || level.CandidateCount==0))
                throw new InvalidOperationException("Invalid level buff pool: "+level.Id);
        }
        public static int[] Draw(IEnumerable<int> allowed,int count,int seed)
        {
            int[] pool=allowed.ToArray();
            if(count<0 || pool.Distinct().Count()!=pool.Length)throw new InvalidOperationException("Invalid buff draw");
            var random=new System.Random(seed);int take=Math.Min(count,pool.Length);
            for(int i=0;i<take;++i){int other=random.Next(i,pool.Length);(pool[i],pool[other])=(pool[other],pool[i]);}
            return pool.Take(take).ToArray();
        }
    }
}
#endif
