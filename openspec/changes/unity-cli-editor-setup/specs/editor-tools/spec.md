## ADDED Requirements

### Requirement: Unity CLI 与编辑器能力

编辑器自动化 MUST 使用已授权安装的 Unity CLI 与项目 Pipeline 包；按 projectPath 精确发现当前工程和实际命令，不依赖已删除的接入包、文件槽位协议或固定端口。Editor-only 工具仍留在编辑器程序集。

#### Scenario: 核实编辑器能力

- **WHEN** 使用 CLI 查询或操作本项目
- **THEN** 先确认对应 Editor/包版本、当前工程实例与实际命令；缺环境记录限制，不把其他工程连接或未完成导入当作可用

## REMOVED Requirements

### Requirement: Editor 与 Bridge

**Reason**: 用户明确要求彻底删除旧接入与协议。
**Migration**: 使用 Unity CLI 与项目 Pipeline，按当前工程发现和实际命令验证能力。
