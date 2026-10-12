# Proposal

## Why

当前厨房随临时样机退出，尚无准备、营业、收尾和下一关。需要先分清持续厨房与短期关卡，才能让顾客、伙伴和 buff 有正确归属。

## What Changes

- 建立单机 RestaurantRuntime 与 Level：Preparing → Operating → Draining → Completed；继续时在同一家餐厅创建下一 Level。
- **BREAKING**：产品玩法树从 Match.GridKitchen 迁至 RestaurantRuntime 的持续厨房，角色归当前 Level；同步现有消费者与快照身份。
- 按用户本轮决定：暂停取消未执行动作，已开始工作停钟保留进度；前厅按真实进度自然收口。
- 成功订单固定加分、零星也可继续；成功换关保留厨房实物与加工，重置本关对象。
- 仅内存单机试玩，不声称检查点已写盘；存档、许可缩小、网络和装修奖励另案。用户后续已授权并完成本轮九案apply。

## Capabilities

### New Capabilities

- `restaurant-level-lifecycle`：单机准备、营业、收尾、暂停与内存换关。

### Modified Capabilities

- `product-session`：补充正式单机餐厅的关卡身份和角色／厨房寿命边界；保留既有临时样机合同。

## Impact

前置 [本地消息](../solo-local-message-contracts/proposal.md)。迁移 ProductRuntimeBootstrap、GridKitchen/System/Messages、ProductAuthority、投影及 fixture 的 owner 查询；新增关卡源表，使用现有 Luban。顾客/NPC 后续接入同一 Tick。实际存档接入前应补专门 change；风险与 NotRun 见 [执行说明](execution-notes.md)。
