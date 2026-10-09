# 资源、事件、实体及 Widget 容器的所有权

## Purpose

记录当前 CarzyCooker 底座的资源、事件、实体及 Widget 容器的所有权。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 生命周期容器

AExUIForm/AExEntity SHALL 复用已有事件、实体、资源和池容器；派生层只清自己直接拥有的资源，不能重复全量清理。

#### Scenario: 生命周期容器的适用行为

- **WHEN** 托管对象关闭或隐藏
- **THEN** 由 base/容器清本轮所有权，保留对象池与重复进入契约

### Requirement: 配置资源路由

资源加载 MUST 复用 AssetUtility、GameEntry.Resource、ResourceContainer 和既有 AssetSet 流程；路径源于配置与对应资源根，不另建同义缓存/loader。

#### Scenario: 配置资源路由的适用行为

- **WHEN** 为 Image/RawImage 或业务 view 装载资源
- **THEN** 先选现有资源设置/容器，取消和回收归明确 owner

## Sources

- [AExUIForm.cs](../../../Unity/Assets/Scripts/Game/UI/Common/AExUIForm.cs)
- [AExEntity.cs](../../../Unity/Assets/Scripts/Game/Entity/EntityLogic/AExEntity.cs)
- [ResourceContainer.cs](../../../Unity/Assets/Scripts/Game/Container/ResourceContainer.cs)
- [EntityContainer.cs](../../../Unity/Assets/Scripts/Game/Container/EntityContainer.cs)
- [EventContainer.cs](../../../Unity/Assets/Scripts/Game/Container/EventContainer.cs)
- [UIWidgetContainer.cs](../../../Unity/Assets/Scripts/Game/Container/UIWidgetContainer.cs)
- [AssetSet.md](../../../Book/AssetSet.md)
