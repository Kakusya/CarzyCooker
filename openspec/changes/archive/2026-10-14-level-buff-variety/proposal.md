# Proposal

## Why

现有 Buff 只加速煮汤，候选对两道菜的选择价值有限。扩到已有手工切配和烘焙即可形成差异，无需新玩法系统。

## What Changes

- 扩展 BuffConfig 明确的效果种类及配方目标，作用于切配和烘焙。
- 在新作业开始时冻结耗时，保留已开始／跨关作业，暂停继续停钟。
- Host 选择、远端只读及同版投影沿用已有权限；分模式验收。

目标模式：Solo 与已交付合作核心。层次：四：内容选择与消息边界；依赖按[整体路线](../../../../Docs/Product/change-roadmap.md)与 design 核对，不把层次当作所有案串行的硬门槛。

范围外：不加多重 Buff 叠加、额外产量、免费材料、全工序或随机效果平台。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `level-buffs`：新增切配／烘焙加速并保持启动时冻结。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/BuffConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/BuffConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/LevelBuffSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/LevelBuffSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridBakeSystem.cs)
- [openspec/specs/level-buffs/spec.md](../../../specs/level-buffs/spec.md)

效果可能重复缩时、影响旧作业或由远端越权选择。按新旧作业对照、真实本地与 KCP 权限小集验收；不测全菜目录。验证取舍与逐任务映射见 [execution-notes.md](execution-notes.md)。本轮已授权实施，相关配置／UTF／普通 Launcher 均已验收，构建 NotRun；不执行提交、归档或第二工作区操作。

