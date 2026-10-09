# OpenSpec 完成与归档 SOP

已有官方 verify/sync/archive 技能是正式入口；本页补参考的收尾检查顺序，不复制技能正文，也不新增 verify-change.ps1。

1. `openspec instructions apply --change <name> --json` 核对实际 tasks 和 contextFiles。任务勾选必须对应已实现行为，不能以历史授权或文档存在冒充完成。
2. 加载 [verify-change](../.agents/skills/openspec-verify-change/SKILL.md)，对照 proposal/design/delta/tasks、源码、表、配置与实际验证结果。区分格式、编译、行为、AOT、联网和构建范围。
3. 失败或未完成项先修；明确报告 NotRun/Skipped 及其对验收的影响，不改写失败结果。任务之外的实现不偷偷并入。
4. 执行 `openspec validate <name> --strict --no-interactive`，检查本项目引用；格式通过不代表产品行为通过。
5. 加载 [sync-specs](../.agents/skills/openspec-sync-specs/SKILL.md)，将已落实的 delta 合并到主规格，保留未涉及内容，不把候选/草案同步为既有能力。
6. 用户请求归档或当前任务范围已含归档时加载 [archive-change](../.agents/skills/openspec-archive-change/SKILL.md)，核对完成、同步与冲突后正常归档。批量归档用已有官方技能。
7. 全量严格验证并同步相关文档/进度/待办；原始日志留原位，不提交证据包、缓存或用户现场，不自动 commit/push。

工作流见 [Docs/AI/workflow](../Docs/AI/workflow.md)。需要额外校验脚本时先说明增量价值与范围，不照搬参考专属废弃词清单。
