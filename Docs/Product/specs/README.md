# 做饭业务目标规格

八份规格已按两份交接完成文档核对与定稿，作为后续实施 change 的长期需求依据。**所有业务能力仍未实施，文档定稿不是玩法验收或开工授权。** 当前已支持的底座契约继续见 [OpenSpec 主规格](../../../openspec/README.md)。

## 规格与来源身份

| 规格 | 约束内容 | 来源身份 |
| --- | --- | --- |
| [cooperative-match](cooperative-match/spec.md) | Match、逻辑成员、连接及餐厅寿命 | 已确认；恢复凭证协议另案 |
| [authoritative-cooking-runtime](authoritative-cooking-runtime/spec.md) | 共同权威、固定 Tick、命令与同步 | 已确认及历史授权补足；非底座保证 |
| [restaurant-level-lifecycle](restaurant-level-lifecycle/spec.md) | 持续餐厅、自然结束、评分与跨关 | 已确认及授权补足；许可缩小 UX 可重审 |
| [kitchen-item-processing](kitchen-item-processing/spec.md) | 物件、容器、手工自动加工与守恒 | 已确认及授权补足；容量、时间未平衡 |
| [orders-front-service](orders-front-service/spec.md) | 顾客、订单、交付与餐具回收 | 自动基线及人工前厅授权补足；收口模式区分 |
| [fixed-companions](fixed-companions/spec.md) | 固定岗位、关内成长与玩家接手 | 身份及成长已确认；人工接手授权补足 |
| [supply-layout-recipes](supply-layout-recipes/spec.md) | 依赖图、物理采购及准备态布局 | 已确认及授权补足；菜单和设备目录仍为候选 |
| [save-progress-claims](save-progress-claims/spec.md) | 存档归属、授权分支与进度领取 | 产品语义已确认；安全及断电协议未设计 |

## ET 树与消息入口

详细归属及本地／远端发送、接收、答复见 [产品架构](../architecture.md)。最小会话规划见 [product-session-bootstrap](../../../openspec/changes/product-session-bootstrap/proposal.md)，只覆盖加入、名单和通信，不包含厨房、存档、自动重连或固定业务 Tick。

本地玩家走 ET 本地消息，远端消息由可信入口转交；两者共同裁决。Parent 表示释放责任，业务关联使用稳定身份。Match 持有 Participant，连接只绑定成员；Kitchen 与 Level 并列，Process 不归 Station 释放。

## 候选、待裁决与范围

- 暂停待执行队列、断线手持物仍存在来源冲突，具体实施前裁决。
- 历史自动收口和人工前厅自然排空分别适用，不将瞬间完成推广到所有模式。
- 关卡许可缩小保留旧物和已受理工作，但限制新生产；交互提示可另行重审。
- 87 菜、60 准备状态、72 物料、19 工位继续是 [候选目录](../catalog.md)，不转为正式配置或首发范围。
- 正式传输、人数、Tick 频率、耗时、价格、容量、产量及关卡内容仍未定。
- 授权签发、密钥、撤销、正常关闭认证、跨设备 UX、中心服务和完整断电模型仍待设计。
- 投掷、冲刺、烧焦、火灾、精细操作小游戏、复杂经济及伙伴维护槽不进入当前基础范围；后置事项不写成永久禁令。

完整清单见 [产品待办](../backlog.md)；未裁决不妨碍已明确规则的文档定稿，也不授权助手为后续实施选择默认方案。

## 维护与追溯

长期规则正文为本目录的八份规格；背景、例子见 [产品设计](../design.md)。目标规则变化须与相关实施 change 及产品说明保持一致，已归档文本保留交付当时状态，不持续维护第二份可变正文。

输入见 [设计交接](../../handoff/Cooking-设计交接-2026-10-09.md) 与 [禁止项交接](../../handoff/Cooking-禁止项交接-2026-10-09.md)。交接内旧仓库路径、工具、版本和历史实施授权仅作追溯，不恢复执行。

2026-10-11 文档整理 change 已完成六项任务并归档；本次来源核对、定稿补写和未运行范围见 [归档核对记录](../../../openspec/changes/archive/2026-10-11-capture-cooking-product-contracts/verification.md)。
