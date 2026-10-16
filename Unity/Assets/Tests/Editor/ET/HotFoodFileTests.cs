using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private static DRGridBakeRecipeConfig HotFoodTimedRecipe(DRGridBakeRecipeConfig source, int ticks)
        {
            var buffer = new Luban.ByteBuf();
            buffer.WriteInt(source.Id); buffer.WriteInt(source.InputDefinitionId); buffer.WriteInt(source.OutputDefinitionId);
            buffer.WriteInt((int)(source.StationCapability)); buffer.WriteInt(ticks); buffer.WriteInt(source.CarrierDefinitionId);
            return new DRGridBakeRecipeConfig(buffer);
        }

        private GridBakeProcess HotFoodStartLongFrozenFrying(ClientSessionComponent client)
        {
            var table = Tables.Instance.DTGridBakeRecipeConfig;
            var map = (Dictionary<int, DRGridBakeRecipeConfig>)table.DataMap;
            var rows = (List<DRGridBakeRecipeConfig>)table.DataList;
            var source = table.Get(2);
            int index = rows.IndexOf(source);
            try
            {
                map[2] = rows[index] = HotFoodTimedRecipe(source, 5000);
                return HotFoodStartFrying(client);
            }
            finally
            {
                map[2] = rows[index] = source;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Category("HotFoodFiles")]
        public void HotFoodActualSoloAndClaimFilesKeepPartialBreakfastAndUnfinishedFrozenFrying(bool cooperative)
        {
            StartHotFood(cooperative);
            var client = (ClientSessionComponent)host.Client;
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative)
                {
                    ProveFixtureProfile(client, repository);
                }
                long toast = DishBake(client);
                var oven = PrepStation("烤箱");
                var plate = PrepStation("盘台");
                PrepDo(client, GridAction.TakeCounter, oven.Id, toast);
                PrepDo(client, GridAction.AddToContainer, plate.Id, toast, plate.ItemId);
                var eggSource = PrepStation("食材台");
                long egg = eggSource.ItemId;
                var frying = PrepStation("煎台");
                PrepDo(client, GridAction.TakeCounter, eggSource.Id, egg);
                PrepDo(client, GridAction.PlaceCounter, frying.Id, egg);
                PrepDo(client, GridAction.StartFry, frying.Id, egg, egg);
                var eggWork = Grid.BakeProcess(frying.BakeProcessId);
                AdvanceTicks(eggWork.RequiredTicks + 3); Pump(15);
                long friedEgg = frying.ItemId;
                Assert.That(Grid.Item(friedEgg).DefinitionId, Is.EqualTo(37));
                PrepDo(client, GridAction.TakeCounter, frying.Id, friedEgg);
                PrepDo(client, GridAction.AddToContainer, plate.Id, friedEgg, plate.ItemId);
                Assert.That(Grid.Item(plate.ItemId).Contents.Count, Is.EqualTo(2));
                Assert.That(Grid.IdentifyDish(plate.ItemId), Is.Zero, "Incomplete breakfast cannot be served as toast");
                var work = HotFoodStartLongFrozenFrying(client);
                Assert.That(work.RequiredTicks, Is.EqualTo(5000));
                Assert.That(Tables.Instance.DTGridBakeRecipeConfig.Get(2).RequiredTicks, Is.EqualTo(80));
                ContentComplete();
                RenovationChoose(8);
                Assert.That(work.State, Is.EqualTo(GridBakeState.Baking));
                int progress = work.ProgressTicks;
                var header = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                var payload = repository.Select(header.SlotId, out header);
                Assert.That(header.Version, Is.EqualTo(SoloSaveFormat.Current));
                Assert.That(payload.Baking.Single().RequiredTicks, Is.EqualTo(5000));
                Assert.That(payload.Baking.Single().ProgressTicks, Is.EqualTo(progress));
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                byte[] original = File.ReadAllBytes(path);
                int count = payload.Items.Length;
                host.StopProduct(); repository.Dispose();
                StartSavedFixture(header, payload);
                var restored = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single();
                Assert.That(restored.RequiredTicks, Is.EqualTo(5000));
                Assert.That(restored.ProgressTicks, Is.EqualTo(progress));
                Assert.That(Grid.Item(restored.InputItemId).DefinitionId, Is.EqualTo(33));
                Assert.That(Grid.Items.Count, Is.EqualTo(count));
                Assert.That(Grid.Width, Is.EqualTo(9));
                var restoredPlate = Grid.Item(PrepStation("盘台").ItemId);
                Assert.That(restoredPlate.Contents.Select(id => Grid.Item(id).DefinitionId), Is.EquivalentTo(new[] { 9, 37 }));
                Assert.That(Grid.IdentifyDish(restoredPlate.Id), Is.Zero);
                Pump(15);
                AdvanceTicks(restored.RequiredTicks - restored.ProgressTicks + 4);
                Pump(15);
                Assert.That(restored.State, Is.EqualTo(GridBakeState.Completed));
                Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 34), Is.EqualTo(1));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally
            {
                repository.Dispose();
                RemoveOwnSaveTestDirectory(directory);
            }
        }

        [Test, Category("HotFoodFiles")]
        public void HotFoodActualV9FileRestoresOriginalMenuWithoutNewGriddleIngredientsOrStock()
        {
            StartSolo();
            CompleteForSave();
            var repository = TestSaveRepository(out string directory);
            try
            {
                var header = SaveHeader();
                repository.Commit(header, SoloSaveCapture.Kitchen(Restaurant));
                var payload = repository.Select(header.SlotId, out header);
                header.Version = 9;
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave");
                File.WriteAllBytes(path, SoloSaveFile.Encode(header, payload));
                byte[] original = File.ReadAllBytes(path);
                payload = repository.Select(header.SlotId, out header);
                Assert.That(header.Version, Is.EqualTo(9));
                host.StopProduct(); repository.Dispose();
                StartSavedFixture(header, payload);
                Assert.That(CurrentLevel.ConfigId, Is.EqualTo(102));
                Assert.That(CurrentLevel.AllowedDishIds, Is.EqualTo(new[] { 1, 2 }));
                Assert.That(Grid.Stations.Values.Any(value => ((GridStation)value).Kind == GridStationKind.Frying), Is.False);
                Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId >= 32), Is.False);
                Assert.That(Supply.RemainingPackages.Keys.Any(id => id >= 9), Is.False);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally
            {
                repository.Dispose();
                RemoveOwnSaveTestDirectory(directory);
            }
        }
    }
}
