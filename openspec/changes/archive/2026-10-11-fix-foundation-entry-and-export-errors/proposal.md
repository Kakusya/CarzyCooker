# Proposal

## Why

同步泛型 UIEntity 入口实际查询普通 Entity 表；Luban 失败后仍可能复制输出、派生 ID，并由 CLI 和 Editor 当作正常结束。用户已确认把这两项小修合并，先提高既有底座入口的可用性，再推进异步生命周期 change。

## What Changes

- 修正同步泛型 UIEntity 路由，使其与 Type 入口使用同一 UIEntity 表、资源根、分组和优先级。
- Luban 汇总子进程退出码和完整输出；失败时停止复制、ID 派生及 ExportAll 后续本地化，CLI 返回非零。
- Editor 导表菜单消费真实失败结果，失败后不继续资源刷新和本地化缓存清理。
- 保留 active 工程、既有输出配置、Check 的不写入行为；生成途中已有部分输出如实记录，不建立整批回滚或新导表管线。
- 更新相应规格、Book、references 和 KnownIssues；规划期不改源码或生成物。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `entity`：同步泛型 UIEntity 使用独立配置与资源路由。
- `data-generation`：失败传播至 CLI，失败后阻断复制、ID 与后续导出。
- `editor-tools`：Editor 导表入口消费失败结果并停止后续成功路径。

## Impact

影响 EntityExtension、Share.Tool 的 ExcelExporter/Loader、Editor 的 ShellTool/EditorTool 及对应说明。不改 Excel 内容、表结构、生成代码、传输或玩法，也不升级 OpenSpec、Luban 或 Unity。现有把导表失败当成功的调用将收到明确失败；正常调用签名尽量保持兼容。

采用局部路由检查和隔离临时目录的成功/失败验证，不扩大到全量产品测试。严谨程度、接受的部分输出和 NotRun 范围见 [execution-notes.md](execution-notes.md)。2026-10-11 用户通过 apply 施工请求授权实施；不自动提交或归档。
