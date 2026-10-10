## MODIFIED Requirements

### Requirement: 工具和证据范围

Codex MUST 遵守当轮授权；Unity CLI 与项目 Pipeline 已获接入授权，新增或进一步改造的技能仍须逐项给出用途、必要性、依赖、文件清单和验收并获得批准。默认不用 MCP，不自动增加测试、打包或端口治理管线，不执行 SHA/hash 校验或新增 JSON 证据/全量状态副本。

#### Scenario: 迁移文档遇到编辑器或运行时验证需求

- **WHEN** 当前任务仅授权协作体系迁移
- **THEN** 只完成静态与 OpenSpec 验证，产品构建和玩法测试记 NotRun，不新增管线

#### Scenario: 已授权安装与后续技能审批

- **WHEN** 当前任务已授权 CLI 接入和 Editor 环境核实
- **THEN** 完成依赖与连接验证，新增技能只呈现提案，不重复请求已有授权；产品构建和玩法测试仍记 NotRun
