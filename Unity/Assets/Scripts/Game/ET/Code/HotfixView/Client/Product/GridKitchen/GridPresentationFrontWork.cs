using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [FriendOf(typeof(GridPresentationComponent))]
    [FriendOf(typeof(GridClientComponent))]
    [FriendOf(typeof(ClientSessionComponent))]
    public static partial class GridPresentationSystem
    {
        public static void SendFrontWork(this GridPresentationComponent self, FrontWorkAction action, long work, long serial, long level, long generation)
        {
            GridClientComponent grid = ((Scene)self.IScene).GetComponent<ClientSessionComponent>()?.GetComponent<GridClientComponent>();
            if (!self.Ready || grid == null || !grid.Confirmed || grid.LevelId != level || grid.LevelGeneration != generation) return;
            ClientSessionComponent client = grid.GetParent<ClientSessionComponent>();
            var owner = self.GridCommandOwner();
            if (owner==null) return;
            var request = grid.CreateFrontWorkCommand(action, work, serial);
            long command = request.CommandId, binding = grid.BindingGeneration, worldGeneration = grid.WorldGeneration; string world = grid.WorldId;
            EntityRef<GridPresentationComponent> original = self;
            EntityRef<GridClientComponent> gridRef = grid;
            owner.Run(async context =>
            {
                var response = await client.CallProductValueAsync(context, request, value =>
                { var reply = (FrontWorkResponse)value; return (reply.Error, reply.Status, reply.Message); });
                context.Apply(() =>
                {
                    GridPresentationComponent current = original; GridClientComponent projection = gridRef;
                    if (current == null || !current.Ready || projection == null || projection.WorldId != world || projection.WorldGeneration != worldGeneration ||
                        projection.BindingGeneration != binding || projection.LevelId != level || projection.LevelGeneration != generation || projection.LastCommandId != command) return;
                    if (response.Error != 0) { projection.LastStatus = GridResultStatus.Rejected; projection.LastReason = response.Message; }
                    current.RefreshGridHud();
                });
            });
        }
    }
}
