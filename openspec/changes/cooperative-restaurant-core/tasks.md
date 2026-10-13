# Tasks

仅规划，全部 NotRun。前置为已交付 Solo 餐厅；本案不等待 Solo 供应／装修／检查点。以下 UTF 名为拟补范围，实施时先发现真实 PlayMode 用例；功能需实际运行后才能勾选。

## 1. 正式 Host 与共同入口

- [ ] 1.1 将正式 Host 接到 RestaurantRuntime／Level，保留 legacy 驱动排他并迁移旧直接 owner 查询；用 CoopCoreOwnership 验证唯一厨房、单步计时、Solo 无监听和 Client 无权威，同步实际树说明。
- [ ] 1.2 接入核心动作与 Level／Layout／Buff 的显式 wire 白名单及所属 Session 值调用，必要协议正常生成；CoopCoreWire 用真实 KCP 验证请求／回执关联、未知与内部消息／旧协议拒绝和本地仍走原消息路径，更新收发说明。
- [ ] 1.3 校验当前 Host 本地 Peer 的共享控制资格并接入相应按钮状态；CoopCorePermissions 参数用例覆盖开店、暂停／恢复、Continue、布局、Buff 的合法 Host 与远端越权零改动，记录权限结果。

## 2. 两菜、投影与生命周期

- [ ] 2.1 接通两菜实际制作、交接与剩余份数；CoopCoreCooking 通过真实本地＋KCP 命令完成汤和吐司，断言锁定／未到位拒绝、重复不产出和末份争抢，保留 Solo 未变配方证据。
- [ ] 2.2 接入完整既有 DTO 复制、当前关本人角色就绪和核心 HUD；CoopCoreProjection 验证 Join 前通知不开输入、回收后投影独立、旧关高版本拒绝及受管重开旧回调无效，更新 UI 边界。
- [ ] 2.3 完成成员退出和本轮停止适配；CoopCoreExit 断言手持落物一次、自动批次继续／手工可接续、旧绑定拒绝和公共 sender 存活，整轮重开无旧结果，记录实际收尾路径。

## 3. 实际核心制作入口

- [ ] 3.1 按核心 design 的同 Editor 普通 Host＋独立 Fiber 真实 KCP 方式，使用两端所属 HUD／场景完成两菜、Host 布局／Buff 与暂停恢复；逐功能记运行结果并更新单机／联机进度，不将该证据称为双 Editor 完整餐厅。

## Workflow follow-up

- 完成本案后进入 cooperative-front-service；三案齐备后才执行原 loopback 双 Editor 验收。
- 本次只获规划授权；实现另由 apply 启动，提交／同步／归档不在本次范围。
