using System;
using Cysharp.Threading.Tasks;
using Game;
using UnityGameFramework.Runtime;
using GameEntry = Game.GameEntry;

namespace ET.Client
{
    [EntitySystemOf(typeof(ProductFeedbackComponent))]
    [FriendOf(typeof(ProductFeedbackComponent))]
    [FriendOf(typeof(GridClientComponent))]
    public static partial class ProductFeedbackSystem
    {
        [EntitySystem]
        private static void Awake(this ProductFeedbackComponent self)
        {
            self.Ready = false;
            self.Pending.Clear(); self.WorkTransitions.Clear(); self.Playing.Clear();
            Array.Clear(self.Requested, 0, self.Requested.Length);
            self.LoadedCount = 0; self.LoadFailure = null;
            ProductAudioSettings.ApplySaved();
            self.Subscriptions = new ProductFeedbackSubscriptions();
            EntityRef<ProductFeedbackComponent> original = self;
            self.Subscriptions.Loaded = (_, args) =>
            {
                ProductFeedbackComponent current = original;
                var value = (PlaySoundSuccessEventArgs)args;
                if (current != null && value.UserData is ProductFeedbackPlayback playback && current.Playing.Contains(playback))
                {
                    playback.Serial = value.SerialId;
                    playback.Agent = value.SoundAgent;
                    ++current.LoadedCount;
                }
            };
            self.Subscriptions.Failed = (_, args) =>
            {
                ProductFeedbackComponent current = original;
                var value = (PlaySoundFailureEventArgs)args;
                if (current != null && value.UserData is ProductFeedbackPlayback playback && current.Playing.Remove(playback))
                {
                    current.LoadFailure = value.SoundAssetName + ": " + value.ErrorMessage;
                    Log.Warning("Product sound failed: " + current.LoadFailure);
                }
            };
            GameEntry.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, self.Subscriptions.Loaded);
            GameEntry.Event.Subscribe(PlaySoundFailureEventArgs.EventId, self.Subscriptions.Failed);
            self.SynchronizeFeedback();
        }

        [EntitySystem]
        private static void Update(this ProductFeedbackComponent self)
        {
            self.SynchronizeFeedback();
            for (int index = self.Playing.Count - 1; index >= 0; --index)
            {
                ProductFeedbackPlayback playback = self.Playing[index];
                if (playback.Agent != null && (playback.Agent.SerialId != playback.Serial || !playback.Agent.IsPlaying))
                {
                    self.Playing.RemoveAt(index);
                }
            }
        }

        [EntitySystem]
        private static void Destroy(this ProductFeedbackComponent self)
        {
            self.StopFeedback();
            self.Pending.Clear(); self.WorkTransitions.Clear();
            if (GameEntry.Event != null)
            {
                GameEntry.Event.Unsubscribe(PlaySoundSuccessEventArgs.EventId, self.Subscriptions.Loaded);
                GameEntry.Event.Unsubscribe(PlaySoundFailureEventArgs.EventId, self.Subscriptions.Failed);
            }
            self.Subscriptions = null;
            self.Ready = false;
        }

        public static ProductFeedbackComponent Feedback(Scene scene)
        {
            if (GameEntry.Sound == null || GameEntry.Event == null)
            {
                return null;
            }
            var grid = scene.GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
            return grid?.Confirmed == true ? scene.GetComponent<ProductFeedbackComponent>() ?? scene.AddComponent<ProductFeedbackComponent>() : null;
        }

        private static bool SynchronizeFeedback(this ProductFeedbackComponent self)
        {
            var client = ((Scene)self.IScene).GetComponent<ClientSessionComponent>();
            var grid = client?.GetComponent<GridClientComponent>();
            if (client?.Stage != ProductClientStage.Joined || grid?.Confirmed != true)
            {
                self.StopFeedback(); self.Pending.Clear(); self.Ready = false;
                return false;
            }
            if (!self.Ready || self.World != grid.WorldId || self.Generation != grid.WorldGeneration || self.Binding != grid.BindingGeneration)
            {
                self.StopFeedback(); self.Pending.Clear(); self.WorkTransitions.Clear();
                self.World = grid.WorldId; self.Generation = grid.WorldGeneration; self.Binding = grid.BindingGeneration;
                self.Level = grid.LevelId; self.LevelGeneration = grid.LevelGeneration;
                self.LastCommandSerial = grid.LastProductOutcomeSerial;
                self.LastKitchenSerial = grid.LastKitchenOutcomeSerial;
                self.LastPhaseSerial = grid.LastLevelPhaseSerial;
                self.LastFinishedTick = grid.Tick;
                self.CompletedLevel = grid.LevelPhase == LevelPhase.Completed ? grid.LevelId : 0;
                self.CompletedGeneration = grid.LevelGeneration;
                self.Ready = true;
            }
            if (self.Level != grid.LevelId || self.LevelGeneration != grid.LevelGeneration)
            {
                self.StopFeedback(); self.Pending.Clear();
                self.Level = grid.LevelId; self.LevelGeneration = grid.LevelGeneration;
            }
            if (grid.LevelPaused)
            {
                self.StopFeedback();
                return false;
            }
            return true;
        }

        public static void StopFeedback(this ProductFeedbackComponent self)
        {
            foreach (ProductFeedbackPlayback playback in self.Playing)
            {
                if (playback.Serial != 0 && GameEntry.Sound != null)
                {
                    GameEntry.Sound.StopSound(playback.Serial, 0);
                }
            }
            self.Playing.Clear();
        }

        public static void RegisterFeedback(this ProductFeedbackComponent self, GridClientComponent grid, long command, GridAction action)
        {
            ProductFeedbackKind kind = action switch
            {
                GridAction.TakeCounter or GridAction.PickupDrop or GridAction.TakeContainerContent or GridAction.TakeSupplyPortion => ProductFeedbackKind.Pickup,
                GridAction.PlaceCounter or GridAction.AddToContainer or GridAction.PourContents or GridAction.PortionSoup or GridAction.PortionJuice or
                    GridAction.PortionBatter or GridAction.PortionDrinkBase or GridAction.PortionTeaBase or GridAction.Unmold => ProductFeedbackKind.Place,
                GridAction.StartStationWork or GridAction.StartHeldWork or GridAction.StartAutoWork or GridAction.StartBake or GridAction.StartJuice or
                    GridAction.StartAssembly or GridAction.StartFry or GridAction.StartMix or GridAction.StartGrind or GridAction.StartExtract or
                    GridAction.HeatMilk or GridAction.FoamMilk or GridAction.StartSteam or GridAction.StartBrewTea or GridAction.StartBoilPearls or GridAction.StartMixTea => ProductFeedbackKind.WorkStart,
                _ => ProductFeedbackKind.None
            };
            if (kind != ProductFeedbackKind.None && self.SynchronizeFeedback())
            {
                self.Pending[command] = (new ProductCommandContext(grid.MatchId, grid.WorldId, grid.WorldGeneration,
                    grid.LevelId, grid.LevelGeneration, grid.BindingGeneration, command), kind);
            }
        }

        public static void ForgetFeedback(this ProductFeedbackComponent self, long command) => self.Pending.Remove(command);

        public static void ObserveCommand(this ProductFeedbackComponent self, ProductCommandOutcomeValue value)
        {
            var grid = ((Scene)self.IScene).GetComponent<ClientSessionComponent>().GetComponent<GridClientComponent>();
            // GridResult 的完成事件没有餐厅 OutcomeSerial；已注册的完整上下文负责其去重。
            if (value.Kind != ProductCommandKind.Grid)
            {
                if (grid.LastProductOutcomeSerial <= self.LastCommandSerial)
                {
                    return;
                }
                self.LastCommandSerial = grid.LastProductOutcomeSerial;
            }
            if (value.Status is GridResultStatus.Accepted or GridResultStatus.PendingApproach)
            {
                return;
            }
            bool pending = self.Pending.TryGetValue(value.Context.CommandId, out var registered);
            self.Pending.Remove(value.Context.CommandId);
            if (!self.SynchronizeFeedback() || value.Status != GridResultStatus.Executed)
            {
                return;
            }
            if (value.Kind == ProductCommandKind.Serve)
            {
                self.EmitFeedback(ProductFeedbackKind.Serve);
            }
            else if (pending && registered.Context.Equals(value.Context))
            {
                self.EmitFeedback(registered.Kind);
            }
        }

        public static void ObserveWork(this ProductFeedbackComponent self, KitchenWorkOutcomeValue value)
        {
            var grid = ((Scene)self.IScene).GetComponent<ClientSessionComponent>().GetComponent<GridClientComponent>();
            if (grid.LastKitchenOutcomeSerial <= self.LastKitchenSerial)
            {
                return;
            }
            self.LastKitchenSerial = grid.LastKitchenOutcomeSerial;
            var key = (value.Kind, value.WorkId, value.WorkInstanceId);
            if (self.WorkTransitions.TryGetValue(key, out long serial) && serial >= value.Serial)
            {
                return;
            }
            self.WorkTransitions[key] = value.Serial;
            if (value.Stage == KitchenWorkStage.Finished && self.SynchronizeFeedback() && self.LastFinishedTick != grid.Tick)
            {
                self.LastFinishedTick = grid.Tick;
                self.EmitFeedback(ProductFeedbackKind.WorkFinished);
            }
        }

        public static void ObservePhase(this ProductFeedbackComponent self, LevelPhaseOutcomeValue value)
        {
            var grid = ((Scene)self.IScene).GetComponent<ClientSessionComponent>().GetComponent<GridClientComponent>();
            if (grid.LastLevelPhaseSerial <= self.LastPhaseSerial)
            {
                return;
            }
            self.LastPhaseSerial = grid.LastLevelPhaseSerial;
            if (!self.SynchronizeFeedback())
            {
                return;
            }
            if (value.Phase == LevelPhase.Completed && (self.CompletedLevel != value.LevelId || self.CompletedGeneration != value.LevelGeneration))
            {
                self.CompletedLevel = value.LevelId; self.CompletedGeneration = value.LevelGeneration;
                self.EmitFeedback(ProductFeedbackKind.LevelComplete);
            }
        }

        public static void EmitFeedback(this ProductFeedbackComponent self, ProductFeedbackKind kind)
        {
            if (GameEntry.Sound.IsMuted(ProductAudioSettings.Group) || GameEntry.Sound.GetVolume(ProductAudioSettings.Group) <= 0)
            {
                return;
            }
            if (self.Playing.Count == 3)
            {
                int lowest = 0;
                for (int index = 1; index < self.Playing.Count; ++index)
                {
                    if (self.Playing[index].Kind < self.Playing[lowest].Kind)
                    {
                        lowest = index;
                    }
                }
                if (kind <= self.Playing[lowest].Kind)
                {
                    return;
                }
                GameEntry.Sound.StopSound(self.Playing[lowest].Serial, 0);
                self.Playing.RemoveAt(lowest);
            }
            int id = kind switch
            {
                ProductFeedbackKind.Pickup => UGFSoundId.ProductPickup,
                ProductFeedbackKind.Place => UGFSoundId.ProductPlace,
                ProductFeedbackKind.WorkStart => UGFSoundId.ProductWorkStart,
                ProductFeedbackKind.WorkFinished => UGFSoundId.ProductWorkFinished,
                ProductFeedbackKind.Serve => UGFSoundId.ProductServe,
                ProductFeedbackKind.LevelComplete => UGFSoundId.ProductLevelComplete,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            var playback = new ProductFeedbackPlayback { Kind = kind };
            self.Playing.Add(playback);
            try
            {
                int? serial = GameEntry.Sound.PlaySound(id, userData: playback);
                if (serial.HasValue)
                {
                    playback.Serial = serial.Value;
                    ++self.Requested[(int)kind];
                }
                else
                {
                    self.Playing.Remove(playback);
                    self.LoadFailure = "Sound table registration missing: " + id;
                }
            }
            catch (Exception error)
            {
                self.Playing.Remove(playback);
                self.LoadFailure = error.Message;
                Log.Warning("Product sound failed: " + error.Message);
            }
        }
    }

    [Event(SceneType.ProductClient)]
    public sealed class ProductFeedbackProjection : AEvent<Scene, GridProjectionApplied>
    {
        protected override UniTask Run(Scene scene, GridProjectionApplied args) { ProductFeedbackSystem.Feedback(scene); return UniTask.CompletedTask; }
    }
    [Event(SceneType.ProductClient)]
    public sealed class ProductFeedbackCommand : AEvent<Scene, ProductCommandOutcomeApplied>
    {
        protected override UniTask Run(Scene scene, ProductCommandOutcomeApplied args) { ProductFeedbackSystem.Feedback(scene)?.ObserveCommand(args.Value); return UniTask.CompletedTask; }
    }
    [Event(SceneType.ProductClient)]
    public sealed class ProductFeedbackWork : AEvent<Scene, KitchenWorkOutcomeApplied>
    {
        protected override UniTask Run(Scene scene, KitchenWorkOutcomeApplied args) { ProductFeedbackSystem.Feedback(scene)?.ObserveWork(args.Value); return UniTask.CompletedTask; }
    }
    [Event(SceneType.ProductClient)]
    public sealed class ProductFeedbackPhase : AEvent<Scene, LevelPhaseChangedApplied>
    {
        protected override UniTask Run(Scene scene, LevelPhaseChangedApplied args) { ProductFeedbackSystem.Feedback(scene)?.ObservePhase(args.Value); return UniTask.CompletedTask; }
    }
}
