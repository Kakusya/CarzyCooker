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
