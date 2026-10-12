#if !DOTNET
using System.Linq;
using Cysharp.Threading.Tasks;

namespace ET
{
    [FriendOf(typeof(CompanionRole))]
    [FriendOf(typeof(FrontWork))]
    public static partial class GridKitchenSystem
    {
        public static void ScheduleCompanions(this GridKitchenComponent self, Level level)
        {
            if (level.Phase != LevelPhase.Operating && level.Phase != LevelPhase.Draining) return;
            foreach (CompanionRole role in level.Children.Values.OfType<CompanionRole>())
            {
                if (role.WorkId != 0 || role.RequestInFlight || role.Motion != GridMotion.Standing ||
                    role.LastSubmittedRequestId > role.LastCommittedRequestId || self.Tick < role.NextScheduleTick) continue;
                role.NextScheduleTick = self.Tick + 1;
                FrontWork candidate = level.Children.Values.OfType<FrontWork>().Where(work => work.State == FrontWorkState.Available &&
                        work.Kind is FrontWorkKind.Ask or FrontWorkKind.Wash)
                    .OrderBy(work => work.Kind == FrontWorkKind.Ask ? 0 : 1).ThenBy(work => work.Id)
                    .FirstOrDefault(work => self.CheckFrontTarget(level, work) == null &&
                        !level.Children.Values.OfType<FrontWork>().Any(other => other.StationId == work.StationId && other.State is FrontWorkState.Approaching or FrontWorkState.Working) &&
                        self.Station(work.StationId).StationApproaches().Any(cell => self.FindGridPath(role.Cell, cell) != null));
                if (candidate != null) role.RequestCompanionWork(new FrontWorkIntent(FrontWorkAction.Claim, candidate.Id, candidate.ClaimSerial)).Forget();
            }
        }
    }
}
#endif
