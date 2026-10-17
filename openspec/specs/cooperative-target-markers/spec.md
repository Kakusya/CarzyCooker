# cooperative-target-markers Specification

## Purpose

定义合作餐厅成员主动标记真实工位、物品或餐桌的共享沟通行为。标记具有发送者、稳定目标及有限业务寿命，只帮助队友发现交接位置，不改变物品、订单、角色移动、工作认领或现有操作权限，退出及旧绑定不能留下可写目标。

## Requirements

### Requirement: 成员可共享当前真实目标

当前合法成员 SHALL 能标记真实工位、物品或餐桌，两端显示发送者与同一目标；物品移动后标记跟随原实物身份。每人同时一个标记，新标记替换本人旧标记，可主动取消。接收者选择只查看，不移动角色、认领工作、绑定或交付；旧世界/关/绑定、未知目标拒绝且不改变现有标记。

#### Scenario: 物品由队友拿走
- **WHEN** 玩家标记锅后队友合法拿起同一锅
- **THEN** 两端标记仍指向原锅及当前持物者，不留在旧台面或授予远距离拿取权

### Requirement: 标记有限时效和退出收尾

标记 MUST 在10个业务秒后失效，暂停冻结；目标消费、发送者退出或失联、换关及餐厅退役清理适用标记，恢复新绑定不重放旧标记。重复请求不延长同次标记或重复广播效果，旧UI回调不得创建新轮标记；标记不写成功存档或停止共用sender。

#### Scenario: 暂停失联和恢复
- **WHEN** 标记期间暂停后恢复，发送者随后失联并手动恢复
- **THEN** 暂停不扣寿命，失联清原标记；恢复显示当前合法标记集合，旧请求不能复活已清标记

## Sources

- [实际领域与消息](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/CooperativeTargetPingCommands.cs)。
- [逐功能验收](../../changes/archive/2026-10-17-cooperative-target-pings/execution-notes.md)。
- [业务边界](../../../Docs/Product/boundaries.md)。
