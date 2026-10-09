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

## Sources

- [AExUIForm.cs](../../../Unity/Assets/Scripts/Game/UI/Common/AExUIForm.cs)
- [UIComponentSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Module/UI/UIComponentSystem.cs)
- [UGFUIForm.cs](../../../Unity/Assets/Scripts/Game/ET/Loader/UGF/UIForm/UGFUIForm.cs)
- [UGFSystemSingleton.cs](../../../Unity/Assets/Scripts/Game/ET/Loader/UGF/UGFSystemSingleton.cs)
- [UIFormLoginComponent.cs](../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Demo/UI/UILogin/UIFormLoginComponent.cs)
