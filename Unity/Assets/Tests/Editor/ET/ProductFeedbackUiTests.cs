#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using ET.Client;
using Game;
using GameFramework.Event;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using GameEntry = Game.GameEntry;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private ProductFeedbackComponent Feedback => ((Scene)Client.IScene).GetComponent<ProductFeedbackComponent>();

        [UnityTest, Category("ProductFeedback")]
        public IEnumerator ProductFeedbackActualMissingResourceKeepsProjectionAndNextCommandWorking()
        {
            yield return StartLauncher();
            long command = Client.NextCommandId;
            long tick = Grid.Tick;
            var playback = new ProductFeedbackPlayback { Kind = ProductFeedbackKind.Pickup };
            Feedback.Playing.Add(playback);
            const string missingAsset = "Assets/Res/Sound/Product/OwnedMissingResourceTest.wav";
            Assert.That(File.Exists(missingAsset), Is.False);
            playback.Serial = GameEntry.Sound.PlaySound(missingAsset, ProductAudioSettings.Group, userData: playback);
            yield return Wait(() => Feedback.LoadFailure != null, "Actual GF resource failure reaches the current owner");
            Assert.That(Feedback.LoadFailure, Does.Contain(missingAsset));
            Assert.That(Feedback.Playing.Contains(playback), Is.False);
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "A failed sound never submits a game command");
            Assert.That(Grid.Tick, Is.GreaterThanOrEqualTo(tick));
            Assert.That(Presentation.Ready, Is.True);
            yield return ReadableCommand("拿 碗台 ·", true);
            yield return Wait(() => Grid.LastStatus == GridResultStatus.Executed, "The original command still succeeds after an actual sound load failure");
            Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Not.Zero);
        }

        [UnityTest, Category("ProductFeedback")]
        public IEnumerator ProductFeedbackColdLoadingOwnerExitCancelsBeforeAnyPlaybackCallback()
        {
            yield return StartLauncher();
            var group = GameEntry.Sound.GetSoundGroup(ProductAudioSettings.Group);
            bool muted = group.Mute; float volume = group.Volume;
            int callbacks = 0;
            ProductFeedbackPlayback playback = null;
            EventHandler<GameEventArgs> loaded = (_, args) =>
            {
                if (((PlaySoundSuccessEventArgs)args).UserData == playback)
                {
                    ++callbacks;
                }
            };
            GameEntry.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, loaded);
            try
            {
                group.Mute = false; group.Volume = .6f;
                var owner = Feedback;
                owner.EmitFeedback(ProductFeedbackKind.LevelComplete);
                playback = owner.Playing.Single();
                int serial = playback.Serial;
                Assert.That(GameEntry.Sound.IsLoadingSound(serial), Is.True, "A cold GF resource is still in the actual editor load queue");
                Runtime.StopProduct();
                yield return Wait(() => !GameEntry.Sound.IsLoadingSound(serial), "The cancelled real GF load reaches its terminal callback");
                yield return null;
                Assert.That(callbacks, Is.Zero, "An owner retired while loading never starts its old clip");
                Assert.That(owner.IsDisposed, Is.True);
            }
            finally
            {
                GameEntry.Event.Unsubscribe(PlaySoundSuccessEventArgs.EventId, loaded);
                group.Mute = muted; group.Volume = volume;
            }
        }

        [UnityTest, Category("ProductFeedback")]
        public IEnumerator ProductFeedbackSixActualResourcesPlayAndOwnedStopPreservesAnotherSerial()
        {
            yield return StartLauncher();
            var group = GameEntry.Sound.GetSoundGroup(ProductAudioSettings.Group);
            bool muted = group.Mute; float volume = group.Volume;
            var loaded = new HashSet<string>();
            GameFramework.Sound.ISoundAgent unrelated = null;
            object marker = new object();
            EventHandler<GameEventArgs> handler = (_, args) =>
            {
                var value = (PlaySoundSuccessEventArgs)args;
                if (value.SoundAssetName.StartsWith("Assets/Res/Sound/Product/"))
                {
                    Assert.That(value.SoundAgent.Length, Is.GreaterThan(.07f));
                    loaded.Add(Path.GetFileNameWithoutExtension(value.SoundAssetName));
                }
                if (value.UserData == marker)
                {
                    unrelated = value.SoundAgent;
                }
            };
            GameEntry.Event.Subscribe(PlaySoundSuccessEventArgs.EventId, handler);
            try
            {
                group.Mute = false; group.Volume = .6f;
                foreach (var kind in new[] { ProductFeedbackKind.Pickup, ProductFeedbackKind.Place, ProductFeedbackKind.WorkStart, ProductFeedbackKind.WorkFinished, ProductFeedbackKind.Serve, ProductFeedbackKind.LevelComplete })
                {
                    Feedback.EmitFeedback(kind);
                    yield return Wait(() => loaded.Contains(kind.ToString()), "GF actually loads and starts " + kind);
                    Assert.That(Feedback.LoadFailure, Is.Null);
                    Feedback.StopFeedback();
                }
                Assert.That(loaded.Count, Is.EqualTo(6));
                long command = Client.NextCommandId;
                Feedback.EmitFeedback(ProductFeedbackKind.Pickup);
                Feedback.EmitFeedback(ProductFeedbackKind.Place);
                Feedback.EmitFeedback(ProductFeedbackKind.WorkStart);
                Assert.That(Feedback.Playing.Count, Is.EqualTo(3));
                int pickups = Feedback.Requested[(int)ProductFeedbackKind.Pickup];
                Feedback.EmitFeedback(ProductFeedbackKind.Pickup);
                Assert.That(Feedback.Playing.Count, Is.EqualTo(3));
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.Pickup], Is.EqualTo(pickups), "A lower-priority cue is dropped instead of queued");
                Feedback.EmitFeedback(ProductFeedbackKind.Serve);
                Assert.That(Feedback.Playing.Count, Is.EqualTo(3));
                Assert.That(Feedback.Playing.Any(value => value.Kind == ProductFeedbackKind.Pickup), Is.False, "Successful delivery preempts the weakest owned cue");
                Feedback.StopFeedback(); yield return null;
                Assert.That(Feedback.Playing, Is.Empty);
                Assert.That(Client.NextCommandId, Is.EqualTo(command), "Audio policy sends no restaurant commands");
                int external = GameEntry.Sound.PlaySound("Assets/Res/Sound/Product/LevelComplete.wav", ProductAudioSettings.Group, userData: marker);
                yield return Wait(() => unrelated != null && unrelated.IsPlaying, "An unrelated serial is really playing");
                Feedback.EmitFeedback(ProductFeedbackKind.Pickup);
                Feedback.StopFeedback();
                Assert.That(unrelated.IsPlaying, Is.True, "Stopping the owner never calls StopAllSound");
                GameEntry.Sound.StopSound(external, 0);
                Feedback.EmitFeedback(ProductFeedbackKind.WorkFinished);
                int retiring = Feedback.Playing[0].Serial;
                Runtime.StopProduct();
                yield return null;
                Assert.That(GameEntry.Sound.IsLoadingSound(retiring), Is.False, "Loading owner retires without later playback");
            }
            finally
            {
                GameEntry.Event.Unsubscribe(PlaySoundSuccessEventArgs.EventId, handler);
                group.Mute = muted; group.Volume = volume;
            }
        }

        [UnityTest, Category("ProductFeedback")]
        public IEnumerator ProductFeedbackActualPickupPlaceBakeFinishAndMuteDoNotReplay()
        {
            yield return StartLauncher();
            var group = GameEntry.Sound.GetSoundGroup(ProductAudioSettings.Group);
            bool muted = group.Mute; float volume = group.Volume;
            try
            {
                group.Mute = false; group.Volume = .6f;
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.Pickup], Is.Zero);
                yield return ReadableCommand("拿 面包台 ·", true);
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.Pickup] == 1, "The actual GridResult reaches the feedback consumer");
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.Pickup], Is.EqualTo(1));
                yield return ReadableCommand("放 烤箱 ·", true);
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.Place] == 1, "The actual placement terminal result is observed");
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.Place], Is.EqualTo(1));
                yield return ReadableCommand("烘烤 烤箱 ·", true);
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.WorkStart] == 1, "The actual work-start terminal result is observed");
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.WorkStart], Is.EqualTo(1));
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.WorkFinished] == 1, "Actual baking completion emits its own cue");
                var outcome = Grid.LastKitchenOutcome;
                Feedback.ObserveWork(outcome);
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.WorkFinished], Is.EqualTo(1));
                int loaded = Feedback.LoadedCount;
                Presentation.CloseGridHud(); yield return null; Presentation.OpenGridHud();
                yield return Wait(() => Presentation.Ready, "HUD reopens without replacing the client feedback owner");
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.WorkFinished], Is.EqualTo(1));
                Assert.That(Feedback.LoadedCount, Is.GreaterThanOrEqualTo(loaded));
                yield return ReadableCommand("拿 盘台 ·", true);
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.Pickup] == 2, "Actual empty plate pickup cue");
                long rejected = Client.NextCommandId + 1;
                yield return ReachAndClick(Button("添加 烤箱 ·", true));
                yield return Wait(() => Grid.LastCommandId == rejected && Grid.LastStatus == GridResultStatus.Rejected && Feedback.Pending.Count == 0, "A real wrong-direction command rejects and retires its feedback registration");
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.Place], Is.EqualTo(1), "Rejected insertion has no successful placement cue");
                yield return ReadableCommand("放 盘台 ·", true);
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.Place] == 2, "Actual plate replacement cue");
                group.Mute = true;
                yield return ReadableCommand("拿 烤箱 ·", true);
                yield return Wait(() => Feedback.Pending.Count == 0, "The muted pickup terminal result has been consumed");
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.Pickup], Is.EqualTo(2));
                group.Mute = false;
                yield return null;
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.Pickup], Is.EqualTo(2), "Unmuting never replays the muted pickup");
                yield return ReadableCommand("添加 盘台 ·", true);
                yield return Wait(() => Feedback.Requested[(int)ProductFeedbackKind.Place] == 3, "The actual plating terminal result is observed");
                Assert.That(Feedback.Requested[(int)ProductFeedbackKind.Place], Is.EqualTo(3));
            }
            finally
            {
                group.Mute = muted; group.Volume = volume;
            }
        }

        [UnityTest, Category("ProductFeedback")]
        public IEnumerator ProductFeedbackImmediateSettingsActualDiskFailureReloadAndOriginalDraftCancel()
        {
            yield return StartModeLauncher();
            string muteKey = ProductAudioSettings.MutedKey(), volumeKey = ProductAudioSettings.VolumeKey();
            bool hadMute = GameEntry.Setting.HasSetting(muteKey), hadVolume = GameEntry.Setting.HasSetting(volumeKey);
            bool oldMute = GameEntry.Setting.GetBool(muteKey, false); float oldVolume = GameEntry.Setting.GetFloat(volumeKey, .6f);
            var helper = UnityEngine.Object.FindFirstObjectByType<DefaultSettingHelper>();
            var pathField = typeof(DefaultSettingHelper).GetField("m_FilePath", BindingFlags.Instance | BindingFlags.NonPublic);
            string originalPath = (string)pathField.GetValue(helper);
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/solo-save-ui-tests", Guid.NewGuid().ToString("N")));
            ownedSaveUiDirectories.Add(directory); Directory.CreateDirectory(directory);
            string settingPath = Path.Combine(directory, "settings.dat");
            pathField.SetValue(helper, settingPath);
            try
            {
                GameEntry.Setting.RemoveSetting(muteKey); GameEntry.Setting.RemoveSetting(volumeKey);
                ProductAudioSettings.ApplySaved();
                Assert.That(GameEntry.Sound.IsMuted(ProductAudioSettings.Group), Is.False);
                Assert.That(GameEntry.Sound.GetVolume(ProductAudioSettings.Group), Is.EqualTo(.6f));
                yield return ReachAndClick(EntryButton("声音"));
                yield return ReachAndClick(EntryButton("音效音量：0%"));
                Assert.That(GameEntry.Sound.GetVolume(ProductAudioSettings.Group), Is.Zero);
                Assert.That(File.Exists(settingPath), Is.True);
                yield return ReachAndClick(EntryButton("音效静音：关"));
                Assert.That(GameEntry.Sound.IsMuted(ProductAudioSettings.Group), Is.True);
                GameEntry.Setting.RemoveSetting(muteKey); GameEntry.Setting.RemoveSetting(volumeKey);
                Assert.That(GameFramework.GameFrameworkEntry.GetModule<GameFramework.Setting.ISettingManager>().Load(), Is.True);
                ProductAudioSettings.ApplySaved();
                Assert.That(GameEntry.Sound.IsMuted(ProductAudioSettings.Group), Is.True);
                Assert.That(GameEntry.Sound.GetVolume(ProductAudioSettings.Group), Is.Zero);
                string blocker = Path.Combine(directory, "not-a-directory"); File.WriteAllText(blocker, "owned setting failure fixture");
                pathField.SetValue(helper, Path.Combine(blocker, "settings.dat"));
                yield return ReachAndClick(EntryButton("音效音量：60%"));
                Assert.That(GameEntry.Sound.GetVolume(ProductAudioSettings.Group), Is.EqualTo(.6f));
                Assert.That(EntryHud.DetailsText.text, Does.Contain("保存失败"));
                pathField.SetValue(helper, settingPath);
                yield return ReachAndClick(EntryButton("重试保存声音"));
                yield return ReachAndClick(EntryButton("音效静音：开"));
                yield return ReachAndClick(EntryButton("返回声音前页面"));
                Pointer(EntryButton("本机设置"));
                yield return Wait(() => Entry.PreferencesDraft?.Busy == false, "Original preference draft ready");
                Pointer(EntryHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("字号：大号")));
                Pointer(EntryButton("取消本机设置"));
                Assert.That(GameEntry.Sound.IsMuted(ProductAudioSettings.Group), Is.False);
                Assert.That(GameEntry.Sound.GetVolume(ProductAudioSettings.Group), Is.EqualTo(.6f), "Cancelling font draft does not roll back instant sound settings");
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Product reentry uses the actual Solo selector");
                yield return StartPreviewAndConfirm(SelectionButton("内存试玩"));
                yield return Wait(() => Presentation?.Ready == true, "Actual kitchen reapplies saved sound settings");
                Assert.That(GameEntry.Sound.GetVolume(ProductAudioSettings.Group), Is.EqualTo(.6f));
                long command = Client.NextCommandId;
                yield return ReachAndClick(Button("声音"));
                yield return ReachAndClick(Button("音效静音：关"));
                yield return ReachAndClick(Button("音效音量：0%"));
                Assert.That(GameEntry.Sound.IsMuted(ProductAudioSettings.Group), Is.True);
                Assert.That(GameEntry.Sound.GetVolume(ProductAudioSettings.Group), Is.Zero);
                yield return ReachAndClick(Button("音效音量：60%"));
                yield return ReachAndClick(Button("音效静音：开"));
                yield return ReachAndClick(Button("返回声音前页面"));
                Assert.That(Client.NextCommandId, Is.EqualTo(command), "Kitchen sound settings never send restaurant commands");
                Assert.That(GameEntry.Sound.GetVolume(ProductAudioSettings.Group), Is.EqualTo(.6f));
                Assert.That(GameEntry.Sound.IsMuted(ProductAudioSettings.Group), Is.False);
                Assert.That(Hud.CameraDragAreaRectTransform.gameObject.activeInHierarchy, Is.True, "Returning from sound settings restores the restaurant map input area");
                yield return null;
                var cell = new GridCell(3, 6);
                Vector2 point = Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(cell));
                Assert.That(Board.InputScreenRect.Contains(point), Is.True);
                long movement = Client.NextCommandId + 1;
                Presentation.ProcessGridPointer(point, true, true, false);
                Presentation.ProcessGridPointer(point, false, false, true);
                Assert.That(Client.NextCommandId, Is.EqualTo(movement));
                yield return Wait(() => Grid.LastCommandId == movement && Grid.LastStatus == GridResultStatus.Executed, "A real map action works after kitchen sound settings return");
            }
            finally
            {
                if (hadMute)
                {
                    GameEntry.Setting.SetBool(muteKey, oldMute);
                }
                else
                {
                    GameEntry.Setting.RemoveSetting(muteKey);
                }
                if (hadVolume)
                {
                    GameEntry.Setting.SetFloat(volumeKey, oldVolume);
                }
                else
                {
                    GameEntry.Setting.RemoveSetting(volumeKey);
                }
                pathField.SetValue(helper, originalPath);
                ProductAudioSettings.ApplySaved();
            }
        }
    }
}
#endif
