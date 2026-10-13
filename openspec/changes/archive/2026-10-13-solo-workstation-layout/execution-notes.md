# Execution Notes

## 最终UI与关卡身份交接（2026-10-12，协议12）

当前6/6。完整UI普通Launcher世界`e1df831fed3f478982fc2ad6bebde671`实际餐桌移至(3,4)、座位(4,4)，带料台移动旋转、重叠零写入、切配台原工作接续、移带锅灶台不重置新60 Tick批次及营业旧按钮WrongPhase；同桌新位置实际顾客入座、伙伴询问、玩家出餐／清桌／洗碗。详细对象与行为复用[UI最终记录](../2026-10-13-solo-restaurant-ui/execution-notes.md)，未改变地形／作业规则不重复原小集。

最终Layout改为先校验当前上下文再返回最新重放；新增四参数关卡UTF证明另一真实Peer换关后原旧布局请求WrongLevel，新布局／库存不变。本组`SoloStationLayoutRealMailboxRotatesOnceAndReplaysOnlySamePayload`单项新运行Passed，合法最新重放与冲突规则仍成立；失败、修复及运行详情见[流程最终记录](../2026-10-13-solo-restaurant-level-flow/execution-notes.md)。

## 同一实际地图的最终集成验收（2026-10-12，协议 11）

本 change 当前 6/6。普通 Launcher 世界 `301cae1fcfba46e9a1042a24607fd723` 通过真实输入先把餐桌1移到 `(3,4)`／新座位 `(4,4)`，再带料移动／旋转备料台、拒绝重叠、移切配台保留同工作、移带锅灶台继续煮制、开店后拒绝旧调整按钮。之后实际顾客在同桌新座位入座、伙伴走位询问、玩家前往交付并清桌／洗碗；沿同一地形完成服务和自然收口。完整命令、对象身份和结果复用 [服务执行记录](../2026-10-13-solo-customer-order-service/execution-notes.md)，不重复未变路径／配方。

实际共享地形 UTF SoloStationTerrainPlayerCompanionAndActualCustomerUseMovedTableTerrain 已 Passed；其他工位与布局规则复用此前已记录结果。新增池内成员随工位移动由 SoloCustomerDrainLastActualWashBlocksCompletionAndNextLevelKeepsCleanPool 的真实本地布局／取回验证 Passed。主要风险是移位丢物／重置工作／不同角色读取不同占地，上述结果已覆盖，不扩大为穷举布局或性能平台。完整 UI 像素、手机和 LAN 未验证，后续真实 UI 结构改动再覆盖相关入口。

## 自动客流／调度与投影接入（2026-10-12，协议 10）

本阶段已验收自动来客、FIFO 真实入座、询问后开单、未出餐超时零分走出入口，以及伙伴询问优先、同类 FIFO、不抢断已开始洗碗和 Board 角色显示。完整 Serve、脏餐具制作限制／取回和最后一次实际洗碗自然收尾仍 NotRun；不把未出餐自然离店替代完整服务回收链。

主风险是队列无界、未到位开单、调度抢断和 wire／Host 状态泄入 view。用默认源的真实来客和原业务 Tick 验证，补同类任务、共享地形与各 Peer 独立复制，再从 Launcher 观察同一局；不扩展经济模拟或菜单矩阵。旧隔离 fixture 只把下一次来客／调度时点设到测试窗外，产品入口和 liveFront 用例使用默认 20 Tick／自主调度；没有产品测试开关，不能据隔离结果宣称自动行为。已变行为实际补跑，未改源表复用前厅配置 Check／生成。

### 接入与生成

GridCustomerFlow 在同一 Kitchen.Blocked／FindGridPath 上推进 Entering→Queued→WalkingToSeat→WaitingAsk→Ordered→Leaving→Gone；Eating 的时序入口已预留并接入，合法 Serve 尚待下一任务。队列包含未入座到店顾客，容量满则跳过且不创建隐藏实体；桌位 reserved／occupied，真实出口完成后释放未用餐桌。Draining 不再产生来客，已有队列和订单自然继续。

ScheduleCompanions 只在空闲／无在途／此前命令已裁决时选择最早合法 Ask，没有 Ask 时最早 Wash；每次从 Role 自己的 owner 发原本地 Call。NextScheduleTick 是调度时点，领取与目标完成仍由原统一工作引擎写入；不抢断、不远处完成、不以通知加成长。

ZZZFrontWork 源末尾追加 GridCompanionState／GridFrontWorkState／GridCustomerState／GridOrderState／GridTableState，10090–10094；ZZGridKitchen 扩展原 Snapshot／Actor／Station／Item 字段，含进度、路线、成长、队列、卫生和池身份。正常 Proto2CS 退出 0，Client／ClientServer 生成，旧 Opcode 10083–10089 保留；协议 10。每 Peer 独立 DTO 经原 LocalDelivery／ClientSession MailBox／GridSnapshotHandler，全部复制只读数组和普通值后同版安装，再发布观察事件；无新增 SceneType、Fiber、listener 或 wire 白名单。

Board 继续归 GFEntityComponent 的原 GridKitchenBoard，新增渲染对象只由其 RenderRoot／ReleaseBoard 管理，不改 Prefab／CodeBind／注册。紫色伙伴显示当前工作、百分比与关内次数，橙色顾客显示阶段；托管 HUD 的询问／清桌／洗碗与停手按钮冻结工作 ID、ClaimSerial、世界／关身份，用所属值等待发送。完整 HUD、订单操作与更清楚的排版仍归 UI change，不把当前基础显示称为最终 UI。

### UTF 证据

PlayMode 实际发现后执行，以下新功能共 11 个独立用例 Passed，所有执行退出 0，Console 覆盖完整。

| 功能 | 实际用例（ET.ProductSessionTests） | 结果 |
| --- | --- | --- |
| 默认节奏、真实到店／入座及询问开单 | SoloCustomerFlowDefaultArrivalWalksRealTerrainAndAskOpensOrder | Passed；准备态无客，20 营业 Tick 首客，真实路径／操作格，询问完成才开独立 Order |
| 未询问零惩罚、玩家参与 | WaitingAskHasNoPenaltyAndPlayerCanOpenRealOrder | Passed；等待无订单／扣分，真实玩家工作后开单 |
| 满队列跳过与 FIFO | FullQueueSkipsWithoutHiddenCustomerAndSeatsFifo | Passed；合法密集客流配置，2 桌＋2 等候，跳过不增实体，空桌给队首 |
| 超时零分、真实走出与自然收口 | UnservedOrderExpiresAndCustomerActuallyWalksOutForZero、DrainingStopsNewArrivalsAndNaturallyFinishesRealOrders | Passed；到期不是瞬间消失，Gone 在入口；Draining 无新客，已有订单实际结束；不覆盖最后洗碗 |
| 全局停钟 | PauseFreezesArrivalMovementAndOrderDeadlineClock | Passed；到店移动、来客与订单时钟冻结，恢复继续 |
| 询问优先／不抢断、同类 FIFO | SoloCompanionScheduleAskPriorityAndStartedWashIsNotPreempted、SameKindUsesCreationFifo 两参数 | Passed；完成接口实写订单和原餐具卫生，不抢已开始 Wash |
| 独立同版前厅投影 | FrontSnapshotCopiesEveryRoleAndTargetValue | Passed；改第一个 DTO 不改已安装值、第二 DTO 或 Host 状态 |
| 原玩家／伙伴／实际顾客共享移桌后的地形 | SoloStationTerrainPlayerCompanionAndActualCustomerUseMovedTableTerrain | Passed；准备态真实布局命令，三种路线避新占地，顾客在原桌新座位实际入座开单 |

相关回归 SoloCompanion 32/32、SoloCustomerTargets 15/15、SoloLevel 10/10、GridWire 3/3 均 Passed，共 60 个独立用例；包含上面 4 个 Schedule，不重复计数。既有工位源非法重叠／越界／入口拒绝复用前厅目标阶段的实际结果，初始化规则未变。编译 errors=0、warnings=0；中间 fixture 少 using System.Linq、view 使用了不存在的 Grid 字段／未导入 Game 扩展，已修正。域重载期间两次 preflight 连接 reset／400，未启动 UTF 作业；Editor ready 后才重新发起，不把连接异常称为测试失败或通过。

### 普通 Launcher 与画面

第一局世界 `057d854e386e4bae9e156dbd987bd6d3`：Temp/SoloLiveFrontAcceptance.cs 的 Observe 通过真实 PointerClick 开店，等待系统生成的询问工作；伙伴与顾客实际标签存在，真实暂停后进度 2/30 和 Tick=483 冻结，真实拖动平移到前厅。Finish 真实恢复后独立订单 1284030697832572 开单、超时 Expired，原顾客从入口离店，Score=0，全部 success=true／diagnostics=[]。没有写 Host 领域状态。Console session `68cf1167f0ba4ee692e314b87dcdb762` seq 1935–1966，32 条、无新增 Error；1 条已保留的 slow actor rpc 警告为 LevelCommandRequest 2064ms，若实际持续卡顿再查，不额外扩大性能测试。

Pipeline 的 game screenshot 返回了全黑帧，不能据此证明可见；第二局世界 `cad0122042d24c67a0cae11ad3f6dc80` 重做 Observe，实际暂停到进度 1/30、Tick=1203。通过同一个运行 Board camera 的 RenderTexture 渲染，生成并实际查看 `Unity/Temp/pipeline-screenshots/solo-live-front-camera.png`（700×900），紫色伙伴“询问 3% · 0/3”和橙色“顾客 1／等询问”可见。此图是实际相机渲染，不是修改或生成示意图；完整屏幕 HUD 像素截图的黑帧限制仍保留给 UI 验收。

自有 Play 已正常停止；第二局 Console seq 1967–1998，32 条、reset=false、dropped=false、新增 Error=0、原生 compilationFailed=false。原始结果仍在 Unity/Temp/pipeline_test_status.json 和原 Editor log，未复制／改写。任务范围未包括完整制作服务回收链，伙伴／顾客整体验收任务保持未完成。


## 先前阶段记录


当前实施 4/6 完成；任务 1.2、3.2 中的伙伴／顾客真实路径仍待对应 change 接入。下面区分已运行功能与建立提案时的规划记录。

菜品交接后：当前协议 8，初始源布局增面包台／盘台两个普通工位和有限实物，现为十个实例、十一占地格；实际烘烤已接入，合法烤箱变换保留输入与同一作业。受源布局改变影响，SoloStation 22/22 已在菜品阶段重验通过，见 [菜品执行说明](../2026-10-13-solo-dish-recipe-chain/execution-notes.md)。以下“八个实例／空烤箱／协议 7”描述原工位交付基线，伙伴／顾客路径仍 NotRun。

上述为建立提案时的记录；后续实施结果如下。

## 工位基础实施（2026-10-12）

`StationConfig.xlsx` 八条能力定义、`InitialLayoutConfig.xlsx` 八个真实初始实例已注册 c,s,e；原七工位和材料保留，新增空两格烤箱。池目前只是有限容量定义，实际前厅仍未接入。五目标 Check 5/5、正常生成 5/5 和四语言本地化成功，工具退出码均 0；Client／ClientServer／Editor 的两张 DT／DR、bytes／JSON 以及 Config/Luban 副本均由源正常生成。协议 7 仅追加原 Station／Snapshot 字段，旧 Opcode 不变。

PlayMode 实际发现 12 项 `SoloStation`，通过现有 AutoTesting 运行：12 Passed、0 Failed、0 Skipped、0 Inconclusive，退出码 0；本轮 Console 覆盖完整。原始结果保留 `Unity/Temp/pipeline_test_status.json`，没有复制或改写。启动中的两次编译发现 ET0004（Hotfix 的计算属性）及 ET0013（静态依赖环），已分别改为纯方法和单向几何／定义访问；最终 recompile_status 为 completed、failed=false、errors=[]。编译结果不替代下述功能 UTF。

| 任务／功能 | 实际 UTF 或操作 | 结果 |
| --- | --- | --- |
| 1.1 配置、单槽、有限池、烤箱能力 | SoloStationConfigActualDefinitionsHaveSingleSlotsAndFinitePools；RejectsInvalidCapacityAndGeometry 四种非法定义；TwoOvensKeepIndependentRuntimeIdentityAndSlots | 6 Passed；烤箱基础交接给菜品 change，设备说明已更新 |
| 1.2 初始布局和操作位 | SoloStationTerrainInvalidInitialLayoutLeavesNoHalfWorld 四种布局；RotatesFootprintAndKeepsConfigurationUnmodified；OvenUsesDeclaredApproachAndCopiesGeometryToProjection | 6 Passed；真实伙伴／顾客行走仍 NotRun，任务暂不勾选 |
| 2.1 运行布局命令、版本与完整预检 | SoloStationLayoutRealMailboxRotatesOnceAndReplaysOnlySamePayload；RejectsBoundaryOverlapAndEntranceWithoutMutation 三项；RejectsClosingLastEntrancePath；RejectsRoleAndRealDisconnectedDrop；RechecksVersionAfterQueuedWinnerAndRejectsPausedOrOperating；CancelsFutureRouteButPreservesCurrentStep | 8 Passed |
| 2.2 身份／进度／认领与更新 | SoloStationPreserveMovedChoppingRetainsMaterialProgressAndReleasesOldClaim；CookingStoveKeepsOriginalPotContentsAndBatch；下述真实 HUD／Board 操作 | 2 Passed；同一原料、工位和作业接续，原锅与批次继续 |
| 3.1 地形迁移相关 Grid 小集 | GridCore 四项；GridPrepWork 两项；GridSoupCooking 两项 | 最终 8 Passed，均退出码 0；首次 GridPrepWork 失败见下文 |
| 3.2 普通 Launcher 的实际移动／旋转 | 带材料移动旋转、非法重叠、切配移位接续、带锅灶台移位、开店后旧调整按钮拒绝 | 玩家操作 Passed；真实伙伴／顾客路径仍 NotRun，任务不勾选 |

布局接入后实际发现 22 项 `SoloStation`，最终 22 Passed、0 Failed／Skipped／Inconclusive，退出码 0；该次覆盖新增命令与同一服务步中原配置、地形和投影的接入。现有 Grid 小集按受影响的移动／认领／自动加工选取八项；其它未改变的汤份数、请求退出与关卡结果复用各自执行说明，不追加全仓套件。所有本次成功运行 Console 覆盖完整。

首次 `GridPrepWork` 是 1 Passed／1 Failed：`GridPrepWorkChoppingStopsResumesMovesAndProducesOnce` 在 `GridPrepTests.cs:123` 期望命令 7、实际停在 6；Console 两项 KeyNotFoundException 来自测试手建第二切配台的 DefinitionId=0，路径为 DTStationConfig.Get → GridStationAccess → InstallIntent／CreateSnapshot。迁移该唯一旧夹具，从真实切配定义赋值身份、容量及能力；随后新运行 2/2 Passed。原生失败输出保持原位置，由后续运行正常更新；原始 Editor 日志在 `Unity/Temp/solo-nine-changes-editor-interactive.log`，没有续跑或改写失败结果。新增 HUD 文件的 CS1061 缺少 Game 命名空间也已修正，最终编译 failed=false、errors=[]。

## 普通 Launcher Solo

本工程现役 Editor PID 39328，打开 `Assets/Launcher.unity` 进入普通 Solo。临时 `Temp/SoloStationAcceptance.cs` 通过实际 HUD PointerClick 与地图 ProcessGridPointer 操作，只从客户端投影核对结果。World `2df264acafd1404285e0a0d2864d09cd`：首段 8 个命令，将带番茄备料台移到 `(2,12)` 并旋转，拒绝移到碗台占地；切配开始后将原台移到 `(5,7)`，原进度暂停、原认领释放，再接近新操作位完成同一切配。Board 标签实际跟随新位置，新番茄块只产生一个。

第二段累计 20 个命令，用真实切配成果和手持打蛋向原锅投料，启动煮汤后将灶台移到 `(1,7)`，检查同一锅、输入内容和批次继续计时。开店与旧旋转按钮同帧 PointerClick，最终阶段 Operating、旋转 WrongPhase、LayoutVersion 保持 5；原批次正常 120/120 Ready、剩 2 份。场景目标格检查 HUD 无遮挡，一次点击只发一个业务命令。截图 `Unity/Assets/Temp/pipeline-screenshots/solo-station-layout.png`（1280×720）已查看，展示两格烤箱、移动后的设备和原汤批次；截图只证明可见表现，操作事实来自上述断言。

运行及退出 Console session `68cf1167f0ba4ee692e314b87dcdb762`，seq 791～823 共 33 条连续、reset=false、dropped=false，原生 Error=0、compilationFailed=false。有两条 StationLayout RPC 大于 200ms 的警告（320／393ms），未据此建立性能门槛；若普通持续操作出现可见卡顿再定位。Play 已由本轮退出，Editor ready／stopped。

普通初始化、交互和投影读取真实配置；候选旋转校验不安装运行地形。伙伴／顾客接入后须用同一地图补真实路径证据，不能用玩家用例代替。声明八种能力不表示八种营业设备已实际启用；实际前厅和烘烤仍由对应 change 接入。

## 原规划验证取舍

规划检查：2026-10-12，`openspec validate solo-workstation-layout --strict` 返回有效，退出码 0；proposal／specs／design／tasks 齐全。建立提案时关联九案共 46 份 Markdown 的本地引用、UTF-8 与行尾空白检查通过，当时功能均 NotRun。当前运行记录以以上正文为准。

主要风险是移动丢失物品／作业、堵住必要目标和复用错误地形。验证一条合法带物／带作业调整、一个旋转与代表性堵路／旧版本拒绝，再走真实准备态入口足够；不做布局穷举或专用 runner。接受首期设备子集和 NPC／顾客不占玩家停驻名额的样机行为；新增设备占地、前厅入口或角色碰撞规则时补受影响路径验证。

| 任务／功能 | 拟 UTF 或实际操作 | 结果 |
| --- | --- | --- |
| 1.1 能力／容量 | SoloStationConfig：真实 Tables、单槽与池有限容量 | NotRun |
| 1.2 同一地形 | SoloStationTerrain：全部路径消费者与非法初始布局 | NotRun |
| 2.1 布局完整预检 | SoloStationLayout：合法／越界堵路／占物／旧版本／阶段拒绝 | NotRun |
| 2.2 关系保留 | SoloStationPreserve：ItemId、内容、ProcessId、进度与认领／路线 | NotRun |
| 3.2 真实操作 | Launcher 准备态调整、开店后拒绝、可见物品继续 | NotRun |

未接伙伴／顾客时其真实行走仍 NotRun，不由玩家寻路用例替代。工位基础能力先交菜品，布局收尾不反向阻塞烘烤。

## 2026-10-13 主规格同步与归档

复用各领域已运行 UTF 和普通 Launcher 结果，以及九案最终逐项核对；本次仅同步规格、移动规划目录和修复引用，没有改变功能，不重复执行未变测试。两个 product-session delta 是互补新增要求，先合并本地消息合同，再合并关卡隔离和暂停恢复合同，原会话要求全部保留。
10 项已完成 change 共 69/69 任务，归档前各自严格校验通过，合并后 30 份主规格严格校验通过；日期目标已检查无冲突，目录未嵌套。新增 Sources 指向实际源码和归档执行记录。五个后续 change 仍未完成；没有提交、推送或操作第二工作区。
