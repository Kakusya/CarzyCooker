using System;
using Game;
using GameFramework;
using GameFramework.Setting;
using UnityEngine;
using GameEntry = Game.GameEntry;

namespace ET.Client
{
    [FriendOf(typeof(GridPresentationComponent))]
    [FriendOf(typeof(ProductEntryPresentation))]
    [FriendOf(typeof(SoloSavePresentation))]
    public static class ProductAudioSettings
    {
        public const string Group = "Sound.Default";
        public static string MutedKey() => string.Format(Constant.Setting.SoundGroupMuted, Group);
        public static string VolumeKey() => string.Format(Constant.Setting.SoundGroupVolume, Group);

        public static void ApplySaved()
        {
            var group = GameEntry.Sound.GetSoundGroup(Group);
            if (group == null)
            {
                throw new InvalidOperationException("Product sound group is missing: " + Group);
            }
            group.Mute = GameEntry.Setting.GetBool(MutedKey(), false);
            group.Volume = Mathf.Clamp01(GameEntry.Setting.GetFloat(VolumeKey(), .6f));
        }

        private static ProductAudioPage Page(Entity owner) => owner switch
        {
            ProductEntryPresentation entry => entry.AudioPage,
            SoloSavePresentation solo => solo.AudioPage,
            GridPresentationComponent kitchen => kitchen.AudioPage,
            _ => throw new ArgumentOutOfRangeException(nameof(owner))
        };
        private static void SetPage(Entity owner, ProductAudioPage page)
        {
            switch (owner)
            {
                case ProductEntryPresentation entry: entry.AudioPage = page; break;
                case SoloSavePresentation solo: solo.AudioPage = page; break;
                case GridPresentationComponent kitchen:
                    kitchen.AudioPage = page;
                    kitchen.HudTargetsValid = kitchen.HudInformationValid = false;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(owner));
            }
        }
        private static UIGridKitchen Hud(Entity owner) => owner switch
        {
            ProductEntryPresentation entry => entry.Hud,
            SoloSavePresentation solo => solo.Hud,
            GridPresentationComponent kitchen => kitchen.Hud,
            _ => null
        };

        public static void AddAudioControl(this Entity owner, MonoUIGridKitchen view, Action refresh)
        {
            EntityRef<Entity> original = owner;
            view.AddControl("声音", () =>
            {
                Entity current = original;
                if (current != null)
                {
                    SetPage(current, new ProductAudioPage()); refresh();
                }
            });
        }

        public static bool RenderAudio(this Entity owner, UIGridKitchen hud, Action refresh)
        {
            ProductAudioPage page = Page(owner);
            if (page == null)
            {
                return false;
            }
            if (!page.Dirty)
            {
                return true;
            }
            page.Dirty = false;
            MonoUIGridKitchen view = hud.View;
            view.ClearTargets(); view.ShowPanels(false, false);
            view.CameraDragAreaRectTransform.gameObject.SetActive(false);
            bool muted = GameEntry.Sound.IsMuted(Group);
            int volume = Mathf.RoundToInt(GameEntry.Sound.GetVolume(Group) * 100);
            view.SetInformation("声音 · 本机即时生效", "音效静音和音量即时生效并保存到本机。\n只控制经营音效；不改变其他玩家或经营进度。\n字体/经营提醒仍在原设置页明确保存，取消其草稿不会撤销声音。\n" +
                (page.Feedback ?? "当前音量 " + volume + "% · " + (muted ? "已静音" : "未静音")), true, false);
            view.PhaseText.text = "声音"; view.ClockText.text = "即时生效"; view.ScoreText.text = volume + "%";
            view.StatusText.text = page.Feedback ?? "本机经营音效";
            view.HintText.text = "音效与可关闭的视觉提醒分别控制";
            view.HandText.text = "默认音量60%，未静音";
            EntityRef<Entity> original = owner;
            EntityRef<UIGridKitchen> originalHud = hud;
            long render = ++page.Render;
            view.SetInputGuard(() =>
            {
                Entity current = original;
                UIGridKitchen currentHud = originalHud;
                return current != null && currentHud?.Available == true && Hud(current) == currentHud && Page(current) == page && page.Render == render;
            });
            void Change(Action apply)
            {
                try
                {
                    apply();
                    // SettingComponent.Save discards this existing manager's bool result.
                    page.Feedback = GameFrameworkEntry.GetModule<ISettingManager>().Save()
                        ? "声音已即时生效并保存到本机。" : "当前声音已生效，但本机保存失败；可重试保存。";
                }
                catch (Exception error) { page.Feedback = "本机声音保存失败：" + error.Message + "；请重试保存。"; }
                page.Dirty = true;
                refresh();
            }
            view.AddTarget("音效静音：" + (muted ? "开" : "关"), () => Change(() => GameEntry.Sound.Mute(Group, !muted)));
            for (int percent = 0; percent <= 100; percent += 10)
            {
                int target = percent;
                view.AddTarget("音效音量：" + target + "%" + (volume == target ? " · 已选" : ""), () => Change(() => GameEntry.Sound.SetVolume(Group, target / 100f)));
            }
            view.AddControl("重试保存声音", () => Change(() => { }));
            view.AddControl("返回声音前页面", () =>
            {
                Entity current = original;
                if (current != null)
                {
                    SetPage(current, null); refresh();
                }
            });
            return true;
        }
    }
}
