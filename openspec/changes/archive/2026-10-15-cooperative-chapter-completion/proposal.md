# Proposal

## Why

Host 默认菜单使用 3→4→4，关卡 4 自循环且 ChapterId 为 0；FinishChapter 在 RestaurantRuntimeSystem 中明确只接受 Solo。

合作经营具有配置明确的终点，完成后所有人看到同一结果，并可返回房间继续组织下一轮。

## What Changes

- 为合作关序增加明确终关与合法终端校验，替代默认无限循环体验。
- 终关不发装修、不接受 Continue 建下一关，由 Host 确认结束餐厅回同一房间。
- 保留零星成功、自然前厅收口、内存结果与持久记录的区别。

优先级：**P0**；建议顺序：**02**。前置：[product-mode-room-flow](../2026-10-15-product-mode-room-flow/proposal.md)。优先级表示用户价值和依赖，不要求所有案串行。

范围外：本案不产生个人存档或领取凭证，合作三关已获独立确认，不改评分/失败规则。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-chapters`：增加合作明确终关与结果返回房间，不冒充个人持久记录。
- `restaurant-level-lifecycle`：合作换关区分非终关 Continue 与终关结束。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)
- [Design/Excel/ET/Datas/Product/LevelConfig.xlsx](../../../../Design/Excel/ET/Datas/Product/LevelConfig.xlsx)
- [逐关顾客流源](../../../../Design/Excel/ET/Datas/Product/CustomerFlowConfig.xlsx)

实施门槛：用户已明确确认合作首个大关三关。

终关判断错误会误建第四关或过早释放结果。用终关/非法引用、重复结束、Client 越权及真实前厅排空 UTF，加一次普通 Editor 合作终点流程；复用原制作证据。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

用户已授权完成全部16案并确认合作首大关三关。按本案最小相关Editor/UTF验收，不包含真机、物理LAN、Player或AOT实测；不提交、push或归档。总体证据与优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
