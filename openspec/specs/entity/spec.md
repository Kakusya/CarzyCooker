# GF Entity、ETEntity 与 UIEntity 的类型、实例及视图边界

## Purpose

记录当前 CarzyCooker 底座的GF Entity、ETEntity 与 UIEntity 的类型、实例及视图边界。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 类型与实例

GF Entity SHALL 使用配置类型 ID 解析资源、分组和优先级，再分配运行 serial ID；ET 的 Mono view 通过对应 UGF 生命周期取得 userData 与视图，不恢复已删除的 GameHot EntityData 示例。

#### Scenario: 类型与实例的适用行为

- **WHEN** 显示一个配置类型的实例
- **THEN** 类型 ID 与运行 ID 分开，userData 满足所选逻辑基类的契约

### Requirement: ET Entity 所有权

GFEntityComponent MUST 通过 Component/Child API 管理 UGFEntity；UGFEntity.Dispose 取消加载并隐藏关联 GF Entity，Show 完成后才能消费 View。

#### Scenario: ET Entity 所有权的适用行为

- **WHEN** owner 移除 ET Entity 或加载期间 Dispose
- **THEN** 由现有生命周期清理，不能把 GF Hide 等同于销毁 ET 对象

### Requirement: UIEntity 独立路由

UIEntity MUST 保持独立 DTUIEntity、资源根和生成 ID；使用调用 overload 前核对实际实现，不能把同步泛型 overload 的现存路由差异写成正确保证。

#### Scenario: UIEntity 独立路由的适用行为

- **WHEN** 需要显示 UIEntity
- **THEN** 使用真实查 DTUIEntity 的路径，已有同步泛型差异见已知问题

## Sources

- [EntityExtension.cs](../../../Unity/Assets/Scripts/Game/Entity/EntityExtension.cs)
- [UGFEntity.cs](../../../Unity/Assets/Scripts/Game/ET/Loader/UGF/Entity/UGFEntity.cs)
- [GFEntityComponentSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Module/GFEntity/GFEntityComponentSystem.cs)
- [known-issues.md](../../../KnownIssues.md)
