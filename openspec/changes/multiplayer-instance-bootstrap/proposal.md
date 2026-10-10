# Proposal

## Why

一个运行实例需要明确选择 Host／Client 启动意图，现有 Launcher 和 ET 入口尚未提供该配置契约。先把单实例的配置、Play 交接与启动边界做清楚，使启动能力可以独立验证，不与第二 worktree 的建立和协作规程捆绑。

## What Changes

- 在既有稳定层增加不可变本轮启动配置，包含运行标识、实例标识、`legacy/host/client` 角色与 endpoint 意图；不包含开发／稳定分支职责、产品名、基线版本或自定义写目录。
- 提供本工程 Editor 启动入口，复用原 Launcher；显式配置无效时拒绝，普通 Launcher 不提交配置时保留旧行为，不继承草稿或上一轮角色。
- 修正既有 SceneHelper 的全局场景请求，使提交、消费与清理按工程管理；这属于启动请求的正确性，不负责建立第二个工程或协调代理。
- 处理跨进入 Play 的域重载、连续运行、忙碌拒绝和本轮状态收尾；不停止用户已有操作，不自动修改 Play 设置。
- 在产品启动能力尚未接入时，Host／Client 明确报告 ProductBootstrapUnavailable，不进入 Demo，不把配置准备完成当作产品 Ready；退出只停止本轮实际启动的入口。

## Capabilities

### New Capabilities

- `runtime-instances`: 单实例的显式启动配置、Editor 请求交接、工程范围的场景请求、状态与退出契约。

### Modified Capabilities

- `startup`: 普通／legacy 保持 ET 固定入口；显式角色配置失效或产品能力缺失时拒绝旧 Demo，只收尾本轮入口。

## Impact

- 候选改动仅涉及 Game 稳定层、Game.Editor 的启动窗口、LauncherSceneToolBar／SceneHelper 和 ProcedureET；复用现有程序集、UTF 和 Unity CLI／Pipeline。
- 验收以本工程单 Editor 的配置、生命周期和拒绝行为为主，不以长期第二 worktree 或双 Editor 联调为本项完成前置；这些结果不证明产品网络或数据隔离。
- 本项不修改 AGENTS、ProductName、GF 写路径、目录锁或 Editor 进程日志，不创建 worktree、代理会话、协作协议、基线构建或多实例编排工具。
- 第二 worktree 的固定分支、独立缓存、ProductName／真实数据路径、原生日志、AGENTS 职责、Orca 消息与基线更新归已建立的 [multiplayer-peer-workspace](../multiplayer-peer-workspace/proposal.md)，独立规划和验收。
- 产品 ET owner 树、本地消息及最小 Host／Client 会话见独立 [product-session-bootstrap](../product-session-bootstrap/proposal.md)；配置 Prepared 与产品就绪状态分开。固定 Player 构建与多人回归范围后续逐项讨论，不在本项预先展开。
- 当前只修订五份规划文件，所有实现任务尚未执行。
