using Game;

namespace ET.Client
{
    [FriendOf(typeof(ProductRoomClient))]
    public static partial class GridPresentationSystem
    {
        private static void RefreshBaselineStatus(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            if (((ProductRuntimeComponent)grid.GetParent<ClientSessionComponent>().Runtime).Config.IsDevelopmentTest)
            {
                view.HintText.text = "测试运行 · 不计正式进度 · " + view.HintText.text;
                return;
            }
            ProductRoomClient room = grid.GetParent<ClientSessionComponent>().GetComponent<ProductRoomClient>();
            if (!string.IsNullOrEmpty(room?.Current.BaselineOwnerId))
            {
                view.HintText.text += " · 来源玩家 " + room.Current.BaselineOwnerId.Substring(0, 8) + " · 修订 " + room.Current.BaselineRevision + "（只读运行）";
            }
        }

        private static void AddRoomControls(this GridPresentationComponent self, MonoUIGridKitchen view, GridClientComponent grid)
        {
            ClientSessionComponent client = grid.GetParent<ClientSessionComponent>();
            if (((ProductRuntimeComponent)client.Runtime).Config.IsDevelopmentTest)
            {
                self.AddDevelopmentTestControls(view, grid);
                return;
            }
            if (IsCooperativeRestaurant(grid))
            {
                self.Root().GetComponent<ProductEntryPresentation>()?.AddClaimInboxControl(view);
                Add("合作进度领取", null);
            }
            ProductRuntimeComponent runtime = client.Runtime;
            if (runtime.Config.Role == RuntimeInstanceRole.Solo)
            {
                Add("返回模式选择", ProductLeaveAction.SoloReturnModes);
            }
            else if (runtime.Config.Role == RuntimeInstanceRole.Client)
            {
                Add("离开房间", ProductLeaveAction.ClientLeave);
            }
            else if (client.GetComponent<ProductRoomClient>()?.Current.CanControlRoom == true)
            {
                Add("返回房间", ProductLeaveAction.HostReturnRoom);
                Add("结束整个房间", ProductLeaveAction.HostEndRoom);
            }

            void Add(string caption, ProductLeaveAction? action)
            {
                EntityRef<GridPresentationComponent> original = self;
                string world = grid.WorldId;
                long generation = grid.WorldGeneration, level = grid.LevelId, levelGeneration = grid.LevelGeneration;
                long binding = client?.BindingGeneration ?? grid.BindingGeneration;
                long epoch = self.HudLoadEpoch;
                view.AddControl(caption, () =>
                {
                    GridPresentationComponent current = original;
                    var activeClient = ((Scene)current?.IScene)?.GetComponent<ClientSessionComponent>();
                    var active = activeClient?.GetComponent<GridClientComponent>();
                    if (current?.Ready != true || current.HudLoadEpoch != epoch || active?.Confirmed != true ||
                        active.WorldId != world || active.WorldGeneration != generation || active.LevelId != level ||
                        active.LevelGeneration != levelGeneration || activeClient.BindingGeneration != binding)
                    {
                        return;
                    }
                    var entry = current.Root().GetComponent<ProductEntryPresentation>();
                    if (action.HasValue)
                    {
                        entry?.RequestProductLeave(action.Value, true);
                    }
                    else
                    {
                        entry?.OpenClaims();
                    }
                });
            }
        }
    }
}
