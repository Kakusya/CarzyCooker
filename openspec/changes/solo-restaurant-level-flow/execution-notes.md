# Execution Notes

当前8/8完成，协议12。实际前厅排空、伙伴接续／成长和buff已接入，完整UI与普通Launcher两关也已验收。下面较早的3/8、空前厅及NotRun记录保留为历史，不代表当前状态。

## 最终关卡交接与逐项验收（2026-10-12）

| 任务／功能 | 实际用例或操作 | 当前结果 |
| --- | --- | --- |
| 1.1 配置／菜单预检 | SoloLevelConfig三项、SoloDishConfig及五目标Check／正常生成 | Passed；真实两关菜单1,2、400 Tick和递增10/20/30阈值，缺配置或非法出生在建树前拒绝；源表最终结果复用各领域记录 |
| 1.2 持续厨房／关角色 | SoloLevelOwnershipUsesPersistentKitchenAndLevelRoles；四项GridCore；SoloCompanionOwnership | Passed；唯一厨房、明确Parent、仅Participant计成员，整体退出收尾，持续伙伴和本关角色分离 |
| 2.1 唯一时钟／恢复 | SoloLevelControlHasOneBusinessClockAndResumeUsesLocalMailbox | Passed；PumpMessages不推进业务时间，服务步唯一推进，暂停仍泵真实恢复请求 |
| 2.2 暂停取消／保留 | SoloLevelPause三项；SoloCompanionResumePauseFreezesStartedWorkButReleasesApproachingClaim、PauseCancelsAcceptedButUnexecutedClaim；SoloCustomerFlowPauseFreezesArrivalMovementAndOrderDeadlineClock；UI ProjectionRealOrderRejectionPauseAndOldLevelButton | Passed；未执行取消、当前步保留后停驻、手工／自动／前厅／客流冻结、未开始认领释放且旧serial无效，恢复不重放。用户裁决已同步产品backlog／KnownIssues |
| 3.1 阶段／真实排空／零星 | SoloCustomerFlowDefaultArrivalWalksRealTerrainAndAskOpensOrder、DrainingStopsNewArrivalsAndNaturallyFinishesRealOrders；SoloCustomerDrainLastActualWashBlocksCompletionAndNextLevelKeepsCleanPool；真实UI零星Projection与下述两关 | Passed；准备能备料且0客，到时停止新客；最后Wash未完仍Draining、实际完成才结算；成功内容只计一次，真实未满足订单0分，0星可继续。规划中的SoloLevelPhases验收由这些已发现用例共同覆盖，不虚构同名已运行套件 |
| 3.2 预检／实物／旧身份 | SoloLevelCarry两项；SoloCompanionOwnershipNextLevelKeepsIdentityAndDisposesRoleWorkGrowthAndWait；SoloBuffCookingCarryKeepsOldBatchAndRejectsOldLevelChoiceWithoutInheritance；SoloBuffChoiceExactOldReplayRejectsAfterAnotherLocalPeerContinuesLevel | Passed；非法下一关零写入、手持落最后提交格，同ItemId／批次／进度／份数／厨房／布局不变、库存不叠加，新角色与成长／buff／得分重置；旧关命令、重放及高版本旧投影拒绝 |
| 4.1 已发现小集／迁移回归 | 最终Buff接入时SoloLevel10、GridSoup11、GridWire3；此前受owner迁移的GridCore4、SoloMessage7与伙伴／顾客对应小集 | 各有实际Passed；仅复用未变实现与功能的结果，详见本记录早期逐点表及相关change，不重复加总或重跑发送器全套 |
| 4.2 普通入口两关／退出 | UI记录Prepare→CookingMoveAndOperatingReject→ServeAndPlayerWash→FinishAndContinue→ZeroStarAndContinue→FreshAfterExit | 六次实际终态success=true／diagnostics=[]；真实备料、布局、选择、开店、暂停／恢复、原碗完整回收、10分1星及0分0星自然结算、换关、整个HUD重开和全新运行均已检查 |

最终普通Launcher世界`e1df831fed3f478982fc2ad6bebde671`，第一关真实玩家部分洗碗后，伙伴对同Wash第二次认领并实际Working，暂停保留约34%进度；恢复后洗净原碗，五个Ask加一Wash成长6。结算10分1星后进入下一关，固定身份与厨房保留、成长6→0、候选未选；第二关未出餐产生真实Expired订单，0分0星仍可继续，原手持碗落(5,6)。退出再开世界`6889b50f72114619a74c99f535b92fa5`，原进度不继承、仅一份初始库存。完整对象ID、实际画面、控制回执和Console连续范围见[UI最终记录](../solo-restaurant-ui/execution-notes.md)，供顾客／伙伴／buff复用同一局，不重复执行未变整链。

主要风险仍是双时钟、暂停堵住恢复、旧关写新关和换关复制实物。真实本地请求小集覆盖这些边界，默认配置下有实际顾客／餐具的两关入口覆盖集成，已足够本次内存单机交付；编译或截图不替代功能证据。接受相同菜单两关、样机节奏和无持久保存。手机／Player／AOT、LAN／公网、缩小许可及正式存档未验证，后续进入这些范围另案补验。除下述旧关身份漏点补验外，复用未变行为结果，不重复全仓测试。

### 最终旧关重放漏点与新运行

完成核对发现Level／Layout／FrontWork／Serve入口先查最新重放，另一真实本地Peer换关后旧回执仍可返回。新增`SoloLevelCarryExactOldReplayFromAnotherPeerRejectsCurrentLevel`四参数以两个真实Peer复现：初次4/4 Failed，旧关响应Error仍0（SoloLevelTests.cs:101），没有把静态怀疑记成已证实。Layout／FrontWork／Serve改为先核对当前上下文；Level只保留最新完整且已Executed的Continue重放例外，其它旧关控制WrongLevel。

修复后新PlayMode运行`SoloLevel`14/14 Passed，含四参数新用例及原10项；旧关拒绝同时断言新Level、布局版本、库存与0分保持，原重复Continue仍只安装一次。相关最新重放各单项新运行：`SoloStationLayoutRealMailboxRotatesOnceAndReplaysOnlySamePayload`、`SoloCustomerServeLatestReplayAndPayloadConflictNeverConsumeOrScoreTwice`、`SoloCompanionClaimOneOwnedPhysicalRequestAndLatestReplayDoNotDuplicateWork`均Passed。这17个实际结果只验证本次改变的身份／重放／控制，未变化的UI、配方、自动调度及普通Launcher整链复用上述已通过证据；没有重复全仓。各工具退出0、failed／skipped／inconclusive=0、Console覆盖完整。原失败与正常覆盖的原生输出保留原位，不复制或改写；域重载外层HTTP400后重新查询原recompile_status completed／failed=false，未重启测试。

### 九案完成核对

完成性：九案spec-driven规划齐全，CLI逐任务确认66/66；delta的各功能要求对应本页及各领域execution-notes的实际UTF／入口证据。正确性：材料唯一位置、冻结加工、单执行者、真实出餐／排空、旧关拒绝和独立投影均有行为断言；UI另有真实Prefab／保存callback／指针与默认两关证据，没有用空前厅或静态格式代替运行。一致性：实际owner树、当前UI等待归属、正常Luban／Proto／CodeBind生成及原消息收尾与设计对齐；早期基线标为历史，阶段结果保留不改写。

最终`openspec validate <九案名> --strict`各退出0；55份Markdown的UTF-8、尾空白和450个本地引用通过，局部手写源／测试／文档git diff --check退出0（仅既有CRLF提示）。链接检查首次未解码原AGENTS的C%23%20路径而误报，修正检查器unquote后通过，未改有效文件路径。当前Editor ready／Play stopped，最后UTF completed／Passed，原生compilationFailed=false、consoleErrors=0；保留历史buffer错误，不称历史全绿。没有本轮范围内未完成项；明确范围外的供应、装修、存档、联机餐厅、手机／Player／AOT与LAN仍未实施或验收。成果保留本地未提交／未归档，没有同步主规格或操作第二工作区。

## 2026-10-12 进入 apply 的交接

开始时：用户已授权完成九个 change，本地消息 6/6 和煮汤 14/14 已有逐项运行证据；本 change 已读取 CLI apply 上下文，当时 0/8，尚未改变关卡 owner、时钟或配置。当前实施和运行结果见下节。

迁移前源码核对：厨房为 Match Component、角色是厨房 Child，GridKitchen 的 IUpdate 独立累积 50ms 步；GridMessages、ProductAuthority、Runtime.Stop 与测试 fixture 有直接 Match.GetComponent<GridKitchenComponent> 查询，厨房内三处 GetParent<Match>，名单发布将 Match.Children 全部 Cast<Participant>。这些消费者已随下节迁移同步处理，旧 Host 适用查询仍保留在测试边界。

下一步按本 design 先做配置预检和 Solo owner 迁移，再将唯一正式餐厅驱动接入本地控制；旧 Host/Client 临时样机合同须继续成立，不扩大成联机餐厅。菜品、顾客及 buff 的真实引用在对应 change 接入后补齐，前厅自然排空依然等待顾客链的实际证据，不能用空集合勾完该验收点。复用汤的实物和批次，不复制另一厨房或重复初始库存。

## 2026-10-12 核心实施与已运行结果

已新增 LevelConfig 源与 c,s,e 注册：两条 400 Tick／10,20,30 星阈值／(1,10) 出生样机，1→2、2→2 继续内存试玩。首次注册列错位导致五目标 Check 失败，按实际 B/C/D/E/H 列修正后 Check 5/5 和正常导出 5/5、四语言导出均成功；失败后没有使用生成物。菜单、布局、客流及 buff 的真实定义引用按各自 change 接入，任务 1.1 在菜单接入前不标完成。

Solo 实际树已迁至 Match.Child RestaurantRuntime：其 Component 厨房拥有物品／工位／作业，Child Level 拥有玩家角色。厨房捕获 Match／Restaurant EntityRef，成员仅枚举 Participant；Host/Client 临时样机保留旧厨房 owner，其时钟由 HostSession 调用。GridKitchen 已撤除 IUpdate，正式 Solo 只有餐厅驱动；控制和成员清理每服务步运行，暂停／Completed 停业务钟。原 Grid 快照一版复制关卡与厨房、共用最新命令事实，旧关结果携带原关身份。

控制已走 LevelCommandRequest→Peer Ordered MailBox／LevelPeerHandler→可信 LevelSubmitRequest→HostSession Ordered MailBox／LevelSubmitHandler，回执快速返回，服务步提交结果后从原 GridSnapshotHandler 安装共同值，不另造发送器／等待表／新 SceneType。协议 5→6，新增消息文件在原格子文件之后，正常 Proto2CS 生成成功；具体控制不会自动开放 wire。

首次 6 项 SoloLevel UTF 全部 Passed；新增四项配置失败／自动暂停／旧投影断言时，测试误把生成的 IReadOnly 接口当可变集合导致六条编译错误。改为测试夹具内明确转换实际后备集合，并在 finally 恢复；没有改生成表。最终编译 completed／failed=false，随后 PlayMode `ET.ProductSessionTests.SoloLevel` 实际 10/10 Passed，AutoTesting Console 覆盖完整、返回 0。

| 功能 | 实际用例 | 当前证据 |
| --- | --- | --- |
| 配置预检与无半树 | `SoloLevelConfigRejectsBeforeCreatingWorld`、`SoloLevelConfigActualBootstrapLeavesNoHalfRestaurant(False/True)` | 3 Passed；缺下一关／非法出生位经真实初始化在建树前拒绝 |
| owner／名单／退出 | `SoloLevelOwnershipUsesPersistentKitchenAndLevelRoles` | Passed；唯一厨房、Level 角色、成员数 1 和整体释放 |
| 一份时钟／本地恢复 | `SoloLevelControlHasOneBusinessClockAndResumeUsesLocalMailbox` | Passed；纯泵消息不提交，单步只进一次，暂停拒绝业务且恢复可处理 |
| 待执行与当前移动步 | `SoloLevelPauseCancelsFuturePathAndPendingIntentButKeepsCurrentStep` | Passed；后续路径和未取物取消，当前步冻结，恢复仅完成该步 |
| 已开始手工／自动作业 | `SoloLevelPauseFreezesStartedManualWorkAndRetainsClaim`、`SoloLevelPauseFreezesActualAutomaticBatch` | 2 Passed；暂停进度／输入／合法认领保留，恢复续进 |
| 跨关实物／旧命令 | `SoloLevelCarryPreservesCookingPotAndRejectsOldLevelAndDuplicateContinue` | Passed；实际投料煮锅在手跨关落最后提交格，批次与原进度保留，不复制库存，继续重放不再换关 |
| 下一关缺失零领域写入／旧投影 | `SoloLevelCarryMissingNextPreservesEverythingAndOldProjectionCannotApply` | Passed；原关／角色／手持／库存保持，旧关高版本投影仍拒绝 |
| 旧 Host 格子驱动 | 四项 `GridCore*` | 4/4 Passed；原移动、重定位、停驻与落物合同继续成立 |
| owner 迁移后的汤／本地责任 | `GridSoup*`、`SoloMessage*` | 11/11＋7/7 Passed；无重复扩到 KCP 或发送器全套 |

原始 UTF 结果留在 `Unity/Temp/pipeline_test_status.json`，正常新运行覆盖该原生文件，不复制证据包。后续真实前厅排空、前厅暂停／成长及 buff 重置须等对应能力接入补证；这部分当前 NotRun，任务不提前勾选。

普通 Launcher World `c1a07f962beb468aac2cfa27b540fac4` 经实际按钮／地图输入完成原材料切配打蛋投料，始终 Preparing、营业 elapsed=0；随后实际按钮“开始营业／暂停／恢复／下一关”通过，暂停等待 600ms 不进业务钟，空前厅样机正常零分零星完成，新 Level generation=2、elapsed=0，同世界及全部 ItemId 保留。该空前厅过程只证明流程骨架，不能代替真实顾客收尾。截图 `Unity/Assets/Temp/pipeline-screenshots/solo-level-generation2.png` 已查看。

整体停止再开新 World `5703c53374e342ff97263e7e59b2ed05`，generation=1、command=0、真实初始番茄／鸡蛋恢复，旧关状态不留。临时输入源 `Unity/Temp/GridSoupSoloAcceptance.cs`／`SoloLevelAcceptance.cs` 用 Pipeline 内存执行，只走实际输入、读取投影，没有直接写关卡或物品完成流程。

最终入口范围 Console session `68cf1167f0ba4ee692e314b87dcdb762`，seq 586–633 共 48 条连续，无 reset／dropped，原生 Error=0、compilationFailed=false；保留 975ms 本地请求 Warning 和六条历史夹具编译错误，未清 Console、未声称历史全绿。Editor ready、Play stopped。正常生成新增 Level opcode 10074–10077，原汤 10073 保持。OpenSpec 严格校验有效，不能代替待接前厅的运行验收。

## 规划时记录

规划检查：2026-10-12，`openspec validate solo-restaurant-level-flow --strict` 返回有效，退出码 0；proposal／specs／design／tasks 齐全。关联九案共 46 份 Markdown 的本地引用、UTF-8 与行尾空白检查通过。本记录仅证明规划有效，全部新增功能运行仍 NotRun。

本轮仅建立规划，业务、生成、编译、Unity 和 UTF 均 NotRun，tasks 全部未勾选。结构／正文／引用和 OpenSpec 检查在规划收尾记录，不能代表功能通过。

风险主要是移动 owner 后旧查询失效、双时钟、暂停堵住恢复及换关复制库存。使用真实 Solo 请求与受控时间的小集逐项覆盖，再从 Launcher 走两关即可；未改变的本地 sender 停止证据可复用，不扩展联网或包体。接受无持久保存、相同菜单两关和未接前厅的阶段性证据；接入顾客后必须补真实排空，菜单许可收窄／存档接入时另案补验。

用户已决定暂停取消未执行动作、已开始工作停钟保留进度，前厅自然收尾；现有产品文档仍列待裁决，实施对应任务同步这些已裁决范围。本 change 不代定跨关许可缩小。

| 任务／功能 | 拟 UTF 或真实操作 | 结果 |
| --- | --- | --- |
| 1.1 配置预检 | SoloLevelConfig：缺下一关／非法出生位，不创建半关 | NotRun |
| 1.2 owner 与成员 | SoloLevelOwnership＋实际受影响 Grid 用例：树、成员和释放 | NotRun |
| 2.1 单时钟与控制 | SoloLevelControl：PumpMessages 回执，AdvanceTicks 提交，暂停可恢复 | NotRun |
| 2.2 暂停矩阵 | SoloLevelPause：未执行取消、当前步冻结、活动工作保留、接近取消 | NotRun |
| 3.1 准备与零星 | SoloLevelPhases＋原 HUD 开店、到点、零星继续 | NotRun |
| 3.1 真实前厅排空 | 顾客 change 完整用餐／清桌／洗碗链后才能完成 | NotRun |
| 3.2 实物换关 | SoloLevelCarry：身份、库存、进度、落物、旧关及预检失败 | NotRun |
| 4.2 真实入口 | Launcher Solo 两关、暂停恢复、退出重开 | NotRun |

拟名称尚非已发现测试。实施时先发现真实过滤范围，保留原始运行输出，失败／空集／跳过不算通过。同一完整流程可服务多个 change 的验收，但须列明点位且实现未再改变。
