# Tasks

仅规划，全部 NotRun。先完成 cooperative-restaurant-core；拟补用例须在实施时实际发现为 PlayMode，不能用核心两菜结果代替前厅运行。

## 1. 前厅请求与状态

- [ ] 1.1 接入 ServeOrder／FrontWork 的外部白名单、独立请求复制和 Client 所属 Session 调用，内部 Companion／Submit 仍关闭；CoopFrontWire 用真实 KCP 覆盖正确交付一次、错菜／换客拒绝保物及答复关联，更新消息路径。
- [ ] 1.2 完成顾客／订单／工作／伙伴／餐具池同版投影与前厅 HUD 目标绑定；CoopFrontProjection 验证 DTO 回收后独立、当前角色就绪以及 UI 旧 callback 不提交，实际按钮请求只发一次，同步界面边界。

## 2. 认领与自然换关

- [ ] 2.1 适配两玩家与伙伴共享工作及失联收尾；CoopFrontWork 用真实请求验证竞争单执行者、停止／断线后接续、清桌洗碗原餐具守恒及公共 sender 可用，记录实际 owner／退出结果。
- [ ] 2.2 完成远端前厅暂停、自然收尾与当前关角色重建；CoopFrontLevel 验证最后洗碗前不 Completed、零分可继续、旧关原请求／高版本拒绝、重复 Continue 只一次及实物保留，同步联机关卡合同。

## 3. 普通入口连续营业

- [ ] 3.1 沿核心案的普通 Host＋同 Editor 真实 KCP 受管入口分工制作／交付两菜、暂停恢复、原碗回收并自然换关；观察双方新角色就绪和新关 Buff，逐功能记录并更新联机前厅进度；完整双 Editor 留原 loopback 案。
