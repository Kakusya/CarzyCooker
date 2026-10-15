# Proposal

## Why

实际工位能力只有切配、煮制、烘烤、榨汁等；烘烤为单物件替换输入，尚无煎制熟料再与其他分支合流的热餐。

用有限首批热餐引入煎台和共享熟料，使合作分工具有备料、热加工和最终组装。

## What Changes

- 首批已确认 F11 经典牛肉汉堡、F43 香肠鸡蛋早餐；只引入这批实际需要的煎台与原料。
- 共享熟肉/煎蛋/香肠可提前制作和交接，最终组装按真实实物和数量完成。
- 所有新增菜带入供应、订单、菜谱、美术识别、清理和支持格式的保存恢复。

优先级：**P2**；建议顺序：**12**。前置：[kitchen-multi-input-assembly](../2026-10-15-kitchen-multi-input-assembly/proposal.md)。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不同时做蒸锅/炸锅/烤架全家桶，不加火灾、烧焦、翻面、油量或温度小游戏；其他热餐保持后续批次候选。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `dish-recipes`：首批煎制熟料、组合热餐及跨阶段合流。
- `kitchen-workstations`：增加首批煎制设备能力及占用。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSoupSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridWorkTransitions.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridWorkTransitions.cs)
- [Docs/Product/catalog.md](../../../../Docs/Product/catalog.md)

实施交接：2026-10-15 已确认 F11 经典牛肉汉堡、F43 香肠鸡蛋早餐，按目录配方制作。煎台显式启动后自动加工，耗时/供给量为可调首包参数。

风险为旧过程类型被新设备混用或跨关重复产物。用共用煎制能力及两菜各自熟料的拒绝/暂停/交接/保存小集，不遍历所有候选热餐。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

本案范围已确认；本轮仅优化规划产物，功能任务仍未验收。Editor/UTF按既有实施范围执行，真机、物理LAN、Player/AOT与构建保持暂缓；本次不执行实现、提交、push或归档。优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
