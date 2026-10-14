# Proposal

## Why

原料仅在初始布局生成，成功跨关又不会补满，连续经营最终缺少可生产材料。需要有限采购、到货与实物拆份，使玩家能持续补料而保持唯一库存。

## What Changes

- 在持续厨房接入有限供应余额、采购预留、在途货物和待收货状态，统一业务 Tick 推进。
- 番茄、鸡蛋和面包通过真实包装到货，接收后逐份取出同一材料实例；不直接供应半成品或成品。
- 收货位满保留货物；重复请求不重复预留或收货，拒绝保持原实物。
- Preparing/Operating 允许采购，Draining 禁止新申请；已受理货物继续，暂停冻结到货，跨关保留供应状态。
- 菜单闭包识别正式补货来源；扩展现有 HUD 展示采购、等待、收货与拆份。
- 仅 Solo，数量和 Tick 为可调样机值，不加价格、退款、取消采购或完整经济。验证取舍见 [execution-notes.md](execution-notes.md)。

## Capabilities

### New Capabilities

- `kitchen-supply`：有限采购、到货、收货、包装拆份与持续供应寿命。

### Modified Capabilities

无；菜单来源校验是本供应合同的一部分，现有布局和制作合同保持。

## Impact

前置为 Solo 九案，建议在 [清空/丢弃](../2026-10-14-solo-kitchen-clear-and-discard/proposal.md) 后交接空包装处理。新增 Product 供应源、注册和 Item 包装定义，修改 RestaurantRuntime 单驱动、GridKitchen、菜单闭包、协议源及受管 UI。不做无限来源、餐具凭空补满、新物品架构或供应工具。
