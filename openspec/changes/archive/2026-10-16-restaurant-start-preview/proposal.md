# Proposal

## Why

新开章节和多种菜单按钮目前点击即建立餐厅，玩家开局前看不到所选章节的关序、菜单与保存方式。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 选择已交付章节或已有餐厅菜单后，先显示章节名称、实际关数、各关菜单、模式及保存语义，再明确开始；取消返回原列表。
- 预览读取当前有效章／Level／Dish 定义，不创造新章节、难度或承诺平衡。Solo 保存选择与合作 Host 开局权限沿原规则；Client 可查看本端目录，但不暗示已同步为全房间共同选项。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。本案已获实施授权；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `product-entry`：增加“章节与新开餐厅预览”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[SoloSavePresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [RestaurantContentChapterUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/RestaurantContentChapterUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[product-entry-navigation](../2026-10-16-product-entry-navigation/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
