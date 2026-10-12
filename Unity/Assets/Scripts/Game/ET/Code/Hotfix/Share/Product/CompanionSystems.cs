#if !DOTNET
using System.Linq;

namespace ET
{
    [FriendOf(typeof(Companion))]
    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(CompanionGrowth))]
    public static partial class RestaurantRuntimeSystem
    {
        static partial void InstallLevelAdditions(this RestaurantRuntime self, Level level)
        {
            GridKitchenComponent kitchen = self.Kitchen;
            kitchen.InstallFrontTables();
            level.AddComponent<LevelBuff,DRLevelConfig,int>(Tables.Instance.DTLevelConfig.Get(level.ConfigId),System.Guid.NewGuid().GetHashCode());
            level.AddComponent<FrontLevelState, DRCustomerFlowConfig>(Tables.Instance.DTCustomerFlowConfig.DataList.Single(value => value.LevelConfigId == level.ConfigId));
            level.AddComponent<CompanionGrowth>();
            level.AddComponent<FrontWorkCommands>();
            foreach (DRCompanionConfig definition in Tables.Instance.DTCompanionConfig.DataList)
            {
                Companion companion = self.Children.Values.OfType<Companion>().SingleOrDefault(value => value.DefinitionId == definition.Id)
                    ?? self.AddChild<Companion, DRCompanionConfig>(definition);
                level.AddChild<CompanionRole, Companion, long>(companion, level.Generation);
            }
        }

        static partial void CheckNextAdditions(this RestaurantRuntime self, DRLevelConfig next, ref string error)
        {
            GridKitchenComponent kitchen = self.Kitchen;
            if (Tables.Instance.DTCompanionConfig.DataList.Any(row => !kitchen.IsWalkable(new GridCell(row.SpawnX, row.SpawnY))))
                error = "InvalidCompanionSpawn";
            if(error==null)
            {
                try{BuffConfig.ValidateLevel(next);}catch(System.InvalidOperationException){error="InvalidBuffConfig";}
            }
        }

        static partial void AdvanceFront(this RestaurantRuntime self, Level level)
        {
            GridKitchenComponent kitchen = self.Kitchen;
            kitchen.AdvanceCustomers(level);
            kitchen.AdvanceServes(level);
            kitchen.AdvanceFrontWork(level);
            kitchen.ScheduleCompanions(level);
        }
        static partial void PauseFront(this RestaurantRuntime self, Level level)
        {
            ((GridKitchenComponent)self.Kitchen).PauseFrontWork(level);
            ((GridKitchenComponent)self.Kitchen).CancelPendingServes(level, "Paused");
            ((GridKitchenComponent)self.Kitchen).CancelBuffChoices(level, "Paused");
        }
    }

    [EntitySystemOf(typeof(Companion))]
    [FriendOf(typeof(Companion))]
    public static partial class CompanionSystem
    {
        [EntitySystem]
        private static void Awake(this Companion self, DRCompanionConfig definition)
        { self.DefinitionId = definition.Id; self.Name = definition.Name; self.Post = (CompanionPost)definition.Post; }
    }

    [EntitySystemOf(typeof(CompanionRole))]
    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(Companion))]
    public static partial class CompanionRoleSystem
    {
        [EntitySystem]
        private static void Awake(this CompanionRole self, Companion companion, long generation)
        {
            DRCompanionConfig definition = Tables.Instance.DTCompanionConfig.Get(companion.DefinitionId);
            self.Companion = companion; self.LevelGeneration = generation;
            self.Cell = self.NextCell = new GridCell(definition.SpawnX, definition.SpawnY);
            self.Motion = GridMotion.Standing; self.StepStartTick = 0; self.Path.Clear(); self.PathIndex = 0;
            self.WorkId = 0; self.ClaimSerial = 0; self.RequestInFlight = false;
            self.NextRequestId = 0; self.LastSubmittedRequestId = 0; self.LastCommittedRequestId = 0; self.LastWorkReason = null;
            self.LastSubmittedIntent = default; self.LastResultStatus = default; self.LastNotificationKind = 0;
            self.NextScheduleTick = 0;
            self.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.OrderedMessage);
            self.GetAsyncOwner();
        }
    }

    [EntitySystemOf(typeof(CompanionGrowth))]
    [FriendOf(typeof(CompanionGrowth))]
    public static partial class CompanionGrowthSystem
    {
        [EntitySystem]
        private static void Awake(this CompanionGrowth self) => self.CompletedByCompanion.Clear();
    }

    [EntitySystemOf(typeof(FrontWork))]
    [FriendOf(typeof(FrontWork))]
    public static partial class FrontWorkSystem
    {
        [EntitySystem]
        private static void Awake(this FrontWork self, FrontWorkDefinition definition, long generation)
        {
            self.Kind = definition.Kind; self.LevelGeneration = generation; self.CustomerId = definition.CustomerId;
            self.StationId = definition.StationId; self.ItemId = definition.ItemId; self.BaseTicks = definition.BaseTicks;
            self.State = FrontWorkState.Available; self.Progress = 0; self.WorkerKind = FrontWorkerKind.None;
            self.WorkerId = 0; self.WorkerInstanceId = 0; self.WorkerGeneration = 0; self.ClaimSerial = 0;
            self.FrozenTicks = 0; self.OperationCell = default; self.StartedTick = 0;
            self.WorkerCommandId = 0;
        }
    }

    [EntitySystemOf(typeof(FrontWorkCommands))]
    [FriendOf(typeof(FrontWorkCommands))]
    public static partial class FrontWorkCommandsSystem
    {
        [EntitySystem]
        private static void Awake(this FrontWorkCommands self) => self.Pending.Clear();
    }
}
#endif
