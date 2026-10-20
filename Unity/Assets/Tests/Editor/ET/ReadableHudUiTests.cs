#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UiEventSystem = UnityEngine.EventSystems.EventSystem;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private IEnumerator ReachAndClick(Button button)
        {
            string caption = button.GetComponentInChildren<Text>().text;
            MonoUIGridKitchen view = button.GetComponentInParent<MonoUIGridKitchen>();
            var hits = new List<RaycastResult>();
            yield return Wait(() =>
            {
                // Resolve the current render, and hit-test/dispatch in the same frame.
                // A projection refresh can legitimately retire an earlier render's buttons.
                Button current = view.GetComponentsInChildren<Button>(false).SingleOrDefault(value => value.GetComponentInChildren<Text>().text == caption);
                if (current == null)
                {
                    return false;
                }
                Canvas.ForceUpdateCanvases();
                ScrollRect scroll = current.GetComponentInParent<ScrollRect>();
                if (scroll != null)
                {
                    scroll.StopMovement();
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, (RectTransform)current.transform);
                    Vector2 position = scroll.content.anchoredPosition;
                    position.y += scroll.viewport.rect.center.y - bounds.center.y;
                    scroll.content.anchoredPosition = position;
                    Canvas.ForceUpdateCanvases();
                }
                Canvas canvas = current.GetComponentInParent<Canvas>().rootCanvas;
                Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                var rect = (RectTransform)current.transform;
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
                var data = new PointerEventData(UiEventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
                hits.Clear();
                UiEventSystem.current.RaycastAll(data, hits);
                if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != current)
                {
                    return false;
                }
                Assert.That(ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerClickHandler), Is.Not.Null);
                return true;
            }, "A real raycast reaches the rendered control: " + caption, 3);
        }

        [UnityTest]
        [Category("ReadableHud")]
        public IEnumerator ReadableHudActualEntryIndependentScrollFixedReturnAndReopen()
        {
            yield return StartLauncher(chooseGame: false);
            var selector = Runtime.Root().GetComponent<SoloSavePresentation>();
            var selectorHud = ((UIGridKitchen)selector.Hud).View;
            Assert.That(selectorHud.LayoutMode, Is.EqualTo(ProductHudLayout.Entry));
            yield return ReachAndClick(selectorHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text == "菜单试玩"));
            yield return ReachAndClick(selectorHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text == "内存试玩"));
            Assert.That(selector.StartPreviewVisible, Is.True);
            yield return ReachAndClick(selectorHud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text == "确认开始"));
            yield return Wait(() => Presentation?.Ready == true, "Real selected restaurant and HUD");
            Runtime.Root().GetComponent<SoloSaveRepository>().Preferences.FontSize = ProductFontSize.Largest;
            Hud.ApplyLocalFontSize(ProductFontSize.Largest);
            Assert.That(Hud.LayoutMode, Is.EqualTo(ProductHudLayout.Restaurant));
            yield return null;
            foreach (GridCell corner in new[] { new GridCell(0, Grid.KitchenBottom), new GridCell(Grid.Width - 1, Grid.Height - 1) })
            {
                Vector2 screen = Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(corner));
                Assert.That(Board.InputScreenRect.Contains(screen), Is.True, "The complete kitchen remains in the usable map viewport");
            }
            yield return ReachAndClick(Button("更多动作"));
            yield return null;
            Vector3 cameraPosition = Board.BoardCamera.transform.position;
            long command = Client.NextCommandId;
            yield return ReachAndClick(Button("操作帮助"));
            Assert.That(Hud.LayoutMode, Is.EqualTo(ProductHudLayout.Details));
            // Use the longest delivered recipe body, not a synthetic filler text.
            string longest = Tables.Instance.DTDishConfig.DataList.Select(dish => GridRecipeGuide.Describe(dish.Id, Grid.TickMilliseconds)).OrderByDescending(text => text.Length).First();
            Hud.SetInformation("完整菜谱", longest, true, false);
            var body = Hud.DetailsPanelRectTransform.GetComponent<ScrollRect>();
            var actions = Hud.TargetsRectTransform.GetComponentInParent<ScrollRect>(true);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(body.content.rect.height, Is.GreaterThan(body.viewport.rect.height));
            Assert.That(actions.gameObject.activeInHierarchy, Is.True);
            Assert.That(actions.content.rect.height, Is.GreaterThan(actions.viewport.rect.height));
            float before = actions.verticalNormalizedPosition;
            var root = Hud.GetComponent<Canvas>().rootCanvas;
            Camera eventCamera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            Hud.ApplySafeArea(new Rect(Screen.width * .04f, Screen.height * .05f, Screen.width * .92f, Screen.height * .90f), new Vector2(Screen.width, Screen.height));
            Canvas.ForceUpdateCanvases();
            Vector2 pointer = RectTransformUtility.WorldToScreenPoint(eventCamera, body.viewport.TransformPoint(body.viewport.rect.center));
            var scrollData = new PointerEventData(UiEventSystem.current) { position = pointer, scrollDelta = new Vector2(0, -1000) };
            var bodyHits = new List<RaycastResult>();
            UiEventSystem.current.RaycastAll(scrollData, bodyHits);
            Assert.That(bodyHits.Count, Is.GreaterThan(0));
            Assert.That(ExecuteEvents.ExecuteHierarchy(bodyHits[0].gameObject, scrollData, ExecuteEvents.scrollHandler), Is.Not.Null);
            Presentation.ProcessGridPointer(pointer, true, true, false);
            Presentation.ProcessGridPointer(pointer + new Vector2(0, 100), false, true, false);
            Presentation.ProcessGridPointer(pointer + new Vector2(0, 100), false, false, true);
            Assert.That(Presentation.PointerConsumed, Is.True, "Detail input cannot pass through in the simulated nonzero safe area");
            Hud.ApplySafeArea(Screen.safeArea, new Vector2(Screen.width, Screen.height));
            yield return null;
            Assert.That(body.verticalNormalizedPosition, Is.EqualTo(0).Within(.01f));
            Assert.That(actions.verticalNormalizedPosition, Is.EqualTo(before).Within(.01f), "Body scrolling does not scroll actions");
            Vector2 actionPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, actions.viewport.TransformPoint(actions.viewport.rect.center));
            var actionData = new PointerEventData(UiEventSystem.current) { position = actionPoint, scrollDelta = new Vector2(0, -1000) };
            bodyHits.Clear(); UiEventSystem.current.RaycastAll(actionData, bodyHits);
            Assert.That(bodyHits.Count, Is.GreaterThan(0));
            Assert.That(ExecuteEvents.ExecuteHierarchy(bodyHits[0].gameObject, actionData, ExecuteEvents.scrollHandler), Is.Not.Null);
            yield return null;
            Assert.That(actions.verticalNormalizedPosition, Is.EqualTo(0).Within(.01f), "The independent long action list reaches its end");
            Assert.That(body.verticalNormalizedPosition, Is.EqualTo(0).Within(.01f), "Action-list scrolling preserves the body position");
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Reading and scrolling issue no command");
            yield return ReachAndClick(Button("收起详情"));
            Assert.That(Hud.LayoutMode, Is.EqualTo(ProductHudLayout.Restaurant));
            Assert.That(Board.BoardCamera.transform.position, Is.EqualTo(cameraPosition));
            var retired = CaptureButtonCallback(Button("查看详情"));
            Presentation.CloseGridHud();
            yield return null;
            Presentation.OpenGridHud();
            yield return Wait(() => Presentation.Ready, "Managed HUD reopens");
            retired();
            Assert.That(Hud.LayoutMode, Is.EqualTo(ProductHudLayout.Restaurant));
            Assert.That(Client.NextCommandId, Is.EqualTo(command));
            yield return ReachAndClick(Button("拿 碗台 ·", true));
            Assert.That(Client.NextCommandId, Is.EqualTo(command + 1), "One real hit sends exactly one request after reopening");
            yield return Wait(() => Grid.LastCommandId == command + 1 && Grid.LastStatus == GridResultStatus.Executed, "Current bowl pickup executes");
            if (Screen.width > Screen.height)
            {
                yield return ReadableCommand("放 碗台 ·", true);
                yield return ReadableChooseBuff();
                yield return ReadableCommand("开始营业");
                yield return Wait(() => Grid.Orders.Values.Any(order => order.State == OrderState.Waiting), "Actual wide-screen order summary");
                Assert.That(Hud.OrderPanelRectTransform.GetComponent<ScrollRect>().content, Is.SameAs(Hud.OrdersText.rectTransform));
                Assert.That(Hud.OrderActionsRectTransform.GetComponentsInChildren<Button>(false), Is.Empty, "Order execution buttons are in contextual actions rather than covering the summary");
                yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Wide-screen ordinary trial naturally reaches settlement", 75);
                yield return ReachAndClick(Button("结算详情"));
                Assert.That(Hud.DetailsText.text, Does.Contain("本关完成").And.Contain("零星仍可继续"));
                yield return ReachAndClick(Button("关闭结算详情"));
                Assert.That(Hud.LayoutMode, Is.EqualTo(ProductHudLayout.Restaurant));
            }
        }

        [UnityTest, Category("ReadableHud")]
        public IEnumerator ReadableHudHostAndRealKcpClientKeepSharedLayoutAndRestrictedControls()
        {
            yield return StartCoopHostLauncher();
            Assert.That(Grid.CanControlRestaurant, Is.True);
            Assert.That(Hud.LayoutMode, Is.EqualTo(ProductHudLayout.Restaurant));
            // Keep the headless presentation owner: removing it lets the next committed
            // Host snapshot recreate a competing singleton HUD in this one-Editor fixture.
            Presentation.GetAsyncOwner().AsyncScope.Stop();
            Presentation.CloseGridHud();
            ((GridKitchenBoard)Presentation.Board).Visible = false;
            yield return null;
            using (var remote = new CoopUiRemote(Runtime.ListenEndpoint))
            {
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true, "Real KCP remote HUD ready");
                Assert.That(remote.Grid.CanControlRestaurant, Is.False);
                Assert.That(remote.Hud.GetComponentsInChildren<Button>(false).Any(button => button.GetComponentInChildren<Text>().text is "开始营业" or "暂停"), Is.False);
                Button Help() => remote.Hud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text == "操作帮助");
                yield return ContentUiPump(ReachAndClick(Help()), remote);
                Assert.That(remote.Hud.LayoutMode, Is.EqualTo(ProductHudLayout.Details));
                long command = remote.Client.NextCommandId;
                var back = remote.Hud.GetComponentsInChildren<Button>(false).Single(button => button.GetComponentInChildren<Text>().text == "收起详情");
                yield return ContentUiPump(ReachAndClick(back), remote);
                Assert.That(remote.Client.NextCommandId, Is.EqualTo(command));
                Assert.That(remote.Hud.LayoutMode, Is.EqualTo(ProductHudLayout.Restaurant));
            }
        }
    }
}
#endif
