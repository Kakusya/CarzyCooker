# Proposal

## Why

备料已有两条真实加工链，煮汤已有独立提案。下一步要让菜品成为可读的依赖链，并用第二道烤吐司检验统一操作能否支持不同加工方式。

## What Changes

- 新增菜品定义与无环阶段依赖核对，区分物品定义、配方、可交付菜品与订单。
- 保留番茄蛋花汤提案的实现边界；增加面包片 → 烤箱加工 → 新烤面包 → 盘装吐司的单机链。
- 按材料自动匹配、玩家显式启动；不增加配方选择步骤、自动投料或免费半成品。
- 菜单必须有原料、设备与容器闭包；原型供应用真实有限初始物品，重启恢复。
- 不导入 87 菜，不制定正式价格、产量、解锁及供应经济。本轮仅规划。

## Capabilities

### New Capabilities

- `dish-recipes`：菜品身份、阶段依赖、菜单闭包与盘装吐司生产链。

### Modified Capabilities

无；备料与汤的具体行为保持原能力负责。

## Impact

依赖 [煮汤提案](../grid-kitchen-soup-batch-cooking/proposal.md) 和 [工作台](../solo-workstation-layout/proposal.md) 的基础定义／烤箱，不依赖工作台的后续布局验收。新增 DishConfig 和烘烤配方源表、物品定义及现有 Tick 分支，沿用四层程序集和表链。[执行说明](execution-notes.md) 区分代表菜验收与完整目录未实施。
