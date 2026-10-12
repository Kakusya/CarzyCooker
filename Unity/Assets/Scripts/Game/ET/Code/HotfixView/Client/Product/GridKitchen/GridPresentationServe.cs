using System;
using Game;

namespace ET.Client
{
    [FriendOf(typeof(GridPresentationComponent))]
    [FriendOf(typeof(GridClientComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    public static partial class GridPresentationSystem
    {
        public static void SendServeOrder(this GridPresentationComponent self,long order,long customer,long table,long item,long level,long levelGeneration)
        {
            var owner=self.GridCommandOwner();if(owner==null)return;EntityRef<GridPresentationComponent> original=self;
            owner.Run(async context=>
            {
                ClientSessionComponent client=((Scene)self.IScene).GetComponent<ClientSessionComponent>();GridClientComponent grid=client.GetComponent<GridClientComponent>();
                if(!grid.Confirmed || grid.LevelId!=level || grid.LevelGeneration!=levelGeneration)return;
                EntityRef<GridClientComponent> projectionRef=grid;var request=grid.CreateServeOrder(order,customer,table,item);
                long command=request.CommandId,binding=grid.BindingGeneration,generation=grid.WorldGeneration;string world=grid.WorldId;
                try
                {
                    string error=await client.CallProductValueAsync(context,request,response=>response.Error==0?null:response.Message);
                    context.ThrowIfStopped();GridPresentationComponent current=original;grid=projectionRef;
                    if(current==null || !current.Ready || grid==null || grid.WorldId!=world || grid.WorldGeneration!=generation || grid.BindingGeneration!=binding ||
                        grid.LevelId!=level || grid.LevelGeneration!=levelGeneration || grid.LastCommandId!=command)return;
                    if(error!=null){grid.LastStatus=GridResultStatus.Rejected;grid.LastReason=error;current.RefreshGridHud();}
                }
                catch(OperationCanceledException){}
                catch(Exception error){GridPresentationComponent current=original;if(current!=null){current.Failure=error.Message;current.RefreshGridHud();}}
            });
        }
    }
}
