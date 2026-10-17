# Spec Delta

## ADDED Requirements

### Requirement: 本机记录可命名和收藏

选档界面 SHALL 为现有记录提供设置/清除显示名、添加/取消收藏及收藏筛选；原Slot、revision、来源和章节仍可查看。名称限制32个Unicode文本元素且不得含换行或控制字符，空名恢复原名称。元数据只属于本机repository/Profile/Slot，不传播到他人分支或设备，不改成功载荷、签名和领取身份，不删除、覆盖或移动进度。

#### Scenario: 两个来源相关但独立的检查点
- **WHEN** 玩家为两个独立Slot分别命名，并只收藏其中一个
- **THEN** 收藏筛选只列被选记录，各自原身份和文件保持，源档与后续新Slot不自动继承改名

### Requirement: 元数据失败和旧页面不改变进度

名称或收藏提交 MUST 完整验证当前记录身份，成功持久化后才显示已保存；取消和失败保留原元数据及原成功文件，允许重试。未知/损坏元数据明确提示，不将有效产品档判损坏或静默覆盖。旧选择回调不得修改另一Slot；继续仍通过原Select完整校验和当前revision确认。

#### Scenario: 文件失败与选档重开
- **WHEN** 名称保存失败后玩家重开选档并触发旧记录按钮
- **THEN** 原名称、收藏和进度完整；旧按钮不作用另一记录，显式重试成功后重新进入仍显示新名

## Sources

- [原选档检查](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSaveInspection.cs)。
- [设计与验收范围](../../design.md)。
