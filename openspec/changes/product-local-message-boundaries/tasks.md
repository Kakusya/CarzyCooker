# Tasks

当前仅完成规划。以下全部为待实施任务；已有实现或历史通过不代表本案已验收。每项完成时在 execution-notes 记录实际 FullName／普通操作与结果；新增或修改的消息路径必须运行。UI 接入阶段加载 carzycooker-ui，UTF 阶段加载 unity-cli／auto-testing-sop；本案不改 Excel 或 UI 结构。

## 1. 消息合同与必要收尾

- [ ] 1.1 在现有协议源追加动作终态与领域转换／完成消息族，补原 Model 的动作／作业阶段身份，按最新基线正常生成并升级协议；核对 Client／ClientServer 的旧 Opcode 保持、内部消息不在 wire 白名单、编译通过，并以真实队列 UTF 证明明确类型能到正确 SceneType／handler，同步 design 的最终消息名称。
- [ ] 1.2 修正现有 sender／queue／Ordered invoker 的未交付、缺邮箱、等锁退出和撤队列输入收尾，明确交接点；补并运行代表性 LocalMessageOwnership UTF，断言输入及晚到响应只释放一次、失效目标不写新实例、原 Stop 及时结束等待，同步 references/framework-lifecycle.md。
- [ ] 1.3 接入单一终态发送点和独立值消费，保留 GridCommandResult 路径，其他动作不再只依赖 Peer 最近快照字段；真实消息 UTF 验证先快照后结果、两条动作各有终态、重复不双发、DTO 回收后值有效，同步 references/reactive-and-network.md。

## 2. 厨房与供应边界

- [ ] 2.1 迁移移动、拿放／掉落拾取、装容器、倒入和分汤的完整入口与结果，收窄直接跨功能提交 API；扩展原 Grid／Solo 行为 UTF，经消息验证各动作成功、适用拒绝及移动替换取消，断言物品身份／份数守恒并记录逐动作覆盖。
- [ ] 2.2 接入手工切配／手持加工的开始、停手、接续及完成转换，保留原进度 owner；扩展备料 UTF 验证到点但未投递时无成品、投递后完成一次、停手接续及暂停不重置，记录实际源／目标／结果消息。
- [ ] 2.3 接入煮汤与烘烤的开始和完成消息，区分世界作业与关内命令；扩展 SoloDish／汤加工 UTF 验证开始不等于完成、暂停到点转换、原锅跨关合法完成、重复完成不复制成品，更新对应本地消息约定。
- [ ] 2.4 补齐 FillWater／DrainLiquid／DiscardSolid 终点，迁移采购／货物到点／接收／拆份消息衔接；运行 SoloCleanup／SoloSupply 的受影响用例及新增消息断言，覆盖三种清理、四段供应、到货暂缓投递、余额一次扣减与货物跨关，同步供应消息说明。

## 3. 前厅自动流程

- [ ] 3.1 将顾客到达、排队入座、到座创建询问、订单创建／到期、用餐结束和离场改为明确请求与完成衔接；扩展 SoloFrontFlow UTF 逐阶段控制消息泵，验证队满跳过、实际行走、询问后开单及到期离场，不以直接状态赋值模拟完成，记录阶段路径。
- [ ] 3.2 统一玩家／伙伴的认领、到位开始、停手接续和作业完成消息，保持已有 CompanionWork 路由及第三次成长原子提交；运行 SoloCompanionWork／SoloCompanion 小集，覆盖单执行者、迟到旧认领、半程接续、完成防重和后续洗碗耗时冻结，同步伙伴消息约定。
- [ ] 3.3 迁移出餐终态、顾客离场→清桌→洗碗的后继请求，原领域阶段承担待衔接事实；扩展 SoloServe／SoloCustomer／SoloFrontFlow UTF，验证一次计分、原碗回收和每个投递间隙均不提前自然结算，更新自然收尾说明。

## 4. 关卡、选择与存档边界

- [ ] 4.1 接入开店／暂停／恢复／Continue 终态、营业到点与自然完成消息；扩展 SoloLevel UTF，验证控制在暂停／Completed 可处理、自动转换冻结、未执行动作取消、新关就绪门槛和旧关消息隔离，并更新关卡调用说明。
- [ ] 4.2 补齐布局结果与 Buff 准备／选择消息，保持原布局原子校验、候选和选择一次性；运行 SoloStationLayout／SoloBuff 相关 UTF，验证准备未就绪不能开店、选择和布局成功／拒绝各有终点、旧按钮失效，同步对应消息清单。
- [ ] 4.3 将结算准备装修候选、选择及换关安装纳入消息合同；运行 SoloRenovation 小集，验证空候选、重复选择、合法安装一次及失败零安装，保持 Continue 内原子应用并更新装修消息说明。
- [ ] 4.4 在 solo-success-checkpoint 的功能基线和验收完成后，统一其已有 Browse／Select／Write／保存完成／换关消息与结果出口；扩展实际 SoloSaveSelection／Commit／Restore 用例，验证选档取消不建世界、写盘失败不换关、重复完成不重存重装、UI 关闭不丢提交事实；使用测试专属目录并同步存档交接说明，不在本案补其未完功能。

## 5. 跨域与真实入口验收

- [ ] 5.1 核对现有 Join／Probe／Leave／Close 与新公共收尾兼容，运行 ProductSession／SoloMessage／LocalMessageShutdown 中必要本地及已有 KCP 小集；验证单成员退出不停止共用 sender、整体退出不等后续帧、内部新消息被 Peer／wire 拒绝；记录范围，不扩大远端产品玩法。
- [ ] 5.2 将原 HUD／Board 输入与结果反馈接入单一消息出口，保持受管 owner、快照观察和当前 Prefab／CodeBind；运行受影响 SoloRestaurantUi UTF 并从普通 UI 实际点击，验证一次点击一次请求、已接收／开始／完成分别显示、重开及旧回调不写新轮，更新 UI 调用说明。
- [ ] 5.3 使用普通 Launcher 跑覆盖两菜、询问出餐与原碗清洗、供应清理、暂停恢复、自然结算、装修／保存及下一关的最少真实流程；按 execution-notes 逐项填写运行证据，补缺口而不重跑已覆盖项，同步进度文档并执行 OpenSpec 严格校验；任何必需点未运行则保留未完成，Player/AOT、双 Editor 与新远端玩法标 NotRun。
