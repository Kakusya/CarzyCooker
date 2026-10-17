#if UNITY_ET_VIEW && UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using ET.Client;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ET
{
    public sealed partial class SoloRestaurantUiTests
    {
        private IEnumerator ExpansionUiNaturalSettlement()
        {
            yield return UnlockUiStartOperating();
            yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed && Grid.RenovationLevelId == Grid.LevelId &&
                Grid.RenovationChoiceVersion > 0, "Actual growth service naturally settles before choosing expansion", 75);
        }

        private IEnumerator ExpansionUiJourney(bool cooperative)
        {
            SoloSaveRepository repository = null;
            if (cooperative)
            {
                yield return StartProgressHostRoom(); yield return StartPreviewAndConfirm(EntryButton("开启经营成长餐厅"));
            }
            else
            {
                yield return StartModeLauncher(); repository = CreateSaveUiRepository(Runtime, out _, out _);
                Pointer(EntryButton("单机餐厅"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true, "Ordinary selector before growth restaurant");
                yield return StartPreviewAndConfirm(SelectionButton("新开经营成长餐厅"));
            }
            yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed, "Ordinary growth restaurant");
            Assert.That(Grid.Width, Is.EqualTo(7)); Assert.That(Grid.Tables.Count, Is.EqualTo(2));
            var originalItems = Grid.Items.Keys.OrderBy(id => id).ToArray();
            yield return ExpansionUiNaturalSettlement();
            yield return Command("装修 扩建单客桌");
            Assert.That(Grid.Width, Is.EqualTo(7)); Assert.That(Grid.Tables.Count, Is.EqualTo(2));
            var retired = CaptureButtonCallback(Button(cooperative ? "下一关" : "保存并继续"));
            yield return UnlockUiNext(cooperative);
            yield return null;
            Assert.That(Grid.Width, Is.EqualTo(9)); Assert.That(Grid.Tables.Count, Is.EqualTo(3));
            Assert.That(Grid.Items.Keys.OrderBy(id => id), Is.EqualTo(originalItems));
            var table = Grid.Stations.Values.Single(station => station.Cell == new GridCell(7, 3));
            Assert.That(Grid.Tables[table.Id].Seat, Is.EqualTo(new GridCell(6, 3)));
            Assert.That(ArtMap<long>("chairs")[table.Id].transform.localPosition, Is.EqualTo(Board.CellToWorld(new GridCell(6, 3))));
            long command = Client.NextCommandId; retired(); Assert.That(Client.NextCommandId, Is.EqualTo(command));
            foreach (var cell in new[] { new GridCell(0, 6), new GridCell(8, 6), new GridCell(0, 17), new GridCell(8, 17) })
            {
                Assert.That(Board.InputScreenRect.Contains(Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(cell))), Is.True, "Expanded kitchen remains visible in the unobstructed viewport");
            }
            Vector2 center = Board.InputScreenRect.center;
            Presentation.ProcessGridPointer(center, true, true, false);
            Presentation.ProcessGridPointer(center + new Vector2(0, 600), false, true, false);
            Presentation.ProcessGridPointer(center + new Vector2(0, 600), false, false, true);
            Assert.That(Client.NextCommandId, Is.EqualTo(command), "Ordinary vertical drag only moves the view");
            var destination = new GridCell(6, 2);
            Vector2 point = Board.BoardCamera.WorldToScreenPoint(Board.CellToWorld(destination));
            Assert.That(Board.InputScreenRect.Contains(point), Is.True); Assert.That(Board.ScreenToCell(point), Is.EqualTo(destination));
            Presentation.ProcessGridPointer(point, true, true, false); Presentation.ProcessGridPointer(point, false, false, true);
            Assert.That(Client.NextCommandId, Is.EqualTo(++command));
            yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Executed && Grid.Actors[Client.MemberId].Cell == destination, "Real pointer moves into the newly opened strip", 20);
            yield return ExpansionUiNaturalSettlement();
            yield return Command("装修 解锁现榨菜单");
            yield return UnlockUiNext(cooperative);
            string savedSlot = cooperative ? null : Grid.LastSavedSlotId;
            SoloSaveHeader checkpoint = null;
            SoloSavePayload saved = cooperative ? null : repository.Select(savedSlot, out checkpoint);
            string path = cooperative ? null : Path.Combine(repository.DirectoryPath, savedSlot + ".ccsave");
            byte[] original = cooperative ? null : File.ReadAllBytes(path);
            Assert.That(Grid.Tables.Count, Is.EqualTo(3)); Assert.That(Grid.Width, Is.EqualTo(9));
            yield return AssemblyUiChop(null, "备料台", 2); yield return AssemblyUiChop(null, "生菜台", 23); yield return AssemblyUiChop(null, "黄瓜台", 25);
            yield return Command("拿 盘台 ·", true); yield return Command("组装 田园沙拉");
            long plate = Grid.Actors[Client.MemberId].HandItemId;
            yield return Wait(() => Grid.Items[plate].Contents.Count == 1 && Grid.Items[Grid.Items[plate].Contents[0]].DefinitionId == 27, "Actual original kitchen still makes F01 after expansion");
            yield return Command("放 盘台 ·", true);
            yield return UnlockUiStartOperating();
            CoopUiRemote remote = null;
            if (cooperative)
            {
                command = Client.NextCommandId + 1; Presentation.ClickGridCell(new GridCell(2, 10));
                yield return Wait(() => Grid.LastCommandId == command && Grid.LastStatus == GridResultStatus.Executed, "Host parks through the map handler");
                Presentation.CloseGridHud();
                remote = new CoopUiRemote(Runtime.ListenEndpoint); ownedCoopUiRemotes.Add(remote);
                yield return WaitCoopUi(remote, () => remote.Presentation?.Ready == true && remote.Grid.Confirmed, "Actual KCP teammate receives expanded geometry");
            }
            try
            {
                var view = remote?.Grid ?? Grid;
                Assert.That(view.Width, Is.EqualTo(9)); Assert.That(view.Tables[table.Id].Seat, Is.EqualTo(new GridCell(6, 3)));
                yield return AssemblyUiCommand(remote, "拿 盘台 ·", true);
                bool HasOrder() => view.Orders.Values.Any(order => order.TableStationId == table.Id && order.State == OrderState.Waiting && order.DishId == 4);
                if (remote == null)
                {
                    yield return Wait(HasOrder, "Real customer sits at the added table and companion asks its order", 60);
                }
                else
                {
                    yield return WaitCoopUi(remote, HasOrder, "Real KCP view observes customer and companion service at the added table", 60);
                }
                var order = view.Orders.Values.First(value => value.TableStationId == table.Id && value.State == OrderState.Waiting);
                Assert.That(view.Companions.Values.Any(role => role.CompletedCount > 0), Is.True);
                yield return AssemblyUiCommand(remote, "前往 " + table.Name);
                yield return AssemblyUiCommand(remote, "交付 田园沙拉 → " + table.Name);
                Assert.That(view.Orders[order.Id].State, Is.EqualTo(OrderState.Served)); Assert.That(view.LevelScore, Is.EqualTo(10));
                bool CanClear() => view.FrontWorks.Values.Any(work => work.StationId == table.Id && work.Kind == FrontWorkKind.Clear && work.State == FrontWorkState.Available);
                if (remote == null)
                {
                    yield return Wait(CanClear, "Actual served customer leaves the new table", 20);
                }
                else
                {
                    yield return WaitCoopUi(remote, CanClear, "KCP observes the actual customer leave the new table", 20);
                }
                yield return AssemblyUiCommand(remote, "清桌 " + table.Name, true);
                bool Washed() => view.Items.TryGetValue(plate, out var carrier) && carrier.Location == GridItemLocation.CleanPool;
                if (remote == null)
                {
                    yield return Wait(Washed, "Companion washes the original plate after the player clears the new table", 40);
                }
                else
                {
                    yield return WaitCoopUi(remote, Washed, "KCP observes companion wash after the player clears the expanded table", 40);
                }
                Assert.That(view.Items[plate].Hygiene, Is.EqualTo(ItemHygiene.Clean));
            }
            finally { remote?.Dispose(); }
            if (!cooperative)
            {
                yield return Wait(() => Grid.LevelPhase == LevelPhase.Completed, "Third stage naturally closes after added table service", 90);
                Pointer(Button("保存完成并返回选档"));
                yield return Wait(() => Selection != null && ((UIGridKitchen)Selection.Hud)?.Available == true && Selection.Headers.Any(header => header.SlotId == savedSlot), "Ordinary terminal action returns the selector", 30);
                yield return InspectAndContinueSave(savedSlot);
                yield return Wait(() => Presentation?.Ready == true && Grid.Confirmed && Grid.LevelConfigId == checkpoint.NextLevelId, "Ordinary selector restores the checkpoint containing installed expansion", 20);
                Assert.That(Grid.Width, Is.EqualTo(9)); Assert.That(Grid.Tables.Count, Is.EqualTo(3));
                Assert.That(Grid.Stations.Count, Is.EqualTo(saved.Stations.Length)); Assert.That(Grid.Items.Count, Is.EqualTo(saved.Items.Length));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
        }

        [UnityTest, Category("RestaurantExpansionUi"), Timeout(300000)]
        public IEnumerator RestaurantExpansionUiSoloActualNewCellCustomerDeliveryWashAndSavedReentry() => ExpansionUiJourney(false);

        [UnityTest, Category("RestaurantExpansionUi")]
        public IEnumerator RestaurantExpansionUiCooperativeActualNewCellAndRemoteNewTableService() => ExpansionUiJourney(true);
    }
}
#endif
