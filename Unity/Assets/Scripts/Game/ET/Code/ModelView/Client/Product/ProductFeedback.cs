using System;
using System.Collections.Generic;
using GameFramework.Event;
using GameFramework.Sound;

namespace ET.Client
{
    public enum ProductFeedbackKind { None = 0, Pickup = 1, Place = 2, WorkStart = 3, WorkFinished = 4, Serve = 5, LevelComplete = 6 }

    [EnableClass]
    public sealed class ProductFeedbackPlayback
    {
        public int Serial;
        public ProductFeedbackKind Kind;
        public ISoundAgent Agent;
    }

    [EnableClass]
    public sealed class ProductFeedbackSubscriptions
    {
        public EventHandler<GameEventArgs> Loaded, Failed;
    }

    [ComponentOf(typeof(Scene))]
    public sealed class ProductFeedbackComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public bool Ready;
        public string World;
        public long Generation, Binding, Level, LevelGeneration;
        public long LastCommandSerial, LastKitchenSerial, LastPhaseSerial;
        public long LastFinishedTick, CompletedLevel, CompletedGeneration;
        public readonly Dictionary<long, (ProductCommandContext Context, ProductFeedbackKind Kind)> Pending = new();
        public readonly Dictionary<(KitchenProcessKind Kind, long Id, long Instance), long> WorkTransitions = new();
        public readonly List<ProductFeedbackPlayback> Playing = new(3);
        public ProductFeedbackSubscriptions Subscriptions;
        // Counts requested GF cues, separately from asynchronous resource/playback success.
        public readonly int[] Requested = new int[(int)ProductFeedbackKind.LevelComplete + 1];
        public int LoadedCount;
        public string LoadFailure;
    }
}
