# Spec Delta

## ADDED Requirements

### Requirement: 通用操作帮助入口可访问

界面 SHALL 提供以下行为：提供可随时开关的操作帮助，说明当前实际点击／拖动规则、目标与动作区、普通拿放／容器处理、工作与暂停，以及 Solo／Host／Client 权限差异；链接原菜谱与相关入口。

#### Scenario: 通用操作帮助入口的正常使用
- **WHEN** 新玩家在准备和营业阶段打开操作帮助并切到菜谱后返回
- **THEN** 能找到当前真实输入与权限说明，阅读不发动作，关闭后原点击和拖动仍按原规则工作

### Requirement: 通用操作帮助入口保持事实与操作边界

界面 MUST 遵守：帮助只解释当前已实现输入，不宣称不存在的快捷键、手柄或自动操作；不强制完成教程、不持久化教学进度，阅读不自动暂停／恢复、改变物品或发命令。

#### Scenario: 通用操作帮助入口的失效或取消
- **WHEN** Client 或暂停中阅读帮助，HUD 重开后旧帮助按钮触发
- **THEN** 不开放 Host 控制或自动恢复，旧回调不改新视图，原单次动作不重复

#### Scenario: 关闭重开后旧页面回调
- **WHEN** 本入口关闭重开或换关后旧按钮回调再次触发
- **THEN** 不改变新上下文，不重复原命令；当前合法操作仍可使用

## Sources

- [现有实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)、[UI 主规格](../../../../../specs/restaurant-ui/spec.md)。
- [设计](../../design.md)、[验收边界](../../execution-notes.md)。

本增量描述本案入口合同，实际交付与逐项运行结果见 execution-notes，不以规格存在代替验收。
