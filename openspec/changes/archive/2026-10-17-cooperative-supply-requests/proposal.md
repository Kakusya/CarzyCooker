# Proposal

## Why

成员能自行采购，却不能向队友表达缺哪种来源的补给。用户确认补货请求可由任意有原采购权限成员处理，不增加Host审批。

## What Changes

- 允许成员发布或替换自己的一个补货请求，首版每次请求一个来源的一包，撤销只影响本人请求。
- 查看请求不采购；合法成员明确处理时沿原采购，成功只预留一次配额并显示关联在途事实，失败保留待处理状态。
- 请求不代表货已收到，不自动拆包/补物；已采购后的运输仍归原持续厨房。
- 本关协调状态不写成功档；退出/换关/关店清理提示，不撤销已提交采购或恢复配额。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `kitchen-supply`：增加合作补货请求与原采购明确处理的行为边界。

## Impact

原HostPeer/HostSession、Level协调状态、KitchenSupply命令提交、独立投影和供应HUD。沿Proto源生成尾部新增消息、保持既有opcode；不替代原补货总览、采购权限或配额规则。验证取舍见 [execution-notes.md](execution-notes.md)。
