# Spec Delta

## ADDED Requirements

### Requirement: 操作拒绝与处理导航可访问

界面 SHALL 提供以下行为：围绕手满、加工锁定、容器不符、不可达、生产许可、收货台满、旧目标及保存失败，提供可理解原因和对应查看入口；未知原因保留可复制诊断信息。

#### Scenario: 操作拒绝与处理导航的正常使用
- **WHEN** 一次采购因收货条件或一次加工因容器条件被拒绝，玩家打开对应说明
- **THEN** 原因和下一步查看目标准确，原物品／配额不变，阅读不重试原动作

### Requirement: 操作拒绝与处理导航保持事实与操作边界

界面 MUST 遵守：建议根据当前真实投影提供，不自动重试、丢弃物品、解绑、采购或改布局；查看帮助与实际业务动作清楚分开。旧命令结果不覆盖当前命令反馈，接收成功不冒称执行成功。

#### Scenario: 操作拒绝与处理导航的失效或取消
- **WHEN** 较早的失败结果迟到、原因未知或目标已改变
- **THEN** 不覆盖当前成功，不对新目标给旧操作建议；未知原因可诊断但不猜测或自动执行

#### Scenario: 关闭重开后旧页面回调
- **WHEN** 本入口关闭重开或换关后旧按钮回调再次触发
- **THEN** 不改变新上下文，不重复原命令；当前合法操作仍可使用

## Sources

- [现有实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)、[UI 主规格](../../../../../specs/restaurant-ui/spec.md)。
- [设计](../../design.md)、[验收边界](../../execution-notes.md)。

本案新增入口已实施并按正常、失效、重开/换关及真实KCP/文件路径运行验收，逐项映射见 execution-notes；原底层证据仅复用行为未变部分，不代替新入口验收。
