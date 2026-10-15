#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    [EntitySystemOf(typeof(ProductRoomClient))]
    [FriendOf(typeof(ProductRoomClient))]
    [FriendOf(typeof(ClientSessionComponent))]
    [FriendOf(typeof(GridClientComponent))]
    public static partial class ProductRoomClientSystem
    {
        [EntitySystem]
        private static void Awake(this ProductRoomClient self)
        {
            self.Current = default;
            self.Candidate = default;
        }

        [EntitySystem]
        private static void Destroy(this ProductRoomClient self)
        {
            self.Current = default;
            self.Candidate = default;
        }

        public static void ApplyRoomSnapshot(this ClientSessionComponent client, ProductRoomSnapshot message)
        {
            if (message.ProtocolVersion != ProductSessionRules.ProtocolVersion || string.IsNullOrEmpty(message.MatchId) ||
                message.MemberId <= 0 || message.BindingGeneration <= 0 || message.RoomVersion <= 0 ||
                !ProductMenuCatalog.IsValid(message.MenuVariant) ||
                (string.IsNullOrEmpty(message.WorldId) ? message.WorldGeneration != 0 : message.WorldGeneration <= 0))
            {
                return;
            }
            ProductRoomClient room = client.GetComponent<ProductRoomClient>();
            if (room == null)
            {
                return;
            }
            var projection = new ProductRoomProjection(message);
            if (client.Stage == ProductClientStage.Joining)
            {
                if (room.Candidate.MatchId != projection.MatchId || room.Candidate.BindingGeneration != projection.BindingGeneration ||
                    projection.Version > room.Candidate.Version)
                {
                    room.Candidate = projection;
                }
                return;
            }
            room.InstallRoom(projection);
        }

        public static void ApplyCandidateRoom(this ClientSessionComponent client)
        {
            ProductRoomClient room = client.GetComponent<ProductRoomClient>();
            if (room == null)
            {
                return;
            }
            ProductRoomProjection candidate = room.Candidate;
            room.Candidate = default;
            room.InstallRoom(candidate);
        }

        private static void InstallRoom(this ProductRoomClient self, ProductRoomProjection projection)
        {
            ClientSessionComponent client = self.GetParent<ClientSessionComponent>();
            if (client.Stage != ProductClientStage.Joined || projection.MatchId != client.MatchId ||
                projection.MemberId != client.MemberId || projection.BindingGeneration != client.BindingGeneration ||
                projection.Version <= self.Current.Version)
            {
                return;
            }
            self.Current = projection;
            GridClientComponent grid = client.GetComponent<GridClientComponent>();
            if (grid.WorldId != null && (!projection.HasRestaurant || grid.WorldId != projection.WorldId ||
                grid.WorldGeneration != projection.WorldGeneration))
            {
                grid.ResetGrid();
            }
            EventSystem.Instance.Publish((Scene)client.IScene, new ProductRoomApplied());
            grid.ConfirmGridProjection();
        }

        public static ProductRoomControlRequest CreateRoomCommand(this ClientSessionComponent client, ProductRoomAction action, int menuVariant = 1, int contentChapterId = 0)
        {
            ProductRoomClient room = client.GetComponent<ProductRoomClient>();
            if (client.Stage != ProductClientStage.Joined || room == null || room.Current.Version <= 0)
            {
                throw new InvalidOperationException("RoomNotReady");
            }
            ProductCommandContext context = client.CreateProductCommandContext(string.Empty, 0, 0, 0);
            return ProductMessages.Create<ProductRoomControlRequest>(request =>
            {
                request.ProtocolVersion = ProductSessionRules.ProtocolVersion;
                request.MatchId = client.MatchId;
                request.BindingGeneration = client.BindingGeneration;
                request.CommandId = context.CommandId;
                request.ExpectedRoomVersion = room.Current.Version;
                request.Action = (int)action;
                request.MenuVariant = menuVariant;
                request.ContentChapterId = contentChapterId;
            });
        }

        public static CooperativeBaselineRequest CreateBaselineCommand(this ClientSessionComponent client, byte[] authorization)
        {
            ProductRoomClient room = client.GetComponent<ProductRoomClient>();
            if (client.Stage != ProductClientStage.Joined || room == null || room.Current.Version <= 0)
            {
                throw new InvalidOperationException("RoomNotReady");
            }
            ProductCommandContext context = client.CreateProductCommandContext(string.Empty, 0, 0, 0);
            return ProductMessages.Create<CooperativeBaselineRequest>(request =>
            {
                request.ProtocolVersion = ProductSessionRules.ProtocolVersion;
                request.MatchId = client.MatchId;
                request.BindingGeneration = client.BindingGeneration;
                request.CommandId = context.CommandId;
                request.ExpectedRoomVersion = room.Current.Version;
                request.Authorization = authorization == null ? null : (byte[])authorization.Clone();
            });
        }
    }

    [MessageHandler(SceneType.ProductClient)]
    public sealed class ProductRoomSnapshotHandler : MessageHandler<ClientSessionComponent, ProductRoomSnapshot>
    {
        protected override UniTask Run(ClientSessionComponent client, ProductRoomSnapshot message)
        {
            try
            {
                client.ApplyRoomSnapshot(message);
            }
            finally
            {
                message.Dispose();
            }
            return UniTask.CompletedTask;
        }
    }
}
#endif
