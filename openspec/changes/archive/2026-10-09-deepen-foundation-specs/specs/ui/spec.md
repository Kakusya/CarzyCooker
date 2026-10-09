# Spec Delta

## ADDED Requirements

### Requirement: UI 异步打开的拒绝条件

配置 ID 打开 UI 的异步入口 MUST 在缺少配置时返回失败任务；不允许多实例的资源正在加载或已经打开时，同样返回失败任务。允许多实例只解除该资源重复检查，不改变 ET Component 的同类型唯一关系；需要同类型多个 ETUI 应使用 Child API。

#### Scenario: 必需配置不存在

- **WHEN** 按类型 ID 打开 UI，而公共 UI 表没有该行
- **THEN** 异步入口返回 GameFrameworkException，不用空界面替代配置错误

#### Scenario: 单实例资源仍在使用

- **WHEN** AllowMultiInstance 为 false，且同一资源正在加载或已打开
- **THEN** 新打开请求失败；不把旧实例当作新打开请求的返回结果

### Requirement: UI 打开等待的终态

GF UI 等待器 SHALL 在取消时关闭已打开或在加载中的对应 serial ID，并以取消任务结束；加载完成但取不到 UI 时以失败结束。ETUI Dispose 负责发送取消并关闭可用 GF UI，不能据此承诺所有并发时序已有动态验证或取消即底层资源下载已中止。

#### Scenario: 打开前或等待中取消

- **WHEN** 传入 token 已取消，或等待器轮询期间检测到取消
- **THEN** 已取消 token 不发起打开；等待中的取消关闭对应 serial ID 并返回取消终态

#### Scenario: 打开被其他入口关闭

- **WHEN** UI 已不在加载，但按本次 serial ID 取不到对象
- **THEN** 等待任务失败，不返回一个成功的空 UI；事件等待记录在任务回收时移除

### Requirement: Widget 托管状态

Widget MUST 先由所属 UI 容器注册并建立 UIForm owner，再打开；重复注册、重复打开、关闭未打开对象或移除仍可用对象的严格 API 都拒绝。关闭只退出本轮使用，移除才解除容器关系；动态打开另外刷新所属界面的深度。

#### Scenario: 动态加入并打开 Widget

- **WHEN** 已注册且未打开的 Widget 经 DynamicOpenUIWidget 打开
- **THEN** 执行本轮 OnOpen 并同步 UIForm 的分组与界面深度

#### Scenario: 移除仍打开的 Widget

- **WHEN** 对 Available 为 true 的 Widget 调用严格移除入口
- **THEN** 拒绝移除；调用者先走关闭生命周期，再解除注册关系

#### Scenario: 父界面关闭

- **WHEN** UIForm 关闭并转交 Widget 容器
- **THEN** 对仍可用 Widget 分发 OnClose；不能把 Close 当成永久销毁所有注册 Widget

## Sources

- [配置 ID 的异步入口](../../../../../../Unity/Assets/Scripts/Game/UI/Common/UIExtension.Awaitable.cs)
- [ET owner API](../../../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Module/UI/UIComponentSystem.cs)
- [ETUI](../../../../../../Unity/Assets/Scripts/Game/ET/Loader/UGF/UIForm/UGFUIForm.cs)
- [GF UI 等待器](../../../../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/Awaitable/Awaitable.UIComponent.cs)
- [Widget 容器](../../../../../../Unity/Assets/Scripts/Game/Container/UIWidgetContainer.cs)
