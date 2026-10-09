# HybridCLR、CodeRunner 与程序集装载边界

## Purpose

记录当前 CarzyCooker 底座的HybridCLR、CodeRunner 与程序集装载边界。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: Code 与 Loader

ET SHALL 将稳定入口放 ET/Loader，业务分为 Model/ModelView/Hotfix/HotfixView，按当前宏和 CodeMode 装载。

#### Scenario: Code 与 Loader的适用行为

- **WHEN** 热更或 Editor 模式启动
- **THEN** 沿对应 Init/CodeLoader 路径，不修改 Unity 生成工程来规避程序集约束

### Requirement: AOT 元数据

HybridCLRHelper MUST 从配置资源读取 AotAssemblies 并调用 LoadMetadataForAOTAssembly，再卸载配置资源；构建准备遵守现有手册。

#### Scenario: AOT 元数据的适用行为

- **WHEN** 执行 AOT 元数据装载
- **THEN** 使用资源配置字节和 Consistent 模式；静态文档检查不证明 Player/AOT 成功

## Sources

- [CodeLoader.cs](../../../Unity/Assets/Scripts/Game/ET/Loader/CodeLoader.cs)
- [HybridCLRHelper.cs](../../../Unity/Assets/Scripts/Game/HybridCLR/HybridCLRHelper.cs)
- [BuildAssemblyHelper.cs](../../../Unity/Assets/Scripts/Game/Editor/Build/BuildAssemblyHelper.cs)
- [Model.asmdef](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Game.ET.Code.Model.asmdef)
- [Hotfix.asmdef](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Game.ET.Code.Hotfix.asmdef)
