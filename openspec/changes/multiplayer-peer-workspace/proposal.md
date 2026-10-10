# Proposal

## Why

频繁打包会拖慢两端联调，而第二个源码目录若直接复用主线的应用身份、日志和协作规则，又会造成文件争用或误操作。需要一个可保持稳定版本的完整第二 worktree，以及清楚的运行、通信和更新边界。

## What Changes

- 建立同仓固定分支的稳定对端工作区规程，保留完整 Unity 与根目录工具依赖，各自维护 Library、Temp、Logs 和 UserSettings；版本只按明确维护请求更新。
- 稳定分支使用不同的固定 ProductName；在运行前核对实际设置文件、AssetSet 和 Resource 写路径，分别配置 Editor 原生进程日志，避免把产品名当成全部隔离保证。
- 主线与稳定分支的 AGENTS 保留共同规范，增加各自职责片段：主线负责开发与基线决策，稳定端默认只运行、观察和回报；任一端均可选择 Host 或 Client。
- 复用现有 Orca 终端消息进行请求、收件确认和结果回报，区分发送入队、实际读取、开始执行与完成，不新增自动编排服务。
- 用 Unity CLI／Pipeline 按完整 projectPath 精确定位两个 Editor，忙碌、重复、过期或目标错误时拒绝，不干扰用户已有操作。
- 明确基线升级保留分支身份、产品名和职责差异，以及只释放自身资源的失败／收尾规程。

## Capabilities

### New Capabilities

- `multiplayer-peer-workspaces`: 稳定对端工作区的源码基线、独立缓存、应用数据和进程日志隔离、实例定位及维护契约。

### Modified Capabilities

- `ai-collaboration`: 增加主线／稳定对端职责和当前活跃终端间协作契约；保留已有授权、工具和证据规则。

## Impact

- 候选修改为主线与稳定分支各自的 AGENTS 职责片段、稳定分支的 Unity ProjectSettings，以及一份共用协作正文与既有 Unity 启动用法链接；不新增业务代码、自动化脚本、技能或服务。
- 本项只维护第二端环境与协作；角色配置和 Play 交接见 [multiplayer-instance-bootstrap](../multiplayer-instance-bootstrap/proposal.md)，产品 ET owner 树与会话见 [product-session-bootstrap](../product-session-bootstrap/proposal.md)。
- 环境与终端通信可独立验收。使用角色入口的双 Editor 验收要求第一项在两端均可用；产品网络结果还要求产品会话能力，不因目录和消息通过就宣称联机通过。
- 不使用 Unity 子仓或固定 Player 包，不修改 BuildPKG；不共享活动 Library，不自动拉取／更新基线，不新增目录锁、写根重定向、端口注册表或持久消息编排。
- 本项尚未实施，不创建长期 worktree、不修改 ProjectSettings、不启动第二 Editor；当前用户单独授权补充主线 AGENTS 的 ET 设计约定，不代表两端协作职责片段已落实。实施与实际工作区建立须按后续明确授权执行，此前临时通信探针授权不延伸为永久工作区授权。
