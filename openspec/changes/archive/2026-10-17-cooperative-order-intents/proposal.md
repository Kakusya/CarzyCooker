# Proposal

## Why

合作玩家能查看同一订单，但无法表达自己打算制作哪一单，容易重复备菜。用户已确认采用提示性的制作意向，不增加独占权或交付限制。

## What Changes

- 成员可显式设置、改选和撤销自己的当前订单制作意向，一人同时保留一个，同单可以显示多人。
- 意向仅作共享提示，不抵扣需求、不认领工作、不贴单，也不限制其他成员制作或交付。
- 订单结束、成员失联/退出、换关及餐厅关闭清理对应意向；手动恢复后需重新设置。
- 使用原订单导航与Host统一裁决；无历史记录、自动分工、奖励或存档字段。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `orders-front-service`：补充合作订单的非独占制作意向、身份与退出边界。

## Impact

涉及原Level订单owner、HostPeer/HostSession消息处理、独立前厅投影及原订单HUD。新增消息由Proto源生成并保留现有opcode；不改原出餐或消费规则，不重复建设营业总览/队友详情。严谨程度与最小验收见 [execution-notes.md](execution-notes.md)。
