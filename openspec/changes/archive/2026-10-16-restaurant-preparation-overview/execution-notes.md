# 探索与执行说明

## 2026-10-16 实施与实际运行

最新收尾：普通Launcher实际主页→Solo内存试玩→确认→准备总览，540×960画面 `Unity/Assets/Temp/entry-preparation-overview.png` 已查看，菜单／条件／建议和返回入口可读可点。浏览保持命令0，原选择加成最终Executed编号1，原开始营业只提交编号2；自然进入Operating后总览选择清空为None。手工终态查看时本关已Completed，未伪报手工仍处于Operating；实时开店阶段和换关由上述UTF断言证明。文档及当前进度已同步。本案相关Scenario分别对应Solo用例（原门槛、建议不强制、暂停／旧回调／换关）和KCP用例（Host及Client权限），不以整组计数掩盖未覆盖点。真人易用性、设备差异仍未验证，未构建／提交／归档。

已在原GridPresentation增加本轮GridOverviewPage.Preparation，显示原投影的菜单、加成条件、开店控制、有效生产许可及库存／未接收采购摘要，链接原菜谱、补货和布局筛选。未增加Ready业务状态或预制菜门槛；原开始营业按钮和请求生成路径不变。阶段／关卡、HUD关闭及切换菜谱／伙伴／目标时清理页面，显示依赖纳入原字段比较，不新增逐帧全状态复制。

PlayMode过滤 `EntryPreparationOverview` 已实际2/2 Passed、Console覆盖完整、AutoTesting退出0。`EntryPreparationOverviewSoloShowsOriginalGatesAndStartsWithoutPreproductionAcrossNextLevel` 覆盖真实必选加成→原选择→无预制菜开店、暂停阅读、三类原入口、池化旧回调、自然结算及真实下一关重新选择；同一用例中未就绪／可选分支临时输入本端投影后立即恢复，仅证明显示分支，不将该夹具当作真实配置或世界状态。`EntryPreparationOverviewActualKcpClientReadsPausedRoomAndHostStartsWithoutPreparedFood` 覆盖真实Host暂停、KCP Client只读不增命令／不授予Host权限、原Host恢复并仅一次开店且远端进入Operating。

原生输出保留 `Unity/Temp/pipeline_test_status.json`，未复制或改写。540×960普通入口实际检查与文档同步尚在收尾，当前任务未勾选。

## 2026-10-16 依据与决定

开店按钮只在 Buff 等门槛满足时出现，准备阶段信息散在菜谱、加成、物资和工位动作中，玩家难以判断为什么还不能开店。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)、[主规格](../../../specs/restaurant-ui/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：只从 LevelPhase、BuffDrawVersion／RequireBuffChoice／SelectedBuffId、当前许可和原投影计算说明；入口调用既有功能，不创建 Ready 状态、持久清单或自动操作。阶段变化即失效，暂停可读但不绕过控制限制。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为把建议性检查误作新增玩法门槛，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | Buff 未就绪／必选／可选、Solo／Host／Client 与暂停／换关；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate restaurant-preparation-overview --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
