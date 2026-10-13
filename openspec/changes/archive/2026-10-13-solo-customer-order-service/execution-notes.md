# Execution Notes

## 最终完整界面与默认两关（2026-10-12，协议12）

当前7/7。完整UI普通Launcher世界`e1df831fed3f478982fc2ad6bebde671`再次经真实备料、做汤、移动后桌位询问开单、交付、用餐离席、玩家清桌、部分洗碗和伙伴接续完成原碗回收；同次调用直接观察伙伴第二次认领Working、进度约34%及暂停冻结，恢复洗净后10分1星自然结算并取回同碗。第二关实际未出餐、订单Expired，0分0星自然收尾且可以继续。全部长调用取得成功终态，原前阶段中间观察超时保留；完整对象ID、截图和操作证据见[UI最终记录](../2026-10-13-solo-restaurant-ui/execution-notes.md)，不再将黑帧或间接终态当作当前UI证据。

最终Serve上下文先于最新重放核对，另一真实Peer换关后旧原交付请求WrongLevel；新增四参数关卡用例及`SoloCustomerServeLatestReplayAndPayloadConflictNeverConsumeOrScoreTwice`单项新运行Passed。失败与修复结果见[流程最终记录](../2026-10-13-solo-restaurant-level-flow/execution-notes.md)；当前关真实两菜、卫生、FIFO及统一工作未改变，复用原通过结果。

## 出餐、有限餐具与真实关尾（2026-10-12，协议 11）

类型化 Serve、脏餐具操作限制和干净池取回已接入并通过行为 UTF。普通 Launcher 完整服务链及自然收口／下一关同碗取回已验收，当前 7/7；不把这套基础入口称为后续完整 UI 已完成。

主要风险是旧订单错交、重复计分、提前消费以及回收时产生新餐具。相称验证采用两道真实源配方、实际自动顾客、同桌下一客、待执行交付暂停取消、五类脏容器操作和最后实际洗碗停钟／排空；原目标接口池满保留、非法配置和伙伴单执行者／成长结果可复用，源配置及这些行为未变。未测 LAN、手机 Player/AOT、持久存档或正式平衡；本轮没有新增监听或传输白名单，发现该路径的实际回归再补验。

ZZZZServe.proto 正常 Proto2CS 生成 ServeOrderRequest／Response／SubmitRequest／SubmitResponse，追加 10095–10098；协议从 10 升至 11，未手改生成代码。完整预检后才消费单份真实内容、原容器留桌、订单 Served 并计分；快答 Accepted 不算执行。普通台面操作不能交付，Table／特殊池不能用普通放物绕过规则。CleanPool 取回固定原 ItemId；池中物品随合法工位移动更新位置。

| 任务／功能 | 实际 PlayMode 用例（ET.ProductSessionTests） | 运行结果 |
| --- | --- | --- |
| 2.1 两道实际菜品、消费一次、原餐具与同版投影 | SoloCustomerServeBothRealDishesConsumeFoodOnceAndKeepOriginalUtensil 两参数 | 2 Passed；真实制作、自动开单、手槽清空、内容消耗、原餐具留桌，第二交付拒绝 |
| 2.1 最新重放、冲突与四种非法交付 | ServeLatestReplayAndPayloadConflictNeverConsumeOrScoreTwice；ServeInvalidPayloadAndPositionKeepFoodOrderAndScore 四参数 | 5 Passed；Accepted 到 Tick 之间零消费，重复不多计分，错误菜／位置／卫生／顾客零提交 |
| 2.1 同桌新客与暂停待执行请求 | ServeStaleOrderCannotServeNextCustomerAtSameTable；ServePauseCancelsAcceptedUnexecutedDeliveryAndResumeCannotReviveIt | 2 Passed；旧订单不能作用新客，暂停取消排队交付且恢复不复活 |
| 2.2 真实用餐→清桌→洗碗→同碗取回再盛汤 | SoloCustomerUtensilsActualEatClearWashAndTakePreserveSameBowlAndStock | Passed；吃饭占桌，清桌释放、自动伙伴洗净、同碗实际取回，三件餐具身份和总数不变 |
| 2.2 脏容器制作限制 | SoloCustomerUtensilsDirtyContainersCannotPreparePourCookOrPortion 五参数 | 5 Passed；装料、打蛋、倒料、煮汤、盛汤均拒绝，原内容／材料／份数保留 |
| 3.1 最后一项真实洗碗阻塞结算及跨关餐具 | SoloCustomerDrainLastActualWashBlocksCompletionAndNextLevelKeepsCleanPool | Passed；Draining 未完 Wash，暂停 100 步进度冻结，恢复真实完成后才结算 10 分 1 星；换关同碗及池保留，合法移池后实际取回 |
| 3.2 实际改变的厨房与消息回归 | GridPrepContainer 2、GridSoup 11、SoloDish 19、GridWire 3 | 35 Passed；卫生规则未破坏合法配方／盛装／装盘，协议 11 的原独立 Fiber loopback KCP 和快照复制通过 |

上述新增 16 个独立用例和相关 35 个回归均实际发现／执行，最终 failed／skipped／inconclusive 为 0，工具退出 0且 Console 覆盖完整。一次新 cook fixture 漏传 ExpectedTargetItemId，原始失败 TargetChanged（4 Passed／1 Failed）已保留原位；补全真实载荷后五参数新运行 5 Passed。尝试带引号参数的 CLI filter 被 shell 转义为无匹配，明确 NotRun，未计为通过，改用实际方法子串运行。原始输出仍在 Unity/Temp/pipeline_test_status.json 和原 Editor log，未复制／改写。原表未改，本轮复用已记录 Check／生成；C# 最终 errors=0、warnings=0。

### 普通 Launcher 的同一服务链

世界 `301cae1fcfba46e9a1042a24607fd723`，Temp/SoloServiceAcceptance.cs 只经真实 HUD PointerClick 和 ProcessGridPointer，没有写 Host 领域状态。Prepare 在准备态把餐桌1移到 `(3,4)`／座位 `(4,4)`，把带番茄的备料台移动／旋转，拒绝重叠，移切配台后同工作接续完成；CookingMoveAndOperatingReject 真实打蛋、倒料、开锅，把带锅灶台移到 `(1,7)`，开店后旧调整按钮 WrongPhase，原批次继续熟、盛汤后暂停。两段 success=true、diagnostics=[]。

ServeAndPlayerWash 恢复后从真实订单前往原桌，实际顾客在新座位；交付只得 10 分、原汤内容消耗、原碗 `1284033536327687` 留桌，真实吃完／离店后玩家清桌。玩家实际领取 Wash `1284033627554648`，洗到 `0.22950819672131142`，暂停冻结；恢复后停手保留 `0.2786885245901639`，阶段 Draining。任务没有重建，播放器也没有完成洗碗或再领取。

后续 HandoffAndNaturalFinish 的中间 Working 观察启动太晚，该任务已由伙伴洗完，因此该观察超时，明确不计通过；未因此重跑配方和全局流程。FinishAndContinue 对同一局实际最终事实补验：原 Wash Completed、ClaimSerial=2、原碗 CleanPool／Clean；5 个完成 Ask 与伙伴完成次数 6 对应，额外一项正是这次 Wash。结合之前的玩家停手、原任务身份和没有再提交玩家认领的事实，确认伙伴完成第二次认领的原工作。这是实际终态证据，中间进度画面未捕获。

剩余未满足订单实际 Expired，全部顾客与必要工作自然排空后才出现 10 分 1 星。真实“下一关”从 Level `1284033536327706` 到 `1284033796375544`，固定伙伴身份保留、成长 6→0；真实“取回 碗（空）”把同一碗拿到新关手槽，没有补餐具。FinishAndContinue success=true、diagnostics=[]。本局同一材料移位／服务／接续／收尾结果供工位和伙伴 change 复用，不重复跑未变行为。

自有 Play 正常停止；Console session `68cf1167f0ba4ee692e314b87dcdb762` 本局 seq 2206–2232、27 条、reset=false／dropped=false、Error／Warn=0，原生 compilationFailed=false／consoleErrors=0。完整 HUD 像素与生命周期仍由 UI change 验收；此前屏幕截图黑帧限制未被此处的 PointerClick／状态断言消除。

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


2026-10-12：用户已授权完成九个 change。当前目标模型和完成接口已验收，任务 1.1 完成；自动来客、真实认领／出餐／工作推进、自然排空和完整服务链仍 NotRun。模型测试不代替顾客实际流程。

主要风险是旧桌请求命中新客、重复消费计分、无限餐具及未洗完提前结算。此阶段新增源／模型和同步目标接口，用少量配置、目标身份、一次开单／转移、池满保留与真实入口安装检查覆盖；不运行尚未存在的业务套件，不做联网或构建。既有备料、汤／吐司的配方和厨房材料布局未变，其相关已通过结果复用；本次增加前厅地形，实际重验工位、格子移动、角色和关卡。当前仅声明 Hygiene 与位置，不声称脏碗规则或取回入口已接入；接入这些规则时补相关盛装／容器行为。

## 源与接口交接

新增 Product 三张源 `CustomerFlowConfig.xlsx`、`OrderConfig.xlsx`、`TableConfig.xlsx`，__tables__ 第 28–30 行注册 c,s,e。InitialLayout 新增两桌、一个洗碗台、脏池与干净池；15 个工位、16 格占地，原初始物品不增加。已有 Bin/Tool Check,ShowCmd 5/5、正常导出 5/5 和四语言，退出码均 0；ET Client／ClientServer C#/bytes、Config/Luban 和 Editor C#/JSON 正常生成，未手改派生结果。Unity recompile 最终 errors=0、warnings=0。

客流、订单、餐桌目标及完成接口见 [设计交接](design.md)。工作引擎核对 `CheckFrontTarget` 后，进度达到完成条件再调用 `CompleteFrontTarget`；目标成功后引擎才终结工作并计成长。完成接口本身不计分／成长。Ask 生成独立 Order，Clear 转移原 Item 并只创建一项 Wash，Wash 洗净并转入有限干净池；失败保留原状态。本轮协议仍为 8，尚未增加前厅 wire DTO 或 Serve 请求。

## 运行结果与覆盖

实际发现后执行 PlayMode、testName 子串 `SoloCustomerTargets`，新增最终 15/15 Passed，failed／skipped／inconclusive 均 0，工具退出码 0、Console 覆盖完整。

| 任务／功能 | UTF 或实际操作 | 结果 |
| --- | --- | --- |
| 1.1 实际配置、持久餐桌与 owner | ActualConfigAndPersistentTablesHaveUniqueOwners | Passed；实际生成表，15 工位／16 地形、两桌、三餐具、合法路径，准备态无伪造顾客 |
| 1.1 配置引用及建世界前拒绝 | RejectInvalidReferencesBeforeCreatingWorld 的 dish／score／layout／seat／flow；ActualBootstrapRejectsMissingTableBeforeHost | Passed，6 项；真实菜单、客流订单与布局引用，非法输入无半个 Host |
| 1.1 询问门槛、两菜单与一次开单 | AskOpensIndependentOrderOnce 两参数；LateAskCannotOpenForNextCustomerAtSameTable | Passed，3 项；同一桌新顾客不接受旧询问，独立 Order／Customer／Station 身份，零得分／成长 |
| 1.1 原餐具一次转移与失败保留 | ClearAndWashTransferSameBowlOnceWithoutGrowthOrStock；FullDestinationPreservesWorkAndOriginalItem 两参数；PausedCompletionAndWrongLevelHaveNoEffects | Passed，4 项；同碗 Table→DirtyPool→CleanPool、只有一个 Wash、不增库存，池满／暂停／旧关不提交 |
| 候选桌位地形 | LayoutRechecksMovedTableSeatBeforeCommit | Passed；真实本地调整，座位压另一桌时零修改；合法移动后的座位使用原 Station 并可达 |
| 初始布局与新目标安装的影响 | SoloStation 22、SoloCompanionOwnership 11、SoloLevel 10、GridCore 4 | 各点有 Passed：工位最初 21 Passed／1 Failed，修复 fixture 后该 1 项新运行 Passed；其余三组 11/11、10/10、4/4 |
| 实际产品入口 | Launcher.unity Play，Temp/SoloFrontTargetsAcceptance.cs 只读树和真实 Board 文字 | Passed；SoloReady，Host／投影各 15 工位，2 桌，5 个真实前厅标签，3 餐具、干净池容量 4、准备态 0 顾客，无监听 |
| 1.2 自动客流／2.1 Serve／2.2 完整餐具规则 | SoloCustomerFlow／Serve／Utensils | NotRun，尚未实施 |
| 3.1 真实排空／3.2 完整顾客小集／3.3 服务整链 | SoloCustomerDrain；备料→询问→出餐→离席→清桌→洗碗→同碗取回 | NotRun；本次入口只证明加载与目标安装 |

目标接口单元场景设置合法的已入座／已用餐离席、执行者到位与待提交完成进度，用来验证接口效果；不声称这些场景已通过真实客流、工作计时或 Serve。完整验收必须由实际模块生成目标并走真实消息、路径与业务 Tick。

工位回归的旧 `SoloStationLayoutRejectsClosingLastEntrancePath` fixture 在第 3 行补围墙，与新增餐桌 (1,3)／(5,3) 重叠，初始化报 InvalidOperationException: Station footprint overlaps / outside map，栈 GridStationConfig.cs:67、SoloStationLayoutTests.cs:72。将围墙移到没有设备的第 2 行，保留唯一 (3,2) 通路，再通过真实布局命令关闭；新运行 1/1 Passed。原先通过的 21 项实现未改变，复用该轮结果，不把原失败组改写为 Passed。

伙伴 transit 位置保护 fixture 调整为旋转烤箱，使候选只覆盖本步目标、不同时覆盖当前格；新运行 11/11 Passed。原 terrain 数量断言随实际前厅源更新为 16。

普通入口世界 `3dcd6f25194140f69d4b07cdb43e07f7`，运行脚本 success=true、diagnostics=[]。自有 Play 正常停止后 Editor ready、compiling=false。Console session `68cf1167f0ba4ee692e314b87dcdb762`，seq 1415–1445 共 31 条连续、reset=false、dropped=false、新增 Error=0，原生 consoleErrors=0、compilationFailed=false。

原始输出保留 `Unity/Temp/pipeline_test_status.json`（后续原生运行自然更新）和 `Unity/Temp/solo-nine-changes-editor-interactive.log`；未复制、改写或建立证据包。实际服务整链可与伙伴／流程共用一次运行，但后续新行为均须有真实证据。

## 2026-10-13 主规格同步与归档

复用各领域已运行 UTF 和普通 Launcher 结果，以及九案最终逐项核对；本次仅同步规格、移动规划目录和修复引用，没有改变功能，不重复执行未变测试。两个 product-session delta 是互补新增要求，先合并本地消息合同，再合并关卡隔离和暂停恢复合同，原会话要求全部保留。
10 项已完成 change 共 69/69 任务，归档前各自严格校验通过，合并后 30 份主规格严格校验通过；日期目标已检查无冲突，目录未嵌套。新增 Sources 指向实际源码和归档执行记录。五个后续 change 仍未完成；没有提交、推送或操作第二工作区。
