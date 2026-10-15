# Proposal

## Why

没有甜品定义、搅拌面糊、烘焙载具或脱模阶段；GridBakeSystem 当前直接把单个面包替换为吐司，不能表达模具身份保留。

用首款小蛋糕形成共享面糊、装模、烘烤、脱模装盘的真实链路。

## What Changes

- 首批已确认 S04 原味小蛋糕，仅引入其需要的搅拌机、面糊、模具和甜品盘。
- 面糊按有限份数取用；模具为真实载具，经装模/烤制/脱模后仍保留身份。
- 菜谱、订单、供应、清理、远端操作和当前保存格式同时覆盖新甜品。

优先级：**P2**；建议顺序：**13**。前置：[kitchen-multi-input-assembly](../2026-10-15-kitchen-multi-input-assembly/proposal.md)。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不在首批加入所有 12 甜品、不模拟发酵/预热/复杂冷却；松饼、奶油水果、炸甜甜圈和圣代留后续内容批次。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `dish-recipes`：面糊、模具烘焙及脱模最终阶段。
- `kitchen-item-processing`：限定载具内容关系与身份保留。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs)
- [Docs/Product/catalog.md](../../../../Docs/Product/catalog.md)

实施交接：2026-10-15 已确认 S04 原味小蛋糕，按目录的甜面糊、装模、烘烤、脱模到甜品盘实施。耗时和批次产量按有限配置落地，不新增其他甜品。

新载具关系可能造成循环包含、提前出品和模具丢失。聚焦有限批次、脱模失败保物、模具复用及实际保存，避免通用容器重构。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

本案范围已确认；本轮仅优化规划产物，功能任务仍未验收。Editor/UTF按既有实施范围执行，真机、物理LAN、Player/AOT与构建保持暂缓；本次不执行实现、提交、push或归档。优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
