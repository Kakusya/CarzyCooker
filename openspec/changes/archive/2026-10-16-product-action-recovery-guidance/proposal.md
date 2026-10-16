# Proposal

## Why

原反馈已区分接收、到位与终态，也翻译了许多拒绝原因，但默认分支仍可能显示内部 reason，且缺少通往解决当前问题的查看入口。

## What Changes

- 围绕手满、加工锁定、容器不符、不可达、生产许可、收货台满、旧目标及保存失败，提供可理解原因和对应查看入口；未知原因保留可复制诊断信息。
- 建议根据当前真实投影提供，不自动重试、丢弃物品、解绑、采购或改布局；查看帮助与实际业务动作清楚分开。旧命令结果不覆盖当前命令反馈，接收成功不冒称执行成功。
- 补齐正常使用、失效结果、界面重开及真实操作验收。

范围外：新输入设备支持、强制教程、自动重试、新业务权威、网络／文件改版、UI 结构改造及新工具。当前已按本案范围实施并实际验收，结果见 execution-notes；设备/物理网络边界保持。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-ui`：增加操作拒绝与处理导航，保持原事实反馈和业务操作合同。

## Impact

源码入口：[GridPresentationActionAdvice.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationActionAdvice.cs)及原GridPresentationSystem，沿原 ModelView／HotfixView、UIGridKitchen（1101）及原绑定实现。新增 [ActionAdviceUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/ActionAdviceUiTests.cs)，并在原供应、真实保存失败用例补充行为断言，使用普通 Launcher／真实 KCP 夹具。涉及共享详情区的案按序整合：[kitchen-action-navigation](../2026-10-16-kitchen-action-navigation/proposal.md)、[kitchen-supply-overview](../2026-10-16-kitchen-supply-overview/proposal.md)、[order-production-navigation](../2026-10-16-order-production-navigation/proposal.md)；不强制等待全部案才验证独立入口。

相称投入见 [execution-notes.md](execution-notes.md)，路线见 [entry-roadmap.md](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
