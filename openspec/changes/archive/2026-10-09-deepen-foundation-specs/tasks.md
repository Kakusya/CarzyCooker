# Tasks

## 1. 源码核对与规格边界

- [x] 1.1 完成文本扫描和十四项增量的源码/场景对应核对，在 Docs/AI/spec-scan.md 记录范围、模块映射及静态验证方式。
- [x] 1.2 将资源回调、字典切片/部分写入、AOT 返回码等已确认限制迁入 KnownIssues.md，核对相应规格不宣称这些缺口已解决。

## 2. 主规格与入口同步

- [x] 2.1 将十四项增量同步到主规格，检查原要求/场景保留、唯一 MODIFIED 完整、每个新增场景与 Sources 对应。
- [x] 2.2 更新 openspec/README.md、模块索引、当前进度和直接相关 references，核对技能/配套现状及文档职责一致；AGENTS.md 和技能文件保持原样。

## 3. 综合静态验证

- [x] 3.1 完成 OpenSpec 严格验证、本轮文档引用检查、主规格与增量对应检查及 Git 差异/范围检查，在 change 的 verification.md 记录结果和产品执行 NotRun。

## Workflow follow-up

- 用户审阅后于 2026-10-09 授权收尾；change 已归档，提交与推送按该授权执行。
