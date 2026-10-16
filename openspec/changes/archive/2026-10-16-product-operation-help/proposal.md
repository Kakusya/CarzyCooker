# Proposal

## Why

当前 HintText 只简述点击目标和拖动，菜谱／首单说明则聚焦制作。新玩家缺少随时查看地图输入、拿放、工作接续、暂停和模式权限的统一帮助。

## What Changes

- 提供可随时开关的操作帮助，说明当前实际点击／拖动规则、目标与动作区、普通拿放／容器处理、工作与暂停，以及 Solo／Host／Client 权限差异；链接原菜谱与相关入口。
- 帮助只解释当前已实现输入，不宣称不存在的快捷键、手柄或自动操作；不强制完成教程、不持久化教学进度，阅读不自动暂停／恢复、改变物品或发命令。
- 补齐正常使用、失效结果、界面重开及真实操作验收。

范围外：新输入设备支持、强制教程、自动重试、新业务权威、网络／文件改版、UI 结构改造及新工具。实施按原18案授权推进，结果见 execution-notes。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-ui`：增加通用操作帮助入口，保持原事实反馈和业务操作合同。

## Impact

源码入口：[GridKitchen/GridPresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)，沿原 ModelView／HotfixView、UIGridKitchen（1101）及原绑定实现。验证复用 [SoloRestaurantUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloRestaurantUiTests.cs) 和普通 Launcher／真实 KCP 夹具。涉及共享详情区的案按序整合：[kitchen-action-navigation](../2026-10-16-kitchen-action-navigation/proposal.md)；不强制等待全部案才验证独立入口。

相称投入见 [execution-notes.md](execution-notes.md)，路线见 [entry-roadmap.md](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
