# 执行说明

## 归档记录

2026-10-21：用户明确授权归档、提交和push。两案均5/5完成，13条delta要求已同步3份主规格，52份主规格严格校验通过；原任务与运行证据逐项对应，复用本轮已通过且行为未变的UTF／Launcher结果，不为归档重跑。独立Player、真机与物理LAN仍NotRun，归档不扩大验证结论。当前目录为 `openspec/changes/archive/2026-10-21-development-test-level-entry`；本轮提交只包含这两案所需内容，Android／LAN其他未完成改动和用户现场保留在工作区。

## 实施验收状态（归档前）

2026-10-21：用户授权实施两案。本案 5/5 已实施并完成当前 Editor 范围内验收，独立开发／非开发 Player 单列 NotRun。没有改源表、生成配置、Prefab 结构或 CodeBind；未构建、提交、推送、同步主规格或归档。首页、章节选择及测试导航继续使用原 UI1101 owner。

## 严谨程度与验证取舍

主要风险是测试误写正式进度、指定中间关缺前置，以及重开残留旧世界。采用当前目录每项实际入场、真实消息操作与拒绝、真实正式文件和领取状态断言、加工中重开及普通 Launcher 串联，足以覆盖这些后果。不建立新框架、状态快照文件、证据副本或全仓测试门槛。

未变的配方和完整关卡平衡不逐项重跑；目录逐项验证真实配置基线，另从 UI 对正式第二小关执行准备、营业、加工，独立咖啡场景执行有限采购及研磨。持久化结算用例只缩短测试所属运行的营业时长并延后顾客到来，仍用真实加成选择、Start、自然收尾、结算和 Continue；不证明正常时长或通关平衡。重开用例只延长本轮已有烘烤以保留待完成作业，没有生产调试接口或源表改动。

开发／非开发独立 Player、Android 真机、物理 LAN 为本轮范围外 NotRun。门禁 UTF 只证明 Editor／开发／非开发布尔分支及实际初始化拒绝；不冒充包内运行。后续交付相应包体、变更裁剪／启动参数，或出现包内门禁异常时补包体验收。

## 功能验收映射

全部用例均由现有 Tools/AutoTesting 发现后按 PlayMode 精确过滤执行。下表用例全名的类前缀分别为 ET.ProductSessionTests 或 ET.SoloRestaurantUiTests。

| 任务／功能 | 实际 UTF／操作 | 运行结果 |
| --- | --- | --- |
| 1.1 环境、角色、有限目录和初始化拒绝 | DevelopmentTestEnvironmentRoleUnknownTargetAndNormalIntentStayDistinct：Editor／开发允许、非开发及 Host／Client／legacy 拒绝；记忆值不等于测试意图；移除内存目录的咖啡首关后显示失效并拒绝原 ID，finally 恢复；未知目标初始化不建 Host／Client | Passed |
| 1.1 目录无世界、选择记忆、失败返回 | 以下三个 DevelopmentTestUi 用例：目录中无 Host／Client；返回后保留 1102／1401；实际非法目标初始化退回目录并显示 UnknownDevelopmentTestLevel，无默认替代世界 | Passed |
| 1.2 每项指定起点、布局、物资、许可及原裁决 | DevelopmentTestEveryExposedLevelStartsExactReadyBaselineAndUsesRealGridArbitration：遍历实际全部 18 条，断言目标、准备阶段、生产许可、全部工位布局、初始实物、各供应源有限包数、无 NetComponent／监听、无保存绑定与资格；每项真实取碗成功且总物品不增，再取空台按原规则拒绝 | Passed，最新含布局与库存断言的新运行 2/2 |
| 1.3 正式保存／领取／授权隔离、成功／继续／终关／退出 | DevelopmentTestUiSuccessContinueTerminalExitPreserveFormalRecordAndTicketThenRestoreOldSave：建立真实记录和签名领取凭证；测试根拒绝保存绑定、Commit、实际本地 Claim 与 ExportImport 请求；真实结算后 101→102，另开1103完成并返回；原文件集合、内容、ResultId 及领取状态不变；回首页明确继续原记录，恢复原库存后取碗 | Passed；HUD 和结算持续显示测试，SaveEnabled=false |
| 1.4 有订单／手持／加工的重开、双击、旧回调与再操作 | DevelopmentTestUiOrdinaryLauncherRestartPendingWorkRetireCallbacksAndReenterIndependentMenu：普通 Launcher 选1102、真实加成／营业／顾客订单／烘烤／手持碗；重开双击与旧回调不能复活旧运行；新WorldId、Preparing、空手、无加工／订单／分数，再取物只发一次；返回目录再开1401，真实采购有限咖啡豆并研磨，回首页后正式新开1101及原取物可用 | Passed，540×960，最大字号实际触达 |
| 1.4 加载中退出与迟到视图 | DevelopmentTestUiExitDuringActualHudLoadAndInvalidInitializationReturnWithoutOrphanViews：实际 HudLoading 时退出、旧 owner 释放且无孤立 HUD；旧开局回调无效；无效目标返回后可重新开101并取物 | Passed，目录前置校验改动后单项补验1/1 |
| 2.1 普通入口及代表视口整合 | 三项 DevelopmentTestUi 均从原 Launcher 开始，使用 EventSystem 射线和真实滚动／点击，指定非首关、独立咖啡、重开／返回、原新开与旧档继续均有运行证据；入口布局补验复用同轮 EntryFlowClarity 的540×960五项与1920×1080章节单项 | Passed；未扩大全仓或重跑完整制作链 |
| 独立包门禁与运行 | 需要后续构建／包体验收授权 | NotRun，非本轮范围 |

开放条目：正式章节「家常开张／进阶小馆／蒸茶小馆」各三小关，共九项；功能「基础操作／饮品基础／沙拉制作／经营成长／热餐制作／甜品制作／咖啡制作／蒸食制作／茶饮制作」九项。不是只核对固定总数：UTF 运行时遍历当前 Entries 并逐项断言、输出实际名称和 ID。经营成长保留原部分解锁；正式指定章节小关具备菜单所需许可，不补历史库存或装修。

## 运行与修正记录

- 目录初次2项运行1 Passed、1 Failed：原经营成长部分解锁被新检查误判。修正为正式章节要求全许可，独立菜单按原初始解锁；其后两次2/2 Passed，最新增加布局、实物、供应数量及目录缺项断言。
- UI初次失败包含夹具名称与重复仓库错误，已改用章表名称、替换测试所属仓库。运行又发现分数刷新覆盖测试标识，改为同步 SetScore 更新，设置／声音同样保留标识。
- 重开后咖啡操作曾因旧测试助手未展开动作菜单失败，改用真实 ReadableCommand／射线、原采购和研磨操作。随后 DevelopmentTestUi 3/3 Passed；最终目录前置校验后仅对实际失效返回／加载退出单项补验1/1 Passed。已有隔离和重开证据仍适用，没有为文档整理重跑套件。
- 新增代码的 ET FriendOf、EnableClass、EntityRef 和测试导入诊断已修正，Unity recompile_status 最终 Passed。测试原失败结果未改写；原始 Pipeline 输出保留在 Unity/Temp/pipeline_test_status.json，历史在原 Editor 日志；未清 Console，Passed 运行 Console 覆盖完整。
- 本案只读现有 Luban 数据，确认 active ET 与原五目标，不需要 Check／gen；没有新增 UI 类型、CodeBind 或配置生成物。原 owner 树及双向消息路径见 design.md，整体 Init 交接不会局部清字典冒充重开。

## 规划与收口检查

- `openspec validate product-entry-flow-clarity --strict`、`openspec validate development-test-level-entry --strict`：均Passed；apply进度分别5/5、all_done。
- 两案23处相对文件引用全部存在，相关diff空白检查Passed；已按openspec-verify-change核对任务完整性、规格场景与原owner／消息路径的一致性，运行过的范围未发现阻断问题。
- 独立开发／非开发Player、Android真机、物理LAN未验证；属于已列明范围外限制，不据此声称包内验收或归档就绪。没有同步主规格或归档。
- 原Launcher的540×960实际画面复查通过；临时屏幕图仅用于本轮查看，已清理本轮两张Assets临时图，其他图与现场文件保留。编辑器已停止本轮Play，Init／FiberManager均释放，Game视口恢复原index6。
