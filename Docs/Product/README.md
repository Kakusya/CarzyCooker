# 做饭产品目标与资料状态

保存产品目标设计，当前能力按[分开的单机／联机进度](../../CarzyCooker当前进度.md)和实施 change 区分。

当前可运行范围统一查[根进度](../../CarzyCooker当前进度.md)和各案原始运行记录。此前入口中“供应实施中、装修与存档仅规划”的表述已过期；相关交付见[供应](../../openspec/changes/solo-supply-delivery/execution-notes.md)、[装修](../../openspec/changes/solo-interlevel-renovation/execution-notes.md)、[成功检查点](../../openspec/changes/solo-success-checkpoint/execution-notes.md)。[合作核心](../../openspec/changes/cooperative-restaurant-core/execution-notes.md)、前厅与清理供应均已验收；普通双 Editor 两种角色排列完整合作3/3通过，见[集成记录](../../openspec/changes/cooperative-restaurant-loopback/execution-notes.md)。Player/AOT、跨设备 LAN 与公网仍未验收。

本次后续规划见[由浅到深的 change 路线](change-roadmap.md)。路线覆盖工程整理、UI/菜谱、三关经营、内容扩展和合作，真机及独立包体验收放在最后；页面不授予实施或设备操作权限。

| 文件 | 内容 |
|---|---|
| [产品设计](design.md) | 保留适用原文的产品、玩法、经营语义 |
| [候选目录](catalog.md) | 87 菜、60 准备状态、72 供应物料、19 工位；非正式配置 |
| [产品边界](boundaries.md) | 新设计适用范围 |
| [待办与待裁决](backlog.md) | 冲突、选型、未完成协议；非实施授权 |
| [由浅到深的 change 路线](change-roadmap.md) | 14 个新规划案与已有消息/合作案的依赖、模式边界和最终设备层 |
| [业务目标规格](specs/README.md) | 八个领域目标契约已定稿；备料子集已实施，完整业务、候选和未决身份分别保留 |
| [ET 业务目标架构](architecture.md) | 释放归属树、本地及远端消息、共用裁决与最小会话边界；未实施 |
| [设计交接](../handoff/Cooking-设计交接-2026-10-09.md)、[禁止项交接](../handoff/Cooking-禁止项交接-2026-10-09.md) | 用户本轮指定的业务输入；历史路径与授权不恢复执行 |
| [原始设计](../../Book/业务逻辑.txt.txt) | 保留原件，只用于追溯 |
| [原始禁止事项](../../Book/禁止事项.txt.txt) | 保留原件，只用于追溯 |

## 四种状态

- 已确认：原资料标记「明确确认」的产品共识；不声称重新检索到每轮原话。
- 授权补足：历史授权细化设计；旧工程实施授权不延续到本项目。
- 候选：目录/方案/未平衡数值，不是逐条批准或已实现。
- 待裁决：来源冲突或未知项，实施前取得决定，助手不代定。

正式文档仅用本项目有效引用与原件章节。原件内历史路径、失效链接、测试入口和工作树记录不参与现行路由；保留原件不恢复旧工具、任务或专属限制。

文档定稿与实现状态分开：`specs/` 为长期目标契约正文，相关实施 change 按具体授权推进；规格整理 change 的归档只表示文档交付，不将这八个领域列为当前已支持能力。
