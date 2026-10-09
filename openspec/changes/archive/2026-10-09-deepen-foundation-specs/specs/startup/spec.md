# Spec Delta

## MODIFIED Requirements

### Requirement: ET 固定入口

ProcedurePreset SHALL 直接进入 ProcedureET；在 UNITY_ET 编译条件成立时，由 CodeRunner.StartRun("ET.Init") 启动，离开时 StopRun。不声明已删除的 GameHot/纯 GF 示例分支仍可切换，也不把进入 ProcedureET 等同于 ET 已成功初始化。

#### Scenario: ET 固定入口的适用行为

- **WHEN** GF Procedure 进入 Preset
- **THEN** 启动 ET 路线，不根据旧文档建立 HotEntry

#### Scenario: ET 编译条件成立时退出

- **WHEN** 已编译 UNITY_ET 路线且 ProcedureET 离开
- **THEN** 调用现有 CodeRunner 停止入口；缺少 UNITY_ET 时不能从流程类存在推断 ET 启动回调已编译

## ADDED Requirements

### Requirement: 启动资源模式分流

启动 SHALL 按实际资源模式选择路径：EditorResourceMode 直接完成资源阶段，Package 先初始化资源，其余模式先检查版本。资源检查完成后，只有存在待更新资源且模式为 Updatable 时进入集中更新；其他情况进入资源完成阶段。

#### Scenario: 编辑器资源模式

- **WHEN** Splash 阶段检测到 EditorResourceMode
- **THEN** 进入资源完成与预加载，不要求在线版本请求完成

#### Scenario: Package 与在线模式

- **WHEN** 未启用 EditorResourceMode
- **THEN** Package 走资源初始化；其他模式走版本检查，不能把编辑器启动成功作为在线更新通过

#### Scenario: 资源检查仍未完成

- **WHEN** 资源检查回调尚未到达
- **THEN** 保持检查流程；回调完成后以更新数量及 ResourceMode 选择更新或完成路径

### Requirement: 预加载完成顺序

预加载 MUST 依次等待公共配置表、当前语言资源与 UX 初始化；UNITY_HOTFIX 且 ENABLE_IL2CPP 时再等待 AOT 元数据装载，然后进入 Preset。前置 await 失败不能报告 ET 业务已就绪。资源完成阶段的 Launcher 路径保存是独立异步任务，不保证预加载等待它结束。

#### Scenario: 正常完成预加载

- **WHEN** 适用的预加载步骤依次完成
- **THEN** 才进入 Preset；Editor 的 UI/Entity 分组核对发生在该步骤中

#### Scenario: 前置加载失败

- **WHEN** 表、语言或 UX 初始化中的 await 抛出异常
- **THEN** 后续步骤与进入 Preset 不在该调用中继续执行；现有流程没有据此提供自动恢复或回滚保证

### Requirement: 版本检查终态边界

版本检查 MUST 区分本流程的响应与其他请求；解析失败、请求失败或强制更新提示不设置版本检查完成。成功解析且无需强制更新后才设置资源地址，并按版本列表结果选择下一流程。现有失败分支不提供自动重试或离线回退保证。

#### Scenario: 无关请求返回

- **WHEN** WebRequest 回调的 UserData 不属于当前版本检查
- **THEN** 忽略该回调，不推进当前流程

#### Scenario: 检查失败或强制更新

- **WHEN** 版本解析失败、请求失败或服务要求强制更新
- **THEN** 保持未完成状态；强制更新提示提供更新与退出入口，不报告资源检查已经通过

## Sources

- [Preset](../../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedurePreset.cs)
- [ET 入口与退出](../../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedureET.cs)
- [资源模式](../../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedureSplash.cs)
- [资源检查](../../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedureCheckResources.cs)
- [预加载](../../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedurePreload.cs)
- [资源完成](../../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedureCompleteResources.cs)
- [版本检查](../../../../../../Unity/Assets/Scripts/Game/Procedure/ProcedureCheckVersion.cs)
