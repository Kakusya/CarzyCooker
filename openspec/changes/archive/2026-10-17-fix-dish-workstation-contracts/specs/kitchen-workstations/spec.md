## ADDED Requirements

### Requirement: 载具烘烤布局保留完整关系

准备态合法调整带有烘烤作业的工位时，系统 MUST 区分直接输入和载具内部输入。载具存在时验证工位、载具、唯一内容与加工的双向关系及进行中状态；完整通过后保留同一对象和冻结耗时，更新载具及内容的位置。任何关系不符须整次拒绝。

#### Scenario: 移动旋转带烘烤中模具的烤箱
- **WHEN** 未暂停准备态将带真实模具和未完面糊作业的烤箱移动或旋转到合法位置
- **THEN** 原模具、面糊、作业和已有进度保持，后续只完成一次蛋糕并能从新操作格拿取和脱模

#### Scenario: 载具加工关系失效
- **WHEN** 载具作业引用错误输入、丢失反向加工引用或内容不再唯一
- **THEN** 拒绝布局操作，位置、方向、布局版本、实物与加工进度不因该拒绝部分提交

## Sources

- [布局裁决](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridStationLayoutSystem.cs)
