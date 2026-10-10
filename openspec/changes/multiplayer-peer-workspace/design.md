# Design

## Context

动机和范围见 [proposal.md](proposal.md)，契约见 [工作区规格](specs/multiplayer-peer-workspaces/spec.md) 与 [协作规格](specs/ai-collaboration/spec.md)。

当前事实：

- Unity 公司名为 GDK，产品名为 GameDevelopmentKit。DefaultSettingHelper 在 Awake 固定设置文件路径，AssetSet 在初始化及扩展时使用 persistentDataPath，Resource 则按当前配置选择 persistentDataPath 或 temporaryCachePath。
- 完整工程不仅包含 Unity：DotNet 链接 Unity 的 ET 源码，Share 提供 Analyzer、SourceGenerator、Excel／Proto 工具，Design 和 Config 包含生成源及运行配置。只抽出 Unity 为子仓会增加版本组合和工具路径问题。
- 当前 .gitignore 已排除 Unity Library、Temp、Logs 和 UserSettings 等缓存，不需要把主线活动缓存复制给第二端。
- 根 AGENTS 当前声明 main／origin/main，尚无稳定对端职责片段。Unity CLI 已要求按完整 projectPath 选择工程；项目没有 PortRegistry 或专用 worker 编排器。
- 此前临时 Orca worktree 已验证一次主线发送、对端实际检查并回复、主线收到回复，然后已删除临时目录和分支。没有启动第二 Unity，没有验证数据或产品网络。

该通信探针证明当前活跃终端可双向通信。发送入队本身不会证明对端已读；当时通过终端输入明确触发收件检查。普通终端消息不保证终端关闭后持久存在，也不是有后台 Run 的 worker。

## Goals / Non-Goals

**Goals:** 复用现有 Git／Orca 与 Unity CLI，形成能保持稳定源码版本的第二端环境和可审核协作规程；分开报告环境、消息、启动与产品会话。

**Non-Goals:** 不实现第一项的启动配置、不实现产品网络、不新增自动启动脚本或常驻代理，不建设持久任务队列；不以本规划授权建立真实长期 worktree。

## Decisions

### 1. 完整同仓 worktree，稳定分支按需更新

采用完整同仓 worktree，而非 Unity 子仓或频繁打包。建议固定分支名 `multiplayer-baseline`，名称与绝对目录在实际建立时确定；固定分支名不意味着内容自动冻结，稳定性来自明确选定版本和禁止自动更新。

基线选用已存在且明确指定的提交／版本标记，直接记录可读版本身份，不进行 hash 校验或源码／产物比对。主线未提交改动不会自然进入 worktree；需要的启动或产品能力应通过明确维护流程纳入，不能复制一份 DLL 就宣称版本匹配。

第二端保持自己的 Library、Temp、Logs、UserSettings 和依赖导入结果。首次完整导入的时间与磁盘成本是该方案的实际代价；不通过共享、junction 或复制正在使用的 Library 降低成本。现有子模块按实际依赖核对，不额外把 Unity 改为子模块。

### 2. 固定产品身份隔离数据，进程日志另配

主线保留 `GDK / GameDevelopmentKit`；稳定端候选为 `GDK / GameDevelopmentKit-NetTest`。产品名在稳定分支 ProjectSettings 中固定，不随每轮或 Host／Client 切换，也不进入第一项运行配置。固定身份允许稳定端保留自己的设置和缓存。

ProductName 改动必须在该工程运行前生效，不在组件 Awake 后临时切换。针对 Windows 双 Editor 核对实际路径，而不是只依据默认目录规则作结论：

| 消费者 | 核对方式与范围 |
| --- | --- |
| DefaultSettingHelper | 实际 FilePath 与保存／重读不同设置值；保留两端原数据 |
| AssetSet | 初始与扩展文件的实际路径；未触发扩展行为明确标未验证 |
| Resource | 当前 ReadWritePathType、实际读写根，包含 temporaryCachePath 分支的适用性 |
| 自定义持久写入 | 只有发现直接消费者并实际核对后才能扩大隔离结论 |

不用目录锁或新增写根重定向；这个阶段只支持两个不同产品身份的工程，不声称支持同时运行多个同产品身份稳定端。

Editor 原生日志由进程启动参数控制，产品名不会自动隔离它。第二端启动前创建专属日志父目录，指定独立 `-logFile`；如使用 `-upmLogFile` 也分开。可使用稳定端 Unity/Logs 下的文件，保留原始位置而不提交。一个 Editor 的多个 Play 轮次可以共用该进程日志，runId 用于关联轮次，不要求每轮重启 Editor。主线已有独立日志时直接保留，不为统一命名打断现场。

### 3. 两端 AGENTS 共同主体，职责片段分开

建议新增共用正文 `Docs/AI/multiplayer-peer-workflow.md`，在主线与稳定分支沿用相同版本；两端根 AGENTS 只增加短职责片段和入口，并使分支描述符合所在工作区。该新文件是实施时拟创建的协作正文，本轮不存在、不作为现有能力引用。

| 角色 | 默认职责 |
| --- | --- |
| 主线开发端 | 需求、OpenSpec、代码修复、决定基线更新及汇总验证 |
| 稳定对端 | 检查本端环境，在已有授权内运行、诊断与回报；业务源码默认只读 |

两者都保留技能加载、生成物只读、授权、用户现场和证据纪律，以及主线 AGENTS 的 ET 树／本地消息设计重点。稳定端回报须指出本轮实际根场景、运行角色、发送和接收路径及对应结果，不自行改树或新增通信架构。稳定端不自动 pull、不自行修业务代码、不开新 change、不归档；明确维护请求可授权必要更新。职责不等于运行角色，主线和稳定端都能作为 Host 或 Client。

维护基线时保留产品名及 AGENTS 职责差异，不把稳定端补丁自动合回主线。共用正文随必要维护同步，不复制两份互不相关的大型规程。

### 4. 现有活跃终端消息，显式收件和回报

复用现有 Orca 的 worktree／terminal 发现和终端通信，不新增包装 CLI、调度脚本、服务、技能或持久任务文件。操作前加载当前 Orca 指南，动态发现 terminal handle，不能固化旧会话 ID。

通信使用现有 `orchestration send`／收件检查／原线程回复；对端没有自行处理消息时，以当前终端输入或人工提示明确请求其检查。不要重复投递执行请求来代替唤醒，也不能把终端输入已接受当作产品启动成功。

请求至少包含 runId、目标 Unity 工程完整路径、Host／Client 角色、适用 listen／connect、动作、观察及完成条件。对端核对实际 cwd／projectPath、授权与忙碌状态，然后确认接受或拒绝、回报开始及最终结果。失败给真实原因、项目栈帧和原日志路径；不产生 JSON 证据包或状态副本。

在当前活跃线程中记住已处理请求和明确结束／取消的 runId，重复或过期请求不重复启动。超时先询问在途状态，终端重建先重新发现目标和核对 Editor；跨重启持久去重、自动唤醒和断点续跑不在范围内。普通 terminal 无绑定后台 Run，不伪造 worker_done／heartbeat 等生命周期信号。

已完成临时探针的范围记录：请求标记 `CC-WORKTREE-PROBE-20261010-A`；发送消息 `msg_60c2fad04388`，对端原线程回复 `msg_4d2114a5b199`，主线实际收到。这里只记录一次探针结果，不将临时路径和 handle 作为正式配置。

### 5. 双 Editor 精确定位与能力依赖

通过 `unity pipeline list` 发现实例，每条 Editor 命令显式传本次目标的完整 `--project-path`。目标不匹配、不可发现或忙碌时报告拒绝，不依靠实例排序，不停止另一个 Editor 的 Play／测试，也不清 Console。

Pipeline 的工具连接端口动态发现，ET 的业务 listen／connect 由启动项及后续会话处理；两者不得互相代替，不新增 PortRegistry，不预留固定工具端口。

环境与消息可先独立建立。两端使用启动窗口时，所选稳定基线必须已包含 [第一项](../multiplayer-instance-bootstrap/proposal.md) 的配置入口和工程范围请求修正。产品会话尚未实现时，显式 Host／Client 的 `ProductBootstrapUnavailable` 是当前应有拒绝，不能使用旧 Demo 凑成产品联机通过。

### 6. 升级、失败与收尾

基线升级只因必要能力、协议／配置兼容或明确维护请求发生。先结束获准结束的本端运行，确认目标版本与依赖，保留稳定分支身份差异，再核对包解析、编译及当轮获准验证。协议不兼容应直接报告，不自动把稳定端更新到最新主线。

不把主线当前 DLL、生成资产或用户设置单独复制到旧基线，不自动提交、导表、合并或推送。若更新失败，报告已发生的变化；经授权恢复此前选定版本时保留未提交现场，不能以 reset 清空处理。

联调结束释放本轮实际创建并获准结束的 Session、监听或 Editor／终端；不关闭别人的运行。长期工作区默认保留，删除需要明确指示。

### 7. 候选文件与实施范围

| 位置 | 变化 |
| --- | --- |
| 主线 AGENTS.md | 开发端职责片段和共用规程链接，保留原主体 |
| 稳定分支 AGENTS.md | 稳定端职责和真实分支说明，保留相同纪律 |
| 稳定分支 Unity/ProjectSettings/ProjectSettings.asset | 固定不同 ProductName；通过适用安全 Editor 操作保存 |
| Docs/AI/multiplayer-peer-workflow.md（拟新增） | 环境准备、日志、消息、更新、收尾与验收的唯一正文 |
| Book/UnityCLI.md、Book/Unity启动与验证SOP.md、Docs/AI/module-index.md | 精确定位、多 Editor 日志和共用正文入口 |

不增加业务程序集、网络协议、Excel 表或工具实现；实际环境准备采用已有 CLI。新脚本或常驻编排需求若出现，另案说明，不借本项引入。

## Risks / Trade-offs

- [固定分支跟着主线自动前进] → 禁止自动更新，明确选定版本与升级原因。
- [产品名不同但 Resource／自定义路径仍共用] → 核对真实消费者和平台，未验证不扩大结论。
- [消息入队被当作对端完成] → 需要收件、开始与最终回报，超时先查在途状态。
- [更新覆盖稳定端职责和产品名] → 差异作为维护流程检查项，更新后重新验证。
- [独立 Library 的初始化成本] → 首次准备完整导入，日常保留稳定目录，不共享活动缓存。

## Migration Plan

1. 规划审核后，明确实施范围及真实长期 worktree 建立授权；本轮仅生成产物。
2. 编写共用规程与主线职责片段；在获准稳定端建立／维护中保存其产品名和职责差异。
3. 独立准备依赖、缓存和日志，用精确 projectPath 核对环境及数据消费者；缺环境记 NotRun，不勾选对应实测任务。
4. 验证当前活跃终端双向请求与回报；接入第一项后再验证双 Editor 交错启动及角色切换。
5. 按真实范围报告结果，同步对应 delta 和说明；本项不自动实施产品会话，也不删除长期工作区。
6. 回退本项协作使用时结束自身获准运行，撤下相关职责入口即可；保留应用数据和用户修改，目录删除另有授权。

## Sources

- [当前身份](../../../Unity/ProjectSettings/ProjectSettings.asset)、[忽略配置](../../../.gitignore)、[DotNet 源码依赖](../../../DotNet/Model/DotNet.Model.csproj)
- [设置 helper](../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Setting/DefaultSettingHelper.cs)、[AssetSet](../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework.Extension/Runtime/AssetSet/AssetSetComponent.FileSystem.cs)、[Resource](../../../Unity/Assets/Scripts/Library/UGF/UnityGameFramework/Runtime/Resource/ResourceComponent.cs)
- [AGENTS](../../../AGENTS.md)、[Unity CLI](../../../Book/UnityCLI.md)、[端口 SOP](../../../references/port-management.md)
- [Unity 数据目录](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html)、[Editor 日志参数](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)
