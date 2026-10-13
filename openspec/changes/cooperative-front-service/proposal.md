# Proposal

## Why

两端能制作菜品，还不能证明远端可以完成顾客服务和连续经营。需要把已有 Solo 前厅、伙伴认领和关卡收尾接到餐厅网络入口，形成可真实交付并自然换关的联机流程。

## What Changes

- 在 [餐厅核心案](../cooperative-restaurant-core/proposal.md)交付后，开放远端 Serve、FrontWork Claim／Stop 等明确请求，复用原权威规则。
- 接通顾客、订单、伙伴、原餐具、有限池及工作进度的同版投影和双端操作入口。
- 验证玩家与伙伴单一认领、同桌换客拒绝、一次交付计分、断线释放工作和原餐具回收。
- 完成双端自然 Draining／Completed／Continue、旧关隔离和新关本人角色就绪；不改变零星成功或实物结转规则。
- 不新增菜品、伙伴 AI 或评分规则，不接供应／设施动作，不实现联机装修、三关终点、存档或原成员恢复。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `orders-front-service`：增加远端交付、前厅接续与独立投影合同；保留全部 Solo 要求。
- `restaurant-level-lifecycle`：增加联机自然收尾、暂停与跨关就绪合同；保持内存运行范围。

## Impact

强依赖 `cooperative-restaurant-core` 的实际实现与相应 KCP 证据；复用已交付 Solo 前厅、伙伴与关卡，不重复开发核心建树、传输框架或权限。

改动位于 ProductWire、FrontWorkMessages、GridServe／FrontWork／CustomerFlow、RestaurantRuntimeSystem、GridFrontBroadcast／Projection 及现有前厅 HUD，必要时修改原协议源并正常生成。领域行为先沿现有实现，只修远端接入暴露的具体缺口。

迁入原联机案 2.1／2.2／2.3 的前厅和跨关部分。真实 KCP 与同 Editor 受管入口的自然两关结果记录于 [execution-notes.md](execution-notes.md)；最终双 Editor 合作仍由 [原联机案](../cooperative-restaurant-loopback/proposal.md)收口。
