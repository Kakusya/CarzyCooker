# Execution Notes

## 2026-10-15 实施中的文件验证

本案6项功能现已取得对应运行结果，以下规划历史保留用于追溯。共19个不同相关UTF（17核心、2普通Launcher受管UI）的最新有效结果Passed；失败与修复记录保留，不把静态规格检查代替运行。

- `CooperativeClaimsFiles` PlayMode UTF：3/3 Passed，原日志 `Unity/Temp/CooperativeClaims-files-UTF.log`。`CooperativeClaimAtomicFailureRetryResponseLossAndLaterCheckpointReturnOriginalRecord` 在真实目标路径放置隔离空目录，造成临时文件完成后发布失败，验证资格保留、无临时文件残留；移除本测试空目录后重试，重开 repository 和再建检查点仍返回原 Slot/revision。
- `CooperativeClaimCloseBeforeTicketKeepsClosedAndAfterClaimPreservesRecord` 验证回执先于 Ticket、旧 Match 回执拒绝、已领取记录保留；`CooperativeClaimRejectsForgedWrongPurposePlatformAndRecipientWithoutFiles` 验证验签与用途、平台、收件人边界，失败不落票或存档。
- 文件格式升为 v5，追加源 Profile、根记录身份、结果/授权身份及领取事实到原记录映射；保留 v1–v4 读取。Header 上界调整为有界 128 KiB，以容纳最多 512 条实际领取映射。业务协议升为 26；所有 Proto 通过原 Proto2CS 入口生成，未手改生成物。
- 最小验证仍以实际文件、同 Editor 独立 Fiber 的真实 KCP、普通 Launcher/受管 UI 为范围；不包含真机、物理 LAN、Player/AOT。文件小集通过后仅补新增的资格、投递、Host 保存门槛和关闭/UI行为。

### 资格、投递和关闭的实际运行

- `CooperativeClaimsRuntime`：3/3 Passed，`Unity/Temp/CooperativeClaims-runtime-UTF.log`。`CooperativeClaimsRealKcpFreezesStartExcludesLateJoinAndRetransmitsStableTicketAfterLostAck` 验证本地及 KCP 已证明身份在 Start 冻结、迟加入不获资格、装修选择前不冻结、稳定结果只生成一次、丢 ACK 重发同票且本端只有一票；退役餐厅后票仍可领取且 Match 未关闭。
- `CooperativeClaimsRealKcpWriteFailureDoesNotAckAndHostSaveFailureDoesNotAdvanceWhileRemoteDoesNotClaim` 用真实目录创建失败证明没有 ACK，恢复存储后相同票重投成功；Host 发布失败保持原关与结算，重试成功只创建一档并前进，远端不领取不阻塞。`CooperativeClaimsAbortedOperatingWorldNeverManufacturesResultOrTicket` 验证营业中中断没有结果或 Ticket。
- `CooperativeClaimsClose`：3/3 Passed，`Unity/Temp/CooperativeClaims-close-UTF.log`。`CooperativeClaimsNormalWholeMatchCloseOnlyAffectsPersistingDeviceAndPreservesClaimedRecord(False/True)` 验证远端不能关闭 Match，一端离线时按实际收到回执处理，本端未领关闭/已领保留、离线端仍可领取；有限关闭阶段没有提前停止 Root sender，关闭期间不能重新开房间餐厅。`CooperativeClaimsReturningProvedProfileGetsSameFrozenTicketOnNewKcpBinding` 验证原成员真实断线 Resume、重新证明同 Profile 后收到原 Ticket，按新绑定单独 ACK，没有新资格。
- 普通 UI 集成首两次失败，日志 `CooperativeClaims-UI-UTF.log`、`CooperativeClaims-UI-reopen-UTF.log`：同 Editor 的两个受管界面切换时，入口存在不可用的旧 UI 组件。已沿原 UIComponent 在重新加载前退役残留组件，不改 Prefab/CodeBind 或创建私有池。后续运行另留日志，失败输出保留。
- `CooperativeClaimsCompletion`：1/1 Passed，`Unity/Temp/CooperativeClaims-terminal-fixed-UTF.log`。`CooperativeClaimsTerminalSaveReturnsSameRoomAndCloseTimeoutDoesNotPretendPersistence` 验证三关自然成功、明确跳过装修仍冻结一次、逐关本机保存、终关完成档及返回同房间；随后阻断本机关闭回执写入，有限4秒阶段仍能结束，但没有该端ACK或虚报回执文件，原已领三档保留。首次 `CooperativeClaims-terminal-UTF.log` 因捕获仍要求装修失败（RenovationRequired），已对齐明确跳过合同并将捕获提前，失败日志保留。
- 资格/结果任务1.1已由上述真实本地/KCP运行覆盖，产品合同已明确“结算选择确定后冻结”；其余任务继续验收，未因局部通过自动勾选。
- 增加当前 Match 的 Host 身份约束后，`CooperativeClaimsFiles` 最新4/4 Passed，日志 `Unity/Temp/CooperativeClaims-files-host-binding-UTF.log`。新增 `CooperativeClaimOtherTrustedHostCannotCloseCurrentHostsMatchEvenWithAValidSignature`：另一已信任 Host 的签名虽有效，仍不能关闭当前 Host 的 Match，回执先到也不抢占该房间；实际 Client 传本连接确认的 Host 公钥身份给私有 Root 请求，验链仍要求主动信任。
- `CooperativeClaimsUiStates` 1/1 Passed，`Unity/Temp/CooperativeClaims-UI-states-UTF.log`。`CooperativeClaimsUiOfflineMenuShowsClaimedAndClosedFactsFromActualFiles` 通过普通 Launcher 模式菜单、真实文件及原受管 HUD，验证没有 Client/Session 时可查询，已领取/关闭分别显示且不会再建档，返回后原模式菜单恢复。
- `CooperativeClaimsCompatibility` 首次1/5 Passed，`Unity/Temp/CooperativeClaims-compatibility-UTF.log`：实际领取后读入 Solo 并提交下一检查点通过，原领取映射、根记录及重复领取 Slot 保留；旧Header四例发现 MemoryPack 把缺失新字段补成 null。已在原 ReadHeader 增加 v1–v4 只读适配。仅重跑四个失败方法实例，`Unity/Temp/CooperativeClaims-legacy-header-fixed-UTF.log` 4/4 Passed；分别使用真正13/18成员旧Header，验证领取数组为空、根锚点有效、载荷可读且原字节不变。真实Solo检查点通过结果未重复运行。
- 普通集成最终 `Unity/Temp/CooperativeClaims-UI-remote-handoff-UTF.log` 1/1 Passed：`CooperativeClaimsUiOrdinaryLauncherBothClaimCancelRetryReloadAndNormalMatchEnd` 从原模式菜单创建房间、双端主动证明身份、自然成功并选择升级开始，双方收票后分别主动领取；真实磁盘失败保资格，取消观察后查询实际已提交档，载入个人档进入下一关且升级恢复，先回同房间再正常结束整个Match。旧领取回调在新世界/新运行不写状态。此前 `UI-owner` 的“打开面板应增加世界命令”断言、`UI-input-fixed` 对已退役按钮重复指针派发、`UI-receipt` 的同Editor共享GF界面交接失败均已纠正；原始失败日志保留。Client视图按既有合作UI夹具退役/重建后接管同一Editor的GF资源，不声称双Editor或设备验收。
- 最终按项目新增规范，将本案新投递类型和Root操作改为具名enum，显式保留原线值及0态，整数边界转换/校验；新代码if补齐块结构，未改变业务合同或协议26。只补两个相关运行：`CooperativeClaims-enum-ticket-UTF.log` 1/1、`CooperativeClaims-enum-close-UTF.log` 1/1 Passed；复用未变的普通UI结果，没有重跑完整套件。
- `CooperativeClaimsPeerAlreadyClaimedHostFailureRetryAndNewRestaurantKeepIndependentFacts` 1/1 Passed，`Unity/Temp/CooperativeClaims-peer-first-UTF.log`：队友先成功领取，Host失败留原结算、重试及精确重放仅前进一步，队友原字节不变；回原房间另开餐厅后签发新结果/票，旧已领事实保留且Match未关闭。这补齐delta中队友已领与原房间再开出新票的具体场景。

## 2026-10-15 规划优化

本轮仅修订未开工计划，统一已确认范围、真实前置和逐功能验收；以下早期“仅规划/待裁决”措辞属于创建记录，当前交接以本节、design与tasks为准。没有实施业务或执行UTF/Editor/构建，功能结果仍为NotRun。本案严格格式、正文/引用、任务到验收映射检查通过；全批汇总见[路线记录](../../../../Docs/Product/full-version-roadmap.md)。

## 当前状态与范围

2026-10-15 后续交接：用户已授权全部实施，并明确接受[离线授权首版](../../../../Docs/Product/offline-progress-protocol.md)。协议决策前置已满足；本案运行任务仍未完成，以下规划历史不代表当前缺少实施授权。

2026-10-15：用户授权继续扫描业务、按优先级建立 change，明确不考虑真机实测，并确认“按已确认目标，菜品分批扩展”。本案只完成规划，任务未勾选；功能实现、UTF、Unity Editor 运行、改表/导表、构建均 **NotRun**。规划检查结果统一见[本轮路线记录](../../../../Docs/Product/full-version-roadmap.md#本轮规划检查)。

PC LAN 与 Android 原案保留，不是本案依赖。没有提交、push、归档或操作第二工作区；此前授权的美术提交不扩展成本轮业务实施许可。

## 严谨程度与已接受边界

主要是误授资格、重复领取或失败吞资格，采用真实本地文件和 KCP 小集覆盖资格/ACK/提交/关闭四条路径，保留失败输出；不建立跨设备资产平台。

只选择实际改变行为的最小相关 PlayMode UTF 与普通 Editor 入口；可复用下列已交付主规格关联的原证据，前提是实现、消费者和验收点均未变。 不建立全目录矩阵、额外状态副本或通用测试平台。

接受未验证：真机、设备输入、Player/AOT、物理 LAN/公网不在此次规划内；全目录平衡、未选择内容批次及跨设备持久故障不由本案证明。 需要相关产品交付时另定范围；本案新增/修改行为或出现具体回归时补相应最小验证，不因整理/审查重复跑旧套件。

## 任务验收映射

| 任务/功能 | 最小验证与断言 | 当前结果 |
| --- | --- | --- |
| 1.1 资格与结果冻结 | Runtime三例、ReturningProvedProfile、TerminalSave：实际Start冻结，迟加入/原身份返回、选择确定后一次签发、未成功不出票。 | Passed；runtime/close/terminal-fixed日志 |
| 2.1 凭证投递与 ACK | Files四例、RuntimeWriteFailure、普通UI双方收票：实际Root验链写盘后ACK，同票重发、失败不ACK，错用途/平台/收件人/Host拒绝。 | Passed；files-host-binding/runtime/enum-ticket/UI-remote-handoff日志 |
| 2.2 正常关闭 | Close三例、CloseBeforeTicket、TerminalSave、PeerAlreadyClaimed：两端观察不同、旧Match/伪造拒绝、已领保留、有界等候、原房间再开新餐厅继续出票。 | Passed；close/terminal-fixed/peer-first/enum-close日志 |
| 3.1 主动领取与 Host 提交 | AtomicFailure、LoadedSoloCheckpoint、RuntimeWriteFailure、PeerAlreadyClaimed：真实发布失败保资格、重开/丢答复幂等、后续检查点保回执、Host失败不换关。 | Passed；files-host-binding/compatibility/legacy-header-fixed/peer-first日志 |
| 3.2 受管领取入口 | 两个普通UI：可领/已领/已关闭、一次点击、失败重试、取消等待后查询真实提交、跨运行旧回调无效。 | Passed；UI-states/UI-remote-handoff日志 |
| 4.1 普通入口集成 | 普通Launcher创建/真实KCP加入，双方证明身份、营业自然收口、各自领取，领取档重新开下一关并恢复合作工位升级；原房间/整个Match区别。终关复用TerminalSave真实三关。 | Passed；UI-remote-handoff/terminal-fixed日志 |

同一运行可覆盖多行；先发现真实 PlayMode 用例并记录过滤范围，再执行改变行为的小集。任务中写的是验收目标，不是已经存在或已通过的测试。已有通过结果只在实现和验收点不变时复用，缺口补跑；失败、空集、Skipped及缺输出仍未验收。

## 可复用的既有合同

已有运行记录：[cooperative-save-baseline](../2026-10-15-cooperative-save-baseline/execution-notes.md)、[cooperative-chapter-completion](../2026-10-15-cooperative-chapter-completion/execution-notes.md)、[2026-10-14-solo-success-checkpoint](../2026-10-14-solo-success-checkpoint/execution-notes.md)。只复用未变的原行为；本案新增功能不因引用它们而通过。


- [solo-save-progress](../../../specs/solo-save-progress/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。
- [restaurant-level-lifecycle](../../../specs/restaurant-level-lifecycle/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。

## 实施交接

离线首版协议已经确认；资格证明和基线接口复用 cooperative-save-baseline 的实际交付，不能把该案文件存在当作验收通过。合作终关与房间已交付，继续沿其原控制合同接入。

参数按配置和实际消费者核对；运行后回填真实方法、结果、原日志路径和失败原因，不保存额外业务状态副本。

## 收口核对

| 维度 | 本案结果 |
| --- | --- |
| 完整性 | 6/6任务；5项新增Requirement均有源码、对应实际运行及文档用法。 |
| 正确性 | 7个delta场景逐项对应上表及原日志；19个不同相关UTF最新有效结果Passed。发布失败、已领队友/未领队友、回同房间再出票、关闭观察差异均有实际断言。 |
| 一致性 | 实际ET树、Peer→HostSession及Root文件邮箱、独立值/DTO回收和原实例复验与设计一致；表/Prefab/CodeBind未新增副本，生成协议走原入口；保留用户其他改动。 |

主规格已智能同步为 `openspec/specs/progress-claims/spec.md`，保留5项需求及7个场景；按当前spec规则快照核对。change严格验证通过，主规格46/46严格验证通过；13份UTF-8文档304个本地引用检查通过。当前Editor编译成功，原生Console当前Error为0，历史失败保留。未发现本案范围内的关键/警告项。

无需重跑实现与行为未变的旧套件。真机、物理LAN、Player/AOT、产品构建及任意断电/磁盘物理损坏未验证，按用户本轮范围及离线协议接受；相关产品交付或出现可复现回归再补相应最小验证。没有commit、push、归档或第二工作区操作。
