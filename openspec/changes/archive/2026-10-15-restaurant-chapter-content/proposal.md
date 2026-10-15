# Proposal

## Why

Solo与合作各自已有首章三关，当前配置分别使用ChapterId=1/2；尚无两个内容大关的选择与逐关递进菜单。现有动态总关数可复用，不能把模式配置链算作两个可选章节。

把已可跑的三关变成有递进内容的经营体验，并提供一个明确有限的后续大关内容包。

## What Changes

- 配置每关菜单、有限供应、设备、客流与装修节奏，形成介绍/组合/分工递进。
- 提供两个可选内容大关、每个三小关的入口、完成记录与复玩，Solo/合作均按内容章选择。
- 只在已落地菜品/工位中编排内容，可独立加入后续批次，不把全部候选设为首包前置。

优先级：**P2**；建议顺序：**11**。前置：[cooperative-chapter-completion](../2026-10-15-cooperative-chapter-completion/proposal.md)、[restaurant-unlock-permissions](../2026-10-15-restaurant-unlock-permissions/proposal.md)。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不设够分才能过关，不做跨大关自动搬同一家厨房、账号付费/成就或复杂经济，不声称脚本证明玩家理解与留存。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-chapters`：从单一三关样机扩展为有限可选择章节和递进菜单。
- `kitchen-recipe-guide`：菜谱/首单提示覆盖已选新内容包。

## Impact

- [Design/Excel/ET/Datas/Product/LevelConfig.xlsx](../../../../Design/Excel/ET/Datas/Product/LevelConfig.xlsx)
- [Design/Excel/ET/Datas/Product/CustomerFlowConfig.xlsx](../../../../Design/Excel/ET/Datas/Product/CustomerFlowConfig.xlsx)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)

实施交接：2026-10-15 已确认两个可选大关、每个三小关。复用合作终关和解锁许可；首包可只编排已交付菜品，新增菜实际交付后再纳入，不依赖全部内容批次。

内容常见风险是生产链缺口与高压到无法完成既有操作，用菜单闭包加每种新关机制一次 Editor 复玩覆盖，不做全目录穷举或统计留存研究。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

本案范围已确认；本轮仅优化规划产物，功能任务仍未验收。Editor/UTF按既有实施范围执行，真机、物理LAN、Player/AOT与构建保持暂缓；本次不执行实现、提交、push或归档。优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
