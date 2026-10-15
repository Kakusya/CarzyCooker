# Proposal

## Why

ProductRoot 的 Solo 分支打开选档，Host/Client 依赖启动角色配置直接建树；InitializeProductCore 在 Host Join 前就创建 RestaurantRuntime，尚无可停留的无餐厅房间。

从普通 Launcher 在游戏内选择单机、创建合作或加入，合作成员可先集合，再由 Host 开启内存餐厅；关闭餐厅可以回同一房间。

## What Changes

- 增加游戏内模式、显式地址加入、房间成员与加载/取消/失败反馈；保留既有 CLI 启动方式。
- 拆开 Match 成立和 RestaurantRuntime 创建，支持同一房间先无餐厅、后开局及结束返回。
- 切换模式使用完整启动/退场交接，不能原地改写已经 Prepared 的 RuntimeInstanceConfig。

优先级：**P0**；建议顺序：**01**。前置：无新增 change 硬前置，复用已交付业务。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不新增大厅发现、Ready 投票、公网、LAN 地址放行、正式传输、Profile 授权、合作选档持久化或新端口工具。

## Capabilities

### New Capabilities

- `product-entry`：游戏内模式选择、房间停留、创建和退役餐厅。

### Modified Capabilities

- `product-session`：产品会话可处于无餐厅房间，Match 与餐厅的退出边界分离。
- `runtime-instances`：普通 Launcher 先显示模式选择；保留显式请求与本轮不可变配置。
- `startup`：普通入口在选择前不建运行餐厅，模式切换经完整退出和新配置交接。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeSystem.cs)
- [Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceContext.cs](../../../../Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceContext.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)

实施门槛：无需新增产品裁决；按钮布局和提示文案作为可逆实现细节。

主要风险是返回房间误停公共 sender、旧快照重新打开旧地图。用真实消息退役/再开、取消加载及旧回调小集和普通 Editor 入口覆盖，接受设备输入与包体 NotRun。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

用户已授权完成全部 16 个 change。本案普通入口及房间生命周期已实现，四项核心与四项普通 Launcher/UI PlayMode 用例通过，具体映射见 execution-notes；不包含真机、物理 LAN、Player 或 AOT 实测。未提交、push 或归档。总体证据与优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
