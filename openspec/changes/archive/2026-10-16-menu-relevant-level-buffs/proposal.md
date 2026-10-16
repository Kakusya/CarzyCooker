# Proposal

## Why

沙拉、热餐、甜品和咖啡扩展后，允许加成池仍只有原汤、番茄切配和吐司烘烤效果。玩家可能必须选择本关完全用不到的加成。

## What Changes

- 为已交付配方补充有限加成定义，保持原效果和 ID。
- 新菜单配置只提供依赖闭包内的适用效果，启动参数仍冻结且不重算旧作业。
- 保留 Host 选择权限、远端只读及现有跨关释放流程。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `level-buffs`：新内容菜单加成必须具有实际可用加工目标。

## Impact

修改 BuffConfig、LevelConfig 源工作簿及正常导表输出，必要消费者和最小 UTF；不增加菜品或文件/协议格式。旧已保存作业不变，不做真机或构建。验证取舍见 [execution-notes.md](execution-notes.md)。
