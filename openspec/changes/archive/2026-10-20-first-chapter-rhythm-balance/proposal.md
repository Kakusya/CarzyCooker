# Proposal

## Why

正式第一章「家常开张」Solo 三关目前营业 25/32.5/40 秒，订单等待 15/14/13 秒；第一关汤的基础加工就有切配、打蛋、煮制等环节。现有参数尚缺面向初学者的完整游玩验证，本案先给一章足够学习与处理并行工作的空间。

## What Changes

- 只调正式第一章 Solo 1101–1103 的 `OperatingTicks` 和对应 `CustomerFlowConfig` 来客/等待参数；设计给出可直接实施的首轮值及简短调参判据。
- 三关分别承载学会汤品交付、加入吐司并行生产、加入沙拉后完成收尾。菜单、关链、星级/奖励规则与既有生产方式保持。
- 本轮保留共享加工时长、有限供应和初始布局，通过真实路线验证其可用性；不为调一章引入每关配方覆盖机制或改全菜单数据。
- 新建和恢复到这些关卡时使用当前生效节奏；保留旧档中的下一关、实物、布局、库存和既有完成记录，不重算旧成绩。
- 明确保留零星自然成功、无普通失败、终关无第四关。合作 2101–2103、独立试玩 201 系列及其他章节不在调参范围。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-chapters`：增加第一章 Solo 的学习节奏与作用范围。

## Impact

- 配置源仅 `Design/Excel/ET/Datas/Product/LevelConfig.xlsx` 和 `CustomerFlowConfig.xlsx` 的六条对应记录；沿 active ET Luban 五目标导出，禁止手改生成物。
- 本案不依赖新玩法框架；建议在 [首局路径](../2026-10-20-product-first-session-path/proposal.md) 与 [情境动作](../2026-10-20-kitchen-contextual-actions/proposal.md) 可用后做一次整体体验验收，避免用时长补偿不可理解的操作。
- 参数易恢复，主要风险是把数值放宽当成已证实的好玩、影响非目标关卡。检查精确数据差异，执行三关生产/自然收尾和一个旧档恢复小集；无需全仓测试或统计实验。见 [execution-notes.md](execution-notes.md)。
- 2026-10-20：后续已按用户实施授权完成4/4任务、两张源表12字段的正常导出及正常计时三关验收，详见[执行记录](execution-notes.md)。未取得新手样本，不宣称平衡成熟；已归档并获授权本地提交，未推送。
