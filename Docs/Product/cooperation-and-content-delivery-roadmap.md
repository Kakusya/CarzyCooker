# 十二项业务方向的实施路线

2026-10-17。用户要求将本轮十二项候选总结为change并全部完成；目标还包括前一轮已建的营业总览。该目标持续有效，不能以规划齐备或少量用例通过代替全部功能交付。本轮不扩大为项目中其他22个既有未开工业务案或两个独立设备案；相邻已有功能按实际受变补验。

## 范围账本

| 原编号 | Change | 当前进度/范围 |
| --- | --- | --- |
| 前轮 | [restaurant-live-service-overview](../../openspec/changes/archive/2026-10-17-restaurant-live-service-overview) | 4/4任务完成并验收；需求/客流/评分、普通入口及真实KCP/恢复 |
| 1 | [cooperative-order-intents](../../openspec/changes/archive/2026-10-17-cooperative-order-intents) | 2/2任务完成并验收；5项不同适用UTF、普通540×960实际制作交付；多人同单、不独占，退出/恢复/超时/换关清理 |
| 2 | [cooperative-target-pings](../../openspec/changes/archive/2026-10-17-cooperative-target-pings) | 2/2任务完成并验收；4项不同适用UTF与普通540×960三类目标、原锅拿放；10业务秒/暂停/退出/恢复/换关，无自动移动/认领 |
| 3 | [cooperative-room-readiness](../../openspec/changes/archive/2026-10-17-cooperative-room-readiness) | 2/2任务完成并验收；16个不同适用UTF、普通540×960及真实KCP双人两轮；显式Host/来源均先发布，全员准备后显式开局 |
| 4 | [cooperative-supply-requests](../../openspec/changes/archive/2026-10-17-cooperative-supply-requests) | 2/2任务完成并验收；4项不同适用UTF及普通540×960；真实KCP竞争一次预留、任意原采购权限成员处理，无Host审批，原货物跨关/退出续运 |
| 5 | [restaurant-event-notices](../../openspec/changes/archive/2026-10-17-restaurant-event-notices) | 2/2任务完成并验收；5项不同UTF、实际KCP/恢复及普通540×960；到货/完工/临期、最近8条及查看原目标 |
| 6 | [restaurant-service-recap](../../openspec/changes/archive/2026-10-17-restaurant-service-recap) | 2/2任务完成并验收；真实交付/超时/零分、Host/KCP同版复盘及普通下一关 |
| 7 | [local-save-labels-and-favorites](../../openspec/changes/archive/2026-10-17-local-save-labels-and-favorites) | 2/2任务完成并验收；4项不同UTF及普通540×960/Stop-Play；本机独立名称/收藏，真实写失败保旧，不改成功文件 |
| 8 | [product-input-readability-settings](../../openspec/changes/archive/2026-10-17-product-input-readability-settings) | 3/3任务完成并验收；5项不同适用UTF、真实KCP/恢复及普通540×960，主规格已同步 |
| 9 | [fixed-companion-role-preferences](../../openspec/changes/archive/2026-10-17-fixed-companion-role-preferences) | 2/2任务完成并验收；6项不同测试实例及普通540×960，Solo/Host三偏好、不中断原工作；KCP只读/拒绝、跨关保留、新运行自动 |
| 10 | [restaurant-themed-chapter](../../openspec/changes/archive/2026-10-17-restaurant-themed-chapter) | 3/3任务完成并验收；9个不同适用UTF最新Passed，含Solo/真实KCP三关四菜交付、实际检查点/完成记录/共同领取、原房间及独立复玩；旧Next/库存不变 |
| 11 | [kitchen-steamed-food-batch](../../openspec/changes/archive/2026-10-17-kitchen-steamed-food-batch) | 3/3任务完成并验收；14个不同适用UTF含Solo/真实KCP完整原入口制作交付洗盘、9个实际文件往返；540×960菜谱/新模型及加成文案检查 |
| 12 | [kitchen-tea-beverage-batch](../../openspec/changes/archive/2026-10-17-kitchen-tea-beverage-batch) | 4/4任务完成并验收；19个不同适用UTF最新Passed，含普通Launcher Solo/真实KCP三杯制作贴单交付30分，限定原设施混杯处理和实际文件 |

状态以各案tasks/execution-notes和实际运行记录为准；骨架不是可实施产物齐备，也不代表完成。

本轮十二项业务候选加营业总览共13案，33/33任务完成并验收。各案均补齐规划产物、实施、最小相关UTF及必要普通入口/KCP/实际文件验证；原始失败与后续新运行分开记录，规划检查不代替运行。完整映射见各案execution-notes，不额外扩大到正式平衡或设备构建。

## 当前决定和事实

- 房间Ready规则及上述四款内容/三关范围，用户已分别答复采用推荐范围，不重复询问。
- 用户已答复采用同一组协作规则：制作意向非独占、补货任意原采购权限成员处理、不加Host审批、小麦Solo/Host选自动/询问优先/洗碗优先，且不中断已开始工作；无需重复询问。
- 经营提醒、命名收藏和本机设置的可逆默认在各案spec/design中明示；不改变网络权限、计分、成功载荷或既有离线授权。
- 探索时章数写死2、热杯只覆盖特定液体；现已交付第三章的实际菜单闭包，以及本批冷杯/调饮壶的限定固液顺序与清理。旧Level/Next、库存和文件读取语义保持，其他容器不放宽。

## 实施与验收安排

先完成营业总览的真实运行并保留失败原输出，随后本机设置→经营提醒、复盘及记录整理可独立推进。合作案沿原本地sender/远端Session、统一Host裁决和当前owner建树；房间Ready和关内协作分别处理。蒸食与茶饮改Luban源后通过Check/生成，再接主题章，不用生成物手改或免费成品替代缺失加工。

每案新增行为用最小相关UTF和普通Launcher真实入口；联机补Host本地/真实KCP，文件功能用隔离临时目录真实往返及失败保旧。各案逐项记录“功能→用例/操作→实际结果”；编译、规划校验、任务数量和同一条成功结果均不能代替其他未运行功能。设备/正式平衡/真人理解分别保留边界，不默认扩大到构建或真机。

2026-10-17 用户已授权将本批13案归档并本地提交，未授权推送。33/33任务均完成；归档保留原逐项运行证据，复用未变行为的适用结果。
