using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ET
{
    public enum TeaFileStage { HalfTea, HalfColdCup, Brewing, Boiling, CleanedColdCup }

    public sealed partial class ProductSessionTests
    {
        [TestCase(false, TeaFileStage.HalfTea), TestCase(true, TeaFileStage.HalfTea)]
        [TestCase(false, TeaFileStage.HalfColdCup), TestCase(true, TeaFileStage.HalfColdCup)]
        [TestCase(false, TeaFileStage.Brewing), TestCase(true, TeaFileStage.Brewing)]
        [TestCase(false, TeaFileStage.Boiling), TestCase(true, TeaFileStage.Boiling)]
        [TestCase(false, TeaFileStage.CleanedColdCup), TestCase(true, TeaFileStage.CleanedColdCup)]
        [Category("TeaBeverageFiles")]
        public void TeaActualSoloAndSignedClaimFilesKeepFiniteTeaMixedCupAndFrozenJobsWithoutBindings(bool cooperative, TeaFileStage stage)
        {
            StartTea(cooperative); ClientSessionComponent client = host.Client;
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative) { ProveFixtureProfile(client, repository); }
                GridAutoProcess brewing = null; GridBakeProcess boiling = null;
                if (stage == TeaFileStage.Brewing)
                {
                    var table = Tables.Instance.DTGridAutoRecipeConfig; var source = table.Get(7);
                    var rows = (List<DRGridAutoRecipeConfig>)table.DataList; var map = (Dictionary<int, DRGridAutoRecipeConfig>)table.DataMap; int index = rows.IndexOf(source);
                    try { rows[index] = map[7] = BeverageTimedAuto(source, 5000); brewing = TeaBrew(client, false); }
                    finally { rows[index] = map[7] = source; }
                }
                else if (stage == TeaFileStage.Boiling)
                {
                    var source = TeaStation(51); long raw = source.ItemId; var boiler = TeaStation(44);
                    PrepDo(client, GridAction.TakeCounter, source.Id, raw); PrepDo(client, GridAction.PlaceCounter, boiler.Id, raw);
                    var table = Tables.Instance.DTGridBakeRecipeConfig; var definition = table.Get(8);
                    var rows = (List<DRGridBakeRecipeConfig>)table.DataList; var map = (Dictionary<int, DRGridBakeRecipeConfig>)table.DataMap; int index = rows.IndexOf(definition);
                    try
                    {
                        rows[index] = map[8] = HotFoodTimedRecipe(definition, 5000);
                        PrepDo(client, GridAction.StartBoilPearls, boiler.Id, raw, raw); boiling = Grid.BakeProcess(boiler.BakeProcessId);
                    }
                    finally { rows[index] = map[8] = definition; }
                }
                else
                {
                    var tea = TeaBrew(client);
                    if (stage == TeaFileStage.HalfTea)
                    {
                        var source = TeaStation(47); long cup = source.ItemId;
                        PrepDo(client, GridAction.TakeCounter, source.Id, cup);
                        PrepDo(client, GridAction.PortionTeaBase, TeaStation(43).Id, cup, tea.PotItemId, tea.Id);
                        PrepDo(client, GridAction.PlaceCounter, source.Id, cup);
                    }
                    else
                    {
                        var milk = TeaMilk(client, tea); long pearl = TeaPearls(client); var source = TeaStation(46); long cup = source.ItemId;
                        PrepDo(client, GridAction.AddToContainer, source.Id, pearl, cup); PrepDo(client, GridAction.TakeCounter, source.Id, cup);
                        PrepDo(client, GridAction.PortionTeaBase, TeaStation(45).Id, cup, milk.PotItemId, milk.Id);
                        PrepDo(client, GridAction.PlaceCounter, source.Id, cup);
                        if (stage == TeaFileStage.CleanedColdCup)
                        {
                            var ice = TeaStation(54); long raw = ice.ItemId;
                            PrepDo(client, GridAction.TakeCounter, ice.Id, raw); PrepDo(client, GridAction.AddToContainer, source.Id, raw, cup);
                            PrepDo(client, GridAction.TakeCounter, source.Id, cup); TeaCleanup(client, PrepStation("水池／洗碗台"), GridAction.DrainLiquid);
                            PrepDo(client, GridAction.PlaceCounter, source.Id, cup);
                        }
                    }
                }
                ContentComplete(); RenovationChoose(8);
                int progress = brewing?.ProgressTicks ?? boiling?.ProgressTicks ?? 0;
                var header = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId); var payload = repository.Select(header.SlotId, out header);
                int count = payload.Items.Length; string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave"); byte[] original = File.ReadAllBytes(path);
                host.StopProduct(); repository.Dispose(); StartSavedFixture(header, payload);
                Assert.That(Grid.Items.Count, Is.EqualTo(count)); Assert.That(CurrentLevel.DrinkBindings, Is.Empty);
                Assert.That(CurrentLevel.Children.Values.OfType<Order>(), Is.Empty);
                foreach (int source in Enumerable.Range(19, 7)) { Assert.That(Supply.RemainingPackages[source], Is.EqualTo(5)); }
                if (stage != TeaFileStage.Boiling) { Assert.That(TeaStation(50).ItemId, Is.Zero, "Used tea leaf counter is not replenished"); }
                if (stage == TeaFileStage.Brewing)
                {
                    var batch = Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Single();
                    Assert.That(batch.ProgressTicks, Is.EqualTo(progress)); Assert.That(batch.RequiredTicks, Is.EqualTo(5000));
                    Assert.That(Grid.Item(batch.PotItemId).Contents.Select(id => Grid.Item(id).DefinitionId), Is.EquivalentTo(new[] { 76, 77 }));
                    Pump(15); AdvanceTicks(batch.RequiredTicks - batch.ProgressTicks + 3); Pump(15);
                    Assert.That(batch.State, Is.EqualTo(GridAutoState.Ready)); Assert.That(batch.RemainingPortions, Is.EqualTo(3));
                }
                else if (stage == TeaFileStage.Boiling)
                {
                    var work = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single();
                    Assert.That(work.ProgressTicks, Is.EqualTo(progress)); Assert.That(work.RequiredTicks, Is.EqualTo(5000));
                    Assert.That(TeaStation(51).ItemId, Is.Zero);
                    Pump(15); AdvanceTicks(work.RequiredTicks - work.ProgressTicks + 3); Pump(15);
                    Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == GridKitchenRules.CookedPearls), Is.EqualTo(1));
                    Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId == GridKitchenRules.RawPearls), Is.False);
                }
                else if (stage == TeaFileStage.HalfTea)
                {
                    var batch = Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).Single();
                    Assert.That(batch.RemainingPortions, Is.EqualTo(2)); Assert.That(Grid.IdentifyDish(TeaStation(47).ItemId), Is.EqualTo(13));
                }
                else if (stage == TeaFileStage.CleanedColdCup)
                {
                    long cup = TeaStation(46).ItemId;
                    Assert.That(Grid.Item(cup).Contents.Select(id => Grid.Item(id).DefinitionId), Is.EqualTo(new[] { 80, 83 }));
                    Assert.That(Grid.IdentifyDish(cup), Is.Zero); Assert.That(TeaStation(54).ItemId, Is.Zero);
                    client = host.Client; Pump(15); PrepDo(client, GridAction.TakeCounter, TeaStation(46).Id, cup);
                    TeaCleanup(client, PrepStation("垃圾桶"), GridAction.DiscardSolid);
                    Assert.That(Grid.Item(cup).Contents, Is.Empty);
                    Assert.That(Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value)
                        .Single(value => value.RequiredCapability == GridStationCapability.MixTea).RemainingPortions, Is.EqualTo(1));
                }
                else
                {
                    long cup = TeaStation(46).ItemId;
                    Assert.That(Grid.Item(cup).Contents.Select(id => Grid.Item(id).DefinitionId), Is.EqualTo(new[] { 80, 82 }));
                    Assert.That(Grid.IdentifyDish(cup), Is.Zero); Assert.That(TeaStation(52).ItemId, Is.Zero);
                    var batches = Grid.AutoProcesses.Values.Select(value => (GridAutoProcess)value).ToArray();
                    Assert.That(batches.Single(value => value.RequiredCapability == GridStationCapability.BrewTea).RemainingPortions, Is.EqualTo(2));
                    Assert.That(batches.Single(value => value.RequiredCapability == GridStationCapability.MixTea).RemainingPortions, Is.EqualTo(1));
                    client = host.Client; Pump(15); var ice = TeaStation(54); long raw = ice.ItemId;
                    PrepDo(client, GridAction.TakeCounter, ice.Id, raw); PrepDo(client, GridAction.AddToContainer, TeaStation(46).Id, raw, cup);
                    PrepDo(client, GridAction.TakeCounter, TeaStation(46).Id, cup); AssemblyStart(client, 16);
                    var blending = Grid.InputProcess(cup); AdvanceTicks(blending.RequiredTicks + 3); Pump(15);
                    Assert.That(Grid.IdentifyDish(cup), Is.EqualTo(15));
                }
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
