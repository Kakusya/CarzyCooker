# Proposal

## Why

Solo 装修已交付，但联机尚无对应入口和权限。将已有新增工位奖励接到完整合作餐厅，保持 Host 选择、队友查看的确认规则。

## What Changes

- 接通 ChooseRenovation 的远端拒绝／Host 本地控制、候选与选择投影。
- 双方看到同一结果和安装，Continue 仍由 Host 发起并完整预检。
- 后加入读取当前装修事实；不靠界面可见性授予权限。

目标模式：Host/Client 内存餐厅。层次：五：完整合作与恢复；依赖按[整体路线](../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不接单机升级奖励、联机存档、投票或收益共享凭证；无需等待设备升级。

## Capabilities

### New Capabilities

- `cooperative-renovation`：Host 选择装修、共同观察与一次安装。

### Modified Capabilities

无。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RenovationMessages.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RenovationMessages.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRenovationSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRenovationSystem.cs)

主要是远端越权和重复安装。真实 KCP 双端选择／拒绝／重放／换关与实际 UI 足够，复用 Solo 布局算法；存档与 Player 不在本案。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮已获三个 change 的实施授权；功能及 UTF 结果逐项回填 execution-notes。提交、归档和第二工作区版本维护仍需对应授权。

