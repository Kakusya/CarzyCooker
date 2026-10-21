using System;
using System.Linq;
using Game;
using UnityEngine;

namespace ET.Client
{
    public static partial class ProductEntryPresentationSystem
    {
        private static string RecordSummaryText(this ProductEntryPresentation self) => self.RecordState switch
        {
            SoloRecordReadState.Ready => self.ContinueRecordCount == 0 ? "还没有可继续的记录。\n从「家常开张」开始你的第一轮经营。" : $"有 {self.ContinueRecordCount} 份可继续记录。\n选择「继续游戏」查看并明确选档。",
            SoloRecordReadState.Failed => "记录读取失败，可重试或进入继续页检查。\n" + self.RecordReadFailure,
            _ => "正在读取本机记录…"
        };

        private static void RefreshRecordSummary(this ProductEntryPresentation self)
        {
            if (self.Suspended || self.Page != ProductEntryPage.Home || !self.Root().GetComponent<ProductRuntimeComponent>().Config.ShowModeMenu)
            {
                return;
            }
            SoloSaveRepository repository = self.Root().GetComponent<SoloSaveRepository>();
            if (self.RecordRepository != (repository?.InstanceId ?? 0))
            {
                self.RecordRepository = repository?.InstanceId ?? 0;
                self.RecordState = SoloRecordReadState.Unknown;
            }
            if (self.RecordState != SoloRecordReadState.Unknown)
            {
                return;
            }
            self.RecordState = SoloRecordReadState.Loading;
            long serial = ++self.RecordReadSerial;
            EntityRef<ProductEntryPresentation> original = self;
            self.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    if (repository == null)
                    {
                        string directory = Application.persistentDataPath;
                        string profile = SoloSaveRepositorySystem.ExistingLocalProfile(directory);
                        if (profile != null)
                        {
                            repository = self.Root().AddComponent<SoloSaveRepository, string, string>(directory, profile);
                        }
                    }
                    self.RecordRepository = repository?.InstanceId ?? 0;
                    int count = 0;
                    if (repository != null)
                    {
                        count = await context.CallLocalValueAsync(self.Root().GetComponent<ProcessInnerSender>(), repository.GetActorId(), SoloSaveBrowseRequest.Create(), response =>
                        {
                            if (response.Error != 0)
                            {
                                throw new InvalidOperationException(response.Message);
                            }
                            return ((SoloSaveBrowseResponse)response).Headers.Count(bytes =>
                                ((SoloSaveHeader)MemoryPackHelper.Deserialize(typeof(SoloSaveHeader), bytes, 0, bytes.Length)).RecordKind == SoloSaveRecordKind.Continue);
                        });
                    }
                    context.ThrowIfStopped();
                    self = original;
                    if (self == null || self.RecordReadSerial != serial)
                    {
                        return;
                    }
                    self.ContinueRecordCount = count;
                    self.RecordState = SoloRecordReadState.Ready;
                    self.RecordReadFailure = null;
                }
                catch (OperationCanceledException) { }
                catch (Exception error)
                {
                    self = original;
                    if (self == null || self.RecordReadSerial != serial)
                    {
                        return;
                    }
                    self.RecordState = SoloRecordReadState.Failed;
                    self.RecordReadFailure = error.Message;
                }
                self = original;
                if (self != null && !self.Suspended && ((UIGridKitchen)self.Hud)?.Available == true)
                {
                    self.RenderProductEntry();
                }
            });
        }
    }
}
