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
            self.Hud = default; self.Headers = Array.Empty<SoloSaveHeader>(); self.Failure = null; self.Epoch = 0;
            self.ViewedResult = null;
            self.NewMenuVariant = 1;
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

        private static void BrowseSoloSelection(this SoloSavePresentation self)
        {
            UIGridKitchen hud = self.Hud; if (hud?.Available != true) return;
            EntityRef<SoloSavePresentation> original = self; long epoch = self.Epoch;
            hud.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    var repository = self.Repository();
                    using var reply = (SoloSaveBrowseResponse)await self.Root().GetComponent<ProcessInnerSender>().Call(repository.GetActorId(), ProductMessages.Create<SoloSaveBrowseRequest>(_ => { }), false);
                    context.ThrowIfStopped(); self = original;
                    if (self == null || self.Epoch != epoch || self.Started) return;
                    if (reply.Error != 0) throw new InvalidOperationException(reply.Message);
                    self.Headers = reply.Headers.Select(bytes => (SoloSaveHeader)MemoryPackHelper.Deserialize(typeof(SoloSaveHeader), bytes, 0, bytes.Length)).ToArray();
                    self.Failure = null; self.RenderSoloSelection();
                }
                catch (OperationCanceledException) { }
                catch (Exception e) { self = original; if (self != null && self.Epoch == epoch) { self.Failure = e.Message; self.RenderSoloSelection(); } }
            });
        }

        private static void RenderSoloSelection(this SoloSavePresentation self)
        {
            UIGridKitchen hud = self.Hud; if (hud?.Available != true || self.Started) return;
            var view = hud.View; view.ClearTargets(); view.ShowPanels(false, true); view.CameraDragAreaRectTransform.gameObject.SetActive(false);
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
            view.SetInputGuard(() => { SoloSavePresentation current = original; return current != null && current.Epoch == epoch && !current.Started && ((UIGridKitchen)hudRef)?.Available == true; });
            if (self.LoadingSelection) { view.AddControl("取消加载", () => { SoloSavePresentation current = original; current?.CancelSoloSelection(); }); return; }
            if (!Application.isMobilePlatform)
                view.AddControl("新开餐厅", () => { SoloSavePresentation current = original; current?.ChooseSoloNewGame(true); });
            view.AddControl("内存试玩", () => { SoloSavePresentation current = original; current?.ChooseSoloNewGame(false); });
            view.AddControl("新开沙拉餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.NewMenuVariant = (int)ProductMenuVariant.Salad;
                    current.ChooseSoloNewGame(!Application.isMobilePlatform);
                }
            });
            view.AddControl("新开经营成长餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.NewMenuVariant = (int)ProductMenuVariant.Growth;
                    current.ChooseSoloNewGame(!Application.isMobilePlatform);
                }
            });
            view.AddControl("新开热餐餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.NewMenuVariant = (int)ProductMenuVariant.Hot;
                    current.ChooseSoloNewGame(!Application.isMobilePlatform);
                }
            });
            view.AddControl("新开甜品餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.NewMenuVariant = (int)ProductMenuVariant.Dessert;
                    current.ChooseSoloNewGame(!Application.isMobilePlatform);
                }
            });
            view.AddControl("新开咖啡餐厅", () =>
            {
                SoloSavePresentation current = original;
                if (current != null)
                {
                    current.NewMenuVariant = (int)ProductMenuVariant.Cafe;
                    current.ChooseSoloNewGame(!Application.isMobilePlatform);
                }
            });
            foreach (var chapter in Tables.Instance.DTRestaurantChapterConfig.DataList.OrderBy(row => row.Id))
            {
                int id = chapter.Id; int menu = (int)chapter.MenuVariant;
                view.AddControl($"新开大关 {id} · {chapter.Name}", () =>
                {
                    SoloSavePresentation current = original;
                    if (current != null)
                    {
                        current.NewMenuVariant = menu;
                        current.ChooseSoloNewGame(!Application.isMobilePlatform, id);
                    }
                });
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
            for (int index = 0; index < self.Headers.Length; ++index)
            {
                var header = self.Headers[index];
                string slot = header.SlotId;
                string label = $"{(header.RecordKind == SoloSaveRecordKind.ChapterCompleted ? "查看完成" : "继续")} 记录{index + 1} · {(header.RecordKind == SoloSaveRecordKind.ChapterCompleted ? RestaurantChapters.Label(header.CompletedLevelId) + " · 已完成" : RestaurantChapters.Label(header.NextLevelId))} · {header.Stars}星 · {new DateTime(header.SavedUtcTicks > 0 && header.SavedUtcTicks <= DateTime.MaxValue.Ticks ? header.SavedUtcTicks : 1).ToLocalTime():MM-dd HH:mm}";
                view.AddTarget(label, () => { SoloSavePresentation current = original; current?.ChooseSoloSave(slot); });
            }
            if (self.Headers.Length == 0) view.SettlementText.text += "\n当前没有可继续的记录";
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
            UIGridKitchen hud = self.Hud;
            if (self.Started || self.LoadingSelection || hud?.Available != true) return;
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
                    if (header.RecordKind == SoloSaveRecordKind.ChapterCompleted)
                    {
                        self.ViewedResult = header; self.LoadingSelection = false; self.RenderSoloSelection(); return;
                    }
                    self.Started = true; self.CloseSoloSelection(); runtime.InitializeSavedProduct(repository.SaveProfile(), header, payload);
                }
                catch (OperationCanceledException) { }
                catch (Exception e) { self = original; if (self != null && self.Epoch == epoch) { self.LoadingSelection = false; self.Failure = e.Message; self.RenderSoloSelection(); } }
            });
        }
    }

    [Event(SceneType.ProductRoot)]
    public sealed class SoloSaveSelectionView : AEvent<Scene, SoloSaveSelectionRequested>
    {
        protected override UniTask Run(Scene scene, SoloSaveSelectionRequested args)
        { scene.RemoveComponent<SoloSavePresentation>(); scene.AddComponent<SoloSavePresentation>(); return UniTask.CompletedTask; }
    }
}
