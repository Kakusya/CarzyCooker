# Proposal

## Why

当前已有动作分组与拒绝说明，但玩家仍需在动作名中推断手持物和目标之间的关系。上轮真实操作中，拿盘对烤箱尝试“添加”被拒绝，说明方向和前提没有在操作处讲清。本案让下一步更直接，同时保留既有裁决。

## What Changes

- 在当前目标区展示一个主要动作、最多两个相关次要动作，以及“更多动作”；候选由当前手持物、目标、阶段和权限推导。
- 将“拿取什么、放到哪里、加工什么、向什么分装、交给谁”写入动作标签；等待到位、加工中、已完成分别反馈。
- 不具备操作条件时显示一条明确原因及只读定位/说明；无法判定时不假称可执行，最终仍由原 Host 裁决。
- 保留全部合法动作、原地图单击语义与拖动隔离。拿盘对烤箱的例子提供合法步骤说明，不自动交换物品，不增设双向装盘规则。
- 适用于 Solo、Host 本地和远端 Client 的厨房/前厅个人动作；管理控制仍遵守原角色权限。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-ui`：补充当前目标动作排序、明确标签和失效后的处理。

## Impact

- 主要修改 `GridPresentationActionNavigation`、已有动作构建/拒绝提示和 ModelView 选择；复用 `GridClientSystem.CounterIntent` 等原意图构造与 `CallProductValueAsync`。
- 结构依赖 [HUD 布局](../2026-10-20-product-hud-readable-detail-layout/proposal.md)；[首局路径](../2026-10-20-product-first-session-path/proposal.md) 可引用本案动作，但无需另建推荐器。`container-operation-preview` 等计划仍负责详细预览，本案不吞并。
- 不改 GridAction 的领域语义、配方、服务器协议、存档、寻路或执行队列。主要风险是推荐误导、目标竞态和重复请求；通过真实拿放/加工/分装/交付和远端失效目标小集验收，见 [execution-notes.md](execution-notes.md)。
- 2026-10-20：后续已按用户实施授权完成5/5任务及对应运行验收，详见[执行记录](execution-notes.md)。不改变原领域动作语义；已归档并获授权本地提交，未推送。
