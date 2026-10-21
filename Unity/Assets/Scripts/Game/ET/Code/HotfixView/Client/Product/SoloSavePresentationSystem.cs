using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ET.Client
{
    [EntitySystemOf(typeof(SoloSavePresentation))]
    [FriendOf(typeof(SoloSavePresentation))]
    [FriendOf(typeof(ProductRuntimeComponent))]
    public static partial class SoloSavePresentationSystem
    {
        [EntitySystem]
        private static void Awake(this SoloSavePresentation self)
        {
            self.PreferencesDraft = null;
            self.AudioPage = null;
            self.Labels = null; self.LabelsFailure = null; self.LabelDraft = null;
            self.InitializePreferences();
            self.Hud = default; self.Headers = Array.Empty<SoloSaveHeader>(); self.Failure = null; self.Epoch = 0;
            self.ViewedResult = null;
            self.NewMenuVariant = ProductMenuVariant.Drink;
            self.RenderVersion = 0; self.StartPreviewVisible = false;
            self.Page = SoloEntryPage.Home;
            self.RecordState = SoloRecordReadState.Unknown;
            self.ClearSaveInspection(); self.SaveFilter = SoloSaveListFilter.All; self.BrowseSerial = 0;
            self.LoadingHud = false; self.LoadingSelection = false; self.Started = false;
            self.OpenSoloSelection();
        }

        [EntitySystem]
        private static void Update(this SoloSavePresentation self)
        {
            if (!self.Started && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if ((UIGridKitchen)self.Hud != null || self.LoadingHud) self.CancelSoloSelection();
                else self.OpenSoloSelection();
            }
        }

        [EntitySystem]
        private static void Destroy(this SoloSavePresentation self) => self.CloseSoloSelection();

        public static void CloseSoloSelection(this SoloSavePresentation self)
        {
            self.AudioPage = null;
            self.PreferencesDraft = null;
            self.LabelDraft = null;
            self.RequestedSlotId = null; self.RequestedRevision = 0;
            self.StartPreviewVisible = false; self.ClearSaveInspection();
            ++self.Epoch; self.LoadingHud = false; self.LoadingSelection = false;
            ((Scene)self.IScene).GetComponent<UIComponent>()?.RemoveComponent<UIGridKitchen>(); self.Hud = default;
        }

        public static void CancelSoloSelection(this SoloSavePresentation self)
        {
            if (self.Started) return;
            self.CloseSoloSelection(); self.Failure = null; self.OpenSoloSelection();
        }

        public static void OpenSoloSelection(this SoloSavePresentation self)
        {
            if (self.Started || self.LoadingHud || ((UIGridKitchen)self.Hud)?.Available == true) return;
            self.LoadingHud = true; long epoch = ++self.Epoch; EntityRef<SoloSavePresentation> original = self;
            self.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    Scene scene = (Scene)self.IScene; var ui = scene.GetComponent<UIComponent>() ?? scene.AddComponent<UIComponent>();
                    var hud = await ui.AddUIFormComponentAsync<UIGridKitchen>(UGFUIFormId.UIGridKitchen);
                    context.ThrowIfStopped(); self = original;
                    if (self == null || self.Epoch != epoch || self.Started) { hud?.Dispose(); return; }
                    self.Hud = hud; hud.View.ConfigureHud(); self.RenderSoloSelection(); self.BrowseSoloSelection();
                }
                catch (OperationCanceledException) { }
                catch (Exception e) { self = original; if (self != null && self.Epoch == epoch) { self.Failure = e.Message; self.RenderSoloSelection(); } }
                finally { self = original; if (self != null && self.Epoch == epoch) self.LoadingHud = false; }
            });
        }

        private static SoloSaveRepository Repository(this SoloSavePresentation self)
        {
            var repository = self.Root().GetComponent<SoloSaveRepository>();
            if (repository != null) return repository;
            if (Application.isMobilePlatform) throw new InvalidOperationException("当前平台只支持内存试玩");
            string directory = Application.persistentDataPath;
            string profile = SoloSaveRepositorySystem.LocalProfile(directory);
            return self.Root().AddComponent<SoloSaveRepository, string, string>(directory, profile);
        }

        public static void NavigateSoloRecord(this SoloSavePresentation self, string slot, int revision)
        {
            self.RequestedSlotId = slot;
            self.RequestedRevision = revision;
            if (((UIGridKitchen)self.Hud)?.Available == true)
            {
                self.BrowseSoloSelection();
            }
        }

        private static void BrowseSoloSelection(this SoloSavePresentation self)
        {
            UIGridKitchen hud = self.Hud; if (hud?.Available != true) return;
            EntityRef<SoloSavePresentation> original = self; long epoch = self.Epoch, serial = ++self.BrowseSerial;
            self.RecordState = SoloRecordReadState.Loading;
            self.RenderSoloSelection();
            hud.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    var repository = self.Root().GetComponent<SoloSaveRepository>();
                    if (repository == null)
                    {
                        string directory = Application.persistentDataPath;
                        string profile = SoloSaveRepositorySystem.ExistingLocalProfile(directory);
                        if (profile == null)
                        {
                            self.Headers = Array.Empty<SoloSaveHeader>();
                            self.RecordState = SoloRecordReadState.Ready;
                            self.Failure = null;
                            self.RenderSoloSelection();
                            return;
                        }
                        repository = self.Root().AddComponent<SoloSaveRepository, string, string>(directory, profile);
                    }
                    using var reply = (SoloSaveBrowseResponse)await self.Root().GetComponent<ProcessInnerSender>().Call(repository.GetActorId(), ProductMessages.Create<SoloSaveBrowseRequest>(_ => { }), false);
                    context.ThrowIfStopped(); self = original;
                    if (self == null || self.Epoch != epoch || self.BrowseSerial != serial || self.Started) return;
                    if (reply.Error != 0) throw new InvalidOperationException(reply.Message);
                    string labelsProfile = repository.SaveProfile();
                    var labels = await context.CallLocalValueAsync(self.Root().GetComponent<ProcessInnerSender>(), repository.GetActorId(), SoloSaveLabelsReadRequest.Create(),
                        response => (Error: response.Error == 0 ? null : response.Message, Value: response.Error == 0
                            ? SoloSaveRepositorySystem.DecodeSaveLabels(((SoloSaveLabelsReadResponse)response).Values, labelsProfile) : null));
                    self = original;
                    if (self == null || self.Epoch != epoch || self.BrowseSerial != serial || self.Started) return;
                    self.Labels = labels.Value;
                    self.LabelsFailure = labels.Error;
                    self.Headers = reply.Headers.Select(bytes => (SoloSaveHeader)MemoryPackHelper.Deserialize(typeof(SoloSaveHeader), bytes, 0, bytes.Length))
                        .OrderBy(header => header.RecordKind).ThenByDescending(header => header.SavedUtcTicks).ToArray();
                    if (self.InspectedSave != null && !self.Headers.Any(header => SameSelectedSave(self.InspectedSave, header)))
                    {
                        self.ClearSaveInspection();
                    }
                    self.Failure = null;
                    self.RecordState = SoloRecordReadState.Ready;
                    if (self.RequestedSlotId != null)
                    {
                        string slot = self.RequestedSlotId;
                        int revision = self.RequestedRevision;
                        self.RequestedSlotId = null; self.RequestedRevision = 0;
                        if (self.Headers.Any(header => header.SlotId == slot && header.Revision == revision))
                        {
                            self.ChooseSoloSave(slot);
                            return;
                        }
                        self.Failure = "RecordChanged";
                    }
                    self.RenderSoloSelection();
                }
                catch (OperationCanceledException) { }
                catch (Exception e) { self = original; if (self != null && self.Epoch == epoch && self.BrowseSerial == serial) { self.RecordState = SoloRecordReadState.Failed; self.Failure = e.Message; self.RenderSoloSelection(); } }
            });
        }

        private static void RenderSoloSelection(this SoloSavePresentation self)
        {
            UIGridKitchen hud = self.Hud; if (hud?.Available != true || self.Started) return;
            self.ApplySavedPreferences(hud.View);
            if (self.RenderAudio(hud, () => self.RenderSoloSelection())) return;
            if (self.RenderPreferences(hud, () => self.RenderSoloSelection())) return;
            long render = ++self.RenderVersion;
            var view = hud.View; view.ClearTargets(); view.SetInformation("", "", false, false); view.ShowEntryLayout();
            view.PhaseText.text = "单机餐厅"; view.ClockText.text = "选择开始方式"; view.ScoreText.text = "成功后保存";
            view.StatusText.text = self.LoadingSelection ? "正在读取并校验检查点…" : "选择新开餐厅，或继续一份本机记录";
            view.HandText.text = "保存只新建，不覆盖原记录"; view.WorkText.text = ""; view.HintText.text = "本机 PC 单机 · 内存试玩退出后不保留";
            view.SettlementText.text = self.Failure == null ? "检查点恢复完整厨房，并进入下一关准备阶段" : "读取失败，尚未创建餐厅。可以重新选择或重试。";
            if (self.ViewedResult != null)
                view.SettlementText.text = $"{RestaurantChapters.Find(Tables.Instance.DTLevelConfig.GetOrDefault(self.ViewedResult.CompletedLevelId))?.Name ?? "大关 " + self.ViewedResult.ChapterId} 已完成 · 第 {self.ViewedResult.ChapterStage} 关\n成功 {self.ViewedResult.ServedOrders} 单 · 未满足 {self.ViewedResult.UnfulfilledOrders} 单\n{self.ViewedResult.Score} 分 · {self.ViewedResult.Stars} 星\n此记录仅供查看，选择新开餐厅可开始独立的一轮";
            if (self.Failure != null) view.StatusText.text = self.Failure switch
            {
                "UnsupportedVersion" => "此记录版本不受支持，请选择其他记录",
                "WrongPlatform" => "此记录不属于当前平台",
                "WrongProfile" => "此记录不属于本机玩家",
                _ => "无法读取检查点，请刷新或选择其他记录"
            };
            EntityRef<SoloSavePresentation> original = self; EntityRef<UIGridKitchen> hudRef = hud; long epoch = self.Epoch;
            view.SetInputGuard(() => { SoloSavePresentation current = original; return current != null && current.Epoch == epoch && current.RenderVersion == render && !current.Started && ((UIGridKitchen)hudRef)?.Available == true; });
            if (self.LoadingSelection) { view.AddControl("取消加载", () => { SoloSavePresentation current = original; current?.CancelSoloSelection(); }); return; }
            if (self.LabelDraft != null)
            {
                self.RenderSaveLabelDraft(hud);
                return;
            }
            self.AddPreferencesControl(view, () => self.RenderSoloSelection());
            self.AddAudioControl(view, () => self.RenderSoloSelection());
            if (self.InspectedSave != null) { self.RenderSaveInspection(view); return; }
            if (self.Page == SoloEntryPage.Chapters && !self.StartPreviewVisible)
            {
                var chapter = Tables.Instance.DTRestaurantChapterConfig.Get(1);
                self.PreviewPersistent = !Application.isMobilePlatform;
                self.PreviewMenuVariant = chapter.MenuVariant;
                self.PreviewChapterId = chapter.Id;
                self.StartPreviewVisible = true;
            }
            if (self.StartPreviewVisible) { self.RenderSoloStartPreview(view); return; }
            if (self.Page == SoloEntryPage.Home)
            {
                view.ClockText.text = "单机 / 开始";
                view.SettlementText.text = "第一次来？从「家常开张」开始，先学会一碗汤，再逐关加入吐司和沙拉。\n已有记录可明确选档继续；独立菜单集中在「菜单试玩」。";
                var first = Tables.Instance.DTRestaurantChapterConfig.Get(1);
                view.AddControl("开始第一章 · " + first.Name, () =>
                {
                    SoloSavePresentation current = original;
                    current?.PreviewSoloStart(!Application.isMobilePlatform, first.MenuVariant, first.Id);
                });
                self.AddSoloPage(view, "选择其他章节", SoloEntryPage.Chapters);
                self.AddSoloPage(view, self.Headers.Length > 0 ? "继续记录" : "查看记录", SoloEntryPage.Records);
                self.AddSoloPage(view, "菜单试玩", SoloEntryPage.TrialMenus);
                view.AddControl("返回模式选择", () =>
                {
                    SoloSavePresentation current = original;
                    if (current != null)
                    {
                        ProductBootstrapBridge.ReturnToModeMenu(current.Root().GetComponent<ProductRuntimeComponent>().Config);
                    }
                });
                return;
            }
            self.AddSoloPage(view, "返回单机首页", SoloEntryPage.Home);
            if (self.Page == SoloEntryPage.Records)
            {
                view.ClockText.text = "单机 / 记录";
                self.AddSaveFilters(view);
            }
            if (self.LabelsFailure != null)
            {
                view.StatusText.text = self.LabelsFailure;
            }
            if (self.Page == SoloEntryPage.TrialMenus)
            {
            view.ClockText.text = "单机 / 菜单试玩";
            view.SettlementText.text = "独立菜单用于单独体验原有制作链。\n正式章节请返回单机首页选择；各入口仍会预览并说明是否保存。";
            if (!Application.isMobilePlatform)
                view.AddControl("新开餐厅", () => { SoloSavePresentation current = original; current?.PreviewSoloStart(true, current.NewMenuVariant); });
            view.AddControl("内存试玩", () => { SoloSavePresentation current = original; current?.PreviewSoloStart(false, current.NewMenuVariant); });
            view.AddControl("新开沙拉餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.PreviewSoloStart(!Application.isMobilePlatform, ProductMenuVariant.Salad);
                }
            });
            view.AddControl("新开经营成长餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.PreviewSoloStart(!Application.isMobilePlatform, ProductMenuVariant.Growth);
                }
            });
            view.AddControl("新开热餐餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.PreviewSoloStart(!Application.isMobilePlatform, ProductMenuVariant.Hot);
                }
            });
            view.AddControl("新开甜品餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.PreviewSoloStart(!Application.isMobilePlatform, ProductMenuVariant.Dessert);
                }
            });
            view.AddControl("新开咖啡餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.PreviewSoloStart(!Application.isMobilePlatform, ProductMenuVariant.Cafe);
                }
            });
            view.AddControl("新开蒸食餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.PreviewSoloStart(!Application.isMobilePlatform, ProductMenuVariant.Steamed);
                }
            });
            view.AddControl("新开茶饮餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.PreviewSoloStart(!Application.isMobilePlatform, ProductMenuVariant.Tea);
                }
            });
            }
            if (self.Page == SoloEntryPage.Chapters)
            {
            view.ClockText.text = "单机 / 选择章节";
            view.SettlementText.text = "章节按各自已有菜单与关链开始；选择后先预览，确认后才建立餐厅。";
            foreach (var chapter in Tables.Instance.DTRestaurantChapterConfig.DataList.OrderBy(row => row.Id))
            {
                int id = chapter.Id; ProductMenuVariant menu = chapter.MenuVariant;
                view.AddControl($"新开大关 {id} · {chapter.Name}", () =>
                {
                    SoloSavePresentation current = original;
                    if (current != null)
                    {
                        current.PreviewSoloStart(!Application.isMobilePlatform, menu, id);
                    }
                });
            }
            }
            if (self.Page != SoloEntryPage.Records)
            {
                return;
            }
            view.AddControl("刷新记录", () => { SoloSavePresentation current = original; current?.BrowseSoloSelection(); });
            view.AddControl("返回模式选择", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    ProductBootstrapBridge.ReturnToModeMenu(current.Root().GetComponent<ProductRuntimeComponent>().Config);
                }
            });
            if (self.RecordState != SoloRecordReadState.Ready)
            {
                view.SettlementText.text = self.RecordState == SoloRecordReadState.Failed
                    ? "记录读取失败，尚未创建餐厅。请刷新重试。\n" + self.Failure : "正在读取本机记录…";
                return;
            }
            for (int index = 0; index < self.Headers.Length; ++index)
            {
                var header = self.Headers[index];
                if (self.SaveFilter == SoloSaveListFilter.Continue && header.RecordKind != SoloSaveRecordKind.Continue ||
                    self.SaveFilter == SoloSaveListFilter.Completed && header.RecordKind != SoloSaveRecordKind.ChapterCompleted ||
                    self.SaveFilter == SoloSaveListFilter.Favorites && self.FindSaveLabel(header.SlotId)?.Favorite != true)
                {
                    continue;
                }
                string slot = header.SlotId;
                string label = $"{(header.RecordKind == SoloSaveRecordKind.ChapterCompleted ? "查看完成" : "继续")} 记录{index + 1} · {(header.RecordKind == SoloSaveRecordKind.ChapterCompleted ? RestaurantChapters.Label(header.CompletedLevelId) + " · 已完成" : RestaurantChapters.Label(header.NextLevelId))} · {header.Stars}星 · {new DateTime(header.SavedUtcTicks > 0 && header.SavedUtcTicks <= DateTime.MaxValue.Ticks ? header.SavedUtcTicks : 1).ToLocalTime():MM-dd HH:mm}";
                var metadata = self.FindSaveLabel(slot);
                if (metadata?.Name != null && metadata.Name != "")
                {
                    label = metadata.Name + " · " + label;
                }
                if (metadata?.Favorite == true)
                {
                    label = "★ " + label;
                }
                view.AddTarget(label, () => { SoloSavePresentation current = original; current?.ChooseSoloSave(slot); });
            }
            if (self.Headers.Length == 0) view.SettlementText.text += "\n当前没有可继续的记录";
        }

        private static void AddSoloPage(this SoloSavePresentation self, MonoUIGridKitchen view, string caption, SoloEntryPage page)
        {
            EntityRef<SoloSavePresentation> original = self;
            view.AddControl(caption, () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.NavigateSoloPage(page);
                }
            });
        }

        public static void NavigateSoloPage(this SoloSavePresentation self, SoloEntryPage page)
        {
            self.Page = page;
            self.StartPreviewVisible = false;
            self.RenderSoloSelection();
        }

        public static void ChooseSoloNewGame(this SoloSavePresentation self, bool persistent, int contentChapterId = 0)
        {
            if (self.Started || self.LoadingSelection || ((UIGridKitchen)self.Hud)?.Available != true) return;
            try
            {
                ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
                string profile = persistent ? self.Repository().SaveProfile() : null;
                self.Started = true; self.CloseSoloSelection();
                if (persistent)
                {
                    runtime.InitializeSavedProduct(profile, menuVariant: self.NewMenuVariant, contentChapterId: contentChapterId);
                }
                else
                {
                    runtime.InitializeProduct(menuVariant: self.NewMenuVariant, contentChapterId: contentChapterId);
                }
            }
            catch (Exception e) { self.Started = false; self.Failure = e.Message; self.OpenSoloSelection(); }
        }

        public static void ChooseSoloSave(this SoloSavePresentation self, string slot)
        {
            self.Page = SoloEntryPage.Records;
            self.ReadSoloSave(slot, false);
        }

        private static void ReadSoloSave(this SoloSavePresentation self, string slot, bool continuing)
        {
            UIGridKitchen hud = self.Hud;
            if (self.Started || self.LoadingSelection || hud?.Available != true) return;
            SoloSaveHeader expected = self.InspectedSave;
            if (continuing && (expected?.SlotId != slot || expected.RecordKind != SoloSaveRecordKind.Continue))
            {
                return;
            }
            self.LabelDraft = null;
            ++self.BrowseSerial;
            self.LoadingSelection = true; self.Failure = null; self.RenderSoloSelection();
            EntityRef<SoloSavePresentation> original = self; long epoch = self.Epoch;
            hud.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    var repository = self.Repository();
                    using var reply = (SoloSaveSelectResponse)await self.Root().GetComponent<ProcessInnerSender>().Call(repository.GetActorId(), ProductMessages.Create<SoloSaveSelectRequest>(r => r.SlotId = slot), false);
                    context.ThrowIfStopped(); self = original;
                    if (self == null || self.Epoch != epoch || self.Started) return;
                    if (reply.Error != 0) throw new InvalidOperationException(reply.Message);
                    var header = (SoloSaveHeader)MemoryPackHelper.Deserialize(typeof(SoloSaveHeader), reply.Header, 0, reply.Header.Length);
                    var payload = (SoloSavePayload)MemoryPackHelper.Deserialize(typeof(SoloSavePayload), reply.Payload, 0, reply.Payload.Length);
                    ProductRuntimeComponent runtime = self.Root().GetComponent<ProductRuntimeComponent>();
                    // Guard all value data again after await, before closing the selector or creating Host.
                    SoloSaveValidation.Payload(header, payload, repository.SaveProfile());
                    if (!continuing || !SameSelectedSave(expected, header) || header.RecordKind == SoloSaveRecordKind.ChapterCompleted)
                    {
                        self.InspectedSave = header;
                        self.SaveInspectionRows = SaveInspectionRows(header, payload);
                        self.ViewedResult = header.RecordKind == SoloSaveRecordKind.ChapterCompleted ? header : null;
                        self.Failure = continuing ? "RecordChanged" : null;
                        self.LoadingSelection = false;
                        self.RenderSoloSelection();
                        return;
                    }
                    self.Started = true; self.CloseSoloSelection(); runtime.InitializeSavedProduct(repository.SaveProfile(), header, payload);
                }
                catch (OperationCanceledException) { }
                catch (Exception e) { self = original; if (self != null && self.Epoch == epoch) { self.ClearSaveInspection(); self.LoadingSelection = false; self.Failure = e.Message; self.RenderSoloSelection(); } }
            });
        }
    }

    [Event(SceneType.ProductRoot)]
    [FriendOf(typeof(ProductRuntimeComponent))]
    [FriendOf(typeof(SoloSavePresentation))]
    public sealed class SoloSaveSelectionView : AEvent<Scene, SoloSaveSelectionRequested>
    {
        protected override UniTask Run(Scene scene, SoloSaveSelectionRequested args)
        {
            ProductRuntimeComponent runtime = scene.GetComponent<ProductRuntimeComponent>();
            if (args.SlotId != null && (runtime.Config.Role != RuntimeInstanceRole.Solo ||
                (ClientSessionComponent)runtime.Client != null || (Scene)runtime.HostScene != null))
            {
                return UniTask.CompletedTask;
            }
            scene.RemoveComponent<SoloSavePresentation>();
            var selection = scene.AddComponent<SoloSavePresentation>();
            if (args.ShowRecords || args.SlotId != null)
            {
                selection.Page = SoloEntryPage.Records;
            }
            if (args.SlotId != null)
            {
                selection.NavigateSoloRecord(args.SlotId, args.Revision);
            }
            return UniTask.CompletedTask;
        }
    }
}
