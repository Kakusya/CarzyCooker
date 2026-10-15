#if !DOTNET
using System;
using System.IO;
using System.Security.Cryptography;
using Cysharp.Threading.Tasks;

namespace ET
{
    public static class ProductProgressErrors
    {
        public static bool Expected(Exception exception) => exception is IOException or UnauthorizedAccessException or
            InvalidOperationException or CryptographicException or MemoryPack.MemoryPackSerializationException or ArgumentException;
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class ProductProfilePeerHandler : ProductActorRequestHandler<HostPeer, ProductProfileRequest, ProductProfileResponse>
    {
        protected override async UniTask Run(HostPeer peer, ProductProfileRequest request, ProductProfileResponse response)
        {
            var inner = ProductMessages.Create<ProductProfileSubmitRequest>(message =>
            {
                message.PeerId = peer.Id;
                message.PeerInstanceId = peer.InstanceId;
                message.ProtocolVersion = request.ProtocolVersion;
                message.MatchId = request.MatchId;
                message.BindingGeneration = request.BindingGeneration;
                message.Action = request.Action;
                message.Proof = request.Proof == null ? null : (byte[])request.Proof.Clone();
            });
            HostSessionComponent authority = peer.Authority;
            using var reply = (ProductProfileSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = reply.Error;
            response.Message = reply.Message;
            response.Challenge = reply.Challenge;
            response.ProfileId = reply.ProfileId;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class ProductProfileSubmitHandler : ProductActorRequestHandler<Entity, ProductProfileSubmitRequest, ProductProfileSubmitResponse>
    {
        protected override UniTask Run(Entity target, ProductProfileSubmitRequest request, ProductProfileSubmitResponse response)
        {
            if (target is not HostSessionComponent authority)
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "InternalOnly";
                return UniTask.CompletedTask;
            }
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId);
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.BindingGeneration ||
                    !peer.EntryOpen || peer.Closing ? "EntryClosed" : null;
            try
            {
                if (error == null)
                {
                    if (request.Proof?.Length > 16384)
                    {
                        error = "InvalidAuthorizationLength";
                    }
                    else
                    {
                        error = authority.ProveProfile(peer, request.MatchId, request.Action, request.Proof, out string challenge, out string profile);
                        response.Challenge = challenge;
                        response.ProfileId = profile;
                    }
                }
            }
            catch (Exception exception) when (ProductProgressErrors.Expected(exception)) { error = exception.Message; }
            response.Error = error == null ? 0 : ProductError.InvalidRequest;
            response.Message = error;
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class CooperativeBaselinePeerHandler : ProductActorRequestHandler<HostPeer, CooperativeBaselineRequest, CooperativeBaselineResponse>
    {
        protected override async UniTask Run(HostPeer peer, CooperativeBaselineRequest request, CooperativeBaselineResponse response)
        {
            response.CommandId = request.CommandId;
            if (!peer.CanControlRestaurant())
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "HostControlOnly";
                response.Status = (int)GridResultStatus.Rejected;
                return;
            }
            var inner = ProductMessages.Create<CooperativeBaselineSubmitRequest>(message =>
            {
                message.PeerId = peer.Id;
                message.PeerInstanceId = peer.InstanceId;
                message.ProtocolVersion = request.ProtocolVersion;
                message.MatchId = request.MatchId;
                message.BindingGeneration = request.BindingGeneration;
                message.CommandId = request.CommandId;
                message.ExpectedRoomVersion = request.ExpectedRoomVersion;
                message.Authorization = request.Authorization == null ? null : (byte[])request.Authorization.Clone();
            });
            HostSessionComponent authority = peer.Authority;
            using var reply = (CooperativeBaselineSubmitResponse)await peer.Root().GetComponent<ProcessInnerSender>().Call(authority.GetActorId(), inner, false);
            response.Error = reply.Error;
            response.Message = reply.Message;
            response.Status = reply.Status;
        }
    }

    [MessageHandler(SceneType.ProductHost)]
    [FriendOf(typeof(HostPeer))]
    public sealed class CooperativeBaselineSubmitHandler : ProductActorRequestHandler<Entity, CooperativeBaselineSubmitRequest, CooperativeBaselineSubmitResponse>
    {
        protected override UniTask Run(Entity target, CooperativeBaselineSubmitRequest request, CooperativeBaselineSubmitResponse response)
        {
            response.CommandId = request.CommandId;
            if (target is not HostSessionComponent authority)
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = "InternalOnly";
                response.Status = (int)GridResultStatus.Rejected;
                return UniTask.CompletedTask;
            }
            HostPeer peer = authority.GetChild<HostPeer>(request.PeerId);
            string error = request.ProtocolVersion != ProductSessionRules.ProtocolVersion ? "IncompatibleProtocol"
                : peer == null || peer.InstanceId != request.PeerInstanceId || peer.Generation != request.BindingGeneration ||
                    !peer.EntryOpen || peer.Closing ? "EntryClosed" : null;
            try
            {
                if (error == null)
                {
                    error = authority.LoadCooperativeBaseline(peer, new ProductCommandContext(request.MatchId, string.Empty, 0, 0, 0,
                        request.BindingGeneration, request.CommandId), request.ExpectedRoomVersion, request.Authorization);
                }
            }
            catch (Exception exception) when (ProductProgressErrors.Expected(exception)) { error = exception.Message; }
            response.Error = error == null ? 0 : ProductError.InvalidRequest;
            response.Message = error ?? peer.LastCommandReason;
            response.Status = (int)(error == null ? peer.LastCommandStatus : GridResultStatus.Rejected);
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductRoot)]
    public sealed class CooperativeBaselineSelectHandler : ProductActorRequestHandler<SoloSaveRepository, CooperativeBaselineSelectRequest, CooperativeBaselineSelectResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, CooperativeBaselineSelectRequest request, CooperativeBaselineSelectResponse response)
        {
            try
            {
                var host = OfflineProfiles.DecodeIdentity(request.HostIdentity);
                OfflineProfiles.RequireTrusted(repository, host);
                response.Authorization = CooperativeBaselineSystem.Authorize(repository, request.SlotId, request.MatchId, host);
            }
            catch (Exception exception) when (ProductProgressErrors.Expected(exception))
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = exception.Message;
            }
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductRoot)]
    public sealed class ProductProfileSignHandler : ProductActorRequestHandler<SoloSaveRepository, ProductProfileSignRequest, ProductProfileSignResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, ProductProfileSignRequest request, ProductProfileSignResponse response)
        {
            try
            {
                if (!SoloSaveValidation.IsIdentifier(request.MatchId) || request.BindingGeneration <= 0 || request.Challenge?.Length != 44)
                {
                    throw new InvalidOperationException("ProfileChallengeRequired");
                }
                response.Proof = OfflineProfiles.Sign(repository, OfflineProfiles.ProfileProof, MemoryPackHelper.Serialize(new ProductProfileProofValue
                {
                    MatchId = request.MatchId, BindingGeneration = request.BindingGeneration, Challenge = request.Challenge
                }));
            }
            catch (Exception exception) when (ProductProgressErrors.Expected(exception))
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = exception.Message;
            }
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductRoot)]
    public sealed class ProgressFileHandler : ProductActorRequestHandler<SoloSaveRepository, ProgressFileRequest, ProgressFileResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, ProgressFileRequest request, ProgressFileResponse response)
        {
            try
            {
                switch ((ProgressFileAction)request.Action)
                {
                    case ProgressFileAction.ExportIdentity:
                    {
                        var own = OfflineProfiles.OwnIdentity(repository);
                        response.Identity = OfflineProfiles.EncodeIdentity(own);
                        response.OutputPath = OfflineProfiles.Export(repository, own.ProfileId + ".ccidentity", response.Identity);
                        break;
                    }
                    case ProgressFileAction.Inspect:
                    {
                        byte[] bytes = OfflineProfiles.ReadLimited(request.InputPath, OfflineProfiles.MaxDocumentBytes);
                        if (string.Equals(Path.GetExtension(request.InputPath), ".ccidentity", StringComparison.OrdinalIgnoreCase))
                        {
                            response.Identity = OfflineProfiles.EncodeIdentity(OfflineProfiles.DecodeIdentity(bytes));
                        }
                        else if (string.Equals(Path.GetExtension(request.InputPath), ".ccbranch", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(Path.GetExtension(request.InputPath), ".ccreplace", StringComparison.OrdinalIgnoreCase))
                        {
                            var inspected = SaveAuthorizationFiles.Inspect(bytes);
                            response.Identity = OfflineProfiles.EncodeIdentity(inspected.Signer);
                            response.Authorization = bytes;
                            response.Summary = inspected.Summary;
                            response.AuthorizationKind = (int)inspected.Kind;
                        }
                        else
                        {
                            var signed = OfflineProfiles.Verify(bytes, OfflineProfiles.BaselineReadRun);
                            var value = CooperativeBaselineSystem.ReadValue(signed, out var header, out _);
                            response.Identity = OfflineProfiles.EncodeIdentity(signed.Signer);
                            response.Authorization = bytes;
                            response.Summary = $"源记录 {header.SlotId} · 修订 {header.Revision}\n目标房主 {value.HostProfileId}\n目标房间 {value.MatchId}\n仅允许读取与本轮结果签发，不授予覆盖权";
                        }
                        break;
                    }
                    case ProgressFileAction.TrustIdentity:
                        OfflineProfiles.TrustIdentity(repository, OfflineProfiles.DecodeIdentity(request.Identity));
                        break;
                    case ProgressFileAction.ExportReadRun:
                    {
                        var host = OfflineProfiles.DecodeIdentity(request.Identity);
                        OfflineProfiles.RequireTrusted(repository, host);
                        response.Authorization = CooperativeBaselineSystem.Authorize(repository, request.SlotId, request.MatchId, host);
                        var signed = OfflineProfiles.Verify(response.Authorization, OfflineProfiles.BaselineReadRun);
                        var value = CooperativeBaselineSystem.ReadValue(signed, out _, out _);
                        response.OutputPath = OfflineProfiles.Export(repository, value.GrantId + ".ccrun", response.Authorization);
                        break;
                    }
                    default:
                        throw new InvalidOperationException("InvalidFileOperation");
                }
            }
            catch (Exception exception) when (ProductProgressErrors.Expected(exception))
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = exception.Message;
            }
            return UniTask.CompletedTask;
        }
    }
}
#endif
