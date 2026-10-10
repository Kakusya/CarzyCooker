# Proposal

## Why

两份业务交接资料已经定义了成员、餐厅、做菜和领取进度的关系，但目前 OpenSpec 主规格主要覆盖底座，业务共识尚未形成可逐项审核的契约。先捕获目标规格，防止最小联机会话误把连接当玩家、把小关当餐厅，或者把历史实现授权当作当前开工授权。

## What Changes

- 从 [设计交接](../../../../Docs/handoff/Cooking-设计交接-2026-10-09.md) 和 [禁止项交接](../../../../Docs/handoff/Cooking-禁止项交接-2026-10-09.md) 捕获八个业务领域的目标契约和正反场景，保留来源章节与状态。
- 明确 Match 持有逻辑 Participant；Connection 只绑定成员，断线结束绑定、保留逻辑成员。Kitchen 与 Level 并列，Process 不随 Station 销毁。
- 写清 Host 本地消息和远端消息如何进入共同权威裁决，以及固定业务 Tick、投影、请求结果、已提交事件和传输 ACK 的区别。
- 将存档 Owner、世界 Host、成功结果与领取凭证分离，记录跨关保留、一次性消费和平台边界。
- 按已提供资料修正 `product-session-bootstrap` 草案中的临时成员生命周期，撤销多余的断线删除／保留裁决门槛；保持其最小通信范围。
- 候选菜单、正式平衡、暂停队列、断线手持物、安全协议和正式传输保持原状态，不以规格收录替代用户裁决。
- 按本轮收尾授权，将定稿的八份目标规格交付到 `Docs/Product/specs/`，ET 树和消息设计交付到 `Docs/Product/architecture.md`；完成静态核对后归档本项，长期入口不依赖活跃 change。

## Capabilities

### New Capabilities

- `cooperative-match`: Host／Client／Participant／Profile 身份、协作与连接归属。
- `authoritative-cooking-runtime`: 本地及远端消息、固定 Tick、命令提交、同步与退出。
- `restaurant-level-lifecycle`: 持续餐厅、大小关、自然成功、评分与跨关承接。
- `kitchen-item-processing`: 实物、单槽、空间交互、手工／自动加工、批次守恒。
- `orders-front-service`: 顾客、订单、交付、餐具与前厅自然收口。
- `fixed-companions`: 固定伙伴、关内成长与人工接手。
- `supply-layout-recipes`: 采购实物、准备态布局与配方依赖图。
- `save-progress-claims`: 平台隔离、授权分支、成功检查点与共同进度领取。

### Modified Capabilities

无底座主规格变更。已有 `product-session` 是另一个进行中 change 的新增能力，其草案修订留在该 change 内，不在这里重复声明。

## Impact

本项交付是规格捕获、文档定稿和规划校准，所有业务能力仍未实施。修改本项规划文件、相关会话引用以及 Docs/Product 的目标规格、架构与索引；不修改代码、表、Proto、资源、构建配置或主规格，不运行 Editor／服务／测试，不建立工作区或恢复旧工程。归档表示文档整理交付完成，不表示业务完成。

后续业务落在现有 Game.ET.Code.Model／Hotfix 的适用层，表现使用 ModelView／HotfixView；沿用既有程序集和生成流程。八份规格是后续分阶段 change 的约束，不是一次实现完整餐厅的任务包。现有启动项、第二工作区、本地请求退出仍按各自授权和 tasks 推进。
