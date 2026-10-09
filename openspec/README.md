# 中文底座规格索引

这些主规格描述当前源码/配置支持的底座与协作契约，不描述做饭玩法已实现。底座以静态基线建立；后续 CLI 接入的实际验证见 [Unity CLI](../Book/UnityCLI.md)，产品构建与玩法验收仍为 NotRun。未来目标读 [产品文档](../Docs/Product/README.md)，模块路由读 [模块索引](../Docs/AI/module-index.md)。

2026-10-09 的 [深度扫描](../Docs/AI/spec-scan.md) 已细化十四项底座规格；合计十六份主规格、74 项要求、120 个场景。新增 31 项要求和 70 个场景，保留原要求；仅给固定 ET 入口补充实际编译条件。增量、来源和验证见 [deepen-foundation-specs](changes/archive/2026-10-09-deepen-foundation-specs/proposal.md) 与 [verification](changes/archive/2026-10-09-deepen-foundation-specs/verification.md)。场景是契约及后续验证目标，不代表这些动态场景已运行。

| 规格 | 范围 |
|---|---|
| [startup](specs/startup/spec.md) | 启动/模式 |
| [runtime-foundation](specs/runtime-foundation/spec.md) | 公共组件/流程/示例 |
| [ui](specs/ui/spec.md) | GF UI/ETUI/Widget |
| [entity](specs/entity/spec.md) | GF Entity/ETEntity/UIEntity |
| [resource-management](specs/resource-management/spec.md) | 容器/资源/AssetSet |
| [data-generation](specs/data-generation/spec.md) | Excel/Luban/生成 ID |
| [proto-generation](specs/proto-generation/spec.md) | Proto/Opcode/消息生成 |
| [localization](specs/localization/spec.md) | 本地化/UX 文本 |
| [network](specs/network/spec.md) | ET Session/RPC/HTTP |
| [server](specs/server/spec.md) | 服务端/DB/Agent/Admin |
| [hot-reload](specs/hot-reload/spec.md) | HybridCLR/程序集/打包 |
| [editor-tools](specs/editor-tools/spec.md) | Editor/Toolbar/骨架/服务工具 |
| [reactive-ui](specs/reactive-ui/spec.md) | ET reactive/外部 ReactiveBinding |
| [dependencies](specs/dependencies/spec.md) | Analyzer/第三方/程序集/包 |
| [ai-collaboration](specs/ai-collaboration/spec.md) | Codex 路由、授权、技能/SOP 与产品资料状态 |
| [automated-testing](specs/automated-testing/spec.md) | 项目测试发现、显式执行、终态和失败报告 |

工作流见 [Codex/OpenSpec](../Docs/AI/workflow.md)，当前配置见 [config.yaml](config.yaml)。每项要求附场景和来源；协作资料规格描述文档与授权行为，不作为玩法能力。
