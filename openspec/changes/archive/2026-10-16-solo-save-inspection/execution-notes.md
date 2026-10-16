# 探索与执行说明

## 最新交付与逐项验收

| 任务／Scenario | 实际用例或操作 | 最新结果 |
|---|---|---|
| 1.1／1.2 半批次、在途货、排序／筛选／刷新 | `EntrySaveInspectionRichFileRequiresReconfirmAfterRevisionAndRestoresHalfBatchAndShipment`：真实已校验文件，继续／完成分类及时间顺序；读摘要无Host，刷新保留选择，修订2变化先更新详情，重新确认后恢复1/2份及在途货，原文件不变，原拿碗动作可用 | Passed |
| 1.1／1.2 旧有效格式、损坏、取消、旧按钮、完成只读 | `EntrySaveInspectionLegacyFileCorruptionCancellationPoolAndCompletedRecordStayReadOnly`：v1真实文件先校验，损坏后拒绝创建Host且不写回；取消加载池化重开后旧确认无效，完成记录没有继续按钮 | Passed |
| 1.2 原选档流程 | `SoloSaveSelectionUiBrowsesWithoutWorldCancelsValidLoadRejectsInvalidAndRestoresSelectedFile`：未知版本／损坏、取消及原有效文件显式确认恢复 | Passed |
| 2.1 540×960普通入口 | 实际Launcher读取本轮独占临时富载荷文件，查看并滚动摘要，确认后实际Confirmed／关卡2／剩余1份／货物InTransit／命令0 | 通过 |

新用例修复夹具目录后2/2 Passed。实际画面发现新增筛选挤占控制区，已移至原滚动列表；对应富载荷／筛选用例新运行1/1 Passed，旧格式用例行为未变，复用前次结果，不重跑整组。相关运行均Console覆盖完整、AutoTesting退出0，原生输出保留 `Unity/Temp/pipeline_test_status.json`，无副本；域重载期两次preflight连接重置未启动测试，待ready后新运行，不算测试失败或通过。

最终 `Unity/Assets/Temp/entry-save-list.png`、`entry-save-inspection.png` 已查看；按钮和摘要可读，完整列表可滚动，进入后保留实际批次／运输状态。原关卡、升级、扩建及授权恢复夹具只将选档改为真实“查看＋确认”指针操作，原业务API未变。操作说明与当前进度已同步。不做文件锁／hash／索引或协议格式迁移，接受真人体验和设备差异未验证；出现实际文件格式或布局回归时只补相应范围。

普通Launcher手工验收的独占临时目录 `Unity/Temp/entry-inspection-manual/20261016-1792121254203` 清理操作被自动审批审查拒绝，文件原位保留，未改用其他方式删除。此目录不属于产品存档，不影响功能结果。未提交、推送、归档或构建。

## 2026-10-16 实施中与失败记录

已实现原Select只读完整校验、轻量摘要、分类排序、有效选择刷新和明确二次继续；继续重新读取并比较Slot／revision／ResultId／时间／来源等选择身份，变化先更新预览。本阶段任务尚未勾选。

原 `SoloSaveSelectionUi` 受变实际UI回归1/1 Passed、Console完整、退出0。新增 `EntrySaveInspection` 首跑0/2：旧格式损坏用例 `FileNotFoundException` 在SaveInspectionUiTests.cs:150；半批次修订用例等待新修订超时，原运行实际处于Preparing。诊断为两个用例使用应用根临时目录，而repository把真实文件放在其Solo/PC/Profile目录：前者读不到，后者写到未被读取的旁路文件。仅修正夹具使用 repository.DirectoryPath 后创建新运行；不以这些失败证明产品行为通过，不改写原测试输出。原生结果在 `Unity/Temp/pipeline_test_status.json`，重跑使用工具自然新生命周期。

## 2026-10-16 依据与决定

选档列表有星级和时间，但继续记录点击后直接创建世界。现有完整载荷已能解释实物、供应和布局，玩家缺少加载前确认入口。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)、[主规格](../../../specs/solo-save-progress/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：复用 SoloSaveSelectRequest 到 Root repository 的只读请求，响应转独立摘要，不在 UI 长期缓存整份载荷；最终继续重新校验当前文件。保持 v12 与旧有效格式适配，不增加 rename／delete／自动覆盖或额外索引文件。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

本案涉及文件操作或运行退出的玩家判断，主要风险为把预览当成加载成功或对已变化文件继续使用旧确认。保持原发布、权限和生命周期实现，只改变入口或增加只读检查；使用真实临时文件／原控制链及 UI 运行验证，覆盖取消、迟到与失败，不扩大成任意断电或全设备试验。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 真实临时文件预览／变化／损坏／取消与普通选档恢复；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

规划阶段历史记录（不代表当前状态）：本案 proposal／design／specs／tasks 均已齐备，`openspec validate solo-save-inspection --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
