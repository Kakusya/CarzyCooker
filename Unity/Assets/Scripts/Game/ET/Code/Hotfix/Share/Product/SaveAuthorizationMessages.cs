#if !DOTNET
using System;
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductRoot)]
    public sealed class SaveAuthorizationHandler : ProductActorRequestHandler<SoloSaveRepository, SaveAuthorizationRequest, SaveAuthorizationResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, SaveAuthorizationRequest request, SaveAuthorizationResponse response)
        {
            try
            {
                repository.RequireFormalProgress();
                switch ((SaveAuthorizationAction)request.Action)
                {
                    case SaveAuthorizationAction.ExportImport:
                    {
                        response.Document = SaveAuthorizationFiles.ExportImport(repository, request.SlotId);
                        string grant = SaveAuthorizationFiles.ReadImport(response.Document, repository).Grant.GrantId;
                        response.OutputPath = OfflineProfiles.Export(repository, grant + ".ccbranch", response.Document);
                        break;
                    }
                    case SaveAuthorizationAction.ImportBranch:
                        response.Receipt = MemoryPackHelper.Serialize(SaveAuthorizationFiles.Import(repository, request.Document));
                        break;
                    case SaveAuthorizationAction.ExportReplace:
                    {
                        var recipient = OfflineProfiles.DecodeIdentity(request.RecipientIdentity);
                        response.Document = SaveAuthorizationFiles.ExportReplace(repository, request.SlotId, recipient);
                        string grant = SaveAuthorizationFiles.ReadReplace(response.Document, repository).Grant.GrantId;
                        response.OutputPath = OfflineProfiles.Export(repository, grant + ".ccreplace", response.Document);
                        break;
                    }
                    case SaveAuthorizationAction.PreviewReplace:
                        response.Preview = MemoryPackHelper.Serialize(SaveAuthorizationFiles.PreviewReplace(repository, request.TicketId, request.Document));
                        break;
                    case SaveAuthorizationAction.Replace:
                        response.Receipt = MemoryPackHelper.Serialize(SaveAuthorizationFiles.Replace(repository, request.TicketId, request.Document,
                            ProgressClaimCodec.Decode<ProgressReplacePreview>(request.Confirmation)));
                        break;
                    case SaveAuthorizationAction.RejectUnusedGrant:
                    {
                        var inspected = SaveAuthorizationFiles.Inspect(request.Document);
                        SaveAuthorizationFiles.Reject(repository, inspected.GrantId);
                        break;
                    }
                    case SaveAuthorizationAction.PrepareOwnReplace:
                    {
                        var ticket = ProgressClaimFiles.Read(repository, request.TicketId);
                        var source = ticket.Authority.Source ?? throw new InvalidOperationException("GrantScopeDenied");
                        response.Document = SaveAuthorizationFiles.ExportReplace(repository, request.SlotId, OfflineProfiles.OwnIdentity(repository), allowedSource: source);
                        response.Preview = MemoryPackHelper.Serialize(SaveAuthorizationFiles.PreviewReplace(repository, request.TicketId, response.Document));
                        break;
                    }
                    default:
                        throw new InvalidOperationException("InvalidAuthorizationAction");
                }
            }
            catch (Exception exception) when (ProductProgressErrors.Expected(exception))
            {
                response.Error = ProductError.InvalidRequest; response.Message = exception.Message;
            }
            return UniTask.CompletedTask;
        }
    }
}
#endif
