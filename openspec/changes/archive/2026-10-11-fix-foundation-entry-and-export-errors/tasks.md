# Tasks

本文件记录实施进度。范围、取舍与验证结果见 [design.md](design.md)、[execution-notes.md](execution-notes.md)。

## 1. UIEntity 路由

- [x] 1.1 将同步泛型 UIEntity 委托到已有正确 Type 入口；核对两种 overload 的配置/资源根、serial ID、userData 和缺行 null 行为，确认不混查普通 Entity；同步 Entity 手册/引用和对应 KnownIssues，不为这项局部委托另写镜像测试。

## 2. 导出失败链路

- [x] 2.1 完整读取 Luban 子进程输出并汇总启动异常、非零退出及现有错误判据；仅全部成功才复制/派生 ID，让异常阻断 ExportAll 本地化并传到 CLI 非零退出。以隔离临时输入/输出验证静默非零、错误输出和正常成功，确认失败时后续写入未发生；同步生成说明和 KnownIssues。
- [x] 2.2 使 ShellTool.RunAsync 向等待方报告失败，导表两个 Editor 菜单失败后终止；核对其他实际消费者的签名兼容，以失败进程验证不会进入刷新/缓存清理/选择恢复，成功路径保持原行为；同步 Editor 导表说明。
- [x] 2.3 在隔离目录验证一条完整 CLI 失败链路及 Check 不写入，检查复制目标原文件、ID 和本地化未被失败后的步骤改动；复用 2.1/2.2 已有适用结果，避免重复运行。报告子命令生成途中可能已有部分源输出，不触碰真实工作表或正式复制目标。

## 3. 必要集成收口

- [x] 3.1 完成受影响工具编译及必要 Unity 编译检查，复用前述局部验证；执行本 change 的 OpenSpec 严格验证并核对源码/文档/delta specs 一致。记录原输出和 Passed/Failed/NotRun，不跑产品玩法、双 Editor、Player/AOT 或全仓回归，不自动提交/归档。
