# Proposal

## Why

单机设施清理已交付，供应正在实施；这些能力不会因采用共同厨房自动成为网络功能。联机需要真实到位的清理、有限补货和货物争抢，才能在耗尽初始材料后继续营业。

## What Changes

- 显式开放已交付的水池接水／排液、垃圾桶倾倒、采购／接收及包装拆份请求，复制完整预期身份与内容。
- 所有已加入成员可按原资格采购、收货和清理；保持到位、容量、卫生、加工锁定及有限配额，不能随身清空。
- 两端共享同一 SupplyComponent／Shipment、收货槽及原包装物品；重复、满槽、争抢与失联不重复扣额或生货。
- 将供应、包装与设施状态放入既有同版投影，验证暂停、结算、在途换关和后加入可见性。
- 不新增供应品类、货币、无限补货、杯子／饮品、装修、存档、重连或新传输。

## Capabilities

### New Capabilities

- `cooperative-kitchen-logistics`：联机厨房有限物资补给、设施处理与跨关物流一致性。

### Modified Capabilities

无；沿用核心案的可信入口规则，新增领域合同不再重复修改 product-session。

## Impact

依赖 [核心](../2026-10-14-cooperative-restaurant-core/proposal.md)、[前厅](../2026-10-14-cooperative-front-service/proposal.md)、已交付 [Solo 清理](../2026-10-14-solo-kitchen-clear-and-discard/proposal.md)和实际完成的 [Solo 供应](../2026-10-14-solo-supply-delivery/proposal.md)。供应未交付时可阅读设计，不能据此标记联机补货已具备或用临时物资替代验收。

接入 ProductWire、SupplyMessages、GridCleanup／GridSupply、既有客户端值调用及 HUD／投影，协议按实际前置源生成。本案不改原料平衡或另设库存；如实施必须改表，先加载 Luban 技能并改源导出。

迁入原联机案 2.x 的清理、供应和货物边界；最终双 Editor 验收归 [原联机案](../2026-10-14-cooperative-restaurant-loopback/proposal.md)。风险与最小验收见 [execution-notes.md](execution-notes.md)。
