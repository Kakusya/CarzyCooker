# Proposal

## Why

DishStage 只有 Raw/Manual/Batch/Bake/Portion/AddToContainer；AddToContainer 终阶段校验容量必须为 1，当前手工配方是单输入切配/打蛋，无法实际完成沙拉等多组件成品。

增加可分工的共享切配与多材料组装，首批菜验证新语法，而非只复制汤和吐司配方。

## What Changes

- 沿现有物品/容器/手工作业增加有限多输入组装阶段，按定义与数量验证投入和唯一输出。
- 首批为已确认的 F01 田园沙拉、F02 芝士蔬菜沙拉，共享番茄、生菜、黄瓜切配，F02另加一份芝士片。
- 配套有限原料供应、盘/组装器具、实际订单、只读菜谱与已支持模式的保存/投影。

优先级：**P1**；建议顺序：**08**。前置：无新增 change 硬前置，复用已交付业务。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不一口气实现 87 菜，不增加精细摆盘/刀工/逐层坐标，不默认供应已切菜或免费成品，不改已有打蛋和汤规则。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `dish-recipes`：增加真实多输入组装阶段与按份依赖。
- `kitchen-item-processing`：多输入手工组装保持单执行者、交接及完整预检。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)
- [Docs/Product/catalog.md](../../../../Docs/Product/catalog.md)

实施交接：2026-10-15 已确认首批 F01 田园沙拉、F02 芝士蔬菜沙拉，按目录配方收敛。具体配置 ID、耗时及布局是实施参数；其余目录不属于首批。

多输入易重复消费或在失败时丢料。相关 UTF 验证数量、错料拒绝、多人抢料、半组装交接与保存恢复；用普通 Editor 实际制作交付首批菜。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

本案范围已确认并按授权实施，实际6核心/文件、4普通Launcher及3受变回归共13个不同UTF最新有效结果Passed；逐项见execution-notes。真机、物理LAN、Player/AOT与构建保持暂缓；没有提交、push或归档。优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
