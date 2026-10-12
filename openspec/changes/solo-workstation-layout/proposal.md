# Proposal

## Why

当前台面与切配台位置写在初始化代码里，后续灶台、烤箱和前厅设备需要明确能力及占地。准备态调整工作台应保留已有材料和加工进度。

## What Changes

- 增加工位定义和初始布局源表，覆盖台面、切配台、灶台、烤箱以及前厅交接能力；不一次交付候选 19 类。
- 分开工位定义、运行身份、物品槽与加工关联，提供烤箱加工所需设备能力，具体菜品由菜品 change 实施。
- Preparing 可移动／旋转已有工位；完整预检通路、占位和承接关系后一次安装，营业时拒绝。
- 玩家、伙伴与顾客使用同一地形；不靠销毁物品或重置作业处理冲突。
- 本轮不做装修奖励选择、区域扩建、供应物流或新布局工具，只建立 change。

## Capabilities

### New Capabilities

- `kitchen-workstations`：工位能力、布局、单槽及准备态移动旋转。

### Modified Capabilities

无；已有格子交互与物品规则继续有效。

## Impact

依赖 [单机流程](../solo-restaurant-level-flow/proposal.md) 和本地消息。适用 GridStation、Blocked/寻路、Process 关系、Board 投影和 StationConfig／布局源表；现有受管 HUD 提供基本动作，完整 UI 另案。[执行说明](execution-notes.md) 说明只检少量代表布局。
