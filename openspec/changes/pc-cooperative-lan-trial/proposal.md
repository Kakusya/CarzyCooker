# Proposal

当前为暂缓设备案。本轮只优化计划，不执行构建、启动Player、开放LAN监听或操作真机；原有暂缓决定保持，恢复实施时按届时授权和实际环境交接。

## Why

Editor 合作无法证明独立包、资源热更与物理两机网络可用。按用户要求，将设备验收放在已选功能的 Editor 验收之后集中完成。

## What Changes

- 复用既有 BuildHelper／HybridCLR／资源入口构建独立 PC Player，并提供明确 Host/Client 启动配置入口。
- 先同机 Player 回环，再显式私网 IPv4 的两机 LAN；默认仍 Solo／loopback。
- 按阶段开始时确认的功能清单验收，不把未来所有候选当成无限前置。

目标模式：PC Player Host/Client。层次：六：最终设备验收；依赖按[整体路线](../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不做公网、中继、NAT、大厅、联机存档或更换正式传输；不新建双包编排器／端口注册表。

## Capabilities

### New Capabilities

- `pc-player-cooperation`：独立 Player 启动及两台 PC 的局域网合作。

### Modified Capabilities

- `runtime-instances`：显式 LAN endpoint 意图。
- `product-session`：回环之外的显式局域网监听范围。

## Impact

- [Book/打包与包体验收SOP.md](../../../Book/打包与包体验收SOP.md)
- [Unity/Assets/Scripts/Game/Editor/Build/BuildHelper.cs](../../../Unity/Assets/Scripts/Game/Editor/Build/BuildHelper.cs)
- [Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceConfig.cs](../../../Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceConfig.cs)
- [Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceRequest.cs](../../../Unity/Assets/Scripts/Game/Base/RuntimeInstance/RuntimeInstanceRequest.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRuntimeBootstrap.cs)

包体和监听范围有明确平台风险。分构建、同机独立进程、物理两机三段验证，失败停在对应段；不修改全局防火墙、不扫描网络。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮仅授权规划，功能／UTF／构建均 NotRun；不执行提交、归档或第二工作区操作。

