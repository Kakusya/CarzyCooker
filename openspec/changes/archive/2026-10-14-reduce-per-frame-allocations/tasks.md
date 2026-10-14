# Tasks

本案规划已补齐，2026-10-14 用户先授权实施第一批，随后要求继续做到 9/9；九项实施与验收现已完成，运行结果逐项见 execution-notes。设计与 owner 边界见 [design.md](design.md)，逐项验收及严谨程度见 [execution-notes.md](execution-notes.md)。纯重构按 `skip_specs: true` 跳过 delta specs，既有产品主规格仍约束验收。

## 批次与优先级

用户要求暂时不考虑真机测试，本案仅排 Editor／PlayMode UTF／真实 Launcher/HUD 验收；原 Player／Android／物理 LAN 范围外边界保持。任务 ID 和九个 checkbox 不变，按下表执行，编号不另复制成第二套任务。

| 内部批次 | 优先级 | 任务及批内顺序 | 退出条件／理由 |
|---|---|---|---|
| 准备 | 必需前置 | 1.1 | 同配置、预热后的基线可比较；只做一次代表性基线，不阻塞于设备环境 |
| A：地图与视口 | P1 | 2.2 → 2.1 | 先消除简单四角数组，再处理常驻地图分配；本组行为断言和实际显示／输入通过 |
| B：恢复与正常 HUD | P1 | 3.2 → 3.1 | 先止住失联帧重建按钮，再合并正常刷新和结构比较；反馈及时、旧回调无效、重开正确 |
| C：固定步与寻路 | P2 | 4.1 → 4.2 | 收益涉及更广调用链，放在表现层之后；顺序、实体增删、满格等待与连续路线独立性通过 |
| D：整体收口 | 必需验收 | 5.1 → 5.2 | 只补尚未覆盖的综合点，再作一次同条件性能复测；所有功能有当前实现的运行证据 |

P1 是近期优先，P2 是随后推进，不表示取消 C 批任务。A/B 每组完成就记录其局部结果，不等 C/D 才第一次验证；已对应同一实现的完整结果可复用，D 不重跑全部小集。必须完成 C/D 才报告整个 change 完成；不为每批切换重复采样或增加硬性提速百分比。

具体执行顺序为 `1.1 → 2.2 → 2.1 → 3.2 → 3.1 → 4.1 → 4.2 → 5.1 → 5.2`。第一、第二批均已获后续实施授权并验收，当前完成状态查下方 checkbox 与 execution-notes。跨 change 排期见[当前路线](../../../../Docs/Product/change-roadmap.md#当前批次与优先级2026-10-14)。

## 1. 代表性基线

- [x] 1.1 在改源码前按同一配置取得普通 Launcher 无变化显示帧、营业移动／加工／前厅和受管失联界面的代表性 GC.Alloc／帧耗时基线；检查实际初始化与预热已经结束，记录相关方法、操作及原输出位置到 execution-notes，不新增性能平台、全量状态副本或百分比门槛。

## 2. 地图与视口

- [x] 2.1 在原 Board owner 内复用渲染遍历／待删缓冲，按投影字段更新成员、地砖路径／高亮、工位、落物排列与标签，保持逐帧插值和跟随；补并运行行为 UTF 覆盖稳定投影、目标改变、同格落物、成员／顾客移除、进度标签和暂停，真实入口检查画面；本组回填 execution-notes 和审查对应条目，缓存用途及退出写明相邻注释。
- [x] 2.2 在原 MonoUIGridKitchen 复用四角数组，保留每帧视口尺寸计算与原 Board viewport 短路；补并运行连续查询和布局／尺寸变化的 viewport 断言，并运行 SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen 验证实际点击、拖动和重开；本组回填视口验收及实现说明，不改 Prefab／Canvas／CodeBind 结构。

## 3. 正常与恢复 HUD

- [x] 3.1 合并正常 HUD 刷新入口并记录已显示版本，以完整字段比较替代长字符串键，分开时间／进度文本与动作结构；补并运行同版事件加轮询不重复重建、同版命令反馈及本地面板变化仍更新的行为 UTF，运行 SoloRestaurantUiInformationHierarchyTargetDetailsAndPrimaryActionUseCurrentProjection；更新本组刷新／失效说明与验收，保持旧运行／旧关／旧绑定按钮保护。
- [x] 3.2 恢复页每帧保留状态和输入资格判断，仅在 Stage／AttemptId／Feedback／凭证可用性／Client／HUD 本轮上下文变化时刷新结构；扩展并运行 CoopMemberResumeUiRealDisconnectManualButtonReopenAndRetiredInput，断言稳定失联帧保持按钮实例、同状态新反馈／尝试及时更新、HUD 重开和旧尝试回调失效；本组记录与 cooperative-member-resume 的保留合同，不把凭证值写入显示键或日志。

## 4. 固定步遍历与寻路

- [x] 4.1 沿 GridKitchen／FrontLevelState／FrontWorkCommands owner 复用角色、顾客、工作和伙伴遍历缓冲，CanStop 直接计数，保留 EntityRef、MemberId／ID 顺序及前厅 Stop→推进→Claim 次序；执行 execution-notes 列出的物品竞争、伙伴优先级／FIFO 小集，顾客／工作在遍历中退出若缺行为断言则补用例并运行；同步缓冲归属／增删／退出约定及本组结果。
- [x] 4.2 厨房复用 BFS 队列与访问／前驱工作区，热循环直接按上右下左生成邻格，内部热消费者使用所属路径或可达性查询，公开 FindGridPath 保持独立结果；运行目标替换、满格等待／穿行、布局变化后玩家／伙伴／顾客路径小集，补并运行连续搜索、不可达和同格行为断言；同步路径所有权说明和本组结果，保留 50ms Tick 与停驻容量。

## 5. 综合验收与性能复测

- [x] 5.1 按 AutoTesting 发现真实 PlayMode 用例及 FullName，运行尚未被本案新结果覆盖的最小相关集，包含 CoopCoreUiRemoteManagedReopenAndRetiredHostCallbacksKeepTheirOwners；从普通 Launcher 实际检查移动插值、路径／高亮、落物、厨房／前厅标签、暂停、面板切换、HUD 关闭重开和恢复操作，逐功能对应 UTF／操作及原输出，确认编译和 Console 结果；已有覆盖完整的同一实现结果不重复跑，任何必需失败／跳过／缺输出保持未完成。
- [x] 5.2 在 1.1 同条件下做一次代表性性能复测，确认目标重复分配消除或明显减少、整体 GC.Alloc 改善且帧耗时无明显退化，记录预热／事件分配和仍保留的快照等成本；结果接近／反常／有回归时才补测，检查各组说明、任务映射、引用和严格 OpenSpec 格式，全部功能实际验收后才勾选完成，不声称整局零 GC。
