# Proposal

## Why

结算文案由统计、装修、保存状态分段追加，持久与试玩、Solo 与合作的下一步不同。玩家需要清楚知道当前卡在哪、什么已保存及继续会到哪里。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 按当前模式和真实阶段组织本关结果、待处理装修／保存、下一关菜单变化或大关终点，并给出唯一清楚的主要下一步和原合法次要入口。
- 不新增自动保存、奖励或第四关；零星仍可继续。内存、保存中、失败、已提交和已领取分别说明，Host／Client 控制与个人领取资格保持。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。已按后续18案实施授权接入并实际验收，取舍和证据见execution-notes；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-ui`：增加“结算到下一步的导航”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [SoloChapterUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloChapterUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[renovation-choice-preview](../2026-10-16-renovation-choice-preview/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
