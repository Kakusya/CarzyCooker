# Proposal

## Why

Solo 已有单级原位升级与冻结耗时；CompleteRenovationPreparation 对 RewardKind=2 在非 Solo 模式直接跳过，合作候选只有新增工位。

把已交付的原工位升级带入合作餐厅，补齐一个成本小且可见的经营差异。

## What Changes

- Host 可从合法候选选定未升级工位，Client 只读目标、预期等级和结果。
- 沿既有装修 Continue 一次安装，保留工位身份、占地、内容及旧作业冻结值。
- 新作业按升级与本关 Buff 合并比例一次取整，旧绑定/旧关不能升级新目标。

优先级：**P0**；建议顺序：**03**。前置：无新增 change 硬前置，复用已交付业务。优先级表示用户价值和依赖，不要求所有案串行。

范围外：不增加升级层数、采购货币、Host 迁移或联机持久化；不要求先做合作三关与房间。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `station-upgrades`：已有单级原位升级扩展为 Host 选择、合作观察。
- `cooperative-renovation`：合作装修允许合法升级奖励及一次安装。

## Impact

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/StationUpgradeConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/StationUpgradeConfig.cs)
- [Unity/Assets/Tests/Editor/ET/StationUpgradeTests.cs](../../../../Unity/Assets/Tests/Editor/ET/StationUpgradeTests.cs)
- [Unity/Assets/Tests/Editor/ET/CoopRenovationTests.cs](../../../../Unity/Assets/Tests/Editor/ET/CoopRenovationTests.cs)

实施门槛：无新增必要产品裁决，沿用已有单级规则。

风险是双装升级或重算旧加工。相关 KCP/受管 HUD 小集覆盖重复、换关、目标变化和作业前后耗时；不扩大原 Solo 整套回归。 具体取舍与未来验收映射见 [execution-notes.md](execution-notes.md)。

本轮授权为业务扫描和建立 change；所有实施任务未完成。只规划 Editor/UTF 验收，不包含真机、物理 LAN、Player 或 AOT 实测。此规划不执行运行、改表/生成、提交、push 或归档。总体证据与优先级见[完整版路线](../../../../Docs/Product/full-version-roadmap.md)。
