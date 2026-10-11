# Proposal

## Why

同时使用多个工作区时，Unity Editor 的默认外观不易区分当前分支。迁入参考项目 `D:/MyWork/blade_game` 的工具栏分支标签，使使用者能直接确认当前工作区的 Git 身份。

## What Changes

- 在主工具栏右侧显示当前 Git 分支；支持普通仓库、linked worktree 和 detached HEAD，读取失败显示 `-`。
- 沿用首次绘制解析、脚本重载后重新解析的方式；适配相对 `gitdir` 路径和本项目代码规范。
- 同步 Toolbar 用法与 `editor-tools` 规格。严谨程度和验证边界见 [execution-notes.md](execution-notes.md)。
- 范围外：自动切分支、定时刷新、修改 ProductName、设置 Git 索引、创建工作区及玩法改动。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `editor-tools`：新增 Editor 工具栏 Git 分支标识契约。

## Impact

- 新增 `Unity/Assets/Scripts/Game/Editor/ToolBar/GitBranchToolBar.cs` 及其资源元数据，编入现有 `Game.Editor`。
- 复用已有 `ToolbarExtension.Editor`，不新增包、程序集或运行时依赖。
- 更新 `Book/自定义Toolbar.md`、对应主规格及本 change；参考项目只读。
