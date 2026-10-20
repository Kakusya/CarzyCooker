# Proposal

## Why

独立手作小馆样板已交付，用户现已授权加入实际项目。当前生产美术覆盖63种物品，须保留新增玩法资源，并让新动作消费已有投影及真实结果。

## What Changes

- 将样板203帧合并为新增动画图集，并从原始帧清单生成切片与手持点布局；由现有Board Prefab持有引用。
- 本地玩家显示四方向八帧待机／行走／端物／手工切配，真实交付成功后显示一次递餐；镜头和权威状态保持。
- 新器具、吐司、切配台／炉台／水槽、窗／植物替换对应表现，汤／橙汁及蒸汽使用逐帧动画；其余63物品与16工位中的未覆盖资源继续使用原图集。
- 补充最小相关UTF、真实Launcher运行、生命周期检查和资源集合刷新。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-art`：明确已接入动画与手持绑定、真实内容、暂停与回收行为。

## Impact

修改原ModelView Board和HotfixView显示调用、Board Prefab与美术源／输出；不改表、协议、业务裁决或HUD结构。复用现有UGF owner、Pipeline和UTF。投入与未运行边界见execution-notes.md。用户已明确授权接入及必要验收，规划后继续apply。
