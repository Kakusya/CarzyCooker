#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest]
        public IEnumerator FirstDrinkUiRealLauncherRecipePreparationLabelAndOrderDeliveryUseActualButtons()
        {
            yield return StartLauncher(menuVariant: 1);
            Assert.That(Grid.LevelConfigId, Is.EqualTo(201)); Assert.That(Grid.AllowedDishIds, Is.EqualTo(new[] { 1, 2, 3 }));
            long initial = Client.NextCommandId;
            Pointer(Button("菜谱 鲜榨橙汁"));
            Assert.That(Client.NextCommandId, Is.EqualTo(initial));
            Assert.That(Hud.GetComponentsInChildren<UnityEngine.UI.Text>(false).Any(text => text.text.Contains("榨汁") && text.text.Contains("贴单")), Is.True);
            Pointer(Button("关闭菜谱")); Assert.That(Client.NextCommandId, Is.EqualTo(initial));
            yield return Command("拿 橙子台 ·", true); yield return Command("放 切配台 ·", true); yield return Command("切配 切配台 ·", true);
            yield return Wait(() => Grid.Items.Values.Any(item => item.DefinitionId == GridKitchenRules.OrangeChunks), "Real orange cutting completes");
            yield return Command("拿 切配台 ·", true); yield return Command("添加 果汁壶台 ·", true);
            yield return Command("拿 果汁壶台 ·", true); yield return Command("放 榨汁机 ·", true); yield return Command("榨汁 榨汁机 ·", true);
            yield return Wait(() => Grid.AutoProcesses.Values.Any(batch => batch.State == GridAutoState.Ready && batch.LockContainer), "Real juicer finishes and unlocks");
            yield return Command("拿 杯台 ·", true); long cup = Grid.Actors[Client.MemberId].HandItemId;
            yield return Command("盛一份 榨汁机 ·", true); yield return Command("放 贴单台 ·", true);
            string buff = Hud.GetComponentsInChildren<UnityEngine.UI.Button>(false).First(button => button.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("选择 ")).GetComponentInChildren<UnityEngine.UI.Text>().text;
            yield return Command(buff); yield return Command("开始营业");
            yield return Wait(() => Grid.Orders.Values.Any(order => order.State == OrderState.Waiting && order.DishId == 3), "Normal flow produces drink order", 70);
            var order = Grid.Orders.Values.First(value => value.State == OrderState.Waiting && value.DishId == 3);
            string table = Grid.Stations[order.TableStationId].Name;
            yield return Command("贴单 → " + table); Assert.That(Grid.Items[cup].BoundOrderId, Is.EqualTo(order.Id));
            yield return Command("拿 贴单台 ·", true); yield return Command("前往 " + table); yield return Command("交付 鲜榨橙汁 → " + table);
            Assert.That(Grid.Orders[order.Id].State, Is.EqualTo(OrderState.Served)); Assert.That(Grid.ServedOrders, Is.EqualTo(1));
            Assert.That(Grid.Items.ContainsKey(cup), Is.False); Assert.That(Grid.Actors[Client.MemberId].HandItemId, Is.Zero);
            Assert.That(Grid.LevelScore, Is.EqualTo(order.Score));
        }
    }
}
#endif
