# Execution Notes

## 当前配置、候选与效果（2026-10-12，协议 12）

BuffConfig、关卡允许池／数量／门槛、固定候选、一次本地选择和冻结新煮制作业已实施；当前6/6。真实Launcher候选重开、选择、新旧锅及下一关复选已补验；实际UI重开UTF已由UI change接入，不以只读领域测试冒充。首版三种煮汤耗时倍率3/4、2/3、1/2，每关从三选二并要求选一；属于样机变体，不是三种已定稿效果。

主风险是重开刷候选、重复叠加、选择改写旧锅和跨关继承。使用受控 Seed／真实本地 Call／新旧真实配方与原时钟验证，并复用当前未改工作／服务结果；无随机频率实验或性能门槛。接受当前 Unity 运行环境内同 Seed 复现、不承诺不同 Seed 必定不同。手机、LAN、存档和新效果种类未验证，进入该范围时再补验。

源 BuffConfig.xlsx 注册 __tables__ 第31行 c,s,e；LevelConfig 追加 AllowedBuffIds、CandidateCount、RequireBuffChoice。标准 ZIP/XML 编辑保留其他成员，Check 5/5、正常生成 5/5及四语言均成功，退出 0。ZZZZZLevelBuff.proto 末尾追加 ChooseBuff 四类型 10099–10102，ZZGridKitchen Snapshot 追加 42–46，正常 Proto2CS 生成；未手改派生数据。最终编译 errors=0／warnings=0。

LevelBuff 是当前 Level Component，只保存 Seed／DrawVersion／候选普通值、已选和普通值命令队列。仅创建关时抽一次；候选开关只是 Presentation 显示状态。玩家经原所属 Call 到 Peer mailbox，再可信内部 Submit 到 authority mailbox；快答只接受，统一步完整验证后选择一次。暂停取消尚未执行选择，恢复不重放；已选门槛在 Start 验证。新适用汤在 Begin 冻结向上取整的 RequiredTicks，推进只读该值；旧作业和材料无重写。

非 buff 领域 UTF 使用真实可配置的可选加成关卡 fixture：在测试创建前通过 DRLevelConfig ByteBuf 把 RequireBuffChoice 配成 false，保留实际池／数量及所有其他字段，并同步同一测试 World 的 DataList／DataMap；不让那些原先无选择的测试暗中发送额外业务编号。产品源两关均必选，SoloBuff/liveBuff 和 Launcher 保留真实门槛，没有产品测试开关。新 LevelConfig schema 的旧构造 fixture 同步追加合法空池／零数量／可选字段。

| 任务／功能 | 实际 PlayMode 用例（ET.ProductSessionTests） | 结果 |
| --- | --- | --- |
| 1.1 实际源、比例／引用、必选及可选空池 | SoloBuffConfigActualSourceHasThreeCookingVariantsAndRequiredTwoCandidates；RejectsInvalidDurationRecipeOrEffect 四参数；RequiredEmptyPoolRejectsBeforeHostCreation；OptionalEmptyPoolCanActuallyStartWithoutChoice | 7 Passed；实际表与 Host 创建前拒绝，无加成关真实开店 |
| 1.2 固定抽样与独立快照 | SoloBuffDrawControlledSeedRepeatsWithoutDuplicatesAndTruncatesSmallPool；ActualLevelKeepsSeedCandidatesAcrossTicksAndSnapshotRecycling | 2 Passed；同 Seed、无重复、小池截断、输入不改；复制后回收 DTO 不改 Host／已安装候选 |
| 2.1 一次选择、门槛和身份 | SoloBuffChoiceGateRejectsStartUntilOneCandidateCommitsAndLocksNewChoice；InvalidCandidateOrDraw 两参数；PausedAndOperatingCallsRejectWithoutAnotherSelection；LatestReplayAndPayloadConflictReturnExistingFacts | 5 Passed；Accepted 时尚未选择，Tick 锁定，同版投影；改选、旧抽样／非法候选、暂停／阶段和载荷冲突拒绝 |
| 2.2 新锅／旧锅、ceil、下限与跨关 | SoloBuffCookingScaleRoundsUpAndKeepsAtLeastOneTick 两参数；NewRealSoupFreezesSelectedDurationAndPausesNormally；ChoosingDoesNotRewriteAlreadyStartedSoupOrMaterials；CarryKeepsOldBatchAndRejectsOldLevelChoiceWithoutInheritance | 5 项各有 Passed；新真实锅加速，旧锅不改，停钟，新关选择归零和旧关请求拒绝 |
| 3.1 改变的汤／关卡／协议回归 | GridSoup 11、SoloLevel 10、GridWire 3 | 24 Passed，Console 覆盖完整；合法可选配置不改变旧厨房流程，原独立 Fiber loopback KCP 和同版复制通过 |

初次 SoloBuff 19 项为 18 Passed／1 Failed：新断言错把 Cooking 阶段尚未产出的 RemainingPortions 0 当 2。修正为比较选择前后的同一份数，单项新运行 Passed；未修改生产规则或原失败输出，其他18项行为／实现未变不重复跑。以上19个独立功能点各有真实通过证据，工具最终运行退出 0；原始输出保留原 Pipeline／Editor log，未复制或建立状态副本。

补查发现另一个本地 Peer 换关时，第一个 Peer 的最新成功 Choose 仍可能是旧关编号，不能先返回重放事实再校验当前关。因此 QueueBuffChoice 先校验世界／关／绑定，再检查当前关最新重放。新增 ExactOldReplayRejectsAfterAnotherLocalPeerContinuesLevel 经两个真实本地 Peer 验证旧原命令明确 WrongLevel、新关不变；SoloBuffChoice 6/6及 CookingCarry 1/1 新运行 Passed。现在共有20个独立 Buff UTF 各有 Passed。此改动没有传输或 UI 行为改变，复用其已有结果。

### 普通 Launcher 的两局验证

完整UI最终补验使用世界`e1df831fed3f478982fc2ad6bebde671`：真实候选关闭重开不变、选择旺火1/2，新锅60 Tick、暂停冻结；实际10分1星换关后未选、新关再次选择，默认未出餐订单自然到0分0星并继续。ZeroStarAndContinue执行44847ms取得success=true／diagnostics=[]，补足早期30秒观察超时未取得方法终态的限制。整个HUD关闭重开复用原Mono、候选及编号不变的UTF和真实操作均已通过，详见[UI最终记录](../2026-10-13-solo-restaurant-ui/execution-notes.md)。早期两局记录及Timeout保留原位。

后续UI接入：ET.SoloRestaurantUiTests.SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen实际加载原受管Prefab，真实隐藏／查看候选不改变Seed／列表或业务编号；真实关闭／重开整个HUD两轮并复用原Mono，候选仍保持。PlayMode实际发现／执行Passed、Console覆盖完整，详细owner和资源证据见 [UI记录](../2026-10-13-solo-restaurant-ui/execution-notes.md)。此实际UI行为补足任务1.2的剩余验收。

Temp/SoloBuffAcceptance.cs 经原 PointerClick／ProcessGridPointer 制作真实材料，没有写 Host 状态。第一局世界 `44748bd5a45c4c16b0c7f1b433bec6db`、Seed=1310322354、候选 `1,3`：在选择前开锅，实际隐藏／查看候选保持 Seed／Draw／列表且不发送命令，选择1后原锅仍 RequiredTicks=120、原材料不变，暂停进度12冻结。PrepareIngredients／OldSoupBeforeChoice success=true、diagnostics=[]。

第二局世界 `e1030a3757184d7aa7ebed55a64a4813`、首关 Seed=-26319745／候选 `3,2`：同样实际重开后选择3，开店再煮汤，原材料保持、新锅 RequiredTicks=60；真实暂停冻结，再恢复熟到60／60、两份。SelectedNewSoup success=true、diagnostics=[]。ZeroStarFinishAndNextChoice 的工具内部 timeout_ms 默认30秒到期，明确 Timeout，Task继续，不把该调用计通过、不重新发送营业或下一关命令。随后 Inspect 和只读 ConfirmNextChoice 新调用补验成功：同一世界已从 Level `1284035512893466` 到 `1284035718415251`，Preparing，Seed=-780893789／候选 `3,1`、重新选择3；原批次 `1284035625091434` 仍60 Tick／2份，新关前厅空、分数与星数0。

原异步方法只有在实际完成阶段、旧关0分0星、存在超时订单的断言通过后才会点击“下一关”，之后又检查新关未选、旧批次保持，再执行候选重开／复选。补验观察到了全部后续效果，结合这条受保护的输入顺序确认原零星自然收尾；未捕获原调用的最终返回，不伪称其终态 Passed。后续需要长等待时同时指定内部 timeout_ms，不只指定外层 CLI --timeout。

自有 Play 均停止。Console session `68cf1167f0ba4ee692e314b87dcdb762` 第一局 seq2372–2398／27条、第二局2404–2431／28条，reset=false／dropped=false、Error=0；各保留一次 GridCommandRequest slow rpc 2770ms／2848ms，发生在临时脚本编译期间，若正常游玩持续出现再查，不追加性能实验。完整 UI 结构／加载关闭／真实重开 UTF 仍归 UI change。

## 原规划记录

规划检查：2026-10-12，`openspec validate solo-random-level-buffs --strict` 返回有效，退出码 0；proposal／specs／design／tasks 齐全。关联九案共 46 份 Markdown 的本地引用、UTF-8 与行尾空白检查通过。本记录仅证明规划有效，全部新增功能运行仍 NotRun。

本轮仅规划，全部功能、生成、UTF 和 Launcher NotRun。用户确认随机候选中选一个，本关生效；拟测试执行前需发现。

主要风险是界面刷候选、重复选择叠加、修改旧锅和换关继承。受控种子／真实选择请求／新旧作业耗时小集加真实 Solo 选择即可；不做随机公平实验、统计采样或包体。接受首版只有煮制加速的配置变体、样机候选数量和当前 Unity 环境内种子复现；新增效果种类、跨运行时复现或保存恢复时补相关验证。

| 任务／功能 | 拟 UTF 或真实操作 | 结果 |
| --- | --- | --- |
| 1.1 配置门槛 | SoloBuffConfig：真实 Tables、比例／引用和必选／可选空池 | NotRun |
| 1.2 候选固定 | SoloBuffDraw：固定 Seed、小池、无重复与重开 | NotRun |
| 2.1 一次选择 | SoloBuffChoice：真实 Call、门槛、锁定／重放、代次与投影 | NotRun |
| 2.2 冻结耗时 | SoloBuffCooking：ceil／下限，新旧锅、暂停与跨关 | NotRun |
| 3.2 真实入口 | Launcher 候选选择、开店煮汤、暂停和新关选择 | NotRun |

各 Seed 可以产生相同候选，不以偶然不同作为随机正确性证明。与流程／汤共享同次运行可复用未变覆盖，不把本次规划检查写成玩法通过。

## 2026-10-13 主规格同步与归档

复用各领域已运行 UTF 和普通 Launcher 结果，以及九案最终逐项核对；本次仅同步规格、移动规划目录和修复引用，没有改变功能，不重复执行未变测试。两个 product-session delta 是互补新增要求，先合并本地消息合同，再合并关卡隔离和暂停恢复合同，原会话要求全部保留。
10 项已完成 change 共 69/69 任务，归档前各自严格校验通过，合并后 30 份主规格严格校验通过；日期目标已检查无冲突，目录未嵌套。新增 Sources 指向实际源码和归档执行记录。五个后续 change 仍未完成；没有提交、推送或操作第二工作区。
