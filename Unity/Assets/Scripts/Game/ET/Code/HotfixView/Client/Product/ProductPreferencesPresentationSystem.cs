using System;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(GridPresentationComponent))]
    [FriendOf(typeof(ProductEntryPresentation))]
    [FriendOf(typeof(SoloSavePresentation))]
    [FriendOf(typeof(SoloSaveRepository))]
    public static class ProductPreferencesPresentationSystem
    {
        public static SoloSaveRepository PreferencesRepository(Entity owner)
        {
            var repository = owner.Root().GetComponent<SoloSaveRepository>();
            if (repository != null)
            {
                return repository;
            }
            string directory = Application.persistentDataPath;
            return owner.Root().AddComponent<SoloSaveRepository, string, string>(directory, SoloSaveRepositorySystem.LocalProfile(directory));
        }

        public static void InitializePreferences(this Entity owner, string applicationDirectory = null)
        {
            try
            {
                if (owner.Root().GetComponent<SoloSaveRepository>() == null)
                {
                    string directory = applicationDirectory ?? Application.persistentDataPath;
                    string profile = SoloSaveRepositorySystem.ExistingLocalProfile(directory);
                    if (profile != null)
                    {
                        owner.Root().AddComponent<SoloSaveRepository, string, string>(directory, profile);
                    }
                }
            }
            catch (Exception error)
            {
                // 设置不可读不阻止内存餐厅；玩家打开设置时仍会得到具体错误。
                Log.Warning("Local preferences unavailable: " + error.Message);
            }
        }

        private static ProductPreferencesDraft Draft(Entity owner) => owner switch
        {
            GridPresentationComponent grid => grid.PreferencesDraft,
            ProductEntryPresentation entry => entry.PreferencesDraft,
            SoloSavePresentation solo => solo.PreferencesDraft,
            _ => null
        };

        private static void SetDraft(Entity owner, ProductPreferencesDraft draft)
        {
            switch (owner)
            {
                case GridPresentationComponent grid:
                    grid.PreferencesDraft = draft;
                    grid.HudTargetsValid = grid.HudInformationValid = false;
                    grid.HelpDisplayedSerial = grid.ActionAdviceDisplayedSerial = long.MinValue;
                    break;
                case ProductEntryPresentation entry: entry.PreferencesDraft = draft; break;
                case SoloSavePresentation solo: solo.PreferencesDraft = draft; break;
            }
        }

        private static UIGridKitchen Hud(Entity owner) => owner switch
        {
            GridPresentationComponent grid => grid.Hud,
            ProductEntryPresentation entry => entry.Hud,
            SoloSavePresentation solo => solo.Hud,
            _ => null
        };

        public static void ApplySavedPreferences(this Entity owner, MonoUIGridKitchen view)
        {
            var value = owner.Root().GetComponent<SoloSaveRepository>()?.Preferences;
            view.ApplyLocalFontSize(value?.FontSize ?? ProductFontSize.Standard);
        }

        public static void AddPreferencesControl(this Entity owner, MonoUIGridKitchen view, Action refresh)
        {
            EntityRef<Entity> original = owner;
            view.AddControl("本机设置", () =>
            {
                Entity current = original;
                if (current == null)
                {
                    return;
                }
                if (current is GridPresentationComponent grid && grid.OverviewPage == GridOverviewPage.Items && view.EntryAddressVisible)
                {
                    grid.ItemLocatorInput = view.EntryAddress;
                }
                var draft = new ProductPreferencesDraft { Value = new ProductPreferencesValue(), Busy = true, Feedback = "正在读取本机设置。" };
                SetDraft(current, draft);
                refresh();
                Load(current, Hud(current), draft, refresh);
            });
        }

        public static bool RenderPreferences(this Entity owner, UIGridKitchen hud, Action refresh)
        {
            var draft = Draft(owner);
            if (draft == null || hud?.Available != true)
            {
                return false;
            }
            var view = hud.View;
            owner.ApplySavedPreferences(view);
            if (!draft.Dirty)
            {
                return true;
            }
            draft.Dirty = false;
            view.ClearTargets();
            view.ShowPanels(false, false);
            view.CameraDragAreaRectTransform.gameObject.SetActive(false);
            view.SetInformation("本机设置 · 草稿", "字号和拖动只影响本设备；设置不会暂停餐厅。\n选择参数后明确保存；取消不改变已保存值。\n经营提醒开关为到货、加工完成、订单临期；阈值使用业务秒。\n" +
                (draft.Feedback ?? "尚未保存本轮草稿。"), true, false);
            view.PhaseText.text = "本机设置";
            view.ClockText.text = draft.Busy ? "正在保存" : "编辑草稿";
            view.SetScore("不改餐厅进度");
            view.StatusText.text = draft.Feedback ?? "选择阅读与操作习惯";
            view.HandText.text = "取消返回原页面";
            view.WorkText.text = "";
            view.SettlementText.text = "";
            view.HintText.text = "保存成功后应用；恢复默认仍需明确保存。";
            EntityRef<Entity> original = owner;
            EntityRef<UIGridKitchen> originalHud = hud;
            long render = ++draft.Render;
            view.SetInputGuard(() =>
            {
                Entity current = original;
                UIGridKitchen currentHud = originalHud;
                return current != null && currentHud?.Available == true && Hud(current) == currentHud && Draft(current) == draft && draft.Render == render;
            });
            if (draft.Busy)
            {
                return true;
            }
            void Change(Action<ProductPreferencesValue> action)
            {
                action(draft.Value);
                draft.Feedback = null;
                draft.Dirty = true;
                refresh();
            }
            foreach (ProductFontSize size in Enum.GetValues(typeof(ProductFontSize)))
            {
                string name = size switch { ProductFontSize.Larger => "较大", ProductFontSize.Largest => "大号", _ => "标准" };
                view.AddTarget("字号：" + name + (draft.Value.FontSize == size ? " · 已选" : ""), () => Change(value => value.FontSize = size));
            }
            foreach (ProductDragSpeed speed in Enum.GetValues(typeof(ProductDragSpeed)))
            {
                string name = speed switch { ProductDragSpeed.Slow => "慢", ProductDragSpeed.Fast => "快", _ => "标准" };
                view.AddTarget("拖动：" + name + (draft.Value.DragSpeed == speed ? " · 已选" : ""), () => Change(value => value.DragSpeed = speed));
            }
            foreach (var option in new[] { ProductNoticeOptions.Arrivals, ProductNoticeOptions.Processing, ProductNoticeOptions.Deadlines })
            {
                string name = option switch { ProductNoticeOptions.Arrivals => "到货", ProductNoticeOptions.Processing => "完工", _ => "临期" };
                view.AddTarget(name + "提醒：" + ((draft.Value.Notices & option) != 0 ? "开" : "关"), () => Change(value => value.Notices ^= option));
            }
            foreach (int seconds in new[] { 3, 5, 10 })
            {
                view.AddTarget("临期阈值：" + seconds + " 秒" + (draft.Value.DeadlineSeconds == seconds ? " · 已选" : ""), () => Change(value => value.DeadlineSeconds = seconds));
            }
            view.AddControl("恢复设置默认", () =>
            {
                draft.Value = new ProductPreferencesValue();
                draft.Feedback = "默认草稿，尚未保存。";
                draft.Dirty = true;
                refresh();
            });
            view.AddControl("取消本机设置", () =>
            {
                Entity current = original;
                if (current != null)
                {
                    SetDraft(current, null);
                    refresh();
                }
            });
            view.AddControl("保存本机设置", () => Save(owner, hud, draft, refresh));
            return true;
        }

        private static void Load(Entity owner, UIGridKitchen hud, ProductPreferencesDraft draft, Action refresh)
        {
            EntityRef<Entity> original = owner;
            EntityRef<UIGridKitchen> originalHud = hud;
            hud.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    var repository = PreferencesRepository(owner);
                    var result = await context.CallLocalValueAsync(owner.Root().GetComponent<ProcessInnerSender>(), repository.GetActorId(), ProductPreferencesReadRequest.Create(),
                        response => (Error: response.Error == 0 ? null : response.Message,
                            Value: SoloSaveRepositorySystem.DecodePreferences(((ProductPreferencesReadResponse)response).Values)));
                    Entity current = original;
                    if (current != null && Hud(current) == (UIGridKitchen)originalHud && Draft(current) == draft)
                    {
                        draft.Value = result.Value;
                        draft.Feedback = result.Error;
                        draft.Busy = false;
                        draft.Dirty = true;
                        refresh();
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception error)
                {
                    Entity current = original;
                    if (current != null && Hud(current) == (UIGridKitchen)originalHud && Draft(current) == draft)
                    {
                        draft.Feedback = "无法读取本机设置：" + error.Message;
                        draft.Busy = false;
                        draft.Dirty = true;
                        refresh();
                    }
                }
            });
        }

        private static void Save(Entity owner, UIGridKitchen hud, ProductPreferencesDraft draft, Action refresh)
        {
            if (draft.Busy)
            {
                return;
            }
            draft.Busy = true;
            draft.Dirty = true;
            EntityRef<Entity> original = owner;
            EntityRef<UIGridKitchen> originalHud = hud;
            refresh();
            hud.GetAsyncOwner().Run(async context =>
            {
                try
                {
                    SoloSaveRepository repository = PreferencesRepository(owner);
                    var request = ProductPreferencesWriteRequest.Create();
                    request.Values = MemoryPackHelper.Serialize(draft.Value);
                    var result = await context.CallLocalValueAsync(owner.Root().GetComponent<ProcessInnerSender>(), repository.GetActorId(), request,
                        response => (Error: response.Error == 0 ? null : response.Message ?? "保存失败", Value: response.Error == 0
                            ? SoloSaveRepositorySystem.DecodePreferences(((ProductPreferencesWriteResponse)response).Values) : null));
                    Entity current = original;
                    if (current == null || Hud(current) != (UIGridKitchen)originalHud || Draft(current) != draft)
                    {
                        return;
                    }
                    draft.Busy = false;
                    draft.Dirty = true;
                    draft.Feedback = result.Error ?? "本机设置已保存。";
                    if (result.Error == null)
                    {
                        draft.Value = result.Value;
                    }
                    refresh();
                }
                catch (OperationCanceledException) { }
                catch (Exception error)
                {
                    Entity current = original;
                    if (current != null && Hud(current) == (UIGridKitchen)originalHud && Draft(current) == draft)
                    {
                        draft.Busy = false;
                        draft.Dirty = true;
                        draft.Feedback = "保存失败，原设置保留：" + error.Message;
                        refresh();
                    }
                }
            });
        }
    }
}
