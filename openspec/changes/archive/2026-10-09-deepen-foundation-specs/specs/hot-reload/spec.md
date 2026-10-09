# Spec Delta

## ADDED Requirements

### Requirement: 程序集装载模式

客户端代码装载 SHALL 只有在 EnableHotfix 与 EnableCodeBytesMode 同时成立时走资源字节模式；Editor 且 UseUnityEditorModelDll 时复用已加载的 Model/ModelView，否则读取其 dll 与 pdb 字节。其余模式查找已有四个程序集；热更字节缺失不能自动回退为其他模式。

#### Scenario: Editor 保留 Model 视图绑定

- **WHEN** 使用 CodeBytes 且 Editor 的 UseUnityEditorModelDll 为 true
- **THEN** Model/ModelView 取已有程序集，Hotfix/HotfixView 仍走热更资源装载

#### Scenario: 完整字节装载

- **WHEN** 使用 CodeBytes 且不复用 Editor Model DLL
- **THEN** 取得 Model、ModelView、Hotfix、HotfixView 的 dll/pdb 字节后注册类型；源配置缺失或读取失败不报告启动已完成

### Requirement: Reload 与元数据的验证范围

Reload MUST 保留已有 Model/ModelView，仅装载新 Hotfix/HotfixView 并重建类型注册；EnableHotfix 未成立时拒绝热更装载。AOT helper 逐项调用元数据加载接口，但当前不检查其返回码，也没有失败回滚保证；结束日志不能证明每个 AOT 元数据成功或 Player 热更兼容。

#### Scenario: 请求热更 Reload

- **WHEN** 已具备有效 Model/ModelView 且热更开启，调用 Reload
- **THEN** 更新热更程序集与类型注册；本入口不重启 ET.Entry，也不自动迁移现存业务数据

#### Scenario: AOT 返回失败码

- **WHEN** 元数据加载接口返回非成功码而没有抛出异常
- **THEN** 当前 helper 仍可能继续并输出结束日志；报告不得据此声称 AOT 已通过，限制见已知问题

## Sources

- [装载及 Reload](../../../../../../Unity/Assets/Scripts/Game/ET/Loader/CodeLoader.cs)
- [AOT helper](../../../../../../Unity/Assets/Scripts/Game/HybridCLR/HybridCLRHelper.cs)
- [程序集构建与字节复制](../../../../../../Unity/Assets/Scripts/Game/Editor/Build/BuildAssemblyHelper.cs)
- [已知问题](../../../../../../KnownIssues.md)
