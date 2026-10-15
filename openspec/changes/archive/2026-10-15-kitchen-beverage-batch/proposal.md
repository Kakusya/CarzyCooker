# Proposal

## Why

只有鲜榨橙汁和一次性杯贴单，未实现磨豆/萃取/热奶/奶泡及定份混合，当前饮品完成仍主要依赖单种壶内批次。

引入共享咖啡基底和顺序收尾，验证多种饮品复用备料、分工调制与原贴单交付。

## What Changes

- 首批已确认 D15 浓缩咖啡、D17 热拿铁、D18 卡布奇诺，增加实际磨豆/萃取及蒸汽加热/打泡模式。
- 热奶与咖啡可并行，配方按游戏份数混合，奶泡必须在最后合法阶段加入。
- 原贴单、换绑、一次性杯、清理、有限采购、菜谱及成功恢复覆盖新饮品。

优先级：**P2**；建议顺序：**14**。前置：[kitchen-multi-input-assembly](../2026-10-15-kitchen-multi-input-assembly/proposal.md)。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不实现全 31 饮品、茶/珍珠/汽水/冰沙全链，不增加糖度杯型排列、拉花、温度控制、封盖或节奏小游戏。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `drink-preparation`：首批咖啡基底、明确设备模式与顺序调饮。
- `dish-recipes`：饮品阶段表达定份多输入及最后收尾。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDrinkSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDrinkSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridRecipeGuide.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridRecipeGuide.cs)
- [Docs/Product/catalog.md](../../../../Docs/Product/catalog.md)

实施交接：2026-10-15 已确认 D15 浓缩咖啡、D17 热拿铁、D18 卡布奇诺及最后加奶泡规则；采用目录游戏份数，其他饮品族不纳入。

主要风险为顺序绕过、热奶模式混淆和错贴单。用一条共享基底分支加顺序拒绝/重绑保旧/暂停/末份/保存小集，普通 Editor 实做三款。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

本案范围已确认并按后续“完成16案”授权实施，当前4/4任务及实际UTF结果见execution-notes；创建时仅规划的阶段已经结束。真机、物理LAN、Player/AOT与构建保持暂缓；未提交、push或归档。优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
