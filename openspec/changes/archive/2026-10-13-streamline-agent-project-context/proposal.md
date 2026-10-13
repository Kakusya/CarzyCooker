# Proposal

## Why

当前 AGENTS、产品入口与模块索引重复维护进度，已出现协议、供应、装修和分支描述滞后。集中动态事实能降低后续 Agent 误判与重复读取成本。

## What Changes

- 将 AGENTS 收敛为稳定纪律、架构边界与技能索引，保留现场保护、授权、UTF 和第二工作区职责。
- 当前能力集中到根进度，任务与最新验收集中在原 change；产品资料只维护目标与候选。
- 按已有交付点提出分批提交和归档清单，但实际提交、push、归档另按授权。

目标模式：文档。层次：一：资料与验证基础；依赖按[整体路线](../../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不改技能内容、业务源码、主规格行为；不创建管理平台或第二份状态台账。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

无。

本案不改变既有规格合同，已显式设置 `skip_specs: true`；不为文档、夹具或原规则内调参制造新要求。

## Impact

- [AGENTS.md](../../../../AGENTS.md)
- [CarzyCooker当前进度.md](../../../../CarzyCooker当前进度.md)
- [Docs/AI/module-index.md](../../../../Docs/AI/module-index.md)
- [Docs/AI/workflow.md](../../../../Docs/AI/workflow.md)
- [Docs/Product/README.md](../../../../Docs/Product/README.md)

主要风险是压缩文档时丢掉有效纪律或把历史状态误写成当前能力。用约束逐项对照、正文与引用检查即可；纯文档不运行 UTF。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮仅授权规划，功能／UTF／构建均 NotRun；不执行提交、归档或第二工作区操作。

