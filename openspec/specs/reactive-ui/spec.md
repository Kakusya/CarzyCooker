# ET 响应式生成机制与外部包的区分

## Purpose

记录当前 CarzyCooker 底座的ET 响应式生成机制与外部包的区分。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 生成 owner

ET reactive MUST 使用 IETReactive owner、partial 声明、ETReactiveSystem 与 Source/Bind attribute；具体签名和 nameof 由 SourceGenerator 诊断约束。

#### Scenario: 生成 owner的适用行为

- **WHEN** 编写 ET 响应式绑定
- **THEN** 使用仓库生成器的 ObserveChanges/ResetReactive 契约，不混用外部包 attribute

### Requirement: 单一投影

新增 UI 表现 SHALL 从领域状态或明确 UI context 建立最小投影，Bind 只更新表现；外部 ReactiveBinding 版本集合与 ET 观察缓存各遵循自己的 API。

#### Scenario: 单一投影的适用行为

- **WHEN** 同一个控件需要刷新
- **THEN** 由一个更新 owner 处理，不叠 event/Refresh/cache 形成多条可写路径

## Sources

- [IETReactive.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Reactive/IETReactive.cs)
- [ETReactiveAttributes.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Reactive/ETReactiveAttributes.cs)
- [ETReactiveSystemGenerator.cs](../../../Share/SourceGenerator/Generator/ETReactiveSystemGenerator/ETReactiveSystemGenerator.cs)
- [EntityMethodDeclarationAnalyzer.cs](../../../Share/Analyzer/Analyzer/EntityMethodDeclarationAnalyzer.cs)
- [reactive-and-network.md](../../../references/reactive-and-network.md)
