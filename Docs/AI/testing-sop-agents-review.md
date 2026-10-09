# 自动测试与 SOP：AGENTS 逐字审阅

这是本轮开始到完成的精确文本 diff，不含此前补齐 OpenSpec 技能的改动。未改动的章节与原文保留；根入口没有整文件复制参考。

## 改动原因

| 改动 | 原因 |
|---|---|
| 新增五行技能路由 | 官方 unity-cli 加四项已迁入项目 SOP 技能均真实存在，入口须可发现 |
| 技能状态说明 | 自动测试框架与 SOP 从候选变为已接入，仍区分独立双包、PortRegistry 和 UI runner 的工具缺口 |
| 自动测试两段 | 使用已迁入编排器；明确真实空集、跳过、超时、Console 缺口与用户现场保护 |
| 端口与动态 UI | SOP 已迁入，专用注册/租约/迁移工具未迁入，不声明不存在能力 |
| C# 规范入口 | 独立手册已补齐，保留 GF/ET 各自字段约定与原摘要 |
| Editor 入口与授权 | 路由到官方技能和实际 SOP；本轮明确授权不重复申请，未来新工具仍按原审批规则 |

## 逐字 diff

```diff
--- AGENTS.md（本轮开始）
+++ AGENTS.md（本轮完成）
@@ -79,20 +79,25 @@
 | 新建 / 修复 GF UI、ETUI、Widget、CodeBind | [carzycooker-ui](.agents/skills/carzycooker-ui/SKILL.md) |
 | 新建 / 修复 GF Entity、ET UGFEntity、UIEntity | [carzycooker-ugfentity](.agents/skills/carzycooker-ugfentity/SKILL.md) |
 | 改表 / 注册表 / 字段分组 / 校验导表；tasks 含 Excel·Luban·配置表 / 生成 ID / 本地化 | [carzycooker-luban](.agents/skills/carzycooker-luban/SKILL.md) |
-
-Unity CLI 与项目 Pipeline 已获授权接入，用法见 [Unity CLI](Book/UnityCLI.md)。官方 CLI 技能接入与后续项目技能改造按 [技能审批清单](Docs/AI/skill-roadmap.md) 逐项批准；测试验收、打包验收和端口治理的专用管线仍未接入。已存在的 OpenSpec 技能继续按阶段加载。
+| Unity CLI / Editor 操作 | [unity-cli](.agents/skills/unity-cli/SKILL.md) |
+| 发现 / 执行已授权 UTF 回归、终态与失败诊断 | [auto-testing-sop](.agents/skills/auto-testing-sop/SKILL.md) |
+| 已授权产品构建与独立 Player 包体验收 | [build-acceptance-sop](.agents/skills/build-acceptance-sop/SKILL.md) |
+| 当前 Unity Console 错误提取与压缩 | [unity-error-extraction](.agents/skills/unity-error-extraction/SKILL.md) |
+| 新监听 / 固定 endpoint / 多 Editor 的端口核对 | [port-management](.agents/skills/port-management/SKILL.md) |
+
+Unity CLI 与项目 Pipeline 已接入；官方 CLI 技能、自动测试编排及配套 SOP 已按本轮授权迁入，用法见 [Unity CLI](Book/UnityCLI.md) 与 [迁移清单](Docs/AI/skill-roadmap.md)。打包验收和端口 SOP 使用当前入口，专用双包编排、PortRegistry 与 UI 结构 runner 尚未接入。已存在的 OpenSpec 技能继续按阶段加载。
 
 ## 自动化测试
 
-- 编辑器自动化统一使用 Unity CLI 与项目 Pipeline；测试能力以当前实例的实际命令和 Unity Test Framework 为准，专用测试 skill/产品验收管线尚待审批。默认不用 MCP。
+- 编辑器自动化统一使用 Unity CLI 与项目 Pipeline；测试能力以当前实例的实际命令和 Unity Test Framework 为准。加载 [auto-testing-sop](.agents/skills/auto-testing-sop/SKILL.md)，按 [自动测试 SOP](Book/自动化测试SOP.md) 使用已迁入 [Tools/AutoTesting](Tools/AutoTesting/README.md)；默认只发现，执行需明确模式、过滤范围和当轮授权。默认不用 MCP。
 - 现有 ET Test / RobotCase 与 Editor 工具只代表底座入口，不等于做饭产品验收；定位见 [模块索引](Docs/AI/module-index.md)。
 - 失败诊断以实际测试消息、堆栈和原始输出为准；不得续跑或改写失败结果，修复后创建新的测试运行。
 - 报告结果如实：测试失败给输出；跳过的步骤说清楚。编译、生成、Unity/AOT、联网、玩法各证明自己范围，未运行写 `NotRun`。
-- 新测试框架或配套工具按下文审批；不因文档存在自动扩大测试范围。缺口见 [配套文件与工具](Docs/AI/reference-alignment.md)。
+- 空集 NotRun、跳过 Skipped，超时或 Console 覆盖缺口不算通过；忙碌 Editor/已有测试拒绝启动，不自动 cancel、stop 或清 Console。后续新测试框架或配套工具按下文审批，不因 SOP 存在自动扩大产品测试范围；缺口见 [配套文件与工具](Docs/AI/reference-alignment.md)。
 
 ## 项目端口注册表
 
-- 项目级端口注册表和端口治理技能尚未接入，缺口见 [配套文件与工具](Docs/AI/reference-alignment.md)。Pipeline 端口从当前实例描述发现，不固定绑定或预留参考端口。
+- 加载已迁入 [port-management](.agents/skills/port-management/SKILL.md)，按 [端口 SOP](references/port-management.md) 核对实际配置和消费者；项目级 PortRegistry/租约工具尚未接入。Pipeline 端口从当前实例发现，不固定绑定或预留参考端口。
 - 新监听、固定 endpoint、多 Editor 或开发期端口需求时主动提醒，核对当前配置与消费者，再提出方案；不自动安装或实现端口管线。
 - 既有网络/服务配置是底座事实，不自动构成启动服务、改端口或实现做饭传输的授权。
 
@@ -122,7 +127,7 @@
 
 - 创建或修改 UI / Widget 时，**加载 `carzycooker-ui`**，核对 Prefab、CodeBind、配置、owner 和生命周期；不建立第二份业务状态或私有池绕过现有 owner。
 - 结构重构、表驱动行为修正、验收工具改进应拆分 OpenSpec change；归档前用现有验证技能和严格格式验证，确认任务、delta spec 与实际契约。
-- 动态 UI 结构注册表、结构测试与迁移 runner 尚未接入，见 [配套文件与工具](Docs/AI/reference-alignment.md)。不自动创建这些工具，不以文件存在冒充验证通过。
+- 结构变更按已迁入 [动态 UI SOP](references/dynamic-ui-sop.md) 核对 Prefab、CodeBind、Canvas 和引用消费者；结构注册表、专用结构测试与迁移 runner 尚未接入，见 [配套文件与工具](Docs/AI/reference-alignment.md)。不自动创建这些工具，不以文件存在冒充验证通过。
 
 ## 待办备忘
 
@@ -271,7 +276,7 @@
 
 ## 代码规范（摘要）
 
-完整实现约定：[references/business-code-conventions.md](references/business-code-conventions.md)。独立 C# 规范手册尚缺，见 [配套文件与工具](Docs/AI/reference-alignment.md)。写代码时对齐周围文件，并遵守：
+完整实现约定：[references/business-code-conventions.md](references/business-code-conventions.md)；命名、组织与提交前检查见已迁入 [C# 代码规范](Book/C%23%20代码规范.md)。写代码时对齐周围文件，并遵守：
 
 - 类型/方法/public：`PascalCase`；局部参数：`camelCase`
 - GF 实例私有字段：`m_PascalCase`；静态私有：`s_PascalCase`；ET 与生成字段按本项目现有模式，不机械重命名存量
@@ -287,13 +292,13 @@
 
 ## Unity Pipeline 与场景刷新
 
-Unity Editor 状态、编译、测试和截图统一通过 Unity CLI/Pipeline。当前 CLI `1.0.0-beta.11`，项目固定 `com.unity.pipeline@0.8.0-exp.1`；项目 Editor 为 `6000.3.18f1`。用法见 [Unity CLI](Book/UnityCLI.md)，专用技能待逐项审批。
+Unity Editor 状态、编译、测试和截图统一通过 Unity CLI/Pipeline。当前 CLI `1.0.0-beta.11`，项目固定 `com.unity.pipeline@0.8.0-exp.1`；项目 Editor 为 `6000.3.18f1`。加载官方 [unity-cli](.agents/skills/unity-cli/SKILL.md)，用法见 [Unity CLI](Book/UnityCLI.md)；启动见 [启动与验证 SOP](Book/Unity启动与验证SOP.md)，资源刷新见 [ResourceCollection SOP](Book/ResourceCollection导出SOP.md)，诊断见 [错误 SOP](Book/Unity错误诊断SOP.md)。
 
 ### 编辑器单轨：Unity Pipeline
 
 - 默认不用 MCP；已授权的 CLI/Pipeline 接入无需重复询问，实际命令和参数以 `unity list`、`unity command --help` 与当前实例为准。
 - 用 `unity pipeline list` 发现实例并按 `projectPath` 精确匹配；显式传 `--project-path`，不固定端口，不记录描述文件中的 token。
-- 新技能、测试/打包验收或端口治理配套仍先给用途、必要性、依赖、改动与验收，逐项审批。
+- 本轮已授权的技能、自动测试框架和 SOP 按现有入口执行，不重复申请接入授权；新增配套仍先给用途、必要性、依赖、改动与验收，逐项审批。打包流程见 [包体验收 SOP](Book/打包与包体验收SOP.md)，收尾见 [OpenSpec 完成与归档 SOP](Book/OpenSpec完成与归档SOP.md)。
 - 按真实环境、风险与当轮任务执行；安装声明、连接成功、编译通过与玩法验收分别报告，缺能力记 `NotRun`。
 
 ### 改代码后的编译验证（必需）
```

来源与每项复制、适配、删除、新增说明见 [迁移来源](testing-sop-migration.md)。
