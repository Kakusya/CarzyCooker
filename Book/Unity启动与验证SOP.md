# Unity 启动与验证 SOP

自动测试统一入口是 [auto-testing-sop](../.agents/skills/auto-testing-sop/SKILL.md)；本页只保留启动上下文，不重复测试编排。

本 SOP 统一本工程 Unity Editor 启动上下文。客户端从 `Assets/Launcher.unity` 进入；底座实际链路是 GF Procedure → ProcedurePreset → ProcedureET → ET.Init/CodeLoader → ET.Entry.Start。底座启动不表示做饭世界已启动。

## 当前可执行启动方式

| 场景 | 固定入口 | 当前行为 | 验证方式 |
|---|---|---|---|
| 底座客户端 | Unity/Assets/Launcher.unity，工具栏 Launcher | ProcedurePreset 直接进入 ProcedureET；按 ET CodeMode 装载程序集 | Editor 状态、编译、Console；Play 按当轮授权 |
| UTF 测试 | 已打开本工程 Editor，Pipeline list_tests/run_tests | 只执行实际发现并已授权的用例 | 终态、断言、堆栈、Console；空集 NotRun |
| Excel 导表 | Game/Tool/ExcelExporter | 工具 AppType=ExcelExporter，不进入 Play | Luban check/gen 输出及 diff |

没有迁入参考的玩法 Profile、座位、投币前置、工具栏 Demo 或专用测试入口。

## 显式单实例请求

通过 `Game/Multiplayer/Instance Launch` 统一校验并提交本轮配置，再复用 Launcher。场景及实例请求只存本工程完整路径范围的 SessionState，旧全局 EditorPrefs 请求不读取、不清理、不回退。窗口草稿和已提交请求分开，普通 Launcher 清理本工程显式请求并保留旧 ET 路线。

支持默认 Play 和关闭 Domain Reload、保留 Scene Reload；显式启动拒绝关闭 Scene Reload。正常域重载在 AfterAssembliesLoaded 交接配置，BeforeSceneLoad 消费 Launcher 场景，AfterSceneLoad 清场景请求；进入 EditMode 清本轮请求并收尾。Play 中编译域重载报告 `PlayDomainReloadInterrupted` 并结束自身显式实例 Play，保留中断失败，不透明恢复或重启；普通 Launcher 不由本项自动结束。

`Prepared` 不等于 ET 或产品 Ready；产品尚未接入时显式 Host/Client 失败为 `ProductBootstrapUnavailable`。ProcedureET 只停止本轮实际启动的 CodeRunner，保留 GF 原设置保存/关闭 owner。配置/作用域小集、普通/legacy、两种 Play 模式的连续两轮及真实编译中断收尾已核对，见 [执行记录](../openspec/changes/multiplayer-instance-bootstrap/execution-notes.md)。

双 Editor 的目录、日志、终端请求与维护仅按 [共用对端规程](../Docs/AI/multiplayer-peer-workflow.md) 操作，环境可用与产品会话分别验收。

## 当前启动参数事实

- 现有 Unity Editor 已运行后无法追加操作系统启动参数；需要重启才能改变进程参数，重启仍须属于任务范围。
- `ET/Loader/Init.cs` 在 Editor 路径以空参数解析 Options，并设置 StartConfig=Localhost；不要宣称自定义 --AppType/--SceneType 已在 Editor Play 生效。
- 先核对 `UNITY_ET`、`UNITY_HOTFIX`、`UNITY_ET_VIEW`、ET CodeMode 与当前配置，不通过删 ET 或关闭 HybridCLR 简化验证。
- 找不到 Editor 才按用户指定 D:/UnityEditor 下载对应版本；当前已有 6000.3.18f1，不重复安装。

## 每次验证前检查

1. 确认当前工程为 `<repo>/Unity`。`unity pipeline list` 精确匹配 projectPath；每条命令显式加 `--project-path`。
2. 用 `unity list` 核对实际命令；读 editor_status/console_status，确认不在编译或域重载、无编译失败。
3. 人工 Play 核对 Launcher 场景，保护未保存场景；UTF 由测试框架管理 Play 生命周期，不手动同时切 Play。
4. 运行完成后检查实际状态、消息、堆栈与 Console；命令提交、进程存在、Console 无错误分别不能单独证明玩法通过。
5. 报告编译、Play、UTF、联网、AOT 等各自结果；没有执行的项目写 NotRun。不要清 Console 掩盖历史错误。

源码：[ProcedurePreset](../Unity/Assets/Scripts/Game/Procedure/ProcedurePreset.cs)、[ET Init](../Unity/Assets/Scripts/Game/ET/Loader/Init.cs)、[Launcher 工具栏](../Unity/Assets/Scripts/Game/Editor/ToolBar/LauncherSceneToolBar.cs)。CLI 详细用法见 [UnityCLI](UnityCLI.md)。
