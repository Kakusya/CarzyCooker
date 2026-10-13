# Proposal

## Why

当前玩家入口已大量使用 ET 本地请求，但加工到点、顾客流转、清桌接洗碗、自然结算等仍依赖直接方法调用和快照中的状态变化。用户确认将所有产品业务的入口、终态和功能间衔接统一为明确的本地消息，使功能之间能从发送端追到接收端，并区分请求接受、开始成功与真正完成。

## What Changes

- 将“所有功能”限定为当前产品业务：会话加入／离开、移动与物品操作、手工／自动加工、顾客订单、玩家／伙伴工作、出餐清洗、设施清理、供应、关卡控制与自然结算、布局、Buff、装修、已有单机存档入口。逐项清单见 [design.md](design.md)。
- **BREAKING**：产品功能间的启动、结束及后继触发必须经现有 `ProcessInnerSender → MessageQueue → MailBox → Handler`；收窄可绕过入口的业务 API。新增内部消息默认不进入 wire 白名单，协议源正常生成，协议版本在实施时按最终基线递增。
- 已有请求继续快速答复；补齐明确的动作结果和长作业完成／拒绝／取消通知。自动流程也有具名入口与出口，不要求玩家点击才能产生消息。
- 保留一个业务时钟、原 ET owner、原子领域提交与独立快照。功能内部计算和进度推进可直接调用；后继功能通过消息进入后续允许的业务步，不递归执行整条业务链。
- 快照安装后仍可发布 ET 观察事件刷新表现。事件、Invoke 和 RPC 各守原职责，不用事件直接触发另一项产品业务。
- 补齐新增消息必经的未投递／目标退出／撤队列收尾，复用现有等待表和所属异步机制，不建设另一套总线、可靠消息平台或工具。
- 本次只创建规划产物。框架建树／强制拆树、UI 与资源生命周期、纯计算、旧 Demo、服务端示例和编辑器工具不做普遍消息化；不增加玩法、监听、联网白名单、持久功能或平衡配置。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `product-session`：统一产品功能入口、具名终态、自动衔接、提交顺序与消息验证契约，补全原有接收／完成分离要求。
- `network`：明确本地消息未交付、接收目标退出和发送器停止时的输入所有权，补足现有请求／通知收尾契约。

## Impact

主要修改 `ET/Code/Model/Share/Product`、`Hotfix/Share/Product`、现有 `HotfixView/Client/Product` 调用与消费、`Design/Proto/ET-Client` 和 `CarzyCooker.ET.Tests`。必要公共修正仅涉及现有 ProcessInnerSender、MessageQueue 与 Ordered MailBox 的丢弃路径；不新建程序集、调度器或私有 RPC 映射，不修改 Excel 平衡和 UI 结构。

2026-10-13 后续规划核对时源码协议为 17，实施仍以届时实际版本递增；摘要文档中的旧版本不是实施基线。供应、清理、装修与 `solo-success-checkpoint` 已有原案验收，`cooperative-restaurant-core` 已接通 Host/Solo 共同 Restaurant 与真实 KCP 两菜核心。前厅合作等继续由原案推进；本案迁移已有消息边界，不代替它们开发或冒领运行结果。既有未提交修改由原任务保留，实施顺序见设计及[整体路线](../../../Docs/Product/change-roadmap.md)。

主要风险是拆开功能衔接后漏发／重复终态、误判自然收尾以及旧实例消息写回。以功能清单对应的最小 PlayMode UTF、关键生命周期测试及普通 Launcher 全流程验收控制风险；未改的纯配置证据可复用，改过的消息行为必须重跑。严谨程度与未验证边界见 [execution-notes.md](execution-notes.md)。
