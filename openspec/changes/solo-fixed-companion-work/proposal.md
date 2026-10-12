# Proposal

## Why

代表菜需要询问开单和餐具清洗，长期目标用固定伙伴减轻这些重复操作。伙伴身份要持续，工作和成长要随当前关卡结束。

## What Changes

- 单机增加一位可见固定伙伴作为可调样机内容，固定岗位自主处理询问与洗碗；最终人数与名字仍未定。
- FrontWork 归 Level，伙伴和玩家都通过真实 ET 本地请求认领；同一工作只有一个执行者，停手保留进度。
- 伙伴实际完成任务才计成长；第三次完成解锁 `max(1, ceil(baseWashTicks / 2))`，领取时冻结耗时。
- 伙伴走真实格子路径，不能远处瞬间完成；关尾按本轮用户决定自然收口。
- 不招募、换岗、加工资疲劳槽或复杂 AI 框架；用户已授权完成九个 change，按 tasks 实施和验收。

## Capabilities

### New Capabilities

- `fixed-companions`：固定身份、前厅工作执行、玩家接手与关内成长。

### Modified Capabilities

无。

## Impact

依赖 [餐厅流程](../solo-restaurant-level-flow/proposal.md)，复用 [本地消息](../solo-local-message-contracts/proposal.md)；负责 FrontWork 数据／认领／推进，顾客 change 负责生成真实询问／清洗需求，双方按接口先后接入，避免依赖循环。适用 CompanionConfig、Model/Hotfix、Board 的受管角色显示。[执行说明](execution-notes.md) 逐项区分已验证与 NotRun。
