# ET 启动与稳定加载层

## Purpose

记录 GF 壳、ET Init/CodeLoader 与实际程序集装载入口。 当前基线为 main 分支的 ET 底座；静态格式/源码核对不代表产品构建、Editor、玩法或联网已验证。

## Requirements

### Requirement: ET 固定入口

ProcedurePreset SHALL 直接进入 ProcedureET，由 CodeRunner.StartRun("ET.Init") 启动，离开时 StopRun；不声明已删除的 GameHot/纯 GF 示例分支仍可切换。

#### Scenario: ET 固定入口的适用行为

- **WHEN** GF Procedure 进入 Preset
- **THEN** 启动 ET 路线，不根据旧文档建立 HotEntry

### Requirement: ET 程序集装载

CodeLoader MUST 按热更与 CodeBytes 配置取得 Model/ModelView/Hotfix/HotfixView 程序集并注册 CodeTypes，再调用 ET.Entry.Start。

#### Scenario: ET 程序集装载的适用行为

- **WHEN** ET.Init 完成基础单例初始化
- **THEN** 通过已有 CodeLoader 启动；Editor Model DLL 与资源字节分支按真实配置处理

## Sources

- [ProcedurePreset](../../../Unity/Assets/Scripts/Game/Procedure/ProcedurePreset.cs)
- [ProcedureET](../../../Unity/Assets/Scripts/Game/Procedure/ProcedureET.cs)
- [ET.Init](../../../Unity/Assets/Scripts/Game/ET/Loader/Init.cs)
- [CodeLoader](../../../Unity/Assets/Scripts/Game/ET/Loader/CodeLoader.cs)
