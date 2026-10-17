using System;
using System.Linq;
using Game;

namespace ET.Client
{
    public static partial class SoloSavePresentationSystem
    {
        private static SoloSaveLabel FindSaveLabel(this SoloSavePresentation self, string slot) =>
            self.Labels?.Entries.FirstOrDefault(entry => entry.SlotId == slot);

        private static void AddSaveLabelEditor(this SoloSavePresentation self, MonoUIGridKitchen view, SoloSaveHeader header)
        {
            var label = self.FindSaveLabel(header.SlotId);
            view.SettlementText.text += "\n本机名称：" + (label?.Name != null && label.Name != "" ? label.Name : "默认") +
                (label?.Favorite == true ? " · 已收藏" : " · 未收藏");
            if (self.LabelsFailure != null)
            {
                view.StatusText.text = self.LabelsFailure;
            }
            EntityRef<SoloSavePresentation> original = self;
            view.AddControl("编辑名称与收藏", () =>
            {
                SoloSavePresentation current = original;
                if (current == null || !SameSelectedSave(current.InspectedSave, header))
                {
                    return;
                }
                current.LabelDraft = new SoloSaveLabelDraft
                {
                    SlotId = header.SlotId, Revision = header.Revision, ResultId = header.ResultId,
                    Name = label?.Name ?? "", Favorite = label?.Favorite ?? false,
                    Feedback = current.LabelsFailure
                };
                current.RenderSoloSelection();
            });
        }

        private static bool CurrentLabelDraft(this SoloSavePresentation self, UIGridKitchen hud, SoloSaveLabelDraft draft, long epoch) =>
            self != null && !self.Started && self.Epoch == epoch && (UIGridKitchen)self.Hud == hud && hud?.Available == true &&
            self.LabelDraft == draft && self.InspectedSave?.SlotId == draft.SlotId && self.InspectedSave.Revision == draft.Revision &&
            self.InspectedSave.ResultId == draft.ResultId;

        private static void RenderSaveLabelDraft(this SoloSavePresentation self, UIGridKitchen hud)
        {
            var draft = self.LabelDraft;
            var view = hud.View;
            view.ClockText.text = "本机名称与收藏";
            view.StatusText.text = draft.Busy ? "正在保存本机名称与收藏…" : draft.Feedback ?? "草稿尚未保存";
            view.HintText.text = "只影响本机显示；空名称恢复默认；不会改进度";
            view.SettlementText.text = "记录 " + draft.SlotId.Substring(0, 8) + " · 修订 " + draft.Revision +
                "\n名称最多32个Unicode文本元素，不含换行或控制字符。\n取消保留原值；继续仍需完整校验。";
            view.ShowEntryAddress(draft.Name, 4096);
            EntityRef<SoloSavePresentation> original = self;
            long epoch = self.Epoch;
            if (draft.Busy)
            {
                return;
            }
            view.AddControl("取消名称编辑", () =>
            {
                SoloSavePresentation current = original;
                if (current.CurrentLabelDraft(hud, draft, epoch))
                {
                    current.LabelDraft = null;
                    current.RenderSoloSelection();
                }
            });
            view.AddControl(draft.Favorite ? "取消收藏（草稿）" : "收藏此记录（草稿）", () =>
            {
                SoloSavePresentation current = original;
                if (current.CurrentLabelDraft(hud, draft, epoch))
                {
                    draft.Name = view.EntryAddress;
                    draft.Favorite = !draft.Favorite;
                    draft.Feedback = "草稿尚未保存";
                    current.RenderSoloSelection();
                }
            });
            view.AddControl("清空名称（草稿）", () =>
            {
                SoloSavePresentation current = original;
                if (current.CurrentLabelDraft(hud, draft, epoch))
                {
                    draft.Name = "";
                    draft.Feedback = "默认名称草稿，尚未保存";
                    current.RenderSoloSelection();
                }
            });
            view.AddControl("保存名称与收藏", () =>
            {
                SoloSavePresentation current = original;
                if (current.CurrentLabelDraft(hud, draft, epoch))
                {
                    draft.Name = view.EntryAddress;
                    current.SaveLabelDraft(hud, draft, epoch);
                }
            });
        }

        private static void SaveLabelDraft(this SoloSavePresentation self, UIGridKitchen hud, SoloSaveLabelDraft draft, long epoch)
        {
            if (draft.Busy)
            {
                return;
            }
            draft.Busy = true;
            self.RenderSoloSelection();
            EntityRef<SoloSavePresentation> original = self;
            EntityRef<UIGridKitchen> originalHud = hud;
            hud.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    var repository = self.Repository();
                    var request = SoloSaveLabelsWriteRequest.Create();
                    string profile = repository.SaveProfile();
                    request.SlotId = draft.SlotId;
                    request.Revision = draft.Revision;
                    request.ResultId = draft.ResultId;
                    request.Name = draft.Name;
                    request.Favorite = draft.Favorite;
                    var result = await context.CallLocalValueAsync(self.Root().GetComponent<ProcessInnerSender>(), repository.GetActorId(), request,
                        response => (Error: response.Error == 0 ? null : response.Message, Value: response.Error == 0
                            ? SoloSaveRepositorySystem.DecodeSaveLabels(((SoloSaveLabelsWriteResponse)response).Values, profile) : null));
                    self = original;
                    if (!self.CurrentLabelDraft(originalHud, draft, epoch))
                    {
                        return;
                    }
                    draft.Busy = false;
                    draft.Feedback = result.Error ?? "本机名称与收藏已保存";
                    if (result.Error == null)
                    {
                        self.Labels = result.Value;
                        self.LabelsFailure = null;
                    }
                    self.RenderSoloSelection();
                }
                catch (OperationCanceledException) { }
                catch (Exception error)
                {
                    self = original;
                    if (self.CurrentLabelDraft(originalHud, draft, epoch))
                    {
                        draft.Busy = false;
                        draft.Feedback = "保存失败，原值保留：" + error.Message;
                        self.RenderSoloSelection();
                    }
                }
            });
        }
    }
}
