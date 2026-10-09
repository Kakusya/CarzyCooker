# Unity CLI 与 Pipeline

本项目编辑器自动化统一使用 Unity CLI 与官方 `com.unity.pipeline`；技能本身按 [审批清单](../Docs/AI/skill-roadmap.md) 逐项批准，不自动安装参考的额外管线。

Unity CLI 是命令行工具，Pipeline 是 Editor 中提供执行能力的包，skill 是 AI 使用它们的说明。官方 `unity-cli` 技能已经内嵌在 CLI 中，可用 `unity skill show` 读取；2026-10-09 本轮获准迁移后，已在仓库根用 `unity skill install codex --local` 接入 [官方技能](../.agents/skills/unity-cli/SKILL.md)，保持 CLI 内嵌原件。官方 `unity-pipeline` 技能随项目包提供；官方技能原文保持上游维护，项目规则放入口与手册。

## 当前环境

| 项目 | 核实结果 |
|---|---|
| Unity Editor | `6000.3.18f1`，revision `5ebeb53e4c07` |
| 已有 Editor | `D:/MyUnityEditor/6000.3.18f1/Editor/Unity.exe`，可执行文件 ProductVersion 匹配 |
| 缺版本时下载目录 | 用户指定 `D:/UnityEditor`；当前版本已有安装 |
| Unity CLI | `1.0.0-beta.11`，当前 PATH 可用 |
| 项目包 | manifest 固定 `com.unity.pipeline@0.8.0-exp.1`，官方 pipeline install 写入 |
| 项目目录 | `D:/MyWork/CarzyCooker/Unity`；CLI 每次显式匹配 |

CLI 与包版本不能照搬其他项目；升级先看所装版本的帮助与包 CHANGELOG，确认配对和任务授权。官方安装说明见 [Unity CLI](https://docs.unity.com/en-us/unity-cli/use-unity-cli) 与 [Pipeline](https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package)。

## 发现与命令真值

从实际 Unity 项目目录运行，或显式指定 `--project-path`。首先检查 `unity pipeline list` 返回的实例是否精确匹配本项目；端口按运行时发现，不固定、不把别的 Editor 当成当前项目。

```powershell
unity --version
unity pipeline list
unity list --project-path "D:/MyWork/CarzyCooker/Unity" --format json
unity command --help
```

实际操作前核对当前实例提供的命令和参数；不能把参考文件的固定命令数量当成当前版本保证。`--format json` 的即时输出不另存为证据包。不要输出或保存 `Library/Pipeline/.unity-pipeline-port` 的 token 内容。

## 编译与状态

确认当前实例提供对应命令后，使用选项形式传参，避免 `key=value` 被解释为字面量；通过 CLI 帮助核实所装版本的参数位置。

```powershell
unity command editor_status --project-path "D:/MyWork/CarzyCooker/Unity"
unity command recompile --project-path "D:/MyWork/CarzyCooker/Unity"
unity command recompile_status --project-path "D:/MyWork/CarzyCooker/Unity"
unity command console_status --project-path "D:/MyWork/CarzyCooker/Unity"
unity command console --project-path "D:/MyWork/CarzyCooker/Unity"
```

编译验证需要实际状态完成和 Error/Exception 输出；命令提交成功不等于编译成功。状态值和结果结构以当前包为准，嵌套 JSON 字符串只在需要时解析，不打印全量日志或截图 base64。

当前 0.8.0-exp.1 的日志命令是 `console` / `console_status`，其中 groundTruth 提供 Editor 原生 Console 计数和编译失败标志；参考旧版本的 `get_console_logs` 当前不存在。以实际命令清单为准，不照抄旧技能示例。

## 首次导入与验证范围

首次启动可能正在解析 Git/registry 依赖、导入和编译。Editor 进程存在而 Pipeline 尚未就绪时报告当前阶段，等待原日志和实际状态；不要反复重启，不用强制刷新掩盖错误。

包解析、CLI 连接、编译、测试、Play 和产品构建分别报告。当前接入任务只验证依赖、实例、命令和编译；不自动运行 Play、玩法测试或打包。缺能力/许可/依赖须列出原日志路径和限制。

## 本次实际验证

2026-10-09 已验证：CLI 1.0.0-beta.11；manifest、lock 与实际包均为 Pipeline 0.8.0-exp.1；Editor 文件版本 6000.3.18f1_5ebeb53e4c07。Pipeline 发现本项目，server 可达；`editor_status` 返回 ready、compiling=false、playMode=stopped。实际命令清单为 160 项（本次观察，不作为固定契约）。

`recompile` / `recompile_status` 返回 up_to_date、failed=false、compilationFailed=false；`console_status` 的 Editor groundTruth 同样没有编译失败。Play、测试、产品构建均未执行。

原 Console 保留 4 条本次探测错误：首次导入窗口两次主线程超时，误用参考旧日志命令两次 Command Not Found；另有本环境管理员运行警告。旧命令已从有效用法中纠正。没有清除或改写错误，不声称原 Console 无错误。后续正确命令运行与编译结果分别确认。

Editor 原日志位于 `C:/Users/Administrator/AppData/Local/Temp/CarzyCooker-UnityCLI-Editor.log`，不提交日志或额外状态副本。

## 已迁入 SOP

启动见 [启动与验证](Unity启动与验证SOP.md)，资源见 [ResourceCollection](ResourceCollection导出SOP.md)，测试见 [自动测试](自动化测试SOP.md)，错误见 [错误诊断](Unity错误诊断SOP.md)，构建见 [包体验收](打包与包体验收SOP.md)。本轮只读发现 EditMode/PlayMode 均为空集，未运行产品测试或构建；自动测试工具自身回归和原生查询分别报告。
