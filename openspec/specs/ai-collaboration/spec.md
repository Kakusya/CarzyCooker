# ai-collaboration Specification

## Purpose

记录 CarzyCooker 的 Codex 协作入口、模块和技能路由、产品资料状态及 OpenSpec 生命周期，保证静态文档与当前源码证据相符，保留用户现场和授权边界，不以迁移文档宣称未来玩法已实现。

## Requirements

### Requirement: 单一 Codex 路由

本项目 MUST 以 AGENTS.md 路由到适用模块、框架手册、实现约定、OpenSpec 规格和首批四技能；只维护 Codex 接入，不保留活跃旧工作流的 hooks、代理或入口。

#### Scenario: 开始一个项目任务

- **WHEN** Codex 读取根入口并选择模块
- **THEN** 能沿有效本项目引用找到源码、规范与技能，不要求已删除工作流或其他代理

### Requirement: 产品资料状态与原件

项目文档 MUST 保留两份原件，并区分已确认、授权补足、候选、待裁决；未来做饭设计不得写成已实现能力，正式文档不得依赖旧工程路径。

#### Scenario: 阅读菜单或冲突设计

- **WHEN** 文档出现候选目录、暂停命令、断线手持物或传输选择
- **THEN** 候选不晋级，冲突保持待裁决，当前源码状态不能代替用户决定

### Requirement: 工具和证据范围

Codex MUST 遵守当轮授权；Unity CLI 与项目 Pipeline 已获接入授权，新增或进一步改造的技能仍须逐项给出用途、必要性、依赖、文件清单和验收并获得批准。默认不用 MCP，不自动增加测试、打包或端口治理管线，不执行 SHA/hash 校验或新增 JSON 证据/全量状态副本。

#### Scenario: 迁移文档遇到编辑器或运行时验证需求

- **WHEN** 当前任务仅授权协作体系迁移
- **THEN** 只完成静态与 OpenSpec 验证，产品构建和玩法测试记 NotRun，不新增管线

#### Scenario: 已授权安装与后续技能审批

- **WHEN** 当前任务已授权 CLI 接入和 Editor 环境核实
- **THEN** 完成依赖与连接验证，新增技能只呈现提案，不重复请求已有授权；产品构建和玩法测试仍记 NotRun

### Requirement: OpenSpec 生命周期与实际验证

项目 MUST 提供探索、提案、实施、调整、验证、规格同步和归档的 Codex skills；中文规格保留英文结构和 SHALL/MUST，交付记录真实验证范围，不自动提交或推送。

#### Scenario: 完成迁移变更

- **WHEN** 入口、技能、源码基线与清理全部完成
- **THEN** 通过严格格式与引用检查，再同步并归档；格式通过不解释为玩法/联网成功

### Requirement: 保留经验证的协作规则

AGENTS.md MUST 尽量保留参考入口的适用结构与原文，包含需求讨论前实际加载探索技能、按阶段主动加载、中文规划与沟通、文档冲突先说明及实现同步文档；项目架构、业务、路径与工具状态按本项目事实适配，不因压缩入口丢弃通用纪律。

#### Scenario: 讨论新的做饭行为

- **WHEN** 用户提出新行为、询问方案取舍或带着改变期望讨论玩法
- **THEN** 在分析前读取 openspec-explore；复杂需求在探索后收敛并输出决策账本，未获授权不自动生成提案或实施

#### Scenario: 对照发现参考配套尚缺

- **WHEN** 通用规则涉及本项目尚未接入的工具或配套文件
- **THEN** 保留规则主题，标明缺口并与用户讨论，不声明已安装、不自动引入管线；已存在的 OpenSpec 技能继续作为正式路由

### Requirement: 配套文档唯一正文

项目 MUST 提供 KnownIssues.md、long-term-goals.md、references/ 和当前进度入口；实现约定与已知问题只维护一份正文，旧路径保留有效跳转。未来设计不晋级为已有玩法，历史迁移范围不作为后续授权任务的永久禁令。

#### Scenario: 从旧约定路径开始任务

- **WHEN** Codex 读取原 Docs/Development 页面
- **THEN** 可到达 references 或根 KnownIssues 的唯一正文，并从入口找到适用源码、规格与技能

## Sources

- [协作入口](../../../AGENTS.md)
- [工作流](../../../Docs/AI/workflow.md)
- [模块索引](../../../Docs/AI/module-index.md)
- [产品资料状态](../../../Docs/Product/README.md)
- [参考对齐记录与缺口](../../../Docs/AI/reference-alignment.md)
- [实现约定入口](../../../references/README.md)
- [已知问题](../../../KnownIssues.md)
- [长期目标](../../../long-term-goals.md)
- [当前进度](../../../CarzyCooker当前进度.md)
