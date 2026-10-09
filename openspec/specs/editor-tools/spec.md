# 既有编辑器工具、生成骨架和服务工具边界

## Purpose

记录当前 CarzyCooker 底座的既有编辑器工具、生成骨架和服务工具边界。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；初始迁移只作静态验证；后续 CLI 接入结果见对应手册，产品构建与玩法验证仍未运行，不描述未来做饭业务已实现。

## Requirements

### Requirement: 骨架工具

UIFormCodeCreator SHALL 创建代码与 UI prefab 并在重载后挂 Mono；UGFEntityCodeCreator 只生成 Game/UGFEntity 下的骨架，不建 prefab、不填 Excel，遇已有代码拒绝覆盖。

#### Scenario: 骨架工具的适用行为

- **WHEN** 生成一个 UGFEntity 名称
- **THEN** 生成 UGFEntity/MonoUGFEntity/System 三文件，资源与配置仍需分别接入

### Requirement: 现有开发服务

Share.Tool MUST 分发 ExcelExporter、Proto2CS、LocalizationExporter；FileServer 按配置目录提供静态资源，其他启动/清理 Shell 不因存在就自动执行。

#### Scenario: 现有开发服务的适用行为

- **WHEN** 文档迁移列出工具和服务
- **THEN** 只读源码与脚本，不启动服务、不执行清理、发布或打包

### Requirement: Unity CLI 与编辑器能力

编辑器自动化 MUST 使用已授权安装的 Unity CLI 与项目 Pipeline 包；按 projectPath 精确发现当前工程和实际命令，不依赖已删除的接入包、文件槽位协议或固定端口。Editor-only 工具仍留在编辑器程序集。

#### Scenario: 核实编辑器能力

- **WHEN** 使用 CLI 查询或操作本项目
- **THEN** 先确认对应 Editor/包版本、当前工程实例与实际命令；缺环境记录限制，不把其他工程连接或未完成导入当作可用

### Requirement: 骨架生成的失败与交付范围

ET UI/UGFEntity 代码骨架生成 MUST 在目标代码已存在或模板缺失时拒绝该项写入，不覆盖已有代码。多个文件按顺序创建，没有整组预检和自动回滚保证。UI 的 Mono 挂载等待脚本重载；生成完成不代表 Excel 注册、资源收集、绑定和实际打开验收已完成。

#### Scenario: 后续目标文件已存在

- **WHEN** 同一组代码前面的文件已生成，后面的目标已存在而抛出异常
- **THEN** 前面的新文件可能保留；先核对实际部分输出，不能把失败当作没有任何改动

#### Scenario: UI Prefab 与脚本重载

- **WHEN** UI 骨架创建新 Prefab 并记录待挂 Mono 的名称
- **THEN** 重载后的回调再尝试挂载；类型尚不可用或资源找不到时不构成 UI 已可运行

#### Scenario: UGFEntity 骨架生成

- **WHEN** 三份 UGFEntity/Mono/System 文件成功生成
- **THEN** Prefab、配置表、CodeBind 与资源导出仍按实际流程接入，工具不代办这些步骤

## Sources

- [UIFormCodeCreator.cs](../../../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UIFormCodeCreator.cs)
- [UGFEntityCodeCreator.cs](../../../Unity/Assets/Scripts/Game/ET/Editor/CodeCreator/UGFEntityCodeCreator.cs)
- [DefineSymbolTool.cs](../../../Unity/Assets/Scripts/Game/Editor/DefineSymbol/DefineSymbolTool.cs)
- [EditorTool.cs](../../../Unity/Assets/Scripts/Game/Editor/Tool/EditorTool.cs)
- [Init.cs](../../../Share/Tool/Loader/Init.cs)
- [Startup.cs](../../../Share/FileServer/Startup.cs)
- [Shell](../../../Tools/Shell)

- [Unity CLI/Pipeline](../../../Book/UnityCLI.md)
- [项目包](../../../Unity/Packages/manifest.json)
- [UI 接入](../../../Book/UI开发.md)
- [Entity 接入](../../../Book/Entity开发.md)
