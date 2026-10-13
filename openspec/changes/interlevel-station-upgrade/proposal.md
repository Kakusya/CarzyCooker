# Proposal

## Why

装修目前只能增加有限新工位。首版在既有奖励选择中升级一个现有加工工位，让经营改善可见，并保留原物品和布局。

## What Changes

- 装修候选增加同占地、单级的加工工位升级，选择目标 StationId。
- 升级沿下一关安装门槛一次提交，原工位、物品、加工身份保持。
- 只影响升级后新启动作业；升级状态进入保存与独立投影。

目标模式：Solo 首版。层次：四：内容选择与消息边界；依赖按[整体路线](../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不做购买经济、多级树、扩占地、联机升级或升级中途重算；终关不发奖励。

## Capabilities

### New Capabilities

- `station-upgrades`：原工位升级、冻结加工参数与持久恢复。

### Modified Capabilities

无。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridRenovationLayout.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridRenovationLayout.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveCapture.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveCapture.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSaveRestore.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSaveRestore.cs)

替换工位可能丢物，重放或恢复可能重复升级。用同身份、旧作业耗时、失败零变更与实际文件往返足够；不扩大全布局组合。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮仅授权规划，功能／UTF／构建均 NotRun；不执行提交、归档或第二工作区操作。

