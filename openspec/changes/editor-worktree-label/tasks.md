# Tasks

## 1. 工具栏迁移与文档

- [x] 1.1 迁入 GitBranchToolBar.cs 及参考元数据，适配可见性、花括号与相对 gitdir，更新 Book/自定义Toolbar.md；核对现有程序集引用、标签注册、当前工作区 HEAD 与元数据 GUID 无冲突。
- [x] 1.2 将标签 Requirement 同步到 editor-tools 主规格并补源码引用；检查主规格与 delta 一致且保留原有要求，执行 OpenSpec 严格校验。

## 2. 最小集成检查

- [x] 2.1 检查局部 diff 和文件格式，在 execution-notes.md 记录结果；仅在 Pipeline 已精确发现本工作区 Editor 时执行编译与 Console 检查，否则将 Unity 编译和界面验证明确记录为 NotRun，不启动额外 Editor，不运行产品测试。
