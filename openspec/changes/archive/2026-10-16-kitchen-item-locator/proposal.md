# Proposal

## Why

物品已真实分布在工位、角色手上、容器、餐具池和地面，当前详情聚焦选中工位。查找某份材料或餐具需要逐处看场景。 本案服务于长期目标中的操作顺畅、物品清楚与自然分工，聚焦现有业务的玩家入口。

## What Changes

- 按物品名称／类别查看当前实物位置、外层宿主和状态，区分手持、台面、容器内、地面、脏净池及加工锁定；选择结果仅查看／高亮当前宿主。
- 物品以唯一实例计数，容器内容与外壳分开，不将锁定输入或在途货冒充可取库存；不自动拾取、不给远距离操作权，地图高亮不移动角色。
- 补齐本案的成功、取消／失效与真实入口验收，保持可定位的业务反馈。

范围外：新增菜品／章节／角色、正式传输选型、自动化代操作、新持久业务规则、新协议或文件格式、Prefab／CodeBind 结构重构及新工具。本案已获实施授权；原 PC／Android 验收案保持独立。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-ui`：增加“厨房物品查找入口”的可观察入口及事实／失败边界；原业务合同保持。

## Impact

- 源码入口：[GridKitchen/GridPresentationInformation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationInformation.cs)，沿原 ModelView／HotfixView 和所属 ET owner 实现。
- 使用原 `UIGridKitchen`（1101）、`Product/UIGridKitchen.prefab` 的现有控制、列表及详情绑定；不增加私有池或第二业务状态。
- 验证复用 [RestaurantArtUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/RestaurantArtUiTests.cs) 所在的实际 PlayMode 程序集及原 Launcher 夹具。
- 实施协调：[kitchen-action-navigation](../2026-10-16-kitchen-action-navigation/proposal.md)。此列表表示共享文件与入口整合顺序，不把其他案全部完成设为新的玩法门槛。
- 依据与验证投入见 [execution-notes.md](execution-notes.md)，本批路线见 [入口路线](../2026-10-16-product-entry-navigation/entry-roadmap.md)。
