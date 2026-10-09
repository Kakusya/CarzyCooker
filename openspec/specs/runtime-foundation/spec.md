# 公共 GameEntry、ET 分层与框架示例的职责

## Purpose

记录当前 CarzyCooker 底座的公共 GameEntry、ET 分层与框架示例的职责。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 公共组件与层

业务 SHALL 复用 GameEntry 公共组件，ET Model/ModelView 声明数据与视图关联，Hotfix/HotfixView 实现系统；程序集由 asmdef/csproj 决定。

#### Scenario: 公共组件与层的适用行为

- **WHEN** 新增组件或系统
- **THEN** 放在实际运行模式对应层，保持 owner 与视图边界

### Requirement: 示例隔离

文档 MUST 把 ET Demo/LockStep/Benchmark 等标为现有示例，不将其能力写成做饭领域已经实现。

#### Scenario: 示例隔离的适用行为

- **WHEN** 索引示例战斗、移动、AOI 或锁步代码
- **THEN** 用作可复用实现定位，不能增加本次玩法范围

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

- [GameEntry.cs](../../../Unity/Assets/Scripts/Game/Base/GameEntry.cs)
- [GameEntry.Game.cs](../../../Unity/Assets/Scripts/Game/Base/GameEntry.Game.cs)
- [Share](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share)
- [Share](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share)
- [Server](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Server)
- [Entity 生命周期与移除 API](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Entity/Entity.cs)
- [EntityRef](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Entity/EntityRef.cs)
- [新增代码的异步约定](../../../references/business-code-conventions.md)
