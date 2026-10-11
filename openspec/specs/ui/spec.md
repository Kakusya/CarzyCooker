# GF UI、ETUI 与所有者生命周期

## Purpose

记录当前 CarzyCooker 底座的GF UI、ETUI 与所有者生命周期。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 表驱动打开

GF UI SHALL 根据 UI 配置的 AssetName、UIGroupName、多实例与覆盖暂停字段打开界面；公共 UIFormId 与 ET UGFUIFormId 区分，均复用 GF 生命周期；本分支不含 GameHot UI。

#### Scenario: 表驱动打开的适用行为

- **WHEN** 通过 GameEntry.UI 或 ET UIComponent 打开已配置界面
- **THEN** 使用既有资源路由与分组，不手动调用 OnOpen 或直接失活托管根

### Requirement: ET UI 所有权

UIComponent MUST 通过 AddUIFormComponentAsync 管理同类型唯一实例，通过 AddUIFormChildAsync 管理多实例；UGFUIForm.Dispose 取消在途打开并关闭已可用 GF UI。

#### Scenario: ET UI 所有权的适用行为

- **WHEN** 逻辑 owner 移除或 Dispose ETUI
- **THEN** 执行 ET 与 GF 关联退出；仅设置 Visible 不等于销毁业务对象

### Requirement: 生命周期分发

ET UI SHALL 由声明的 IUGFUIForm 生命周期接口与 UGFUIFormSystem 分发，System 通过 View 使用 Mono 绑定。

#### Scenario: 生命周期分发的适用行为

- **WHEN** 声明 OnOpen/OnClose marker 并实现对应 system
- **THEN** 由框架分发本轮回调，Mono 不另持有重复业务状态

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

### Requirement: 界面本轮所属异步工作

公共界面、Widget 及迁移后的动态 Widget 加载 MUST 关联实际界面本轮 owner，统一托管其资源和普通异步工作。关闭或销毁 SHALL 使本轮失效并取消业务等待；重新打开使用新轮次，旧加载不能创建、绑定或改写新界面。可见性变化和表现暂停不自动等同于关闭。

#### Scenario: 动态 Widget 尚在加载时父界面关闭

- **WHEN** 动态 Widget 仍在加载，父界面关闭而加载 Mono 尚未销毁
- **THEN** 按父界面本轮 owner 取消等待，晚到结果不实例化或绑定旧 Widget，并由原请求收尾

#### Scenario: 界面关闭后重新打开

- **WHEN** 同一个池化界面关闭后再次打开，同时旧异步工作完成
- **THEN** 新轮次工作有效，旧工作不能写回新界面，也不能清除新轮次登记

#### Scenario: 界面只被遮挡或暂停表现

- **WHEN** 界面仍属于同一打开轮次，只改变可见性或表现暂停
- **THEN** 不以此取消所属任务；真实关闭和销毁仍负责失效及收尾

#### Scenario: 未消费加载结果

- **WHEN** 界面启动受管资源加载后未 await 或读取结果，随后关闭
- **THEN** 公共入口仍取消本轮业务等待并负责资源归属，不要求派生界面逐次补写清理

## Sources

- [AExUIForm.cs](../../../Unity/Assets/Scripts/Game/UI/Common/AExUIForm.cs)
- [UIComponentSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Module/UI/UIComponentSystem.cs)
- [UGFUIForm.cs](../../../Unity/Assets/Scripts/Game/ET/Loader/UGF/UIForm/UGFUIForm.cs)
- [UGFSystemSingleton.cs](../../../Unity/Assets/Scripts/Game/ET/Loader/UGF/UGFSystemSingleton.cs)
- [UIFormLoginComponent.cs](../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Demo/UI/UILogin/UIFormLoginComponent.cs)
- [配置 ID 的异步入口](../../../Unity/Assets/Scripts/Game/UI/Common/UIExtension.Awaitable.cs)
- [GF UI 等待器](../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/Awaitable/Awaitable.UIComponent.cs)
- [Widget 容器](../../../Unity/Assets/Scripts/Game/Container/UIWidgetContainer.cs)
