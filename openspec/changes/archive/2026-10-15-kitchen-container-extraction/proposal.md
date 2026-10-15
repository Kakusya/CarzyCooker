# Proposal

## Why

GridAction 没有普通内容取出动作；GridSupplySystem 的 TakeSupplyPortion 要求来源匹配 SupplyConfig 包装定义。当前普通碗/盘/锅只能整体拿放或沿倾倒/加工/设施清理处理，尚不能从未加工容器取回指定固体。

补齐已确认基础“取出”的有限安全子集，让投入容器但尚未加工的固体可以回到空手，而不是只能把内容丢掉。

## What Changes

- 空手玩家在合法台面操作位，从未加工且未锁定的普通容器选择一个真实固体 ItemId 取出。
- 同一实物从容器移到手槽，外层容器身份/位置/脏净及其他内容保持；默认拿走整个容器的行为仍保留。
- 本地、远端、场景目标和 HUD 详情使用相同动作/预检/结果。

优先级：**P0**；建议优先补齐此局部基础动作，与游戏入口案独立。无新案硬前置。

范围外：不允许徒手取液体、不拆完成批次、不取消或拆开已开始/暂停加工，不开放容器嵌套；包装拆份、倾倒与按份盛装保持原语义。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `kitchen-item-processing`：普通容器指定未加工固体取出，保持既有加工/分装锁定。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSupplySystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSupplySystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs)
- [Docs/Product/design.md](../../../../Docs/Product/design.md)

低到中等局部实物转移风险，重点防重复位置与错取。用未加工鸡蛋/盘中固体、满手/错目标/锁定拒绝、双人抢取和旧请求的小集，再检查普通 Editor 操作；不为此重构容器体系。 取舍和验收映射见 [execution-notes.md](execution-notes.md)。

用户随后授权完成全部 16 案，本案按原有限范围实施并运行相关 Editor/UTF；不包含真机、物理 LAN、Player/AOT 或构建，不提交、push 或归档。总体安排见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
