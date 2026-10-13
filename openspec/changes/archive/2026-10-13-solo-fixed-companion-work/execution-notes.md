# Execution Notes

## 最终直接接续观察（2026-10-12，协议12）

当前7/7。后续完整UI普通Launcher世界`e1df831fed3f478982fc2ad6bebde671`在同一次ServeAndPlayerWash调用中直接观察原Wash：玩家停手保留0.278689，伙伴ClaimSerial=2、Working推进到0.343205，暂停350ms保持同Tick／比例，HUD显示洗碗34%。恢复后同碗洗净、自然结算，实际下一关成长6→0。该调用和后续FinishAndContinue均success=true／diagnostics=[]，不再只依赖先前错过中间态的终态推断；旧超时历史仍保留。完整输入、对象身份和画面见[UI最终记录](../2026-10-13-solo-restaurant-ui/execution-notes.md)。

最终关卡身份修复新增四参数旧关重放UTF，FrontWork旧原请求在另一真实Peer换关后明确WrongLevel；本组`SoloCompanionClaimOneOwnedPhysicalRequestAndLatestReplayDoNotDuplicateWork`单项新运行Passed，当前关最新事实仍成立。具体失败／修复／17项结果见[流程最终记录](../2026-10-13-solo-restaurant-level-flow/execution-notes.md)，未改工作引擎及成长的原结果复用。

## 完整服务链与伙伴收口（2026-10-12，协议 11）

本 change 当前 7/7。32 项 SoloCompanion 行为结果按此前逐点记录复用，工作引擎／成长／调度代码未改；新增实际顾客服务和关尾的结果见 [服务执行记录](../2026-10-13-solo-customer-order-service/execution-notes.md)。SoloCustomerUtensils 验证真实服务后自动伙伴洗原碗，SoloCustomerDrain 验证最后洗碗未完仍 Draining、暂停保留、真实完成后才结算和下一关归零；不是空前厅或人为完成记录。

普通 Launcher 同一局 `301cae1fcfba46e9a1042a24607fd723` 实际移动后的桌位入座、伙伴询问开单；玩家清桌、部分 Wash=0.22950819672131142、暂停冻结、停手保留 0.2786885245901639。原工作最终 ClaimSerial=2／Completed，伙伴完成次数为 5 个真实 Ask＋1 个真实 Wash，没有后续玩家认领，确认伙伴接续并完成原任务。中间 Working 观察调用因启动晚于完成而超时，未计通过；此功能采用同一局最终事实补验，具体事实与限制见服务记录。自然排空后实际下一关保留固定身份、成长 6→0，原碗仍可实际取回。运行 Console 无新增 Error；完整 UI 与手机／LAN 不属本 change 验收。

主风险仍是认领重叠、工作进度被重建及成长误计；既有真实请求 UTF＋这条有实际顾客／餐具的入口链覆盖本轮功能。复用未变工作测试，避免因阶段切换重复全套；新 buff 或 UI 改到冻结耗时／投影／owner 时再验其对应行为。

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


## 当前工作核心验收（2026-10-12，协议 9）

当前 4/7：任务 1.1、1.2、2.2、2.3 已验收。自动询问优先／洗碗 FIFO、客户端 Role／FrontWork 值投影与 Board、真实客流与自然整链仍待任务 2.1／3.1／3.2，不因后台用例通过提前勾选。

主要风险是双执行者、无到位进度、停手丢比例、迟到实例命中新角色和重复成长。新增用例走真实 Peer／Role 到 authority 的本地消息、原有序 MailBox、同一地形与业务 Tick，再检查独立订单／原餐具和实际成长；用现有 CoroutineLock 暂挂 authority MailBox 检查角色退出后的物理等待责任，不建测试平台。未改菜谱、食材与原菜单，既有对应结果复用；重验受控制／移动／消息和目标推进影响的小集。接受自动策略和完整 UI 尚未接入，新消息仅本地；接入实际来客／调度／显示后补各自 UTF 和同一条 Launcher 整链。

### 源、消息与运行范围

追加源 `Design/Proto/ET-Client/ZZZFrontWork.proto`，正常 Bin/Tool Proto2CS 退出 0；生成 Client／ClientServer 消息与 ID，FrontWork Request／Response／Submit／SubmitResponse 为 10083–10086，Companion Request／Response／Result 为 10087–10089。既有 StationLayout 10078–10081、Bake 10082 保留，当前协议 9；无 wire 白名单扩展或监听。未改 Excel，不重复导表，复用前厅目标阶段的源 Check／生成结果。

实际树与发送／接收、等待和消息对象收尾见 [design.md](design.md) 与 [消息约定](../../../../references/reactive-and-network.md)。FrontWorkCommands 归 Level，只持复制的值 envelope 和 EntityRef，不拥有 wire 消息；Role 用自己的 AsyncOwnerComponent 和至多一项在途标记。停止先提交，再推进到位工作和成功目标效果，再处理 Claim；第三项完成先计成长，后续同 Tick 领取按冻结的新耗时。

### 功能与证据

PlayMode 先发现再执行相关 testName 子串；28 个 SoloCompanion 用例当前各有 Passed 证据，实际补充／改变断言后只重跑对应用例。初始整组 23/23；随后 Growth 3/3、迟到物理 Claim 1/1、增强 ResumePlayerStops 1/1、两项新 Resume 各 1/1。没有 Failed、Skipped 或空集，工具每次退出 0、Console 覆盖完整；不能把这些分组计数重复相加成更多独立用例。

| 任务／功能 | UTF（ET.ProductSessionTests 下）或实际操作 | 结果 |
| --- | --- | --- |
| 1.1 模型与换关身份 | SoloCompanionOwnership 11 项 | Passed；复用协议 9 首组结果 |
| 1.2 双入口、固定步与排他 | SoloCompanionClaimRealPeerAndRoleCompeteThroughAuthority 两顺序 | Passed；回执时 Available／零进度，到位前零进度；仅胜者推进一次开单 |
| 1.2 可信身份与有限重放 | TrustedRoleRejectsStaleIdentity 四参数、ClientCannotEnterTrustedNpcOrSubmitHandler、OneOwnedPhysicalRequestAndLatestReplayDoNotDuplicateWork | Passed；错误实例／关／世界／协议拒绝，Peer 内部入口 InternalOnly，同一在途限制及同请求重放／冲突 |
| 1.2 await 后实例及物理等待 | DisposedOwnedRoleRejectsItsLatePhysicalRequestAfterSameIdReuse | Passed；真实 authority MailBox lock，Role scope 退出立即取消、原 sender requestCallback 仍在；同 ID 新 Role 不接旧请求，解除原锁后原等待表清空，新角色正常认领 |
| 2.2 停手与换人 | SoloCompanionResumePlayerStopsAndNpcContinuesSameActualWash | Passed；玩家真实 Clear 产生一项 Wash，玩家实际部分洗碗后 Stop 保留比例，伙伴接同一工作，旧 ClaimSerial Stop 无效，原碗洗净进干净池 |
| 2.2 暂停与未开始认领 | PauseFreezesStartedWorkButReleasesApproachingClaim、PauseCancelsAcceptedButUnexecutedClaim | Passed；接近／待执行取消且保留已开始单步，已工作比例／认领停钟保留，恢复后继续 |
| 2.2 移动／成员／角色退出 | MoveAndMemberExitReleaseOnlyOriginalPlayerClaim、DisposedWorkingRoleKeepsProgressForItsReplacement | Passed；实际 Move／Leave、同 ID 新角色均保留比例，旧实例请求无效，不 Stop 公共 sender |
| 2.3 来源、次数与重复 | SoloCompanionGrowthCancellationAndDuplicateNotificationsNeverAddCompletion、ClearIsPlayerOnlyAndDoesNotCount；Claim／Resume 中玩家完成断言 | Passed；实际完成才加，Player／Clear／取消／重放／重复完成通知不加；新通知只观察 |
| 2.3 第三次／ceil／同 Tick／新关 | ThirdActualAskAcceleratesSameTickNextWashWithCeil | Passed；三次真实 Ask 后，同 Tick 洗碗冻结 61→31，30 Tick 未完成、31 完成；只计一次。真实 Continue 后身份保留、成长归零，旧请求 RoleChanged |
| 控制／消息／移动／目标接口回归 | SoloCustomerTargets 15、SoloLevel 10、SoloMessage 7、GridCore 4 | Passed，36/36；覆盖新引擎对目标效果、关卡、原物理观察者和原玩家移动的影响 |
| 普通产品启动 | Launcher.unity Play，Temp/SoloFrontWorkBootstrapAcceptance.cs 只读检查 | Passed；SoloReady、Protocol=9、队列 owner=Level、Pending=0、ScopeActive=true、OrderedMessage、RoleInFlight=false、Growth=0、Listener=false |
| 2.1 自动策略／工作值与 Board | SoloCompanionSchedule／可见自动服务 | NotRun，未接入；消息引擎运行不代表自动策略已存在 |
| 3.1 完整伙伴小集／3.2 顾客整链 | 真实自动来客、询问、清桌洗碗与自然关尾 | NotRun；上述普通入口只检查启动，不替代整链 |

本阶段新增 Claim／Resume／Growth 用例只设置合法已入座／已离席及已入脏池的初始历史，没有直接写工作完成进度或期望成长；这些用例的进度和效果来自真实消息、路径与 Tick。Ownership／Targets 的模型初始条件按各自阶段记录区分，不替代真实工作验收。换关隔离用例将顾客声明为已离店的初始条件，只证明换关身份／成长隔离；实际订单超时和自然离店另由顾客 change 验收。

中间编译错误 CS0508（通知 handler 返回类型需 UniTask）、CS1061（测试应读取现有 requestCallback 字段）已修正；未清 Console 或虚报编译通过。最后 recompile errors=0／warnings=0／compilationFailed=false。格式清理不改变行为，未重复已覆盖测试。

普通启动世界 `f7ddecfafe054d729da78b1fb5962026`，脚本 success=true、diagnostics=[]。自有 Play 正常停止；Console session `68cf1167f0ba4ee692e314b87dcdb762`、seq 1678–1708 共 31 条连续，reset=false、dropped=false、新增 Error=0、原生 consoleErrors=0、compilationFailed=false。

原始输出仍保留 `Unity/Temp/pipeline_test_status.json`（原生运行自然更新）及 `Unity/Temp/solo-nine-changes-editor-interactive.log`，无状态副本／证据包。完整产品运行尚未验收，后续 UI／自动策略变化不复用本次启动作为整链证据。

## 任务 1.1 模型阶段历史记录（协议 8）


2026-10-12：用户已授权完成九个 change，本 change 继续实施。当前已验收任务 1.1；认领、目标效果、调度、接手和实际成长仍 NotRun，不能由模型存在替代真实服务验收。

主要风险仍是双执行者、远处完成、停手丢进度及重复成长。此阶段只建立持续身份／本关模型与其释放边界，新增真实 owner UTF、关卡／消息小集和普通 Launcher 启动检查足够覆盖本次改变；不增加联网、构建或 AI 工具。工位已有菜品阶段的完整结果复用，本次只重验受新角色位置保护影响的布局小集。接受工作引擎与顾客目标尚未接入；实际认领、移动或目标效果改变时补对应 UTF 和完整顾客链。

## 源、模型与交接

新增源 `Design/Excel/ET/Datas/Product/CompanionConfig.xlsx`，注册 `__tables__.xlsx` 第 27 行，c,s,e；一位固定岗位，出生 (2,5)，Ask／Clear／Wash 为 30／20／61 Tick。已有 Bin/Tool 的 Check,ShowCmd 5/5，通过后正常导出 5/5 和四语言，均退出码 0；生成 ET Client／ClientServer C#、bytes、Config/Luban 及 Editor C#/JSON，未手改派生物。Unity recompile 最终 errors=0、warnings=0。

身份归 Restaurant，Role／FrontWork 归 Level，成长组件归 Level。Role 的 Ordered MailBox 与 AsyncOwnerComponent 绑定真实角色；换关释放旧角色及其等待，不 Stop 公共 sender。FrontWorkDefinition 的目标身份字段、创建入口和职责见 [设计交接](design.md)。顾客模块据此创建真实工作并提供校验／完成效果；当前准备态不制造询问或洗碗任务。

## 任务与运行结果

PlayMode 先发现真实用例再按 testName 子串执行，最新 11 项 SoloCompanionOwnership Passed，failed／skipped／inconclusive 均 0，工具退出码 0，Console 覆盖完整。

| 任务／功能 | UTF 或实际操作 | 结果 |
| --- | --- | --- |
| 1.1 配置、树、岗位和出生 | SoloCompanionOwnershipUsesActualGeneratedConfigAndOrderedRoleMailbox、RejectsInvalidDefinition 四参数、RejectsMissingIdentityBeforeHostCreation | Passed；实际源生成加载、无半个 Host、Scope 非空、Ordered MailBox、无监听／伪造准备工作 |
| 1.1 工作目标值归属、持续身份、本关角色／成长重置 | SoloCompanionOwnershipNextLevelKeepsIdentityAndDisposesRoleWorkGrowthAndWait | Passed；真实本地 Continue，旧工作／角色／成长释放，角色等待取消，身份／岗位与公共 sender 保留；历史成长仅为生命周期初始条件 |
| 1.1 失败预检和整体释放 | BlockedNextSpawnPreservesCurrentTree、WholeExitDisposesIdentityAndOwnedRole | Passed；伙伴出生格阻塞拒绝换关并保留树；整个退出释放固定身份和 owner |
| 新角色对布局的影响 | LayoutCannotCoverCurrentOrTransitCell 两参数；SoloStationLayout 小集 8 项 | Passed；真实本地调整拒绝覆盖当前／本步目标格，工位与布局版本无修改；既有布局行为 8/8 |
| 角色安装对关卡和消息的影响 | SoloLevel 10 项、SoloMessage 7 项 | Passed，10/10 和 7/7；有序消息、物理等待责任、关卡单时钟、暂停及跨关原厨房保留 |
| 普通产品入口安装真实角色 | Launcher.unity Play，Temp/SoloCompanionFoundationAcceptance.cs 只读运行树 | Passed；SoloReady、ScopeActive=true、OrderedMessage、Growth=0、位置 (2,5)、Listener=false；未验证 Board 角色显示或服务行为 |
| 1.2 消息认领 | SoloCompanionClaim | NotRun，未实施 |
| 2.1 调度与到位／2.2 接手暂停／2.3 实际成长 | SoloCompanionSchedule／Resume／Growth | NotRun，未实施 |
| 3.1 完整伙伴小集／3.2 真实顾客服务链 | Launcher 询问、玩家接续洗碗、自然收尾、换关成长重置 | NotRun；当前入口只证明安装，不证明这些行为 |

第一次新 UTF 为 8 Passed／1 Failed：换关用例 line 98 scope 为空，原因是原 ProductSession fixture 没有把 Loader 程序集加入 CodeTypes，实际 AsyncOwner Awake 未执行。补入 Loader 后第二次为 5 Passed／4 Failed，原始消息为 ArgumentNullException(runtime)，栈在 AsyncScope.cs:91／AsyncOwnerComponent.cs:23；fixture 同样缺少真实异步 runtime 前置。随后 fixture 在 Setup Begin 所属 AsyncRuntime、Cleanup Stop 原实例；修复后新运行 9/9，再加两项布局保护用例后最终 11/11。失败结果未续跑或改写，不用 LogAssert 隐藏异常。

普通入口世界 `64d2b29ca09d4f66acdd8a23d801277e`，Level 1284022556688405／Companion 1284022556688406／Role 1284022556688407；实际内存脚本 success=true、diagnostics=[]。自有 Play 正常停止后 Editor ready。Console session `68cf1167f0ba4ee692e314b87dcdb762`、seq 1189–1219 共 31 条，reset=false、dropped=false，无新增 Error，原生 consoleErrors=0、compilationFailed=false。

原始结果保留 `Unity/Temp/pipeline_test_status.json`（后续原生运行自然更新）与 `Unity/Temp/solo-nine-changes-editor-interactive.log`；不复制状态或证据包。当前协议仍为 8，本阶段未添加 wire DTO。后续按设计接真实请求与每 Peer 值投影；NPC 自动请求可能下一服务步才认领，同 Tick 后续合法领取另测，不承诺每次自动领取都在同 Tick。

## 2026-10-13 主规格同步与归档

复用各领域已运行 UTF 和普通 Launcher 结果，以及九案最终逐项核对；本次仅同步规格、移动规划目录和修复引用，没有改变功能，不重复执行未变测试。两个 product-session delta 是互补新增要求，先合并本地消息合同，再合并关卡隔离和暂停恢复合同，原会话要求全部保留。
10 项已完成 change 共 69/69 任务，归档前各自严格校验通过，合并后 30 份主规格严格校验通过；日期目标已检查无冲突，目录未嵌套。新增 Sources 指向实际源码和归档执行记录。五个后续 change 仍未完成；没有提交、推送或操作第二工作区。
