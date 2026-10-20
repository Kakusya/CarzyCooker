#if UNITY_ET_VIEW && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        [UnityTest, Category("VisualCoherence")]
        public IEnumerator VisualCoherenceEveryDefinitionAndActualHotCupEmptyOtherItemKeepCorrectIcons()
        {
            yield return StartLauncher(menuVariant: ProductMenuVariant.Cafe);
            var icon = (Image)typeof(MonoUIGridKitchen).GetField("m_HandIcon", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Hud);
            var sprites = (Sprite[])typeof(MonoUIGridKitchen).GetField("handArt", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Hud);
            var identities = new HashSet<(Texture2D, Rect)>();
            foreach (var definition in Tables.Instance.DTGridItemConfig.DataList)
            {
                Hud.SetHandItemIcon(definition.Id);
                Assert.That(icon.enabled, Is.True, definition.Name);
                Assert.That(icon.sprite, Is.SameAs(sprites[definition.Id - 1]));
                Assert.That(icon.sprite.rect.xMax, Is.LessThanOrEqualTo(icon.sprite.texture.width));
                Assert.That(icon.sprite.rect.yMax, Is.LessThanOrEqualTo(icon.sprite.texture.height));
                Assert.That(identities.Add((icon.sprite.texture, icon.sprite.rect)), Is.True, "Each valid definition has its own authored silhouette: " + definition.Name);
            }
            Assert.That(sprites.Length, Is.EqualTo(MonoGridKitchenBoard.ItemArtDefinitionCount));
            Assert.That(identities.Count, Is.EqualTo(Tables.Instance.DTGridItemConfig.DataList.Count));
            Hud.SetHandItemIcon(9000);
            Assert.That(icon.enabled, Is.True);
            Assert.That(icon.GetComponentInChildren<Text>(false).text, Is.EqualTo("？"));
            Assert.That(sprites.Contains(icon.sprite), Is.False, "Unknown definition never retains a previous item's image");
            Hud.SetHandItemIcon(0);
            Assert.That(icon.enabled, Is.False); Assert.That(icon.sprite, Is.Null);
            Presentation.RefreshGridHud();
            yield return ReadableCommand("拿 热饮杯台 ·", true);
            yield return Wait(() => icon.sprite == sprites[58], "Definition 59 reaches the actual HUD projection");
            Assert.That(Grid.Items[Grid.Actors[Client.MemberId].HandItemId].DefinitionId, Is.EqualTo(59));
            Assert.That(icon.enabled, Is.True);
            yield return ReadableCommand("放 热饮杯台 ·", true);
            yield return Wait(() => icon.sprite == null && !icon.enabled, "Empty hand clears the previous hot cup");
            yield return ReadableCommand("拿 杯台 ·", true);
            yield return Wait(() => icon.sprite == sprites[GridKitchenRules.DisposableCup - 1], "A different actual cup gets its own icon");
            Assert.That(icon.sprite, Is.Not.SameAs(sprites[58]));
        }
    }
}
#endif
