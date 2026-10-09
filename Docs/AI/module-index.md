# 模块索引

当前基线为 `main` 分支，来自原 ET 底座。先读 [实现约定](../../references/business-code-conventions.md)，再读适用行和实际源码。表内路径从本文解析；源码目录定位整个模块，不只一个示例。技能适用不代表获准启动工具、服务或编辑器。

| 模块 | 源码/资料入口 | 用法/约定 | 主规格 | 技能 |
|---|---|---|---|---|
| 启动/模式 | [Unity/Assets/Scripts/Game/Procedure](../../Unity/Assets/Scripts/Game/Procedure) | [快速开始.md](../../Book/快速开始.md) | [startup](../../openspec/specs/startup/spec.md) | [openspec-explore](../../.agents/skills/openspec-explore/SKILL.md) |
| 公共组件/流程/示例 | [Unity/Assets/Scripts/Game/Base](../../Unity/Assets/Scripts/Game/Base) | [Project结构.md](../../Book/Project结构.md) | [runtime-foundation](../../openspec/specs/runtime-foundation/spec.md) | [openspec-apply-change](../../.agents/skills/openspec-apply-change/SKILL.md) |
| ET Model/Hotfix/View/共享 | [Unity/Assets/Scripts/Game/ET/Code](../../Unity/Assets/Scripts/Game/ET/Code) | [ET动态事件.md](../../Book/ET动态事件.md) | [runtime-foundation](../../openspec/specs/runtime-foundation/spec.md) | [openspec-apply-change](../../.agents/skills/openspec-apply-change/SKILL.md) |
| GF UI/ETUI/Widget | [Unity/Assets/Scripts/Game/UI](../../Unity/Assets/Scripts/Game/UI) | [UI开发.md](../../Book/UI开发.md) | [ui](../../openspec/specs/ui/spec.md) | [carzycooker-ui](../../.agents/skills/carzycooker-ui/SKILL.md) |
| GF Entity/ETEntity/UIEntity | [Unity/Assets/Scripts/Game/Entity](../../Unity/Assets/Scripts/Game/Entity) | [Entity开发.md](../../Book/Entity开发.md) | [entity](../../openspec/specs/entity/spec.md) | [carzycooker-ugfentity](../../.agents/skills/carzycooker-ugfentity/SKILL.md) |
| 容器/资源/AssetSet | [Unity/Assets/Scripts/Game/Container](../../Unity/Assets/Scripts/Game/Container) | [AssetSet.md](../../Book/AssetSet.md) | [resource-management](../../openspec/specs/resource-management/spec.md) | [openspec-apply-change](../../.agents/skills/openspec-apply-change/SKILL.md) |
| Excel/Luban/生成 ID | [Share/Tool/ExcelExporter](../../Share/Tool/ExcelExporter) | [Luban配置.md](../../Book/Luban配置.md) | [data-generation](../../openspec/specs/data-generation/spec.md) | [carzycooker-luban](../../.agents/skills/carzycooker-luban/SKILL.md) |
| Proto/Opcode/消息生成 | [Share/Tool/Proto2CS](../../Share/Tool/Proto2CS) | [Proto生成工具.md](../../Book/Proto生成工具.md) | [proto-generation](../../openspec/specs/proto-generation/spec.md) | [openspec-apply-change](../../.agents/skills/openspec-apply-change/SKILL.md) |
| 本地化/UX 文本 | [Unity/Assets/Scripts/Game/Localization](../../Unity/Assets/Scripts/Game/Localization) | [多语言.md](../../Book/多语言.md) | [localization](../../openspec/specs/localization/spec.md) | [carzycooker-luban](../../.agents/skills/carzycooker-luban/SKILL.md) |
| ET Session/RPC/HTTP | [ET 消息模块](../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message) | [reactive-and-network.md](../../references/reactive-and-network.md) | [network](../../openspec/specs/network/spec.md) | [openspec-explore](../../.agents/skills/openspec-explore/SKILL.md) |
| 服务端/DB/Agent/Admin | [DotNet](../../DotNet) | [管理后台.md](../../Book/管理后台.md) | [server](../../openspec/specs/server/spec.md) | [openspec-apply-change](../../.agents/skills/openspec-apply-change/SKILL.md) |
| HybridCLR/程序集/打包 | [Unity/Assets/Scripts/Game/HybridCLR](../../Unity/Assets/Scripts/Game/HybridCLR) | [HybridCLR热更.md](../../Book/HybridCLR热更.md) | [hot-reload](../../openspec/specs/hot-reload/spec.md) | [openspec-explore](../../.agents/skills/openspec-explore/SKILL.md) |
| Editor/Toolbar/骨架/服务工具 | [Unity/Assets/Scripts/Game/Editor](../../Unity/Assets/Scripts/Game/Editor) | [ET代码生成工具.md](../../Book/ET代码生成工具.md) | [editor-tools](../../openspec/specs/editor-tools/spec.md) | [openspec-explore](../../.agents/skills/openspec-explore/SKILL.md) |
| ET reactive/外部 ReactiveBinding | [Share/SourceGenerator](../../Share/SourceGenerator) | [reactive-and-network.md](../../references/reactive-and-network.md) | [reactive-ui](../../openspec/specs/reactive-ui/spec.md) | [carzycooker-ui](../../.agents/skills/carzycooker-ui/SKILL.md) |
| Analyzer/第三方/程序集/包 | [Unity/Assets/Scripts/Library](../../Unity/Assets/Scripts/Library) | [conventions.md](../../references/business-code-conventions.md) | [dependencies](../../openspec/specs/dependencies/spec.md) | [openspec-explore](../../.agents/skills/openspec-explore/SKILL.md) |
| 未来产品/需求/候选 | [Docs/Product](../../Docs/Product) | [README.md](../../Docs/Product/README.md) | [ai-collaboration](../../openspec/specs/ai-collaboration/spec.md) | [carzycooker-requirements](../../.agents/skills/carzycooker-requirements/SKILL.md) |

## 模块内进一步定位

- 启动精读 ProcedurePreset、ProcedureET、ET/Loader/Init 与 CodeLoader；当前 ProcedurePreset 直接进入 ET，不含 GameHot/纯 GF 示例分支。
- ET Code 的 Model/Hotfix 下 Client、Server、Share 按业务分层；ModelView/HotfixView 仅客户端 view。Demo、LockStep、Benchmark、RobotCase、AOI、Move、Numeric、Recast 等是底座示例/模块，非做饭实现。
- ETUI 与 ETEntity owner 在 ET/Code/ModelView/Client/Module/UI 与 GFEntity；桥接在 ET/Loader/UGF。具体文件已列入 UI/Entity 主规格 Sources。
- 服务端 DotNet/App、Loader、Model、Hotfix 的 csproj 会链接 Unity ET 源。Config/Luban、NLog、Recast、Design/Excel 和 Design/Proto 需一起核对，Admin/Agent/Watcher 是现有运维能力，迁移不启动它们。
- Share/Analyzer 与 SourceGenerator 是编译诊断/生成；Share/Tool 是导表与 Proto；Share/FileServer、Share/Aspire 与 Tools/Shell 是既有开发服务/脚本，不因存在就执行。
- Library 包括 ET/Core/ThirdParty、UGF/GameFramework/UnityGameFramework/Extension、LubanLib、Extension、UXTool、SocoTool、FolderTag、ReplaceComponent。Unity/Assets/Plugins 与 Packages 另有包/二进制依赖，读取配置而不扫描二进制正文。
- Res 的 UI、Entity、ET、Luban、Localization、Editor 模板与 Launcher 场景由对应 owner 使用。元数据、Prefab/场景 YAML 只作配置事实，不用它们推断运行成功。

## 扫描方法和界限

技能融入后按路径全仓枚举，读取可解码文本源码、生成源码、配置、工具与文档，并检查 Unity 文本设置/元数据。排除 Git 对象、Library/PackageCache、Temp、Bin/bin/obj、构建/发布/日志/用户设置缓存、node_modules、HybridCLRData 和编辑器运行态描述文件；二进制素材只记录路径类别，不读正文。

切换前 main 分支的历史文本扫描读取 1212 个源码/配置/文档文件，排除 AI 接入目录后枚举 3925 条文件路径；另外读取 2061 个 Unity 文本设置/元数据/资产（1922 meta、64 asset、69 prefab、6 场景）。后续新增文档引用另外检查。这是静态覆盖，不声称逐行业务审计或动态验证。核心验收抽查启动、UI、Entity、Luban、网络和热更。

切换后的 ET 分支重新枚举 6794 条文件路径（排除协作资料目录和上述缓存），读取 2606 个源码/配置/工具/文档文本文件及 3644 个 Unity 序列化/元数据文件，其中 3492 个 meta、64 个 asset、48 个 prefab、4 个场景，其余为材质、动画和控制器。`Unity/Assets/Scripts/Library/` 是源码，纳入扫描；排除的 `Unity/Library/` 是缓存。两个无法按文本读取的文件只计路径，不读二进制正文。另检查 52 份维护中的 Markdown 和 409 个本地链接；历史归档保留原 main 范围，不用其描述推断 ET 当前能力。

## 质量与已有测试

OpenSpec 官方技能已补齐至十二项，完整入口见 [AGENTS 技能加载](../../AGENTS.md#技能加载) 与 [工作流](workflow.md)。规划、实施、验证和归档按各自职责调用，新增教学或批量入口不自动授权业务实现。

适用 [生命周期](../../references/framework-lifecycle.md)、[生成链路](../../references/data-generation.md)、[已知问题](../../KnownIssues.md)。初始迁移只作静态检查；后续已授权接入 [Unity CLI/Pipeline](../../Book/UnityCLI.md)，包解析、连接和编译实际结果见手册。现有 ET Test/RobotCase 仅作定位，产品测试验收和后续 [技能改造](skill-roadmap.md) 逐项批准；未运行项保持 NotRun。

## 已迁入测试与 SOP

| 范围 | 正文 / 工具 | 技能 |
|---|---|---|
| 启动、资源 | [启动](../../Book/Unity启动与验证SOP.md)、[资源导出](../../Book/ResourceCollection导出SOP.md) | [官方 CLI](../../.agents/skills/unity-cli/SKILL.md) |
| 自动测试 | [SOP](../../Book/自动化测试SOP.md)、[AutoTesting](../../Tools/AutoTesting/README.md)、[主规格](../../openspec/specs/automated-testing/spec.md) | [auto-testing-sop](../../.agents/skills/auto-testing-sop/SKILL.md) |
| 打包验收 | [SOP](../../Book/打包与包体验收SOP.md)；现有 BuildHelper，专用双包工具未接入 | [build-acceptance-sop](../../.agents/skills/build-acceptance-sop/SKILL.md) |
| 错误诊断 | [SOP](../../Book/Unity错误诊断SOP.md)；参考压缩器与当前 entries 适配 | [unity-error-extraction](../../.agents/skills/unity-error-extraction/SKILL.md) |
| 端口 | [SOP](../../references/port-management.md)；专用注册/租约工具未接入 | [port-management](../../.agents/skills/port-management/SKILL.md) |
| 动态 UI | [SOP](../../references/dynamic-ui-sop.md)；注册表/runner 未接入 | [carzycooker-ui](../../.agents/skills/carzycooker-ui/SKILL.md) |
| C# / 收尾 | [规范](../../Book/C%23%20代码规范.md)、[OpenSpec 完成归档](../../Book/OpenSpec完成与归档SOP.md) | 现有 apply/verify/sync/archive |

本轮来源及验证范围见 [SOP 迁移](testing-sop-migration.md)，AGENTS 精确修改见 [逐字审阅](testing-sop-agents-review.md)。
