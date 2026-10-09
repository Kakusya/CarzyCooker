# Spec Delta

## ADDED Requirements

### Requirement: ET 所有者树退出

ET Entity MUST 通过既有 Component/Child 关系持有下级对象；Dispose 首先使当前 InstanceId 失效，再递归销毁 children 和 components，随后分发当前对象的 IDestroy 并解除父关联。重复 Dispose 不再次执行同一轮树清理；不能从父 Destroy 回调中假定下级仍可用。

#### Scenario: 父对象销毁

- **WHEN** 已持有 children 或 components 的 ET owner 执行 Dispose
- **THEN** 下级先退出，当前 IDestroy 后执行；当前对象的实例身份已失效

#### Scenario: 移除一个下级对象

- **WHEN** 通过 RemoveChild 或 RemoveComponent 移除仍存在的下级
- **THEN** 进入其 Dispose 路径，不仅删除容器记录而遗留生命周期

### Requirement: 异步引用的实例身份

跨 await 保存 ET 对象的调用者 MUST 区分业务 Id 与本轮 InstanceId。现有 EntityRef 在解引用时核对 InstanceId，不匹配时返回 null；该机制不自动取消正在运行的任务，也不使普通强引用具备同样检查。

#### Scenario: 等待期间对象被销毁或复用

- **WHEN** await 前取得的 EntityRef 指向对象，其 InstanceId 在等待期间改变
- **THEN** 解引用不返回旧生命周期对象；调用者按所属行为处理退出，不能继续把旧引用应用到新实例

#### Scenario: 正常仍存活的引用

- **WHEN** 对象 InstanceId 与捕获值一致
- **THEN** EntityRef 返回对应对象；业务 Id 相同本身不足以证明实例仍是原生命周期

## Sources

- [Entity 生命周期与移除 API](../../../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Entity/Entity.cs)
- [EntityRef](../../../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Entity/EntityRef.cs)
- [新增代码的异步约定](../../../../../../references/business-code-conventions.md)
