# 执行说明

## 严谨程度与验证取舍

UIEntity 修正是局部委托路由；导表改动会影响后续文件复制和失败可信性，需要明确检查失败后的写入边界。采用一条路由验证及隔离临时目录中的正常、静默非零退出、明确错误验证即可，不为小修建立完整测试平台。复用现有配置目标及工具入口，不重跑已有联机套件。

接受 Luban 子命令运行期间留下部分源输出，不承诺事务回滚；失败后停止复制、ID、本地化和 Editor 成功路径。若实际失败仍出现后续写入、成功导出输出变化或错误结果被吞掉，针对该疑点补验。

## 实施与证据

2026-10-11 用户授权本 change 施工。实现保持既有 API 和五目标配置，没有修改真实 Excel、生成物或第二工作区。

- `ShowUIEntity<T>` 委托已有 UIEntity Type 入口；静态核对 DTUIEntity、`Assets/Res/UI/UIEntity/`、分组、优先级、负 serial ID、逻辑类型和原 userData。缺行警告/null，不查普通 Entity；按任务约定不另建委托镜像测试。
- 每条 Luban 命令分别保留失败原因，等待退出及两个输出流 EOF 后判断；全部并行命令收尾后统一抛出。任一失败阻断复制、目标/源空目录清理、ID 和 ExportAll 本地化。现有 Loader catch 已返回 1，无需改写 CLI 或 ExportAll。
- ShellTool.RunAsync 保持 UniTask 签名，启动/执行异常及非零退出向等待方抛出；两个导表菜单分别记录上下文并 return。其他实际消费者为 AssetBundleExtractTool 和 UXTextTable，await/UniTask.WhenAll 签名兼容；它们收到失败也会沿原 await 终止，没有扩大修改消费者。
- Book、生成/生命周期引用及进度页已同步，两个已修 KnownIssues 条目已移除。实施结束时只更新 delta；后续用户明确授权归档后，主规格同步结果见下方归档收口。

隔离夹具和原始日志位于 `C:/Users/Administrator/AppData/Local/Temp/CarzyCooker-foundation-13c5785eb01848eea674966bc9d27f12/`，保留原位。夹具是本轮临时验证，不新增仓库测试平台或持久工具。

| 验证 | 结果 | 证据与边界 |
| --- | --- | --- |
| Share.Tool 编译 | Passed | `dotnet build Share/Tool/Share.Tool.csproj --no-restore -v minimal`，退出 0，0 warning/0 error，6.91 秒；原始输出在本会话工具记录 |
| 静默非零退出完整 CLI 链路 | Passed | `run2-silent.log`：模拟 Luban 子命令退出 23，实际 Tool 返回 1；每个失败夹具的 16 份复制目标、meta、ID、本地化原文件内容不变；源部分输出和空目录保留，慢速成功 peer 已完成 |
| 零退出但 stdout 错误 | Passed | `run2-stdout.log`：2000 行普通输出后无换行末尾 `\|ERROR\|` 被捕获，实际 Tool 返回 1，后处理未执行 |
| 零退出但 stderr 错误 | Passed | `run2-stderr.log`：无换行末尾错误被捕获，实际 Tool 返回 1，后处理未执行 |
| 正常成功完整 CLI 链路 | Passed | `run2-success.log`：实际 Tool 返回 0，复制更新/删除旧普通文件/保留 meta，7 份 ET ID 和 5 份本地化输出更新 |
| Check 不写入 | Passed | `run2-check.log`：实际 Tool 返回 0，子进程检查已移除 -c/-d/输出路径；夹具文件内容及集合不变（子进程完成观察标记除外），未复制/派生/导本地化 |
| 本地化自身失败 | Passed | `run2-localization-failure.log`：隔离无效工作簿使 Tool 返回 1，Luban 复制/ID 已完成，本地化原文件不变；不宣称回滚前序写入 |
| 启动异常 | Passed | `editor-probe.log`：不存在工作目录使 ShellTool await 抛出并保留 Win32Exception；反射调用实际 Tool 的 RunCommand 返回启动异常诊断供汇总 |
| 两个菜单失败/成功分支 | Passed | `editor-probe.log`：直接编译本次 ShellTool/EditorTool 源码，以框架替身记录服务调用；失败两项的刷新/缓存清理/选择写入均 0，成功各为 1/1/2，选择对象保持；每次失败在菜单边界记录 1 条原因 |
| Unity 编译 | Passed | 当前主工程 Editor PID 61084，Unity 6000.3.18f1、Pipeline 0.8.0-exp.1；recompile_status=completed，failed=false、compilationFailed=false；Game/Game.Editor DLL 的 12:36:57 时间晚于本项源码修改，复用已完成编译，不重复触发 |
| OpenSpec 严格验证、diff 空白检查 | Passed | `openspec validate fix-foundation-entry-and-export-errors --strict`、`git diff --check`，见本会话原始输出；收口核对源码/设计/文档/delta 一致 |

初次 CLI 夹具由 PATH 选中的 `D:/Environment/dotnet` 启动，该安装只含 Microsoft.NETCore.App 10.0.8、没有 8.x runtime，因此启动失败；原 `silent.log` 保留该宿主错误。第二次使用新隔离目录及仅对子进程生效的 `DOTNET_ROLL_FORWARD=Major`，实际 Tool 仍以 net8.0 为目标编译，在 .NET 10 上验证上述失败链路。该轮验证未安装 runtime、未改正式工具配置。

2026-10-11 后续环境安装/迁移任务纠正了“本机缺少 .NET 8”的表述：`D:/MyEnvironment/dotnet` 原本已有 SDK 8.0.425 和 runtime 8.0.31，只是没有被当前 PATH/DOTNET_ROOT 选中。用户授权后已在该目录并存安装 .NET 10 SDK 10.0.401/runtime 10.0.12，并将持久 PATH/DOTNET_ROOT 指向它；新环境可发现 8/10 两套 runtime。未为环境迁移重跑本 change 的测试或真实导表。

Unity 初次实例发现为空；后续一次性编译启动被 CLI 拒绝（本工程已由另一个会话打开）。随后精确发现并复用现有 Editor，不关闭、不重启。原日志为 `C:/Users/Administrator/AppData/Local/Unity/Editor/Editor.log`；Pipeline buffer 保留异步生命周期并行施工先前的 4 条 OwnedResourceLifecycleTests 编译错误（每条 count=1）。当前 groundTruth consoleErrors=0、compilationFailed=false、compiling=false；没有清 Console，也不把历史错误抹为通过。其他会话新增的 ResourceContainer、AsyncScope、测试/asmdef 等不属于本 change，原样保留。

## 收口核对与未运行项

5/5 任务完成。验证结果复用本轮已有证据，没有为阶段切换重跑套件。

四项 delta Requirement 均可映射到入口、失败汇总或菜单边界。UIEntity 三个场景采用最小委托静态核对；导表及菜单场景使用本轮上述证据，复制/ID 自身异常传播另由现有无吞错调用链静态核对。实现复用 Type 入口、既有 CLI catch、UniTask 和原复制规则，符合 design 的四项决策。没有新增业务树、SceneType、消息或 owner 架构。

真实 Luban 配置/工作表导出、真实 Unity 菜单服务副作用、UIEntity 实际显示及 .NET 8 runtime 下执行：NotRun。模拟子命令只控制进程退出/输出，不代做被测 Tool 的复制、ID、本地化或 CLI catch；菜单框架替身只记录服务调用，不冒充真实 Editor 操作。

产品玩法、UTF 全仓回归、双 Editor、Player/AOT、LAN：NotRun，属于本项明确非目标。接受非 Check 子命令的部分源输出以及后处理失败前已有写入；失败后仍发生后处理、正常输出出现变化或错误被吞时再针对疑点补验。不自动提交、push、同步主规格或归档。

## 归档收口

2026-10-11 用户明确要求归档后 commit。复用前述工具/Unity 编译与隔离验证，不为归档重跑行为测试。

- 已同步 entity 的 UIEntity 路由、data-generation 的两项失败契约和 editor-tools 的菜单失败契约；逐项比较 delta 与主规格一致，主规格标题、Purpose 和其他 Requirement 保持。
- `openspec validate --specs --strict --no-interactive`：19/19 Passed；随后使用 `openspec archive fix-foundation-entry-and-export-errors --skip-specs --yes` 归档，skip-specs 仅避免重复已完成的同步。
- 归档位置为当前目录；内部相对引用、生成参考和进度页链接同步到归档路径。commit 仅包含本项实现/文档/主规格/归档产物及环境事实纠正，另一项异步生命周期的共用文档修改按片段保留为未提交。
- 归档后 `openspec validate --all --strict --no-interactive`：21/21 Passed；相对引用迁移 14 处、暂存 diff 空白检查 Passed。归档没有触发额外行为回归。
