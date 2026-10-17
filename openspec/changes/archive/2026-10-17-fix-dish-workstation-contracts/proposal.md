# Proposal

## Why

菜品和工作台审查发现两处可定位缺角：带模具烘烤的合法烤箱无法调整布局；启动校验未拒绝运行选择条件相同的配方和成品身份，可能出现“开局通过、实际 NoRecipe”或按表顺序选择错误配方。现有十一款菜的统一实物、时间和权威结构继续沿用。

## What Changes

- 布局预检区分直接烘烤输入和载具内部输入，合法移动／旋转保留原模具、内容、加工及冻结耗时；关系非法时整次拒绝。
- 菜单校验按实际运行选择条件拒绝歧义手工、烘烤、批次配方及重复成品身份，错误报告列出冲突编号；显式 RecipeId 组装不受此唯一性限制。
- 在 audit 记录代码健康、业务闭合情况及已有界面提案，补充 UTF 和普通 Launcher 运行验收。
- 不新增菜品、工位、配方选择界面、生产队列或新业务状态，不改 Excel 源、生成物、协议或存档格式。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `dish-recipes`：启动时验证配方选择和成品识别的唯一性。
- `kitchen-workstations`：明确载具烘烤工位调整时的完整关系保留与拒绝合同。

## Impact

修改 Hotfix 的 GridDishConfig、GridStationLayoutSystem 及既有 PlayMode 测试程序集，沿用原消息、Model 和投影。普通玩法局部修正采用小集运行验证，覆盖真实制作、合法布局／非法关系及初始化拒绝；取舍和逐点证据见 [execution-notes.md](execution-notes.md)。审查正文见 [audit](../../../../audit/2026-10-17-dishes-and-workstations.md)。
