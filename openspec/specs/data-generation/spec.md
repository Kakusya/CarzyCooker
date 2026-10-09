# Luban 工程发现、校验、输出复制及 ID 派生

## Purpose

记录当前 CarzyCooker 底座的Luban 工程发现、校验、输出复制及 ID 派生。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 配置发现与并行

ExcelExporter SHALL 发现 Design/Excel 的直接子目录 luban.conf，只运行 active=true 的 cmds，并按路径变量展开后并行执行。

#### Scenario: 配置发现与并行的适用行为

- **WHEN** 执行 ExcelExporter 的配置导出
- **THEN** 启用工程的命令参与，调用方避免写同一个源输出目录

### Requirement: 只检查与多输出

Customs 包含 Check 时 MUST 移除输出/生成选项且跳过产物复制、ID 与本地化写入；非 Check 支持第一输出到其余目录的复制。

#### Scenario: 只检查与多输出的适用行为

- **WHEN** 执行 --Customs=Check,ShowCmd
- **THEN** 运行校验且不写派生输出；非 Check 导出另需授权

### Requirement: ID 开关与失败事实

文档 MUST 如实记录 IsEnableET/IsEnableGameHot 按目录存在性设置，ID 由公共 Editor JSON 派生；Luban 子命令失败汇总日志不等价于 Tool 非零退出。

#### Scenario: ID 开关与失败事实的适用行为

- **WHEN** 当前 main 分支的 ET 底座仅有 ET 配置工程，而导出器保留旧模式条件分支
- **THEN** 命令按 active 选择，现有 ET/公共 ID 输出核对日志和 diff；不从 IsEnableGameHot 的代码存在推断 GameHot 已接入

## Sources

- [luban.conf](../../../Design/Excel/ET/luban.conf)
- [ExcelExporter.cs](../../../Share/Tool/ExcelExporter/ExcelExporter.cs)
- [ExcelExporter.Luban.cs](../../../Share/Tool/ExcelExporter/ExcelExporter.Luban.cs)
- [GenerateUGFEntityId.cs](../../../Share/Tool/ExcelExporter/Generate/GenerateUGFEntityId.cs)
- [Init.cs](../../../Share/Tool/Loader/Init.cs)
