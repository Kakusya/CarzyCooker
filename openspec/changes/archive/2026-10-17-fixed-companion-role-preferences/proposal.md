# Proposal

## Why

小麦当前固定先询问、后洗碗，玩家不能调整下一项工作的优先级。用户确认由Solo玩家或Host选择自动、询问优先、洗碗优先，已开始工作不中断。

## What Changes

- 提供三种优先偏好；自动保持原询问优先，偏好类无合法可用工作时仍回退另一原有类别。
- 设置只改变下一次空闲调度顺序，不中断已认领/前往/执行，不重置进度、冻结耗时或成长。
- Solo/Host明确修改，合作Client只读；仍只有小麦、询问与洗碗，不新增清桌能力或人工逐项派工。
- 偏好本轮连续关卡保留，关闭餐厅后新开恢复自动；不写成功检查点或新增人物成长存档。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `fixed-companions`：固定工作资格下的可选调度优先级、控制权限与寿命。

## Impact

原Restaurant/CompanionRole和GridCompanionSchedule、Host统一控制消息、独立伙伴投影及原伙伴HUD。运行enum不改表；Proto改源生成并保持旧opcode。与原人物/成长说明案分别负责改变优先级和只读解释，风险/最小验收见 [execution-notes.md](execution-notes.md)。
