# Proposal

## Why

行为测试证明规则执行，仍缺乏新玩家卡点和经营选择的反馈。需要一次有范围的试玩及参数调校，使已完成机制形成可理解的三关体验。

## What Changes

- 观察首单、补货、伙伴接续、结算和三关结束的真实理解情况。
- 仅调整已有菜单不变条件下的来客、耗时、供货量及评分阈值，记录动机和代表性结果。
- 必要提示文案在既有界面修改；结构问题与新规则回原对应案，不扩展本案。

目标模式：Solo 试玩与调参。层次：三：完整一轮经营；依赖按[整体路线](../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不新增埋点平台、统计门槛、经济系统、新菜或 UI 结构；不以两三人反馈宣称普遍留存结论。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

无。

本案不改变既有规格合同，已显式设置 `skip_specs: true`；不为文档、夹具或原规则内调参制造新要求。

## Impact

- [Docs/Product/design.md](../../../Docs/Product/design.md)
- [Docs/Product/backlog.md](../../../Docs/Product/backlog.md)
- [Design/Excel/ET/Datas/Product](../../../Design/Excel/ET/Datas/Product)
- [Unity/Assets/Tests/Editor/ET/SoloLevelTests.cs](../../../Unity/Assets/Tests/Editor/ET/SoloLevelTests.cs)
- [Unity/Assets/Tests/Editor/ET/SoloSupplyTests.cs](../../../Unity/Assets/Tests/Editor/ET/SoloSupplyTests.cs)

无依据调参可能制造无法完成的订单或掩盖问题。先记录实际卡点，再选择少量参数；配置检查与一次对应场景复玩足够，规则未变结果复用。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮仅授权规划，功能／UTF／构建均 NotRun；不执行提交、归档或第二工作区操作。

