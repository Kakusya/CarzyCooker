# Design

## Context

当前可运行能力与文档中“规划/未实现”的描述已不完全一致；本轮核对时协议为 17，供应、装修、存档与合作核心 tasks 均已勾完，完整合作仍有后续案。这里仅设计资料整理，不把任务勾选或片段运行提升为设备验证。 动机见 [proposal.md](proposal.md)。本案为待实施规划。

## Goals / Non-Goals

目标模式：文档。不改技能内容、业务源码、主规格行为；不创建管理平台或第二份状态台账。

依赖：无玩法硬前置。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 按读者问题分配唯一正文

AGENTS 保留稳定纪律、架构边界与技能路由；当前进度维护已落地范围；OpenSpec 保留合同、tasks 和执行记录；产品目录保留未来方向。动态数字通过引用取得，不再各处复制。可选方案是继续加长 AGENTS，但会放大过期风险。

### 2. 压缩前先保护约束

逐段确认授权、保护用户文件、第二工作区只执行、UTF 实际验收、ET 消息与 owner、生成物规则仍可从入口找到。删掉重复描述不等于删除规则，也不修改技能本身。

### 3. 让版本操作可审阅

按已验证交付点列出建议提交/归档集合、包含与排除路径及缺失证据；只给清单。main 必要提交选择、第二工作区同步、实际提交与归档仍依用户相应授权。历史失败保留，新增“被哪次结果替代”引用，避免直接抹掉失败。

### 状态归属与交接路径

纯资料交接，不新增运行树、消息、工具或持久状态。交付给后续 Agent 的是权威路径和边界；进行中的任务仍以其原 tasks 与执行记录为准。

## Risks / Trade-offs

主要风险是压缩文档时丢掉有效纪律或把历史状态误写成当前能力。用约束逐项对照、正文与引用检查即可；纯文档不运行 UTF。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [AGENTS.md](../../../../AGENTS.md)
- [CarzyCooker当前进度.md](../../../../CarzyCooker当前进度.md)
- [Docs/AI/module-index.md](../../../../Docs/AI/module-index.md)
- [Docs/AI/workflow.md](../../../../Docs/AI/workflow.md)
- [Docs/Product/README.md](../../../../Docs/Product/README.md)
- [Docs/Product/backlog.md](../../../../Docs/Product/backlog.md)
- [openspec/specs/ai-collaboration/spec.md](../../../specs/ai-collaboration/spec.md)
