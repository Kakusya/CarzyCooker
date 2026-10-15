using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private static DRGridAutoRecipeConfig BeverageTimedAuto(DRGridAutoRecipeConfig source, int ticks)
        {
            var buffer = new Luban.ByteBuf(); buffer.WriteInt(source.Id); buffer.WriteInt(source.ContainerDefinitionId);
            buffer.WriteSize(source.InputDefinitionIds.Count);
            foreach (int id in source.InputDefinitionIds) { buffer.WriteInt(id); }
            buffer.WriteSize(source.InputCounts.Count);
            foreach (int count in source.InputCounts) { buffer.WriteInt(count); }
            buffer.WriteInt(ticks); buffer.WriteInt(source.OutputDefinitionId); buffer.WriteInt(source.OutputPortions);
            buffer.WriteInt(source.ServingContainerDefinitionId); buffer.WriteInt(source.StationCapability);
            buffer.WriteBool(source.LockContainer); buffer.WriteInt(source.MenuVariant); return new DRGridAutoRecipeConfig(buffer);
        }

        private void BeverageLongFoamBases(ClientSessionComponent client)
        {
            var table = Tables.Instance.DTGridAutoRecipeConfig; var source = table.Get(6);
            var map = (Dictionary<int, DRGridAutoRecipeConfig>)table.DataMap;
            var rows = (List<DRGridAutoRecipeConfig>)table.DataList; int index = rows.IndexOf(source);
            try
            {
                map[6] = rows[index] = BeverageTimedAuto(source, 5000); BeverageBases(client, finishFoam: false);
            }
            finally
            {
                map[6] = rows[index] = source;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("BeverageFiles")]
        public void BeverageActualSoloAndClaimFilesRestoreHalfCupPausedLatteFrozenFoamAndFiniteBasesWithoutBindings(bool cooperative)
        {
            StartBeverage(cooperative); var client = (ClientSessionComponent)host.Client;
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative)
                {
                    ProveFixtureProfile(client, repository);
                }
                BeverageLongFoamBases(client);
                var halfSource = PrepStation("切配台"); long half = halfSource.ItemId;
                PrepDo(client, GridAction.TakeCounter, halfSource.Id, half);
                BeveragePortion(client, "咖啡机"); BeveragePortion(client, "热饮杯台");
                PrepDo(client, GridAction.PlaceCounter, halfSource.Id, half);
                long latte = BeverageSupplyCup(client);
                BeveragePortion(client, "咖啡机"); BeveragePortion(client, "热饮杯台"); BeveragePortion(client, "热饮杯台");
                AssemblyStart(client, 10); var mixing = Grid.InputProcess(latte);
                AdvanceTicks(10); PrepDo(client, GridAction.StopWork, input: latte, expectedTarget: latte, process: mixing.Id);
                int mixProgress = mixing.ProgressTicks;
                PrepDo(client, GridAction.PlaceCounter, PrepStation("第二奶缸台").Id, latte);
                long espresso = BeverageSupplyCup(client); BeveragePortion(client, "咖啡机");
                var label = PrepStation("贴单台"); PrepDo(client, GridAction.PlaceCounter, label.Id, espresso);
                LevelDo(LevelControlAction.Start); var order = WaitActualOrder(9);
                PrepDo(client, GridAction.BindDrink, label.Id, espresso, espresso, order: order.Id);
                Assert.That(CurrentLevel.DrinkBindings.ContainsKey(espresso), Is.True);
                ContentComplete(); RenovationChoose(8);
                var foam = Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Single(value => value.RecipeId == 6);
                Assert.That(foam.State, Is.EqualTo(GridAutoState.Cooking)); int progress = foam.ProgressTicks;
                var header = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                var payload = repository.Select(header.SlotId, out header);
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave"); byte[] original = File.ReadAllBytes(path);
                Assert.That(header.Version, Is.EqualTo(SoloSaveFormat.Current)); Assert.That(payload.ManualWork.Single().ProgressTicks, Is.EqualTo(mixProgress));
                int count = payload.Items.Length;
                host.StopProduct(); repository.Dispose(); StartSavedFixture(header, payload);
                foam = Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Single(value => value.RecipeId == 6);
                Assert.That(foam.ProgressTicks, Is.EqualTo(progress)); Assert.That(foam.RequiredTicks, Is.EqualTo(5000));
                Assert.That(foam.RequiredCapability, Is.EqualTo((int)GridStationCapability.FoamMilk));
                Assert.That(Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Where(value => value.RecipeId is 4 or 5).All(value => value.RemainingPortions == 1), Is.True);
                Assert.That(Grid.Items.Count, Is.EqualTo(count)); Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
                Assert.That(Supply.RemainingPackages[13], Is.EqualTo(3)); Assert.That(Supply.RemainingPackages[14], Is.EqualTo(4));
                Assert.That(Supply.RemainingPackages[15], Is.EqualTo(4));
                halfSource = PrepStation("切配台"); half = halfSource.ItemId;
                Assert.That(Grid.Item(half).Contents.Select(id => Grid.Item(id).DefinitionId), Is.EqualTo(new[] { 56, 57 }));
                var latteSource = PrepStation("第二奶缸台"); latte = latteSource.ItemId;
                mixing = Grid.InputProcess(latte); Assert.That(mixing.ProgressTicks, Is.EqualTo(mixProgress));
                Assert.That(mixing.State, Is.EqualTo(GridWorkState.Paused)); Pump(15);
                client = (ClientSessionComponent)host.Client; PrepDo(client, GridAction.TakeCounter, latteSource.Id, latte); AssemblyStart(client, 10);
                AdvanceTicks(mixing.RequiredTicks - mixing.ProgressTicks + 3); Pump(15);
                Assert.That(Grid.IdentifyDish(latte), Is.EqualTo(10)); PrepDo(client, GridAction.PlaceCounter, latteSource.Id, latte);
                AdvanceTicks(foam.RequiredTicks - foam.ProgressTicks + 3); Pump(15);
                PrepDo(client, GridAction.TakeCounter, halfSource.Id, half); BeveragePortion(client, "蒸汽机"); AssemblyStart(client, 11);
                var capp = Grid.InputProcess(half); AdvanceTicks(capp.RequiredTicks + 3); Pump(15);
                Assert.That(Grid.IdentifyDish(half), Is.EqualTo(11)); Assert.That(foam.RemainingPortions, Is.EqualTo(1));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally
            {
                repository.Dispose(); RemoveOwnSaveTestDirectory(directory);
            }
        }

        [Test, Category("BeverageFiles")]
        public void BeverageActualV11DessertFileKeepsCarrierAndHalfBatterWithoutAddingCafeResources()
        {
            StartDessert(); var client = (ClientSessionComponent)host.Client; DessertMix(client);
            var source = PrepStation("模具台"); PrepDo(client, GridAction.TakeCounter, source.Id, source.ItemId); DessertPortion(client);
            var bake = DessertLongBake(client); ContentComplete(); RenovationChoose(8);
            var payload = SoloSaveCapture.Kitchen(Restaurant); var header = SaveHeader(); header.Version = 11;
            var repository = TestSaveRepository(out string directory);
            try
            {
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave"); byte[] original = SoloSaveFile.Encode(header, payload);
                File.WriteAllBytes(path, original); payload = repository.Select(header.SlotId, out header);
                int progress = bake.ProgressTicks;
                host.StopProduct(); repository.Dispose(); StartSavedFixture(header, payload);
                bake = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single();
                Assert.That(bake.ProgressTicks, Is.EqualTo(progress)); Assert.That(bake.CarrierItemId, Is.Not.Zero);
                Assert.That(Grid.Item(bake.CarrierItemId).Contents, Is.EqualTo(new[] { bake.InputItemId }));
                Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId >= 52), Is.False);
                Assert.That(Supply.RemainingPackages.Keys.Any(id => id >= 14), Is.False);
                Assert.That(Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Single().RemainingPortions, Is.EqualTo(1));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test, Category("BeverageFiles")]
        public void BeverageActualOldSecondChapterFileFollowsOriginal1202RouteAfterNewChapterMenuChanges()
        {
            var table = Tables.Instance.DTRestaurantChapterConfig; var source = table.Get(2);
            var buffer = new Luban.ByteBuf(); buffer.WriteInt(source.Id); buffer.WriteString(source.Name); buffer.WriteInt((int)ProductMenuVariant.Growth);
            buffer.WriteInt(source.SoloChapterId); buffer.WriteInt(source.CooperativeChapterId); buffer.WriteInt(1201); buffer.WriteInt(2201);
            var old = new DRRestaurantChapterConfig(buffer);
            var map = (Dictionary<int, DRRestaurantChapterConfig>)table.DataMap; var rows = (List<DRRestaurantChapterConfig>)table.DataList; int index = rows.IndexOf(source);
            try { map[2] = rows[index] = old; StartContentChapter(false, 2); }
            finally { map[2] = rows[index] = source; }
            Assert.That(CurrentLevel.ConfigId, Is.EqualTo(1201)); ContentComplete(); RenovationChoose(8);
            var payload = SoloSaveCapture.Kitchen(Restaurant); var header = SaveHeader(); header.Version = 11;
            var repository = TestSaveRepository(out string directory);
            try
            {
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave"); byte[] original = SoloSaveFile.Encode(header, payload);
                File.WriteAllBytes(path, original); payload = repository.Select(header.SlotId, out header);
                host.StopProduct(); repository.Dispose(); StartSavedFixture(header, payload);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(1202)); Assert.That(CurrentLevel.MenuVariant, Is.EqualTo((int)ProductMenuVariant.Growth));
                Assert.That(CurrentLevel.NextLevelConfigId, Is.EqualTo(1203)); Assert.That(RestaurantChapters.Find(Tables.Instance.DTLevelConfig.Get(1202)).Id, Is.EqualTo(2));
                Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId >= 32), Is.False);
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length)); Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
