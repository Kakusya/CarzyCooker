# 自动化测试 SOP

本 SOP 专用于编辑器内 EditMode/PlayMode 回归。若要运行独立 Player 包体验收与启动冒烟，使用 [打包与包体验收 SOP](打包与包体验收SOP.md)。本页是唯一流程正文，skill 负责加载；工具用法见 [AutoTesting](../Tools/AutoTesting/README.md)。

## 功能任务的验收门槛

每个功能任务在标记完成前，必须实际运行其功能验收点。可自动断言的行为使用 UTF，已有用例不足时补充并执行；启动、UI、输入或联机任务还要通过真实入口确认对应表现与交互。编译和代码阅读各证明自身范围，不能将未运行功能写为通过。

在当前 change 的 execution-notes 中简短记录 `任务／功能 → UTF 用例或实际操作 → 结果`。同一用例可覆盖多个任务，同一任务可对应多个用例；逐项检查覆盖，不能用一组 Passed 计数代替功能对应关系。新增或改动行为必须运行相关用例。实现与验收条件未变的已有有效结果可复用，写明对应功能及结果来源；覆盖齐全后停止验证。

必需功能失败、NotRun、Skipped、空集、超时或输出缺失时，验收未完成，相关任务保持未完成。用户明确接受有限交付时记录例外与未验证功能，不称该功能已验收。纯文档任务记录格式、正文和引用检查，不新增无行为占位用例。

已授权的功能实施或验收包含该功能对应的相关 UTF 小集；运行前仍须明确模式、过滤范围并发现实际用例。没有功能任务／验收授权的调查默认只发现，保护已有 Play 和测试作业。

## 前置

- 当前工程 Unity Editor 已打开且 Pipeline 可达。工具自动带 `--project-path`，手工命令必须自带。
- 先读 [Unity CLI](UnityCLI.md) 和 [启动与验证](Unity启动与验证SOP.md)，核对实际命令与参数。
- Editor ready、无编译失败、Play stopped、无正在运行的测试。忙碌时停止本轮编排，不自动 cancel、停止 Play、重启 Editor 或清 Console。
- 工具同时检查 Pipeline 作业与原生 UTF 注册表。当前 UTF 1.1.33 无公开活跃查询，使用已有 eval 只读读取注册表；版本结构改变或查询失败就拒绝运行，不创建 Editor 探针脚本。
- Python 3.10+ 标准库，无需 jsonschema 或 pytest。没有参考专属套件、启动 Profile 或 productName 身份工具。

## 跑一次回归

1. 从仓库根运行 `python Tools/AutoTesting/autotesting/run.py --mode editor` 或 `--mode playmode`，发现真实 UTF 用例，不执行。
2. 按当前授权选择用例、程序集或类别。没有实际行为就不新增占位测试；ET Test/RobotCase 不自动等同 UTF 或做饭回归。
3. 明确模式与 filter 后执行：

```powershell
python Tools/AutoTesting/autotesting/run.py --run --mode playmode --filter_type testName --filter "<FullName>"
```

4. 工具核对工程和命令参数、匹配非 Explicit 测试，然后只启动一个异步作业。原生 PlayMode 使用 `run_tests --async_tests`；不要同步 HTTP 执行 PlayMode。
5. 只轮询 `test_status` 到 completed/error/cancelled 等终态。域重载短暂不可达可在总时限内等待；超时给输出，作业可能仍活着，不重发 run。
6. completed 后逐项核对终态测试名单与发现范围，等待 Player 停止，再等待 2 秒收集 Console 延迟异常。使用开跑前 session/cursor 读取全部级别，检查 seq 连续性及原生 Error 增量；reset/dropped、截断、空 groundTruth 表示覆盖缺口，不声称全绿。
7. 报告模式、过滤范围、实际计数、失败消息/堆栈、原输出路径和退出码。修复后创建新测试运行，不改写失败结果。

## 结论解读

| 退出码 | 结论 |
|---|---|
| 0 | 执行时所有用例 Passed 且本轮 Console 覆盖完整；发现命令的 0 只表示查询成功 |
| 1 | 编排/接口/超时/日志覆盖异常 |
| 2 | 测试 Failed/Inconclusive 或本轮新增 Console Error/Exception/Assert |
| 3 | 部分或全部 Skipped，不算完整通过 |
| 4 | 空集 NotRun |

没有迁参考的默认四条 mega、L1/L2/L3 注册表或逐用例重建模式。后续若建立产品回归，先确认实际行为、生命周期边界及成功/拒绝/取消/守恒等验收，再设计适合的层次；不复制参考业务名或数量。

## 原始输出与故障排查

- Pipeline 自身原始作业结果在 `Unity/Temp/pipeline_test_status.json`；本工具只读取即时接口，不复制、搬走、删改它，不新建证据 JSON、Recorder 或状态副本。
- 原始 Editor/Player 日志保留原位置。压缩错误用 [错误诊断 SOP](Unity错误诊断SOP.md)，压缩不替代原始失败。
- 多 Editor：每条命令显式传 `<repo>/Unity`。工程不符直接拒绝。
- 运行卡住：核实 test_status；取消需明确针对该作业授权，不能作为新 run 的自动前置。
- 当前 Pipeline 没有稳定作业 ID；不能恢复旧 run 或证明另一个操作者没有覆盖状态。工程内遵守单写者，本工具有临时锁；残锁不自动删。
- 编译、测试、AOT、联网和产品玩法各证明自己的范围，未运行写 NotRun。
