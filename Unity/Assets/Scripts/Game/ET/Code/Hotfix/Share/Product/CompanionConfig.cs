#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public static class CompanionConfig
    {
        public static void Validate(IEnumerable<DRCompanionConfig> definitions, Func<GridCell, bool> canSpawn)
        {
            DRCompanionConfig[] rows = definitions.ToArray();
            if (rows.Length != 1 || rows.Select(row => row.Id).Distinct().Count() != rows.Length)
                throw new InvalidOperationException("Prototype requires one fixed companion");
            foreach (DRCompanionConfig row in rows)
                if (row.Id <= 0 || string.IsNullOrWhiteSpace(row.Name) || row.Post != CompanionPost.FrontService ||
                    row.AskTicks <= 0 || row.ClearTicks <= 0 || row.WashTicks <= 0 || !canSpawn(new GridCell(row.SpawnX, row.SpawnY)))
                    throw new InvalidOperationException($"Invalid companion definition {row.Id}: identity / post / duration / spawn");
        }
    }
}
#endif
