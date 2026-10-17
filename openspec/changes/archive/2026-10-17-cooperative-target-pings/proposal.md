# Proposal

## Why

队友状态只说明正在做什么，无法主动指明需要交接的锅、材料或餐桌。增加短时共享目标标记，减少依赖口头解释坐标的沟通。

## What Changes

- 当前餐厅成员可从真实选中工位/物品/餐桌发送标记；双方看到发送者及目标，物品移动后跟随同一实物身份。
- 每名成员同刻一个标记，新标记替换本人旧标记；可主动取消，10个业务秒后失效，暂停冻结，目标消失/成员退出/换关清理，恢复旧绑定不重发。
- 标记只沟通，不移动角色、认领订单/工位或获得操作权；拒绝旧世界、旧绑定和非法目标，不创建新监听。

## Capabilities

### New Capabilities

- `cooperative-target-markers`：成员主动标记当前真实目标的共享展示、时效与收尾。

### Modified Capabilities

无。

## Impact

原HostPeer→HostSession统一裁决、Level短寿命状态、Grid投影及原受管Board/HUD；本地不向自身发UDP，远端沿原wire白名单/Proto生成。目标可查看而不能远程操作，工位/物品沿原唯一身份。运行树和最小验收在后续design/tasks明确。
