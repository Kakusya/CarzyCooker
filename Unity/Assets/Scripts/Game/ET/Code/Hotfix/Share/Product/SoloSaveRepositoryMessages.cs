#if !DOTNET
using System;
using System.IO;
using Cysharp.Threading.Tasks;

namespace ET
{
    [MessageHandler(SceneType.ProductRoot)]
    public sealed class SoloSaveBrowseHandler : ProductActorRequestHandler<SoloSaveRepository, SoloSaveBrowseRequest, SoloSaveBrowseResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, SoloSaveBrowseRequest request, SoloSaveBrowseResponse response)
        {
            try { foreach (var header in repository.Browse()) response.Headers.Add(MemoryPackHelper.Serialize(header)); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
            { response.Error = ProductError.InvalidRequest; response.Message = e.Message; }
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductRoot)]
    public sealed class SoloSaveSelectHandler : ProductActorRequestHandler<SoloSaveRepository, SoloSaveSelectRequest, SoloSaveSelectResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, SoloSaveSelectRequest request, SoloSaveSelectResponse response)
        {
            try
            {
                var payload = repository.Select(request.SlotId, out var header);
                response.Header = MemoryPackHelper.Serialize(header); response.Payload = MemoryPackHelper.Serialize(payload);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or MemoryPack.MemoryPackSerializationException)
            { response.Error = ProductError.InvalidRequest; response.Message = e.Message; }
            return UniTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.ProductRoot)]
    public sealed class SoloSaveWriteHandler : ProductActorRequestHandler<SoloSaveRepository, SoloSaveWriteRequest, SoloSaveWriteResponse>
    {
        protected override UniTask Run(SoloSaveRepository repository, SoloSaveWriteRequest request, SoloSaveWriteResponse response)
        {
            try
            {
                if (request.Record == null || request.Record.Length > SoloSaveFile.MaxPayloadBytes + SoloSaveFile.MaxHeaderBytes + 16)
                    throw new InvalidOperationException("InvalidSaveRequest");
                using var stream = new MemoryStream(request.Record, false);
                var payload = SoloSaveFile.ReadPayload(stream, repository.SaveProfile(), out var header);
                response.Header = MemoryPackHelper.Serialize(repository.Commit(header, payload));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or MemoryPack.MemoryPackSerializationException)
            { response.Error = ProductError.InvalidRequest; response.Message = e.Message; }
            return UniTask.CompletedTask;
        }
    }

    public static partial class SoloSaveRepositorySystem
    {
        public static string SaveProfile(this SoloSaveRepository self) => self.ProfileId;
    }
}
#endif
