# Proposal

## Why

GridKitchenRules 固定 7×18、KitchenBottom=6；寻路缓冲按常量预分配。已有装修只在固定地图增加工位，缺少空间解锁和作为经营选择的餐桌扩建。

使装修不仅增加加工速度，还能扩大可用厨房/前厅、增加接待容量和改变动线。

## What Changes

- 采用有界配置地图/可用区域替代相关固定尺寸，首批只提供一个有限区域加一张普通单客桌的扩建奖励。
- 扩建含可用格、必要路径和明确餐桌/设施安装，整体预检后一次提交。
- 扩建状态沿持续餐厅/成功档恢复，视图/点击/寻路/顾客/伙伴使用相同已提交几何。

优先级：**P1**；建议顺序：**10**。前置：[restaurant-unlock-permissions](../2026-10-15-restaurant-unlock-permissions/proposal.md)。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不做自由建造编辑器、多楼层、无限地图、多人桌/团体客或新经济系统；不自动扩大测试平台。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `kitchen-workstations`：有界地图与可用区域扩建。
- `restaurant-renovation`：增加有限空间/餐桌奖励并一次安装。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridStationConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridStationConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridFrontLayout.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridFrontLayout.cs)
- [Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.cs)

实施交接：2026-10-15 已确认首批一块有限扩建区加一张普通单客桌。前置为解锁/许可合同，具体格坐标与尺寸按现有地图可达性选定；不扩大最终规模。

共享几何变更会影响输入与三类角色寻路，需小范围跨消费者验证。选择一次代表性扩建，覆盖占位拒绝、旧物/加工保留、顾客入座和保存恢复，接受其他候选地图 NotRun。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

本案范围已确认并授权实施；功能按实际运行逐项验收，结果与明确未验证项见execution-notes。Editor/UTF按既有实施范围执行，真机、物理LAN、Player/AOT与构建保持暂缓；不自动提交、push或归档。优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
