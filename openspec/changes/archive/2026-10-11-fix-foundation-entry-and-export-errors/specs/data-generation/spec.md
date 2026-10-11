# Spec Delta

## MODIFIED Requirements

### Requirement: ID 开关与失败事实

文档 MUST 如实记录 IsEnableET/IsEnableGameHot 按目录存在性设置，ID 由公共 Editor JSON 派生。Luban 子命令启动异常、非零退出或已有错误输出判据命中 SHALL 使本轮导出失败并传播至 Tool 非零退出；不得以存在输出文件或缺少错误文本报告成功。

#### Scenario: ID 开关与失败事实的适用行为

- **WHEN** 当前 main 分支的 ET 底座仅有 ET 配置工程，而导出器保留旧模式条件分支
- **THEN** 命令按 active 选择，现有 ET/公共 ID 输出核对日志和 diff；不从 IsEnableGameHot 的代码存在推断 GameHot 已接入

#### Scenario: 无错误文本的非零退出

- **WHEN** 一个启用的 Luban 子命令以非零退出，且没有输出错误文本
- **THEN** 本轮导出及 Tool 仍报告失败，诊断包含失败命令和退出码

#### Scenario: 输出错误但进程返回零

- **WHEN** 子命令输出命中现有错误判据而退出码为零
- **THEN** 完整读取输出后判定导出失败，不让退出码覆盖已发现的错误

### Requirement: 复制和失败的写入边界

生成验证 MUST 区分子命令结果与后续写入。非 Check 路径 SHALL 仅在所有启用子命令及输出读取完成且成功后执行复制、ID 派生和 ExportAll 后续本地化；失败阻止这些后续步骤并报告可能的部分源输出，不保证回滚已发生写入。正常复制保留原目标清理规则、meta 及排除项。

#### Scenario: 多输出复制目标含手写文件

- **WHEN** 成功导出声明多个目录，复制目标内有不属于生成链路的普通文件
- **THEN** 该文件仍可能被既有目标清理删除；源配置和复制目录必须先确认，不把复制目录当手写代码存放处

#### Scenario: 子命令失败但仍出现输出

- **WHEN** 任一启用子命令失败，而生成期间已有部分源输出
- **THEN** 报告失败与部分产物，跳过后续复制、目标清理、ID 派生和 ExportAll 本地化；不得把部分或旧输出当作本轮成功

#### Scenario: 并行导出全部成功

- **WHEN** 所有启用子命令均成功退出，完整输出也未报告错误
- **THEN** 非 Check 导出按原配置执行复制和派生；Check 仍不写派生输出

#### Scenario: 后续派生发生异常

- **WHEN** 复制、ID 派生或本地化步骤自身失败
- **THEN** 失败传播至调用方并阻止剩余步骤，如实说明已发生的部分写入，不宣称整批回滚

## References

当前链路见 [ExcelExporter.Luban.cs](../../../../../../Share/Tool/ExcelExporter/ExcelExporter.Luban.cs)、[ExcelExporter.cs](../../../../../../Share/Tool/ExcelExporter/ExcelExporter.cs) 和 [Tool Init.cs](../../../../../../Share/Tool/Loader/Init.cs)；实际验证与部分输出边界见 [执行说明](../../execution-notes.md)。
