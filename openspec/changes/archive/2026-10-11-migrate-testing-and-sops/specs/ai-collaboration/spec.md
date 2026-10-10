# Spec Delta

## ADDED Requirements

### Requirement: 已授权的测试与 SOP 入口

AGENTS MUST 路由到已迁入的自动测试、打包验收、错误诊断、启动验证、资源导出、代码规范、端口、动态 UI 和 OpenSpec 收尾流程。SOP 使用本项目真实入口；没有专用工具的流程须明确限制。用户明确授权迁移后不重复请求同一接入授权。

#### Scenario: 按 SOP 开始验证
- **WHEN** Codex 从入口选择一个已迁入的 SOP
- **THEN** 找到有效的唯一正文和适用技能，核实依赖后仅执行当轮授权范围，不以 SOP 存在扩大为玩法或产品构建

#### Scenario: 审阅迁移来源
- **WHEN** 用户审阅 AGENTS 或迁入技能
- **THEN** 能查到适用原文保留、项目路径/API 替换、删去的参考专属能力和新增规则的原因
