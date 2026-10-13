# 执行说明

## 决策与当前状态

- 2026-10-13：用户要求查看项目并构建 change；对“产品业务入口、结束及功能间衔接统一消息，内部计算／每 Tick 进度仍直接调用”的推荐回答“是，按你想的来”。已授权规划，尚未实施。
- 已加载 openspec-explore、carzycooker-requirements 和 openspec-propose；按实际源码／主规格构建提案。无剩余阻塞性产品决定。有限消息调度延后、框架建树／强制退出例外及存档交接见 design。
- 初次提案的历史静态基线：源码协议 16；当时供应、清理和装修已有实现，solo-success-checkpoint 仍在实施。以下后续核对替代其“当前状态”，原消息迁移任务和运行结果未改变。
- 2026-10-13 后续路线规划核对：源码协议 17；solo-success-checkpoint 与 cooperative-restaurant-core 原案已有验收，Host/Solo 共用 Restaurant。同步 proposal/design 的基线、owner 树和 tasks 4.4 的前置说明；不改原任务勾选。当前前厅等后续能力仍查原 change，消息迁移本案继续 0/17、NotRun。
- 产品功能、UTF、普通 Launcher、构建与生成：**NotRun**。文末记录本轮规划检查，不能用它代表功能通过。

## 严谨程度与取舍

主要风险是跨功能从同步衔接变为消息后漏掉／重复效果、提前自然结算、跨关旧实例写回，以及公共丢弃路径重复回收。保留原 owner、业务步和原子效果，用每类功能的真实消息行为断言加几个代表性退出／丢弃测试即可定位；不建立全历史存储、可靠消息平台或穷举调度实验。

已有配置纯校验和未变规则的证据可以引用原 change，但改过的请求／终态／自动衔接不能用旧通过代替。自动断言尽量扩展已有 ProductSessionTests／Solo* 测试；一次普通 Launcher 连续流程覆盖多个功能。覆盖齐全后不因整理文档或归档重跑。接受功能衔接经过后续消息轮次，不保证原直调链同 Tick；明显交互停顿、前厅卡住、一次效果出现两次才触发针对性补验。

本案明确未验证 Player/AOT、手机、LAN／公网、双 Editor、新合作餐厅玩法、任意断电文件恢复及跨进程可靠投递。这些不因消息化自动升级为支持项。存档使用原 change 的文件保护合同，若改动实际触及文件写入路径则复跑其失败／冲突保护用例。

## 功能与验收对应

下列是计划，全部 **NotRun**。现有名称指源码中的用例族，新增消息断言和拟议测试尚未实现；执行时必须通过 Tools/AutoTesting 发现真实 PlayMode FullName。每行中的不同动作均须得到运行覆盖，可以由一个参数化用例或同一流程完成。

| 任务／功能 | 最小相关 UTF／实际操作与关键断言 | 结果 |
| --- | --- | --- |
| 1.1–1.3 协议、终态、丢弃收尾 | 真实 handler 路由；两条命令分别终结；旧 Opcode；未交付／缺邮箱／锁等待退出／Stop 的一次 Dispose | NotRun |
| 2.1 移动及物品 | 原 Grid／备料／汤用例覆盖移动、拿放、掉落拾取、装容器、倒入、分汤；命令结果与守恒 | NotRun |
| 2.2 手工加工 | 原备料用例＋入口／Started／停手接续／完成消息；到点未投递不产物 | NotRun |
| 2.3 自动加工 | SoloDishBakeLocalReplayLocksInputAndProducesOneNewIdentity 等及汤用例；暂停与跨关后合法完成一次 | NotRun |
| 2.4 清理与供应 | SoloCleanup／SoloSupply：接水、排液、固体倾倒；采购、运输到货、接收、拆份；余额与内容守恒 | NotRun |
| 3.1 顾客订单 | SoloCustomerFlow*：来客、队满跳过、入座、询问后开单、到期／用餐后真实离场 | NotRun |
| 3.2 玩家／伙伴 | SoloCompanionWork／SoloCompanion：认领竞用、到位、半程接续、第三次成长、迟到旧消息 | NotRun |
| 3.3 出餐和原碗循环 | SoloServe／SoloCustomer／SoloFrontFlow：一次消费计分、清桌→洗碗间隙不结算、同一原碗洗净 | NotRun |
| 4.1 关卡控制与收尾 | SoloLevel：开店、暂停恢复、自然零星完成、Continue、旧关拒绝及新关就绪 | NotRun |
| 4.2 布局与 Buff | SoloStationLayout／SoloBuff：成功／拒绝、准备与候选、重复选择、旧上下文 | NotRun |
| 4.3 装修 | SoloRenovation：候选空／合法、选择一次、下一关安装一次、失败零安装 | NotRun |
| 4.4 已有存档 | 对照原案已交付入口，SoloSaveSelection／Commit／Restore：选档、取消、写盘失败、重试、恢复与迟到完成 | NotRun，原案证据不代替消息迁移后运行 |
| 5.1 会话与公共生命周期 | ProductSession 本地／现有 RealKcpSharesAuthorityKeepsAliveAndLeaveReturnsCommitted、WireInternalMessageCannotJoinOrTakeLocalIdentity 及必要 SoloMessage／LocalMessageShutdown | NotRun |
| 5.2–5.3 UI 与连续玩法 | SoloRestaurantUi＋普通 Launcher；新轮→两菜／前厅／供应清理→暂停恢复→自然结算→装修／保存→下一关，关闭重开和旧按钮 | NotRun |

运行顺序按 tasks 的依赖，先本地功能后必要 KCP 和真实界面。已有相关小集当次完整对应某项时直接复用，不再以集成为名重复。测试发现、过滤、原始输出和失败信息遵循 [自动测试 SOP](../../../../Book/自动化测试SOP.md)，不复制证据包或修改原结果。

## 规划检查

2026-10-13 后续路线核对：同步协议/owner/已交付前置后，`openspec validate product-local-message-boundaries --strict --no-interactive` 退出码 0。与新案一起检查的 80 份 Markdown 未发现失效引用、编码、空白或原场景遗漏；未运行本案功能，不改变 0/17 与 NotRun。

2026-10-13 本轮规划检查：

- `openspec validate product-local-message-boundaries --strict`：通过。
- 6 份 Markdown、24 个本地引用：目标均存在；UTF-8 与尾随空白检查通过。
- 17 项实施任务均未勾选；十二类功能与验收对应已列出。ET 树已对照创建语句，持续 Companion 归 RestaurantRuntime，角色归 Level。
- CLI 的 planning complete 仅表示规划产物齐全；未执行生成、编译、UTF 或普通 Launcher，不代表功能完成。

## 2026-10-13 实施：消息输入收尾（任务 1.2）

用户授权三个基础设施 change 实施；资料与夹具两案已完成各 3/3 后，先补本案迁移消息必需的输入收尾。只修改原 ProcessInnerSender、MessageQueue 与 Ordered invoker；没有新等待表、发送器或可靠消息平台。原规划 NotRun 保留其阶段含义，尚未迁移的厨房／前厅／关卡／UI 功能仍未验收。

Call 实际调用后接管输入，验证异常或目标队列不存在由发送边界回收；Send/Reply 队列不存在时回收，同步异常仍由发起者负责。入队与撤队列交接互斥，撤下只释放原队列未取出的输入；缺邮箱由分发层回复目标失效并释放。Ordered 等锁后原实例消失，用捕获的 sender EntityRef 收尾，不从退役邮箱找新 Root；已交 handler 的输入保持原责任。生命周期约定已同步。

| 功能／任务 | 实际 PlayMode UTF（ET.LocalMessageShutdownTests） | 结果 |
| --- | --- | --- |
| 1.2 调用拒绝与未交接释放 | LocalMessageOwnershipCallValidationFailureReleasesBeforeQueueHandoff：错误 ActorId／已停 sender 不入队，各释放一次。 | Passed |
| 1.2 缺目标队列 | LocalMessageOwnershipMissingQueueReleasesRequestNoticeAndLateResponseOnce：请求失败，通知与迟到响应各释放一次，不调用 handler。 | Passed |
| 1.2 入队后邮箱消失 | LocalMessageOwnershipMissingMailboxReleasesUndeliveredInputOnce：真实队列请求返回 ERR_NotFoundActor，输入与通知各释放一次。 | Passed |
| 1.2 撤队列与即时取消 | LocalMessageOwnershipStopDrainsQueuedInputAndCancelsWithoutFuturePump：未取出请求／通知释放一次，Stop 无后续泵立即取消，重复停止不双释放。 | Passed |
| 1.2 等锁时目标退役／替换 | LocalMessageOwnershipOrderedWaitRejectsRetiredMailboxWithoutTouchingReplacement：第二个真实请求等锁时销毁邮箱并同 ID 建新实例，拒绝旧输入，替换 handler 未被调用。 | Passed |
| 1.2 已交付输入与晚到答复 | LocalMessageOwnershipStopDoesNotReleaseHandlerInputBeforeItsLateResponse：Stop 不抢 handler 输入，完成后输入与晚到物理响应各释放一次。 | Passed |
| 1.2 原停止／取消／实例合同 | 同族原 13 项，包括 StopDirectDisposeAndNoTimerSettleBothModes、OwnedLocal_CancelOnlyBusinessWaitAndLetOriginalReplyRetire、OldReplyAndRealOldTimerCannotCompleteReusedSender 及整体退出／批次重入。 | Passed |

先跑新增五项 5/5 Passed；补原 Call 验证失败路径及第六项后，重新发现 PlayMode 366 项，仅运行 filter=ET.LocalMessageShutdownTests，19/19 Passed、退出 0，Console 覆盖完整，Player 已停止。没有因文档阶段重跑此前夹具 85 项；本轮新增公共输入行为由本次 19 项覆盖。

首次编译 ET0002：Ordered invoker 读取 sender.IsStopped 缺 FriendOf。补按原模式的 FriendOf 后新 recompile completed／failed=false／compilationFailed=false；原编译错误保留 Console 原记录，未清日志。原始测试输出仍为 Unity/Temp/pipeline_test_status.json。任务 1.2 完成；其余 16 项未完成，协议生成与全部产品消息迁移尚未运行。

### 任务 1.1 开始：源协议

追加 ZZZZZZZZZProductTransitions.proto，包含 KitchenWorkTransition／Outcome、CustomerFlowTransition／Finished、FrontWorkTransition／Finished／Outcome、ShipmentTransition／Outcome、LevelTransition／PhaseChanged、BuffPrepare／Prepared、RenovationPrepare／Prepared 和 ProductCommandOutcome，共 16 个明确消息类型。内部转换携世界、适用关、源实例和阶段序号；结果另含收件人、版本与业务 Tick。没有改既有 proto 消息序号。

在 Bin 用既有 dotnet --roll-forward Major Tool.dll --AppType=Proto2CS --Console=1 正常生成 Client／ClientServer，输出 proto2cs success、退出 0。对照 HEAD 的旧 Opcode 名称与数值，两模式旧编号均保持；新类型仅追加。尚未接入这些类型的业务 handler、投影与自动功能；协议版本暂保留实际 17，连同合同接入再递增。任务 1.1 继续未完成，不能把源生成当作功能验收。原日志路径已从 Editor 读取为 C:/Users/Administrator/AppData/Local/Unity/Editor/Editor.log。

### 后续实施：转换接收与动作终态

协议已按实际基线升为 18；在原厨房、FrontLevelState、FrontWorkCommands、KitchenSupply、Restaurant、Buff 和装修 owner 添加纯值待处理输入。九类内部 handler 只接受 ProductHost/HostSession 的本轮身份，拒绝 Peer 目标、错误世界或退役源实例；输入都在 finally 收尾，不保留消息 DTO。12 项真实队列接收／拒绝 UTF 已 Passed，不推进业务时间；具体业务转换仍由后续任务实施。

动作结果接入原 HostPeer 记账和原 GridBroadcast 提交点：非 Grid 保留每条已接收命令的原 Context，再发 ProductCommandOutcome；Grid 保留原 GridCommandResult 并补独立观察出口，较早取消不覆盖新命令。先发送同版快照，再发送独立终态；收件端复制值，按结果流序号拒绝重复，版本未安装或旧世界拒绝。只增加结果通知的 Client 接收类型，Host 请求 wire 白名单没有增加内部转换。

本轮编译 ET0004：Hotfix 不允许属性形式的 LevelScoped；改成方法后编译通过。新增测试编译 CS1674（IResponse 不能 using），改为原具体响应类型后通过；编译失败期间 AutoTesting 拒绝执行，发现仅旧程序集 378 项，NotRun，不冒称新用例已跑。

首次扩展 ProductMessageBoundary 运行 24 项：23 Passed／1 Failed。唯一失败是测试用默认 Create() 冒充池对象，ReferenceEquals 不成立；实际生成器默认 isFromPool=false。改为显式 Create(true)，保留实际复用身份断言，准备新单例运行。其他两条终态／快照顺序／重放防重、旧 Grid 取消与九类真实 KCP 内部拒绝用例 Passed；未修改领域预期或原失败结果。

任务 1.3 实际映射：ProductMessageBoundaryEachAcceptedCommandFamilyHasExplicitTerminal 七个参数分别覆盖 Level／Layout／Buff／Supply／Renovation／Serve／FrontWork，7/7 Passed；ProductMessageBoundaryTwoCommandsEachNotifyAfterSnapshotAndReplayDoesNotNotifyTwice 覆盖两条供应命令、先快照后两个终态及重放无重复；ProductMessageBoundaryOldGridCancellationStillHasOutcomeWithoutReplacingNewCommand 覆盖原 Grid 旧取消独立观察和新命令不被覆盖；池对象单例在显式 Create(true) 后 1/1 Passed，实际复用 ReferenceEquals=true、消息回收后原值有效、重复／未来版本／旧世界拒绝。与原 24 项中未改的 23 项结果合用，31 个消息边界用例的最新有效结果全部 Passed，所有运行 Console 覆盖完整、退出 0；失败那次仍保留 Failed。响应式约定已同步，任务 1.3 完成，不将此结果当加工／前厅迁移通过。

加工迁移开始：Manual／Soup／Bake 原 owner 增加阶段序号与 completionQueued／completionReceived。到点只发 KitchenWorkTransition，原 HostSession handler 复制值入厨房队列，后续允许业务步复验源实例／序号和进度后完成；厨房进度在到点等待时夹止，旧序号和重复转换不复制输出。Started／Paused／Resumed／Finished 通过原统一提交后发送 KitchenWorkOutcome；暂停或成员退出仍沿原 owner 收尾，不另建时钟。

首次加工扩展 37 项运行 36 Passed／1 Failed：切配例在 PrepDo 返回后尚未泵出 Started 通知便断言收到，打蛋与完成／重放／汤／烘烤／跨关暂停均 Passed。补显式只泵消息再检查 Started，未推进业务或改完成断言；准备新运行。编译还补了 Manual Awake 的 FriendOf，修正测试参数 expectedProcess 为真实 process。原错误／失败保留。自动作业 Awake 序号从 1 开始，恢复中的 Cooking／Baking 即使尚未经过新 Start 也有有效阶段身份，不把技术 pending 布尔值当作持久事实。


### 加工消息验收完成（2.2、2.3）

修正通知泵等待后，ProductMessageBoundary 37/37 Passed，退出 0、Console 覆盖完整。运输扩展后最新 42/42 Passed；前述失败记录保留。

| 功能／任务 | 实际 ET.ProductSessionTests PlayMode UTF | 结果 |
| --- | --- | --- |
| 2.2 切配／手持到点与防重 | ProductMessageBoundaryManualDeadlineNeedsDeliveryThenCompletesOnce(False/True)：实际到点未投递仍无输出，投递后业务步完成一次；重放旧序号不复制。 | 2 Passed |
| 2.2 停手接续／迟到完成 | ProductMessageBoundaryManualStopResumeKeepsProgressAndInvalidatesOldCompletion：Stop 回执后业务裁决，Paused／Resumed 保持原进度，旧完成无效。 | Passed |
| 2.3 汤／烤制到点与防重 | ProductMessageBoundaryAutomaticDeadlineWaitsForQueueAndCompletesOnce(Soup/Bake)：实际原料开始，截止等待不产成品，原实例随后完成一次。 | 2 Passed |
| 2.3 跨关／暂停到点 | ProductMessageBoundarySoupRetainsOwnIdentityAcrossLevelAndPausePendingCompletion：自然完成、合法 Continue 保留原锅；暂停收到转换不提交，恢复 Ready 一次。 | Passed |
| 2.2 原交接／退出 | GridPrepWorkChoppingStopsResumesMovesAndProducesOnce；GridPrepWorkHeldBowlHandoffDisconnectLeaveAndNewWorld | 2/2 Passed |
| 2.3 原汤回归 | GridSoup 11 项：原锅移动／离开、真实材料、份数守恒与竞用、旧批次及池化值 | 11/11 Passed |
| 2.3 原烤制回归 | SoloDishBake 五项：共钟／暂停、Completed冻结及跨关、布局保持耗时、重放一次输出、快照及整轮退出 | 5/5 Passed |
| 恢复身份兼容 | SoloSaveRestoreHalfSoupAndBakingFrozenProgressDoNotRefillOrRecalculate：实际文件恢复新实例，份数／FrozenTicks／进度不重填，恢复作业继续推进 | 1/1 Passed |

路径为原作业 Tick → ProcessInnerSender.Send(HostSession.ActorId, KitchenWorkTransition) → ProductHost／KitchenWorkTransitionHandler → 原厨房 WorkTransitions → 后续允许业务步原子效果 → GridBroadcast 同版快照后 KitchenWorkOutcome → ProductClient／KitchenWorkOutcomeHandler → 独立观察值。汤／烤制作业按世界及原实例，不按旧关授权；手工停手提高序号。PrepTick 夹具补显式消息泵，汤／烤制到点等待补后续一步，未改 RequiredTicks 或成品断言。任务 2.2、2.3 完成，当前 4/17。

### 供应／清理消息迁移验收中（2.4）

SupplyShipment 到点只发 ShipmentTransition，原 HostSession 接收值，后续业务步复验后 Ready；运输 owner 保持原厨房，世界作业跨关。采购、到货、接收另发 ShipmentOutcome，客户端复制值并发布观察事件。Receive／TakePortion 接入原 Grid 寻路时冻结 OriginKind=Supply，最终成功／拒绝／替换取消仍走 ProductCommandOutcome，不再同时发 GridCommandResult 终态。

正常 Proto2CS 再生成两个模式，旧 opcode 保持。首次编译测试观察者误写 Shipments（实际 SupplyShipments）导致 CS1061；更正后 recompile_status completed／failed=false／compilationFailed=false。原错误保留，新测试才启动。扩展 ProductMessageBoundary 42/42 Passed、Console 覆盖完整：新增 ShipmentDeadlineWaitsForDeliveryAndReadyReplayCannotMintCargo、ShipmentOriginalWorldSurvivesLevelAndPausedReadyDelivery、SupplyReceivePortionAndRejectionKeepSupplyTerminalAndOriginalItems、ReplacingSupplyApproachCancelsSupplyWithoutReceiving、CleanupHasThreeExplicitGridTerminalsAndKeepsOriginalContainers 均带 ProductMessageBoundary 前缀，分别验证到点不投递无到货／无实物、跨关暂停后一次 Ready、接收拆份及满手拒绝保留原材料和供应终态、移动替换取消不收货、水／液／固体清理三个实际终点。SoloSupply／清理受影响回归尚在执行，2.4 保持未完成。

供应／清理受影响回归完成：SoloSupply 18/18（含受管采购、接收、拆份／空包装、旧关回调两项UI）；SoloSink 3/3；SoloCleanup 23/23（含受管真实水池按钮1项UI）；SoloSaveRestoreRealFileRebuildsNewIdentityAndRemapsContainersSupplyAndRenovationExactlyOnce 1/1。均退出0、Console覆盖完整，原始结果仍在 Pipeline 输出原位。水／液／固体分类、原锅剩汤清理、加工锁与原餐具、消息重放／上下文／接近再验、一次预留／物资守恒／跨关、实际文件重建货物与一次收货均有本次运行证据，复用新增5项消息断言。任务2.4完成，当前5/17；普通Launcher及前厅等仍未完成。

前厅3.1–3.3迁移开始：Customer／Order／FrontWork保存阶段序号和单项转换／后继 pending；CustomerFlowTransition／Finished及FrontWorkTransition／Finished接入原队列，追加FrontWorkCreateRequest以区分完成通知与后继请求；FrontWorkOutcome承担认领、开始、释放和完成观察。原成长随完成原子提交，清桌已移到原DirtyPool后等待洗碗请求，Customer／Clear工作pending继续阻止自然Completed。

首次SoloCustomerFlow 6/6 Failed；补诊断与到点下一步等待后仍6/6 Failed。诊断显示首客去第一桌却停在第二桌坐标，是本次AdmitSeat把PlanCustomerPath副作用放进OrderBy.FirstOrDefault谓词所致；枚举可访问多个候选并改写路径。改顺序foreach选中立即break，不改目标位置或完成断言。真实调用时长仍不变；来客、离场入下一桌和到期需要后续投递／业务步，原用例补显式等待与到点尚Waiting的断言。失败原输出保持原位，3.1–3.3仍未验收。

修正顺序选桌后SoloCustomerFlow 6/6 Passed，真实来客/FIFO/队满、入座询问/订单、到期离场及Draining自然收口均通过。新增三项独立投递前厅边界首次2 Passed/1 Failed：顾客全部转换与Ask后继、旧认领不能完成新认领均通过；真实交付后Clear未创建，发现Customer.SuccessorQueued从Ask阶段带到离场阶段。进入新后继阶段重置该标志，保留阶段serial和原pending守护，不把失败当通过。任务3.1–3.3继续未完成。

Customer后继标志按新阶段重置后，新前厅3项全部3/3 Passed。SoloCompanion首次32项30 Passed/2 Failed：洗碗后继尚待投递时就查询不存在的Wash，第三次成长用例也仍期待在截止检测步直接完成。补等待实际Wash请求，第三次Ask先到点投递再与下一Wash认领同一步提交，保留“第三次完成先计成长、后续认领冻结ceil(61/2)=31”断言；Wash截止步明确仍Working，下一业务步才Completed。其余30项覆盖竞用、旧身份、Owned退出、暂停／半程接续与原通知防重，保持已通过结果可复用。未修改原失败输出。

### 顾客与玩家／伙伴工作验收（3.1、3.2）

新前厅边界3/3及原SoloCustomerFlow 6/6 Passed，证明真实Arrival、ReachedQueue、AdmitSeat、ReachedSeat、OrderExpired、FinishedEating、ReachedExit逐阶段均经过消息；到座完成／Ask后继的两个投递间隙分别无Ask，无消息泵时在实际坐标停住但不提前创建询问或完成订单。FrontCustomerTransitionsAndAskSuccessorWaitForTheirOwnDelivery还重放同一Create，唯一Ask和订单保持；已有Flow六项覆盖队满跳过无暗生顾客、FIFO、实际行走／暂停、询问后订单、到期真实离场和自然Draining。全部测试名前缀ET.ProductSessionTests.ProductMessageBoundary或ET.ProductSessionTests.SoloCustomerFlow见上文，本项不依赖人工修改阶段模拟自动完成。

玩家到位Start、进度到点Complete由ProductMessageBoundaryFrontWorkAtPositionAndDeadlineWaitsAndStaleClaimCannotFinish实际验证：到位但未投递仍Approaching，到点未投递无Order；Stop保留原进度，新认领后迟到旧serial／ClaimSerial不能开单，新合法完成只建一单且玩家不计伙伴成长。客户端FrontWorkOutcome已消费Finished独立值。新退出清桌洗碗用例完整使用实际材料制作、真实服务请求与行走，验证原碗和各后继投递间隙，不把它当普通Launcher证据。

SoloCompanion最新有效32项全部Passed：首次未修改30项结果复用，修正的SoloCompanionGrowthThirdActualAskAcceleratesSameTickNextWashWithCeil及SoloCompanionResumePlayerStopsAndNpcContinuesSameActualWash各新运行1/1 Passed、退出0、Console完整。增长第三次提交先于后续同一步Wash认领、冻结31tick、完成第四次一次性、原Wash半程玩家→伙伴接续、Pause／成员离开／实例替换、消息重放和单执行者均有结果；此前2 Failed保留。任务3.1、3.2完成，当前7/17；3.3仍等待两菜／原餐具受影响回归结束。

SoloCustomer37项首次36 Passed/1 Failed。两菜真实交付、重放一次计分、出餐拒绝、暂停取消、原碗完整清洗取回及自然换关均Passed；唯一dirty pour用例在手持加工到点但消息未提交时取旧输入ID，随后加工合法完成造成内容ID变化。用实际RequiredTicks+1并断言GridWorkState.Completed后再设Dirty／冻结内容ID，仍要求dirty pour拒绝且容器内容不变，不修改加工合法完成行为。该项准备新单例运行，3.3仍未勾选。

### 出餐与原餐具后继验收（3.3）

dirty pour单例包含参数引号的过滤未匹配，发现413项后NotRun空集，不算通过。使用真实用例族SoloCustomerUtensilsDirty新运行5/5 Passed；与未修改36项有效结果合用，SoloCustomer37个最新结果均Passed（其中4个参数复跑只计一次）。原Fail保留，生成/编译/运行各只证明自己的范围。

| 功能／任务 | 实际 ET.ProductSessionTests PlayMode UTF | 结果 |
| --- | --- | --- |
| 3.3 两菜一次交付／原餐具 | SoloCustomerServeBothRealDishesConsumeFoodOnceAndKeepOriginalUtensil(1/2)；ServeLatestReplayAndPayloadConflictNeverConsumeOrScoreTwice | Passed |
| 3.3 非法菜／位置／卫生／身份、旧单与暂停 | SoloCustomerServeInvalidPayloadAndPositionKeepFoodOrderAndScore四项；ServeStaleOrderCannotServeNextCustomerAtSameTable；ServePauseCancelsAcceptedUnexecutedDeliveryAndResumeCannotReviveIt | Passed |
| 3.3 原碗DirtyPool→Wash→CleanPool→取回并再次分汤 | SoloCustomerUtensilsActualEatClearWashAndTakePreserveSameBowlAndStock；TargetsClearAndWashTransferSameBowlOnceWithoutGrowthOrStock | Passed |
| 3.3 自然收尾、暂停最后洗碗／下一关保留原池 | SoloCustomerDrainLastActualWashBlocksCompletionAndNextLevelKeepsCleanPool | Passed |
| 3.3 每个衔接间隙不能结算 | ProductMessageBoundaryFrontExitClearWashPendingGapsKeepNaturalLevelOpenAndOriginalBowl：实际汤制作／交付后真实离场，到出口消息、Clear创建、Clear完成、Wash后继各停泵，pending均阻止Completed，洗净后自然10分1星 | Passed |
| 3.3 满目的地／旧目标／暂停／脏容器守恒 | SoloCustomerTargetsFullDestinationPreservesWorkAndOriginalItem两项与其他Targets；UtensilsDirty五项 | Passed |

上表没有省略Serve前缀：所有Serve族实际完整名称均以SoloCustomerServe开头。保持原唯一出餐提交，ProductCommandOutcome承担其Executed／Rejected／Cancelled；CustomerFlowFinished→FrontWorkCreateRequest(Clear)，FrontWorkFinished→FrontWorkCreateRequest(Wash)，原Customer／Clear pending持续持有衔接事实。clear及wash用例补等待真正后继请求，不直接创建业务效果。任务3.3完成，当前8/17；普通Launcher、关卡准备与其余任务仍未完成。

最新协议生成对照HEAD：Client与ClientServer各121个原opcode名称和数值保持，追加17个明确类型；未手改生成物。当前OpenSpec严格校验已通过，不能代替未完功能验收。

FrontWorkCreateRequest真实队列／真实KCP拒绝两项2/2 Passed，新内部类型只能进入正确HostSession，wire无业务入队；旧opcode121个两模式保持、新增17个。Buff准备开始迁移：原LevelBuff冻结seed／allowedIds／count，Awake只校验不产候选，DrawVersion0表示未准备；原Restaurant服务步投递BuffPrepare到HostSession，接收值后下一允许Preparing业务步抽一次，提交后快照／BuffPrepared到ProductClient。Start以BuffNotReady门槛阻止提前开店，Choice有独立ProductCommandOutcome。源与消费者编译已通过，新增门槛和防重测试待执行，任务4.2尚未完成。

新增ProductMessageBoundaryBuffPreparationGatesNewLevelAndReportsPreparedAfterSnapshotOnce 1/1 Passed：Continue后尚无候选／DrawVersion0，未投递准备时Start业务拒绝BuffNotReady；只泵消息也不抽候选，后续步抽一次并在安装快照后复制BuffPrepared，重复serial不重抽／重发，选定后合法开店。SoloBuff首次21项20 Passed/1 Failed；唯一跨关携带用例仍同步索引新关候选（尚等待准备），补FrontWait最多8步等待Buff.Prepared后才选择，保留旧锅、原耗时与旧关选择拒绝断言。UI准备期文本改为“本关加成准备中”，Prefab／CodeBind／owner不变。原失败保持，4.2待新单例和布局回归。

### 布局与Buff准备／选择验收（4.2）

ProductMessageBoundaryBuff两项2/2 Passed：PreparationGatesNewLevelAndReportsPreparedAfterSnapshotOnce覆盖尚未准备门槛、投递后一次抽取／通知／旧序号拒绝；BuffAndLayoutSuccessAndRefusalEachHaveExplicitTerminal实际提交四条独立命令，Buff invalid/valid、Layout blocked/valid各有自己的Rejected／Executed，原工位物品身份保持、候选不重抽。SoloBuff21个最新有效结果全部Passed：前次未修改20项复用，修正跨关携锅的SoloBuffCookingCarryKeepsOldBatchAndRejectsOldLevelChoiceWithoutInheritance新1/1 Passed，旧失败保留。原候选种子、抽取、重复／冲突、选择锁、暂停、旧关按钮、烹饪冻结时长与受管Buff UI旧记录均在当前准备实现下运行。

SoloStation23/23 Passed，包含实际两段邮箱布局／旋转、防重、版本竞用、暂停及Operating拒绝、边界／重叠／入口、角色／掉落物、原锅批次／手工进度、真实玩家／伙伴／顾客移动地形和几何投影。全部运行退出0、Console覆盖完整；没有原场景／Prefab／CodeBind结构改动，未将受管UI用例当普通Launcher验收。

任务4.2完成，当前9/17。CLI status的isComplete只说明规划齐全，apply仍显示8项待实施；关卡自动收尾、装修准备、移动入口整理、存档、会话全面兼容与HUD／普通连续入口仍须完成。

装修准备4.3开始：自然Completed只发送新LevelFinished内部完成通知；原Restaurant.FinishedTransitions消费后再发RenovationPrepare，原装修owner收到值后下一允许Completed步生成候选，先快照后RenovationPrepared。暂停不自动准备，旧关／原实例／serial拒绝；准备之前Continue或保存为RenovationNotReady，空候选须先完成实际准备。原选择一次、预检和Continue内原子安装不改，不增加免费物品。Proto2CS正常生成，追加LevelFinished，协议仍为本案未发布18；原生成物未手改。测试夹具RenovationComplete补最多8步等待实际Prepared，LevelDo的成功Start仅在预设合法开店前提下等待Buff.Prepared；显式提前开店拒绝用例仍直接消息调用，不被该夹具等待掩盖。新增完整通知／准备／Continue重放断言正在编译，4.3未完成。

新增ProductMessageBoundaryRenovationCompletionAndPrepareNeedSeparateDeliveryAndContinueInstallsOnce 1/1 Passed，证明Completed通知与候选准备两个独立投递间隙、提前Continue拒绝、候选／通知一次、重放Prepare／Finished不刷新、选择后Continue一次安装与精确重放不再换关。SoloRenovation首次16项14 Passed/2 Failed：所有原业务空池／耗尽／选择锁／安装零物资／失败零安装／旧消息Passed，两个受管UI用例仍把Completed当候选已准备，分别提前断言空池提示／固定候选。补等待当前RenovationLevelId和ChoiceVersion真实安装；实际Solo界面准备时显示“正在准备装修候选”并等准备后再显示Continue，Host／Client保持原Continue范围。原Fail保留，4.3未完成。

原测试合法选择／Continue夹具补最多8步等待真实装修Prepared，暂停和明确提前拒绝的边界用例仍直接消息调用。两个厨房跨关边界用例在读取候选前显式等待，旧Buff原始两端Continue用例同样先等待结果准备；该等待只通过原服务／消息泵推进，不手写Ready／候选／阶段，也不改变Completed期间厨房冻结。后续受影响回归按新增范围运行。

### 装修完成通知／准备／安装验收（4.3）

两个修正的受管UI用例新运行2/2 Passed、退出0、Console完整：SoloRenovationUiActualBlockedCandidatesShowsEmptyFeedbackAndCanContinue，SoloRenovationUiRealZeroStarChoicesReopenOnceInstallAndOldCallbacks。保留前次未改业务14项Passed，16项最新有效结果全部Passed；失败那次仍记录Failed。零星自然结算、固定候选、合法选择/锁定、耗尽与无合法空间空池、关闭重开旧回调、安装一次／无额外物资、布局及晚到落物冲突零安装均有当前实现运行证据。

新增LevelFinished的真实HostSession队列与真实KCP wire拒绝2/2 Passed；正常生成后两个模式旧121 opcode保持，新增18类型。调整合法调用夹具等待后，完整ProductMessageBoundary52/52 Passed，覆盖前述所有家庭消息及厨房跨关、配送跨关、顾客／玩家／伙伴后继、Buff门槛、装修两个投递间隙／防重与Continue精确重放；旧结果没有因阶段切换重跑，复跑原因是新增完成准备影响共用Continue入口。范围仍是本机Editor；真实普通Launcher连续验收未完成。

任务4.3完成，当前10/17。同步15份Markdown本地367引用、UTF-8检查无错误；首个检查器未解码C%23%20编码目标，修正URL解码后目标实际存在，未修改正确引用。OpenSpec严格校验通过；不把文档或生成当功能验收。

4.1关卡自动转换开始：原Level有TransitionSerial／Queued，营业到点夹止ElapsedOperatingTicks，只发LevelTransition(Drain)；Draining无真实前厅活动只发Complete。原HostSession接入，Restaurant控制先提交再复验转换，暂停时保持收到的转换等待；完成仍一次取消原未执行动作、冻结手工、计原星数并发送LevelFinished到装修后继。暂停Completed仍按原规则拒绝LevelCompleted，合法Resume/Continue及拒绝结果都有原ProductCommandOutcome。Start／Pause／Resume／Draining／Completed和新Level的Preparing另经同版快照后LevelPhaseChanged，客户端只复制观察值。

没有新时钟或Tick消息；营业过点等待投递不增加Elapsed或新来客，原世界厨房作业继续按业务步推进，进入Completed后冻结。Proto2CS正常生成现有LevelPhaseChanged的流序号字段，旧opcode不改。新增到点／暂停待转换／自然零星／阶段通知和新关准备／旧Level拒绝测试待运行，4.1尚未完成。

新增两项ProductMessageBoundaryLevel 2/2 Passed：截止未投递时仍Operating且Elapsed夹止，暂停保持收到的Drain/Complete等待，恢复各提交一次、自然0星并复制阶段通知；Continue后未准备Start拒绝、准备后合法开店，退役Level转换不进新关队列。SoloLevel首次14项12 Passed/2 Failed，均为旧用例把deadline+1当已Completed：缺下一关用例尚Draining便Continue得到WrongPhase，原锅跨关同样尚Draining。补后续允许步到deadline+2，保留MissingNext与原锅／原角色／物资断言，原失败保留。

原装修两个投递间隙用例补真实Drain转换投递；原餐具间隙用例用120tick合法配置初态，使真正离场／清桌时已Draining，不直接写阶段或重入消息泵保持原同Tick。供应／Buff跨关等待对应后续步，不改任何生产配置时长。4.1及受影响消息验证继续执行，不把已编译当完成。

关卡迁移后完整边界首次54项53 Passed/1 Failed，唯一装修边界在AdvanceTicks截止后先跑业务步、后泵出Drain，仍Operating而断言Draining。原夹具AdvanceTicks每tick只泵一轮（本轮新发通知尚未到handler），补“先只泵消息、再业务步”的准确序列，不改源时长或阶段效果。新增Level两项、前厅120tick合法初态间隙及其他51项均Passed，失败原记录保留。

装修边界准确泵序新单例1/1 Passed；最新54项消息边界有效结果全部Passed（未改53项与修正单例合用）。SoloLevelCarry新小集6/6 Passed，和原未改12项合用全部14个最新有效结果Passed。源码仍按控制先提交、Complete转换再冻结厨房，因此供应回归调整为该消费步不再额外推进运输；前厅Draining观察需截止后再允许一步。旧配置、星数、原锅与库存断言不改；这些修改尚待对应小集运行，4.1继续未勾选。

### 完整具名合同与阶段身份（1.1）

最终追加18类型：KitchenWorkTransition/Outcome，CustomerFlowTransition/Finished，FrontWorkTransition/Finished/Outcome/CreateRequest，ShipmentTransition/Outcome，LevelTransition/Finished/PhaseChanged，BuffPrepare/Prepared，RenovationPrepare/Prepared，ProductCommandOutcome。既有GridCommandResult保留；11类内部接收及7类结果发送／消费者均已接入原owner、稳定HostSession和ClientSession，不创建新总线／等待表／Clock。

Model阶段身份在原Manual/Auto/Bake、Customer/Order/FrontWork、SupplyShipment、Level、LevelBuff、RestaurantRenovation中；字段是当前阶段及单项待衔接值，消息回收前复制独立值。协议17→18、正常Proto2CS的Client／ClientServer原121 opcode保持，追加18类型；后续字段扩展同属未发布本案18，生成物未手改。最新recompile completed/failed=false/compilationFailed=false。

54个消息边界最新有效结果全部Passed，11类TypedTransitionsReachOwnedQueueBeforeBusinessCommit证明正确SceneType／HostSession／原队列，只投递不推进业务；11类NewInternalTypesCannotEnterHostFromWire以既有实际UDP-KCP入口证明无业务入队、连接和绑定保持，Host白名单未开放内部类型。客户端的7类结果独立消费、各业务开始／结束／准备／Phase实际UTF见相应任务。design已按最终名称同步ET树、请求路径、源／目标、原子效果、结果及后继pending。

任务1.1完成，当前11/17；剩余任务仍需功能验证，不能把planning isComplete当实施完成。当前KCP仍用原测试Host port=0/OS分配、原NetComponent owner退出释放；Pipeline端口由Main实际实例发现，没有新固定endpoint、PortRegistry或第二Editor启动。

### 关卡控制／自动阶段验收（4.1）

新增ProductMessageBoundaryLevel两项2/2 Passed，53个未改边界与修正装修单例合用最新54项全部Passed；真实截止等待、Elapsed夹止、暂停待Drain／Complete、恢复提交一次、零星自然完成、阶段通知、Continue新准备门槛及旧关转换隔离均有运行证据。SoloLevel14项最新有效结果全部Passed：此前未改12项及新SoloLevelCarry6/6，失败两个已修正后续步，实际开店／暂停／恢复、取消未执行路线、保留当前步、手工／汤冻结、当前厨房与原锅跨关、缺下一关零修改、各种旧动作与重复Continue均保持断言。

因自然完成改为消息，补跑直接受影响的SoloSupplyLifecyclePauseCompletedDrainingAndNextLevelKeepSameOwnerAndShipment、SoloCustomerFlowDrainingStopsNewArrivalsAndNaturallyFinishesRealOrders、SoloBuffCookingCarryKeepsOldBatchAndRejectsOldLevelChoiceWithoutInheritance、SoloBuffChoiceExactOldReplayRejectsAfterAnotherLocalPeerContinuesLevel，各1/1 Passed，退出0、Console完整。运输在Complete消费步先冻结，既有前厅排尽无新增，旧锅与原耗时、旧Buff选择拒绝不继承仍成立。接受消息经过下一业务步的调度变化，不改配置时长或重入sender。

任务4.1完成，当前12/17。剩余2.1移动实物入口／验收、4.4保存入口和结果、5.1会话兼容、5.2 HUD/Board及5.3普通Launcher连续流程，未运行部分继续未完成；未提交、未归档。

2.1移动实物入口开始收窄：SubmitGridIntent改为Hotfix程序集internal，仅对应GridSubmitHandler调用，Add/Pour/Portion等完整原子效果仍私有。原GridCore直接提交夹具改为ClientSession→Peer→HostSession两段真实消息、只消息回执不推进时间；退役本地／KCP绑定用例在退出前复制可信GridSubmitRequest身份，退出后经原authority邮箱证明EntryClosed，不再直接跨程序集调用提交函数。非法移动同样经真实请求拒绝。旧生成及场景资源未改，新编译／对应原行为小集待执行，2.1仍未完成。

### 移动与完整实物动作（2.1）

SubmitGridIntent已为internal，生产仅GridSubmitHandler调用，测试源码无直接调用；所有Core有效动作经ClientSession／Peer／HostSession真实请求。原世界／绑定／关／命令身份与整个动作校验后原子效果不拆字段。PrepDo在本地最终业务结果后只泵消息，逐命令断言独立终态恰好一次，Status／Reason对应实际结果。

| 动作／验收 | 实际 ET.ProductSessionTests PlayMode UTF | 结果 |
| --- | --- | --- |
| Move、真实到达／替换取消／等待占格 | GridCoreMoveCompletesOnArrivalAndCancelsReplacedMove；GridCoreRetargetFinishesStepAndCancelsOldInteraction；GridCoreTwoStandThirdWaitsAndCanCrossFullCell | Passed，含新增旧取消／新执行各一次通知 |
| 拿／放竞用、断线掉落与同格拾取 | GridCoreItemRaceDropIdentityAndSameCellPickup；GridPrepContainerDefinitionsAndUniqueContents；CoopCoreExitRealKcpDisconnectDropsSameCookingPotOnceAndKeepsClockSenderAndOfflineMember | Passed，原Item/容器/内容身份保持，错格拾取拒绝 |
| AddToContainer、满手／容量／类型／旧目标／嵌套拒绝 | GridPrepContainerDefinitionsAndUniqueContents；GridPrepContainerRejectionsDoNotTransfer；GridPrepWireLocalKcpDuplicateAndIndependentVersionedGraph | Passed，一份原材料移入原容器，重放不复制 |
| PourContents、顺序合并与满锅／工作锁／旧内容拒绝 | GridSoupTransferOrdersPreserveBowlAndActualMaterials(False/True)；GridSoupTransferRejectionsKeepBothSides；GridSoupTransferFullPotAndCookingLocksKeepMaterial | Passed，所有原内容ID守恒 |
| PortionSoup、原锅／份数／最后一份及旧批次拒绝 | GridSoupPortionsConsumeInputsOnLastAndRejectOldBatch；GridSoupPortionLastRaceUsesRealLocalRequests | Passed，原碗不变，最后一份一次消费原输入 |
| 退役绑定拒绝及接续 | GridPrepWorkHeldBowlHandoffDisconnectLeaveAndNewWorld；CoopCoreExitRealRemoteLeavePausesHeldWorkAndLocalCanResumeSameProcess | Passed，原authority真实邮箱返回EntryClosed，公共sender仍运行 |

本次GridCore4/4、加终态断言后Move单例1/1、GridPrep5/5、带逐命令终态断言的GridSoup11/11、两个改用真实authority消息的CoopCoreExitReal2/2，均退出0／Console完整；Move单例不重复计数。非法移动经真实回执BlockedCell，已开始工作不被破坏。默认port=0的现有KCP测试入口与原owner退出释放保留；没有新玩法、场景／Prefab／表源变更。

任务2.1完成，当前13/17；剩余保存、会话兼容、HUD反馈和普通Launcher连续流程。

4.4保存入口核对与终态开始：保留ProductRoot的SoloSaveBrowse/Select/Write请求响应，原SaveResult AsyncOwner持有物理写入，完成经SoloSaveCompletedRequest到原HostSession/绑定完成队列，Restaurant服务步提交持久事实再排原Continue。此前1.3已统一失败/成功Level动作终态，本轮客户端独立值按OriginLevel与适用Level复制ChangedLevel，新增失败/重试验证每条各唯一Rejected SaveFailed/Executed、原关身份保留和实际新关；不另开文件writer或等待表。

SoloSave首次29项27 Passed/2 Failed：全部文件保护、Browse/Select不建世界、取消、写盘失败/重试/响应丢失/原结果不变、重复/迟到完成、半汤与烘烤恢复及选档UI通过；新增两条独立终态断言通过。唯一实物恢复用例被我新增PrepDo断言只按command/binding匹配，重建世界后这些编号会重用；补WorldId/WorldGeneration，原业务恢复断言不变。保存UI在Completed第一帧找尚未准备的装修按钮，补当前候选实际就绪等待。原Fail保留，当前补跑两个受影响单例，4.4未完成。测试仍用原随机专属Temp目录、finally清理自己创建文件和空目录，用户存档未触碰。

### 已有保存完整消息与终态（4.4）

SoloSaveRestoreRealFileRebuildsNewIdentityAndRemapsContainersSupplyAndRenovationExactlyOnce新1/1 Passed，SoloSaveUiOneClickCloseAndReopenKeepsResultOwnedCommit新1/1 Passed；和未修改27项合用，SoloSave29个最新有效结果全部Passed，退出0／Console完整。两个旧失败原记录保留。

| 已有功能／任务 | 实际 ET.ProductSessionTests／ET.SoloRestaurantUiTests UTF | 结果 |
| --- | --- | --- |
| Browse/Select、损坏拒绝且不建世界、取消选档／合法装载 | SoloSaveRepositoryBrowseReadsOnlyHeaderAndSelectRejectsDamagedPayloadWithoutWorldCreation；SoloSaveSelectionInvalidBaselineRejectsBeforeHostAndNewGameCreatesNoFakeLoadedSave；SoloSaveSelectionUiBrowsesWithoutWorldCancelsValidLoadRejectsInvalidAndRestoresSelectedFile | Passed |
| Write失败／不可变结果重试／独立两条终态 | SoloSaveCommitFailureRetainsSameImmutableResultAndRetryPublishesOneFile：每条独立失败SaveFailed、成功Executed且ChangedLevel=true，原关Context保留，已安装新关 | Passed |
| 持久事实门槛、重放不重存重装 | SoloSaveCommitZeroStarRequiresDurableFactBeforeSingleNextLevelAndReplayReturnsSameResult；PublishedFileWithLostWriteReceiptRetriesSameResultWithoutSecondRecord | Passed |
| 无成功状态／未选装修不写文件、旧/重复完成不入新关 | SoloSaveCommitPreparingOperatingExitAndUnsettledRenovationDoNotCreateSuccessFiles；SettlementChoiceMustBeLockedAndLateCompletionCannotEnterDifferentLevel | Passed |
| 原实物和活动恢复新身份，不补库存／重算耗时 | SoloSaveRestoreRealFileRebuildsNewIdentityAndRemapsContainersSupplyAndRenovationExactlyOnce；RestoreHalfSoupAndBakingFrozenProgressDoNotRefillOrRecalculate | Passed |
| UI单击／关闭／重开不取消结果owner提交 | SoloSaveUiOneClickCloseAndReopenKeepsResultOwnedCommit：真实普通Prefab指针、失败重试、隐藏期间写入、同池HUD重开、唯一文件／新关／装修 | Passed |
| 原文件保护 | 原Repository三项及Payload族：冲突保留旧bytes、损坏清楚拒绝、只清自己临时文件；本次全部运行 | Passed |

未改原文件保护或扩展存档功能。写盘源只在原repository handler；file续体只发送SoloSaveCompletedRequest并消费答复，不能直接换关；物理写入与迟到观察仍由SaveResult AsyncOwner收尾，不由HUD掌管。合法结果Context从文件完成服务步到原统一ProductCommandOutcome，消费者复制OriginLevel／适用新Level差异，值内ChangedLevel不再默认丢失。

任务4.4完成，当前14/17；剩余会话兼容、HUD／Board反馈与普通Launcher连续流程。所有新测试目录由fixture随机创建并在finally核对根后只清自身文件，用户目录与记录未改。

5.2显式作业观察接入：原ClientScene中KitchenWorkOutcomeApplied／FrontWorkOutcomeApplied只刷新原受管GridPresentation HUD，原WorkText从独立值显示手工／汤／烤制或询问／清桌／洗碗的已认领／已开始／已接续／已停手／已完成。仍先安装快照，再收到阶段，按当前投影是否含原作业ID显示；旧Level前厅不在新投影就不显示。没有新UI业务状态、池、Prefab／CodeBind或输入路径。烤制真实UI用例新增“已开始”和“已完成”消息显示断言，回执阶段仍Accepted／无完成输出，待运行。5.1会话兼容只选相关原SoloMessage及本地／KCP入口，之前公共输入生命周期19项结果可复用，不因文档阶段全量重跑。

### 会话／原公共收尾兼容（5.1）

本次必要小集：SoloMessage7/7，LocalJoinProbeValidationAndSingleLeaveKeepListener1/1，RealKcpSharesAuthorityKeepsAliveAndLeaveReturnsCommitted1/1，ClosingBeforeSharedCommitRejectsAndDrainsMailbox1/1，均PlayMode真实发现、退出0、Console完整。覆盖Join/Probe校验、精确本人成员/绑定、真实KCP心跳保持和Leave提交后回送，单成员退出余端仍Probe、公共sender/监听不停止；Closing先关资格、真实有序队列拒绝并结清。

SoloMessage包括WholeStopCancelsHeldCallAndRetiresItsLateInput（Stop当场取消值等待，迟到输入/响应各按原所有权收尾，不等后续帧）、CanceledOwnerKeepsPhysicalPeerUntilReply、Timeout、未发送输入、消息泵与业务时间分离、统一编号/旧上下文及物品投影。复用本案1.2的19/19 LocalMessageShutdown公共行为结果（原sender/queue/Ordered源码此后未变），并复用11类新内部消息真实KCP拒绝／失效Peer队列验证及本次2/2 CoopCoreExitReal的原对象掉落/接续、旧资格拒绝/公共sender继续。没有扩大远端产品玩法或启动第二Editor。

任务5.1完成，当前15/17；余5.2真实UI阶段显示/普通点击与5.3普通Launcher连续流程。Player/AOT、手机、LAN/公网及双Editor均NotRun，不由本案本机现有KCP证明。

5.2当前实际UTF：SoloRestaurantUiProjectionActualBakingSeparatesReceiptProgressAndCompletion新1/1 Passed，明确指针一次请求、回执Accepted且无成品、WorkText“烤制已开始”、暂停冻结、完成消息“烤制已完成”且一个成品／不计分。SoloRestaurantUiLifecycle2/2 Passed，覆盖普通Prefab/Board实际输入、同池关闭重开、旧回调不提交，以及实际初始加载中退出无孤儿HUD。原实时订单/暂停/旧Level按钮用例补等待当前结算准备完毕后合法下一关；不手改本案业务阶段。普通Launcher仍待连续操作，5.2与5.3尚未勾选。

### 最终核对发现的 Continue 就绪结果差异

普通 Launcher 已完成两菜、清理供应、暂停、自然20分2星、原餐具回收、装修保存和下一关。核对 design 时发现原 Continue 在新 Level 建树即发 Executed，实际 Buff 仍未 Prepared；开店门槛虽拒绝提前 Start，但不满足设计要求的 Continue 终点。4.1 恢复未完成，补最小就绪结果修正：新 Level 持有原 LevelControlEnvelope，安装后仍 Accepted，原 BuffPrepare 在允许服务步提交后才发唯一 ChangedLevel 终态。准备期间精确重放返回 Accepted 不再次安装，保存写盘后的 Continue 共用该路径，不新建等待表或时钟。待运行相关边界／Level／Save／已有合作控制及 UI 小集，并补普通入口跨关增量。
Continue就绪修正首轮：ProductMessageBoundary54/54 Passed（退出0、Console完整）；SoloLevel14项13 Passed/1 Failed，SoloLevelCarryPreservesCookingPotAndRejectsOldLevelAndDuplicateContinue在SoloLevelTests.cs:325仍期待建树后立即Executed，实际Accepted（Expected3/Actual1）。原实物／锅／原作业断言均已通过，补实际BuffPrepared等待后再断言最终重放，不削弱终态预期；直接装修重放夹具同样接入此等待。失败原结果不改写，其他13项复用，本例另建运行。

### 普通 Launcher 完整流程（5.2／5.3）

本次精确工程 D:/MyWork/CarzyCooker/Unity、Editor PID44196、Unity6000.3.18f1，普通 Assets/Launcher.unity Play，角色Solo，协议18。没有 UTF harness 驱动，没有直接写领域状态、加速业务时间或改平衡表；CLI eval 仅读取状态，操作调用真实当前可交互 Button 的 ExecuteEvents.pointerClickHandler。每个业务按钮即时编号增加1，随后观察对应 Executed／Rejected。首次“新开餐厅”建立新世界 e3563b2b6cc3457dba527b8c7b2608cd，原关1284133726715932。

| 功能／任务 | 普通 UI 实际操作与提交证据 | 结果 |
| --- | --- | --- |
| 5.2 单次输入与阶段出口 | cmd1–54逐点击编号+1；回执Accepted后等待真实对应终态。烤制“已开始／已完成”与回执分离由当前真实Prefab UTF SoloRestaurantUiProjectionActualBakingSeparatesReceiptProgressAndCompletion 1/1验证；原输入/池重开/旧回调由SoloRestaurantUiLifecycle 2/2验证。 | Passed |
| 清理／有限供应 | 原碗1284133726715912接水、排液身份保持；cmd23采购番茄，库存5→4；cmd24接收包装1284134354813619，cmd25/27拆出原番茄1284134354813620/3621，cmd26/28垃圾处理两份，cmd29取空包装、cmd30倾倒。交接台清空，余量仍4，没重复采购/收货。 | Passed |
| 两菜及手工暂停 | 实际切番茄、碗内打蛋并倒入原锅1284133726715918，cmd19煮汤完成两份；cmd22烘烤成真实吐司再装原盘1284133726715925。手工打蛋cmd15暂停，两次真实间隔读值均2/60，cmd16恢复后真实完成；没有用回执代替成品。 | Passed |
| 询问开单与出餐 | 固定伙伴真实询问后生成两菜订单；cmd38未到餐桌的汤交付明确Rejected RequiresTablePosition，手持原汤碗保留。cmd39到餐桌1／cmd40汤成功；cmd41拿原盘、cmd42到餐桌2／cmd43吐司成功，总分20，原餐具留桌；cmd44/45前厅暂停恢复。 | Passed |
| 清桌／洗碗与自然结算 | cmd46/47实际清桌两桌，原碗1284133726715912和原盘1284133726715925经DirtyPool／真实伙伴Wash进入CleanPool；最终客户0、必要工作全部Completed，Wash冻结31Tick。自然Draining→Completed，20分2星；没有直接Complete或删除顾客。 | Passed |
| 装修、文件与下一关 | cmd49选择新增台面；cmd50保存并继续，真实新记录a8a919badcce4d329056fb1e7407e1e0.ccsave发布后新关1284135162218458 Preparing。工位16→17，仅增一个；实物6件、原锅剩1份、番茄余4、鸡蛋/面包各5，旧库存不补满。cmd51新关取回同一原碗，cmd52放碗台。 | Passed，Continue结果时序的最终修正另补增量 |
| 重开与收尾 | 原GridPresentation.CloseGridHud/OpenGridHud经受管owner关闭重开，编号仍52；随后暂停/恢复各+1为53/54。旧回调和新轮隔离复用当前生命周期UTF，普通运行Stop后Launcher active且dirty=false、compiling=false。 | Passed |

cmd48尝试Completed取回原碗为Rejected LevelCompleted，没有改物；待下一关cmd51合法取回。这个拒绝按原关卡合同记录，不当作完成取回。临时只读探针曾误用不存在的Batches/ProgressTicks/RequiredTicks/TargetId/Root()/DefinitionId而编译失败，均未执行后续动作、未改生产代码；纠正字段后重新查询实际值。它们不是产品编译错误或功能通过证据。

实际存档位于Application.persistentDataPath/Solo/PC/82a5f5085f71444087133b2aaeedb02f/，新文件1171bytes，2026-10-13T14:01:39.0743469Z。既有8118cfdb94c1492ca54555903055fcc0.ccsave仍1410bytes、原2026-10-13T03:41:20.9429434Z；profile.id也保持原写入时间。只新增本次新餐厅记录，没有覆盖或删除用户旧档。

Console即时查询session d121b52c362241e8b50ae9b4ca4295df、cursor3076、reset/dropped=false，groundTruth compilationFailed=false/compiling=false/consoleErrors=0。console原始100项返回均按实际level/logType读取，无Error/Exception/Assert。compact_console.py把全部entries传给只处理错误的core，core默认把Log归为Error，误标了Initialize等Info；核对原始level=log/logType=Log，不能将压缩显示当实际错误或吞掉真实错误。本案未改该技能，作为诊断工具遗留记录，Tools/AutoTesting仍按原始级别与groundTruth校验。

Player/AOT、Android、LAN/公网、双Editor及新远端玩法仍NotRun，本案不提升其交付范围。后续Continue时序只补结果就绪与普通跨关，不重做已覆盖且行为未变的两菜/清理/前厅全流程。
Continue就绪最终UTF：ProductMessageBoundary54/54 Passed，新增既有两项控制泵断言证明建树后Accepted且没有终态、待准备重放Accepted不重装，Buff提交后原Continue仅一次Executed/ChangedLevel。SoloLevel首轮未改13项与修正Carry单例1/1共同构成14个最新有效Passed；SoloRenovation原工位／半程手工身份与重放单例1/1 Passed；SoloSaveCommit5/5 Passed（实际文件失败重试、落盘门槛、准备后唯一终态、迟到/重复）；原真实KCP CoopCorePermissionsHostLocalBuffLayoutStartPauseResumeAndContinueUseOrderedServiceStep及CoopFrontLevelNaturalZeroRepeatedContinueAndOldFrontMessagesKeepNewRolesAndInventory各1/1 Passed。SoloSaveUiOneClickCloseAndReopenKeepsResultOwnedCommit新1/1 Passed，真实保存失败/重试/隐藏中结果owner写入、受管重开、唯一文件和下一关。各运行均退出0、Console覆盖完整，无Skipped或空集。

4.1差异已修正并有上述消息/时钟/重放证据，回填完成。字段留在新Level，实体复用Awake清空，原Peer EntityRef及Binding二次检查排除迟到成员；Stop仍释放原树与异步owner。5.2最后原订单/暂停/旧Level按钮UTF正在独立新运行，5.3待普通跨关增量与最后静态检查。
最后UI增量：SoloRestaurantUiProjectionRealOrderRejectionPauseAndOldLevelButton新1/1 Passed，退出0、Console覆盖完整；原真实订单拒绝／暂停／旧关按钮及准备后的下一关通过。23份本轮Markdown/423本地引用均存在且UTF8有效，git diff --check退出0，技能目录无改动。5.2全部实际UTF与普通UI证据齐全，回填完成。

### 最终普通跨关增量与交付核对

Continue修正后再次普通Launcher Play：选档只读显示既有11:41记录及本次22:01新记录，选择内存试玩建立独立世界624d469c0c7b48b488cab3ebe2c0bb3f，未读入旧世界实物。原关1284136571502620，真实选择Buff/开店后自然Draining→Completed，0分0星。cmd3选择新增切配台，cmd4下一关回执Accepted；真实终态Executed时，新关1284136623932115/BuffDrawVersion1/PreparationControl=null，独立ProductCommandOutcome.ChangedLevel=true且OriginLevel为原关。工位17、实物7，供应各5，装修只安装一次。本次内存流程无文件写入；前一完整经营流程的写盘、两菜、原碗/盘和半锅承接证据复用，文件服务与这些玩法算法没有再改。

增量结束后通过本次自有Editor的editor_stop收尾。Console session仍d121b52c362241e8b50ae9b4ca4295df/cursor3471，reset/dropped=false；groundTruth compilationFailed=false/compiling=false/consoleErrors=0。当前原输出仍在Unity/Temp/pipeline_test_status.json、Editor原日志，不建证据包。该原生结果是最后UI用例1/1 Passed，前述各次结果保留其原即时输出和此处逐功能映射。

三个change的openspec validate --strict --no-interactive均valid、退出0；资料与夹具两案明确skip_specs，免规格按原范围处理。完成前对照本案2份delta、10项Requirement/28个Scenario、design与17项任务逐项核对：

| 核对维度／规格族 | 实现与运行证据 | 结论 |
| --- | --- | --- |
| 完整性：三个change任务与资料交付 | 资料各纪律/唯一正文/引用及分组建议3/3；夹具消息/业务时间、等待失败/部分清理及85项迁移3/3；本案逐功能UTF与上述普通入口完整覆盖17项 | 已完成 |
| 输入所有权／技术退出2项规格 | MessageQueue、ProcessInnerSender、Ordered invoker原路径；LocalMessageShutdown19项、SoloMessage7项、现有Join/Probe/Leave/关闭与KCP小集，输入只收尾一次且Stop不等未来帧 | 已覆盖 |
| 接收与工作完成／状态事件2项规格 | 原命令入口及ProductCommand/KitchenWork/FrontWork结果；单快照后独立通知、真实UI回执/开始/完成、初始化失败不建半树及成员事件只观察 | 已覆盖 |
| 完整边界／自动后继2项规格 | 11类内部HostSession路径、wire/Peer拒绝，54项边界全通过；手工/汤/烤制/顾客/询问/清桌/洗碗/供应/关卡/Buff/装修的实际停泵间隙及后续服务步 | 已覆盖 |
| 终态身份／自然收尾2项规格 | 原世界/Level/实例/绑定/serial；厨房与货物跨关、旧认领/消息防重、原Customer/Clear successor pending阻止提前Completed、第三次伙伴成长后冻结洗碗时长 | 已覆盖 |
| 投影顺序／真实边界运行2项规格 | DTO回收后纯值保留、同版快照先终态、文件失败不换关、准备后唯一Continue结果；逐点UTF及两菜20分2星完整普通流程、补0星新轮跨关 | 已覆盖 |
| 一致性：owner、生成物、模式范围 | 保留原ET树/程序集/唯一时钟，SubmitGridIntent收窄且仅原SubmitHandler调用，原UI/CodeBind结构未改；正常生成18个新增类型/旧121 Opcode不变，协议18，内部请求未进入wire白名单 | 符合design |

核对未发现本案范围内未覆盖的必需验收或未实现规格；Continue与设计的时序差异已修复并补实际验收。未改变的原配置、夹具及本案已覆盖的功能证据按对应运行复用，没有阶段性全仓重跑。已知Console压缩脚本误标已写KnownIssues，未扩展技能修复。Player/AOT、Android、物理LAN/公网、双Editor、新远端功能明确范围外NotRun，不能由本案升为通过。未提交、未归档、未push、未操作第二工作区；后续单机/联机案仍未开工。
最终状态复查：openspec list显示本案17/17 complete、另外两案各3/3 complete；apply为all_done/remaining0。Client与ClientServer均对HEAD逐常量核对旧121 Opcode全部不变、新增18；23份Markdown/423本地引用缺失0、UTF8有效，git diff --check退出0。Editor最终Play stopped、compiling=false、Assets/Launcher.unity active且dirty=false。全部23项任务交付证据齐全，不执行提交/归档。


## 2026-10-13 归档记录

用户明确授权归档、commit、push。任务与既有运行证据复核完整，归档到当前日期目录；先同步消息案的network/product-session两份主规格，再由CLI归档，资料/夹具两案按skip_specs不增加规格。已同步要求逐块比较一致，旧场景和其他要求保留；迁移相对引用与入链，不修改业务代码或原始运行输出。已有行为未变，UTF与普通Launcher按原映射复用，本次仅做格式/引用/规格检查；Player/AOT、手机与LAN等未验范围仍保留。此前“未提交/未归档”描述均按原阶段解释，最终版本状态查Git。

归档收尾检查：本次27份Markdown、295个本地引用均存在且UTF8有效；72处迁移相对引用已按原目标重定位。network主规格11→13项，product-session21→27项，10个delta要求块与主规格逐块一致，其他要求与原场景保留。openspec validate --specs --strict为30/30，--archived任务lint为27/27，5个只调整依赖链接的活动案严格格式均valid；CLI活动清单已无本次三个案。本次无业务修改或功能重跑，已有运行证据按原范围复用。
