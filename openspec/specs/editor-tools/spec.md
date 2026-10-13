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

### Requirement: Editor 工具栏 Git 分支标识

Editor 主工具栏右侧 SHALL 显示当前工作区的 Git 分支名，兼容普通仓库和 linked worktree；detached HEAD 显示提交号前七位，较短内容完整显示；无法取得 Git 信息时显示 `-`。标签在首次绘制时读取，脚本域重载或重开 Editor 后重新读取，不保证运行中切分支实时刷新。

#### Scenario: 普通仓库显示当前分支

- **WHEN** 仓库 `.git` 为目录且 HEAD 指向命名分支
- **THEN** 标签显示完整分支名，tooltip 显示 HEAD 引用文本

#### Scenario: linked worktree 显示自身分支

- **WHEN** 仓库 `.git` 为合法 gitdir 文件，路径为绝对路径或相对于仓库根的路径
- **THEN** 标签读取该 worktree 元数据中的 HEAD 并显示自身分支，不使用主仓分支或固定 main

#### Scenario: detached HEAD

- **WHEN** HEAD 包含提交号而非分支引用
- **THEN** 标签显示前七位或完整较短内容，tooltip 为 `Detached HEAD`

#### Scenario: Git 信息不可用

- **WHEN** Git 文件缺失、格式不合法、HEAD 为空或发生读取异常
- **THEN** 标签保留 `-` 和 `Git info unavailable` tooltip，不阻断工具栏绘制

#### Scenario: 脚本重载后刷新身份

- **WHEN** 分支改变后脚本域重载或重开 Editor
- **THEN** 下一次首次绘制重新读取当前身份；重载前允许保留原标签

### Requirement: 导表菜单的失败传播

Editor 导表入口 MUST 消费工具的真实执行结果。工具启动异常、非零退出或导出异常 SHALL 明确报告失败，并停止该菜单的资源刷新、本地化缓存清理和成功路径的选择恢复；不得只记录错误后正常返回给菜单。成功时保留既有后续行为。

#### Scenario: 导表工具返回失败

- **WHEN** Editor 执行导表菜单，而工具以非零退出
- **THEN** 菜单报告失败及工具诊断，不执行后续资源刷新或本地化缓存清理，不提示成功

#### Scenario: 工具无法启动

- **WHEN** 导表工具启动发生异常
- **THEN** 菜单按失败终止，不因执行包装层吞掉异常而继续后续步骤

#### Scenario: 导表工具成功

- **WHEN** 导表工具完整执行并返回成功
- **THEN** 菜单继续原有资源刷新、适用的缓存清理和选择恢复

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
- [GitBranchToolBar.cs](../../../Unity/Assets/Scripts/Game/Editor/ToolBar/GitBranchToolBar.cs)
- [自定义 Toolbar](../../../Book/自定义Toolbar.md)
- [editor-worktree-label 执行与验收记录](../../changes/archive/2026-10-13-editor-worktree-label/execution-notes.md)。
