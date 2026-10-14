# Proposal

## Why

现有关卡链要求有效下一关，保存格式也要求 NextLevel。用户确认三关后结束大关，需要明确终态和文件语义，避免无限继续或假第四关。

## What Changes

- 用已有汤和吐司建立三关链与阶段提示，保持零星继续和自然前厅收口。
- 第三关不发装修，不安装第四关；保存大关完成记录后返回选档。
- 选档区分可继续记录与已完成记录；完成记录可查看，另行新开不复活旧餐厅。
- 提供每关订单结果与得分／星级说明，汇总归 Level，表现只读。

目标模式：Solo。层次：三：完整一轮经营；依赖按[整体路线](../../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不实现联机三关、账号解锁、跨设备领取、覆盖旧档或强制失败；不借菜单变化裁决跨关许可缩小。

## Capabilities

### New Capabilities

- `restaurant-chapters`：三关进度、大关完成记录与返回选档。

### Modified Capabilities

- `restaurant-level-lifecycle`：区分普通换关与第三关终态。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)

终关与文件兼容可能误建世界或丢失有效进度。保留不覆盖发布，强化实际文件旧格式读取、终态重试及一次普通三关保存退出重入；不验证任意断电。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮已授权实施，配置／消息／文件／受管 UI 已通过相关 UTF；普通入口三关保存退出重入已通过，构建 NotRun；不执行提交、归档或第二工作区操作。

