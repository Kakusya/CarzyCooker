using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using NUnit.Framework;

namespace ET
{
    public sealed partial class ProductSessionTests
    {
        private void StartSteamed(bool cooperative = false)
        {
            if (cooperative)
            {
                StartRoomFixture(); UseOptionalBuffFixture(); OpenRoomForFixture(ProductMenuVariant.Steamed);
            }
            else
            {
                var config = RuntimeInstanceConfig.Create("steamed-test", "solo", "solo");
                RuntimeInstanceContext.Prepare(config); var fiber = CreateFiber(); UseOptionalBuffFixture();
                host = fiber.Root.AddComponent<ProductRuntimeComponent, RuntimeInstanceConfig>(config);
                host.InitializeProduct(menuVariant: ProductMenuVariant.Steamed); Pump(18);
            }
            Assert.That(host.Stage, Is.Not.EqualTo(ProductStage.Failed), host.Failure);
            Assert.That(CurrentLevel.ConfigId, Is.EqualTo(cooperative ? 2601 : 1601));
            Assert.That(CurrentLevel.ProductionPermit.DishIds, Does.Contain(12));
        }

        private GridStation SteamedStation(int layout) => Grid.Stations.Values.Select(value => (GridStation)value)
            .Single(value => value.InitialLayoutId == layout);

        private long SteamedFill(ClientSessionComponent client, bool finishMix = true)
        {
            var onion = SteamedStation(36); var chop = PrepStation("切配台"); var bowl = SteamedStation(39);
            long raw = onion.ItemId;
            PrepDo(client, GridAction.TakeCounter, onion.Id, raw);
            PrepDo(client, GridAction.PlaceCounter, chop.Id, raw);
            PrepDo(client, GridAction.StartStationWork, chop.Id, raw, raw);
            var cutting = Grid.InputProcess(raw); AdvanceTicks(cutting.RequiredTicks + 3); Pump(12);
            Assert.That(cutting.State, Is.EqualTo(GridWorkState.Completed));
            long chopped = cutting.OutputItemId, carrier = bowl.ItemId;
            PrepDo(client, GridAction.TakeCounter, chop.Id, chopped);
            PrepDo(client, GridAction.AddToContainer, bowl.Id, chopped, carrier);
            PrepDo(client, GridAction.TakeCounter, bowl.Id, carrier);
            AssemblyStart(client, 13, "AssemblyIngredientsMismatch");
            Assert.That(Grid.Item(carrier).Contents, Is.EqualTo(new[] { chopped }));
            PrepDo(client, GridAction.PlaceCounter, bowl.Id, carrier);
            var beef = SteamedStation(37); long mince = beef.ItemId;
            PrepDo(client, GridAction.TakeCounter, beef.Id, mince);
            PrepDo(client, GridAction.AddToContainer, bowl.Id, mince, carrier);
            PrepDo(client, GridAction.TakeCounter, bowl.Id, carrier);
            AssemblyStart(client, 13);
            if (finishMix)
            {
                var mixing = Grid.InputProcess(carrier); AdvanceTicks(mixing.RequiredTicks + 3); Pump(12);
                Assert.That(Grid.Item(raw), Is.Null); Assert.That(Grid.Item(chopped), Is.Null); Assert.That(Grid.Item(mince), Is.Null);
                Assert.That(Grid.Item(Grid.Item(carrier).Contents.Single()).DefinitionId, Is.EqualTo(68));
            }
            return carrier;
        }

        private long SteamedWrap(ClientSessionComponent client, long carrier)
        {
            var bowl = SteamedStation(39); var wrappers = SteamedStation(38);
            long wrapper = wrappers.ItemId;
            PrepDo(client, GridAction.PlaceCounter, bowl.Id, carrier);
            PrepDo(client, GridAction.TakeCounter, wrappers.Id, wrapper);
            PrepDo(client, GridAction.AddToContainer, bowl.Id, wrapper, carrier);
            PrepDo(client, GridAction.TakeCounter, bowl.Id, carrier);
            AssemblyStart(client, 14);
            var wrapping = Grid.InputProcess(carrier); AdvanceTicks(wrapping.RequiredTicks + 3); Pump(12);
            long raw = Grid.Item(carrier).Contents.Single();
            Assert.That(Grid.Item(raw).DefinitionId, Is.EqualTo(69)); Assert.That(Grid.Item(wrapper), Is.Null);
            PrepDo(client, GridAction.PlaceCounter, bowl.Id, carrier); Pump(15);
            ExtractionFinish(client, ExtractionRequest(client, bowl, raw));
            Assert.That(Grid.Item(carrier).Contents, Is.Empty);
            return raw;
        }

        private GridBakeProcess SteamedBegin(ClientSessionComponent client, long raw)
        {
            var steamer = SteamedStation(41);
            PrepDo(client, GridAction.PlaceCounter, steamer.Id, raw);
            PrepDo(client, GridAction.StartSteam, steamer.Id, raw, raw);
            var work = Grid.BakeProcess(steamer.BakeProcessId);
            Assert.That(work.RecipeId, Is.EqualTo(7)); return work;
        }

        [TestCase(false), TestCase(true), Category("SteamedFoodCore")]
        public void SteamedActualSoloAndKcpExactMixWrapHandoffSteamPauseLockPlateServeAndWash(bool cooperative)
        {
            StartSteamed(cooperative); GridDishConfig.ValidateAllMenus(); GridKitchenSystem.ValidateStationConfiguration();
            var local = (ClientSessionComponent)host.Client; var client = cooperative ? CoopRemote() : local;
            Assert.That(ProductMenuCatalog.Includes(ProductMenuVariant.Cafe, ProductMenuVariant.Steamed), Is.False);
            Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId is 68 or 69 or 70), Is.False);
            long carrier = SteamedFill(local);
            if (cooperative)
            {
                var bowl = SteamedStation(39); PrepDo(local, GridAction.PlaceCounter, bowl.Id, carrier);
                PrepDo(local, GridAction.Move, cell: new GridCell(2, 16));
                PrepDo(client, GridAction.TakeCounter, bowl.Id, carrier);
            }
            long raw = SteamedWrap(client, carrier);
            Assert.That(Grid.Actor(client.MemberId).HandItemId, Is.EqualTo(raw));
            LevelDo(LevelControlAction.Start); var order = WaitActualOrder(12);
            CoopFrontServe(client, order, raw, "WrongDish");
            Assert.That(Grid.Item(raw), Is.Not.Null); Assert.That(CurrentLevel.Score, Is.Zero);
            var work = SteamedBegin(client, raw); var steamer = SteamedStation(41);
            PrepDo(client, GridAction.TakeCounter, steamer.Id, raw, rejection: "WorkBusy");
            PrepDo(client, GridAction.StartSteam, steamer.Id, raw, raw, rejection: "WorkBusy");
            LevelDo(LevelControlAction.Pause); int progress = work.ProgressTicks;
            AdvanceTicks(30); Assert.That(work.ProgressTicks, Is.EqualTo(progress));
            LevelDo(LevelControlAction.Resume); AdvanceTicks(work.RequiredTicks - work.ProgressTicks + 3); Pump(15);
            Assert.That(work.State, Is.EqualTo(GridBakeState.Completed)); Assert.That(Grid.Item(raw), Is.Null);
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 70), Is.EqualTo(1));
            PrepDo(client, GridAction.TakeCounter, steamer.Id, work.OutputItemId);
            var plate = SteamedStation(40); long serving = plate.ItemId;
            PrepDo(client, GridAction.AddToContainer, plate.Id, work.OutputItemId, serving);
            Assert.That(Grid.IdentifyDish(serving), Is.EqualTo(12));
            PrepDo(client, GridAction.TakeCounter, plate.Id, serving); CoopFrontServe(client, order, serving);
            Assert.That(CurrentLevel.Score, Is.EqualTo(order.Score)); Assert.That(CurrentLevel.ServedOrders, Is.EqualTo(1));
            Assert.That(Grid.Item(work.OutputItemId), Is.Null); Assert.That(Grid.Item(carrier).Contents, Is.Empty);
            AdvanceTicks(65); Pump(15);
            FrontWait(() => CurrentLevel.Children.Values.OfType<FrontWork>().Any(value => value.Kind == FrontWorkKind.Clear && value.ItemId == serving), 200);
            var clear = CurrentLevel.Children.Values.OfType<FrontWork>().Single(value => value.Kind == FrontWorkKind.Clear && value.ItemId == serving);
            CoopFrontDo(client, FrontWorkAction.Claim, clear);
            FrontWait(() => Grid.Item(serving).Hygiene == ItemHygiene.Clean &&
                Grid.Item(serving).Location == GridItemLocation.CleanPool, 1200);
            Assert.That(Grid.Item(serving).Contents, Is.Empty);
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 71), Is.EqualTo(1));
        }

        [Test, Category("SteamedFoodCore")]
        public void SteamedActualKcpTwoStartsAndReplayHaveOneProcessAndOneOutput()
        {
            StartSteamed(true); ClientSessionComponent local = host.Client; var remote = CoopRemote();
            var wrong = PrepStation("备料台"); var bowl = SteamedStation(39); long tomato = wrong.ItemId;
            PrepDo(remote, GridAction.TakeCounter, wrong.Id, tomato);
            PrepDo(remote, GridAction.AddToContainer, bowl.Id, tomato, bowl.ItemId, rejection: "IncompatibleMaterial");
            Assert.That(Grid.Item(bowl.ItemId).Contents, Is.Empty); Assert.That(Grid.Actor(remote.MemberId).HandItemId, Is.EqualTo(tomato));
            PrepDo(remote, GridAction.PlaceCounter, wrong.Id, tomato);
            long carrier = SteamedFill(local), raw = SteamedWrap(local, carrier);
            var steamer = SteamedStation(41); PrepDo(local, GridAction.PlaceCounter, steamer.Id, raw);
            PrepDo(local, GridAction.Move, cell: new GridCell(4, 13));
            PrepDo(remote, GridAction.Move, cell: new GridCell(4, 13)); Pump(15);
            var request = local.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.StartSteam, target: steamer.Id, expected: raw, expectedTarget: raw);
            var replay = ExtractionCopy(request);
            var other = remote.GetComponent<GridClientComponent>().CreateGridCommand(GridAction.StartSteam, target: steamer.Id, expected: raw, expectedTarget: raw);
            var first = local.CallProduct(request); var second = remote.CallProduct(other);
            using (var one = (GridCommandResponse)Finish(first, 500))
            using (var two = (GridCommandResponse)Finish(second, 500))
            {
                Assert.That(one.Error, Is.Zero, one.Message); Assert.That(two.Error, Is.Zero, two.Message);
            }
            Pump(12);
            Assert.That(Grid.BakeProcesses.Count, Is.EqualTo(1));
            Assert.That(new[] { Grid.Actor(local.MemberId), Grid.Actor(remote.MemberId) }.Count(actor => actor.LastResultStatus == GridResultStatus.Executed), Is.EqualTo(1));
            var work = Grid.BakeProcess(steamer.BakeProcessId);
            using (var repeated = (GridCommandResponse)Finish(local.CallProduct(replay), 500)) { Assert.That(repeated.Error, Is.Zero, repeated.Message); }
            Assert.That(Grid.BakeProcesses.Count, Is.EqualTo(1));
            AdvanceTicks(work.RequiredTicks + 3); Pump(15);
            Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 70), Is.EqualTo(1));
            Assert.That(Grid.Item(raw), Is.Null); Assert.That(Grid.Item(carrier).Contents, Is.Empty);
        }

        [TestCase(false, false), TestCase(true, false), TestCase(false, true), TestCase(true, true), Category("SteamedFoodFiles")]
        public void SteamedActualSoloAndClaimFilesKeepHalfAssemblyOrFrozenSteamWithoutRefill(bool cooperative, bool steaming)
        {
            StartSteamed(cooperative); var client = (ClientSessionComponent)host.Client;
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative) { ProveFixtureProfile(client, repository); }
                long carrier; GridBakeProcess work = null;
                if (steaming)
                {
                    carrier = SteamedFill(client); long raw = SteamedWrap(client, carrier);
                    var table = Tables.Instance.DTGridBakeRecipeConfig;
                    var map = (Dictionary<int, DRGridBakeRecipeConfig>)table.DataMap; var rows = (List<DRGridBakeRecipeConfig>)table.DataList;
                    var source = table.Get(7); int index = rows.IndexOf(source);
                    try { map[7] = rows[index] = HotFoodTimedRecipe(source, 5000); work = SteamedBegin(client, raw); }
                    finally { map[7] = rows[index] = source; }
                }
                else
                {
                    carrier = SteamedFill(client, false);
                    var mixing = Grid.InputProcess(carrier);
                    PrepDo(client, GridAction.StopWork, input: carrier, expectedTarget: carrier, process: mixing.Id);
                }
                ContentComplete(); RenovationChoose(1);
                int progress = work?.ProgressTicks ?? Grid.InputProcess(carrier).ProgressTicks;
                var header = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                var payload = repository.Select(header.SlotId, out header); int count = payload.Items.Length;
                string path = Path.Combine(repository.DirectoryPath, header.SlotId + ".ccsave"); byte[] original = File.ReadAllBytes(path);
                host.StopProduct(); repository.Dispose(); StartSavedFixture(header, payload);
                Assert.That(Grid.Items.Count, Is.EqualTo(count));
                Assert.That(Supply.RemainingPackages[16], Is.EqualTo(5));
                Assert.That(Supply.RemainingPackages[17], Is.EqualTo(5));
                Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 72), Is.EqualTo(1));
                if (steaming)
                {
                    var restored = Grid.BakeProcesses.Values.Select(value => (GridBakeProcess)value).Single();
                    Assert.That(restored.ProgressTicks, Is.EqualTo(progress)); Assert.That(restored.RequiredTicks, Is.EqualTo(5000));
                    AdvanceTicks(restored.RequiredTicks - restored.ProgressTicks + 3); Pump(15);
                    Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 70), Is.EqualTo(1));
                    Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId == 69), Is.False);
                }
                else
                {
                    var restored = Grid.Processes.Values.Select(value => (GridManualProcess)value).Single(value => value.RecipeId == 13);
                    Assert.That(restored.ProgressTicks, Is.EqualTo(progress));
                    Assert.That(Grid.Item(restored.InputItemId).Contents.Count, Is.EqualTo(2));
                    Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId is 68 or 69 or 70), Is.False);
                }
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [TestCase(false, false), TestCase(true, false), TestCase(false, true), TestCase(true, true), Category("SteamedFoodFiles")]
        public void SteamedActualSoloAndClaimFilesKeepWrappedOrCookedPlateWithoutRefillOrOldOrders(bool cooperative, bool cooked)
        {
            StartSteamed(cooperative); var client = (ClientSessionComponent)host.Client;
            var repository = TestSaveRepository(out string directory);
            try
            {
                if (cooperative) { ProveFixtureProfile(client, repository); }
                long carrier = SteamedFill(client), raw = SteamedWrap(client, carrier);
                if (cooked)
                {
                    var work = SteamedBegin(client, raw); AdvanceTicks(work.RequiredTicks + 3); Pump(15);
                    PrepDo(client, GridAction.TakeCounter, SteamedStation(41).Id, work.OutputItemId);
                    var plate = SteamedStation(40); PrepDo(client, GridAction.AddToContainer, plate.Id, work.OutputItemId, plate.ItemId);
                }
                else
                {
                    PrepDo(client, GridAction.PlaceCounter, SteamedStation(41).Id, raw);
                }
                ContentComplete(); RenovationChoose(1);
                var header = ExpansionFile(repository, cooperative, CurrentLevel.ConfigId);
                var payload = repository.Select(header.SlotId, out header); int count = payload.Items.Length;
                host.StopProduct(); repository.Dispose(); StartSavedFixture(header, payload);
                Assert.That(Grid.Items.Count, Is.EqualTo(count));
                Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == (cooked ? 70 : 69)), Is.EqualTo(1));
                Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId is 64 or 65 or 66 or 67 or 68), Is.False);
                Assert.That(Grid.Items.Values.Count(value => ((GridItem)value).DefinitionId == 72), Is.EqualTo(1));
                long plateId = SteamedStation(40).ItemId;
                Assert.That(Grid.IdentifyDish(plateId), Is.EqualTo(cooked ? 12 : 0));
                Assert.That(CurrentLevel.Children.Values.OfType<Order>(), Is.Empty);
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }

        [Test, Category("SteamedFoodFiles")]
        public void SteamedActualOldCafeSoloAndSignedClaimFilesKeepOriginalInventoryAndNextLevel()
        {
            StartBeverage(); ContentComplete(); RenovationChoose(8);
            var payload = SoloSaveCapture.Kitchen(Restaurant); var originalHeader = SaveHeader();
            var repository = TestSaveRepository(out string directory);
            try
            {
                string path = Path.Combine(repository.DirectoryPath, originalHeader.SlotId + ".ccsave");
                byte[] original = SoloSaveFile.Encode(originalHeader, payload); File.WriteAllBytes(path, original);
                var restored = repository.Select(originalHeader.SlotId, out var header);
                var claim = ClaimFileFixture(repository, payload);
                ProgressClaimFiles.PersistTicket(repository, claim.Ticket, claim.MatchId);
                var receipt = ProgressClaimFiles.Claim(repository, claim.TicketId);
                var claimed = repository.Select(receipt.SlotId, out var claimedHeader);
                Assert.That(claimed.Items.Length, Is.EqualTo(payload.Items.Length));
                Assert.That(claimed.Items.Any(item => item.DefinitionId >= 64), Is.False);
                Assert.That(claimedHeader.NextLevelId, Is.EqualTo(originalHeader.NextLevelId));
                host.StopProduct(); repository.Dispose(); StartSavedFixture(header, restored);
                Assert.That(Grid.Items.Count, Is.EqualTo(payload.Items.Length));
                Assert.That(Grid.Items.Values.Any(value => ((GridItem)value).DefinitionId >= 64), Is.False);
                Assert.That(Supply.RemainingPackages.Keys.Any(id => id >= 16), Is.False);
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            }
            finally { repository.Dispose(); RemoveOwnSaveTestDirectory(directory); }
        }
    }
}
