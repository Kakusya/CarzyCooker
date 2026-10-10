# Spec Delta

## MODIFIED Requirements

### Requirement: ET 固定入口

ProcedurePreset SHALL 直接进入 ProcedureET；在 UNITY_ET 编译条件成立且本轮为普通／合法 legacy 路线时，由 CodeRunner.StartRun("ET.Init") 启动，离开时仅对本轮已启动入口 StopRun。显式实例配置错误、交接失效或产品 Host／Client 能力未接入时 MUST 明确失败，不调用旧 ET 示例入口。不声明已删除的 GameHot／纯 GF 示例分支仍可切换，也不把进入 ProcedureET 等同于 ET 已成功初始化。

#### Scenario: ET 固定入口的适用行为

- **WHEN** GF Procedure 进入 Preset，且本轮为普通或合法 legacy 启动
- **THEN** 启动 ET 路线，不根据旧文档建立 HotEntry

#### Scenario: ET 编译条件成立时退出

- **WHEN** 已编译 UNITY_ET 路线且 ProcedureET 离开
- **THEN** 仅在本轮确实启动 CodeRunner 时停止它；缺少 UNITY_ET 时不能从流程类存在推断 ET 启动回调已编译

#### Scenario: 显式产品角色尚无启动能力

- **WHEN** 用户显式请求 Host 或 Client，但产品启动能力尚未接入
- **THEN** 报告 ProductBootstrapUnavailable，不调用旧 ET 示例入口，不创建 Demo 服务／客户端，不报告产品 Ready

#### Scenario: 实例请求交接失败

- **WHEN** 本轮显式实例配置或跨 Play 交接失效
- **THEN** 不推进 ET 业务启动，不回落到默认参数或旧 Demo 链路；不宣称该检查能撤销此前 GF 初始化读写

## Sources

- [本项设计](../../design.md)
- [既有固定入口主规格](../../../../specs/startup/spec.md)
- [ProcedurePreset](../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedurePreset.cs)
- [ProcedureET](../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedureET.cs)
- [当前 ET.Init](../../../../../Unity/Assets/Scripts/Game/ET/Loader/Init.cs)
