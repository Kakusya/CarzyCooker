# Execution Notes

## 实施前交接（2026-10-15）

00–06已经交付，06共19个不同相关UTF最新有效结果Passed，主规格46/46严格检查通过；本案从实际文件v5/wire26接入，不再把“只有直接来源”或“结果领取待实现”的规划历史当作现状。

本案覆盖文件，主要风险是错误目标/Owner、重复消费及失败毁旧。沿原Root有序repository、原同目录暂存和系统原子替换入口实施；只在隔离测试目录用真实文件验证权限/修订冲突、锁定导致发布失败、重开/丢答复和后续替换，普通受管UI验证显式确认及取消。无需新服务、通用事务框架或设备构建。

必要格式交接：v6把消费事实表示为带用途、消费Profile、目标Owner/Slot/revision及结果/Grant身份的轻量回执，保留v1–v5只读适配。替换后重复请求返回原回执元数据，不能伪造已经不存在的旧载荷；后续覆盖/检查点继承事实，同一次目标发布即可恢复，不另写消费证据副本。wire从当前26演进为27，内部文件请求不开放远端。

ImportBranch与ReplaceGrant用途分离；导入只新建本机独立分支，不从外部授权包继承另一设备的本端消费事实。覆盖只解析本设备PC库内稳定Owner/Slot，须存在对应有效目标并有目标Owner签名、领取者公钥绑定、允许的来源根范围和预期revision，不能指定任意路径。目标Owner不同仍保留其归属；未找到本端目标明确拒绝。有限时效和本端拒绝记录不影响已导入分支或别设备副本。

接受不验证：任意断电、磁盘物理损坏、用户外部手改/删除文件、中心撤销/跨设备全局一次、真机/物理LAN/Player/AOT。出现本应用可复现发布破坏或重复领取则修复并补对应小集，不能以接受边界掩盖故障。

### 当前运行

- v6回执接入后，原 `CooperativeClaimAtomicFailureRetryResponseLossAndLaterCheckpointReturnOriginalRecord` 1/1 Passed，`Unity/Temp/SaveAuthorization-receipt-v6-UTF.log`；真实发布失败、重开和后续记录查回原领取仍成立。
- `SaveAuthorizationFiles` PlayMode 4/4 Passed，`Unity/Temp/SaveAuthorization-files-UTF.log`。`SaveAuthorizationImportCreatesIndependentOwnerLineageAndContinuesAfterSourceOwnerGoesOffline` 验证新Owner/Slot、直接与根来源、重复导入返原记录、源字节不变；释放源Owner后，用新Profile真实进入餐厅并保存下一检查点，原源档仍不变。
- `SaveAuthorizationReadRunExpiryLocalRejectionPlatformAndSignatureNeverCreateBranch` 覆盖用途隔离、过期、本端拒绝、平台标记及篡改签名，失败无新档/临时残留。`SaveAuthorizationForeignOwnerAtomicReplaceFailureRetryAndLaterReplaceRetainOriginalReceiptWithoutBackup` 在允许读取但禁止删除的真实文件锁下使发布点失败，旧字节/资格保留；解除后替换成功保持另一Owner，第二次合法替换后原领取仍返原Slot/修订，无备份或临时文件。
- `SaveAuthorizationReplacementScopeRevisionAndStaleConfirmationRejectWithoutTouchingTargetOrEligibility` 覆盖接收Profile/公钥、允许根、预期revision及旧确认；另一真实合法提交改动目标后，原确认被RevisionConflict拒绝，目标新字节和原未领资格保持。该组为文件层结果，界面与普通入口结果见下文。

### 普通入口与变更回归（实际完成）

- `SaveAuthorizationUi` PlayMode 2/2 Passed，原输出 `Unity/Temp/SaveAuthorization-UI-UTF.log`。导入用例从普通Launcher模式页进入，查看并主动信任源身份，取消不创建档，旧确认无效；重新确认导入后释放源Owner，从原单机选档创建下一关并执行实际拿碗命令，源字节保持。还从新Owner界面导出自己的分支许可。浏览阶段没有Client或世界。
- 替换UI用例呈现完整Owner/Slot/revision及不可恢复含义；取消无写。另一合法提交改变目标后旧确认被拒；真实文件锁导致发布失败仍保旧，重新查看并确认后成功。目标Owner保持、源不变、无备份/临时残留；旧确认和关闭窗口后的回调不能再写。测试只使用Launcher所属的隔离目录；签名结果夹具用于验证文件/UI，不将其描述成新一轮合作经营验收。
- 实际27成员v5旧Header兼容 `SaveAuthorizationReadsActualV5ReceiptWithoutRewritingAndDuplicateClaimReturnsOriginalMetadata` 1/1 Passed，`Unity/Temp/SaveAuthorization-v5-compatibility-UTF.log`：适配旧回执、重复返回原元数据、不改原文件。v1–v4沿用06未变的显式旧布局适配与原运行记录。
- 受v6回执答复改变影响的 `CooperativeClaimsRealKcpWriteFailureDoesNotAckAndHostSaveFailureDoesNotAdvanceWhileRemoteDoesNotClaim` 1/1 Passed，`Unity/Temp/SaveAuthorization-v6-host-save-UTF.log`。连同上文原子新建回归，合计7个新增、2个受变回归，共9个不同用例最新有效结果Passed。
- 当前直接Console `groundTruth` 在2026-10-15 07:41:24 UTC为编译成功、原生Error/Warning各0；历史Pipeline累计计数未清理，不将旧累计错误当本次失败。编译过程中测试成员名一次修正后重新编译及发现，功能运行均采用修正后的实际程序集。

| 任务/功能 | 实际运行映射 | 结果 |
| --- | --- | --- |
| 1.1 导入、独立Owner及谱系 | ImportCreatesIndependentOwnerLineage…、ReadRunExpiryLocalRejection…、UIImportCancels…；独立运行并保存下一检查点，源Owner离线/源不变 | Passed |
| 2.1 权限、修订、原子发布及重复 | ForeignOwnerAtomicReplaceFailure…、ReplacementScopeRevision…、UIReplacement…；真实文件锁失败/重试、跨Owner、后续覆盖仍返原修订 | Passed |
| 3.1 摘要、有损确认及旧窗口 | 两项SaveAuthorizationUi；取消、目标改变、失败重试、退役确认、无世界浏览 | Passed |
| 4.1 普通Editor集成及平台边界 | 普通Launcher两项UI，ReadRunExpiryLocalRejection…；原文件字节、Owner与无备份断言 | Passed |

v6与wire27由真实源及原生成器交接，根目录使用/协议/当前进度及主规格同步。本案未构建、未真机/物理LAN、未提交/push/归档，也未操作第二工作区。以下NotRun表及措辞仅为创建时历史，不代表上述已完成结果。

## 2026-10-15 规划优化

本轮仅修订未开工计划，统一已确认范围、真实前置和逐功能验收；以下早期“仅规划/待裁决”措辞属于创建记录，当前交接以本节、design与tasks为准。没有实施业务或执行UTF/Editor/构建，功能结果仍为NotRun。本案严格格式、正文/引用、任务到验收映射检查通过；全批汇总见[路线记录](../../../../Docs/Product/full-version-roadmap.md)。

## 当前状态与范围

2026-10-15 后续交接：用户已授权全部实施，并明确接受[离线授权首版](../../../../Docs/Product/offline-progress-protocol.md)。协议决策前置已满足；本案运行任务仍未完成，以下规划历史不代表当前缺少实施授权。

2026-10-15：用户授权继续扫描业务、按优先级建立 change，明确不考虑真机实测，并确认“按已确认目标，菜品分批扩展”。本案只完成规划，任务未勾选；功能实现、UTF、Unity Editor 运行、改表/导表、构建均 **NotRun**。规划检查结果统一见[本轮路线记录](../../../../Docs/Product/full-version-roadmap.md#本轮规划检查)。

PC LAN 与 Android 原案保留，不是本案依赖。没有提交、push、归档或操作第二工作区；此前授权的美术提交不扩展成本轮业务实施许可。

## 严谨程度与已接受边界

这是会覆盖文件的改动，验证比内容案严格。用独立临时目录真实文件核对成功、权限/修订冲突、写入失败、重复与跨平台拒绝，避免动用户现有档。

只选择实际改变行为的最小相关 PlayMode UTF 与普通 Editor 入口；可复用下列已交付主规格关联的原证据，前提是实现、消费者和验收点均未变。 不建立全目录矩阵、额外状态副本或通用测试平台。

接受未验证：真机、设备输入、Player/AOT、物理 LAN/公网不在此次规划内；全目录平衡、未选择内容批次及跨设备持久故障不由本案证明。 需要相关产品交付时另定范围；本案新增/修改行为或出现具体回归时补相应最小验证，不因整理/审查重复跑旧套件。

## 创建时的未来验收映射（历史）

| 任务/功能 | 最小验证与断言 | 当前结果 |
| --- | --- | --- |
| 1.1 授权导入 | 临时文件 UTF：用途隔离、分支独立、谱系正确、源档不变。 | NotRun |
| 2.1 显式替换提交 | 临时文件 UTF：权限、冲突、提交前后失败与跨新建/覆盖去重。 | NotRun |
| 3.1 确认与重试 UI | 受管 UI UTF：目标明确、每次有效确认、取消/冲突无写入。 | NotRun |
| 4.1 普通入口集成 | 普通 Editor+文件 UTF：导入后继续、显式替换及PC/Android拒绝。 | NotRun |

同一运行可覆盖多行；先发现真实 PlayMode 用例并记录过滤范围，再执行改变行为的小集。任务中写的是验收目标，不是已经存在或已通过的测试。已有通过结果只在实现和验收点不变时复用，缺口补跑；失败、空集、Skipped及缺输出仍未验收。

## 可复用的既有合同

已有运行记录：[cooperative-save-baseline](../2026-10-15-cooperative-save-baseline/execution-notes.md)、[2026-10-14-solo-success-checkpoint](../2026-10-14-solo-success-checkpoint/execution-notes.md)。只复用未变的原行为；本案新增功能不因引用它们而通过。


- [solo-save-progress](../../../specs/solo-save-progress/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。

## 实施交接

离线协议已确认。ImportBranch 与 ReplaceGrant 是独立用途；依赖基线案的身份/验签和领取案的原子回执合同。覆盖测试只使用本案隔离目录，玩家操作每次明确确认目标。

参数按配置和实际消费者核对；运行后回填真实方法、结果、原日志路径和失败原因，不保存额外业务状态副本。


### 文档与格式核对

本案严格检查通过；当前主规格47/47严格检查通过。本案及同步入口12份UTF-8文档、本地引用检查无缺失，git diff --check无空白错误（现有CRLF归一提示保留）。无归档/提交/push。
