# Proposal

## Why

Solo 已有完整餐厅，但正式 Host 仍创建临时厨房，远端只开放备料动作。先交付真实 Host 餐厅、两菜制作与共享控制，才能让前厅、供应和后续联机功能拥有实际可运行的前置。

## What Changes

- **BREAKING**：Host 与 Solo 共用 RestaurantRuntime／持续厨房／当前 Level；移除正式 Host 的临时厨房回退，保持唯一业务驱动。
- 接入汤、吐司的完整制作请求及同版投影；远端走可信 Session 桥，本地继续走 ET 本地消息。
- 开店、暂停／恢复、Continue、布局和 Buff 由真实 Host 本地 Peer 控制；Client 合法制作保留，越权零改动。
- 补齐加入后本人角色与快照就绪、池化消息所有权、两菜末份争抢和持物退出边界。
- 本案不开放远端交付／前厅工作、清理／供应新动作，不实现装修、存档、重连、LAN 或新美术。原 Solo 功能保持。

## Capabilities

### New Capabilities

- `cooperative-kitchen`：持续餐厅中的双人两菜制作、共享控制与就绪／退出合同。

### Modified Capabilities

- `product-session`：正式 Host 的餐厅 owner，以及按交付功能显式扩展的可信协议入口。

## Impact

基于已交付 Solo 九案；不以尚未完成的 Solo 供应、装修或检查点为本案开工前置。若当前共享 Restaurant 已带供应组件，保留其原行为，但远端物流接入留给后续案。

修改范围为 ProductRuntimeBootstrap、HostSession 驱动、ProductWire、Level／Layout／Buff／Grid 客户端调用与 handler、GridClient 投影、现有 HUD 以及必要协议源。保持原程序集与 owner，不另建命令总线。

从 [原联机案](../2026-10-14-cooperative-restaurant-loopback/proposal.md)迁入原 1.1、1.2 与 2.x 的核心部分；后继为 [前厅案](../2026-10-14-cooperative-front-service/proposal.md)。本案以最小真实 KCP UTF 和同 Editor 普通 Host＋临时远端受管入口验收，双 Editor 整体验收归原案。取舍与逐任务证据位置见 [execution-notes.md](execution-notes.md)。
