# Tasks

本项为业务规格捕获与文档收敛。用户已明确授权由 Agent 对照交接核对、定稿、建立长期产品规格入口并归档本项；已有决定不要求用户重新确认。以下任务不包含八个领域的业务实现，任务完成也不代表玩法验收；新增歧义保留待裁决，不同步为当前支持的主规格。

## 1. 身份与运行契约审阅

- [x] 1.1 审核 cooperative-match 与 authoritative-cooking-runtime，对照交接 A、G 和 design 的 ET 树，确认成员／连接、共同裁决、控制请求／业务 Tick 的边界；逐条处理反馈并保留明确正反场景，不为原型增加自动重连或凭证安全承诺。
- [x] 1.2 审核 product-session-bootstrap 的四份相关规划，核对 Participant 归 Match、HostPeer 不拥有成员、断线保留离线成员和显式 Leave；确认不存在多余“断线删除／保留”裁决门槛或会话与完整玩法范围混淆。

## 2. 玩法及进度规则审阅

- [x] 2.1 审核 restaurant-level-lifecycle、kitchen-item-processing、orders-front-service、fixed-companions、supply-layout-recipes，对照交接 C–F、H2；逐条确认跨关保留、守恒、服务收口与成长，候选数值不晋级，反馈修改同步涉及规格和产品说明。
- [x] 2.2 审核 save-progress-claims，对照交接 B，确认 Host 不等于存档 Owner、持久 ACK 不等于领取、原子消费和跨设备限制；授权安全与完整故障模型继续标为未设计，不以规格宣称协议可上线。

## 3. 文档集成与范围核对

- [x] 3.1 将审阅中真正裁决的内容同步相关规划及 Docs/Product；未裁决的暂停队列、断线手持物等继续保留来源冲突。验证设计索引、引用和状态一致，不为未回答问题选择默认分支。
- [x] 3.2 定稿后执行本项及受影响会话 change 的 OpenSpec 严格验证与有效路径检查，记录静态检查范围；代码编译、UTF、包体、实际联网及做菜验收为 NotRun，不用文档场景数量替代测试。

## Workflow follow-up

- 后续用户明确指定具体能力时，另建小范围实施 change 并加载 apply 所需技能；本项不包含代码、表、Proto、资源、工具或工作区任务。
- 八个领域的定稿目标交付到 Docs/Product/specs/，ET 归属及消息设计交付到 Docs/Product/architecture.md。保留未来设计、授权补足和未决身份；不提前同步到 openspec/specs/。归档仅表示文档整理任务已完成，不表示玩法已实现。
- 六项文档任务真正完成后勾选；严格检查和引用检查通过后按本轮授权归档，不提交、推送或启动服务。
