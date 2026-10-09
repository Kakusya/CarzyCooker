# Verification Report: deepen-foundation-specs

日期：2026-10-09。本变更仅完善规格和文档，以当前 main 源码/配置为静态依据；未修改运行时、技能或 AGENTS。扫描说明与逐字改动范围见 [扫描记录](../../../../Docs/AI/spec-scan.md)。

## Summary

| 维度 | 核对结果 |
|---|---|
| Completeness | 十四项 capability 的 delta 均存在且已同步；31 项 ADDED、1 项 MODIFIED，共 71 个增量场景；[tasks](tasks.md) 5/5 项完成，apply 即时结果核对为 all_done |
| Correctness | 增量要求和场景与 Sources 静态核对；原文保留与增量对应检查通过。动态 Scenario Coverage 为 Not verified：本轮未执行 Unity、故障注入、导表、Player/AOT 或联网测试 |
| Coherence | 保留已有 capability、原要求和场景；与设计的文档分层、授权及限制表达一致。Code Pattern Consistency 不适用：无新增或修改运行时代码，文档结构按既有规范核对 |

## 完成的检查

- 仓库文本枚举/读取：6878 条路径；2639 个源码/配置/文档文本，3627 个 Unity 序列化/元数据文本，610 个二进制或其他类型只记路径，2 个不可文本读取文件跳过；详见扫描记录的时间点及排除项。
- 主规格：十六份，要求由 43 增至 74，场景由 50 增至 120。
- 同步前后文本对照：除“ET 固定入口”的说明补充外，全部原 Requirement block 正文不变；全部原 Scenario 名及 Sources URL 保留。
- 增量对应：每个 ADDED/MODIFIED block 与同步后的主规格一致；未删除、重命名或新增 capability。
- 格式：`openspec validate --specs --strict --no-interactive` 16/16；`openspec validate --all --strict --no-interactive` 19/19（十六份主规格、三个进行中变更）。
- 本轮 41 个修改/新增文件的 367 个本地文件引用有效；包括未跟踪文件的 UTF-8、替换字符和行尾空白检查通过。没有运行时、技能、AGENTS、Excel 或包变更。
- Git：`git diff --check` 通过；扫描完成时尚未暂存、提交、推送或归档；后续收尾见下文。

## 源码与场景核对方式

每个 capability 在 [扫描映射](../../../../Docs/AI/spec-scan.md#契约映射) 中给出主要源码，其 delta 的 Sources 列出完整链路。以实现分支和调用消费者检查正常与异常场景；以下为关键抽查锚点：

| 维度 | 静态核对的实际边界 |
|---|---|
| 启动/资源 | EditorResourceMode、Package、在线路径；更新整体结果只有 true 才推进；预加载 await 顺序与 UNITY_ET/IL2CPP 条件 |
| owner/UI/Entity | Entity 下级先销毁；EntityRef 校验 InstanceId；UI 缺表/重复返回失败；GF Entity 缺表返回 null，ET 包装层再抛异常；等待器取消关闭/隐藏运行 ID |
| 生成/依赖 | 五个 ET 配置目标；失败仍可能复制/派生；协议先递增再赋 Opcode；程序集宏与 DotNet 链接范围 |
| RPC/HTTP/DB | 默认 RPC 没有 time 超时任务；取消是 ERR_Cancel 响应，超时与销毁是异常；HTTP 收尾没有统一业务错误协议；DB 单文档 upsert 不等于多文档事务 |
| 响应式/热更 | 首次 bind、合并多源、次数节流、Reset；Reload 保留 Model/View；AOT 返回码被 helper 忽略 |

新增 API 使用约定不声称存量每个调用者已符合；源码条件覆盖的静态核对，也不等同于通过动态时序测试。

## Issues

### CRITICAL

在已执行的静态检查中未发现阻止本轮文档交付的问题。动态测试未执行，不能据此宣称运行时无缺陷。

### WARNING

- 71 个增量场景的动态验证为 Not verified。本轮是文档任务，不新增产品测试或强行启动 Editor；后续修改对应行为时，优先验证加载中退出、RPC 超时/迟到、容器复用、资源失败和 Player/AOT 场景。
- 资源回调、字典切片/非事务写入、AOT 返回码四项现存限制已列入 [KnownIssues](../../../../KnownIssues.md)，没有修复；该记录不构成新修复任务授权。

### SUGGESTION

无额外工具或新增技能建议进入本轮实施范围。

## NotRun

工具/.NET 构建、Luban Check/导出、Proto 生成、ResourceCollection 导出、Unity 编译/Play、UTF 执行、Player/AOT、实际联网、DB/HTTP 运行及做饭玩法均未执行。既有接入和测试迁移的历史结果不改写为本轮运行结果。

## Assessment

本轮规格与文档的静态检查通过；动态 Scenario Coverage 未验证。扫描完成时保留 change 供用户审阅；用户后续授权收尾，归档情况见下文。没有 SHA 校验、新增 JSON 证据包、业务状态副本或原始日志复制。

## Archive follow-up

2026-10-09，用户审阅解释后授权归档收尾、提交与推送。5/5 任务完成，十四项增量的 32 个要求块与主规格逐字对应；归档前严格验证 19/19。主规格已同步，使用 `openspec archive deepen-foundation-specs --skip-specs --yes` 归档，不重复合并。归档后全量严格验证 18/18（十六份主规格与两个进行中变更）；41 个文档文件的 367 个本地引用、UTF-8/空白及 32 个增量要求块对应检查通过。动态验证仍为 NotRun / Not verified。

CLI 提示超过十个 delta 可考虑拆分；本次十四项均属于同一轮已完成的文档扫描，未拆分历史记录。另两个历史 change 保持原样。
