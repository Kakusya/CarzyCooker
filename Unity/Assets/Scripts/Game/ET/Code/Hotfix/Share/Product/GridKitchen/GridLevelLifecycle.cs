#if !DOTNET
using System.Linq;

namespace ET
{
    [FriendOf(typeof(GridKitchenComponent))]
    [FriendOf(typeof(GridActor))]
    [FriendOf(typeof(GridItem))]
    public static partial class GridKitchenSystem
    {
        public static void CancelLevelIntents(this GridKitchenComponent self, string reason)
        {
            self.CancelSupplyCommands(reason);
            foreach (GridActor actor in self.OrderedActors())
            {
                if (self.Incoming.TryGetValue(actor.MemberId, out var incoming)) self.Result(actor, incoming.CommandId, GridResultStatus.Cancelled, reason, incoming.OriginKind);
                if (actor.HasIntent) self.Result(actor, actor.ActiveIntent.CommandId, GridResultStatus.Cancelled, reason, actor.ActiveIntent.OriginKind);
                actor.HasIntent = false; actor.ActiveIntent = default; actor.Path.Clear(); actor.PathIndex = 0;
                GridCell stop = actor.Motion == GridMotion.Transit ? actor.NextCell : actor.Cell;
                actor.Goal = stop; actor.StopCell = stop; actor.HasStopReservation = true;
                if (actor.Motion != GridMotion.Transit) actor.Motion = GridMotion.Standing;
            }
            self.Incoming.Clear(); self.Dirty = true;
        }

        public static void PauseLevelWorkers(this GridKitchenComponent self)
        {
            foreach (GridActor actor in self.OrderedActors()) self.PauseWork(actor);
        }

        public static void RetireLevelActors(this GridKitchenComponent self)
        {
            self.CancelLevelIntents("LevelChanged");
            foreach (GridActor actor in self.OrderedActors())
            {
                self.PauseWork(actor);
                GridItem held = self.Item(actor.HandItemId);
                if (held != null) { held.Location = GridItemLocation.Ground; held.HolderId = 0; held.Cell = actor.Cell; actor.HandItemId = 0; }
                actor.Dispose();
            }
            self.Actors.Clear(); self.Dirty = true;
        }
    }
}
#endif
