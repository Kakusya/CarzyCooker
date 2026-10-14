# Proposal

## Why

鲜榨橙汁是已确认的首饮，能验证区别于汤和吐司的设备、杯子供应与独立贴单链。完整接通现有实物与订单规则，避免免费成品或界面假绑定。

## What Changes

- 实际橙子切块，放入果汁壶并在榨汁机显式加工；加工中锁壶，完成后可搬壶逐杯盛装。
- 杯数与耗时由配置定义，有限供应包含橙子与一次性杯。
- 完成饮品在贴单台显式绑定／解绑／换绑有效订单，合法交付消费一次性杯与饮品。
- 液体在水池处理，固体按垃圾桶规则；跨关与保存保留合法实物、份数和加工。

目标模式：Solo。层次：四：内容选择与消息边界；依赖按[整体路线](../../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不加冰热糖度、封口小游戏、其他饮品、联机动作或联机存档。

## Capabilities

### New Capabilities

- `drink-preparation`：切橙、榨汁、壶中批次与逐杯盛装。

### Modified Capabilities

- `orders-front-service`：饮品绑定与一次性杯的交付和收尾。
- `restaurant-chapters`：保留相同两道饭菜，新增饮品菜单变体；旧有效档保持原菜单与初始实物语义。

## Impact

- [Docs/Product/catalog.md](../../../../Docs/Product/catalog.md)
- [Docs/Product/specs/kitchen-item-processing/spec.md](../../../../Docs/Product/specs/kitchen-item-processing/spec.md)
- [Docs/Product/specs/orders-front-service/spec.md](../../../../Docs/Product/specs/orders-front-service/spec.md)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridServeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridServeSystem.cs)

风险集中在守恒、加工锁定、旧订单绑定和一次性杯收尾。复用现有物品、服务与供应，新增相应 UTF、实际文件往返及一条普通经营链；旧有效档不因默认新菜单而免费增加实物。2026-10-14 已获实施授权，实际结果与失败修正见 [execution-notes.md](execution-notes.md)，普通三关链已运行；Player／手机／联机饮品不在本案验收范围。不执行提交、归档或第二工作区操作。

