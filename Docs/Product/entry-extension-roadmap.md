# 入口业务与 UI 第二批补充路线

后续继续盘点见[第三批操作闭环与记录导航](entry-business-deepening-roadmap.md)：另增10案；下文11案与规划检查数字保留本批范围，实时实施状态以各案 tasks/execution-notes 为准。

2026-10-16。用户要求尽可能继续寻找值得做的入口业务和 UI change。本批新增 **11案、33项未开始的实施任务**，每案包含 proposal/design/delta spec/tasks/execution-notes；不重复上一批18案和原队友总览案。

## 盘点结论

当前经营、实物加工、前厅、供应、成长和合作进度已有实际交付，上一批入口案正在按原任务推进。本批重点补“执行前理解”和“执行中查看”：菜谱目录发现、生产许可来源、容器转移量、持续加工进度、布局候选与加成效果，以及特定对象的详情。

源码与长期文档有一处明显差异：[长期目标](../../long-term-goals.md)仍写玩法属于未来设计，而[当前进度](../../CarzyCooker当前进度.md)、源码和已归档验收已覆盖真实玩法；本批据当前代码与规格选缺口，不据旧状态句重复创建系统。长期目标仍有效约束合作做菜、操作顺畅、物品清楚和自然分工的方向。

上一批[入口路线](../../openspec/changes/archive/2026-10-16-product-entry-navigation/entry-roadmap.md)开头是当时的规划快照；最新完成状态按 openspec list、各案 tasks 与 execution-notes 判断。本批没有回退旧任务或将新增规划算作原18案交付。

## 新增 change

| 编号 | Change | 可观察补充 | 相邻案与分工 |
|---|---|---|---|
| 1 | [菜谱目录检索与浏览](../../openspec/changes/recipe-catalog-navigation/proposal.md) · `recipe-catalog-navigation` | 增加当前许可菜谱目录，支持名称检索、按实际依赖设备筛选、空结果说明；选择后进入原菜谱，返回保留本轮筛选与滚动位置。 | order-production-navigation、restaurant-preparation-overview |
| 2 | [本关菜单与生产许可说明](../../openspec/changes/menu-production-permission-overview/proposal.md) · `menu-production-permission-overview` | 增加按已交付菜品查看的许可说明，区分当前菜单、持续解锁和本关可生产结果；说明受限旧物/在途货/已开始作业的保留边界，并链接原菜谱、物品或补货查看。 | restaurant-preparation-overview、product-action-recovery-guidance、workstation-capability-inspector |
| 3 | [容器内容与转移预览](../../openspec/changes/container-operation-preview/proposal.md) · `container-operation-preview` | 提供可选的容器操作说明页，显示容器身份、容量单位、真实内容和锁定；对原可用动作说明源/目标与一次转移量，明确整容器搬运、固体取出、液体倾倒、按份盛装的差别。 | kitchen-item-locator、kitchen-supply-overview、order-production-navigation |
| 4 | [厨房加工与待取产物看板](../../openspec/changes/kitchen-production-workboard/proposal.md) · `kitchen-production-workboard` | 增加加工看板，区分接近/手工进行/停止保留、自动进行、完成待盛装或待脱模；显示真实冻结耗时、执行者、所在容器/工位和有限剩余份数，链接原位置查看与合法接续动作。 | front-service-workboard、kitchen-item-locator、cooperative-team-awareness |
| 5 | [布局调整预览与明确提交](../../openspec/changes/restaurant-layout-edit-workflow/proposal.md) · `restaurant-layout-edit-workflow` | 仅在显式进入调整模式后，地图点选和旋转先改变本端候选预览，显示原位置/朝向、候选占地和操作格；确认一次提交原布局请求，取消恢复查看已提交布局。 | workstation-capability-inspector、kitchen-action-navigation |
| 6 | [本关加成效果预览与确认](../../openspec/changes/level-buff-choice-preview/proposal.md) · `level-buff-choice-preview` | 候选先打开只读说明，列出本关实际受益配方、无本关加成与采用候选后的新作业耗时，解释已有工位升级和作业冻结；确认后才发原 BuffChoice，取消返回同一候选。 | restaurant-preparation-overview、renovation-choice-preview |
| 7 | [小麦岗位与关内成长详情](../../openspec/changes/companion-role-and-growth-inspector/proposal.md) · `companion-role-and-growth-inspector` | 提供只读伙伴详情，说明现有询问优先/否则洗碗、已开始不抢断，展示当前真实目标/前往或执行、已完成次数、本关洗碗成长是否获得和当前冻结耗时。 | front-service-workboard、cooperative-team-awareness |
| 8 | [顾客与餐桌状态查看](../../openspec/changes/customer-table-inspector/proposal.md) · `customer-table-inspector` | 提供按桌位/顾客查看，区分空闲、入座待询问、有效订单等待、用餐、离席待清；显示真实关联顾客和当前订单剩余期限，链接原询问、订单菜谱或清桌入口。 | front-service-workboard、order-production-navigation |
| 9 | [合作失联与原成员恢复说明](../../openspec/changes/cooperative-recovery-status-details/proposal.md) · `cooperative-recovery-status-details` | 补齐房间和餐厅失联页，区分失联可手动恢复、正在尝试、恢复被拒绝/资格结束；解释本次运行内恢复原成员与重新加入的不同，说明原实物保留/落物和恢复后空手。 | cooperative-connection-assistance、cooperative-team-awareness、restaurant-leave-confirmation |
| 10 | [合作房间身份与存档职责说明](../../openspec/changes/cooperative-room-authority-summary/proposal.md) · `cooperative-room-authority-summary` | 在房间和餐厅增加身份/职责摘要，标出本人角色、当前房间控制权、来源记录 Owner/Slot/修订和本端已验证身份；解释内存新开、只读来源运行和个人领取去向，链接原进度/授权入口。 | save-authorization-task-flow、cooperative-progress-inbox、solo-save-inspection |
| 11 | [密集详情与动作列表的可读布局](../../openspec/changes/archive/2026-10-20-product-hud-readable-detail-layout/proposal.md) · `product-hud-readable-detail-layout` | 独立调整现有 HUD 的详情与长列表布局：明确可滚动正文和固定关闭/返回区，动作列表与正文滚动分离，长中文可完整阅读；详情关闭后恢复地图操作区，保留相关结果列表的有效滚动位置。 | kitchen-action-navigation、product-operation-help及本批所有详情入口 |

## 推荐实施安排

先完成原批正在推进的功能，保留已验收结果。新批优先菜单许可、容器预览、加工看板，再接布局和加成确认；这些最直接减少无效备料与操作误解。若长文实际不可完整阅读，先做独立的可读布局案，再接需要长列表的目录与详情。最后补小麦、顾客、失联恢复和房间职责说明。

各案不是互相全部阻塞：源数据和原动作已存在即可单独交付，相邻案未落地时通过原入口完成验收。共有 GridPresentationSystem/DisplayContext/MonoUIGridKitchen 顺序整合；原稳定按钮和原功能不能为新增看板被移除。

## 避免重复与控制边界

- 菜谱目录以“找菜”为单位；原订单导航以“某一订单”为单位，仍负责订单阅读目标。
- 许可页解释为何能生产；准备总览负责开店条件；工位说明负责设备能力，三者不各自裁决资格。
- 容器预览解释一次转移及源/目标；物品查找负责物理位置；供应总览负责外部配额/货物/实物，不建第二份库存。
- 加工看板以厨房作业为单位；前厅看板以必要前厅工作为单位；小麦详情解释固定岗位和关内成长，不新增派工能力。
- 顾客/餐桌详情覆盖无订单时的查看与桌位换客；订单导航仍负责选定订单的制作上下文。
- 布局候选只在明确调整模式使用，普通点击继续直接执行；工位说明仍只画已提交几何。
- 加成预览解释原候选，确认后才选择；装修预览解释另一套奖励，不合并两者业务状态。
- 失联页复用已经交付的手动恢复，不把该系统列作未实现；房间职责页说明 Host、Save Owner、本端身份，不重做授权任务流程。
- 结构布局单独成案，保留UI1101、原owner和CodeBind生成纪律；不把结构runner、通用消息总线或新架构当隐含前置。

## 继续研究的方向

以下 **8个方向保留为候选**。它们会新增玩法状态、权限或文件语义，需要决定后才适合建立可实施的合同；不以含糊任务凑 change 数。

| 方向 | 价值 | 建案前关键决定 |
|---|---|---|
| 房间章节提案与成员准备 | 双方明确即将开始的内容 | Host能否强开、晚加入和换提案如何使Ready失效 |
| 玩家分工/订单制作意向 | 减少同菜重复生产 | 只提示还是独占、取消/过期/断线清理 |
| 队友补货请求 | 将材料需求与正式采购分开 | 审批权限、重复合并、取消和采购权限 |
| 伙伴换岗/派工 | 玩家主动分配前厅工作 | 固定伙伴已确认边界如何调整、优先级和已开始任务能否抢断 |
| 经营事件提醒 | 视线离开地图时发现到货/完工 | 首批事件、音效素材、默认开关、暂停与重复合并 |
| 输入与可读性设置 | 适应具体设备及操作习惯 | 支持设备/重绑定范围、字体或缩放上限和保存位置 |
| 存档命名与整理 | 区分独立检查点和来源分支 | 名称元数据、是否删除、恢复策略及文件覆盖保护 |
| 真人首次上手观察 | 用真实阻塞点确定下一轮优先级 | 可参与人员/设备和观察安排，脚本不能替代真人 |

这些候选不改变已确认离线授权、现有固定伙伴和跨关许可规则。正式传输、更多菜品、经济/平衡及真机验收继续沿其独立决定和原案。

## 验收与当前状态

每案 execution-notes 写明主要风险、最小验收与可复用范围，按“任务/功能 → UTF或实际操作 → 结果”记录。文件/身份/共享布局和UI生命周期按实际后果加强检查；纯只读页面不新增请求、仓库写入或自动操作。

本轮只做规划检查：11案逐案严格校验11/11通过，56份Markdown及157个本地引用检查通过，无编码替换字符或行尾空白。功能全部 **NotRun**，33项实施任务未勾选。每案四项规划产物齐备，无条件跳过，另有execution-notes；可进入后续 apply。本轮不启动Editor、测试、构建、服务，不提交、推送、同步主规格或归档。静态检查结果见各案 execution-notes。
