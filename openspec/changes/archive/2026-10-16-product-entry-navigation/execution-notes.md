# 探索与执行说明

## 2026-10-16 批量归档与提交范围

用户明确授权归档并commit。原18案共54/54项任务、36条增补要求和56个场景已逐案核对；合并到10份主规格，保留原Purpose与未涉及内容，无重名要求冲突。归档目录使用统一日期2026-10-16，所有`.openspec.yaml`保留，归档相对引用已调整。主规格验证48/48、全量严格验证72/72通过；功能实现未因归档改变，复用本批适用UTF和普通Launcher证据，不重新运行或扩大为设备验收。

入口代码依赖同工作区此前已完成并归档的product-semantic-enums、menu-relevant-level-buffs、extended-station-upgrades、recipe-guidance-physical-progress；本次提交包含这些必要前置及其原运行说明。动画和PC／Android适配通过共享文件分块暂存排除，后建21案仅保留规划、原始日志／缓存／用户现场不提交。本轮只做本地commit，不推送。接受上述未提交改动仍留工作区；后续对应任务独立提交。

分块暂存后，用现有Unity6000.3.18f1的编译器／Bee响应文件核对Git索引中的实际源内容；未提交的动画和平台源排除，混合文件读取暂存版本，未改工作源。Model、Hotfix、ModelView、HotfixView、Game.ET.Editor和CarzyCooker.ET.Tests六程序集全部退出0。这只证明提交内容的编译依赖，不能替代功能运行；输出留在`Unity/Temp/EntryIndexCompile/20261016-175940/`，不提交。正文／格式／本地引用另核对；`git diff --cached --check`的剩余诊断仅在Luban自动生成C#中，保留生成器输出，不手改生成物；排除生成目录后的检查通过。

## 2026-10-16 依据与决定

模式菜单、合作领取和文件授权共用一个按钮区，单机与房间又直接罗列多种餐厅。已有能力能进入，但缺少按玩家目的组织的稳定导航与返回层级。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryPresentationSystem.cs)、[主规格](../../../specs/product-entry/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：复用 ProductEntryPresentation、SoloSavePresentation 与原 UIGridKitchen 的控制／列表区，以具名页面枚举组织本轮导航。通过现有 ReturnToModeMenu 及进入选档事件切换 owner，不增加长期导航服务或持久化偏好。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为页面 owner 交接时旧按钮误操作新页，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 模式／选档／房间／领取／授权的往返及启动参数入口；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

## 2026-10-16 实施进展

最新验收已补齐：PlayMode `ProductEntryUi` 过滤命中4项，4/4 Passed、Console覆盖完整、AutoTesting退出0。`ProductEntryUiJoinTimeoutReturnsModesAndCanStartSolo` 对应超时后回主页并可单机；`ProductEntryUiOrdinaryClientAddressJoinsRealHostRoomAndOpensOnlyProjection` 对应普通地址加入真实KCP；`ProductEntryUiOrdinaryHostRoomRealKcpReopenAndRetiredWorldCallbacks` 对应Host／远端房间、旧世界回调及新世界；`ProductEntryUiOrdinaryModesRejectInvalidAddressCancelJoinAndStartSoloWithoutListener` 对应非法地址、取消及单机不监听。原生输出 `Unity/Temp/pipeline_test_status.json` 的终态为 completed／total4／passed4，不复制结果包。

任务1.1／1.2的主页→合作→返回→领取→返回→单机与旧页面、HUD重开，对应下述两项新增UTF（2/2 Passed）。显式参数和Editor隔离行为未改，复用[PC案](../../pc-cooperative-lan-trial/execution-notes.md)已记录的26项参数／范围UTF，不重跑。授权页仍交接原 OpenAuthorizationFiles／Claims owner，原文件签发、信任和导入并未随导航重写，复用其既有验收。

任务2.1：普通Launcher在540×960实际点击主页→合作房间→返回→单机→内存试玩，原拿碗命令最终Executed且仅编号1；分组浏览编号保持0。实际画面已查看：`Unity/Assets/Temp/entry-home.png`、`entry-cooperation.png`、`entry-kitchen-actions.png`。按钮可读可点、合作地址字段仅下级显示。已同步[操作说明](../../../../Docs/Product/cooperative-play-guide.md)及[当前进度](../../../../CarzyCooker当前进度.md)。本机结果不覆盖真人体验、目标设备和物理LAN；没有构建、提交或归档。

已在原 Presentation 接入 Home／Cooperation／Chapters／TrialMenus 具名页面；继续记录与原 Solo 选档交接，合作创建／加入集中在下级页，合作房间将内容章和保留菜单分开。每次重绘捕获 renderVersion，旧页面回调不能切换新页；原运行参数和业务请求保持。

PlayMode／EntryExperience 的两个新增实际 Launcher UTF 最新 Passed：`EntryNavigationReturnsFromProgressAndRejectsRetiredPageButtons` 覆盖模式页→合作分组→返回→领取查询→返回→Solo选档及旧按钮隔离；`EntryNavigationRoomSeparatesChaptersAndTrialMenusWithoutStarting` 覆盖真实 Host 房间中两类内容入口、浏览零开局、命令编号不变与池化重开。运行终态2/2 Passed、无新增Console错误，AutoTesting退出0；原生输出在 `Unity/Temp/pipeline_test_status.json`，未复制／改写。本案真实 KCP 受变回归、540×960 实际检查与文档同步仍待完成，不将本记录视为整案验收。

规划阶段历史记录（不代表当前状态）：本案 proposal／design／specs／tasks 均已齐备，`openspec validate product-entry-navigation --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
