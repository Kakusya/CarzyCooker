# Proposal

## Why

Solo 橙汁链不能自动证明远端可用。单列网络接入、身份和多人争抢，避免共享代码被误报为联机交付。

## What Changes

- 将已经验收的橙汁、分杯与贴单动作加入显式 wire 白名单和所属 Session 调用。
- 投影壶批次／剩份／订单绑定／杯子状态，所有世界修改仍由 Host 同一裁决。
- 验证分工搬壶传杯、最后一杯、订单竞争和断线交接。

目标模式：Host/Client 内存餐厅。层次：五：完整合作与恢复；依赖按[整体路线](../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不增加饮品配方、联机持久化、自动恢复或新传输；重连能力单列依赖时才验收其组合。

## Capabilities

### New Capabilities

- `cooperative-drinks`：远端饮品动作、贴单投影与多人争抢。

### Modified Capabilities

无。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridClientSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridClientSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridMessages.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridMessages.cs)
- [Unity/Assets/Tests/Editor/ET/CoopCoreCookingTests.cs](../../../Unity/Assets/Tests/Editor/ET/CoopCoreCookingTests.cs)
- [Docs/Product/specs/orders-front-service/spec.md](../../../Docs/Product/specs/orders-front-service/spec.md)

远端字段漏拷贝、杯／订单重复消费和旧绑定是主要风险。用真实 KCP 请求加两端真实输入，复用 Solo 配方算法证据。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮已获三个 change 的实施授权；功能及 UTF 结果逐项回填 execution-notes。提交、归档和第二工作区版本维护仍需对应授权。

