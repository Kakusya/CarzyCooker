#if !DOTNET
using Cysharp.Threading.Tasks;

namespace ET
{
    [EntitySystemOf(typeof(RestaurantNoticesComponent))]
    [FriendOf(typeof(RestaurantNoticesComponent))]
    [FriendOf(typeof(GridClientComponent))]
    [FriendOf(typeof(SoloSaveRepository))]
    public static partial class RestaurantNoticesSystem
    {
        public const int MaximumNotices = 8;

        [EntitySystem]
        private static void Awake(this RestaurantNoticesComponent self) => self.ResetNotices();
        [EntitySystem]
        private static void Destroy(this RestaurantNoticesComponent self) => self.ResetNotices();

        private static void ResetNotices(this RestaurantNoticesComponent self)
        {
            self.WorldId = null;
            self.WorldGeneration = self.BindingGeneration = self.LevelId = self.LevelGeneration = 0;
            self.LastTick = -1;
            self.KitchenSerial = self.ShipmentSerial = self.NextNotice = self.Version = 0;
            self.BaselineReady = false;
            self.Enabled = ProductNoticeOptions.All;
            self.DeadlineSeconds = 5;
            self.Entries.Clear();
            self.InitialFinishedWorks.Clear();
            self.InitialReadyShipments.Clear();
            self.FinishedWorks.Clear();
            self.ArrivedShipments.Clear();
            self.ObservedDeadlines.Clear();
        }

        private static ProductNoticeOptions Option(RestaurantNoticeKind kind) => kind switch
        {
            RestaurantNoticeKind.Arrival => ProductNoticeOptions.Arrivals,
            RestaurantNoticeKind.Processing => ProductNoticeOptions.Processing,
            _ => ProductNoticeOptions.Deadlines
        };

        private static void Changed(this RestaurantNoticesComponent self)
        {
            ++self.Version;
            EventSystem.Instance.Publish((Scene)self.IScene, new RestaurantNoticesChanged { Version = self.Version });
        }

        private static bool Current(this RestaurantNoticesComponent self, GridClientComponent grid) => grid?.Confirmed == true && self.BaselineReady &&
            self.WorldId == grid.WorldId && self.WorldGeneration == grid.WorldGeneration && self.BindingGeneration == grid.BindingGeneration;

        private static bool NearDeadline(this RestaurantNoticesComponent self, GridClientComponent grid, GridOrderProjection order) =>
            order.State == OrderState.Waiting && (order.DeadlineTick - grid.Tick) * grid.TickMilliseconds <= self.DeadlineSeconds * 1000L;

        private static void BaselineDeadlines(this RestaurantNoticesComponent self, GridClientComponent grid)
        {
            foreach (var order in grid.Orders.Values)
            {
                if (self.NearDeadline(grid, order))
                {
                    self.ObservedDeadlines.Add(order.Id);
                }
            }
        }

        public static void ObserveNoticePreferences(this RestaurantNoticesComponent self, GridClientComponent grid)
        {
            if (!self.Current(grid))
            {
                return;
            }
            var preferences = self.Root().GetComponent<SoloSaveRepository>()?.Preferences;
            var enabled = preferences?.Notices ?? ProductNoticeOptions.All;
            int seconds = preferences?.DeadlineSeconds ?? 5;
            if (enabled == self.Enabled && seconds == self.DeadlineSeconds)
            {
                return;
            }
            self.Enabled = enabled;
            self.DeadlineSeconds = seconds;
            for (int index = self.Entries.Count - 1; index >= 0; --index)
            {
                if ((enabled & Option(self.Entries[index].Kind)) == 0)
                {
                    self.Entries.RemoveAt(index);
                }
            }
            // Changing a threshold or enabling a category starts from current facts, not historical alerts.
            self.BaselineDeadlines(grid);
            self.Changed();
        }

        public static void ObserveNoticeProjection(this RestaurantNoticesComponent self, GridClientComponent grid)
        {
            if (grid?.Confirmed != true)
            {
                return;
            }
            if (!self.Current(grid))
            {
                self.ResetNotices();
                self.WorldId = grid.WorldId;
                self.WorldGeneration = grid.WorldGeneration;
                self.BindingGeneration = grid.BindingGeneration;
                self.LevelId = grid.LevelId;
                self.LevelGeneration = grid.LevelGeneration;
                self.KitchenSerial = grid.LastKitchenOutcomeSerial;
                self.ShipmentSerial = grid.LastShipmentOutcomeSerial;
                self.BaselineReady = true;
                var preferences = self.Root().GetComponent<SoloSaveRepository>()?.Preferences;
                self.Enabled = preferences?.Notices ?? ProductNoticeOptions.All;
                self.DeadlineSeconds = preferences?.DeadlineSeconds ?? 5;
                foreach (var work in grid.Processes.Values)
                {
                    if (work.State == GridWorkState.Completed)
                    {
                        self.InitialFinishedWorks.Add(work.Id);
                    }
                }
                foreach (var work in grid.AutoProcesses.Values)
                {
                    if (work.State == GridAutoState.Ready)
                    {
                        self.InitialFinishedWorks.Add(work.Id);
                    }
                }
                foreach (var work in grid.BakeProcesses.Values)
                {
                    if (work.State == GridBakeState.Completed)
                    {
                        self.InitialFinishedWorks.Add(work.Id);
                    }
                }
                foreach (var shipment in grid.SupplyShipments.Values)
                {
                    if (shipment.Stage is ShipmentStage.Ready or ShipmentStage.Received)
                    {
                        self.InitialReadyShipments.Add(shipment.Id);
                    }
                }
                self.BaselineDeadlines(grid);
                self.LastTick = grid.Tick;
                self.Changed();
                return;
            }
            if (self.LevelId != grid.LevelId || self.LevelGeneration != grid.LevelGeneration)
            {
                self.LevelId = grid.LevelId;
                self.LevelGeneration = grid.LevelGeneration;
                self.ObservedDeadlines.Clear();
                self.Entries.RemoveAll(static entry => entry.Kind == RestaurantNoticeKind.Deadline);
                self.BaselineDeadlines(grid);
                self.LastTick = grid.Tick;
                self.Changed();
            }
            self.ObserveNoticePreferences(grid);
            if (grid.LevelPaused || self.LastTick == grid.Tick)
            {
                return;
            }
            self.LastTick = grid.Tick;
            foreach (var order in grid.Orders.Values)
            {
                if (!self.NearDeadline(grid, order) || !self.ObservedDeadlines.Add(order.Id))
                {
                    continue;
                }
                self.AddNotice(RestaurantNoticeKind.Deadline, order.Id, 0, order.CustomerId,
                    Tables.Instance.DTDishConfig.Get(order.DishId).Name + " 订单临期");
            }
        }

        private static void AddNotice(this RestaurantNoticesComponent self, RestaurantNoticeKind kind, long target, long item, long customer, string caption)
        {
            if ((self.Enabled & Option(kind)) == 0)
            {
                return;
            }
            if (self.Entries.Count == MaximumNotices)
            {
                self.Entries.RemoveAt(0);
            }
            self.Entries.Add(new RestaurantNoticeValue(++self.NextNotice, kind, target, item, customer, self.LevelId, caption));
            self.Changed();
        }

        public static void ObserveKitchenNotice(this RestaurantNoticesComponent self, GridClientComponent grid, KitchenWorkOutcomeValue value)
        {
            if (!self.Current(grid) || grid.LastKitchenOutcomeSerial <= self.KitchenSerial)
            {
                return;
            }
            self.KitchenSerial = grid.LastKitchenOutcomeSerial;
            if (value.Stage != KitchenWorkStage.Finished || self.InitialFinishedWorks.Contains(value.WorkId) ||
                !self.FinishedWorks.Add((value.WorkId, value.WorkInstanceId)))
            {
                return;
            }
            long item = value.OutputItemId;
            string name = grid.Items.TryGetValue(item, out var output) ? output.Name : "加工";
            if (item == 0 && grid.AutoProcesses.TryGetValue(value.WorkId, out var batch))
            {
                item = batch.PotItemId;
                name = Tables.Instance.DTGridItemConfig.Get(batch.OutputDefinitionId).Name;
            }
            self.AddNotice(RestaurantNoticeKind.Processing, value.WorkId, item, 0, name + " 已完工");
        }

        public static void ObserveShipmentNotice(this RestaurantNoticesComponent self, GridClientComponent grid, ShipmentOutcomeValue value)
        {
            if (!self.Current(grid) || grid.LastShipmentOutcomeSerial <= self.ShipmentSerial)
            {
                return;
            }
            self.ShipmentSerial = grid.LastShipmentOutcomeSerial;
            if (value.Stage != ShipmentStage.Ready || self.InitialReadyShipments.Contains(value.ShipmentId) ||
                !self.ArrivedShipments.Add((value.ShipmentId, value.ShipmentInstanceId)))
            {
                return;
            }
            string name = grid.SupplyShipments.TryGetValue(value.ShipmentId, out var shipment)
                ? Tables.Instance.DTSupplyConfig.Get(shipment.SourceId).Name : "补货";
            self.AddNotice(RestaurantNoticeKind.Arrival, value.ShipmentId, 0, 0, name + " 已到货");
        }
    }

    [Event(SceneType.ProductClient)]
    public sealed class RestaurantNoticeProjectionObserver : AEvent<Scene, GridProjectionApplied>
    {
        protected override UniTask Run(Scene scene, GridProjectionApplied args)
        {
            var grid = scene.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
            if (grid?.Confirmed == true)
            {
                var notices = scene.GetComponent<RestaurantNoticesComponent>() ?? scene.AddComponent<RestaurantNoticesComponent>();
                notices.ObserveNoticeProjection(grid);
            }
            return UniTask.CompletedTask;
        }
    }

    [Event(SceneType.ProductClient)]
    public sealed class RestaurantKitchenNoticeObserver : AEvent<Scene, KitchenWorkOutcomeApplied>
    {
        protected override UniTask Run(Scene scene, KitchenWorkOutcomeApplied args)
        {
            scene.GetComponent<RestaurantNoticesComponent>()?.ObserveKitchenNotice(scene.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>(), args.Value);
            return UniTask.CompletedTask;
        }
    }

    [Event(SceneType.ProductClient)]
    public sealed class RestaurantShipmentNoticeObserver : AEvent<Scene, ShipmentOutcomeApplied>
    {
        protected override UniTask Run(Scene scene, ShipmentOutcomeApplied args)
        {
            scene.GetComponent<RestaurantNoticesComponent>()?.ObserveShipmentNotice(scene.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>(), args.Value);
            return UniTask.CompletedTask;
        }
    }

    [Event(SceneType.ProductClient)]
    public sealed class RestaurantNoticePreferencesObserver : AEvent<Scene, ProductPreferencesChanged>
    {
        protected override UniTask Run(Scene scene, ProductPreferencesChanged args)
        {
            scene.GetComponent<RestaurantNoticesComponent>()?.ObserveNoticePreferences(scene.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>());
            return UniTask.CompletedTask;
        }
    }

    [Event(SceneType.ProductClient)]
    public sealed class RestaurantNoticesLeftObserver : AEvent<Scene, ProductClientLeft>
    {
        protected override UniTask Run(Scene scene, ProductClientLeft args)
        {
            scene.RemoveComponent<RestaurantNoticesComponent>();
            return UniTask.CompletedTask;
        }
    }

    [Event(SceneType.ProductClient)]
    [FriendOf(typeof(ProductRoomClient))]
    public sealed class RestaurantNoticesRoomObserver : AEvent<Scene, ProductRoomApplied>
    {
        protected override UniTask Run(Scene scene, ProductRoomApplied args)
        {
            var room = scene.GetComponent<ClientSessionComponent>()?.GetComponent<ProductRoomClient>();
            if (room != null && !room.Current.HasRestaurant)
            {
                scene.RemoveComponent<RestaurantNoticesComponent>();
            }
            return UniTask.CompletedTask;
        }
    }
}
#endif
