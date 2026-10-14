# Proposal
## 2026-10-14 用户试玩豁免

用户对“真实新玩家操作记录”的安排答复“忽略这项”。本轮跳过新玩家理解、求助点与再次经营意愿观察，标记 Skipped（用户豁免）；实际效果仍未验证。UTF 和普通 Launcher 操作验收继续，不把脚本当人类理解证据。试玩调校仅针对可复现的实际操作问题；没有依据时不改参数。此决策覆盖下文原规划中要求无玩家仍待完成的条款，历史记录保留。

## Why

行为测试证明规则执行，仍缺乏新玩家卡点和经营选择的反馈。需要一次有范围的试玩及参数调校，使已完成机制形成可理解的三关体验。

## What Changes

- 观察首单、补货、伙伴接续、结算和三关结束的真实理解情况。
- 仅调整已有菜单不变条件下的来客、耗时、供货量及评分阈值，记录动机和代表性结果。
- 必要提示文案在既有界面修改；结构问题与新规则回原对应案，不扩展本案。

目标模式：Solo 试玩与调参。层次：三：完整一轮经营；依赖按[整体路线](../../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不新增埋点平台、统计门槛、经济系统、新菜或 UI 结构；不以两三人反馈宣称普遍留存结论。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

无。

本案不改变既有规格合同，已显式设置 `skip_specs: true`；不为文档、夹具或原规则内调参制造新要求。

## Impact

- [Docs/Product/design.md](../../../../Docs/Product/design.md)
- [Docs/Product/backlog.md](../../../../Docs/Product/backlog.md)
- [Design/Excel/ET/Datas/Product](../../../../Design/Excel/ET/Datas/Product)
- [Unity/Assets/Tests/Editor/ET/SoloLevelTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloLevelTests.cs)
- [Unity/Assets/Tests/Editor/ET/SoloSupplyTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloSupplyTests.cs)

无依据调参可能制造无法完成的订单或掩盖问题。先记录实际卡点，再选择少量参数；配置检查与一次对应场景复玩足够，规则未变结果复用。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。2026-10-14 已授权实施并完成工程复玩，未额外改数值；人类理解和意愿按用户豁免保留 Skipped。未执行产品构建、提交、归档或第二工作区操作。
