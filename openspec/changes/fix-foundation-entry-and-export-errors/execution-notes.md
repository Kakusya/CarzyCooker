# 执行说明

## 严谨程度与验证取舍

UIEntity 修正是局部委托路由；导表改动会影响后续文件复制和失败可信性，需要明确检查失败后的写入边界。采用一条路由验证及隔离临时目录中的正常、静默非零退出、明确错误验证即可，不为小修建立完整测试平台。复用现有配置目标及工具入口，不重跑已有联机套件。

接受 Luban 子命令运行期间留下部分源输出，不承诺事务回滚；失败后停止复制、ID、本地化和 Editor 成功路径。若实际失败仍出现后续写入、成功导出输出变化或错误结果被吞掉，针对该疑点补验。

## 当前状态

- 2026-10-11：用户确认合并 UIEntity 入口修正和导表失败阻断，授权创建 change；当前仅规划。
- 已静态核对 EntityExtension、ExcelExporter.Luban、ExportAll、Share.Tool Loader 和 Editor 导表菜单的失败链路。
- Unity 编译、UIEntity 实际显示、工具构建、Check/导出及运行时测试：NotRun。
- 规划产物齐全，5 项实施任务均未开始。`openspec validate fix-foundation-entry-and-export-errors --strict`：Passed。
- 两个新 change 共 17 份 Markdown、48 个本地引用的存在性/编码/空白检查：Passed；工作区仅新增这两个 change，无已跟踪文件改动。不以格式通过证明实现或验收完成。
- 不创建额外 JSON 证据、状态副本或 hash 清单；后续原始输出保留原位。
