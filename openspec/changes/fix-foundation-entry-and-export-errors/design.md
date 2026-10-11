# Design

## Context

动机见 [proposal.md](proposal.md)。当前差异已在 [KnownIssues](../../../KnownIssues.md) 记录，不属于已修复能力：

- [EntityExtension](../../../Unity/Assets/Scripts/Game/Entity/EntityExtension.cs) 的同步泛型 ShowUIEntity 转给普通 ShowEntity；Type 入口已查询 DTUIEntity。
- [Luban 导出](../../../Share/Tool/ExcelExporter/ExcelExporter.Luban.cs) 未用子进程退出码判定结果，失败汇总后仍复制并派生 ID。[ExportAll](../../../Share/Tool/ExcelExporter/ExcelExporter.cs) 随后调用本地化。
- [CLI](../../../Share/Tool/Loader/Init.cs) 正常返回 0；[ShellTool](../../../Unity/Assets/Scripts/Game/Editor/Tool/ShellTool.cs) 不返回失败结果，[EditorTool](../../../Unity/Assets/Scripts/Game/Editor/Tool/EditorTool.cs) 在 await 后继续刷新。
- 当前 [ET 配置](../../../Design/Excel/ET/luban.conf) active=true，保留 gameclient/gameeditor/client/clientserver/editor 五目标。第一输出可以已经是正式生成目录，失败不能承诺无任何文件变化。

## Goals / Non-Goals

**Goals:** 修正调用路由，贯通子进程、导出编排、CLI 和 Editor 的失败结果；失败阻断后续写入且保留原因。

**Non-Goals:** 不改表或生成物，不重构 ID 开关，不建立临时发布区/整批事务回滚，不重写通用 Shell 工具的全部调用者；异步任务生命周期由另一个 change 处理。

## Decisions

### 1. 复用已有 UIEntity Type 入口

泛型入口委托正确的 Type 入口，保留类型约束、运行 serial ID、userData 和缺行警告/null 语义。两张表存在同 ID 也只选 UIEntity，缺行不回落普通表。不新增第二套解析函数，避免以后两入口再次偏离。

### 2. 所有 Luban 子命令结束并读完输出后统一判断

保留并行执行。子进程非零、明确错误输出或启动/执行异常均为失败；等待 stdout/stderr 收尾，防止只在 WaitForExit 后遗漏最后的错误。汇总每条命令结果，不依赖一个成功日志或无错误文本。

全部命令成功后才执行复制、清理目标、四类 UGF ID 派生及 ExportAll 后续本地化。失败以带工程/目标和原因的异常传到现有 CLI catch，使它返回非零；本地化自身异常也沿同一路径退出。已经启动的其他子命令正常收尾，不强杀进程。复制或派生本身失败同样阻断后续步骤，不把已写文件回滚。

### 3. Editor 接收结构化失败

使 ShellTool.RunAsync 对启动异常和非零退出向等待方报告失败；签名优先保持原 UniTask，失败抛出而非吞掉。仅调整导表两个菜单的消费：失败日志保留原因，结束当前菜单流程，不执行 AssetDatabase.Refresh、缓存清理和选择对象恢复。正常路径保留现有刷新行为。其他通用 Shell 消费者按签名兼容核对，运行行为不随本项扩大改造。

相比只修 CLI，这一步才能避免 Editor 继续执行成功路径。相比全局修改 Shell 成功/失败 UX，本项只处理实际导表消费者。

### 4. 输出边界与两 change 交接

Check 保持不写输出；非 Check 的子命令可能先写源输出，失败只保证后续步骤不开始。成功时保留现有复制清理规则，手写文件不放进生成目标。第一个 change 的 entity delta 只改“UIEntity 独立路由”；第二项新增 view 所属异步契约，没有同一 Requirement 的覆盖冲突，代码实施宜先完成本项。

这是纯工具/配置交接，不创建 ET 业务节点、SceneType、网络消息或新 owner 树。

## Risks / Trade-offs

- [失败仍可能留下源输出] → 报告失败和受影响路径，停止后续复制/派生；不以文件存在判断有效。
- [通用 Shell 调用方此前依赖吞错] → 保持签名，检查实际消费点，异常如实报告；不加入重试或补偿平台。
- [复制目标包含用户文件] → 仅在隔离目录制造失败，不改真实 Excel 或运行真实破坏性复制来证明阻断。

## Migration Plan

局部修改源码，按 [tasks.md](tasks.md) 完成必要验证，再同步 Book 的 Entity/Luban 用法、生成参考与 KnownIssues。实施前加载 Entity、Luban 与适用验证技能。若需撤销实现，仅撤销本 change 的授权改动，保留用户现场；不自动 reset、清输出、提交或推送。
