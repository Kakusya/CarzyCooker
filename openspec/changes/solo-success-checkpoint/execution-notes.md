# Execution Notes

## Planning status

2026-10-12 用户授权先写五个 change；本轮只创建规划，未实施、未导表、未编译，Unity/UTF/包体均 NotRun。以现有 Solo 九案按契约交付为规划假设；apply 前核对前置的逐功能运行证据，不修改或替其它工作勾任务。

## Rigor and scope

主要风险是文件写入失败损坏有效记录、部分恢复或提前换关，后果包含进度丢失，因而加强实际文件边界验证。首版只新建且不覆盖，使用受控专属测试目录的真实写入/失败/冲突/幂等 UTF和一次 Launcher 退出重入；纯数据校验覆盖关系与关键持续状态，不建故障平台。复用未变的厨房制作证据，恢复路径必须新增运行结果。接受当前 PC Editor、无覆盖/授权/跨设备/联机领取，未验证任意断电与 Player/AOT；发生实际文件异常或新增平台时补具体验收。

## Acceptance mapping

以下名称是拟补用例范围，不是已经发现或运行的用例；实施时发现真实 PlayMode 过滤并记录原始结果。结果留现有工具原位，不复制证据包。

| 任务 | 功能 | 对应运行 | 结果 |
| --- | --- | --- | --- |
| 1.1–1.2 | 载荷/实际文件失败/幂等 | 拟 SoloSavePayload/Repository | NotRun |
| 2.1–2.3 | 选档/提交门槛/新身份恢复 | 拟 SoloSaveSelection/Commit/Restore | NotRun |
| 3.1 | 真实 UI取消/失败重试 | 实际 UI UTF | NotRun |
| 3.2 | 保存后退出重入/物资与进度 | 普通 Launcher + 相关小集 | NotRun |

## Planning validation

2026-10-12：`openspec validate solo-success-checkpoint --strict` 退出码 0，返回 valid；proposal/specs/design/tasks 齐全。本轮五案共 26 份 Markdown 的 UTF-8、行尾空白、本地引用及任务未勾选检查通过。上述检查仅证明规划格式/引用有效，当时功能运行全部 NotRun。

## 2026-10-13 载荷、文件与恢复 API

供应与装修已分别完成 7/7、6/6，本案使用其实际持续字段。首版为 `CCSOLO01` 包络、4 KiB 上限 MemoryPack Header 与 4 MiB 上限 MemoryPack 载荷；格式 1、平台 PC、revision 1。浏览只读 Header，选择才完整校验。未知版本/平台/Owner、缺集合、重复键、非法引用、容量、地形/出生位、加工参数、供应守恒和装修关系拒绝，不构造 Host。

载荷使用文件内 Item/Station/作业键。未完成手动作业恢复为无 worker 的 Paused；自动作业保留冻结 RequiredTicks/已用 ticks，汤保留原输入关系与剩余份数，烘烤保留输入。已完成手工/烘烤的历史作业不再有持续作用，输出实物独立保存；收货事实仍保留，已丢弃包壳用空实物引用表示。手持物在载荷中归最后提交格，捕获不改实际手槽。供应余额/全部货物、已得装修工位键与待安装奖励一起保存。恢复跳过初始物资路径，映射全新运行 Id，待安装奖励只安装一次。

Repository 是 ProductRoot 的组件与 Ordered MailBox；Browse/Select/Write 经现有 ProcessInnerSender/原等待表答复。稳定 Profile 用本地元数据；记录在 `Solo/PC/<Profile>/<Slot>.ccsave`，仅内部合法 Id 生成文件名。同目录独占暂存、`Flush(true)`、不覆盖 `File.Move`，异常仅删除本次拥有的暂存。Header 重建同结果/Profile 的已提交事实，重开后重试返回原 Slot，不维护第二份领取账本。

| 任务/功能 | 真实 PlayMode 用例 | 最新结果 |
| --- | --- | --- |
| 1.1 零星/手持归格/引用及格式拒绝 | `SoloSavePayloadNaturalZeroStarCapture...`、`InvalidValues...(12 cases)`、`CorruptEnvelope...` | Passed |
| 1.1 冻结手动作业/在途/已预留配额 | `SoloSavePayloadRetainsPausedHeldWorkFrozenTicksAndActualTransitReservation` | Passed |
| 1.1 半锅汤/脏净/已得及待安装装修 | `SoloSavePayloadHalfSoupHygieneAndAppliedRenovationRemainPureValues` | Passed |
| 1.2 实际文件/本地消息/同结果重开幂等/稳定 Profile | `SoloSaveRepositoryRealNewFileRoundTripRetryAfterResponseLossAndStableProfile` | Passed |
| 1.2 冲突/暂存后发布失败/旧字节及他人暂存保护 | `SoloSaveRepositoryConflictAndPublicationFailurePreserveOldBytesAndCleanOnlyOwnTemporary` | Passed |
| 1.2 有界 Header 浏览/损坏完整载荷拒绝 | `SoloSaveRepositoryBrowseReadsOnlyHeaderAndSelectRejectsDamagedPayloadWithoutWorldCreation` | Passed |
| 2.3 实际文件重建新身份/唯一关系/无初始补给/货物及冻结作业/装修/旧命令拒绝 | `SoloSaveRestoreRealFileRebuildsNewIdentityAndRemapsContainersSupplyAndRenovationExactlyOnce` | Passed；产品入口仍待验收 |
| 1.1/2.3 半锅汤及冻结烘烤实际文件往返/重基计时 | `SoloSaveRestoreHalfSoupAndBakingFrozenProgressDoNotRefillOrRecalculate` | Passed |
| 2.1 API 基线预检/新开无假 LoadedSave | `SoloSaveSelectionInvalidBaselineRejectsBeforeHostAndNewGameCreatesNoFakeLoadedSave` | Passed；受管 UI/取消仍 NotRun |

首次相关 19 用例全部 Passed；增加恢复后 22 用例中原 19 Passed、3 Failed。失败是两个重启 fixture 未重置 RuntimeInstanceContext，以及烘烤 fixture 误认原面包已在烤箱；已按真实新轮上下文及实际拿放修正。新恢复小集 2/2 Passed，选档 API 单例新运行 Passed；唯一用例最新结果共 22 Passed，无 Skipped/空集。失败没有续跑/改写；原输出留 Pipeline 固定位置和 Editor 原日志，后续测试正常更新 Pipeline 最新状态。

代表性真实文件失败验证覆盖本案只新建、不覆盖及重试路径；任意断电、磁盘完全耗尽、Player/AOT 和跨设备未验证，发生实际文件异常或新增平台时补对应验收。当时普通 Launcher 仍为内存入口；持久门槛与受管选择、退出重入验收未完成，未报告检查点产品完成。

## 持久门槛与结果 owner

当前协议 16，增加 SaveAndContinue 控制及独立保存状态投影；仅绑定保存模式启用持久门槛，未绑定 fixture 保留原 Continue。自然 Completed/装修锁定后建立 Match 的 SoloSaveResult，一次捕获编码载荷，失败重试复用原字节和 ResultId/SlotId。写入快速接受不换关；成功事实进入原 HostSession Ordered MailBox，再由 Completed 也运行的 Restaurant 服务步确认，最后执行原 Continue。

实际完整路径：受管 HUD → ClientSession 的 LevelCommandRequest(SaveAndContinue) → LocalMessage.Peer ActorId / LevelPeerHandler → HostSession ActorId / LevelSubmitHandler →控制队列→ Restaurant 服务步 / SoloSaveResult →结果 AsyncOwner / root ProcessInnerSender.Call(repository ActorId) → ProductRoot 的 SoloSaveWriteHandler →原 RPC 等待表答复并 Dispose →结果 owner.Call(authority ActorId) / SoloSaveCompletedHandler →复制到 SoloSaveBinding 完成队列→服务步核对当前 Result/World/Level 并确认持久→原 Continue 预检/安装→独立快照。UI 关闭仅停止显示等待，整体 Stop 才停止结果 owner 和共用 sender；晚到或不同结果不安装新关。

| 任务/功能 | 新运行 | 结果 |
| --- | --- | --- |
| 2.2 零星/快速接受无提前安装/提交后唯一换关/最新重放 | `SoloSaveCommitZeroStarRequiresDurableFactBeforeSingleNextLevelAndReplayReturnsSameResult` | Passed |
| 2.2 文件失败保留原结算/不可变字节/同结果重试 | `SoloSaveCommitFailureRetainsSameImmutableResultAndRetryPublishesOneFile` | Passed |
| 2.2 Preparing/Operating 不生成结果，营业退出无成功文件 | `SoloSaveCommitPreparingOperatingExitAndUnsettledRenovationDoNotCreateSuccessFiles` | Passed |
| 2.2 装修未确定拒绝/旧完成回执拒绝 | `SoloSaveCommitSettlementChoiceMustBeLockedAndLateCompletionCannotEnterDifferentLevel` | Passed |
| 2.2 已发布文件/实际等待表注入丢失回执/原物理回执迟到/重试只一份 | `SoloSaveCommitPublishedFileWithLostWriteReceiptRetriesSameResultWithoutSecondRecord` | Passed |
| 2.2 / 3.1 实际 Launcher 指针双击只发一次、立即关闭 HUD 后继续提交、池重开读同事实 | `SoloRestaurantUiTests.SoloSaveUiOneClickCloseAndReopenKeepsResultOwnedCommit` | Passed |
| 2.3 恢复后的在途→Ready 不造物、满收货台拒绝、恢复后正常收货与配额守恒 | 扩展 `SoloSaveRestoreRealFileRebuildsNewIdentityAndRemapsContainersSupplyAndRenovationExactlyOnce` | Passed |
| 2.3 已获得装修1 + 待安装装修2恢复为18工位、脏碗保留 | 扩展 `SoloSaveRestoreHalfSoupAndBakingFrozenProgressDoNotRefillOrRecalculate` | Passed |

UI 沿用原受管 UIGridKitchen/模板与 CodeBind，没有另加根面板或改变 UI 表/Prefab。只有保存模式显示“保存并继续”、写入中/失败/已保存反馈；写入只归结果 owner。上述实际 UI 用例在专属临时 Profile 绑定模式下跑真实 Launcher，证明保存/HUD 生命周期，不能冒充普通用户启动选档或真正退出重入。

5项 SoloSaveCommit 最新有效结果全部 Passed；实际保存 UI 单例 Passed。恢复扩展首次2项中1 Passed/1 Failed（测试误认交接台默认满槽），按实际拿放设置满槽后，该例先因断言名称 `ReceivingOccupied` 与现有 `ReceivingFull` 不符失败，再修正并新建单例运行 Passed。失败均未改写/续跑。当前保存相关唯一用例的最新有效结果共28 Passed，复用未改变行为的对应结果，不扩大全仓。普通 Launcher 选档/取消和保存后退出重入仍 NotRun，任务2.1、3.1、3.2保持未勾选，检查点仍未整体交付。

受 Continue 入口影响的 `SoloLevelControlHasOneBusinessClockAndResumeUsesLocalMailbox` 重新发现并实际执行，1/1 Passed，证明未绑定内存模式仍保留原单钟/本地控制合同。前一次过滤误写成 `SoloRestaurantLevelControl...`，工具返回空集 NotRun；该次不算通过，未运行任何测试。当前 change strict valid，相关 diff 无行尾空白错误；Editor ready、Play stopped，未提交/归档。

## 受管入口与普通 Launcher 退出重入

实际入口改为 ProductRoot 的 SoloSavePresentation / UIComponent，在选档前不创建 Host 或 Client 世界。复用原 UIGridKitchen 注册、Prefab、CodeBind 和模板；新开绑定稳定本机 Profile，内存试玩不绑定档，只有选中记录完整校验通过才建树。UI 等待归原表单，取消关闭该 owner 并重开选择，Epoch/EntityRef拒绝旧响应和按钮。没有新增 UI 表或生成绑定字段。

| 任务/功能 | 实际运行 | 结果 |
| --- | --- | --- |
| 2.1 / 3.1 浏览三份 Header 无世界、有效读取途中取消、旧按钮拒绝、未知版本/损坏文件拒绝、有效选择恢复 | `SoloSaveSelectionUiBrowsesWithoutWorldCancelsValidLoadRejectsInvalidAndRestoresSelectedFile` | Passed；提示文本后续有中文断言小集 |
| 3.1 真实文件失败反馈、关闭重开保留失败、同结果重试、双击只一请求、关闭显示不取消结果 | 扩展 `SoloSaveUiOneClickCloseAndReopenKeepsResultOwnedCommit` | Passed |
| 3.2 启动加载途中退出无孤儿表单 | `SoloRestaurantUiLifecycleExitDuringActualInitialLoadDoesNotShowOrphanHud` | 修复后新运行 Passed |

保存 UI 初次在新入口下失败，原因是旧测试再次 Add 已由选择入口建立的 repository；改为切换专属测试目录后通过。选择取消首次失败于误要求同帧 Close/Open 必须复用同一个 Mono；GF 释放可能延后一帧，修正为真实合同断言：原 EntityRef 退役、当前唯一受管表单、旧按钮不启动世界。新运行通过，未放宽加载取消或世界创建断言。退出回归发现真正缺陷：晚到物理 Close 时 IScene 已释放，旧 OnClose 仍直接访问 Scene。已加空场景保护，并新建单例运行 Passed；没有用 LogAssert 忽略错误。

普通 Launcher 在保存模式从真实 Pointer 开始新游戏。`SoloCheckpointAcceptance.NewGame` 新开世界 `2ce62857233f4c1bbfd8f1a4bee837bb` / Level `1284095988465692`；Profile 为 `82a5f5085f71444087133b2aaeedb02f`。真实备料/煮制/盛一份后半锅剩1份，冻结煮汤80 ticks；有限鸡蛋供应收货/拆份、原空碗加蛋后开始打蛋并停手，进度3/60。自然营业结束及前厅收口达到0星，选择装修后“保存并继续”完成独立记录：Slot `8118cfdb94c1492ca54555903055fcc0`，Result `9b8bd6ca3413481da18f2e56fcc25c08`，文件11物品、手动作业3/60、番茄在途523/1600、半锅1份，进入下一 Preparing。

普通运行的方法只通过实际 HUD Pointer 发业务请求、读取独立投影及选中真实文件；没有改厨房状态。`HalfSoup` detached job `9e8585ee5ce64f13b4aca214dbdaabaa` completed/success（19769 ms）；`CompleteAndSave` job `a6cecf6987214ff7bc2ab08fba4d4322` completed/success（46545 ms），均确认终态后才继续。长运输是源 SupplyConfig H4 的代表性1600参数，修改与恢复均正常 Check 5/5 + Export 5/5、四语言成功；停止本轮 Play 后已恢复40、同步资源刷新。重新普通 Play 的 `InspectSelector` 实际读取源40，确认同一稳定 Profile/真实 Slot 且没有 Host。

真正 Stop Play、重新普通 Play 后，实际点击该记录恢复新世界 `a3b3b1fe67ef4c89af58de0890fdfd3f` / Generation `647972120990580759` / Level `1284096225443879`。`RestoreLatest` 成功：11物品、17工位、1份汤、3/60暂停加工、货物531/1600，余额保持原值，装修只一次，下一 Preparing；默认源40没有重算已保存货物的冻结1600。未向档内注入初始物资，Slot仍为原记录。普通成功文件保留为正常产品记录，不另复制证据包。

实际 540×960 选择与恢复 GameView 已查看：`Unity/Assets/Temp/pipeline-screenshots/solo-checkpoint-selection.png`、`solo-checkpoint-restored.png`。第一次紧接 Play 的抓图因域重载连接重置失败，待真实入口就绪后重新抓图成功；没有因该观察失败重启 Play 或重复业务命令。恢复后原生 Console Error=0、compilationFailed=false。Player/AOT、任意断电、跨设备/授权及联机存档仍 NotRun，既有密集 Board 标签限制保留。

## 最终核对

加载途中退出的新运行 Passed；选择中文拒绝提示及完整选择单例在晚到关闭保护修复后再运行 Passed。保存相关29个唯一用例最新有效结果 Passed；单钟/内存控制及初始加载退出两个相关回归 Passed。上述结果按各功能复用，没有因文档/核对而扩大全仓。七项任务均有本文件的实际映射；原生失败、NotRun 与新运行结果分别保留，不能把原失败改作通过。

按 openspec-verify-change 对当前六个 ADDED Requirements / 六个 Scenario 核对：轻量身份/选择对应 Repository/Browse/Select 和真实选择 UTF；成功来源对应 Capture/Commit 阶段校验及零星/退出 UTF；提交后换关对应结果 owner、HostSession 完成消息、服务步门槛及失败重试 UTF；最多一次对应 Header 事实重建/不覆盖发布/丢回执重试；持续载荷对应纯值校验及半锅/冻结加工/供应/装修文件往返；新运行恢复对应独立恢复路径、旧请求拒绝及普通 Launcher Stop/Play/选档。任务7/7，规格与运行范围一致；ET owner、两段控制请求、结果观察者和受管 UI 与设计树/消息路径相符，无未处理关键差异。

本案 PC Editor 范围实现与验收完成，未归档、未提交；明确不支持的覆盖/跨设备/授权/联机领取，以及 Player/AOT/任意断电 NotRun 不改为已支持。后续新平台或真实文件异常触发对应补验；未来产品本地消息边界 change 是另案规划，不由本案代替实施。

## 2026-10-13 本地提交收尾

用户确认授权后，本案实现随本地main提交906829ef保存，原验收记录随4bfe1a2c保存；此前“未提交”描述保留其当时阶段语义。源码与行为未再改变，逐功能UTF／普通Launcher结果直接复用。本轮五案均已实际验收；归档和push未执行。
