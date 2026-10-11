# 第二工作区执行与维护规程

main本地目录固定为 `D:/MyWork/CarzyCooker`，主工作区使用main并跟踪origin/main；临时开发分支不改变目录约定，版本维护始终核对实际main提交。第二工作区固定本地分支为 `multiplayer-baseline`，当前实际工程 `D:/MyWork/CarzyCooker-NetTest/Unity`，目录可以变化。用户已明确新增必要提交维护技能、物理精简与禁止第二工作区开发，见 [精简执行记录](../../openspec/changes/archive/2026-10-11-simplify-second-workspace-executor/execution-notes.md)；之前环境/联调历史见 [环境记录](../../openspec/changes/archive/2026-10-11-multiplayer-peer-workspace/execution-notes.md)。

## 目录与职责

**工作区识别约定**：`main` 的本地目录固定为 `D:/MyWork/CarzyCooker`。“第二工作区”以固定本地分支 `multiplayer-baseline` 识别，其 worktree 本地目录可以变化；`D:/MyWork/CarzyCooker-NetTest` 是本轮实际位置，不是固定路径约束。即使另有多个 worktree，也保持这个名称，不按创建顺序或数量编号；Host／Client 切换不改变称呼。下文“稳定端／稳定对端”描述第二工作区的职责，沟通和汇报统一使用“第二工作区”。操作前按固定分支发现当前实际目录，再用完整 projectPath 选择 Editor，不凭历史目录或终端句柄推断目标。

稳定端使用同仓完整 worktree，包含 Unity、Share、Design、Config、DotNet 和 Tools；不拆 Unity 子仓，不复制开发端 DLL/生成物到旧基线。两端各用自己的 Library、Temp、Logs、UserSettings；不共享、链接或复制活动 Library。版本记录使用可读提交身份，不做 hash 校验。

主工作区负责需求、开发、OpenSpec、自审、提交选择与汇总。第二工作区只听明确命令，执行、观察、诊断到足以回报及指定版本维护；**禁止承担开发工作**，包括写/修源码、表/协议源、修改测试、创建提案或修源码冲突。发现缺陷回报主工作区处理。两端都可选择Host或Client，运行角色不改变职责。

第二工作区AGENTS使用唯一 [短执行模板](../../.agents/skills/second-workspace-sync/references/second-workspace-agents.md)，主工作区保留完整开发入口。共同授权、现场保护和结果真实性规则一致，开发流程仅放主工作区。

第二工作区只保留6项项目技能：second-workspace-sync、unity-cli、unity-error-extraction、port-management、auto-testing-sop、build-acceptance-sop；按指定动作加载。12项OpenSpec和4项业务开发技能在第二工作区实际删除，不用禁用开关替代。账户共享技能不在本端删除，也不得用于绕过禁止开发的职责。项目工程源码与运行依赖完整保留。

## 运行前核对

开发端保持 `GDK / GameDevelopmentKit`，稳定端在运行前固定保存 `GDK / GameDevelopmentKit-NetTest`；不在组件 Awake 后临时切产品名。分别核对 DefaultSettingHelper.FilePath、AssetSet 初始及扩展文件路径、Resource 的 ReadWritePathType 和实际根目录。保存不同临时设置值并重读，随后只移除自己的临时键，保留原数据。未触发的扩展和自定义消费者列为未验证。

稳定端启动前准备 `Unity/Logs/`，指定独立 `-logFile`；如用 `-upmLogFile`，也指定独立文件。保留开发端已使用的原生日志，不为统一命名重启它。日志和缓存不提交。

用 `unity pipeline list` 按完整 projectPath 发现实例，所有命令显式传本端 `--project-path`。核对 Editor ready、编译终态、Console groundTruth、Play stopped 和 UTF 无在途任务；忙碌拒绝，不 cancel、stop 他人操作或清 Console。Pipeline 工具端口动态发现，不能当产品 endpoint；Host 的实际 loopback endpoint 从产品监听结果取得，Client 显式提交非零目标，不扫描端口。

## 当前活跃终端消息

先加载现有 `orca-cli`，动态发现 worktree 和 terminal handle；使用现有 orchestration send/check/reply，不创建调度脚本、常驻服务、持久任务文件或假 worker 生命周期。

请求必须包含 runId、目标 Unity 完整路径、Host/Client 角色、listen/connect、动作、观察范围与完成条件。接收方核对 cwd/projectPath、授权和忙碌状态，回报接受或拒绝；接受后分别回报开始与最终结果，原线程附失败原因、项目栈帧和原日志位置。

发送入队只表示已发送。没有收件确认时，用一次终端输入提示检查收件，不重复投递执行请求。当前活跃上下文记住已处理或明确作废的 runId：目标错误、忙碌、重复或作废请求拒绝，不再次启动。超时先核实在途状态；终端重建重新发现并检查 Editor，不自动重发或声称跨重启持久去重。

## 必要版本维护与收尾

维护统一使用 [second-workspace-sync](../../.agents/skills/second-workspace-sync/SKILL.md)。主工作区从本地main挑选必要已提交commit，核对依赖/等效补丁并提供明确有序清单；第二工作区只执行指定cherry-pick，不自行选提交、改清单、全量merge/pull或复制DLL。源码修复和冲突解决属于主工作区。指定cherry-pick生成的提交是维护结果，不是第二工作区开发提交。

Git维护前需干净现场且无其他Git操作；AGENTS、ProductName、精简删除等未提交差异也须回报，由主工作区安排保存或维护提交，不自动stash/reset/clean。冲突仅abort本轮自己启动的序列，并回报实际最后版本和冲突路径，不修改源码。维护后由主工作区核对固定产品身份、短AGENTS和6项技能清单；指定配置维护或文件删除也只能由主工作区安排，不能恢复完整开发职责。命令参数与版本源/目标记录以技能为唯一详细正文。

若失败，报告发生的变化和缺口，保留未提交现场，不 reset 清空。联调只释放本轮拥有且获准结束的 Session、监听、Editor 或终端；长期目录保留，删除另须明确授权。

环境、终端通信、单实例启动、本地产品消息、真实 KCP 与双 Editor 两种角色排列分别报告。文件存在、消息入队、Prepared 或编译通过不能替代 Joined/Probe/名单/心跳/退出证据。不把双 Editor 扩大为物理两机 LAN、正式传输或玩法验收。

2026-10-11 两个精确工程的 Pipeline、稳定端实际编译、独立缓存/原生日志和固定产品名已核实。B 原线程完整收件/开始/观察/终态回报完成；同名临时设置键在两端交错写不同值，磁盘重读仍各自原值，最后只清理各自指定键和 Play。四项错误目标/忙碌/重复/作废请求均在启动前拒绝，B 身份和 Play 未被替换；去重属于当前代理上下文，没有持久执行器。

环境B使用8b95e509，实际产品入口为ProductBootstrapUnavailable；维护C已明确纳入20582007并实际编译通过，固定身份/职责保持。随后产品D开发Host/稳定Client与E稳定Host/开发Client均完成：名单/Probe一致、空闲心跳持续，D断线保留离线成员，E显式Leave移除一次，存活端Probe仍成功。双方只结束自身Play和产品资源，长期Editor/终端/目录及20582007选定代码基线保留；最终开发收口提交仅为文档，不自动升级稳定端。AssetSet文件系统初次/扩展写入与其他自定义写入仍未验证，本机双Editor不证明物理两机LAN或独立包体。用户授权不因转发扩大；详细终态、历史失败和原日志位置见本项execution-notes与产品execution-notes。

随后用户明确授权F盲发收件与退出两个Editor；Host单次发送，第二工作区从真实KCP收件日志读取文本并原样回复，两个Editor均正常退出、进程及Pipeline实例已消失。工作区、缓存、日志与代理终端继续保留；此授权不扩大为自动重启或删除长期目录，实测细节见产品execution-notes。
