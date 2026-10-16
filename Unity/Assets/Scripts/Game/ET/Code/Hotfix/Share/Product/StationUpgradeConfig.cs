#if !DOTNET
using System;
using System.Linq;

namespace ET
{
    public static class StationUpgradeConfig
    {
        public static void ValidateAll()
        {
            var rows = Tables.Instance.DTStationUpgradeConfig.DataList;
            if (rows.Select(r => r.Id).Distinct().Count() != rows.Count) throw new InvalidOperationException("DuplicateStationUpgrade");
            foreach (var row in rows)
            {
                GridStationCapability capability = GridStationCapability.None;
                switch (row.WorkKind)
                {
                    case StationWorkKind.Batch:
                        foreach (var recipe in Tables.Instance.DTGridAutoRecipeConfig.DataList)
                        {
                            capability |= (GridStationCapability)recipe.StationCapability;
                        }
                        break;
                    case StationWorkKind.Chop:
                        capability = GridStationCapability.Chop;
                        break;
                    case StationWorkKind.Bake:
                        foreach (var recipe in Tables.Instance.DTGridBakeRecipeConfig.DataList)
                        {
                            capability |= (GridStationCapability)recipe.StationCapability;
                        }
                        break;
                }
                var requested = (GridStationCapability)row.StationCapability;
                if (row.Id <= 0 || string.IsNullOrWhiteSpace(row.Name) || row.TargetLevel != 1 || requested == GridStationCapability.None ||
                    (capability & requested) != requested ||
                    row.DurationNumerator <= 0 || row.DurationDenominator <= row.DurationNumerator ||
                    !Tables.Instance.DTStationConfig.DataList.Any(s => s.Kind == row.StationKind && (s.Capabilities & row.StationCapability) == row.StationCapability))
                    throw new InvalidOperationException("InvalidStationUpgrade:" + row.Id);
            }
        }

        public static bool Matches(DRStationUpgradeConfig upgrade, GridStationKind kind, GridStationCapability capabilities)
            => upgrade != null && upgrade.StationKind == kind && (capabilities & (GridStationCapability)upgrade.StationCapability) == (GridStationCapability)upgrade.StationCapability;

        public static int CombinedTicks(int basis, int buffNumerator, int buffDenominator, int upgradeNumerator, int upgradeDenominator)
        {
            if (basis <= 0 || buffNumerator <= 0 || buffDenominator <= 0 || upgradeNumerator <= 0 || upgradeDenominator <= 0)
                throw new InvalidOperationException("InvalidWorkDuration");
            decimal ticks = (decimal)basis * buffNumerator * upgradeNumerator / ((decimal)buffDenominator * upgradeDenominator);
            return checked((int)Math.Max(1m, decimal.Ceiling(ticks)));
        }
    }
}
#endif
