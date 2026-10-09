# ai-collaboration 增量

## Purpose

记录 CarzyCooker 的 Codex 协作入口、模块和技能路由、产品资料状态及 OpenSpec 生命周期，保证静态文档与当前源码证据相符，保留用户现场和授权边界，不以迁移文档宣称未来玩法已实现。

## ADDED Requirements

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

Codex MUST 遵守当轮授权；新配套工具先提出用途、必要性、依赖、文件清单和验收，获得批准再实施。默认不用 MCP/Bridge，不自动迁入编辑器、测试、打包或端口管线，不执行 SHA/hash 校验或新增 JSON 证据/全量状态副本。

#### Scenario: 迁移文档遇到编辑器或运行时验证需求

- **WHEN** 当前任务仅授权协作体系迁移
- **THEN** 只完成静态与 OpenSpec 验证，产品构建和玩法测试记 NotRun，不新增管线

### Requirement: OpenSpec 生命周期与实际验证

项目 MUST 提供探索、提案、实施、调整、验证、规格同步和归档的 Codex skills；中文规格保留英文结构和 SHALL/MUST，交付记录真实验证范围，不自动提交或推送。

#### Scenario: 完成迁移变更

- **WHEN** 入口、技能、源码基线与清理全部完成
- **THEN** 通过严格格式与引用检查，再同步并归档；格式通过不解释为玩法/联网成功
