# 执行说明

## 规划阶段状态与决策

2026-10-13：用户授权“由浅到深写成 change”，并要求真机测试最后。本案目前仅为规划；所有 tasks 未勾选，未实现业务、未执行 UTF、未启动 Unity/Player、未构建或操作第二工作区。第三关保存完成后返回选档、无装修和第四关；橙汁加工锁壶、完成可搬壶分杯、产量/耗时配表，已获用户确认。具体相关合同以本案 specs/design 为准。

## 相称的严谨程度

测试可信性是实际风险。先保留原行为断言，按变动夹具执行最小 PlayMode 小集，并用代表性条件未满足情况确认等待会失败；不延长所有超时。 所选验收按下表覆盖本次改变的行为，已有结果只有在实现/行为未变且明确对应该点时才复用，并填写原执行记录路径。不因提案完成或阶段切换重跑全仓；测试失败、空集、Skipped、缺输出均不算成功。

接受的未验证范围：本轮所有运行行为 NotRun；除最后设备层外，手机/独立 Player/LAN 仍待对应案。没有玩家、设备或已授权运行条件时保留相关任务未完成。若改到共享 owner、文件格式、协议、行为断言或出现实际回归，则补对应局部验证，不机械放大为完整矩阵。

## 规划阶段的任务与验收映射

| 任务 | 功能／交付 | 拟使用的最小验收 | 本轮结果 |
| --- | --- | --- | --- |
| 1.1 | 等待与配置辅助 | 用受影响真实消息用例证明只泵消息不前进业务时间；代表性缺条件等待须明确失败，记录辅助函数约定。 | NotRun（规划阶段） |
| 2.1 | 迁移夹具 | 逐个运行被迁移行为对应 PlayMode 用例，保留原断言/Console 检查及独立资源收尾，记录被替换重复点。 | NotRun（规划阶段） |
| 3.1 | 夹具集成 | 发现后只跑受影响合集，核对成功、超时和部分初始化失败清理；不覆写旧测试输出。 | NotRun（规划阶段） |

UTF 执行阶段使用现有 Tools/AutoTesting，先发现模式与方法再记录实际过滤范围；涉及产品消息/UI 的候选集按 PlayMode 核对，目录名不能证明模式。原始输出保留原位，只在此引用运行路径及功能对应关系。纯正文任务只检查文本、链接与严格格式，不制造占位 UTF。

## 实施时回填

每项记录“实际方法或真实操作 → 原输出路径 → Passed/Failed/NotRun/Skipped → 未覆盖点”。文档格式验证只证明规划可解析，不证明功能已完成。当前不预填执行日期、通过计数或不存在的测试结果。

## 规划阶段检查

2026-10-13：`openspec validate stabilize-product-test-fixtures --strict --no-interactive` 退出码 0，规划格式通过。当前相关文档检查共 80 份 Markdown，链接缺失 0、编码/尾随空格/原规格场景遗漏 0；新案依赖无环。以上均为静态规划检查，表内功能仍全部 NotRun，任务未勾选。

## 2026-10-13 实施与实际验收

用户授权三个基础设施 change 后开始本案。修改限定既有 ProductSessionTests 和 Solo/Coop partial；没有生产行为、配置表、UTF 框架或编排协议变化。历史问题来源：供应记录中的不兼容容器假设、合作核心记录中 Finish 推进时间导致提前提交，以及选档记录中重复 repository／旧 UI 身份假设；原失败保持原位。

提取 PumpUntil／FinishMessages：默认只泵消息，真实网络明确推进传输计时；缺条件在原预算内失败并报告阶段。供应／装修接收辅助使用消息等待，CoopRemote 等待 Join 与正确世界／关卡的完整投影，不固定多等帧数。供应来源按实际表查唯一项；两个保存 UI 用例共用受控 repository／随机专属目录登记及所属清理。原领域断言和 Console 检查未减少。

| 任务／行为 | 实际 UTF 或检查 | 结果 |
| --- | --- | --- |
| 1.1 接收与业务提交分离 | ProductFixtureMessageReceiptDoesNotAdvanceTimeOrCommitSupply：真实供应请求接收后钟／Tick／库存不变，明确推进一步后才扣一次库存并建货物。 | Passed |
| 1.1 等待失败与配置缺项 | ProductFixtureMissingConditionFailsWithStageWithoutAdvancingTime：缺条件产生 AssertionException 和阶段信息，不推进钟；缺材料定义明确失败。 | Passed |
| 3.1 部分初始化与重复清理 | ProductFixturePartialInitializationCleanupReleasesOwnedWorldAndFiber：只建 Fiber／根组件后清理，原 EntityRef 退役，单例与线程 Fiber 释放，重复清理不出错。 | Passed |
| 2.1 供应接收／守恒／退出 | SoloSupplyPurchaseRealMailboxReservesOnceAndRejectsShortageWithoutPartialState、SoloSupplyReceiveFullSlotKeepsCargoThenCreatesOnePackageAtMovedStation 及同族其余迁移用例：接收不等于执行，原库存、包装身份、满槽拒绝、暂停与跨关断言保留。 | Passed |
| 2.1 装修接收／重放／原子安装 | SoloRenovationChoiceZeroStarsLatestReplayLockAndOldLevelUseRealMailboxes、SoloRenovationInstallKeepsContainerManualIdentityAndOnlyAddsOneStationOnReplay、SoloRenovationLayoutLateGroundConflictRejectsWholeContinueWithoutPartialInstall 及同族用例。 | Passed |
| 2.1 保存／选档独占目录与实际 UI | SoloSaveUiOneClickCloseAndReopenKeepsResultOwnedCommit、SoloSaveSelectionUiBrowsesWithoutWorldCancelsValidLoadRejectsInvalidAndRestoresSelectedFile；同族实际文件、失败重试与恢复用例覆盖复用的装修接收辅助。原旧按钮、加载取消、文件不覆盖与结果 owner 断言保留。 | Passed |
| 2.1 合作 Join／投影条件与输入 | CoopCoreOwnershipRemoteHasOnlyIndependentClientAndSameWorld、CoopCoreProjectionSnapshotBeforeOwnConfirmationCopiesValuesButCannotOpenInput、CoopCoreCookingRealLocalAndKcpCompleteSoupToastAndIndependentProjection 及其他 CoopRemote 消费者；真实 KCP、越权、旧关、单成员退出及受管重开断言保留。 | Passed |
| 3.1 发现／原始结果／Console／所属清理 | 下述五个明确 PlayMode 过滤运行均终态 Passed，Console 覆盖完整；缺条件失败、部分初始化和重复清理由三个 ProductFixture 行为用例覆盖。 | Passed |

当前 Editor 精确工程 D:/MyWork/CarzyCooker/Unity、PID 44196。recompile_status completed／failed=false／compilationFailed=false，原生 Console Error=0。Tools/AutoTesting 发现 PlayMode 360 项；先执行 filter=ProductFixture，3/3 Passed、退出 0、Console 覆盖完整。原始作业结果仍在 Unity/Temp/pipeline_test_status.json，Editor 原日志保留原目录，不复制或改写失败结果。

本案的主要风险是等待辅助误推进业务或清理错误目录；新增三个行为用例覆盖这些阶段和原部分初始化边界，迁移用例仍须实际运行。UTF 结果只证明夹具合同；生产玩法未变，普通入口历史结果按原范围复用，不扩展 Player／设备／公网验收。

### 本轮终态与收尾

Tools/AutoTesting 分别执行 PlayMode testName 过滤：ProductFixture 3/3、ET.ProductSessionTests.SoloSupply 16/16、ET.ProductSessionTests.SoloRenovation 14/14、SoloSave 29/29、CoopCore 23/23。共 85 个唯一用例均 Passed，每次退出码 0、无 Skipped／空集／新增 Console Error，已等待测试终态与 Player 停止。上述家族覆盖所有迁移的供应／装修接收、保存和选择 UI、CoopRemote 消费者，不扩大到全仓。未改变生产业务，不为资料阶段重复验收原普通流程。

新用例和迁移用例没有失败；代表性缺条件以 Assert.Throws 验证明确失败合同，不将真实失败吞成通过。旧案原失败与 NotRun 保留。

文档收尾检查：本案四份 Markdown 和 AutoTesting README 共 5 份、19 个本地引用均存在、UTF-8 有效；git diff --check 退出 0；openspec validate stabilize-product-test-fixtures --strict --no-interactive 返回 valid、退出 0。最后 editor_status ready／compiling=false／playMode=stopped。三项任务实施及验收完成，没有提交、归档、push 或第二工作区操作。


## 2026-10-13 归档记录

用户明确授权归档、commit、push。任务与既有运行证据复核完整，归档到当前日期目录；先同步消息案的network/product-session两份主规格，再由CLI归档，资料/夹具两案按skip_specs不增加规格。已同步要求逐块比较一致，旧场景和其他要求保留；迁移相对引用与入链，不修改业务代码或原始运行输出。已有行为未变，UTF与普通Launcher按原映射复用，本次仅做格式/引用/规格检查；Player/AOT、手机与LAN等未验范围仍保留。此前“未提交/未归档”描述均按原阶段解释，最终版本状态查Git。
