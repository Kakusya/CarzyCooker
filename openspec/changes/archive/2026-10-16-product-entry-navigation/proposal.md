# Proposal

## Why

模式菜单、合作领取和文件授权共用一个按钮区，单机与房间又直接罗列多种餐厅。已有能力能进入，但缺少按玩家目的组织的稳定导航与返回层级。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 将开始游戏、继续记录、合作房间、合作进度和存档授权组织为清楚的入口分组，展示当前位置与返回目标；保留所有现有合法入口和显式启动参数。
- 导航仅保存本轮页面选择；切换页面不创建餐厅、不加入房间、不触发文件操作。旧菜单／试验餐厅保留可访问入口，并与已交付内容章明确区分。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。本案已获实施授权；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `product-entry`：增加“主入口任务导航”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[ProductEntryPresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryPresentationSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [ProductEntryUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/ProductEntryUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：可独立开始。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](entry-roadmap.md)。
