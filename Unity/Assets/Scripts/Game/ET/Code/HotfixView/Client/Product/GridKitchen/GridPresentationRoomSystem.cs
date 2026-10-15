using System;
using Game;

namespace ET.Client
{
    [FriendOf(typeof(ProductRoomClient))]
    public static partial class GridPresentationSystem
    {
        private static void RefreshBaselineStatus(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            ProductRoomClient room = grid.GetParent<ClientSessionComponent>().GetComponent<ProductRoomClient>();
            if (!string.IsNullOrEmpty(room?.Current.BaselineOwnerId))
            {
                view.HintText.text += " · 来源玩家 " + room.Current.BaselineOwnerId.Substring(0, 8) + " · 修订 " + room.Current.BaselineRevision + "（只读运行）";
            }
        }

        private static void AddRoomControls(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (IsCooperativeRestaurant(grid))
            {
                EntityRef<GridPresentationComponent> presentation = self;
                string currentWorld = grid.WorldId;
                view.AddControl("合作进度领取", () =>
                {
                    GridPresentationComponent active = presentation;
                    var current = ((Scene)active?.IScene)?.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
                    if (active?.Ready == true && current?.WorldId == currentWorld)
                    {
                        active.Root().GetComponent<ProductEntryPresentation>()?.OpenClaims();
                    }
                });
            }
            if (grid.LevelPhase == LevelPhase.Completed && Tables.Instance.DTLevelConfig.Get(grid.LevelConfigId).IsChapterTerminal)
            {
                return;
            }
            ClientSessionComponent client = grid.GetParent<ClientSessionComponent>();
            ProductRoomClient room = client.GetComponent<ProductRoomClient>();
            if (room == null || !room.Current.CanControlRoom || room.Current.WorldId != grid.WorldId)
            {
                return;
            }
            if (self.RoomControlPending)
            {
                view.StatusText.text = "正在返回房间…";
                return;
            }
            EntityRef<GridPresentationComponent> original = self;
            long version = room.Current.Version;
            string world = grid.WorldId;
            view.AddControl("返回房间", () =>
            {
                GridPresentationComponent current = original;
                current?.CloseRoomRestaurant(version, world);
            });
        }

        private static void CloseRoomRestaurant(this GridPresentationComponent self, long expectedVersion, string expectedWorld)
        {
            var owner = self.GridCommandOwner();
            if (owner == null || self.RoomControlPending)
            {
                return;
            }
            ClientSessionComponent client = ((Scene)self.IScene).GetComponent<ClientSessionComponent>();
            ProductRoomClient room = client.GetComponent<ProductRoomClient>();
            if (room?.Current.Version != expectedVersion || room.Current.WorldId != expectedWorld || !room.Current.CanControlRoom)
            {
                return;
            }
            self.RoomControlPending = true;
            self.RefreshGridHud();
            long epoch = self.HudLoadEpoch;
            EntityRef<GridPresentationComponent> original = self;
            owner.Run(async context =>
            {
                try
                {
                    string error = await client.CallProductValueAsync(context,
                        client.CreateRoomCommand(ProductRoomAction.CloseRestaurant, room.Current.MenuVariant),
                        response => response.Error == 0 ? null : response.Message);
                    context.ThrowIfStopped();
                    self = original;
                    if (self != null && self.HudLoadEpoch == epoch && error != null)
                    {
                        self.Failure = error;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception exception)
                {
                    self = original;
                    if (self != null && self.HudLoadEpoch == epoch)
                    {
                        self.Failure = exception.Message;
                    }
                }
                finally
                {
                    self = original;
                    if (self != null && self.HudLoadEpoch == epoch)
                    {
                        self.RoomControlPending = false;
                        self.RefreshGridHud();
                    }
                }
            });
        }
    }
}
