# Design

## Context

动机见 proposal.md。参考实现为 `D:/MyWork/blade_game/Unity/Assets/Scripts/Game/Editor/ToolBar/GitBranchToolBar.cs`；本工程 `Game.Editor.asmdef` 已引用 `ToolbarExtension.Editor`，右侧现有 ExportExcel 使用 priority 99，标签可沿用 priority 100。

实际本工作区分支为 `Kakusya/dev-name`，`.git` 为指向主仓元数据的文件；不采用文档中的固定 main 作为标签。本工具没有 ET 实体、消息、表或运行时 owner：静态 GUIContent、样式和解析缓存由 Editor 域持有，域重载后重建。

## Goals / Non-Goals

**Goals:** 复用现有 Toolbar 接口；明确仓库路径解析与缓存交接边界。

**Non-Goals:** 不引入实时 Git 监控，也不关联参考项目的 ProductName 隔离工具。

## Decisions

- 复制参考文件，保持右侧标签与 tooltip 语义；仅补显式 internal、if 花括号和相对 gitdir 路径解析。这些适配解决本项目风格与路径基准问题，无需新组件或外部 Git 进程。
- 从 `Application.dataPath` 的上两级取得仓库根；普通 `.git` 目录读取 HEAD，`.git` 文件解析 gitdir，再相对于仓库根解析非绝对路径。保持当前仓库 `Unity/Assets` 布局；不引入通用仓库搜索器。
- 首次绘制解析一次，复用静态内容和样式，避免逐帧 I/O；切分支后通过域重载或重开 Editor 更新，不增加轮询或刷新按钮。
- Git 信息属于可选显示，缺文件、格式错误或读取异常保留占位符；不修改 Git、ProjectSettings 或工作区文件。

## Risks / Trade-offs

- [Git 不可读或缺失] → 显示 `-`；本工具不承担修复仓库职责。
- [分支切换后缓存未刷新] → 保留参考边界并写入 Book。
- [本工作区 Editor 未打开] → Unity 编译与界面检查记 NotRun；验证投入见 execution-notes.md。

## Migration Plan

在现有 Editor 目录新增脚本并复制参考元数据，同步用法和规格；Editor 下一次导入即可发现标签。回退时仅移除这对新增资源并撤销本次文档增量，不操作用户场景或 Git 分支。
