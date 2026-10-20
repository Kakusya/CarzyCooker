#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("RecipeStepRegression")]
        public IEnumerator KitchenRecipeGuideActualDefinitionsPrepreparedDishPauseAndManagedReopenStayReadOnly()
        {
            yield return StartLauncher();
            Assert.That(Presentation.FirstOrderGuideVisible, Is.False, "Independent trials retain a manual guide");
            Pointer(Button("重看首单引导"));
            string soup=GridRecipeGuide.Describe(1,Grid.TickMilliseconds);
            foreach(string required in new[]{"番茄","鸡蛋","煮锅","碗","切配台","灶台","倒入","盛一份"})
                Assert.That(soup,Does.Contain(required));
            var batch=Tables.Instance.DTGridAutoRecipeConfig.Get(1);
            Assert.That(soup,Does.Contain($"{batch.RequiredTicks*Grid.TickMilliseconds/1000f:0.0} 秒").And.Contain($"{batch.OutputPortions} 份"));
            string toast=GridRecipeGuide.Describe(2,Grid.TickMilliseconds);
            foreach(string required in new[]{"面包片","烤箱","盘","烤面包","交付"})Assert.That(toast,Does.Contain(required));
            long command=Client.NextCommandId;
            Pointer(Button("菜谱 番茄蛋花汤"));yield return null;
            Assert.That(Client.NextCommandId,Is.EqualTo(command));Assert.That(Hud.DetailsText.text,Does.Contain(soup));
            Pointer(Button("收起详情"));yield return null;
            Assert.That(Presentation.RecipeVisible,Is.False);Assert.That(Hud.DetailsPanelRectTransform.gameObject.activeSelf,Is.False);
            yield return Command("拿 面包台 ·",true);yield return Command("放 烤箱 ·",true);yield return Command("烘烤 烤箱 ·",true);
            yield return Wait(()=>Grid.Items.Values.Any(item=>item.DefinitionId==GridKitchenRules.Toast),"Actual toast preparation");
            yield return Command("拿 烤箱 ·",true);yield return Command("添加 盘台 ·",true);yield return Command("拿 盘台 ·",true);
            long plate=Grid.Actors[Client.MemberId].HandItemId;
            yield return Command("暂停");long tick=Grid.Tick;command=Client.NextCommandId;
            Pointer(Button("菜谱 盘装吐司"));yield return null;
            Assert.That(Hud.DetailsText.text,Does.Contain("已备好，无需重做").And.Contain("已暂停"));
            var step = GridRecipeGuide.CurrentStep(Grid, 2);
            Assert.That(step.Kind, Is.EqualTo(GridRecipeStepKind.Delivery));
            Assert.That(step.ItemId, Is.EqualTo(plate), "Shared step metadata points to the existing real prepared carrier");
            Assert.That(Client.NextCommandId,Is.EqualTo(command));
            var retiredRecipe=CaptureButtonCallback(Button("菜谱 番茄蛋花汤"));
            Pointer(Button("跳过首单引导"));yield return null;
            Assert.That(Presentation.FirstOrderGuideVisible,Is.False);
            Pointer(Button("重看首单引导"));yield return null;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(Grid.Tick,Is.EqualTo(tick));Assert.That(Grid.Actors[Client.MemberId].HandItemId,Is.EqualTo(plate));
            Assert.That(Grid.LevelScore,Is.Zero);Assert.That(Client.NextCommandId,Is.EqualTo(command));
            int mono=Hud.GetInstanceID();Presentation.CloseGridHud();yield return null;Presentation.OpenGridHud();
            yield return Wait(()=>Presentation.Ready,"Actual recipe HUD pool reuse");
            Assert.That(Hud.GetInstanceID(),Is.EqualTo(mono));
            Assert.That(Hud.DetailsText.text,Does.Contain("已备好，无需重做"));
            Pointer(Button("关闭菜谱"));yield return null;retiredRecipe();yield return null;
            Assert.That(Presentation.RecipeVisible,Is.False);Assert.That(Presentation.RecipeDishId,Is.EqualTo(2));
            Assert.That(Client.NextCommandId,Is.EqualTo(command));
            yield return Command("恢复");yield return Command("放 面包台 ·",true);
            Assert.That(Grid.Stations.Values.Single(value=>value.Name=="面包台").ItemId,Is.EqualTo(plate));
        }

        [UnityTest]
        public IEnumerator KitchenRecipeGuideRejectedEmptyDishAndNewLevelRecomputeInsteadOfAdvancing()
        {
            yield return StartLauncher();yield return Command("拿 碗台 ·",true);
            Pointer(Button("菜谱 番茄蛋花汤"));yield return null;
            string before=GridRecipeGuide.CurrentNeed(Grid,1);
            var retiredRecipe=CaptureButtonCallback(Button("菜谱 盘装吐司"));
            Pointer(Button("关闭菜谱"));yield return null;
            string buff=Hud.GetComponentsInChildren<Button>(false).First(value=>value.GetComponentInChildren<Text>().text.StartsWith("选择 ")).GetComponentInChildren<Text>().text;
            yield return Command(buff);yield return Command("开始营业");
            yield return Wait(()=>Grid.Orders.Values.Any(value=>value.State==OrderState.Waiting && value.DishId==1),"Actual first soup order");
            var order=Grid.Orders.Values.First(value=>value.State==OrderState.Waiting && value.DishId==1);
            string table=Grid.Stations[order.TableStationId].Name;yield return Command("前往 "+table);
            Pointer(Button(OrderNavigationCaption(Grid, order)));
            Assert.That(Presentation.RecipeOrderId, Is.EqualTo(order.Id));
            long command=Client.NextCommandId+1;Pointer(Button("交付 番茄蛋花汤 → "+table));
            yield return Wait(()=>Grid.LastCommandId==command && Grid.LastStatus==GridResultStatus.Rejected,"Actual empty-dish rejection");
            Assert.That(Grid.LastReason,Is.EqualTo("WrongDish"));Assert.That(GridRecipeGuide.CurrentNeed(Grid,1),Is.EqualTo(before));
            Assert.That(Grid.LevelScore,Is.Zero);Assert.That(Hud.HintText.text,Does.Not.Contain("已备好"));
            yield return Wait(()=>Grid.LevelPhase==LevelPhase.Completed,"Natural old-level drain for recipe boundary",75);
            yield return Wait(()=>Grid.RenovationLevelId==Grid.LevelId && Grid.RenovationChoiceVersion>0,"Continue prepared");
            long old=Grid.LevelId;yield return Command("下一关");command=Client.NextCommandId;
            Assert.That(Grid.LevelId,Is.Not.EqualTo(old));retiredRecipe();yield return null;
            Assert.That(Presentation.RecipeOrderId, Is.Zero);
            Assert.That(Presentation.RecipeVisible,Is.False);Assert.That(Client.NextCommandId,Is.EqualTo(command));
            Assert.That(Hud.HintText.text,Does.Not.Contain(table));Assert.That(Grid.Orders,Is.Empty);
            Assert.That(GridRecipeGuide.CurrentNeed(Grid,1),Does.Not.Contain("旧订单"));
        }

        [UnityTest]
        public IEnumerator KitchenRecipeGuideRealKcpClientReadsBothRecipesWithoutHostControlOrSoloSteps()
        {
            yield return StartCoopHostLauncher();
            var retiredHost=CaptureButtonCallback(Button("菜谱 番茄蛋花汤"));
            ((Scene)Client.IScene).RemoveComponent<GridPresentationComponent>();yield return null;
            using(var remote=new CoopUiRemote(Runtime.ListenEndpoint))
            {
                yield return WaitCoopUi(remote,()=>remote.Presentation?.Ready==true,"KCP Client recipe reader ready");
                long command=remote.Client.NextCommandId;long[] items=remote.Grid.Items.Keys.OrderBy(id=>id).ToArray();
                foreach(string caption in new[]{"菜谱 番茄蛋花汤","菜谱 盘装吐司"})
                {
                    var button=remote.Hud.GetComponentsInChildren<Button>(false).Single(value=>value.GetComponentInChildren<Text>().text==caption);
                    Pointer(button);yield return null;
                    Assert.That(remote.Hud.DetailsPanelRectTransform.gameObject.activeSelf,Is.True);
                    Assert.That(remote.Hud.DetailsText.text,Does.Contain(caption.Substring(3)).And.Contain("交付"));
                }
                Assert.That(remote.Client.NextCommandId,Is.EqualTo(command));Assert.That(remote.Grid.CanControlRestaurant,Is.False);
                Assert.That(remote.Grid.Items.Keys.OrderBy(id=>id),Is.EqualTo(items));
                string[] labels=remote.Hud.GetComponentsInChildren<Button>(false).Select(value=>value.GetComponentInChildren<Text>().text).ToArray();
                Assert.That(labels.Any(value=>value is "跳过首单引导" or "重看首单引导" or "查看首单步骤" or "暂停" or "开始营业"),Is.False);
                Pointer(remote.Hud.GetComponentsInChildren<Button>(false).Single(value=>value.GetComponentInChildren<Text>().text=="关闭菜谱"));
                retiredHost();yield return null;
                Assert.That(remote.Presentation.RecipeVisible,Is.False);Assert.That(remote.Client.NextCommandId,Is.EqualTo(command));
            }
        }
    }
}
#endif
