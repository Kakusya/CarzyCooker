# Proposal

## Why

当前订单入口逐单展示制作目标，前厅看板逐项展示工作，但玩家还不能集中查看各菜待交付数量、真实等位压力和得分门槛。营业中的 HUD 还直接显示只在结算时更新的 `LevelStars`，容易让已有得分看起来始终只有零星；需要区分过程参考与最终结果。

## What Changes

- 增加一个可随时关闭的“营业总览”，集中展示按菜品汇总的待交付订单、前厅队列与桌位负荷、当前得分及关卡星级门槛。
- 每种菜展示真实 Waiting 订单数量及最早剩余期限，展开后选择具体顾客进入原订单制作；不把成品、绑定杯或加工批次自动抵扣为已交付。
- 从原等位列表、顾客和桌位投影解释进场/等位、桌位状态及满队列跳过来客；收尾明确停止来客，既有前厅仍须自然排空。
- 营业与收尾区分“当前分数对应的参考星级”和尚未产生的结算星级，显示距下一门槛分差；Completed 使用 Host 已提交星级。同步修正原 HUD 过程星级文案，不改变计分和结算。
- 查看与跳转不发业务命令；暂停、换关、恢复、HUD 重开和旧回调遵守原投影与 owner 边界。
- 范围外：自动配餐、库存分配、订单意向/独占、生产预测、补货建议、客流或评分平衡、新提醒音效、存档/协议改版及设备验收。

## Capabilities

### New Capabilities

无；复用已有餐厅界面能力。

### Modified Capabilities

- `restaurant-ui`：增补营业总览、需求/客流/评分说明及只读导航与生命周期合同；原订单、前厅和关卡业务规则保持。

## Impact

主要修改现有 ModelView `GridPresentationComponent` 的页面选择、HotfixView 的显示与导航以及 `MonoUIGridKitchen` 的原信息区绑定。直接入口为 [GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)、[订单导航](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationOrderNavigation.cs)和[前厅看板](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationFrontWorkboard.cs)。

数据已由 [GridClientComponent](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridClient.cs) 提供，阈值/队列上限读取当前可信定义；无需新查询请求、配置行、Prefab 结构或 CodeBind 变更。详细扫描、与现有22个未开工业务案的分工及验证投入见 [execution-notes.md](execution-notes.md)。后续已获整批实施授权，当前按tasks逐项验收；不以规划完成代替功能通过。
