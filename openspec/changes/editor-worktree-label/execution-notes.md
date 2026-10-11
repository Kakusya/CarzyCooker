# 执行说明

本次为局部、可逆的 Editor 显示工具迁移，只读 Git 元数据；主要风险是身份显示错误或工具栏编译失败，不涉及游戏状态、存档或文件覆盖。复用探索阶段已核对的参考实现、Toolbar 依赖、Game.Editor 引用和实际 HEAD；用局部 diff、实际工作区元数据核对及 OpenSpec 严格校验收口，不运行玩法、构建或全量测试。

当前 Pipeline 发现的两个 Editor 分别打开 D:/MyWork/CarzyCooker/Unity 和 D:/MyWork/CarzyCooker-NetTest/Unity，没有精确匹配 D:/MyWorkTree/CarzyCooker/dev-name/Unity。本轮 Unity 编译与界面验证为 NotRun；不另开 Editor 或操作其他工程。接受首次读取后切分支不实时刷新的原实现边界；本工作区 Editor 打开后应补一次编译与标签显示检查，如发现 CS 错误、标签不出现或身份不符，再针对该问题补验。

## 本轮结果

- 已迁入 GitBranchToolBar.cs 与原参考元数据；补齐 internal、if 花括号、相对 gitdir 解析和空路径保护。GUID 仅在新增文件中出现一次，Game.Editor 已有 ToolbarExtension.Editor 引用。
- 实际 `.git` 指针对应 HEAD 为 `ref: refs/heads/Kakusya/dev-name`，标签预期显示 `Kakusya/dev-name`；此项为文件核对，不冒充 Editor 显示结果。
- Book 用法、主规格与 delta 已同步；新增 Requirement 文本一致，原主规格内容保留。`openspec validate editor-worktree-label --strict` 通过，`openspec validate --specs --strict` 为 16 项规格通过、0 失败；均为格式校验，不是产品测试。
- `git diff --check` 通过；8 个新增文件的空白与 UTF-8 文本检查通过，新增源码链接有效。
- Unity 编译、Console 与工具栏界面验证：NotRun（无本工作区 Editor 实例）。玩法测试与产品构建：NotRun（不属于本次验证范围）。
