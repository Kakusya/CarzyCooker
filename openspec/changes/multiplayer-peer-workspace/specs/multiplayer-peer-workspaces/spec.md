# Spec Delta

## Purpose

为本机双 Editor 联调提供可保留稳定源码版本的完整对端工作区，规定基线维护、缓存和应用数据隔离、进程日志及目标定位的可观察行为。该能力只证明环境可用，不能替代产品启动、会话或玩法验收。

## ADDED Requirements

### Requirement: 稳定对端工作区与明确基线

稳定对端 MUST 使用同仓完整 worktree 与独立固定分支，覆盖 Unity 及其根目录源码、配置和工具依赖；记录选定版本，只在明确维护请求下更新。主线未提交修改不得被视为已包含于基线。

#### Scenario: 准备稳定对端

- **WHEN** 用户明确授权建立稳定工作区并指定或确认基线版本
- **THEN** 工作区具有自己的源码和 Unity 工程路径，能够指出分支与选定版本，并列明必要依赖准备情况

#### Scenario: 主线继续开发

- **WHEN** 主线新增提交或存在未提交代码，而未请求更新稳定端
- **THEN** 稳定端保持原基线，不自动拉取、合并或复制主线热更程序集及生成物

### Requirement: 工作区缓存独立

两个 Editor MUST 使用各自工程下的 Library、Temp、Logs 与 UserSettings，不共享或链接活动 Library；缺少依赖或导入尚未结束时报告未就绪，不能把目录存在当作环境通过。

#### Scenario: 首次打开稳定工程

- **WHEN** 稳定端需要解析包、导入资源或编译
- **THEN** 使用稳定工程自己的缓存，完成状态按实际 Editor 核对，不复制主线活动缓存来宣称可用

### Requirement: 应用数据身份与实际消费者隔离

稳定端 MUST 在运行前具有与主线不同的固定 ProductName，并核对实际 settings、AssetSet 和 Resource 写路径；Host／Client 切换不得改变该工作区产品身份。隔离结论仅覆盖实际核对的消费者和平台，不自动扩展到自定义写文件。

#### Scenario: 两端保存独立设置

- **WHEN** 两个工程用各自产品身份保存不同设置值并重新读取
- **THEN** 实际设置路径互异，各自读取自己的值，主线用户数据不被复制、清空或迁移

#### Scenario: 核对资源文件系统

- **WHEN** 验收 AssetSet 的初始与扩展文件路径及 Resource 当前读写根
- **THEN** 两端实际路径分离；未触发的扩展行为或未核对消费者明确列为未验证，不以产品名不同直接宣称通过

#### Scenario: 稳定端切换运行角色

- **WHEN** 稳定端从 Client 改为 Host
- **THEN** 保持其固定产品名和自身数据，角色选择不与主线／稳定分支职责绑定

### Requirement: 进程日志独立

第二 Editor MUST 在进程启动时指定独立原生日志文件，并保证父目录存在；配置独立包管理日志时也不得与主线共用文件。产品名差异不得被用作 Editor 原生日志隔离证据。

#### Scenario: 启动第二 Editor

- **WHEN** 主线 Editor 已有运行日志而稳定端准备启动
- **THEN** 稳定端使用另一日志文件，不覆盖主线日志或强制重启主线；同一 Editor 多轮 Play 仍可保留在自身进程日志中

### Requirement: Editor 目标精确定位

工作区操作 MUST 通过实际实例发现按完整 Unity projectPath 精确选择目标，命令显式携带目标路径；不固定 Pipeline 端口，不将工具端口当作产品网络端口。目标缺失、多义或忙碌时拒绝推进。

#### Scenario: 两个 Editor 同时可发现

- **WHEN** 向稳定端请求状态、编译或运行
- **THEN** 命令作用于稳定端完整工程路径，不因为实例排序或窗口标题操作主线

#### Scenario: 目标尚未就绪

- **WHEN** 目标仍导入、编译、测试或已有未授权停止的 Play
- **THEN** 报告忙碌或未就绪，不自动 cancel、stop、清 Console 或改用另一实例

### Requirement: 维护保留差异与验收范围

稳定端更新 MUST 保留其产品身份及协作职责，核对所需配置、协议、包和工具的一致可用版本，并重新验证环境；结束联调只释放本轮拥有的资源。环境、终端通信、角色启动与产品会话 MUST 分别报告，缺环境记 NotRun。

#### Scenario: 明确升级稳定基线

- **WHEN** 用户因兼容性或必要能力批准更新选定版本
- **THEN** 更新后仍保持稳定端产品名和角色规则，核对编译与适用验证结果，不将分支差异自动合回主线

#### Scenario: 首项启动能力尚不可用

- **WHEN** 第二端环境与消息已验证，但启动入口或产品会话未实施
- **THEN** 仅报告环境和消息结果；角色启动或产品网络列为 NotRun 或其实际拒绝结果，不宣称 Host／Client 已连通

#### Scenario: 一轮协作结束

- **WHEN** 本轮运行完成或失败
- **THEN** 只清理自身运行资源，保留长期工作区及用户现场，删除长期目录须另有明确授权

## Sources

- [当前产品身份](../../../../../Unity/ProjectSettings/ProjectSettings.asset)、[缓存忽略配置](../../../../../.gitignore)
- [设置路径](../../../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Setting/DefaultSettingHelper.cs)
- [AssetSet 文件路径](../../../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/AssetSet/AssetSetComponent.FileSystem.cs)、[Resource 路径](../../../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Resource/ResourceComponent.cs)
- [Unity CLI 定位](../../../../../Book/UnityCLI.md)、[端口 SOP](../../../../../references/port-management.md)
- [启动项边界](../../../multiplayer-instance-bootstrap/proposal.md)
