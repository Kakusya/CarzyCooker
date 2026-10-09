# CarzyCooker — Agent 指令

本仓库只维护 Codex 接入；当前工作分支为 `main`，跟踪 `origin/main`；底座来自原 `ET` 分支。需要公共基础设施时主动提醒。

## 需求讨论必须先进探索模式（硬规则）

只要当前回合属于需求 / 玩法 / 设计讨论，**在给出任何分析或建议之前**，必须先实际读取并加载 `openspec-explore`；禁止只口头提及或直接开答。

判定看用户消息形态，不靠自我归类，满足任一条即算：

- 提出想要的新功能 / 新行为 / 新玩法（「我想加…」「能不能让…」「要不要做…」）
- 询问方案、取舍、实现思路（「怎么实现」「有什么方案」「A 还是 B」「帮我看看这个问题」）
- 对现有玩法提出疑问、不满，或带着改变的期望在讨论

拿不准算不算：按算处理，先加载再回答。

仅以下情况豁免：纯事实查询（在哪 / 是什么 / 现状如何）、构建与报错排查、与玩法需求无关的纯技术问答、明确的实现类任务（直接走 apply 流程并加载对应技能）。明确的小型文档修订可直接完成，不为它制造空提案。

## 审查与遗留盘点（audit）

- 静态审查、清理候选与遗留历史记录放在 **[audit/](audit/README.md)**。
- **默认不必阅读**该目录；仅在排查已知债务、清理残留、或用户明确要求对照审查记录时再打开。
- 该目录**不**替代 OpenSpec / `TODO.md` / `KnownIssues.md`，也**不**自动授权修复、删除或开工。

## 长期目标

- 产品/工程方向见 **[long-term-goals.md](long-term-goals.md)**。
- 该文档约束**方向感**，**不**替代当前 OpenSpec 主规格，也**不**自动授权实现其中的远期能力。
- 探索或提案若触及长期方向：先读该文档，再与下文当前边界对照。

## 文档分层（必读）

| 层级 | 路径 | 用途 |
| --- | --- | --- |
| Agent 本文件 | `AGENTS.md` | 协作约束、边界、索引与技能加载 |
| 上手 / 框架用法 | [Book/](Book/README.md)、[框架总览](Docs/Framework/README.md) | 怎么做：快速开始、项目结构、UI/Entity、Luban、AssetSet、热更、打包等 |
| 模块实现约定 | [references/](references/README.md) | 本项目实际用法、坑点、开关（非规格） |
| 玩法交付进度 | [CarzyCooker当前进度.md](CarzyCooker当前进度.md) | 已落地能力一览（人读；冲突时以代码/表 + OpenSpec 为准） |
| 待办备忘 | [TODO.md](TODO.md)、[产品待办](Docs/Product/backlog.md) | 已知要做、不现在做（**不自动授权**） |
| 已知问题与取舍 | [KnownIssues.md](KnownIssues.md) | 已确认限制/取舍（**非自动 work item**） |
| 长期目标 | [long-term-goals.md](long-term-goals.md) | 方向感（**阅读 ≠ 授权实现**） |
| 产品设计 | [Docs/Product/](Docs/Product/README.md) | 已确认、授权补足、候选与待裁决的未来设计 |
| 变更与主规格 | [openspec/](openspec/README.md) | 跨会话规格；CLI：`openspec` |
| 模块索引 / 协作记录 | [模块索引](Docs/AI/module-index.md)、[迁移记录](Docs/AI/migration.md) | 源码、手册、技能、规格与历史验证范围 |

**阅读约定**：

- `Book/` 解释框架与工具；**不要**在 `Book/` 里堆长篇玩法设计或验收。两份原始资料保留原位，只用于追溯；正式设计在 `Docs/Product/`。
- `references/` 只写项目会用到的部分，不照搬官方全文；**非规格**，阅读 ≠ 授权实现文中「后续 / 未做」能力。
- `TODO.md` / `KnownIssues.md` / `long-term-goals.md` **不**替代 OpenSpec tasks 或主规格，也**不**自动授权开工。
- 自动生成文件禁止手改；改 Excel 后重新导表。Proto、CodeBind、ID 及 Unity 自动生成工程/缓存同样改源文件或生成器。
- 发现 **文档与实现对不上** 时先向用户说明差异与影响，再动手；改实现时同步更新对应文档/规格。

## 语言

- **用中文**撰写 OpenSpec 提案、design、specs、tasks 等规划产物。
- **用中文**回答用户；代码标识符、路径、API 名可保持英文。
- 提交说明若无特别要求，优先中文简洁描述。

## 技能加载

在对应工作阶段**主动加载**相关 skill，不要只口头提及。Codex 通过 `$技能名` 或技能选择器调用；在当前工具环境中，加载意味着实际读取对应 `SKILL.md` 并执行其规则，不要求存在名为 `Skill` 的工具。以下七项 OpenSpec 技能均已在项目中生成。

| 阶段 | 应加载的 skill |
| --- | --- |
| 探索 / 澄清需求 / 方案讨论 | [openspec-explore](.agents/skills/openspec-explore/SKILL.md) |
| 复杂 / 高风险需求在探索后的收敛拷问 | [carzycooker-requirements](.agents/skills/carzycooker-requirements/SKILL.md) |
| 提出变更、生成提案与产物 | [openspec-propose](.agents/skills/openspec-propose/SKILL.md) |
| 实现 tasks | [openspec-apply-change](.agents/skills/openspec-apply-change/SKILL.md) |
| 修订已有 change 的规划产物 | [openspec-update-change](.agents/skills/openspec-update-change/SKILL.md) |
| 验证实施与规格 | [openspec-verify-change](.agents/skills/openspec-verify-change/SKILL.md) |
| 将 delta specs 同步到主规格 | [openspec-sync-specs](.agents/skills/openspec-sync-specs/SKILL.md) |
| 归档已完成 change | [openspec-archive-change](.agents/skills/openspec-archive-change/SKILL.md) |
| 新建 / 修复 GF UI、ETUI、Widget、CodeBind | [carzycooker-ui](.agents/skills/carzycooker-ui/SKILL.md) |
| 新建 / 修复 GF Entity、ET UGFEntity、UIEntity | [carzycooker-ugfentity](.agents/skills/carzycooker-ugfentity/SKILL.md) |
| 改表 / 注册表 / 字段分组 / 校验导表；tasks 含 Excel·Luban·配置表 / 生成 ID / 本地化 | [carzycooker-luban](.agents/skills/carzycooker-luban/SKILL.md) |

Unity CLI 与项目 Pipeline 已获授权接入，用法见 [Unity CLI](Book/UnityCLI.md)。官方 CLI 技能接入与后续项目技能改造按 [技能审批清单](Docs/AI/skill-roadmap.md) 逐项批准；测试验收、打包验收和端口治理的专用管线仍未接入。已存在的 OpenSpec 技能继续按阶段加载。

## 自动化测试

- 编辑器自动化统一使用 Unity CLI 与项目 Pipeline；测试能力以当前实例的实际命令和 Unity Test Framework 为准，专用测试 skill/产品验收管线尚待审批。默认不用 MCP。
- 现有 ET Test / RobotCase 与 Editor 工具只代表底座入口，不等于做饭产品验收；定位见 [模块索引](Docs/AI/module-index.md)。
- 失败诊断以实际测试消息、堆栈和原始输出为准；不得续跑或改写失败结果，修复后创建新的测试运行。
- 报告结果如实：测试失败给输出；跳过的步骤说清楚。编译、生成、Unity/AOT、联网、玩法各证明自己范围，未运行写 `NotRun`。
- 新测试框架或配套工具按下文审批；不因文档存在自动扩大测试范围。缺口见 [配套文件与工具](Docs/AI/reference-alignment.md)。

## 项目端口注册表

- 项目级端口注册表和端口治理技能尚未接入，缺口见 [配套文件与工具](Docs/AI/reference-alignment.md)。Pipeline 端口从当前实例描述发现，不固定绑定或预留参考端口。
- 新监听、固定 endpoint、多 Editor 或开发期端口需求时主动提醒，核对当前配置与消费者，再提出方案；不自动安装或实现端口管线。
- 既有网络/服务配置是底座事实，不自动构成启动服务、改端口或实现做饭传输的授权。

### 探索与设计阶段（特别强调）

- 需求 / 设计讨论的触发判定按顶部「需求讨论必须先进探索模式（硬规则）」执行；命中即在第一轮回复前加载 `openspec-explore`，再进行分析。
- 复杂 / 高风险需求在 `openspec-explore` 已梳理现状与候选方向后，加载 `carzycooker-requirements` 做决策树拷问；简单任务不强制经过该 gate。
- `carzycooker-requirements` 结束时只输出决策账本并询问是否进入提案；必须等用户确认后再加载 `openspec-propose`，不得自动创建 artifacts。用户已明确授权生成提案或给出实施计划时，不重复询问同一授权。
- 探索中若需要落成正式提案：再加载 `openspec-propose`（或提示用户 `$openspec-propose`）。
- 探索模式**不实现业务代码**；实现走 apply 流程。
- 涉及 Unity 场景/资源/编辑器验证时，使用 Unity CLI/Pipeline，按 projectPath 精确匹配本工程；先确认实际状态和命令，不能作用于其他项目。
- 搭建或修复本项目 GF Entity、ET UGFEntity 或 UIEntity 时：**加载 `carzycooker-ugfentity`**，并与 `Book/Entity开发.md` / Entity 表对照；不要只口头提及。

### 表配置与 OpenSpec 实现（特别强调）

- 任务涉及以下任一情况时，**必须先加载 `carzycooker-luban`**，再改表或导表：
  - 路径含 `Design/Excel/`、`.xlsx`、`__tables__` / `__beans__` / `__enums__`
  - tasks / design / specs 写到配置表、Luban、导表、生成 `DT*` / `DR*`
  - 需要新增表、改字段、区分 client/server group 或调整 ET 的 gameclient/gameeditor/client/clientserver/editor 输出
- **禁止**手改生成物（`DT*` / `DR*` / `.bytes` / 生成 JSON）；改 Excel 源或生成器，再按 skill 跑 check / gen。
- 执行 `openspec-apply-change` 时：若当前 tasks 触及配置表，**在动手实现前**一并加载 `carzycooker-luban`；不要只加载 OpenSpec skill。
- 仅当 tasks 明确是纯代码 / 场景 / Prefab、且不改表时，才可不加载 Luban skill。
- 使用本项目既有 `Share/Tool/ExcelExporter/` 与 active Luban 工程；Check / 导出失败后不得继续使用生成物。退出码不能单独证明成功，见 `KnownIssues.md`。
- Entity 表变更：先/同时加载 `carzycooker-luban`；UGFEntity Prefab/挂载流程另加载 `carzycooker-ugfentity`。

### 动态 UI 结构纪律

- 创建或修改 UI / Widget 时，**加载 `carzycooker-ui`**，核对 Prefab、CodeBind、配置、owner 和生命周期；不建立第二份业务状态或私有池绕过现有 owner。
- 结构重构、表驱动行为修正、验收工具改进应拆分 OpenSpec change；归档前用现有验证技能和严格格式验证，确认任务、delta spec 与实际契约。
- 动态 UI 结构注册表、结构测试与迁移 runner 尚未接入，见 [配套文件与工具](Docs/AI/reference-alignment.md)。不自动创建这些工具，不以文件存在冒充验证通过。

## 待办备忘

- 非当前必做、但已知要做的事项见 **[TODO.md](TODO.md)**。
- 该列表**不**替代 OpenSpec tasks，也**不**自动授权开工；实现前仍须走探索 / 提案 / change（除非用户在会话中明确指定立刻做某条）。
- Agent 在相关域探索或提案时宜扫一眼 `TODO.md`，避免重复遗漏；**完成或放弃条目后直接删除该条**，不要打勾保留（本文件只列未做事项）。
- 做饭设计待办与冲突见 [产品待办](Docs/Product/backlog.md)；未决产品事项只有实际裁决后才能移除，不能由助手代定。

## 已知问题与实现取舍

- 已确认、但未必需要修复的实现限制与取舍见 **[KnownIssues.md](KnownIssues.md)**。
- 本文件不替代 OpenSpec 规格或待办；若问题影响产品契约、验收或计划，须在相应 change / 主规格中明确。
- Agent 在修改相关实现、解释行为或提出 change 时应先对照该文件，避免把已知取舍误述为已支持能力。

## 项目约定

- OpenSpec 根目录：`openspec/`；变更在 `openspec/changes/`，归档在 `openspec/changes/archive/`。CLI 初始固定 `1.14.1`，升级须有明确任务授权。
- 本仓库是 **GameDevelopmentKit 双端底座上的 CarzyCooker 项目**：

| 层 | 职责 |
| --- | --- |
| **UGF 壳** | 启动 Procedure、资源/UI/Entity/Scene/Sound 等运行时组件 |
| **ET 业务** | 服务端 ET 8.1；客户端 Model/Hotfix、ModelView/HotfixView 分层与 UGF 桥 |
| **HybridCLR / UniTask** | 热更与 AOT 元数据；项目统一异步模型 |
| **Luban** | 多工程表驱动配置与生成常量、ID、本地化 |
| **工具链** | 根目录 `Share/` 的 Analyzer / SourceGenerator / Tool / FileServer 等 |

- 服务端、ET Session、HybridCLR 和 Model/Hotfix 结构保留。当前 `main` 的 ET 底座已移除 GameHot、纯 GF 示例与 GF 网络模块；不按其他分支的目录或宏残留恢复它们。
- `ProcedurePreset` 直接进入 `ProcedureET`；ET CodeMode/热更装载仍由实际宏与配置决定。不假定已有做饭 `AppType`、业务入口或厨房模块。
- 业务代码落在 `Unity/Assets/Scripts/Game/ET/Code/` 的 Model/Hotfix/ModelView/HotfixView 适用层；先核对程序集、owner 和现有模式，不创建新业务架构作为隐含前置。
- **逻辑与表现分离**：组件/实体持有领域状态，Mono view 提供绑定与表现；UI、网络回调、动画不自动成为未来做饭世界的第二写入权威。
- 改行为前先读相关主规格 `openspec/specs/<capability>/spec.md` 与进行中 change；主规格未齐时以代码/表 + 用户意图为准，并提议同步文档。不要只扫脚本。

### 构建与运行（最短路径）

- 环境：.NET 8 SDK、Unity **6000.3.18f1**、付费插件 Odin Inspector；MongoDB 按服务端功能需要。
- 人工步骤：根目录 `dotnet build Kit.sln` → Unity 打开 `Unity/` → 工具栏 **Launcher** 运行当前示例 → 首次/表变更后 `Game/Tool/ExcelExporter`。这些是用法说明，执行仍须属于当前任务。
- 详解见 [Book/快速开始.md](Book/快速开始.md)；热更准备与打包见 [HybridCLR](Book/HybridCLR热更.md)、[一键打包](Book/一键打包.md)。
- 解决方案：`Kit.sln`（工具）、`DotNet/DotNet.sln`（服务端）；Unity IDE 工程由 Editor 生成。
- 编译符号：`UNITY_ET`、`UNITY_HOTFIX`、`UNITY_ET_VIEW`，ET code mode 另分 CLIENT / SERVER / CLIENTSERVER；刷新菜单 `Game/Define Symbol/Refresh`、`ET/Define Symbol/Refresh`。以当前配置和源码为准，不套用 client-only 设置。

### 仓库与程序集（摘要）

```text
DotNet/                # 服务端 App / Core / Loader / Model / Hotfix / ThirdParty
Share/                 # Analyzer / SourceGenerator / Tool / FileServer / Libs 等
Tools/                 # 脚本与 Luban 发行物
Config/                # 服务端运行配置及派生数据
Design/Excel/          # ET / Localization 等配置源（无 GameHot 工程）
Design/Proto/          # 协议源和 proto.conf
Book/                  # 框架与工具中文文档；两份原始资料保留
references/            # 模块实现约定与坑点（唯一维护正文）
Docs/Product/          # 未来做饭设计、候选与待裁决
Docs/AI/               # 工作流、模块索引、历史迁移及配套缺口
openspec/              # 变更与主规格
CarzyCooker当前进度.md  # 已落地能力一览（人读）
TODO.md / KnownIssues.md / long-term-goals.md
Unity/
  Assets/
    Res/               # 运行时资源、UI/Entity Prefab、场景、配置
    Scripts/
      Game/            # GF 公共组件、Procedure、UI、Entity、HybridCLR
        ET/Loader/     # Init、CodeLoader、UGF 桥
        ET/Code/       # Model / Hotfix / ModelView / HotfixView
      Library/         # UGF、ET.Core、LubanLib 与扩展
  Packages/            # 依赖声明；声明不等于环境已验证
```

| 程序集 | 职责 |
| --- | --- |
| `Game` / `Game.Editor` | GF 壳、Procedure、公共运行时与编辑器入口 |
| `Game.ET.Loader` | ET 初始化、CodeLoader、UGF 桥 |
| `Game.ET.Code.Model` / `Game.ET.Code.ModelView` | ET 数据与客户端 view 模型 |
| `Game.ET.Code.Hotfix` / `Game.ET.Code.HotfixView` | ET 逻辑与客户端 view 逻辑 |
| `ET.Core` / `ET.ThirdParty` | ET 核心与第三方；按 asmdef/csproj 核对实际依赖 |

启动链路：`Launcher.unity` → GF Procedure → `ProcedurePreset` → `ProcedureET` → `ET.Init` / CodeLoader → `ET.Entry.Start`。底座启动不表示做饭世界已经启动。

### 当前架构边界

- 玩法「已有什么」优先看：**代码 + 表** + [CarzyCooker当前进度.md](CarzyCooker当前进度.md) + 相关 `references/`；冲突时以代码/表与用户最新意图为准并提议同步文档。
- 当前源码支持的是底座与示例。做饭的房间、厨房、订单、顾客、伙伴、供应和存档设计尚未实施；正式设计见 [Docs/Product/](Docs/Product/README.md)。
- 未来业务保留“已确认、授权补足、候选、待裁决”的区别；历史授权、候选目录、文件和标签不增加本轮实施授权。
- 已确认的未来边界：Host 是世界权威但不自动是存档 Owner；本地/远端玩家走同一裁决；运行世界、持久进度、领取凭证分开；PC/Android 生态隔离。这些是设计，不是现有底座保证。
- 普通手槽/台面一格一物、容器内容随容器移动、分装守恒、加工命令完整验证后提交等详见 [产品设计](Docs/Product/design.md)，不在文档整理中新增实现。
- 暂停待执行命令、断线手持物、传输选型等仍见 [待裁决](Docs/Product/backlog.md)，助手不代定。
- 表默认值可按已授权需求调整；生成常量路径和启用模式见 `carzycooker-luban`，生成物勿手改。
- UI / Entity 流程见 [Book/UI开发.md](Book/UI开发.md)、[Book/Entity开发.md](Book/Entity开发.md)；表在启用模式的 `Design/Excel/<模式>/Datas/Game/`。
- ET 的 Model/ModelView/Hotfix/HotfixView 程序集按配置装载，Loader 为稳定层；HybridCLR 保留，不把关闭热更或删除 ET 分层当成整理文档的前置。
- 跨仓对照仅在用户明确要求的范围内进行；读取参考不授权修改或同步其他仓库。

### 主规格与 change 索引

| 领域 | 说明 |
| --- | --- |
| 当前主规格 | [规格索引](openspec/README.md)：启动、运行时、UI、Entity、资源、热更、配置、协议、本地化、网络、服务端、编辑器、依赖、响应式、协作资料 |
| 模块与技能 | [模块索引](Docs/AI/module-index.md)：按实际源码、手册、规格和技能定位 |
| 进行中 / 近期 change | 以 `openspec list` 与各 change `tasks.md` 勾选为准，**勿假设本表已穷尽** |

- 主规格目录 **可能尚未收齐**全部已完成能力；**归档 / sync 前不要假设** `openspec/specs/` 已覆盖全部玩法。
- 历史迁移的范围与验证见 [迁移记录](Docs/AI/migration.md)，不是后续任务的永久禁令。没有明确任务时不强制处理归档债。

### 当前明确非目标（默认勿擅自开做）

- 把 `long-term-goals.md`、`TODO.md`、`references/` 或产品设计中的远期/未做条目当作已授权实现
- 没有具体玩法任务时实施做饭业务、改变底座架构，或替用户裁决暂停/断线/传输问题
- 在已授权 Unity CLI/Pipeline 接入之外，自动引入 MCP、额外测试框架、打包验收、端口治理工具或未经批准的 skill
- 无用户明确要求时的跨仓同步 / 考古、提交、推送或发布

## Book 框架文档

- 目录入口：**[Book/README.md](Book/README.md)**。
- 放「怎么做」：环境上手、项目结构、UI/Entity、Luban、AssetSet、代码规范、热更、打包与工具菜单等。
- **不要**把玩法设计、分期 roadmap、验收清单堆进 `Book/`；已落地能力一览见根目录 [`CarzyCooker当前进度.md`](CarzyCooker当前进度.md)。
- 改框架用法、导表/打包流程或 Entity/UI SOP 时：同步对应 Book 页，并保持与实现一致。
- 两份原始资料保留原位，只用于追溯；正式产品设计在 `Docs/Product/`，原件不恢复旧工程执行路由。

## 参考文档

- 仓库参考文档目录为 **[references/](references/README.md)**；新模块实现约定路径一律写 `references/...`。
- 各文顶部有统一页眉：**非规格**；冲突以代码/表/进行中 change/已同步主规格及用户最新意图为准；**阅读 ≠ 授权**文中未做能力。
- 改对应模块前**先读**相关参考，并与主规格 / change 对照；保持文档与实现同步。
- 原 `Docs/Development/` 对应页只保留跳转，正文在 `references/` 或 `KnownIssues.md` 唯一维护。
- 共用约定先读 [业务约定](references/business-code-conventions.md)、[生命周期](references/framework-lifecycle.md)、[生成约定](references/data-generation.md)、[数据与投影](references/reactive-and-network.md)；细定位见 [模块索引](Docs/AI/module-index.md)。

| 模块 | 源码/资料入口 | 用法/约定 | 主规格 | 技能 |
|---|---|---|---|---|
| 启动/模式 | [Unity/Assets/Scripts/Game/Procedure](Unity/Assets/Scripts/Game/Procedure) | [快速开始.md](Book/快速开始.md) | [startup](openspec/specs/startup/spec.md) | [openspec-explore](.agents/skills/openspec-explore/SKILL.md) |
| 公共组件/流程/示例 | [Unity/Assets/Scripts/Game/Base](Unity/Assets/Scripts/Game/Base) | [Project结构.md](Book/Project结构.md) | [runtime-foundation](openspec/specs/runtime-foundation/spec.md) | [openspec-apply-change](.agents/skills/openspec-apply-change/SKILL.md) |
| ET Model/Hotfix/View/共享 | [Unity/Assets/Scripts/Game/ET/Code](Unity/Assets/Scripts/Game/ET/Code) | [ET动态事件.md](Book/ET动态事件.md) | [runtime-foundation](openspec/specs/runtime-foundation/spec.md) | [openspec-apply-change](.agents/skills/openspec-apply-change/SKILL.md) |
| GF UI/ETUI/Widget | [Unity/Assets/Scripts/Game/UI](Unity/Assets/Scripts/Game/UI) | [UI开发.md](Book/UI开发.md) | [ui](openspec/specs/ui/spec.md) | [carzycooker-ui](.agents/skills/carzycooker-ui/SKILL.md) |
| GF Entity/ETEntity/UIEntity | [Unity/Assets/Scripts/Game/Entity](Unity/Assets/Scripts/Game/Entity) | [Entity开发.md](Book/Entity开发.md) | [entity](openspec/specs/entity/spec.md) | [carzycooker-ugfentity](.agents/skills/carzycooker-ugfentity/SKILL.md) |
| 容器/资源/AssetSet | [Unity/Assets/Scripts/Game/Container](Unity/Assets/Scripts/Game/Container) | [AssetSet.md](Book/AssetSet.md) | [resource-management](openspec/specs/resource-management/spec.md) | [openspec-apply-change](.agents/skills/openspec-apply-change/SKILL.md) |
| Excel/Luban/生成 ID | [Share/Tool/ExcelExporter](Share/Tool/ExcelExporter) | [Luban配置.md](Book/Luban配置.md) | [data-generation](openspec/specs/data-generation/spec.md) | [carzycooker-luban](.agents/skills/carzycooker-luban/SKILL.md) |
| Proto/Opcode/消息生成 | [Share/Tool/Proto2CS](Share/Tool/Proto2CS) | [Proto生成工具.md](Book/Proto生成工具.md) | [proto-generation](openspec/specs/proto-generation/spec.md) | [openspec-apply-change](.agents/skills/openspec-apply-change/SKILL.md) |
| 本地化/UX 文本 | [Unity/Assets/Scripts/Game/Localization](Unity/Assets/Scripts/Game/Localization) | [多语言.md](Book/多语言.md) | [localization](openspec/specs/localization/spec.md) | [carzycooker-luban](.agents/skills/carzycooker-luban/SKILL.md) |
| ET Session/RPC/HTTP | [ET 消息模块](Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message) | [reactive-and-network.md](references/reactive-and-network.md) | [network](openspec/specs/network/spec.md) | [openspec-explore](.agents/skills/openspec-explore/SKILL.md) |
| 服务端/DB/Agent/Admin | [DotNet](DotNet) | [管理后台.md](Book/管理后台.md) | [server](openspec/specs/server/spec.md) | [openspec-apply-change](.agents/skills/openspec-apply-change/SKILL.md) |
| HybridCLR/程序集/打包 | [Unity/Assets/Scripts/Game/HybridCLR](Unity/Assets/Scripts/Game/HybridCLR) | [HybridCLR热更.md](Book/HybridCLR热更.md) | [hot-reload](openspec/specs/hot-reload/spec.md) | [openspec-explore](.agents/skills/openspec-explore/SKILL.md) |
| Editor/Toolbar/骨架/服务工具 | [Unity/Assets/Scripts/Game/Editor](Unity/Assets/Scripts/Game/Editor) | [ET代码生成工具.md](Book/ET代码生成工具.md) | [editor-tools](openspec/specs/editor-tools/spec.md) | [openspec-explore](.agents/skills/openspec-explore/SKILL.md) |
| ET reactive/外部 ReactiveBinding | [Share/SourceGenerator](Share/SourceGenerator) | [reactive-and-network.md](references/reactive-and-network.md) | [reactive-ui](openspec/specs/reactive-ui/spec.md) | [carzycooker-ui](.agents/skills/carzycooker-ui/SKILL.md) |
| Analyzer/第三方/程序集/包 | [Unity/Assets/Scripts/Library](Unity/Assets/Scripts/Library) | [conventions.md](references/business-code-conventions.md) | [dependencies](openspec/specs/dependencies/spec.md) | [openspec-explore](.agents/skills/openspec-explore/SKILL.md) |
| 未来产品/需求/候选 | [Docs/Product](Docs/Product) | [README.md](Docs/Product/README.md) | [ai-collaboration](openspec/specs/ai-collaboration/spec.md) | [carzycooker-requirements](.agents/skills/carzycooker-requirements/SKILL.md) |

约定：新增模块通用用法总结时，在 `references/<模块>.md` 创建；内容聚焦本项目实际用法；补上统一页眉。缺少配套文件时先说明用途并与用户讨论，不因参考有该文件就声明本项目已有对应工具。

## 代码规范（摘要）

完整实现约定：[references/business-code-conventions.md](references/business-code-conventions.md)。独立 C# 规范手册尚缺，见 [配套文件与工具](Docs/AI/reference-alignment.md)。写代码时对齐周围文件，并遵守：

- 类型/方法/public：`PascalCase`；局部参数：`camelCase`
- GF 实例私有字段：`m_PascalCase`；静态私有：`s_PascalCase`；ET 与生成字段按本项目现有模式，不机械重命名存量
- 缩进 4 空格；显式可见性；禁止单行 `if`
- ET 业务：Model/ModelView 持数据，Hotfix/HotfixView 的 `System` 持逻辑；命名空间按 `ET` / `ET.Client` / `ET.Server`
- 生成文件只读；FriendOf / EntitySystemOf 等属性按现有模式使用

## 文档与实现一致

- **发现文档与实现对不上时要提出来**：OpenSpec（proposal / design / specs / tasks）、`AGENTS.md`、`Book/`、`references/`、产品文档、`CarzyCooker当前进度.md`、注释或 README 与代码行为不一致时，先向用户说明差异与影响，再动手改；不要静默按「可能过时」的文档硬做，也不要默认文档一定对。
- **改实现时要同步修改文档**：行为、接口、约定或验收标准有变，必须同步更新对应主规格（`openspec/specs/`）、进行中的 change 产物，以及会误导后续工作的说明；只改代码不改文档视为未完成。
- 若仅实现符合新意图、文档仍描述旧行为，收尾前应列出待更新的文档项并完成更新（或明确请用户确认暂缓同步的范围）。

## Unity Pipeline 与场景刷新

Unity Editor 状态、编译、测试和截图统一通过 Unity CLI/Pipeline。当前 CLI `1.0.0-beta.11`，项目固定 `com.unity.pipeline@0.8.0-exp.1`；项目 Editor 为 `6000.3.18f1`。用法见 [Unity CLI](Book/UnityCLI.md)，专用技能待逐项审批。

### 编辑器单轨：Unity Pipeline

- 默认不用 MCP；已授权的 CLI/Pipeline 接入无需重复询问，实际命令和参数以 `unity list`、`unity command --help` 与当前实例为准。
- 用 `unity pipeline list` 发现实例并按 `projectPath` 精确匹配；显式传 `--project-path`，不固定端口，不记录描述文件中的 token。
- 新技能、测试/打包验收或端口治理配套仍先给用途、必要性、依赖、改动与验收，逐项审批。
- 按真实环境、风险与当轮任务执行；安装声明、连接成功、编译通过与玩法验收分别报告，缺能力记 `NotRun`。

### 改代码后的编译验证（必需）

1. 确认 Editor 已打开本工程且 Pipeline 实例可发现，核对适用程序集。
2. 按实际命令执行 `recompile`，轮询 `recompile_status`；读取 `console` / `console_status` 的 Error/Exception 和 Editor groundTruth，不把命令返回成功当作编译成功。
3. 提交前修掉本次引入的 CS 错误和未预期测试日志；存量错误先说明范围与影响。
4. 导入/域重载期间等待实际状态；缺环境时记 `NotRun`，不反复重启 Editor 或擅改用户现场。
5. 纯文档检查引用与规格；包/程序集接入变更需验证解析和编译，不自动运行产品构建或玩法测试。

### 场景与外部文件

- **不要在 Unity Editor 当前打开且未保存的场景上，直接通过文件系统覆写 `.unity` / `.meta` 文件。**
- 场景结构或序列化字段优先通过已有且获授权的 Unity Editor 安全操作修改并保存。
- 若必须文本编辑场景：先确保该场景未在 Editor 打开，完成后再等待 Unity 重新导入。
- 编辑 C# 脚本后，优先等待 Editor 可用并检查 Console，避免无必要的强制刷新。

### 注意

- 用户未要求时不要 push；提交、合并、发布须有相应授权
- 不要把标题含 Unity 的 Rider 窗口当成 Editor
- 不要提交验证用的临时文件、原始日志、缓存、用户现场或额外状态副本

## 协作习惯

- 难以逆转或对外发布的操作先确认（除非用户已明确授权本次执行）。
- 报告结果如实：测试失败给输出；跳过的步骤说清楚。文档、候选、标签和构建结果不能冒充玩法实现、用户决定或产品验收。
- 写代码时对齐周围文件的命名、注释密度与习惯。
- 行尾规约以本项目 `.gitattributes` 和周围文件为准；不要照搬其他项目的逐路径设置，也不要为无关改动整文件重写行尾。
- 需要公共基础设施（新表管线、新程序集边界、跨模块事件约定等）时主动提醒。
- **禁止创建新的 Git / Orca worktree**；任务默认只在当前 worktree 执行。用户明确授权创建时，以当轮授权为准。
- 保留用户未提交修改和已删除文件；不用清空、reset 或全局终止进程处理任务，不恢复旧任务或已删除工作流。
- 新增配套工具前，提供用途、必要性、依赖、逐文件改动和验收方式，取得批准再安装或实现；已有明确授权无需重复询问。
- 不执行、要求或委托 SHA 校验、源码/产物 hash 比对或批量 hash 清单。
- 不新增 JSON 证据包、全量状态副本或额外证据快照归档。原始测试输出留在原目录，报告只留简短结果、失败原因、原日志路径和退出码；压缩、改名不增加授权。
- OpenSpec CLI 的即时结果格式和正常规格归档不等于额外证据/业务状态副本；不得借技能保存或输出完整业务 JSON 状态。
- 用户授权与项目规范优先于技能建议，技能不自动增加授权。探索、拷问、提案和实施的阶段边界按现有技能执行。
- 专用 worker 编排及其依赖的编辑器探针尚未接入，见 [配套文件与工具](Docs/AI/reference-alignment.md)；不自动迁入或声明已具备。不并发覆盖同一文件，不因主会话中断自行终止用户进程。
- 运行过程日志不堆进业务文档；实现约定放 `references/`，规格与任务放 OpenSpec，审查记录按需放 `audit/`。
