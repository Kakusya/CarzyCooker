# Spec Delta

## MODIFIED Requirements

### Requirement: 菜谱对应有效配方

菜谱 SHALL 展示当前已开放菜品实际需要的原料、加工步骤、精确份数、设备及容器，名称和要求与生效定义一致；无效引用不得被显示成可执行配方，不限于旧汤、吐司、橙汁。提示只解释当前实际阶段与缺口，不自动完成动作。

#### Scenario: 查看汤和吐司
- **WHEN** 玩家打开任一已开放菜谱
- **THEN** 可找到完成制作和交付需要的依赖，不出现免费的原料或不存在的设备

#### Scenario: 选择新菜单菜品
- **WHEN** 玩家查看新包中已开放菜谱且已有部分备料
- **THEN** 提示读取当前有效定义和实物，显示剩余步骤，无须重做或免费生成材料

## Sources

- [提案](../../proposal.md)、[设计](../../design.md)、[验收取舍](../../execution-notes.md)。
- [本轮业务扫描与优先级](../../../../../../Docs/Product/full-version-roadmap.md)。
- [Design/Excel/ET/Datas/Product/LevelConfig.xlsx](../../../../../../Design/Excel/ET/Datas/Product/LevelConfig.xlsx)。
