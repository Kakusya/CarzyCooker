# 技能、SOP 与配套工具迁移清单

2026-10-09，用户确认上一轮讨论的技能、SOP 与配套工具全部保留在迁移清单中，包括暂时用不到、依赖未齐或存在版本差异的项目；不因执行顺序靠后而遗漏或删除。

当前确认的是完整保留迁移范围。各项接入、适配与工具实现仍按用户此前要求逐项审阅、批准；“已确认保留”不写成“已安装”或“已迁移完成”。Unity CLI/Pipeline 和 Editor 环境已经核实；十二项 OpenSpec 官方技能已存在；本轮又接入官方 unity-cli、四项测试/诊断/端口 SOP 技能，并完善原四项项目技能。其他官方候选和独立配套工具仍按剩余范围审阅。

本页维护完整清单与审批材料，AGENTS 已有路由。SOP 正文在适用 Book 或 references 中维护一份，skill 负责加载和执行；旧入口重合时作跳转，不维护重复正文。

## 本轮实际交付

用户明确要求迁移自动测试框架和 SOP 后，已迁入 Tools/AutoTesting、auto-testing-sop、build-acceptance-sop、unity-error-extraction、port-management 和官方 unity-cli；补齐启动、ResourceCollection、错误诊断、C#、OpenSpec 收尾、端口及动态 UI 正文，并完善原四项项目技能。共 21 项可发现技能。以下原迁移范围保留，涉及这些项目的接入已经完成；具体已交付路径、来源和剩余工具见 [SOP 迁移](testing-sop-migration.md)。

下表中的拟维护位置是保留的设计信息，已交付项以交付明细为准；独立工具和其他官方候选仍按实际状态区分，不能把 SOP 完成写成工具全部接入。产品用例、打包运行和玩法测试未在本轮执行。

## OpenSpec 全部官方技能：已授权、已安装

2026-10-09，用户明确要求补齐全部 OpenSpec 技能。已使用本机官方 CLI 1.14.1，将 custom/workflows 扩展为十二项，在当前仓库运行 `openspec update`，补齐 new-change、continue-change、ff-change、bulk-archive-change、onboard。原七项由同一 CLI 刷新，其中 apply/update 的部分引导随完整工作流改为引用 continue/new；四项项目技能未改。

完整路由见 [AGENTS](../../AGENTS.md) 与 [工作流](workflow.md)。这次安装不扩大其他技能/配套工具的审批范围；官方模板不手改。profile/workflows 是全局配置，本次实际文件刷新只作用于当前 Codex 工程。

## 官方 unity-cli：本轮已授权、已接入

- **用途**：使用 Unity 官方现成说明，指导 Codex 调用命令行工具；Unity CLI 是工具，unity-cli skill 是使用指导，二者分开。
- **必要性**：CLI/包已接入，官方技能现已进入项目 Codex 发现目录。复用官方原件，避免自写版本重复维护或落后于 CLI。
- **依赖**：已有 CLI 1.0.0-beta.11、Pipeline 0.8.0-exp.1、Editor 6000.3.18f1；不再增加软件、MCP、端口工具或自定义命令。
- **官方来源与命令**：Unity CLI 内嵌与自身版本匹配的技能；官方仓库见 [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills/tree/main/skills/unity-cli)。在仓库根执行 `unity skill install codex --local`。此前 dry-run 确认目标；本轮已执行官方安装器，目标为本项目根目录，未改官方原件。
- **文件清单**：由官方安装器写入 `.agents/skills/unity-cli/`，包括 SKILL.md、CHANGELOG.md、SECURITY.md 和自带 references；不新增自写 command-usage.md。已在 `AGENTS.md`、`Docs/AI/module-index.md`、`Book/UnityCLI.md` 加正式路由，本页同步状态。
- **改造原则**：官方技能原文由上游维护；本项目路径、版本、授权、MCP/测试/打包边界留在 AGENTS 与手册，用户授权和项目规则优先。不从参考项目复制自写技能，不因官方说明覆盖多项能力就自动启用它们。
- **验收**：官方文件可被 Codex 发现，元数据和内部引用有效；用当前工程验证发现、状态、编译与 Console，真实报告未就绪/失败，遵守项目证据边界。接入不自动扩展为玩法测试。

Pipeline 0.8.0-exp.1 包还附带官方 `unity-pipeline` skill，目前在其 PackageCache 的 `.claude/skills/unity-pipeline/SKILL.md`；这只是上游包装位置，不构成本项目 Claude 接入。若要让 Codex 发现这项技能，接入范围另行列明，不自行改写或默认扩大第一项授权。

## 项目技能适配：本轮已完成基础完善

| 候选 | 用途 / 必要性 | 依赖 | 拟改文件 | 可观察验收 |
|---|---|---|---|---|
| 改造 carzycooker-requirements | 与参考拷问方式进一步对齐，明确逐轮问题、冲突和决策账本，避免为已授权任务重复造 gate | 已有 explore、requirements；只读参考问题组织 | 对应 SKILL.md；必要时新增 references/interview.md；AGENTS 仅同步真实边界 | 简单文档直接做；复杂需求只问影响范围的问题；原样保留待裁决，不自动提案 |
| 改造 carzycooker-luban | 删除旧自动改表能力后，补完整 ET 的“读表 → check → gen → diff”职责和失败出口 | 既有 ExcelExporter 与可用工作簿编辑方式；新增编辑工具另案审批 | 对应 SKILL.md、references/data-generation.md；若获准再加技能 references | Check 不写产物；失败不继续使用生成物；公式/样式不丢失；无隐式工具安装 |
| 改造 carzycooker-ui、carzycooker-ugfentity | 把参考 Prefab/CodeBind/owner 的 SOP 接到本项目 ETUI/Entity 层 | 前项 CLI 技能、已有 UI/Entity 表和生成器；不额外建结构注册工具 | 两项 SKILL.md 和必要 references；相应 Book/模块路由 | 普通 Entity/UIEntity ID 不混；骨架生成职责真实；加载中 Dispose、反复进入退出验收可执行 |
| auto-testing-sop | 在需要产品验证时统一 UTF 测试发现、执行、失败诊断，避免把编译当作行为验收 | 已有 Pipeline 引入的 UTF、CLI 技能；首个测试行为需单独确认 | 新技能 SKILL.md/测试引用；AGENTS 与模块索引；产品测试代码另案批准 | 能发现/执行一个已授权测试，准确报告失败、跳过与新运行；不改失败结果 |

以下各项均已确认保留。建议的执行顺序用于安排审阅，不缩小保留范围；每项实际实施前补齐用途、必要性、依赖、逐文件改动和可观察验收。

## 日常开发技能与 SOP（本轮已迁入）

| 项目 | 来源与用途 | 本项目适配与依赖 | 拟维护位置与验收 |
|---|---|---|---|
| 官方 unity-cli | Unity 官方内嵌技能；工具使用说明 | 复用原件，项目规则留在 AGENTS/Book；详细审批材料见上文 | `.agents/skills/unity-cli/`；发现、元数据、引用及当前工程命令/状态核对 |
| Unity 启动与验证 SOP | 参考 Book 同名页；统一工程、Editor、场景、编译与入口核对 | 保留通用检查；改为 ProcedureET、ET CodeMode、Model/Hotfix 链路；不迁投币、座位或参考 LaunchProfile | 拟 `Book/Unity启动与验证SOP.md`，从 Book/README 与 UnityCLI 路由；按实际入口与状态验收 |
| ResourceCollection 导出 SOP | 参考 Book 同名页；避免 Prefab、Luban 与热更资源遗漏 | 使用现有 ResourceRuleEditor_ET、Refresh/Optimize；保留 HybridCLR；不假设已有参考轮询文件与菜单 | 拟 `Book/ResourceCollection导出SOP.md`，资源/打包手册引用；检查规则、实际输出、失败日志和 diff |
| 完善 Luban SOP | 参考 luban-table-workflow 与导表手册；补全读表、改源、注册、Check、生成、差异和失败出口 | 使用 ET 工程与当前 ExcelExporter；保留公式/样式；专用编辑工具另项审批 | 改造现有 carzycooker-luban 与生成约定；Check 不写产物，失败不继续使用生成物 |
| 完善 UI SOP | 参考 UI 流程；补齐 Prefab、CodeBind、表、owner、关闭/回收与验收 | 使用 ETUI/Widget 与实际生成器；不复制另一套业务状态或资源池 | 改造现有 carzycooker-ui 与相关手册；反复开关、加载中关闭、订阅/池重用验收 |
| 完善 Entity SOP | 参考 ugf-entity-sop；补齐资源、类型/实例 ID、owner 与生命周期 | 保留通用原则；适配 ModelView/HotfixView、生成器命名和真实表字段，去除旧 MCP 路由 | 改造现有 carzycooker-ugfentity；Show/Hide、加载中 Dispose、owner 移除与 UIEntity ID 边界验收 |
| 错误诊断 SOP / unity-error-extraction | 参考错误汇总技能；控制刷屏，保留错误次数和项目栈帧 | 用当前 Pipeline Console，移除旧 MCP handoff；启动阶段保留原 Editor 日志排查；诊断脚本另项审批 | 拟技能/诊断参考页在审批时确定；未知错误不吞掉，原消息与定位可追溯 |

## 官方现成技能（全部保留，按需求安排接入）

官方来源为 [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills/tree/main/skills)。安装时核对当时的官方版本、依赖与现有项目；原件由上游维护，项目特定规则写在本项目入口和 SOP。

| 技能 | 用途 | 适配或版本条件 |
|---|---|---|
| ui-ugui | Canvas、RectTransform、Layout、ScrollRect 和交互可用性 | 与 carzycooker-ui 分工：通用布局由官方技能指导，ETUI/表/生命周期由项目技能负责 |
| generate-editor-search-query | 资源与场景对象的路径、类型、引用查询 | 核对 Search API 与实际 CLI 命令；是否打开窗口服从当轮用户要求 |
| unity-package-management | UPM 包增删、升级、查找与解析 | 包或 Editor bootstrap 脚本变更单独授权；不因安装技能自动生成脚本或新增包 |
| urp-postprocessing | Volume、Bloom、色调与后处理 | 当前 URP 17.3.0；按实际命令与场景核对，不预设画面需求 |
| validate-urp-render-graph-renderer-feature | 审查 Unity 6 Render Graph 自定义渲染功能 | 在实际 RendererFeature 任务中使用；不为接入技能主动新增渲染功能 |
| shader-graph-create-custom-node | HLSL 自定义 Shader Graph 节点 | 参考中的技能要求 Shader Graph >=17.5.0，本项目 lock 为 17.3.0；保留条目，接入前核实兼容性，不自动升级渲染包 |
| unity-pipeline | 官方项目包附带的 Editor 操作说明 | 包内现有技能不等于根 Codex 已发现；接入范围与 unity-cli 分工单独核对，不增加 Claude 接入 |

## 配套 SOP（本轮已迁入，独立工具另列）

| 项目 | 值得保留的流程 | 依赖与审批边界 |
|---|---|---|
| auto-testing-sop | 发现、选取、执行、等待终态、失败诊断、修复后新运行、真实报告 | 首版可基于已有 Pipeline/UTF；先确定实际行为，不搬参考四条 mega、投币战斗前置或整套证据管线 |
| build-acceptance-sop | 热更准备、资源刷新、构建、独立 Player 启动与验收 | 本项目保留 HybridCLR；目标平台、双包编排器、业务测试与执行范围分别审批 |
| OpenSpec 完成与归档 SOP | tasks、实现、delta、主规格、严格验证、同步与归档核对 | 已有 verify/sync/archive；先补操作清单，额外校验脚本另项审批 |
| grilling / grill-me 核心流程 | 事实/决定/后果分开，按依赖提问，输出决策账本 | 完善现有 carzycooker-requirements；保留原文适用部分，避免重复技能流程或重复询问已授权任务 |
| C# 代码规范与提交前检查 SOP | 格式、命名、生成物边界、源码/文档一致性与提交检查 | 可补独立手册；ET 与 GF 约定分别核对，不机械重命名存量或格式化全仓 |
| port-management | 实例发现、端口角色登记、冲突检查与释放 | 本项目保留服务端；核实实际监听，移除旧 MCP 假设，不复制参考的固定范围或退役角色状态 |
| 动态 UI 结构与迁移 SOP | Prefab、CodeBind、Canvas、引用边界、安全修改和验收 | 首个实际 UI 行为中确定范围；结构注册表、测试和迁移 runner 分别审批 |

## 配套工具（保留评估项，不以 SOP 接入代替工具审批）

| 配套 | 对应流程 | 接入前需要确认 |
|---|---|---|
| Tools/LubanTableEditor | Luban 源表语义编辑 | 依赖、真实工作簿、约束与失败恢复；不迁入 SHA/hash 审计或额外 JSON 证据包 |
| Tools/AutoTesting（已迁入） | 自动化测试 | 已接入实际发现/显式执行/终态/失败报告编排；产品用例按实际需求新增 |
| Tools/BuildAcceptance | 打包与包体验收 | HybridCLR/AOT、资源、目标平台与启动契约；工具存在不代表获准执行构建 |
| Tools/PortRegistry | 端口治理 | 本项目现役角色与配置消费者；不迁旧 MCP/Bridge 或扫描 token |
| Tools/OpenSpec/verify-change.ps1 | 完成/归档检查 | 与已有技能/CLI 的增量价值；移除参考专属 obsoleteTerms，核对同步前后语义 |
| Tools/EditorConfigGuard | C# 规范 | 本项目 .editorconfig、扫描范围、只读检查与可控修复；不全仓批量改格式 |
| 动态 UI 注册表/测试/Prefab 迁移 runner | UI 结构与迁移 | 确定具体 UI、白名单、dry-run、引用保护与可观察验收 |
| 专用 worker SOP 与编辑器探针 | 后续委派与编辑器协作 | 真实委派需求、单写者、依赖和运行方式；保留评估项不启动代理或引入编排 |

## 逐字审阅与来源说明

- 每项交付列出原样保留、路径替换、架构适配、删除与新增，并给出原因；尽量保留参考已验证的结构与适用原文。
- 官方技能优先使用现成原件；项目 SOP 按源码、配置与实际命令适配，不能把缺失工具写成已有能力。
- AGENTS 的修改单独展示；正文统一维护，模块索引和技能只作路由。
- 参考中旧 MCP/handoff、HybridCLR 排除、旧启动锁、专属测试/端口和 JSON/hash 机制须明确处理，不因“全部保留”恢复已删除接入或改变架构。
- 接入完成后才更新“已迁移/已安装/已验证”状态；未运行的检查如实记 NotRun。
