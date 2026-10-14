# Proposal

## Why

当前 Board 标签与底部操作密集，功能已运行但目标、物品和内容不易分辨。先整理现有受管界面，为菜谱与后续内容提供稳定显示位置。

## What Changes

- 区分阶段／手持物／选中目标／操作／详情区域，减少工位和内容标签重叠。
- 按现有动作规则突出主要操作，保留点击即执行、拖动不点击以及越权按钮约束。
- 将等待到位、加工中、完成与拒绝原因做清楚的文字和视觉反馈。

目标模式：Solo 与已开放 Host/Client。层次：二：看得清、操作清楚；依赖按[整体路线](../../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不改配方、菜单、经济、命令权限或领域时钟；不新增私有 UI 池、业务状态或结构 runner；首版不引入音频资产包。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-ui`：补充信息层次、上下文动作和清楚的结果反馈。

## Impact

- [Unity/Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab](../../../../Unity/Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab)
- [Unity/Assets/Res/Entity/Product/GridKitchenBoard.prefab](../../../../Unity/Assets/Res/Entity/Product/GridKitchenBoard.prefab)
- [Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoUIGridKitchen.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoUIGridKitchen.cs)
- [Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)

结构变化可能留下旧绑定、遮挡输入或使池化按钮重复订阅。真实 Prefab／CodeBind 检查、UI 小集与普通入口点击拖动足够；真机尺寸与触控在最后补验。2026-10-14 已按后续实施授权完成 4/4，相关 UTF 与普通 540×960 两菜／前厅已运行；逐功能结果、失败记录与未验证设备范围见 [execution-notes.md](execution-notes.md)。未提交、归档或操作第二工作区。

