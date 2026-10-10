# 联机稳定对端协作规程

本轮开发分支为 `feat/multiplayer-bootstrap`，开发工程 `D:/MyWork/CarzyCooker/Unity`。用户已授权建立长期第二目录 `D:/MyWork/CarzyCooker-NetTest`、固定分支 `multiplayer-baseline`，以及纳入四项必要本地版本与双 Editor 联调。实际建立、依赖导入和联调结果见 [执行记录](../../openspec/changes/multiplayer-peer-workspace/execution-notes.md)，未完成不当作环境可用。

## 目录与职责

稳定端使用同仓完整 worktree，包含 Unity、Share、Design、Config、DotNet 和 Tools；不拆 Unity 子仓，不复制开发端 DLL/生成物到旧基线。两端各用自己的 Library、Temp、Logs、UserSettings；不共享、链接或复制活动 Library。版本记录使用可读提交身份，不做 hash 校验。

开发端负责需求、实现、自审、维护版本决策与汇总；稳定端默认只运行、诊断和回报，业务源码只读，不自动 pull、修代码、建 change、归档。两端均保留根 AGENTS 的共同纪律和 ET 树/本地消息约定，都可选择 Host 或 Client。职责与运行角色无关。

稳定端 AGENTS 使用以下短片段，主体规范随获准版本维护：

> 当前工作分支为 `multiplayer-baseline`，工程为 `D:/MyWork/CarzyCooker-NetTest/Unity`。本端是联调稳定对端，默认只运行、观察与回报，业务源码只读；改代码或更新版本须有明确维护授权。运行角色可选择 Host 或 Client，不由分支决定。共同纪律与 ET 树/本地消息约定保留；协作正文见 `Docs/AI/multiplayer-peer-workflow.md`。本轮用户已授权必要版本纳入和联调，仍禁止 push、发布、PR、合回 main、自动归档或删除长期目录。

## 运行前核对

开发端保持 `GDK / GameDevelopmentKit`，稳定端在运行前固定保存 `GDK / GameDevelopmentKit-NetTest`；不在组件 Awake 后临时切产品名。分别核对 DefaultSettingHelper.FilePath、AssetSet 初始及扩展文件路径、Resource 的 ReadWritePathType 和实际根目录。保存不同临时设置值并重读，随后只移除自己的临时键，保留原数据。未触发的扩展和自定义消费者列为未验证。

稳定端启动前准备 `Unity/Logs/`，指定独立 `-logFile`；如用 `-upmLogFile`，也指定独立文件。保留开发端已使用的原生日志，不为统一命名重启它。日志和缓存不提交。

用 `unity pipeline list` 按完整 projectPath 发现实例，所有命令显式传本端 `--project-path`。核对 Editor ready、编译终态、Console groundTruth、Play stopped 和 UTF 无在途任务；忙碌拒绝，不 cancel、stop 他人操作或清 Console。Pipeline 工具端口动态发现，不能当产品 endpoint；Host 的实际 loopback endpoint 从产品监听结果取得，Client 显式提交非零目标，不扫描端口。

## 当前活跃终端消息

先加载现有 `orca-cli`，动态发现 worktree 和 terminal handle；使用现有 orchestration send/check/reply，不创建调度脚本、常驻服务、持久任务文件或假 worker 生命周期。

请求必须包含 runId、目标 Unity 完整路径、Host/Client 角色、listen/connect、动作、观察范围与完成条件。接收方核对 cwd/projectPath、授权和忙碌状态，回报接受或拒绝；接受后分别回报开始与最终结果，原线程附失败原因、项目栈帧和原日志位置。

发送入队只表示已发送。没有收件确认时，用一次终端输入提示检查收件，不重复投递执行请求。当前活跃上下文记住已处理或明确作废的 runId：目标错误、忙碌、重复或作废请求拒绝，不再次启动。超时先核实在途状态；终端重建重新发现并检查 Editor，不自动重发或声称跨重启持久去重。

## 必要版本维护与收尾

只有明确维护请求才更新稳定端。本轮已授权纳入四项必要本地提交；等待开发端可审查提交后记录目标版本，核对启动、产品协议、配置、包和工具依赖。稳定端保留固定 ProductName 与职责差异，按真实状态验证包解析和编译，不自动更新到最新 main、不 pull、不单独复制程序集，不自动把身份差异合回开发端。

若失败，报告发生的变化和缺口，保留未提交现场，不 reset 清空。联调只释放本轮拥有且获准结束的 Session、监听、Editor 或终端；长期目录保留，删除另须明确授权。

环境、终端通信、单实例启动、本地产品消息、真实 KCP 与双 Editor 两种角色排列分别报告。文件存在、消息入队、Prepared 或编译通过不能替代 Joined/Probe/名单/心跳/退出证据。不把双 Editor 扩大为物理两机 LAN、正式传输或玩法验收。

2026-10-11 两个精确工程的 Pipeline、稳定端实际编译、独立缓存/原生日志和固定产品名已核实。B 原线程完整收件/开始/观察/终态回报完成；同名临时设置键在两端交错写不同值，磁盘重读仍各自原值，最后只清理各自指定键和 Play。四项错误目标/忙碌/重复/作废请求均在启动前拒绝，B 身份和 Play 未被替换；去重属于当前代理上下文，没有持久执行器。

稳定端仍使用 8b95e509，实际产品入口为 ProductBootstrapUnavailable；AssetSet 文件系统尚未写入/扩展，NotRun。开发端后续产品单端、本地小集及真实 KCP 结果只证明开发端范围，不能代替稳定端必要升级和双 Editor 两种排列。用户授权不因转发扩大；详细终态、历史失败和原日志位置见本项 execution-notes。
