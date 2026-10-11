# Spec Delta

## ADDED Requirements

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

## References

本文件新增导表入口契约，不修改现有工作区标签能力；当前入口见 [EditorTool.cs](../../../../../../Unity/Assets/Scripts/Game/Editor/Tool/EditorTool.cs) 和 [ShellTool.cs](../../../../../../Unity/Assets/Scripts/Game/Editor/Tool/ShellTool.cs)。
