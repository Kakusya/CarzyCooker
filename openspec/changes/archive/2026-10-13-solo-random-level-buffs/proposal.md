# Proposal

## Why

每关随机候选可以让同一厨房产生新的选择。用户本轮已确认“随机候选、玩家选一个”，需让随机结果与生效状态归当前 Level，便于测试和换关清理。

## What Changes

- Preparing 从关卡允许池无放回抽取候选，玩家经本地命令选一个；重新打开 UI 不重抽。
- 候选、随机种子和已选定义归 Level，选择只能提交一次；完成选择后允许开店。
- 第一项实际效果为煮制加速，作用于选择后的新启动作业；已有作业保持冻结耗时。
- 使用定义声明的效果与参数，不做同 buff 叠层、随机负面效果或通用互斥。
- 只做单机运行态，不做存档奖励、联机同步或随机测试平台；用户已授权完成九个 change，按 tasks 实施和验收。

## Capabilities

### New Capabilities

- `level-buffs`：关卡随机候选、一次选择与烹饪效果。

### Modified Capabilities

无。

## Impact

依赖 [餐厅流程](../2026-10-13-solo-restaurant-level-flow/proposal.md) 与 [汤自动作业](../2026-10-13-grid-kitchen-soup-batch-cooking/proposal.md)，UI 依赖本 change。新增 BuffConfig／关卡允许池字段，接入原 Tick 与新作业耗时计算，复用原请求路径。候选数量、倍率为可调样机值，验证风险见 [执行说明](execution-notes.md)。
