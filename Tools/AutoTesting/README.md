# AutoTesting — 自动测试编排

流程适配自参考编排器；为当前 CLI/Pipeline 重写实现，保留发现、前置、运行、轮询、Console、真实报告职责。没有迁入专属测试套件、productName 改写、JSON 证据/状态副本与外部验证器。

依赖：Python 3.10+ 标准库、已有 unity CLI、本项目 Editor 与 Pipeline/UTF。无第三方 Python 包。执行唯一流程见 [自动测试 SOP](../../Book/自动化测试SOP.md)。

从仓库根执行：

```powershell
python Tools/AutoTesting/autotesting/run.py --mode editor
python Tools/AutoTesting/autotesting/run.py --mode playmode
# 只有用户已授权该测试范围后才使用 --run；把占位符替换成发现的真实名字
python Tools/AutoTesting/autotesting/run.py --run --mode playmode --filter_type testName --filter "<FullName>"
python -m unittest discover -s Tools/AutoTesting/tests -v
```

默认仅发现。执行拒绝繁忙 Editor/已有作业；不会调用停止、取消或清 Console。结果即时输出，Pipeline 原生结果保持原目录。临时独占锁只防止本工具重复启动；同一工程其他操作者也必须遵守单写者。异常终止残留锁先核实进程/作业，不能自动删锁。

原生 UTF 作业使用当前 1.1.33 注册表的只读 eval 查询，兼容性未知时拒绝执行，不写入 Editor 脚本或启动测试。终态逐项核对发现名单；Console 读取全部级别、核对 seq 覆盖和原生 Error 增量，尾部截断不能算通过。域重载导致 groundTruth 尚未采样时报告编排异常。

执行退出码：0 Passed；1 编排异常/超时；2 测试或新增 Console 错误；3 部分/全部 Skipped；4 空集 NotRun。发现命令的 0 只表示查询成功，并会明确打印 NotRun。超时不取消、不能重发 run；修复后重新运行。

本次工具回归模拟真实接口的成功、失败、空集、跳过、并发、超时和日志缺口；它们不证明产品玩法通过。

## 现有产品夹具的使用

产品用例仍在 `Unity/Assets/Tests/Editor/ET` 的既有 PlayMode UTF 程序集中，辅助方法不属于 AutoTesting 编排协议。`ProductSessionTests.PumpUntil` 默认只泵真实消息，`FinishMessages` 等待接收答复，均不推进测试时钟；业务提交用原 `AdvanceTicks` 明确推进。真实 KCP 等待显式选择 `advanceTime: true`，并同时检查 Join 和匹配当前世界／关卡的独立投影。截止未满足就失败并输出阶段，不扩大超时或重跑到绿。

供应来源按真实表定义查唯一项，缺失／重复明确失败。选档与保存 UI 共用 `CreateSaveUiRepository`，只替换本夹具拥有的 Launcher repository，并登记随机测试专属目录；原任务失败、提前退出与正常结束均保留所属收尾，不触碰产品存档或别的运行。实际迁移与逐用例结果见[夹具 change](../../openspec/changes/stabilize-product-test-fixtures/execution-notes.md)。
