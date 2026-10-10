# Tasks

全部任务尚未实施。本轮只创建规划，不运行 Editor、编译或测试。实现使用原本地消息 owner，不引入产品 Scene、网络桥接或其他工具。

## 1. 停止入口与等待者结清

- [ ] 1.1 在 Unity/Assets/Tests/Editor/ET 建立非生产消息夹具与独占的 ET 测试环境，复用 UTF，仅新增必要 Editor asmdef；列清 Fiber.Root／sender／接收 Actor 的 owner 树、实际 ActorId 与 MailBox／Handler／答复路径，验证测试可发现且通过真实 Call／队列／Dispose 工作并在 finally 收尾，不替换用户活跃 World。
- [ ] 1.2 在原 ProcessInnerSender/System 实现幂等 Stop、销毁兜底与新 Call／Send 拒绝，利用现有名单先清空／撤下旧入口再完成等待；必要时捕获响应类型元数据，验证多等待者、两种 needException 结果以及计时器／单例已退出仍能结清。
- [ ] 1.3 补齐正常答复、退出先到、答复先到、重复 Stop／Dispose、通知中再次停止／重建和消息批次中退出的回归；核对每个请求只完成一次，旧收尾不删新队列，原成功／业务错误行为保持。
- [ ] 1.4 更新 references/reactive-and-network.md 的本地 Stop／Destroy 与 ERR_Cancel 消费说明，明确仅停止当前发送器、结束等待不撤销远端执行；核对使用说明与增量规格及实测结果一致。

## 2. 旧任务与重建实例隔离

- [ ] 2.1 修订本地 RpcId 分配为当前托管域内跨发送器重建不重复且线程安全，耗尽拒绝不回绕；超时／等待返回及批次处理使用既有实例身份核对，验证旧计时恢复和旧答复入新队列均不改变新请求，不新增协议字段或回调表。
- [ ] 2.2 补齐正常超时两种模式、相同 Fiber 标识重建、对象复用及编号耗尽回归，使用可控测试时间驱动真实 Timer；同步 references/reactive-and-network.md 与 Docs/AI/module-index.md 的隔离范围，核对未降低生产超时、未承诺跨域恢复。

## 3. 编译与集成验收

- [ ] 3.1 在获准验证时，按精确 projectPath 核对本工程 Pipeline，执行 recompile 并确认状态及 Console groundTruth；覆盖 ET.Core 和测试程序集，拒绝忙碌现场，缺环境记 NotRun 并保持相关实测未完成。
- [ ] 3.2 核对 DotNet.Core 对同源框架文件的编译，确认改动没有增加 Unity-only 依赖；按实际依赖报告成功／失败，缺依赖记 NotRun，不启动服务、不用整包构建替代本项验证。
- [ ] 3.3 按明确授权执行本项实际发现的 UTF 集合，核对终态、失败和原始输出；完成四份规划／用法的引用、空白与 OpenSpec 严格检查，不把本地回归解释为双 Editor 或产品联机通过。

## Workflow follow-up

- 规划审核后按明确请求进入 openspec-apply-change；本轮不实施、不修改主规格、不归档。
- 实现和必要实测完成后再按项目收尾流程同步 network 增量并归档，未运行不能以任务勾选冒充验证。
- 本项可独立于 multiplayer-instance-bootstrap 与 multiplayer-peer-workspace 实施。[product-session-bootstrap](../product-session-bootstrap/proposal.md) 复用本地消息收尾能力，其业务树、单成员退出和网络桥接不并入本项；不自动实施该 change。
