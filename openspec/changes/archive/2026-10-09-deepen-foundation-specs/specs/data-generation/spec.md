# Spec Delta

## ADDED Requirements

### Requirement: ET 配置目标与消费范围

当前 ET 导出 SHALL 区分 gameclient、gameeditor、client、clientserver、editor 的组与输出；公共运行表、公共 Editor JSON、ET 客户端表、ET 双端表和 ET Editor JSON 各由其实际消费者读取。clientserver 数据同时复制到 Config/Luban，不把公共表与 ET 表混成一套生成 ID。

#### Scenario: 公共 UI 与 Entity 配置输出

- **WHEN** 导出 gameeditor 目标
- **THEN** 公共 Editor JSON 进入 Res/Editor/Luban，供既有 ID 生成器读取；不由 ET Editor JSON 路径替代

#### Scenario: 服务端共享配置

- **WHEN** 导出 clientserver 目标
- **THEN** 数据第一输出进入 Res/ET/ClientServer/Luban，并复制到 Config/Luban；消费者仍按 clientserver 生成的表类型读取

### Requirement: 复制和失败的写入边界

生成验证 MUST 区分子命令结果与后续写入。非 Check 路径在子命令结束后执行复制及 ID 派生，即使汇总为失败也没有自动跳过或回滚保证；复制先清理目标中符合规则的文件，再复制源输出，保留 meta 及被排除的文件。失败后不得以存在的产物或 Tool 的零退出码报告导出成功。

#### Scenario: 多输出复制目标含手写文件

- **WHEN** 输出配置声明多个目录，复制目标内有不属于生成链路的普通文件
- **THEN** 该文件可能被目标清理删除；源配置和复制目录必须先确认，不把复制目录当手写代码存放处

#### Scenario: 子命令失败但仍出现输出

- **WHEN** Luban 汇总日志报告失败而后续复制或 ID 写入已经发生
- **THEN** 报告失败与可能的部分产物，不继续把旧输出作为本轮有效配置；修复和重新生成另按授权执行

## Sources

- [五个目标及输出配置](../../../../../../Design/Excel/ET/luban.conf)
- [导出与派生流程](../../../../../../Share/Tool/ExcelExporter/ExcelExporter.Luban.cs)
- [复制清理规则](../../../../../../Share/Tool/ExcelExporter/LubanFileHelper.cs)
- [工具退出码](../../../../../../Share/Tool/Loader/Init.cs)
- [公共 Entity ID 生成](../../../../../../Share/Tool/ExcelExporter/Generate/GenerateUGFEntityId.cs)
