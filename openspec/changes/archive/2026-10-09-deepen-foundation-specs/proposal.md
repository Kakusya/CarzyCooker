# Proposal

## Why

现有十六份主规格已提供模块基线，但多数底座要求只有概括场景，资源流程、异步退出、RPC 失败和响应式刷新等行为缺少可审阅的边界。用户已授权继续扫描并完善规格，本变更将源码支持的现状写成具体契约，供后续修改与验证使用。

## What Changes

- 枚举当前仓库并读取适用文本，深入启动、资源、ET owner、UI/Entity、生成、网络与热更等链路。
- 为十四项底座 capability 增补正常、拒绝、取消、清理及模式差异场景；保留已有要求，仅修正与实际条件不符的表述。
- 区分已有机制、已知限制与尚未验证的时序，不把扫描发现的缺陷写成应长期保持的行为。
- 更新规格索引、模块扫描记录及已滞后的当前进度说明，保持用法文档和规格职责分离。
- 不改变运行时行为，不实现做饭玩法，不增加工具、技能或依赖。

## Capabilities

### New Capabilities

无。复用现有 capability 边界。

### Modified Capabilities

- `startup`：资源模式分支、预加载顺序、ET 宏前提及退出。
- `runtime-foundation`：ET Entity 树销毁与跨 await 的实例身份。
- `ui`：单实例拒绝、等待取消与 Widget 托管状态。
- `entity`：配置缺失、异步取消、GF view 回调及 owner 关系。
- `resource-management`：容器清理、回调资源归属与资源更新终态。
- `data-generation`：五个 ET 配置目标、多输出复制及失败边界。
- `proto-generation`：当前生成域和输出，以及排序对协议编号的影响。
- `localization`：语言切换与解析失败的非事务边界。
- `network`：RPC 超时、迟到响应与不同终态语义。
- `server`：DB 操作与 HTTP 生命周期的现有职责。
- `hot-reload`：程序集装载模式、重载和 AOT 验证界限。
- `reactive-ui`：首次投影、节流、版本集合和重置语义。
- `editor-tools`：骨架生成的拒绝、部分输出及后续接入责任。
- `dependencies`：程序集与服务端链接约束，以及依赖声明的验证界限。

## Impact

改动仅限 OpenSpec 规划产物、十四份主规格和直接相关 Markdown 文档。`automated-testing` 与 `ai-collaboration` 保持现有契约；`AGENTS.md`、技能、源码、Excel、包、场景和资源不需要修改。格式、引用、增量对应关系与源码抽查采用静态验证；产品构建、导表执行、Unity/AOT、玩法和实际联网均为 NotRun。保留进行中的两个已有 change；本轮不提交、推送或自动归档。
