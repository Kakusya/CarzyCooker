# 迁移验证记录

## 完整性

四组任务对应工具/技能、配套 SOP、路由/来源、整体校验。入口包含 21 项可发现技能：12 项官方 OpenSpec、4 项既有项目技能、官方 unity-cli 和 4 项 SOP 技能。新增 SOP 正文统一在 Book/references，根入口和技能只作路由。

## 正确性与场景映射

| 契约 / 场景 | 验证依据 | 结果 |
|---|---|---|
| 精确工程 / 显式范围 | command_result、命令发现 endpoint 校验、CLI --run/--filter、单写者锁；wrong-project、discovery、lock 回归 | Passed |
| Pipeline / 原生 UTF 繁忙拒绝 | ready、只读 native_tests_active；busy/native_busy 回归；真实 active=false | Passed（模拟繁忙，真实只读空闲） |
| 失败消息与堆栈 | verdict、结果输出；Failed/Inconclusive 与压缩器回归 | Passed（工具回归） |
| 空集 / 跳过 | select_tests 与 verdict；空集/全跳过/零终态回归；真实两模式均 0 | Passed，实际产品执行 NotRun |
| 域重载暂不可达 / 超时 | poll 有限轮询，不重发/取消；transient/timeout 回归 | Passed（模拟），真实产品域重载 NotRun |
| 原结果保留 / Console 完整性 | 不写结果工件；同会话游标、全级别、seq 连续、groundTruth 增量；reset/dropped/tail/null 回归 | Passed（工具回归） |
| 终态范围 | fullname 与发现的选定名单一致性检查及 unrelated-terminal 回归 | Passed |
| SOP 可路由 / 来源可审阅 | AGENTS 路由、来源表与本轮独立逐字 diff；本地链接检查 | Passed |

## 一致性与检查范围

- Python 标准库回归 `python -m unittest discover -s Tools/AutoTesting/tests -v`：27 项 Passed、exit 0；其中参考 reducer 回归原件也被执行。
- 独立只读复核发现并修复四项边界问题，复核通过。没有创建新 worktree，没有并发编辑文件或启动产品测试。
- `quick_validate.py` 缺少 PyYAML，未成功运行；不安装第三方依赖。改用已有 OpenSpec 的 Node YAML 库检查 21 项 frontmatter/name/description、OpenSpec 生成版本及 config YAML。
- 文档/源码引用检查与 `git diff --check` 通过；两份参考压缩器直接文本比较一致，无 hash。
- `openspec validate --all --strict --no-interactive`：18 项 Passed（16 主规格 + 2 活跃 change），已同步 automated-testing 与 ai-collaboration 增量。
- 真实 Editor 6000.3.18f1/Pipeline 0.8.0-exp.1：ready、Play stopped、编译失败 false；EditMode/PlayMode 均 0 条，原生 UTF active=false。历史 Console 4 条错误保留，未清除，不声称零历史错误。

## 限制与 NotRun

没有实际产品 UTF 测试，因此真实成功/失败 PlayMode 全链路、产品域重载和行为验收 NotRun；模拟覆盖不能替代这部分。不声明“可归档即全部产品验证通过”。

产品构建、导表执行、资源刷新、AOT、联网和做饭玩法均 NotRun。专用双包编排、PortRegistry/租约、UI runner 等未接入，SOP 已明确使用现有入口与缺口。UTF 内部注册表改变时只读查询受控失败；Pipeline 无稳定作业 ID，同工程须保持单写者。

没有 JSON 证据包、状态副本、SHA 校验、提交或推送。当前 change 留待用户审阅，不自动归档。
