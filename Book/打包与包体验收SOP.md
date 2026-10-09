# 打包与包体验收 SOP

本 SOP 用于准出期的热更准备、资源刷新、独立 Player 构建与验收。开发期 Editor 回归使用 [自动测试 SOP](自动化测试SOP.md)。迁入 SOP 不代表获准在本轮构建产品。

## 当前入口与依赖

- Unity 6000.3.18f1、Odin、目标平台模块与许可；核对实际机器，不预设 Android/IL2CPP 模块已齐。
- 读 [HybridCLR](HybridCLR热更.md)、[资源导出](ResourceCollection导出SOP.md)、[一键打包](一键打包.md)；本项目保留 AOT 元数据和热更 DLL。
- 现有 `Game/Build Tool Editor` 与 `Game.Editor.BuildHelper` 负责构建。没有参考的 BuildAcceptancePkg、双包 run_job.py、业务验收参数或 L1 测试集合。

## 执行步骤

1. 确定当轮授权的平台、正式包目标、运行验证范围与输出目录，核对现有源码/主规格。不将独立 Player 测试包写成现有能力。
2. 核对工程和实际命令，确认编译与 Console 状态；不要重启、关闭用户 Editor 或改变构建设置解决不明问题。
3. 按当前模式准备 HybridCLR DLL/AOT、Luban、资源规则与 ResourceCollection，分别检查成功日志和 diff。失败停在该阶段，不用旧产物继续验收。
4. 通过既有构建工具执行授权的资源和 Player 构建；检查实际 BuildReport/错误及输出，返回“triggered”或目录存在不算构建完成。
5. 启动当轮产生的独立 Player，保留原 Player 日志、退出码和实际行为结果。底座正常入口是 Launcher → ProcedureET，不要求参考 boot ready 文本。
6. 验证启动、资源/热更加载、UI/输入、平台适配和任务要求的真实行为。发生异常给原消息/堆栈与日志路径；退出码 0 不能单独证明这些行为。
7. 检查本轮构建副作用，仅恢复本轮临时变更，保留用户原设置与未提交修改。分别报告 Passed/Failed/Skipped/NotRun。

## 人工验收矩阵

| 范围 | 最低可观察结果 |
|---|---|
| 资源 | 刷新与构建真实完成，所需资源进入正确组，diff 无意外删包 |
| 热更 / AOT | 目标平台 DLL/元数据准备与加载符合现有 HybridCLR 链路 |
| 正式 Player | 构建成功，启动进入实际 ET 底座，原日志无本轮未处理致命异常 |
| 产品行为 | 仅验证已实现并已授权的真实需求，未实现做饭玩法不标通过 |
| 独立测试包 | 当前专用构建/调度器未接入，NotRun；不能套用参考启动参数 |

## 无头与隔离边界

同工程 Library 只能由一个 Editor 持有。若后续授权无头构建，先精确核对工程并确认该工程 Editor 已关闭；后台进程隐藏窗口，同时保留原 stdout/stderr 与 Editor 日志。`-logFile` 缓冲可能漏掉早期秒退消息，不能只看该文件。

UTF TestRunnerApi 若后续被用于独立测试包构建，须 `ScriptableObject.CreateInstance<TestRunnerApi>()`；`new` 不保证调度成功。专用构建入口、双包编排和 E2E 用例仍须另案设计与授权，不自行引入证据 JSON/hash。

源码：[BuildHelper](../Unity/Assets/Scripts/Game/Editor/Build/BuildHelper.cs)、[BuildToolEditor](../Unity/Assets/Scripts/Game/Editor/Build/BuildToolEditor.cs)、[ET Build](../Unity/Assets/Scripts/Game/ET/Editor/Build/)。
