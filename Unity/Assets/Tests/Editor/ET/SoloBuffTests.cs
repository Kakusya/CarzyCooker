using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace ET
{
    [FriendOf(typeof(LevelBuff))]
    public sealed partial class ProductSessionTests
    {
        private LevelBuff Buff=>CurrentLevel.GetComponent<LevelBuff>();
        private DRLevelConfig BuffLevel(DRLevelConfig source,bool? required=null,int? count=null,int[] pool=null,int? duration=null)
        {
            var buffer=new Luban.ByteBuf();buffer.WriteInt(source.Id);buffer.WriteInt(source.NextLevelConfigId);buffer.WriteInt(duration??source.OperatingTicks);
            buffer.WriteSize(source.StarThresholds.Count);foreach(int star in source.StarThresholds)buffer.WriteInt(star);
            buffer.WriteInt(source.SpawnX);buffer.WriteInt(source.SpawnY);buffer.WriteSize(source.AllowedDishIds.Count);foreach(int dish in source.AllowedDishIds)buffer.WriteInt(dish);
            int[] ids=pool??source.AllowedBuffIds.ToArray();buffer.WriteSize(ids.Length);foreach(int id in ids)buffer.WriteInt(id);
            buffer.WriteInt(count??source.CandidateCount);buffer.WriteBool(required??source.RequireBuffChoice);
            buffer.WriteSize(source.AllowedRenovationIds.Count);foreach(int id in source.AllowedRenovationIds)buffer.WriteInt(id);
            buffer.WriteInt(source.ChapterId); buffer.WriteInt(source.ChapterStage); buffer.WriteBool(source.IsChapterTerminal);
            buffer.WriteInt(source.MenuVariant); buffer.WriteSize(source.AllowedDrinkIds.Count); foreach (int drink in source.AllowedDrinkIds) buffer.WriteInt(drink);
            WriteLevelUnlockFixture(buffer, source);
            return new DRLevelConfig(buffer);
        }
        private void InstallBuffLevel(DRLevelConfig definition)
        {
            var rows=(System.Collections.Generic.List<DRLevelConfig>)Tables.Instance.DTLevelConfig.DataList;
            var map=(System.Collections.Generic.Dictionary<int,DRLevelConfig>)Tables.Instance.DTLevelConfig.DataMap;
            rows[rows.FindIndex(value=>value.Id==definition.Id)]=definition;map[definition.Id]=definition;
        }
        private ChooseBuffResponse BuffReply(ChooseBuffRequest request)
        {
            var task=((ClientSessionComponent)host.Client).CallProduct(request);PumpMessages(8);
            Assert.That(task.Status,Is.Not.EqualTo(UniTaskStatus.Pending));return (ChooseBuffResponse)task.GetAwaiter().GetResult();
        }
        private void BuffDo(int choice,string rejection=null,long? draw=null)
        {
            var projection=((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            using(var reply=BuffReply(projection.CreateBuffChoice(choice,draw??Buff.DrawVersion)))Assert.That(reply.Error,Is.Zero,reply.Message);
            AdvanceTicks(1);PumpMessages(8);
            Assert.That(FrontPeer.LastCommandStatus,Is.EqualTo(rejection==null?GridResultStatus.Executed:GridResultStatus.Rejected),FrontPeer.LastCommandReason);
            Assert.That(FrontPeer.LastCommandReason,Is.EqualTo(rejection));
        }
        private DRBuffConfig BuffDefinition(int id,int numerator=1,int denominator=2,int kind=1,int recipe=1)
        {
            var buffer=new Luban.ByteBuf();buffer.WriteInt(id);buffer.WriteString("测试加成");buffer.WriteInt(kind);
            buffer.WriteInt(numerator);buffer.WriteInt(denominator);buffer.WriteInt(recipe);buffer.WriteInt(0);buffer.WriteInt(0);return new DRBuffConfig(buffer);
        }

        [Test]
        public void SoloBuffConfigActualSourceHasThreeCookingVariantsAndRequiredTwoCandidates()
        {
            BuffConfig.ValidateAll();Assert.That(Tables.Instance.DTBuffConfig.DataList.Count,Is.EqualTo(5));
            foreach(DRBuffConfig row in Tables.Instance.DTBuffConfig.DataList.Where(row => row.Id <= 3))
            {Assert.That(row.EffectKind,Is.EqualTo(1));Assert.That(row.BatchRecipeId,Is.EqualTo(1));Assert.That(row.DurationNumerator,Is.LessThan(row.DurationDenominator));}
            foreach(DRLevelConfig row in Tables.Instance.DTLevelConfig.DataList)
            {Assert.That(row.RequireBuffChoice,Is.True);Assert.That(row.CandidateCount,Is.EqualTo(2));Assert.That(row.AllowedBuffIds,Is.EqualTo(new[]{1,2,3,4,5}));}
            StartSolo(liveBuff:true);Assert.That(Buff.Parent,Is.SameAs(CurrentLevel));Assert.That(Buff.SelectedId,Is.Zero);
        }
        [TestCase("numerator")]
        [TestCase("denominator")]
        [TestCase("recipe")]
        [TestCase("kind")]
        public void SoloBuffConfigRejectsInvalidDurationRecipeOrEffect(string invalid)
        {
            var rows=(System.Collections.Generic.List<DRBuffConfig>)Tables.Instance.DTBuffConfig.DataList;
            rows[0]=BuffDefinition(1,numerator:invalid=="numerator"?0:1,denominator:invalid=="denominator"?0:2,
                kind:invalid=="kind"?99:1,recipe:invalid=="recipe"?999:1);
            Assert.Throws<InvalidOperationException>(()=>BuffConfig.ValidateAll());Assert.That(host,Is.Null);
        }
        [Test]
        public void SoloBuffConfigRequiredEmptyPoolRejectsBeforeHostCreation()
        {
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(101),pool:Array.Empty<int>()));
            Assert.Throws<InvalidOperationException>(()=>StartSolo(liveBuff:true));
            Assert.That((Scene)host.HostScene,Is.Null);Assert.That((HostSessionComponent)host.Authority,Is.Null);
        }
        [Test]
        public void SoloBuffConfigOptionalEmptyPoolCanActuallyStartWithoutChoice()
        {
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(101),required:false,pool:Array.Empty<int>()));
            StartSolo(liveBuff:true);Assert.That(Buff.Candidates,Is.Empty);LevelDo(LevelControlAction.Start);
            Assert.That(CurrentLevel.Phase,Is.EqualTo(LevelPhase.Operating));Assert.That(Buff.SelectedId,Is.Zero);
        }
        [Test]
        public void SoloBuffDrawControlledSeedRepeatsWithoutDuplicatesAndTruncatesSmallPool()
        {
            int[] source={1,2,3};int[] first=BuffConfig.Draw(source,2,1234);
            Assert.That(BuffConfig.Draw(source,2,1234),Is.EqualTo(first));Assert.That(first.Distinct().Count(),Is.EqualTo(2));
            Assert.That(first.All(source.Contains),Is.True);Assert.That(source,Is.EqualTo(new[]{1,2,3}));
            Assert.That(BuffConfig.Draw(new[]{1},4,1234),Is.EqualTo(new[]{1}));Assert.That(BuffConfig.Draw(Array.Empty<int>(),2,1234),Is.Empty);
        }
        [Test]
        public void SoloBuffDrawActualLevelKeepsSeedCandidatesAcrossTicksAndSnapshotRecycling()
        {
            StartSolo(liveBuff:true);int seed=Buff.Seed;int[] candidates=Buff.Candidates.ToArray();long draw=Buff.DrawVersion;
            AdvanceTicks(20);Assert.That(Buff.Seed,Is.EqualTo(seed));Assert.That(Buff.Candidates,Is.EqualTo(candidates));Assert.That(Buff.DrawVersion,Is.EqualTo(draw));
            var client=((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            GridWorldSnapshot first=GridBroadcast.CreateSnapshot(Grid,FrontPeer),second=GridBroadcast.CreateSnapshot(Grid,FrontPeer);
            Assert.That(first.BuffCandidates,Is.Not.SameAs(second.BuffCandidates));client.ApplyGridSnapshot(first);
            first.BuffCandidates.Clear();first.Dispose();Assert.That(client.BuffCandidates,Is.EqualTo(candidates));Assert.That(Buff.Candidates,Is.EqualTo(candidates));
            Assert.That(second.BuffCandidates,Is.EqualTo(candidates));second.Dispose();
        }
        [Test]
        public void SoloBuffChoiceGateRejectsStartUntilOneCandidateCommitsAndLocksNewChoice()
        {
            StartSolo(liveBuff:true);LevelDo(LevelControlAction.Start,"BuffChoiceRequired");Assert.That(CurrentLevel.Phase,Is.EqualTo(LevelPhase.Preparing));
            int choice=Buff.Candidates[0];BuffDo(choice);var projection=((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            Assert.That(Buff.SelectedId,Is.EqualTo(choice));Assert.That(projection.SelectedBuffId,Is.EqualTo(choice));
            Assert.That(projection.BuffCandidates,Is.EqualTo(Buff.Candidates));Assert.That(projection.Version,Is.EqualTo(Grid.Version));
            BuffDo(Buff.Candidates[1],"BuffLocked");Assert.That(Buff.SelectedId,Is.EqualTo(choice));LevelDo(LevelControlAction.Start);
            Assert.That(CurrentLevel.Phase,Is.EqualTo(LevelPhase.Operating));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void SoloBuffChoiceInvalidCandidateOrDrawLeavesSeedAndPoolUnchanged(bool badDraw)
        {
            StartSolo(liveBuff:true);int seed=Buff.Seed;int[] candidates=Buff.Candidates.ToArray();
            BuffDo(badDraw?candidates[0]:999,badDraw?"DrawChanged":"InvalidBuffChoice",badDraw?Buff.DrawVersion+1:null);
            Assert.That(Buff.SelectedId,Is.Zero);Assert.That(Buff.Seed,Is.EqualTo(seed));Assert.That(Buff.Candidates,Is.EqualTo(candidates));
        }
        [Test]
        public void SoloBuffChoicePausedAndOperatingCallsRejectWithoutAnotherSelection()
        {
            StartSolo(liveBuff:true);var projection=((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            LevelDo(LevelControlAction.Pause);
            using(var reply=BuffReply(projection.CreateBuffChoice(Buff.Candidates[0],Buff.DrawVersion)))Assert.That(reply.Message,Is.EqualTo("Paused"));
            Assert.That(Buff.SelectedId,Is.Zero);LevelDo(LevelControlAction.Resume);BuffDo(Buff.Candidates[0]);LevelDo(LevelControlAction.Start);
            int selected=Buff.SelectedId;
            using(var reply=BuffReply(projection.CreateBuffChoice(Buff.Candidates[1],Buff.DrawVersion)))Assert.That(reply.Message,Is.EqualTo("WrongPhase"));
            Assert.That(Buff.SelectedId,Is.EqualTo(selected));
        }
        [Test]
        public void SoloBuffChoiceLatestReplayAndPayloadConflictReturnExistingFacts()
        {
            StartSolo(liveBuff:true);var projection=((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            var request=projection.CreateBuffChoice(Buff.Candidates[0],Buff.DrawVersion);int choice=request.ChoiceDefId;long draw=request.DrawVersion;
            ProductCommandContext context=new(request.MatchId,request.WorldId,request.WorldGeneration,request.LevelId,request.LevelGeneration,request.BindingGeneration,request.CommandId);
            using(var reply=BuffReply(request))Assert.That(reply.Status,Is.EqualTo((int)GridResultStatus.Accepted));Assert.That(Buff.SelectedId,Is.Zero);
            AdvanceTicks(1);PumpMessages(8);
            ChooseBuffRequest Replay(int id)=>ProductMessages.Create<ChooseBuffRequest>(value=>
            {value.ProtocolVersion=ProductSessionRules.ProtocolVersion;value.MatchId=context.MatchId;value.WorldId=context.WorldId;value.WorldGeneration=context.WorldGeneration;
                value.LevelId=context.LevelId;value.LevelGeneration=context.LevelGeneration;value.BindingGeneration=context.BindingGeneration;value.CommandId=context.CommandId;value.DrawVersion=draw;value.ChoiceDefId=id;});
            using(var reply=BuffReply(Replay(choice)))Assert.That(reply.Status,Is.EqualTo((int)GridResultStatus.Executed));
            using(var reply=BuffReply(Replay(Buff.Candidates[1])))Assert.That(reply.Message,Is.EqualTo("CommandConflict"));
            Assert.That(Buff.SelectedId,Is.EqualTo(choice));
        }
        [Test]
        public void SoloBuffChoiceExactOldReplayRejectsAfterAnotherLocalPeerContinuesLevel()
        {
            StartSolo(liveBuff:true);ClientSessionComponent first=host.Client,other=GridJoin();
            var request=first.GetComponent<GridClientComponent>().CreateBuffChoice(Buff.Candidates[0],Buff.DrawVersion);
            int choice=request.ChoiceDefId;long draw=request.DrawVersion;
            ProductCommandContext context=new(request.MatchId,request.WorldId,request.WorldGeneration,request.LevelId,request.LevelGeneration,request.BindingGeneration,request.CommandId);
            using(var reply=BuffReply(request))Assert.That(reply.Error,Is.Zero);AdvanceTicks(1);PumpMessages(8);
            var second=other.GetComponent<GridClientComponent>();
            using(var reply=LevelReply(other,second.CreateLevelCommand(LevelControlAction.Start)))Assert.That(reply.Error,Is.Zero);
            AdvanceTicks(CurrentLevel.OperatingTicks+2);PumpMessages(8);Assert.That(CurrentLevel.Phase,Is.EqualTo(LevelPhase.Completed));
            FrontWait(() => Renovation.Prepared, 8);
            using(var reply=LevelReply(other,second.CreateLevelCommand(LevelControlAction.Continue)))Assert.That(reply.Error,Is.Zero);
            AdvanceTicks(1);PumpMessages(8);int[] candidates=Buff.Candidates.ToArray();
            var replay=ProductMessages.Create<ChooseBuffRequest>(value=>
            {value.ProtocolVersion=ProductSessionRules.ProtocolVersion;value.MatchId=context.MatchId;value.WorldId=context.WorldId;value.WorldGeneration=context.WorldGeneration;
                value.LevelId=context.LevelId;value.LevelGeneration=context.LevelGeneration;value.BindingGeneration=context.BindingGeneration;value.CommandId=context.CommandId;value.DrawVersion=draw;value.ChoiceDefId=choice;});
            using(var reply=BuffReply(replay))Assert.That(reply.Message,Is.EqualTo("WrongLevel"));
            Assert.That(Buff.SelectedId,Is.Zero);Assert.That(Buff.Candidates,Is.EqualTo(candidates));
        }

        [TestCase(5,1,2,3)]
        [TestCase(1,1,4,1)]
        public void SoloBuffCookingScaleRoundsUpAndKeepsAtLeastOneTick(int baseTicks,int numerator,int denominator,int expected)
        {Assert.That(BuffConfig.ScaleTicks(baseTicks,numerator,denominator),Is.EqualTo(expected));}
        [Test]
        public void SoloBuffCookingNewRealSoupFreezesSelectedDurationAndPausesNormally()
        {
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(101), pool: new[] { 1, 2, 3 }));
            StartSolo(liveBuff:true);BuffDo(Buff.Candidates[0]);DRBuffConfig definition=Tables.Instance.DTBuffConfig.Get(Buff.SelectedId);
            long pot=SoupFillPot(host.Client);GridAutoProcess batch=SoupBegin(host.Client,pot);
            int expected=BuffConfig.ScaleTicks(Tables.Instance.DTGridAutoRecipeConfig.Get(batch.RecipeId).RequiredTicks,definition.DurationNumerator,definition.DurationDenominator);
            Assert.That(batch.RequiredTicks,Is.EqualTo(expected));Assert.That(expected,Is.LessThan(120));AdvanceTicks(4);int progress=batch.ProgressTicks;
            LevelDo(LevelControlAction.Pause);AdvanceTicks(20);Assert.That(batch.ProgressTicks,Is.EqualTo(progress));
            LevelDo(LevelControlAction.Resume);AdvanceTicks(expected);Assert.That(batch.State,Is.EqualTo(GridAutoState.Ready));Assert.That(batch.RemainingPortions,Is.EqualTo(2));
        }
        [Test]
        public void SoloBuffCookingChoosingDoesNotRewriteAlreadyStartedSoupOrMaterials()
        {
            StartSolo(liveBuff:true);long pot=SoupFillPot(host.Client);GridAutoProcess batch=SoupBegin(host.Client,pot);long[] inputs=Grid.Item(pot).Contents.ToArray();int portions=batch.RemainingPortions;
            Assert.That(batch.RequiredTicks,Is.EqualTo(120));BuffDo(Buff.Candidates[0]);
            Assert.That(batch.RequiredTicks,Is.EqualTo(120));Assert.That(Grid.Item(pot).Contents,Is.EqualTo(inputs));Assert.That(batch.RemainingPortions,Is.EqualTo(portions));
        }
        [Test]
        public void SoloBuffCookingCarryKeepsOldBatchAndRejectsOldLevelChoiceWithoutInheritance()
        {
            InstallBuffLevel(BuffLevel(Tables.Instance.DTLevelConfig.Get(101),duration:20,pool:new[]{1,2,3}));StartSolo(liveBuff:true);
            BuffDo(Buff.Candidates[0]);var projection=((ClientSessionComponent)host.Client).GetComponent<GridClientComponent>();
            long pot=SoupFillPot(host.Client);GridAutoProcess batch=SoupBegin(host.Client,pot);int required=batch.RequiredTicks;
            long level=CurrentLevel.Id,generation=CurrentLevel.Generation;int oldChoice=Buff.SelectedId;
            LevelDo(LevelControlAction.Start);AdvanceTicks(22);Assert.That(CurrentLevel.Phase,Is.EqualTo(LevelPhase.Completed));Assert.That(batch.State,Is.EqualTo(GridAutoState.Cooking));
            LevelBuff retired=Buff;LevelDo(LevelControlAction.Continue);
            Assert.That(retired.IsDisposed,Is.True);Assert.That(Buff.SelectedId,Is.Zero);Assert.That(batch.RequiredTicks,Is.EqualTo(required));Assert.That(Grid.Item(pot).AutoProcessId,Is.EqualTo(batch.Id));
            var stale=projection.CreateBuffChoice(oldChoice,1);stale.LevelId=level;stale.LevelGeneration=generation;
            using(var reply=BuffReply(stale))Assert.That(reply.Message,Is.EqualTo("WrongLevel"));
            Assert.That(Buff.SelectedId,Is.Zero); FrontWait(() => Buff.Prepared, 8);
            BuffDo(Buff.Candidates[0]);Assert.That(batch.RequiredTicks,Is.EqualTo(required));
        }
    }
}
