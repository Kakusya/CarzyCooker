# Execution Notes

## 状态与严谨程度

2026-10-15：用户授权完成全部16案并明确合作首个大关三关。本案已实现并运行：4项核心及1项普通Launcher/真实KCP受管UI唯一用例的最新有效结果全部Passed。新开合作使用ChapterId=2的301→302→303/401→402→403；旧1/2、3/4及Solo101/201配置保留。无个人成功档或领取凭证，界面明确内存合作。

主要风险是误判终关建第四关、提前释放结果或关闭新世界；保留旧行避免改变v1存档Next语义。采用真实前厅自然排空、Host/远端共同结果、重放/权限/旧上下文断言，加普通入口三关整轮。既有汤/吐司/饮品制作与旧格式恢复路径未改，复用原记录；不因整理扩跑全部UTF。真机、物理LAN、独立双进程新入口、Player/AOT与产品构建NotRun，同Editor独立Fiber真实KCP不冒充这些环境。新增相关行为或出现具体回归时补最小集。

## 任务／功能 → 实际用例 → 结果

| 任务/功能 | 实际运行与断言 | 结果 |
| --- | --- | --- |
| 1.1 三关配置/动态总数/非法引用 | CooperativeChapterConfigDefinesFiniteChainsAndPreservesLegacyReferences 验证两条3关链、末端无Next/装修、实际章节计数、缺第二/三关拒绝、旧2/4仍自循环。LevelConfig和CustomerFlowConfig源各追加6行，所有原行及其他ZIP条目保留；正常Luban5目标Check与生成通过。 | Passed |
| 2.1 终关自然收口与相同结果 | CooperativeChapterThreeNaturalZeroStarLevelsProjectSameTerminalWithoutFourth(False/True) 两个参数化用例，均经三次真实Start/Operating/Draining/Completed、顾客自然排空，真实KCP两端0分0星和未满足订单一致；厨房/库存不补满，终关不装修、不建第四关、Continue拒绝。 | Passed |
| 2.1 Host确认只退役餐厅 | CooperativeChapterHostFinishReplaysOnceKeepsRoomAndRejectsOldWorld：先验证非终关Finish在业务步WrongPhase，再自然三关；Client确认HostControlOnly；相同Host确认重放仅关闭一次。保留Match/成员/Session/sender，RoomVersion加1，新开World不同。 | Passed |
| 3.1 旧输入/重放/权限/失败引用 | 同一HostFinish用例中，本地旧Finish返回WrongWorld，远端旧控制先被HostControlOnly拒绝，只有一个新餐厅；配置缺失、终关Continue、旧源兼容由上两组断言共同覆盖。 | Passed |
| 4.1 普通Launcher/UI整轮 | CooperativeChapterUiOrdinaryHostThreeLevelsRemoteResultFinishAndSameRoomRestart：模式菜单建房并开餐厅，三个实际营业与自然结算，双方独立值同统计，动态1/3至3/3。Client通过原owner重开只读终关HUD，Host双击只发一次确认，回同房间再开，旧真实按钮不发新命令。最终用时148.44秒。 | Passed |

## 原输出及失败处理

- `Unity/Temp/CooperativeChapter-core-UTF.log`：首轮4项1Passed/3Failed。运行启动校验暴露未追加逐关CustomerFlow；补齐源表后正常Check/生成，没有继续用旧生成物。
- `Unity/Temp/CooperativeChapter-core-fixed-UTF.log`：4项3Passed/1Failed。剩余为远端旧控制的预期错误原因不符，实际按权限优先拒绝HostControlOnly；生产权限行为正确，修正断言。
- `Unity/Temp/CooperativeChapter-finish-UTF.log`：仅复验上述失败用例，1/1Passed。其余三个用例行为未变，复用上一轮Passed。
- `Unity/Temp/CooperativeChapter-UI-UTF.log`：同Editor双视图竞争同一GF form失败，测试按原owner的CloseGridHud/OpenGridHud交接视图。
- `Unity/Temp/CooperativeChapter-UI-fixed-UTF.log`：下一关投影已到而按钮尚未刷新，测试直接查Single失败；改为等实际受管按钮出现后Pointer点击。
- `Unity/Temp/CooperativeChapter-UI-controls-UTF.log`：普通整轮1/1Passed，失败/跳过/不确定均0，退出0。没有清Console、改写原失败或用空集冒充通过。

每轮先实际发现PlayMode用例，再经现有Tools/AutoTesting运行category CooperativeChapter、CooperativeChapterUi及失败项精确testName。最终原始Pipeline输出保留 `Unity/Temp/pipeline_test_status.json`，未复制JSON证据包或状态副本。各运行都已等终态；不会因文档同步重复运行已覆盖测试。

Luban最终输出 `Unity/Temp/CooperativeChapter-Luban-check-2.log`、`Unity/Temp/CooperativeChapter-Luban-gen-2.log`：5目标与本地化完成，退出0。生成数据只增加对应Level/CustomerFlow行，派生代码/ID未新增；原工位升级此前修改仍保留。协议仍24，本案不新增wire字段。后续基线案的Proto源准备不代表生成或功能完成。

## 文档和范围

已同步restaurant-chapters及restaurant-level-lifecycle主规格，保留原Solo/联机有效场景；当前进度、提案和设计按实际终点更新。没有提交、push、归档或操作第二工作区。没有修改既有用户的grid-kitchen规格。
