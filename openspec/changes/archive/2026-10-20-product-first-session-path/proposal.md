# Proposal

## Why

目前 Solo 选档把多个“新开某菜单”与正式章节平铺，首单提示又主要是长菜谱文字。玩家即使能进入游戏，也难判断该从哪里开始、眼下该操作什么。本案把现有能力组织成明确的首次游玩路径。

## What Changes

- 首页突出“开始单人游戏”，Solo 页面优先展示三个正式章节和继续记录；默认推荐第一章「家常开张」。独立菜单集中到明确的“菜单试玩”次级页，仍完整保留。
- 新开仍走原开局预览和显式确认，展示章节、存储方式和首关菜单；继续旧档不替换章节、不创建新记录。
- 在 Solo 第一章首关 1101 自动显示可跳过的汤品首单提示：当前目标、一步说明、真实材料/设备高亮；按投影重算，已有备料不重做。
- 从准备、加成选择、备料到接待、询单、交付、收尾衔接现有入口；提示不自动操作、不新增开店门槛、不持久化教程进度。
- 其他菜单保留现有菜谱与手动重看能力；合作保持现有房间选择/准备规则，不强制首单教程。

## Capabilities

### New Capabilities

无，复用既有入口和菜谱能力。

### Modified Capabilities

- `product-entry`：增加单人章节优先的导航和试玩次级入口。
- `kitchen-recipe-guide`：增加第一章首单的分步说明与场景定位。

## Impact

- 修改 `ProductEntryNavigation`、`SoloSavePresentationSystem`、`GridPresentationRecipeSystem`、`GridRecipeGuide` 及对应 ModelView 显示选择；复用 UI1101 和 Board 的现有高亮能力。
- 结构前置为 [HUD 布局](../2026-10-20-product-hud-readable-detail-layout/proposal.md)。与 [节奏案](../2026-10-20-first-chapter-rhythm-balance/proposal.md) 共用第一章身份，但本案不改数值；情境动作案只排序动作，不持有教程进度。
- 风险主要是选错开局/存储语义、指向过期目标。以原预览取消/确认、真实首单、重开和旧档检查覆盖，详细取舍见 [execution-notes.md](execution-notes.md)。
- 2026-10-20：后续已按用户实施授权完成5/5任务及对应运行验收，详见[执行记录](execution-notes.md)。未取得真人新手观察；已归档并获授权本地提交，未推送。
