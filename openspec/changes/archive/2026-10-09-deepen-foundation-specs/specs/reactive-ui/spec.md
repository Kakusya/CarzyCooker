# Spec Delta

## ADDED Requirements

### Requirement: 首次投影与合并变更

生成的 ObserveChanges SHALL 对已失效实例直接返回；首次观察读取全部 source 并调用全部 bind，后续观察只因相关 source 变化调用对应 bind。多 source bind 在同一次观察中按任一关联源变化触发一次；调用者仍需安排观察时机，attribute 本身不自动注册每帧更新。

#### Scenario: 首次观察

- **WHEN** 存活 owner 尚未初始化观察缓存
- **THEN** 缓存源值并执行全部 bind；旧值/新值签名的首次投影使用同一初始值

#### Scenario: 同轮多个关联源变化

- **WHEN** 后续一次观察发现一个多源 bind 的多个关联源变化
- **THEN** 缓存新值后调用该多源 bind 一次；无关联变化时不调用

### Requirement: 节流与观察重置

ET reactive 节流 MUST 按 ObserveChanges 的调用次数计数，而非时间间隔；首次观察不等待节流次数。ResetReactive 仅清初始化标志，使下一次观察重新投影；它不清业务字段、不销毁资源，也不自动订阅或解绑其他事件。

#### Scenario: 启用次数节流

- **WHEN** throttleCount 大于一且已经完成首次观察
- **THEN** 未达到调用计数阈值时跳过本次比较，达到后比较当前状态；不保证观察期间每次中间状态都被投递

#### Scenario: 对象重绑后的 Reset

- **WHEN** 调用 ResetReactive 后再次观察存活 owner
- **THEN** 将当前 source 作为首次投影重新绑定；原领域数据和其他订阅由其自身 owner 管理

### Requirement: 版本源与绑定签名

版本源 SHALL 通过引用身份或 IVersion.__Version 的变化触发；普通集合原地修改不自动获得版本源语义。bind 的参数可为 owner、owner 加当前值，或非版本源的 owner 加成对旧新值；版本源不提供旧新值签名，source 身份须遵守现有 nameof 诊断。

#### Scenario: 同一版本集合原地变化

- **WHEN** 集合引用相同但可观察版本已经改变
- **THEN** 关联 bind 在实际观察时触发；既未换引用也未改版本不能视为已通知

#### Scenario: 绑定版本源的旧新值签名

- **WHEN** 给版本源声明旧值/新值参数的 bind
- **THEN** 生成器按签名约束诊断，不把同一可变对象当成可靠旧值副本

## Sources

- [生成语义与签名诊断](../../../../../../Share/SourceGenerator/Generator/ETReactiveSystemGenerator/ETReactiveSystemGenerator.cs)
- [响应式声明](../../../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Reactive/ETReactiveAttributes.cs)
- [投影约定](../../../../../../references/reactive-and-network.md)
