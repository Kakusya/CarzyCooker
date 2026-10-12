using System;
using Game;

namespace ET.Client
{
    public static partial class GridPresentationSystem
    {
        public static void ChooseLayoutStation(this GridPresentationComponent self,long station,long level,long generation)
        {
            if(!self.Ready)return;
            GridClientComponent grid=((Scene)self.IScene).GetComponent<ClientSessionComponent>().GetComponent<GridClientComponent>();
            if(grid.LevelId!=level || grid.LevelGeneration!=generation || grid.LevelPhase!=LevelPhase.Preparing || grid.LevelPaused || !grid.Stations.ContainsKey(station))return;
            self.LayoutStationId=station;self.LayoutLevelId=level;self.LayoutLevelGeneration=generation;self.RefreshGridHud();
        }

        public static void SendStationLayout(this GridPresentationComponent self,long station,GridCell cell,int orientation,
            long layoutVersion,long level,long generation)
        {
            var owner=self.GridCommandOwner();if(owner==null)return;
            EntityRef<GridPresentationComponent> selfRef=self;
            owner.Run(async context=>
            {
                ClientSessionComponent client=((Scene)self.IScene).GetComponent<ClientSessionComponent>();
                GridClientComponent grid=client.GetComponent<GridClientComponent>();EntityRef<GridClientComponent> gridRef=grid;
                if(grid.LevelId!=level || grid.LevelGeneration!=generation)return;
                StationLayoutRequest request=grid.CreateStationLayout(station,cell,orientation,layoutVersion);
                request.LevelId=level;request.LevelGeneration=generation;
                long command=request.CommandId,world=grid.WorldGeneration,binding=grid.BindingGeneration;
                string worldId=grid.WorldId;
                try
                {
                    string error=await client.CallProductValueAsync(context,request,response=>response.Error==0?null:response.Message);
                    context.ThrowIfStopped();self=selfRef;grid=gridRef;
                    if(self==null || grid==null || !self.Ready || grid.WorldId!=worldId || grid.WorldGeneration!=world || grid.BindingGeneration!=binding || grid.LevelId!=level || grid.LevelGeneration!=generation || grid.LastCommandId!=command)return;
                    if(error!=null){grid.LastStatus=GridResultStatus.Rejected;grid.LastReason=error;self.RefreshGridHud();}
                }
                catch(OperationCanceledException) { }
                catch(Exception exception) { self=selfRef;if(self!=null){self.Failure=exception.Message;self.RefreshGridHud();} }
            });
        }
    }
}
