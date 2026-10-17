#if !DOTNET
using Cysharp.Threading.Tasks;

namespace ET
{
    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(FrontWork))]
    [FriendOf(typeof(CompanionPreferenceState))]
    public static partial class GridKitchenSystem
    {
        public static void ScheduleCompanions(this GridKitchenComponent self, Level level)
        {
            if (level.Phase != LevelPhase.Operating && level.Phase != LevelPhase.Draining) return;
            FrontWorkCommands commands=level.GetComponent<FrontWorkCommands>();
            commands.ScheduleRoles.Clear();commands.ScheduleCandidates.Clear();
            foreach(Entity child in level.Children.Values)
            {
                if(child is CompanionRole role)
                {
                    commands.ScheduleRoles.Add(role);
                }
                if(child is FrontWork work && work.State==FrontWorkState.Available && work.Kind is FrontWorkKind.Ask or FrontWorkKind.Wash)
                {
                    commands.ScheduleCandidates.Add(work);
                }
            }
            CompanionWorkPreference preference = level.GetParent<RestaurantRuntime>().GetComponent<CompanionPreferenceState>().Value;
            if (preference == CompanionWorkPreference.WashFirst)
            {
                commands.ScheduleCandidates.Sort(static (left, right) => CompareCompanionCandidate(left, right, FrontWorkKind.Wash));
            }
            else
            {
                commands.ScheduleCandidates.Sort(static (left, right) => CompareCompanionCandidate(left, right, FrontWorkKind.Ask));
            }
            foreach (var reference in commands.ScheduleRoles)
            {
                CompanionRole role=reference;
                if(role==null)
                {
                    continue;
                }
                if (role.WorkId != 0 || role.RequestInFlight || role.Motion != GridMotion.Standing ||
                    role.LastSubmittedRequestId > role.LastCommittedRequestId || self.Tick < role.NextScheduleTick) continue;
                role.NextScheduleTick = self.Tick + 1;
                FrontWork candidate=null;
                foreach(var workRef in commands.ScheduleCandidates)
                {
                    FrontWork work=workRef;
                    if(work==null || work.State!=FrontWorkState.Available || self.CheckFrontTarget(level,work)!=null)
                    {
                        continue;
                    }
                    bool busy=false;
                    foreach(Entity child in level.Children.Values)
                    {
                        if(child is FrontWork other && other.StationId==work.StationId && other.State is FrontWorkState.Approaching or FrontWorkState.Working)
                        {
                            busy=true;break;
                        }
                    }
                    if(busy)
                    {
                        continue;
                    }
                    foreach(GridCell cell in self.Station(work.StationId).StationApproaches())
                    {
                        if(self.SearchGridPath(role.Cell,cell))
                        {
                            candidate=work;break;
                        }
                    }
                    if(candidate!=null)
                    {
                        break;
                    }
                }
                if (candidate != null) role.RequestCompanionWork(new FrontWorkIntent(FrontWorkAction.Claim, candidate.Id, candidate.ClaimSerial)).Forget();
            }
            commands.ScheduleRoles.Clear();commands.ScheduleCandidates.Clear();
        }

        private static int CompareCompanionCandidate(EntityRef<FrontWork> left, EntityRef<FrontWork> right, FrontWorkKind preferred)
        {
            FrontWork first = left, second = right;
            int priority = (first.Kind == preferred ? 0 : 1).CompareTo(second.Kind == preferred ? 0 : 1);
            return priority != 0 ? priority : first.Id.CompareTo(second.Id);
        }
    }
}
#endif
