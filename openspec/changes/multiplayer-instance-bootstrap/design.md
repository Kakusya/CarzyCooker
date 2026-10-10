# Design

## Context

范围见 [proposal.md](proposal.md)。本项回答“一个实例如何按指定角色启动”，第二 worktree 的环境和代理协作已经移出。

当前事实：

- SceneHelper 的 StartScene、BeforeSceneLoad 和 AfterSceneLoad 共用全局 EditorPrefs 键 `UnityEditorSceneToOpen`，不同工程可能互相消费请求。
- ProcedurePreset 直接进入 ProcedureET，后者启动 ET.Init；当前 ET.Init／Entry 尚未消费产品 Host／Client 配置。
- Game 稳定层与 Game.Editor 已有适用程序集，Loader 引用稳定层；不需要新业务程序集或 GF 反向引用。
- 当前默认 Play 保留 Domain Reload 与 Scene Reload；运行时上下文须处理进入 Play 和连续运行的生命周期。

配置和场景请求的独立验收可以在当前 Editor 完成。已有跨 worktree 通信探针不计入本项启动验收；双 Editor、应用数据隔离和产品会话均未验证。

## Goals / Non-Goals

**Goals:**

- 一份不可变本轮配置，明确输入、拒绝条件和状态范围。
- 一个本工程 Editor 入口，正确交接到现有 Launcher／ET 边界，并保留普通启动。
- 本轮请求及入口 owner 的正确收尾，不污染下一轮或另一工程。

**Non-Goals:**

- 不建立第二 worktree、稳定分支或代理会话，不修改 AGENTS 和 ProductName。
- 不管理应用数据路径、GF 文件消费者、目录锁、原生进程日志、端口租约或跨会话消息。
- 不建立产品 SceneType、网络监听、Session、协议、Ready 或玩法；Player argv 和 Android 也不在首期范围。

## Decisions

### 1. 最小运行配置，不携带工作目录职责

建议在既有 Game 稳定层 `Base/RuntimeInstance/` 保存值对象与本轮上下文。

| 字段 | 启动语义 |
| --- | --- |
| runId | 非空运行关联标识，不等于产品 MatchId |
| instanceId | 非空实例关联标识，不等于 ET Entity.InstanceId 或账号 |
| role | legacy、host、client，显式请求必填 |
| listen／connect | Host 仅接受 loopback 监听意图，省略时记录端口 0；Client 必须有非零 loopback 连接目标；legacy 无 endpoint |

标识接受字母、数字、点、下划线与短横线，拒绝空值、单独点及控制字符。校验不打开网络，端口 0 只是一份意图，不能作为 Client 连接目标。

没有 workspaceRole、expectedProductName、基线版本或 dataRoot 输入。Host／Client 可在任一工程选择，不与开发／稳定分支绑定。当前工程路径仅用于 Editor 请求作用域，不成为角色选择或产品身份来源。

生效值对象从已提交请求复制；运行中改窗口草稿不影响当前值。ET AppType 和 CodeMode 不用来模拟本轮角色。Player 参数解析留待具体 Player 使用需求。

### 2. 显式 Editor 入口与普通 Launcher

建议 Game.Editor 提供 `Game/Multiplayer/Instance Launch`，验证通过后复用 SceneHelper.StartScene 进入原 Launcher，不文本覆写场景。

显式入口在进入 Play 前检查配置及本工程状态。正在 Play、编译、导入或测试时拒绝，不自动停止、取消或清 Console。未知角色、非法 endpoint 等无效请求不能被解释为普通启动。

普通 Launcher 未提交请求时保留现有行为，不能消费联调窗口草稿或上一轮角色。这是当前草案采用的兼容默认。接口直接使用同一配置校验，不能在窗口与稳定层维护两套不同语义。

### 3. 既有场景请求按工程管理

在现有 `LauncherSceneToolBar.cs` 内修改 SceneHelper，让提交、BeforeSceneLoad 消费和 AfterSceneLoad 清理使用同一工程作用域。建议临时场景请求及角色草稿／提交请求存 Editor SessionState，键包含规范化 Unity 工程完整路径。

不继续读取、删除或回退到旧全局 EditorPrefs 键，也不为清理旧键影响另一旧版本 Editor。普通 Launcher 的场景路径和操作入口保持原样。

单 Editor 验收使用真实请求存储，验证两份工程作用域的请求不会互相消费和清理；静态核对所有真实调用点。真实双 Editor 的交错启动属于后续工作目录验收，本项不得冒充已经完成。

SessionState 跨程序集重载保留，Editor 退出清空；首期不加入跨 Editor 进程的持久预设或配置文件。

### 4. 跨 Play 交接与本轮状态

建议稳定层用 SubsystemRegistration 清理上一轮状态，并在 AfterAssembliesLoaded 取得已提交请求，早于 SceneHelper 的 BeforeSceneLoad 场景消费。只建立本轮上下文，不接管 GF 文件初始化或写根。

首期支持默认 Play，以及关闭 Domain Reload、保留 Scene Reload 的连续两轮。关闭 Scene Reload 的显式入口在进入 Play 前拒绝，不修改用户选项。Play 中编译域重载将本轮标为中断，不透明恢复、自动启动新轮或回落到 Demo。

建议状态为 NotRequested、Preparing、Prepared、Failed、Stopping、Stopped。Prepared 仅表示配置已交接，不证明 GF 预加载、ET 初始化、实际端口或产品会话。后续失败必须保留为最终失败。

### 5. 产品不可用边界与入口 owner

ProcedurePreset 仍进入 ProcedureET。普通／合法 legacy 继续既有 CodeRunner.StartRun("ET.Init")。本项尚无产品 Host／Client 能力，显式请求在调用旧入口之前报告 `ProductBootstrapUnavailable` 并停止推进，不启动 Demo，也不注册假的产品 bootstrap。

独立的 [产品会话设计](../product-session-bootstrap/design.md) 在该边界消费同一上下文，创建产品 ET SceneType 与会话；第一项不提前重构传输、Fiber 或协议。ET owner 树和本地消息的发送／接收归该设计明确列出，本项只持有启动配置和入口状态，不创建第二份成员状态或消息等待者。该能力实际接入前，ProductBootstrapUnavailable 仍是正确结果。

运行时交接失效只保证 ET 入口拒绝，不宣称能撤销此前 GF 初始化读写。退出仅对本轮确实启动的 CodeRunner 执行 StopRun，清理自己的请求与上下文；不改 GF 原有设置保存与文件系统 Shutdown。

### 6. 候选文件与验收

| 路径 | 本项变化与验收 |
| --- | --- |
| `Unity/Assets/Scripts/Game/Base/RuntimeInstance/`（建议新） | 不可变配置、校验、上下文与状态；真实输入／错误输入和后续失败终态 |
| `Unity/Assets/Scripts/Game/Editor/Multiplayer/`（建议新） | Editor 启动入口；忙碌拒绝、草稿与提交分离 |
| `Unity/Assets/Scripts/Game/Editor/ToolBar/LauncherSceneToolBar.cs` | 工程范围请求；提交、消费、清理和旧键无回退 |
| `Unity/Assets/Scripts/Game/Procedure/ProcedureET.cs` | 产品不可用拒绝与本轮入口 owner；普通／legacy 回归 |
| 既有 UTF 程序集及必要测试夹具 | 配置、真实请求存储与生命周期；无程序集时仅新增 UTF asmdef |
| `Book/快速开始.md`、`Book/Unity启动与验证SOP.md`、`Docs/AI/module-index.md` | 同步已实现启动入口、支持边界和定位；不引入第二端协作正文 |

不创建新的自动化框架、协作工具、业务程序集或角色配置表。本项不改表，不处理 AGENTS、ProductName、进程拉起、日志部署或基线更新。

### 7. 第二个 change 的交接边界

已建立的 [multiplayer-peer-workspace](../multiplayer-peer-workspace/proposal.md) 负责完整稳定 worktree、固定分支更新、独立 Library、ProductName／真实写路径、Editor 原生日志、精确 Pipeline 定位、AGENTS 职责及现有 Orca 消息协作。本项只保留交接边界，不重复其实现任务或规格要求。

第二端可以先独立验证目录身份、会话通信和协作规则；使用本项角色入口时才依赖本项能力。两项结合后的双 Editor 运行、数据和联调验收不能由任何一项的单独结果替代。

`product-session-bootstrap` 仍独立拥有产品启动／会话能力，真实产品联机需要启动入口、可用第二端及产品会话三个部分。后续 change 仍逐个讨论，不在本项固定全部实施顺序。

## Risks / Trade-offs

- [请求作用域只改一处，其他消费者仍用旧键] → 核对 StartScene、BeforeSceneLoad、AfterSceneLoad 的完整消费链并验证清理。
- [连续 Play 遗留上下文] → reset 与交接按本轮处理，验证正常和关闭 Domain Reload 的两轮运行。
- [配置准备完成被误当成产品成功] → 明确 Prepared 范围，Host／Client 缺能力最终失败。
- [启动入口通过但第二端文件仍争用] → 应用数据和多进程日志归下一工作目录 change，本项不声称隔离通过。

## Migration Plan

1. 审核本项缩减草案，再按明确 apply 请求在当前开发工作目录实现配置和 Editor 请求作用域。
2. 验证单 Editor 的交接、连续 Play、普通 Launcher 和显式角色拒绝，按真实环境核对编译／UTF；缺环境记 NotRun。
3. 同步本项用法与定位，通过静态和 OpenSpec 检查；未完成实测不勾选其任务。
4. 第二端环境按已建立的独立 change 审核和实施，再讨论产品会话；本项不自动创建工作目录、修改 AGENTS 或推进稳定分支。
5. 回退时结束自身本轮运行并取消显式请求，使用普通 Launcher；不删除应用数据或改动其他工程。

## Sources

- [Launcher 与 SceneHelper](../../../Unity/Assets/Scripts/Game/Editor/ToolBar/LauncherSceneToolBar.cs)、[GameEntry](../../../Unity/Assets/Scripts/Game/Base/GameEntry.cs)
- [ProcedurePreset](../../../Unity/Assets/Scripts/Game/Procedure/ProcedurePreset.cs)、[ProcedureET](../../../Unity/Assets/Scripts/Game/Procedure/ProcedureET.cs)、[ET.Init](../../../Unity/Assets/Scripts/Game/ET/Loader/Init.cs)
- [Game 程序集](../../../Unity/Assets/Scripts/Game/Game.asmdef)、[Loader 程序集](../../../Unity/Assets/Scripts/Game/ET/Loader/Game.ET.Loader.asmdef)、[Play 配置](../../../Unity/ProjectSettings/EditorSettings.asset)
- [当前启动主规格](../../specs/startup/spec.md)、[Unity CLI](../../../Book/UnityCLI.md)
- [SessionState 生命周期](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SessionState.html)
