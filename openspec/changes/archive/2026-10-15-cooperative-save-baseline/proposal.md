# Proposal

## Why

InitializeSavedProduct 明确拒绝非 Solo；SelectedSaveBaseline/SoloSaveBinding 归 Match，但 Participant 没有长期 Profile 绑定，合作新开没有选档入口。

合作房间可选择合法检查点建立新的持续餐厅，文件归属与 Host 世界裁决分开。

## What Changes

- 接入已确认权限协议的 Profile/Slot/Revision 基线选择与房间只读概要。
- 完整校验后只创建一个新 RestaurantRuntime，恢复厨房/供应/装修/加工，使用新运行和关代次。
- 取消或失败保留房间与原有效文件；更换基线先退役旧餐厅。

优先级：**P1**；建议顺序：**05**。前置：[product-mode-room-flow](../2026-10-15-product-mode-room-flow/proposal.md)、[progress-authorization-contracts](../2026-10-15-progress-authorization-contracts/proposal.md)。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不从客户端同步快照倒灌存档，不自动改写 Owner 文件，不实现本案之外的导入、领取、覆盖或进程重启成员恢复。

## Capabilities

### New Capabilities

- `cooperative-save-baselines`：合作房间选择有权限的持久基线并恢复新餐厅。

### Modified Capabilities

无。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveBindingSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveBindingSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs)
- [Unity/Assets/Tests/Editor/ET/SoloSaveSelectionUiTests.cs](../../../../Unity/Assets/Tests/Editor/ET/SoloSaveSelectionUiTests.cs)

实施门槛：离线协议已于 2026-10-15 明确确认，按[协议正文](../../../../Docs/Product/offline-progress-protocol.md)验证 Profile 与 ReadRun/MatchAuthority；房间流程已交付，不得自报 ProfileId 绕过。

涉及文件权限及恢复，错误会越权或复制库存。补实际临时 repository 文件、非法授权/旧有效格式、载入取消与重开 UTF，再做普通 Editor 合作选档。任意断电和设备导入未验证。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

用户已授权完成全部 16 个 change，并确认离线授权首版。本案5核心及2普通入口唯一用例的最新有效结果全部Passed，逐功能映射见execution-notes，后续领取/导入替换仍待实施，只运行各自相关 Editor/UTF，不包含真机、物理 LAN、Player 或 AOT 实测；未获提交、push 或归档授权。总体证据与优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
