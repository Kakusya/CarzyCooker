#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    public enum RestaurantNoticeKind { Arrival = 0, Processing = 1, Deadline = 2 }

    public readonly struct RestaurantNoticeValue
    {
        public readonly long Serial, TargetId, ItemId, CustomerId, LevelId;
        public readonly RestaurantNoticeKind Kind;
        public readonly string Caption;
        public RestaurantNoticeValue(long serial, RestaurantNoticeKind kind, long target, long item, long customer, long level, string caption)
        { Serial = serial; Kind = kind; TargetId = target; ItemId = item; CustomerId = customer; LevelId = level; Caption = caption; }
    }

    [ComponentOf(typeof(Scene))]
    public sealed class RestaurantNoticesComponent : Entity, IAwake, IDestroy
    {
        public string WorldId;
        public long WorldGeneration, BindingGeneration, LevelId, LevelGeneration, LastTick;
        public long KitchenSerial, ShipmentSerial, NextNotice, Version;
        public bool BaselineReady;
        public ProductNoticeOptions Enabled;
        public int DeadlineSeconds;
        public readonly List<RestaurantNoticeValue> Entries = new(8);
        public readonly HashSet<long> InitialFinishedWorks = new();
        public readonly HashSet<long> InitialReadyShipments = new();
        public readonly HashSet<(long Id, long Instance)> FinishedWorks = new();
        public readonly HashSet<(long Id, long Instance)> ArrivedShipments = new();
        public readonly HashSet<long> ObservedDeadlines = new();
    }

    public struct RestaurantNoticesChanged { public long Version; }
    public struct ProductPreferencesChanged { public long Version; }
}
#endif
