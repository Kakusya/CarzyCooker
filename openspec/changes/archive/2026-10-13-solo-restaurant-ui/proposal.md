# Proposal

## Why

现有 HUD 只表达格子与备料。玩家需要直接看懂营业阶段、顾客订单、伙伴进度和 buff，并通过同一输入规则操作厨房和前厅。

## What Changes

- 在现有受管地图/HUD 增加关卡状态、订单列表、手持内容、伙伴工作、buff、结算与候选选择。
- 厨房一屏可见，纵向拖动查看下方前厅；点击目标和 HUD 动作走同一命令，拖动不误发移动。
- 区分收到请求、动作成功和加工完成；拒绝保留物品并给可理解原因。
- 明确新增 Prefab/CodeBind 字段及 owner 生命周期，关闭／重开、加载中退出不遗留回调。
- 实施本轮单机 UI，用户已授权完成九个 change；不做联机大厅、存档选择、全新 UI 框架或结构 runner。

## Capabilities

### New Capabilities

- `restaurant-ui`：单机餐厅状态、可操作反馈、地图与流程 UI。

### Modified Capabilities

无；UI 基础 owner 与表驱动合同保持原状。

## Impact

分阶段依赖流程、工作台、菜品、伙伴、顾客和 buff 投影，不能反向成为领域 change 的前置。适用 UIGridKitchen Prefab/Mono、CodeBind 源、HotfixView GridPresentation、必要 UI.xlsx 和本地化源；逻辑验收只使用原 HUD 基本入口，完整表现由本 change 收口。验证取舍见 [执行说明](execution-notes.md)。
