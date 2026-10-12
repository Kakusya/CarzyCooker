#if !DOTNET
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public readonly struct GridCompanionProjection
    {
        public readonly long Id, CompanionId, WorkId, ClaimSerial, StepStartTick;
        public readonly int DefinitionId, CompletedCount;
        public readonly string Name;
        public readonly GridCell Cell, NextCell;
        public readonly GridMotion Motion;
        public readonly IReadOnlyList<GridCell> Path;
        public GridCompanionProjection(GridCompanionState value)
        {
            Id=value.RoleId;CompanionId=value.CompanionId;DefinitionId=value.DefinitionId;CompletedCount=value.CompletedCount;Name=value.Name;
            Cell=new GridCell(value.X,value.Y);NextCell=new GridCell(value.NextX,value.NextY);Motion=(GridMotion)value.Motion;
            WorkId=value.WorkId;ClaimSerial=value.ClaimSerial;StepStartTick=value.StepStartTick;
            Path=Array.AsReadOnly(value.Path.Select(cell=>new GridCell(cell.X,cell.Y)).ToArray());
        }
    }
    public readonly struct GridFrontWorkProjection
    {
        public readonly long Id, CustomerId, StationId, ItemId, WorkerId, ClaimSerial;
        public readonly FrontWorkKind Kind;
        public readonly FrontWorkState State;
        public readonly FrontWorkerKind WorkerKind;
        public readonly double Progress;
        public readonly int FrozenTicks;
        public readonly GridCell OperationCell;
        public GridFrontWorkProjection(GridFrontWorkState value)
        {
            Id=value.WorkId;CustomerId=value.CustomerId;StationId=value.StationId;ItemId=value.ItemId;WorkerId=value.WorkerId;ClaimSerial=value.ClaimSerial;
            Kind=(FrontWorkKind)value.Kind;State=(FrontWorkState)value.State;WorkerKind=(FrontWorkerKind)value.WorkerKind;
            Progress=value.Progress;FrozenTicks=value.FrozenTicks;OperationCell=new GridCell(value.OperationX,value.OperationY);
        }
    }
    public readonly struct GridCustomerProjection
    {
        public readonly long Id, TableStationId, OrderId, StepStartTick;
        public readonly int ArrivalSequence, RemainingEatTicks;
        public readonly CustomerPhase Phase;
        public readonly GridCell Cell, NextCell;
        public readonly GridMotion Motion;
        public readonly IReadOnlyList<GridCell> Path;
        public GridCustomerProjection(GridCustomerState value)
        {
            Id=value.CustomerId;TableStationId=value.TableStationId;OrderId=value.OrderId;StepStartTick=value.StepStartTick;
            ArrivalSequence=value.ArrivalSequence;RemainingEatTicks=value.RemainingEatTicks;Phase=(CustomerPhase)value.Phase;
            Cell=new GridCell(value.X,value.Y);NextCell=new GridCell(value.NextX,value.NextY);Motion=(GridMotion)value.Motion;
            Path=Array.AsReadOnly(value.Path.Select(cell=>new GridCell(cell.X,cell.Y)).ToArray());
        }
    }
    public readonly struct GridOrderProjection
    {
        public readonly long Id, CustomerId, TableStationId, DeadlineTick, ServedItemId;
        public readonly int DishId, Score;
        public readonly OrderState State;
        public GridOrderProjection(GridOrderState value)
        { Id=value.OrderId;CustomerId=value.CustomerId;TableStationId=value.TableStationId;DeadlineTick=value.DeadlineTick;
            ServedItemId=value.ServedItemId;DishId=value.DishId;Score=value.Score;State=(OrderState)value.State; }
    }
    public readonly struct GridTableProjection
    {
        public readonly long Id, CustomerId;
        public readonly FrontTableState State;
        public readonly GridCell Seat;
        public GridTableProjection(GridTableState value)
        { Id=value.StationId;CustomerId=value.CustomerId;State=(FrontTableState)value.State;Seat=new GridCell(value.SeatX,value.SeatY); }
    }
}
#endif
