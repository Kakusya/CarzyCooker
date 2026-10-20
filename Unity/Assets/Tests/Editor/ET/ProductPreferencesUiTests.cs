#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("ProductPreferences"), Category("ProductPreferencesLifecycle"), Timeout(120000)]
        public IEnumerator EntryPreferencesActualKcpClientUsesOwnLocalMailboxAndCannotChangeHostSettings()
        {
            yield return StartCoopHostLauncher();
            var hostRepository = CreateSaveUiRepository(Runtime, out _, out _);
            Pointer(Button("本机设置"));
            yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "Host local settings read");
            Pointer(Button("字号：较大", true));
            Pointer(Button("拖动：快", true));
            Pointer(Button("保存本机设置"));
            yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "Host local settings write");
            Pointer(Button("取消本机设置"));
            byte[] hostBytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(hostRepository.DirectoryPath, SoloSaveRepositorySystem.PreferencesFileName));
            Presentation.CloseGridHud();
            ((GridKitchenBoard)Presentation.Board).Visible = false;
            yield return Wait(() => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Host releases managed HUD");
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Actual KCP client HUD");
                string directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Temp/solo-save-ui-tests", System.Guid.NewGuid().ToString("N")));
                ownedSaveUiDirectories.Add(directory);
                var remoteRepository = remote.UseOwnedPreferencesRepository(directory, System.Guid.NewGuid().ToString("N"));
                long command = remote.Client.NextCommandId;
                Pointer(ClaimUiButton(remote.Hud, "本机设置"));
                yield return WaitCoopUi(remote, () => remote.Presentation.PreferencesDraft?.Busy == false, "Remote queries its own Root mailbox");
                Pointer(remote.Hud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith("字号：大号")));
                Pointer(remote.Hud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text.StartsWith("拖动：慢")));
                var oldSave = CaptureButtonCallback(ClaimUiButton(remote.Hud, "保存本机设置"));
                Pointer(ClaimUiButton(remote.Hud, "保存本机设置"));
                yield return WaitCoopUi(remote, () => remote.Presentation.PreferencesDraft?.Busy == false, "Remote local file save without Peer permission changes");
                Assert.That(remote.Presentation.PreferencesDraft.Feedback, Does.Contain("已保存"));
                Assert.That(remoteRepository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Largest));
                Assert.That(remoteRepository.Preferences.DragSpeed, Is.EqualTo(ProductDragSpeed.Slow));
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                Assert.That(hostRepository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Larger));
                Assert.That(hostRepository.Preferences.DragSpeed, Is.EqualTo(ProductDragSpeed.Fast));
                Assert.That(System.IO.File.ReadAllBytes(System.IO.Path.Combine(hostRepository.DirectoryPath, SoloSaveRepositorySystem.PreferencesFileName)), Is.EqualTo(hostBytes));
                long version = remoteRepository.PreferencesVersion;
                long binding = remote.Client.BindingGeneration;
                var remoteRuntime = (ProductRuntimeComponent)remote.Client.Runtime;
                ((Session)remote.Client.Session).Dispose();
                yield return WaitCoopUi(remote, () => remoteRuntime.Stage == ProductStage.Disconnected, "Actual disconnect retires preferences draft");
                oldSave();
                Assert.That(remote.Presentation.PreferencesDraft, Is.Null);
                Assert.That(remoteRepository.PreferencesVersion, Is.EqualTo(version));
                Pointer(ClaimUiButton(remote.Hud, "手动恢复"));
                yield return WaitCoopUi(remote, () => remote.Presentation.Ready && remote.Grid.Confirmed && remote.Client.BindingGeneration > binding, "Actual manual resume installs a new binding");
                oldSave();
                Assert.That(remoteRepository.PreferencesVersion, Is.EqualTo(version), "Old settings callback cannot save after resume");
                remote.Presentation.CloseGridHud();
                yield return null;
                oldSave();
                Assert.That(remoteRepository.PreferencesVersion, Is.EqualTo(version));
                yield return WaitCoopUi(remote, () => !Game.GameEntry.UI.HasUIForm("Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab"), "Client releases pooled HUD");
                Presentation.OpenGridHud();
                yield return WaitCoopUi(remote, () => Presentation.Ready, "Host reuses original HUD with its own font setting");
                Assert.That(hostRepository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Larger));
                Assert.That(Presentation.PreferencesDraft, Is.Null);
            }
        }

        [UnityTest, Category("ProductPreferences"), Category("ProductPreferencesLifecycle"), Timeout(120000)]
        public IEnumerator EntryPreferencesActualLevelContinueAndNewRuntimeRetireOldDraftCallbacks()
        {
            yield return StartLauncher();
            var repository = CreateSaveUiRepository(Runtime, out _, out _);
            var restaurant = (RestaurantRuntime)((Match)((HostSessionComponent)Runtime.Authority).Match).Restaurant;
            ((Level)restaurant.CurrentLevel).OperatingTicks = 1;
            yield return UnlockUiStartOperating();
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationChoiceVersion > 0, "Real empty opening naturally completes");
            Pointer(Button("本机设置"));
            yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "Current completed-level settings draft");
            var oldLevelSave = CaptureButtonCallback(Button("保存本机设置"));
            long version = repository.PreferencesVersion;
            long level = Grid.LevelId;
            var next = Client.CallProduct(Grid.CreateLevelCommand(LevelControlAction.Continue)).AsTask();
            yield return Wait(() => next.IsCompleted && Grid.LevelId != level && Grid.LevelPhase == LevelPhase.Preparing, "Original Host command advances while settings remain a view");
            using (var response = (LevelCommandResponse)next.GetAwaiter().GetResult())
            {
                Assert.That(response.Error, Is.Zero, response.Message);
            }
            Assert.That(Presentation.PreferencesDraft, Is.Null);
            oldLevelSave();
            Assert.That(repository.PreferencesVersion, Is.EqualTo(version));
            Pointer(Button("本机设置"));
            yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "New level preferences draft");
            var oldRuntimeSave = CaptureButtonCallback(Button("保存本机设置"));
            Pointer(Button("取消本机设置"));
            var originalRuntime = Runtime;
            string world = Grid.WorldId;
            Pointer(Button("返回模式选择"));
            yield return Wait(() => Entry.LeaveAction == ProductLeaveAction.SoloReturnModes && ((UIGridKitchen)Entry.Hud)?.Available == true, "Original leave confirmation");
            Pointer(EntryProgressButton("确认返回模式选择"));
            yield return Wait(() => Runtime != originalRuntime && ((UIGridKitchen)Entry?.Hud)?.Available == true, "Original explicit return rebuilds the runtime");
            ownedInit = Init.Instance;
            Pointer(EntryButton("单机餐厅"));
            yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Fresh normal Solo selector");
            yield return StartPreviewAndConfirm(SelectionButton("内存试玩"));
            yield return Wait(() => Presentation?.Ready == true, "New independent restaurant runtime");
            var currentRepository = Runtime.Root().GetComponent<SoloSaveRepository>();
            version = currentRepository.PreferencesVersion;
            long command = Client.NextCommandId;
            oldRuntimeSave();
            oldLevelSave();
            Assert.That(Grid.WorldId, Is.Not.EqualTo(world));
            Assert.That(Presentation.PreferencesDraft, Is.Null);
            Assert.That(currentRepository.PreferencesVersion, Is.EqualTo(version));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
        }

        [UnityTest, Category("ProductPreferences"), Timeout(120000)]
        public IEnumerator EntryPreferencesActualSelectorSaveCancelGameInputAndPooledReopen()
        {
            yield return StartLauncher(chooseGame: false);
            var repository = CreateSaveUiRepository(Runtime, out string directory, out _);
            var selection = Runtime.Root().GetComponent<SoloSavePresentation>();
            selection.OpenSoloSelection();
            yield return Wait(() => ((UIGridKitchen)selection.Hud)?.Available == true, "Owned selector preferences HUD");
            MonoUIGridKitchen menu = ((UIGridKitchen)selection.Hud).View;
            Button MenuButton(string caption, bool prefix = false) => menu.GetComponentsInChildren<Button>(false)
                .Single(button => prefix ? button.GetComponentInChildren<Text>().text.StartsWith(caption) : button.GetComponentInChildren<Text>().text == caption);
            int baseFont = menu.DetailsText.fontSize;
            float originalScoreStart = menu.ScoreText.rectTransform.anchorMin.x;
            Pointer(MenuButton("本机设置"));
            yield return Wait(() => selection.PreferencesDraft?.Busy == false, "Actual local preferences read reply");
            Pointer(MenuButton("字号：大号", true));
            Pointer(MenuButton("拖动：慢", true));
            Pointer(MenuButton("到货提醒：开"));
            Pointer(MenuButton("临期阈值：3 秒"));
            var oldChoice = CaptureButtonCallback(MenuButton("字号：标准", true));
            Pointer(MenuButton("保存本机设置"));
            yield return Wait(() => selection.PreferencesDraft?.Busy == false, "Actual local preferences write reply");
            Assert.That(selection.PreferencesDraft.Feedback, Does.Contain("已保存"));
            Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Largest));
            Assert.That(repository.Preferences.Notices.HasFlag(ProductNoticeOptions.Arrivals), Is.False);
            Assert.That(menu.DetailsText.fontSize, Is.EqualTo(Mathf.RoundToInt(baseFont * 1.3f)));
            Assert.That(menu.DetailsText.resizeTextForBestFit, Is.False);
            Pointer(MenuButton("字号：标准", true));
            Pointer(MenuButton("取消本机设置"));
            Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Largest), "Cancel only discards the unsaved draft");
            oldChoice();
            Assert.That(selection.PreferencesDraft, Is.Null);
            Pointer(MenuButton("菜单试玩"));
            yield return StartPreviewAndConfirm(MenuButton("内存试玩"));
            yield return Wait(() => Presentation?.Ready == true, "Saved local parameters reach ordinary restaurant");
            Assert.That(Hud.DetailsText.fontSize, Is.EqualTo(Mathf.RoundToInt(baseFont * 1.3f)));
            Canvas.ForceUpdateCanvases();
            foreach (var text in new[] { Hud.PhaseText, Hud.ClockText, Hud.ScoreText })
            {
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height), "Enlarged top row remains fully readable: " + text.name);
            }
            long command = Client.NextCommandId;
            Vector3 origin = Board.BoardCamera.transform.position;
            float[] amounts = new float[3];
            string[] captions = { "慢", "标准", "快" };
            for (int index = 0; index < captions.Length; ++index)
            {
                Pointer(Button("本机设置"));
                yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "Read current preferences");
                Pointer(Button("拖动：" + captions[index], true));
                Pointer(Button(index == 0 ? "字号：较大" : index == 1 ? "字号：标准" : "字号：大号", true));
                Pointer(Button("保存本机设置"));
                yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "Save each named local option");
                Pointer(Button("取消本机设置"));
                Canvas.ForceUpdateCanvases();
                yield return null;
                Board.BoardCamera.transform.position = origin;
                Vector2 point = Board.InputScreenRect.center;
                Presentation.ProcessGridPointer(point, true, true, false);
                Presentation.ProcessGridPointer(point + new Vector2(0, 20), false, true, false);
                Presentation.ProcessGridPointer(point + new Vector2(0, 20), false, false, true);
                amounts[index] = Mathf.Abs(Board.BoardCamera.transform.position.y - origin.y);
                Assert.That(amounts[index], Is.GreaterThan(0));
                Assert.That(Client.NextCommandId, Is.EqualTo(command), "Sensitivity only pans the camera");
            }
            Assert.That(amounts[0] / amounts[1], Is.EqualTo(.7f).Within(.03f));
            Assert.That(amounts[2] / amounts[1], Is.EqualTo(1.4f).Within(.03f));
            Pointer(Button("操作帮助"));
            string help = Hud.DetailsText.text;
            Pointer(Button("本机设置"));
            yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "Settings opened from cached help");
            Pointer(Button("取消本机设置"));
            Assert.That(Hud.DetailsText.text, Is.EqualTo(help), "Returning restores the original cached help body");
            Pointer(Button("关闭操作帮助"));
            Pointer(Button("暂停"));
            yield return Wait(() => Grid.LevelPaused, "Actual pause before preferences reading");
            long tick = Grid.Tick;
            Pointer(Button("本机设置"));
            yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "Paused settings read");
            var retiredDefault = CaptureButtonCallback(Button("恢复设置默认"));
            Pointer(Button("恢复设置默认"));
            Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Largest), "Default remains draft until explicit save");
            Pointer(Button("保存本机设置"));
            yield return Wait(() => Presentation.PreferencesDraft?.Busy == false, "Persist explicit defaults");
            Pointer(Button("取消本机设置"));
            Assert.That(repository.Preferences.FontSize, Is.EqualTo(ProductFontSize.Standard));
            Assert.That(Hud.ScoreText.rectTransform.anchorMin.x, Is.EqualTo(originalScoreStart), "Standard restores the original column layout");
            Assert.That(Grid.Tick, Is.EqualTo(tick));
            Assert.That(Grid.LevelPaused, Is.True);
            Presentation.CloseGridHud();
            yield return null;
            Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Preferences retain the original managed HUD pool");
            retiredDefault();
            Assert.That(Presentation.PreferencesDraft, Is.Null);
            yield return Command("恢复");
            yield return Command("拿 碗台 ·", true);
            yield return Command("放 碗台 ·", true);
            command = Client.NextCommandId;
            Vector2 uiPoint = RectTransformUtility.WorldToScreenPoint(null, Button("操作帮助").transform.position);
            Presentation.ProcessGridPointer(uiPoint, true, true, false);
            Presentation.ProcessGridPointer(uiPoint, false, false, true);
            Assert.That(Presentation.PointerConsumed, Is.True, "Actual UI raycast blocks the board input path");
            Assert.That(Client.NextCommandId, Is.EqualTo(command));

            Mouse originalMouse = Mouse.current;
            Touchscreen originalTouch = Touchscreen.current;
            Mouse mouse = UnityEngine.InputSystem.InputSystem.AddDevice<Mouse>();
            Touchscreen touch = UnityEngine.InputSystem.InputSystem.AddDevice<Touchscreen>();
            try
            {
                Vector2 point = Board.InputScreenRect.center;
                command = Client.NextCommandId;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new MouseState { position = point + new Vector2(60, 30) }.WithButton(MouseButton.Left));
                yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new MouseState { position = point + new Vector2(60, 30) });
                yield return null;
                Assert.That(Client.NextCommandId, Is.EqualTo(command), "Actual mouse drag does not become a click");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = point });
                yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = point + new Vector2(60, -30) });
                yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = point + new Vector2(60, -30) });
                yield return null;
                Assert.That(Client.NextCommandId, Is.EqualTo(command), "Actual primary touch drag does not become a click");
                Board.BoardCamera.transform.position = origin;
                Canvas.ForceUpdateCanvases();
                Vector2 tap = Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(new GridCell(2, 6)));
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new MouseState { position = tap }.WithButton(MouseButton.Left));
                yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new MouseState { position = tap });
                yield return null;
                Assert.That(Client.NextCommandId, Is.EqualTo(command + 1), "Actual mouse click submits exactly once after dragging");
                yield return Wait(() => Grid.LastCommandId == command + 1 && Grid.LastStatus == GridResultStatus.Executed, "Original pointer command executes");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Began, position = tap });
                yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(touch, new TouchState { touchId = 2, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = tap });
                yield return null;
                Assert.That(Client.NextCommandId, Is.EqualTo(command + 2), "Actual primary touch click follows the same single-command rule");
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(touch);
                UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
                originalMouse?.MakeCurrent();
                originalTouch?.MakeCurrent();
            }
        }
    }
}
#endif
