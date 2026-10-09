# 自动测试框架与 SOP 迁移

2026-10-09，用户在确认缺口后明确要求迁移。实施 change：[migrate-testing-and-sops](../../openspec/changes/migrate-testing-and-sops/tasks.md)。本轮接入授权已给定，不为同一迁移重复申请；产品构建和玩法运行仍按具体任务授权。

## 交付与边界

| 项目 | 已交付位置 | 能力边界 |
|---|---|---|
| 官方 CLI 技能 | [.agents/skills/unity-cli](../../.agents/skills/unity-cli/SKILL.md) | 现有 CLI 1.0.0-beta.11 安装器生成，未手改；包内 unity-pipeline 未镜像 |
| 自动测试框架 | [Tools/AutoTesting](../../Tools/AutoTesting/README.md) | 实际发现、显式过滤、异步运行、终态、Console 与失败报告；默认只发现 |
| 自动测试 SOP/技能 | [手册](../../Book/自动化测试SOP.md)、[技能](../../.agents/skills/auto-testing-sop/SKILL.md) | 工具已可用；没有产品套件，空集不算通过 |
| 启动 SOP | [Unity启动与验证SOP](../../Book/Unity启动与验证SOP.md) | Launcher/ProcedureET 与真实宏/CodeMode |
| 资源导出 SOP | [ResourceCollection导出SOP](../../Book/ResourceCollection导出SOP.md) | ET 规则、Refresh/Optimize，保留 HybridCLR |
| 打包验收 SOP/技能 | [手册](../../Book/打包与包体验收SOP.md)、[技能](../../.agents/skills/build-acceptance-sop/SKILL.md) | 既有 BuildHelper/BuildToolEditor；专用双包编排未接入 |
| 错误 SOP/技能 | [手册](../../Book/Unity错误诊断SOP.md)、[技能](../../.agents/skills/unity-error-extraction/SKILL.md) | 参考压缩器和当前 Pipeline entries 适配；不扫描历史快照 |
| C# / 提交检查 | [C# 代码规范](../../Book/C%23%20代码规范.md) | GF/ET 模式分别适用，不批量修改存量 |
| OpenSpec 完成归档 | [手册](../../Book/OpenSpec完成与归档SOP.md) | 官方 verify/sync/archive；不新增专用校验脚本 |
| 端口 SOP/技能 | [正文](../../references/port-management.md)、[技能](../../.agents/skills/port-management/SKILL.md) | 动态发现、配置消费者和释放检查；注册/租约工具未接入 |
| 动态 UI SOP | [正文](../../references/dynamic-ui-sop.md) | Prefab/CodeBind/owner/引用迁移；注册表/测试/runner 未接入 |
| 完善既有技能 | requirements/luban/ui/ugfentity | 决策图、注册与输出核对、结构与生命周期步骤；仍使用现有工具 |

现有 12 项 OpenSpec 官方技能保留；加原四项项目技能、本轮四项 SOP 技能和官方 unity-cli，共 21 项。其他官方 uGUI/搜索/渲染/包管理候选、LubanTableEditor、BuildAcceptance、PortRegistry、EditorConfigGuard、UI runner 和 worker 工具保留评估，未因本轮 SOP 迁移自动安装。

## 源文保留、适配与原因

来源为用户指定的参考仓库中同名技能、Book 页与 AutoTesting 工具。这里记录相对参考位置，正式文档不依赖外部工程绝对路径。

| 内容 | 原样保留 / 沿用 | 适配、删除或新增及原因 |
|---|---|---|
| AGENTS | 原章节结构与未涉及原文保留 | 增五行路由和已接入状态；不整文件复制。精确本轮 diff 见下文 |
| unity-cli | 当前官方安装器生成的所有文件原件 | 不复制参考自写版本；项目路径/边界在入口和 Book |
| 错误压缩器 | error_message_compactor.py 与 test_error_message_compactor.py 两份参考文件原样复制 | 新增 compact_console.py 适配本版 entries/logType/stackTrace；旧读取协议/历史扫描不迁 |
| auto-testing-sop | 编辑器回归与独立 Player 验收分工、前置/执行/产物/结论/故障的流程主题，精确工程与失败不续跑原则 | 正文迁 Book；run.py 保留入口与流程，内部重写。删参考 mega/车道/默认程序集、productName 修改、自动 cancel/stop、工件搬移、外部验证器及 JSON/hash 机制；这些不适用于当前工程或违反本轮约束 |
| 新自动测试实现 | 参考结构化解析/有限轮询/显式 project-path 思路 | 新增默认只发现、显式范围、空集/跳过非零码、繁忙拒绝、同工具独占锁和 Console 覆盖检查；当前无产品用例，保护用户现场，不伪造成功 |
| 启动 SOP | “当前启动方式/参数事实/每次验证前检查”结构及适用语句 | 玩法/Profile/投币前置替换为实际 Launcher→ProcedureET；ET Init 解析空参数，不编造业务 AppType |
| 资源 SOP | 目的/前置/人工步骤/自动化/验收结构 | 用 ResourceRuleEditor_ET 与 Refresh/Optimize；保留 HybridCLR；删不存在的 request/result 轮询、导出菜单与参考成功数量，不新增隐藏工具 |
| 打包 SOP | Editor 回归与准出验收分工、分阶段验证、独立 Player/Library 隔离、早期 stdout 日志、TestRunnerApi 创建陷阱 | 去掉参考双包脚本、业务过滤/退出参数、固定 boot 文本和 JSON 门禁；按本项目 BuildHelper 形成可用人工/CLI 流程。没有声称双包工具已迁入 |
| C# 手册 | 命名/文件/组织/编码四层结构和通用条目 | 字段规则限定 GF，ET 按既有源码；省去演示 ObservableLinkedList 样板，补本项目分层与提交检查，不全仓格式化 |
| 端口 SOP | 发现、角色/消费者、冲突、释放的通用顺序 | 移除旧工具角色、固定端口范围、MCP/KCP 租约 API 与快照；改为当前 CLI 与服务配置，不代定传输 |
| 需求拷问 | 参考 grilling 的事实/决定/后果、依赖轮次和具体结束条件 | 中文融入已有 requirements，不建立重复 grill-me；保留已授权任务无需重问及未决产品身份 |
| Luban/UI/Entity | 保留已有技能正文，沿用参考 SOP 的源/表/绑定/owner/验收顺序 | 补注册消费者、diff、资源与结构路由，生成器能力按当前源码核实；不搬专属表编辑器或不存在的 Prefab 能力 |
| 动态 UI / OpenSpec 收尾 | 沿用参考入口的纪律、生命周期与完成检查主题 | 本项目综合整理成唯一正文，引用现有 owner/工具/官方技能，未复制专属 runner、注册表或 obsoleteTerms 脚本 |

## AGENTS 逐字审阅

[本轮精确 diff 与每处原因](testing-sop-agents-review.md)。与本轮开始时比较，既有未提交的 OpenSpec 技能补齐改动保留，不混入此次审阅 diff。

## 验证记录

工具自身 27 项回归通过，包含参考压缩器原测试；真实 EditMode/PlayMode 发现均 0，明确 NotRun。当前 Editor ready、Play stopped、Pipeline 无运行测试、原生 UTF 注册表 active=false，未触发任何产品测试。

独立复核发现并修复 Console tail 截断漏错误、终态名单不一致、空 groundTruth 和原生 UTF 忙碌检查缺口；补充回归，不改写旧失败结果。没有在真实产品测试中验证完整 PlayMode 域重载/成功运行路径，该部分保持 NotRun，模拟回归不冒充实测。

21 项技能的 frontmatter/name/description 和 OpenSpec YAML 通过既有 Node 校验器。skill-creator 的 quick_validate.py 因本机缺少 PyYAML 未运行成功；没有新增该依赖，改用现有 OpenSpec YAML 库完成元数据检查，并另外检查引用及脚本行为。

两份压缩器文件与参考正文直接比较一致，不使用 hash。文档引用、源码路径、严格格式和 Git whitespace 的检查均通过；最终详细结果见 change 下 [验证记录](../../openspec/changes/migrate-testing-and-sops/verification.md)。

产品构建、Play/玩法、联网、AOT、资源实际刷新与导表均 NotRun，不把工具回归解释为产品通过。既有未提交修改保留，没有提交或推送；change 保留供审阅，没有自动归档。
