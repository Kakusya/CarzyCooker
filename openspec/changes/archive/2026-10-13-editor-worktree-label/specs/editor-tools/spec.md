# Spec Delta

## ADDED Requirements

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
