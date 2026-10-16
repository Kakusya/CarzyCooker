# Proposal

## Why

原工位升级目前只支持切配台和烤箱，新菜单使用的榨汁、煎制、搅拌及咖啡工位无法作为经营升级目标。现有校验把加工类别硬绑定到单一设备能力，阻止复用已交付升级流程。

## What Changes

- 对现有灶台、榨汁机、煎台、搅拌机、磨豆机、咖啡机和蒸汽机开放同占地单级升级。
- 复用三类已有作业的冻结耗时、原子安装、保存及合作投影；能力按真实配方类别校验。
- 非终关新菜单增加可兑现升级，终关及已升级设备不提供重复奖励。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `station-upgrades`：扩展已存在加工设备的有限升级。

## Impact

StationUpgradeConfig/LevelBuffSystem 与升级、装修、关卡源表及生成输出。保持原工位/实物/作业、旧定义 ID、wire/file 格式和 owner。不做真机、构建或新增基础设施。验证取舍见 [execution-notes.md](execution-notes.md)。
