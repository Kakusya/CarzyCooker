## Why

现有协作入口仍指向已删除的旧工作流，框架手册与未来产品资料混在同一入口。用户已批准将 Codex 协作迁移到 OpenSpec，并要求保留原件和现场、不实施玩法。

## What Changes

- 重写 AGENTS.md：协作约束、文档分层、模块与技能路由、授权后才使用 Bridge。
- 安装固定 OpenSpec 1.14.1，只生成 Codex skills，覆盖探索、提案、实施、调整、验证、同步和归档。
- 适配需求拷问、Luban、UI、UGFEntity 四个项目技能，再扫描全仓文本源码/配置/工具/文档。
- 从实际底座建立中文主规格，保留两份原件，未来产品维持四种状态与未决边界。
- 迁出有价值的旧约定，最后清除项目旧技能、代理、hooks 和路由，核对全局安装。

## Capabilities

### New Capabilities

- `ai-collaboration`: Codex 文档路由、四种资料状态、工具授权与 OpenSpec 变更生命周期。

### Modified Capabilities

无运行时能力变更。其他主规格是从现有源码建立的底座基线，不是新增玩法。

## Impact

修改协作文档、项目 skills 和 Codex 配置；移除旧接入。机器全局只安装已授权 OpenSpec 并选择七项工作流。现有 Bridge 包保持，不新增配套工具或管线。产品构建、玩法测试、服务启动、SHA 校验、额外 JSON 证据、提交和推送均不在范围。
