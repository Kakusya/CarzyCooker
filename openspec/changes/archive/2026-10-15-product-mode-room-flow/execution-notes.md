# Execution Notes

## 当前状态与范围

2026-10-15：用户授权完成全部16案。本案模式菜单、真实无餐厅房间、Host开/关/重开、取消及失败返回已实现。相关8项唯一 PlayMode UTF 全部 Passed：4核心、2普通Host/菜单、2普通Client入口。原输出保留原位，无提交/push/归档或第二工作区操作。

## 严谨程度与已接受边界

主要风险是误停公共 sender、旧快照重建旧世界及切角色时释放正在遍历的 ET 树。选最小真实请求/KCP及普通入口小集，覆盖每个新增行为和关键拒绝/旧上下文。不存在只凭编译或总计数替代验收。真机、物理 LAN、第二进程/双Editor新入口、Player/AOT、包体均 NotRun；本次同Editor的独立Fiber真实KCP不冒充物理网络或双Editor。实现变更或出现具体回归再补对应最小集，不因文档同步重跑。

## 任务／功能 → 实际运行 → 结果

| 任务/功能 | 实际用例与操作 | 结果 |
| --- | --- | --- |
| 1.1 默认菜单、地址校验、取消、保留显式入口 | ProductEntryUiOrdinaryModesRejectInvalidAddressCancelJoinAndStartSoloWithoutListener 从普通Launcher实际按钮进入；非法地址保留Root，取消加入回新菜单，旧回调不切角色，单机无监听并实际取出碗内鸡蛋。ProductRoomExistingDirectHostStillLoadsOneWorldAndCanCook 保留显式Host。 | Passed |
| 2.1 无餐厅Join、唯一开局、关闭/重开与sender存活 | ProductRoomRealKcpJoinWithoutWorldStartCloseAndRestartKeepMembersSessionsAndSender 真实KCP先加入空房间，再开/关/重开；Match/成员/连接/sender保留，World更新，真实拿放。 | Passed |
| 2.1 越权与空世界拒绝 | ProductRoomRealKcpCannotStartAndWorldCommandWithoutRestaurantRejects 远端不能开局，无餐厅格子请求拒绝且无异常。 | Passed |
| 3.1 重复/冲突、旧房间及退役快照 | ProductRoomReplayConflictOldRoomAndRetiredSnapshotNeverCreateAnotherWorld 通过真实本地/远端消息断言重放一次、冲突拒绝、旧版本失效及超高版本旧世界快照不能重开。 | Passed |
| 3.1 受管显示/关开与旧视图 | ProductEntryUiOrdinaryHostRoomRealKcpReopenAndRetiredWorldCallbacks：普通Host菜单建房、独立远端只读HUD、owner交接、开启/返回/重开、退役地图回调不发新命令。 | Passed |
| 4.1 实际加入超时后可用 | ProductEntryUiJoinTimeoutReturnsModesAndCanStartSolo：实际无答复UDP目标，4秒请求超时回菜单显示失败，再进入真实单机地图。 | Passed |
| 4.1 普通原生Client填写地址成功加入/开局/地图输入 | ProductEntryUiOrdinaryClientAddressJoinsRealHostRoomAndOpensOnlyProjection：普通Launcher填实际端口，原生Client连接独立Host Fiber真实KCP；无本端权威，房间只读，Host经有序本地请求开局，Client真实Pointer拿放。 | Passed |

核心四项原输出：`Unity/Temp/ProductRoom-core-UTF.log`；普通菜单/Host两项：`Unity/Temp/ProductEntry-UI-UTF.log`；普通Client两项：`Unity/Temp/ProductEntry-client-UI-UTF.log`。各次summary分别4/4、2/2、2/2，failed/skipped/inconclusive均0，退出码均0。原始Pipeline输出仍在 `Unity/Temp/pipeline_test_status.json`，后续作业按原工具入口更新，没有复制JSON证据包。

每次先按 PlayMode 发现真实用例，再执行对应 category：`ProductRoom`、`ProductEntry`、`ProductEntryClient`。工具入口 `python Tools/AutoTesting/autotesting/run.py --run --mode playmode --filter_type category --filter <category>`；完整命令stdout在上述原日志。最新发现520项，不执行无关全仓套件。

普通Client测试使用当前World的独立Host Fiber和真实KCP Socket，实际普通客户端拥有AppRoot与UI。测试夹具只手动推进独立Host，并释放其显示以保持原生Client视图可见；服务端领域树、可信Peer、Join/房间/格子handler都走真实实现，无handler替身。测试结束只释放自己建立的Fiber与监听。

## 生成、编译与文档检查

协议23→24，新增源 `Design/Proto/ET-Client/ZZZZZZZZZZZProductRoom.proto`；从Bin正常运行 `./Tool.exe --AppType=Proto2CS --Console=1`，退出0，Client/ClientServer各新增199行消息和5个ID，旧Opcode未重排。无手改生成物，无Excel改动，不为纯代码执行Luban导表。原23字段CodeBind/Prefab未改，地址框由原Mono生命周期动态管理。

增量编译曾报新增视图缺少FriendOf和异步helper缺少using Game，已修复后重新编译成功；随后8项行为运行通过，Console历史记录未清除。没有把编译错误或未运行报告为通过。

主规格 product-entry新增、product-session/runtime-instances/startup局部合并，保留原场景。文档格式与本地引用单独检查，不替代上述运行证据。

同步后 `openspec validate product-mode-room-flow --strict` Passed，主规格严格验证44/44；12份相关Markdown的UTF-8及65条本地引用检查通过，四项任务按原行逐项勾选，apply终态all_done。后续合作三关改用新开关序，不由本案历史运行推断该增量已通过。
