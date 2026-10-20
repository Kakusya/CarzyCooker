# 模块索引

## 当前模块入口

主工作区基线为 main，来自原 ET 底座。动态能力、协议版本和验收环境统一查[当前进度](../../CarzyCooker当前进度.md)；本页维护源码、约定、规格和技能定位，不复制任务与测试计数。先读[实现约定](../../references/business-code-conventions.md)，再读适用行和实际源码；技能适用不增加运行授权。

- 稳定启动配置与 Editor 入口：Game/Base/RuntimeInstance、Game/Editor/Multiplayer 和既有 SceneHelper。Prepared 只表示配置交接，不能证明产品就绪。用法见[快速开始](../../Book/快速开始.md)与[启动 SOP](../../Book/Unity启动与验证SOP.md)；历史接入证据见[启动记录](../../openspec/changes/archive/2026-10-11-multiplayer-instance-bootstrap/execution-notes.md)。
- 产品数据在[Model/Share/Product](../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product)，建树、共同裁决、客户端、wire 和生命周期在[Hotfix/Share/Product](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product)。ProcedureET → Loader/Entry → ProductRoot；根退出沿既有 HotfixView OnShutdown／Loader 交接。
- 当前 Host/Solo：ProductHost → Match → RestaurantRuntime；Match 持成员，Restaurant 持持续 GridKitchenComponent 和当前 Level。HostSession 引用权威，ClientSession.GridClientComponent 持独立投影。核对[实际建树](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)、[餐厅 owner](../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs)及[消息约定](../../references/reactive-and-network.md)。旧样机的 Match 厨房不能作为当前餐厅 owner；[旧样机记录](../../openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/execution-notes.md)只证明其历史范围。
- 表现在[ModelView/Client/Product](../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product)和[HotfixView/Client/Product](../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product)，沿原受管 Board／HUD owner。协议源为 Design/Proto/ET-Client/ZProductSession.proto、ZZGridKitchen.proto 及同域产品协议；按既有工具生成 Client/ClientServer，不手改生成物。
- 餐厅美术源与导出在 [Art/CarzyCooker](../../Art/CarzyCooker/README.md)，实际图片沿原 Product Board／HUD Prefab 引用；方向、内容图层、座位与切片释放见 [美术约定](../../references/restaurant-art.md)，本轮验收查 [执行说明](../../openspec/changes/archive/2026-10-15-restaurant-art-production/execution-notes.md)。
- 经营短音源见[Audio](../../Art/CarzyCooker/Audio/README.md)；[ProductFeedbackSystem](../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductFeedbackSystem.cs) 在ProductClient观察已提交结果并管理所属GF播放serial，[ProductAudioSettings](../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductAudioSettings.cs) 管理即时本机声音页。主契约见[restaurant-feedback](../../openspec/specs/restaurant-feedback/spec.md)，当前验收见[声画案](../../openspec/changes/archive/2026-10-20-restaurant-feedback-audio-pass/execution-notes.md)。
- 本地 sender／队列与请求收尾：ET.Core、Hotfix/Share/Module/Message；整体 Stop 不作为单 Peer 取消接口。测试在 Unity/Assets/Tests/Editor/ET 的 PlayMode 程序集，目录名不代表模式；发现与执行见[自动测试 SOP](../../Book/自动化测试SOP.md)。已有收尾证据见[原记录](../../openspec/changes/archive/2026-10-11-local-message-request-shutdown/execution-notes.md)，后续消息迁移以其原 change 记录为准。
- 第二工作区按 multiplayer-baseline 分支发现，只执行、观察、回报和指定版本维护，禁止开发。职责与技能范围唯一查[共用规程](multiplayer-peer-workflow.md)和[维护技能](../../.agents/skills/second-workspace-sync/SKILL.md)；原精简证据见[记录](../../openspec/changes/archive/2026-10-11-simplify-second-workspace-executor/execution-notes.md)。

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

## 历史规格扫描与技能迁移

2026-10-09 继续扫描并细化十四项底座规格，补充模式、异常、取消、清理和兼容边界。十六份主规格当前有 74 项要求、120 个场景；扫描范围、模块与源码对应、保留原文及修改原因见 [深度扫描](spec-scan.md)。这里的数量属于本轮完成记录，后续能力清单以 OpenSpec CLI 为准；静态场景不等于动态测试已执行。

OpenSpec 官方技能已补齐至十二项，完整入口见 [AGENTS 技能加载](../../AGENTS.md#技能加载) 与 [工作流](workflow.md)。规划、实施、验证和归档按各自职责调用，新增教学或批量入口不自动授权业务实现。

适用 [生命周期](../../references/framework-lifecycle.md)、[生成链路](../../references/data-generation.md)、[已知问题](../../KnownIssues.md)。初始迁移只作静态检查；后续已授权接入 [Unity CLI/Pipeline](../../Book/UnityCLI.md)，包解析、连接和编译实际结果见手册。现有 ET Test/RobotCase 仅作定位，产品测试验收和后续 [技能改造](skill-roadmap.md) 逐项批准；未运行项保持 NotRun。

## 测试与 SOP 入口

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

## 所属异步生命周期

公共机制为 `Game/Base/AsyncScope.cs`，资源在 `Game/Container/ResourceContainer.cs`，ET 适配在 `ET/Loader/UGF/AsyncOwnerComponent.cs`，请求值化/原 Fiber 延时在 `ET/Code/Hotfix/Share/Module/Message/OwnedRequestExtensions.cs`。这是有限迁移，未覆盖任意旧 async；用法见 [生命周期](../../references/framework-lifecycle.md)，27个新增 UTF 与真实 AssetBundle 范围见 [执行说明](../../openspec/changes/archive/2026-10-11-manage-owned-async-lifecycle/execution-notes.md)。
