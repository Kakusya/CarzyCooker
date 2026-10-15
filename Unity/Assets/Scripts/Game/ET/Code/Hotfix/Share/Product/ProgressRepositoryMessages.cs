#if !DOTNET
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductRoot)]
    public sealed class ProgressRepositoryHandler : ProductActorRequestHandler<SoloSaveRepository, ProgressRepositoryRequest, ProgressRepositoryResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, ProgressRepositoryRequest request, ProgressRepositoryResponse response)
        {
            try
            {
                switch ((ProgressRepositoryOperation)request.Operation)
                {
                    case ProgressRepositoryOperation.PrepareResult:
                        Prepare(repository, request, response);
                        break;
                    case ProgressRepositoryOperation.PersistTicket:
                        response.Id = ProgressClaimFiles.PersistTicket(repository, request.Document, request.MatchId,
                            request.ExpectedHostIdentity?.Length > 0 ? OfflineProfiles.DecodeIdentity(request.ExpectedHostIdentity) : null);
                        break;
                    case ProgressRepositoryOperation.PersistClose:
                        response.Id = ProgressClaimFiles.PersistClose(repository, request.Document, request.MatchId,
                            request.ExpectedHostIdentity?.Length > 0 ? OfflineProfiles.DecodeIdentity(request.ExpectedHostIdentity) : null);
                        break;
                    case ProgressRepositoryOperation.Claim:
                        response.Receipt = MemoryPackHelper.Serialize(ProgressClaimFiles.Claim(repository, request.Id));
                        break;
                    case ProgressRepositoryOperation.Browse:
                        foreach (var summary in ProgressClaimFiles.Browse(repository))
                        {
                            response.Documents.Add(MemoryPackHelper.Serialize(summary));
                        }
                        break;
                    case ProgressRepositoryOperation.SignClose:
                    {
                        var close = ProgressClaimCodec.Decode<ProgressCloseValue>(request.Document);
                        var authority = ProgressClaimCodec.Authority(close.Authority, repository);
                        var own = OfflineProfiles.OwnIdentity(repository);
                        if (close.MatchId != authority.Value.MatchId || close.MatchId != request.MatchId ||
                            authority.Host.ProfileId != own.ProfileId || authority.Host.KeyId != own.KeyId ||
                            !SoloSaveValidation.IsIdentifier(close.ProfileId) || !SoloSaveValidation.IsIdentifier(close.ReceiptId))
                        {
                            throw new InvalidOperationException("InvalidCloseReceipt");
                        }
                        response.Document = OfflineProfiles.Sign(repository, OfflineProfiles.NormalClose, MemoryPackHelper.Serialize(close));
                        break;
                    }
                    default:
                        throw new InvalidOperationException("InvalidProgressOperation");
                }
            }
            catch (Exception exception) when (ProductProgressErrors.Expected(exception))
            {
                response.Error = ProductError.InvalidRequest;
                response.Message = exception.Message;
            }
            return UniTask.CompletedTask;
        }

        private static void Prepare(SoloSaveRepository repository, ProgressRepositoryRequest request, ProgressRepositoryResponse response)
        {
            var success = ProgressClaimCodec.Decode<CooperativeSuccessValue>(request.Document);
            var authority = ProgressClaimCodec.Decode<ProgressAuthorityValue>(success.Authority);
            var own = OfflineProfiles.OwnIdentity(repository);
            if (success.MatchId != request.MatchId || authority.MatchId != request.MatchId || success.WorldId != authority.WorldId ||
                request.Profiles.Count == 0 || request.Profiles.Count > 32 ||
                request.Profiles.Count != request.Keys.Count || request.Profiles.Count != request.TicketIds.Count)
            {
                throw new InvalidOperationException("InvalidSuccessResult");
            }
            authority.HostProfileId = own.ProfileId;
            authority.HostKeyId = own.KeyId;
            success.Authority = OfflineProfiles.Sign(repository, OfflineProfiles.MatchAuthority, MemoryPackHelper.Serialize(authority));
            // 同一验证器核对所载源档委托；Root不会替Host扩大权限。
            ProgressClaimCodec.Authority(success.Authority, repository);
            response.Document = OfflineProfiles.Sign(repository, OfflineProfiles.SuccessResult, MemoryPackHelper.Serialize(success));
            var profiles = new HashSet<string>(StringComparer.Ordinal);
            var tickets = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < request.Profiles.Count; ++i)
            {
                if (!SoloSaveValidation.IsIdentifier(request.Profiles[i]) || !SoloSaveValidation.IsIdentifier(request.Keys[i]) ||
                    !SoloSaveValidation.IsIdentifier(request.TicketIds[i]) || !profiles.Add(request.Profiles[i]) || !tickets.Add(request.TicketIds[i]))
                {
                    throw new InvalidOperationException("InvalidClaimIdentity");
                }
                response.Documents.Add(OfflineProfiles.Sign(repository, OfflineProfiles.ClaimTicket, MemoryPackHelper.Serialize(new ProgressTicketValue
                {
                    TicketId = request.TicketIds[i], EligibleProfileId = request.Profiles[i], EligibleKeyId = request.Keys[i], Success = response.Document
                })));
            }
        }
    }
}
#endif
