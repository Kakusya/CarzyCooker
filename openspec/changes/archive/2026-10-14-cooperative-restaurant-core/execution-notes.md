# Execution Notes

## 2026-10-14 归档收口

最终收口：全量严格验证 44/44（42 份主规格＋2 个保留的设备 change）通过；73 份相关 Markdown、561 个本地引用无缺失，编码／空白及 `git diff --check` 通过。八案已从活动列表移除，仅余 PC LAN／Android 各 0/5；本轮未新增 commit／push，原有未提交修改和临时资源保留。

本案已按用户要求归档，任务全部完成。已复用本文件逐功能 UTF／普通入口的最终有效结果；联机完整双 Editor 两种角色排列以 cooperative-restaurant-loopback 原记录为准。本次未改变实现或重跑功能测试。

本案 delta 已合并并逐块核对主规格；本批八案合计 47/47 任务、11 份 delta、41 项新增和 4 项修改要求、66 个场景，无批内规格冲突。保留各主规格已有标题／Purpose 和未涉及内容；本案范围外或历史 NotRun 不否定后续已交付的恢复、装修、饮品与保存能力，各归对应规格及执行记录。原始失败／日志保留原位，不作为本次新运行，也不提交额外证据包。下文未提交／未归档／尚待完成等文字保留当时阶段语境，以各节最终结果和本段归档状态为准。

## Current status

核心已完成7/7；核心交付阶段28个唯一UTF及受影响旧Grid小集23/23 Passed，普通Host＋同Editor独立Fiber真实KCP受管两菜验收通过。物流随后开放Grid13–17，原未知动作的这五项已迁出，当前0／99两项及受影响核心UI新运行通过，见[物流记录](../2026-10-14-cooperative-cleanup-supply/execution-notes.md)。当前协议17；本案证据限核心，前厅／物流另有各自验收记录，完整双Editor尚未完成。下文保留规划与实施历史，最终逐功能证据见末节。

## Planning status

2026-10-13 用户要求创建缺失的前置联机 change。本案从原 cooperative-restaurant-loopback 拆出核心开发范围；仅规划，0/7，全部功能 NotRun。没有业务代码、表、协议生成、Unity、监听或第二工作区操作。

## Rigor and scope

主要风险是 Host owner 切换、共享控制越权和旧消息写入新关，后果是局内物品／时间或身份错误。采用原 ET 队列与单写者，最小真实 KCP UTF 加一次受管入口两菜实操即可覆盖本案。复用未变 Solo 配方算法与 sender 生命周期证据，不重跑全仓。

明确接受同 Editor 验收边界；双 Editor 归原 loopback 案，Player／LAN／存档／身份恢复未验证。核心协议、owner 或 UI 改变时补对应受影响测试，出现具体晚到消息或重复物品再加局部用例。

## Acceptance mapping

用例名为拟补范围，不是已发现或执行的测试。每组可用一个有多项行为断言的用例覆盖，不要求按表行拆出固定数量。

| 任务／功能 | 拟用例或实际操作 | 结果 |
| --- | --- | --- |
| 1.1 owner／驱动／Solo／Client 边界 | CoopCoreOwnership | NotRun |
| 1.2 wire／本地路径／协议与答复 | CoopCoreWire | NotRun |
| 1.3 各共享控制与拒绝零改动 | CoopCorePermissions | NotRun |
| 2.1 两菜／锁定／末份与重放 | CoopCoreCooking | NotRun |
| 2.2 就绪／独立投影／旧关／UI | CoopCoreProjection＋实际 HUD 生命周期 | NotRun |
| 2.3 失联／Leave／整体退出 | CoopCoreExit | NotRun |
| 3.1 普通 Host 与真实 KCP 受管操作 | 两菜、布局、Buff、暂停恢复 | NotRun |

## Planning validation

2026-10-13：本案 `openspec validate cooperative-restaurant-core --strict` 退出 0、valid。四案与三个入口文档共 25 份 Markdown 的 UTF-8、尾空白及 164 个本地引用检查通过；全部任务保持未勾选。这些只证明规划文件有效，功能仍 NotRun。

## 2026-10-13 核心实现与 UTF

正式 Host 与 Solo 已统一为 design 的 RestaurantRuntime / GridKitchen / Level 树，Client 仅持有自己的 Session/投影。HostSession 的无餐厅 legacy clock guard 保留，真实50ms服务步只推进一次；历史 fixture 的父对象查询迁到 ProductKitchen / Grid.Match，成员计数改为明确 Participant，不再把新增餐厅 Child 当成员。原配方/容器/计时算法未改，复用对应 Solo 证据。

协议17在源 GridWorldSnapshot 追加 CanControlRestaurant，Proto2CS正常生成两模式；wire动作白名单明确枚举现有1–12动作，不按上限放开13–17/未知值。Level/Layout/Buff请求显式复制后进Peer ActorId的Ordered MailBox，Peer再复制可信Submit至HostSession Ordered MailBox，服务步提交；外层wire RpcId另存，内部Call使用原等待表，原请求/响应各自finally回收。已有所属Session值调用复用，不增RPC映射或传输入口。ProductClient响应白名单按实际类型与原pending.RequestType关联。

Host共享控制由 ProductRuntime.Client → ClientSession.Local → LocalMessage.Peer 的当前EntityRef/代次取得，同时检查 Participant 在线绑定；Peer接收和服务步最终提交均验证。客户端普通值权限标志只限制显示/旧按钮，不授予权限。Solo既有可信内存fixture合同保留，Host本地Leave不转交权限。核心HUD已按权限隐藏开店/暂停/继续/布局/Buff选择；远端前厅与供应入口仍未开放，实际受管双端验收待完成。

| 任务/功能 | 真实 PlayMode / KCP 用例 | 最新结果 |
| --- | --- | --- |
| 1.1 Host单厨房/Level/单步、Solo无监听、Client无权威及同世界 | CoopCoreOwnership 三例 | Passed |
| 1.2 未知/13–17动作拒绝且会话继续、旧协议/内部Submit拒绝、三控制响应RPC/业务编号关联 | CoopCoreWire 九例 | Passed |
| 1.3 远端四Level控制及Layout/Buff伪装本地代次拒绝、个人拿料仍合法；Host完整控制及最终绑定重验、Leave不转交 | CoopCorePermissions 八例 | Passed |
| 2.1 本地备料/远端交接/蛋液/煮汤/搬锅/盛装/烘烤/装盘；ProcessLocked/NotReady、远端远距只接收未提前加工、重复启动与末份争抢 | CoopCoreCooking 两例 | Passed；扩展锁定/远距后单例新运行Passed |
| 2.2 Join前snapshot复制但不开输入、DTO回收后值独立、旧关高版本拒绝 | CoopCoreProjection 两例 | Passed；受管远端UI重开仍待验收 |
| 2.3 实际KCP持锅失联/唯一落物/原自动批次继续/离线成员、远端Leave暂停原手工作业并本地接续、整轮Stop新世界拒绝旧命令 | CoopCoreExit 三例 | Passed |

11项首次核心小集10 Passed/1 Failed：权限切换测试用Finish推进了业务钟，暂停在切换前已提交；改为只泵消息的原LevelReply后，准确覆盖接收后/服务步前失去资格，新单例Passed。测试代码编译曾因EntityRef接收者/当前NUnit无Is.AnyOf失败，修正后编译通过；旧程序集发现返回空集NotRun，不冒充通过。当前27个唯一核心用例最新有效结果Passed，失败/NotRun没有改写或续跑；结果留原Pipeline固定位置和Editor日志，无证据副本。

现有测试Host监听仍为动态127.0.0.1:0、Client读取实际endpoint，所有实例独立Fiber/Session且fixture释放自己网络与树，未绑定Pipeline端口或改服务端配置。本案没有操作第二工作区、提交或归档。同Editor的两个UI实际输入、受管远端重开，以及受owner变化影响的旧小集仍待完成；完整两关/前厅/物流/双Editor不算在本次核心UTF证明内。

## 2026-10-13 受管远端 HUD

2.2 的 `CoopCoreUiRemoteManagedReopenAndRetiredHostCallbacksKeepTheirOwners` 已经真实 PlayMode 运行，1/1 Passed，退出码0。普通Host Launcher建树、关闭所属视图后，独立临时Fiber经原KCP/Session/Join接入同一餐厅；远端仅在本人确认、角色及受管Board/HUD就绪后可操作。实际Pointer拿碗仅发一条请求，关闭显示不撤销已接收动作，原Mono池复用后可放至交接台；旧Host/远端回调均不再发请求，未开放前厅/供应/清理的UI路径不发包，远端保持Joined。fixture只泵和释放自己的Fiber，并恢复原Fiber/同步上下文，无新产品工具或权威副本。

首次运行Failed：普通Host首个快照先于本地Join答复，原权限判断错误依赖客户端已收到答复，导致首屏无控制资格。改为权威在线Participant/Peer精确绑定加runtime.Client.Local的真实EntityRef资格；界面确认及当前角色/视图就绪门槛仍保留。修复后新运行Passed，失败输出未改写。编译期间发现EntityRef不可直接与null比较及测试误用旧fixture的Join简称，修正后recompile_status completed/failed=false。

当前6/7；3.1普通入口两菜、布局、Buff及暂停恢复尚待验收。此次权限修改须补跑原CoopCorePermissions小集；旧owner查询受影响的Grid小集仍待核对。原始输出保留 `Unity/Temp/pipeline_test_status.json` 与 `Unity/Logs/solo-five-changes-editor-20261013.log`。

## 2026-10-13 最终核心验收：7/7

首屏权限修复后，CoopCorePermissions八项最新运行8/8 Passed；受Host owner查询影响的GridCore／GridPrep／GridWire及汤引用小集23/23 Passed，两次退出码均0、Console覆盖完整。此前未变的核心制作／wire／投影／退出结果复用；加上受管远端HUD，28个唯一核心用例的最新有效结果均Passed。旧Join／Probe／单成员Leave监听保留用例亦1/1 Passed。没有扩大到全仓、产品构建或第二工程。

3.1经原Instance Launch／SceneHelper普通Host Launcher进入，临时Client复用已有独立Fiber fixture的KCP/Session/Join，并按owner依次关闭与重开两个受管Board/HUD。所有玩法操作来自真实Pointer按钮或地图输入，未注入菜品、加工状态或跳过时间。最终job `cad495fcb95247cb92c3ddb70c7a3230` completed、result.success=true，执行31227ms：

| 功能 | 实际操作与观察 | 结果 |
| --- | --- | --- |
| Host布局／Buff、远端同版观察 | Host选择Buff2，地图Pointer把交接台移到(2,12)；Client看到同选择/布局与自己角色，无Host控制按钮 | Passed |
| 原材料分工、汤与搬锅／剩份 | Host真实切配并放交接台，Client取同一番茄块、投料／打蛋／倒蛋液／煮制，搬同一锅至交接台，原碗盛一份，原批次剩1份 | Passed |
| 业务暂停／消息仍可恢复 | Host原暂停按钮使tick1335、汤progress6冻结300ms；恢复按钮答复后同批次继续，双方受管视图可依次重开 | Passed |
| 吐司与原盘、同一权威 | Client拿原面包烘烤、取真实吐司放入原盘；Host重开后看到原碗/盘，权威IdentifyDish分别为1/2，Client仍Joined | Passed |
| 普通Host开店与退出 | 原开始营业按钮进入Operating；释放fixture自有远端Fiber后，只停止本轮自有Host Play | Passed |

world=`ebaaabfae4204a7ba89ac896af882765`，Host member1／Client member2；原番茄块1284099843031183、原锅1284099786407949、批次1284099855614523、汤碗1284099786407943、吐司盘1284099786407956。仅为定位运行事实的短记录，不保存状态副本。普通Host画面见 `Unity/Assets/Temp/pipeline-screenshots/cooperative-core-host.png`，已实际查看；截图不替代上述功能断言。结束前Console groundTruth无编译失败、Error0、Warning0。

普通入口首段job `2a2a8f8d0703468789bdd4df64520be0`在吐司装盘返回NestedContainer：验收脚本错误地尝试把盘添加到吐司；既有裁决正确拒绝。按原规则改为拿吐司→添加盘台→拿原盘。下一job `b308f00ab40a426eac49ba45aa9d7d22`在Launcher异步建树前过早检查Root，立即失败且没有玩法写入；脚本等待实际Root后新job完成上述全流程。失败终态保持原工具输出，未续跑或冒充通过。

完整远端前厅／清理供应及双Editor仍NotRun，分别归两个后继与原loopback集成案；同Editor实际核心验收不改称双Editor完整餐厅。Protocol17，未操作第二工作区、提交或归档。
