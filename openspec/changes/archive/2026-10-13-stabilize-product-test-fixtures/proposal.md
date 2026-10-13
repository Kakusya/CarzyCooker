# Proposal

## Why

近期记录中有默认物品假设、投影尚未投递和重启上下文导致的夹具失败。沿用现有 UTF 改善这些重复点，可以缩短定位时间而不削弱行为断言。

## What Changes

- 在现有 ProductSessionTests 和 Solo/Coop fixture 中复用真实配置查找、受控消息泵与世界／投影就绪等待。
- 区分消息接受、领域提交和客户端可见状态；超时保留当前阶段与原失败。
- 只迁移已观察到重复问题的供应、装修、存档、核心合作夹具。

目标模式：测试维护。层次：一：资料与验证基础；依赖按[整体路线](../../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不改生产业务、Tools/AutoTesting 协议或 UTF 框架；不全面重写测试，不新增测试编排器。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

无。

本案不改变既有规格合同，已显式设置 `skip_specs: true`；不为文档、夹具或原规则内调参制造新要求。

## Impact

- [Unity/Assets/Tests/Editor/ET/ProductSessionTests.cs](../../../../Unity/Assets/Tests/Editor/ET/ProductSessionTests.cs)
- [Unity/Assets/Tests/Editor/ET/SoloSupplyTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloSupplyTests.cs)
- [Unity/Assets/Tests/Editor/ET/SoloSaveSelectionUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloSaveSelectionUiTests.cs)
- [Unity/Assets/Tests/Editor/ET/CoopCoreControlTests.cs](../../../../Unity/Assets/Tests/Editor/ET/CoopCoreControlTests.cs)
- [Tools/AutoTesting/README.md](../../../../Tools/AutoTesting/README.md)

测试可信性是实际风险。先保留原行为断言，按变动夹具执行最小 PlayMode 小集，并用代表性条件未满足情况确认等待会失败；不延长所有超时。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮仅授权规划，功能／UTF／构建均 NotRun；不执行提交、归档或第二工作区操作。

