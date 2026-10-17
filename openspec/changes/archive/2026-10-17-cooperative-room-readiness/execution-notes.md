# 执行说明

本轮第3项。用户已确认全部当前连接成员Ready、Host不可强开、内容/成员变化清Ready；不重复询问。实现及下述功能验收已完成；16个不同适用UTF实例均有最新Passed，另完成普通Launcher540×960与真实KCP双人两轮检查。

## 严谨程度与取舍

开局共享权限会影响原入口，因此要在领域验证门槛且覆盖本地/真实KCP，不只遮按钮。最小集检查两个成员、提案/成员变化和旧请求，普通入口走完整双人准备/开局/回房再开；已有未变Solo及运行中加入/恢复证据可复用，受变入口需实际补跑。不扩成公网大厅/主机迁移或设备矩阵；若兼容路径出现绕过或来源权限问题，补对应实际路径。

## 功能映射

| 任务/功能 | 实际验证 | 当前结果 |
| --- | --- | --- |
| 1.1 提案与开局门槛 | 下述四项核心用例 | Passed：全员/无强开、版本/绑定、重复Start、成员变化和本机来源修订，均有实际运行 |
| 1.2 原入口和寿命 | 双人UI两轮、取消/池化、受变入口/基线、显式Host及普通Launcher | Passed：2项Ready UI、7项原核心回归、2项来源UI及1项显式Host/设置/KCP旅程；普通入口另见最终记录 |

本案严格格式校验通过、4/4规划产物齐备；本轮9案45份Markdown/52个本地引用的编码、引用和空白检查通过。功能未运行不勾选，不提交/推送/归档。

## 实施中记录（2026-10-17）

- 新Match组件持提案及成员/绑定Ready，原Host统一裁决；所有直接Host启动先停在已发布提案，源记录载入亦不直接建Restaurant。Solo入口不改。旧RoomSnapshot与RosterMember源追加字段，经原Proto2CS生成，193个旧opcode保持，wire35→36。
- 初次编译被ET0013阻止：房间→准备状态→基线解析/广播形成静态类环。已把原授权纯值解析供两个真实消费者共用，原公开API保留转发；准备状态不反向广播，交回原ProductRoomSystem。之后FriendOf缺少表现实体字段访问已补声明，编译通过；没有把编译当运行验收。
- 核心首跑3项：`EntryRoomReadinessActualJoinLeaveDisconnectAndResumeClearEveryReadyBinding` 和 `EntryRoomReadinessActualKcpSharedProposalAllMembersCannotForceAndReplayCannotCreateTwice` Passed；来源修订用例Failed于Solo切换Host测试漏重置RuntimeInstanceContext，尚未触及来源行为。夹具已修正，另补菜单更换、旧绑定及显式Host启动覆盖，待新运行；不把旧通过冒充新增断言已通过。
- 原测试夹具改为真实发布/逐成员Ready/Start请求；UI新开预览辅助由void变IEnumerator以等待真实按钮准备，41处定义/调用、21文件仅做所需await适配，不在产品自动Ready。受变入口/基线用例仍须按覆盖补跑。
- 已关闭的房间显示owner不因每次Ready快照重新抢占HUD；真实餐厅退役事件明确重开原入口。此处UI行为仍待运行，不以静态阅读声明完成。

## 核心补验结果

已发现752个PlayMode用例，补充断言后仅运行 `RoomReadiness` 四项：4/4 Passed，退出码0，Console覆盖完整，无Skipped/空集。对应：

- `EntryRoomReadinessActualExplicitHostBootstrapPublishesButCannotCreateKitchenBeforeHostReady`：原显式Host初始化仅发布参数内容，无厨房；直接Start被MembersNotReady拒绝，真实本地Ready/Start后才建厨房及本人角色。
- `EntryRoomReadinessActualKcpSharedProposalAllMembersCannotForceAndReplayCannotCreateTwice`：真实KCP同版提案、未准备/仅Host准备拒绝、双方准备及取消、重复Ready、修改菜单清全部Ready、重复Start仅一个Restaurant，退役后须新提案/准备，错提案拒绝。
- `EntryRoomReadinessActualJoinLeaveDisconnectAndResumeClearEveryReadyBinding`：真实第三成员加入、Leave、Session断线及手动恢复新绑定均撤全部Ready；离线期间Host新Ready也在恢复时清空，旧绑定Ready请求拒绝，尚无厨房。
- `EntryRoomReadinessActualLocalSourceRevisionRequiresRepublishAndPrepareWithoutWritingProgress`：真实成功文件/原签名授权/身份证明后只发布来源；原字节保持。测试明确替换同Slot修订夹具后Start拒绝并撤提案/Ready，新的合法授权重新发布/准备/Start才恢复NextLevel，实际字节仍为修订夹具基准且成功文件数1。首次缺上下文Reset的失败已单独保留，未改写为通过。

以上为核心阶段的4/4结果；其后界面和普通检查见下文。原始结果仍在Pipeline原位置和Editor原日志，不复制或改写。

## 界面与受变回归（过程记录，终态见下节）

- `EntryRoomReadinessActualHostUiPublishPrepareCancelAndPooledCallbacksDoNotStart` Passed：原按钮发布/准备/取消均不建厨房，等待全员入口仅解释，不发Start；HUD重开旧准备回调无效。
- `EntryRoomReadinessActualKcpUiTwoPreparedRoundsRetireAndOldWorldCallbacks` Passed：复用原完整房间旅程，真实Launcher/实际KCP各自原UI准备，两次发布/准备/开局间真实退役，旧世界回调无效，成员/Session/sender保留。Host每次从确认到开局是发布、本人Ready、Start三请求，非原一请求；远端通过自己的原按钮准备。
- 原 `ProductRoom` 四项核心回归4/4 Passed；`CooperativeBaselineLoad` 三个测试实例3/3 Passed，涵盖真实他人/旧格式授权、最新命令重放/冲突、原源字节不变、半批次/在途货和冻结烘焙/升级恢复，不补物资。
- 原 `CooperativeBaselineUi` 首跑2 Failed：一项新文案只改高级页而遗漏任务页；另一项发布成功后过早关闭ProgressVisible，使原异步Current guard将自身收尾判为取消而Busy未释放。已补任务页文案，并在原RunProgress成功收尾、核对owner后才切回房间；没有绕过权限或提前建世界。尚待两项新运行，普通检查也未完成。

## 最终适用结果（2026-10-17）

后续补货请求验收触达旧 `StartGrowth(true)` 测试辅助，补其原显式Ready/Start步骤，避免把已发布房间误作已有世界；没有给产品自动Ready。该Growth/真实KCP菜单权限旅程在补货请求案单项新跑Passed，未据此重跑本案已有完整结果。

- 来源UI修复后新跑：他人授权用例Passed，本机来源用例在已完成来源准备/开局/真实KCP加工后因旧夹具重复添加由本机设置已建立的repository而Failed。将该夹具改用已存在的 `UseOwnedPreferencesRepository` 正常退役旧owner再建立隔离测试repo，仅重跑失败用例1/1 Passed。两来源UI当前均Passed，含浏览/取消、实际签名导出、用途/房间拒绝、显式信任、原源文件字节保持及KCP原操作。未重跑已通过的他人用例。
- `EntryPreferencesActualKcpClientUsesOwnLocalMailboxAndCannotChangeHostSettings` 单项Passed：覆盖修改后的显式Host Launcher真实准备/开局辅助，原本机设置、Host与Client文件隔离、KCP断线手动恢复及旧按钮仍适用；不重跑其余未变设置套件。
- 最小相关结果为4核心＋2准备UI＋4原房间核心＋3原基线核心＋2原基线UI＋1显式Host/设置旅程，共16个不同测试实例全部最新Passed，无Skipped/空集；Console覆盖完整。原始失败和后续新运行分别保留在 `Unity/Temp/pipeline_test_status.json` 和 `Unity/Logs/Business-Delivery-Editor.log`，未复制/改写输出。

普通检查使用原Launcher、540×960、运行时分配的回环UDP `127.0.0.1:62457`。沿普通主页创建房间、选择保留菜单并发布提案：Host先准备后，复用既有KCP客户端建立第二成员，原两端投影均为提案#3且两人未准备，证明加入清全部Ready。两人各自真实UI准备后，仍无世界；Host显式Start command4才建立同一世界和2角色，原拿碗command5实际Executed。原返回房间确认command6释放餐厅，仍2成员/原Session；无提案且无Ready。再发布提案#6、Client第二次Ready及Host command8准备后，Host command9开局得到不同新世界，两端同世界、2角色且空手。

第二客户端在本次普通Play中复用现有CoopUiRemote创建真实KCP/ET owner，未运行UTF；本次Editor update只负责该客户端的实际Tick与显示交接。退出时已请求并确认Dispose该owner、移除本次回调/数据引用及SessionState标志，再退出自有Play。限于本机回环，不提升双Editor/独立Player/物理LAN结论。原7个成功文件保持数量，未新增进度；原有本机名称/收藏及设置不变。

`room-ready-client-proposal.png`、`room-ready-host-all.png`、`room-ready-second-world.png` 三张普通截图均已实际查看，成员/准备状态、实际菜单、Host开局及新餐厅可读。GameView恢复原索引6，移除本次临时尺寸/键。仅核查本次UDP端口，退出后未见62457监听；Editor/Pipeline保留，未操作第二工作区。普通结束时Native Console Error为0。

主规格与玩家说明按实际合同同步；实际严格格式/引用检查见本次命令结果。其余未变Solo启动复用原无监听合同与本轮来源核心测试中的实际Solo完成/文件捕获；运行中加入与恢复由本轮真实KCP/设置旅程覆盖。真人协作节奏、设备/Player/物理LAN保持未验，不因本案扩大构建。未提交、推送或归档。

## 2026-10-17 归档与提交

用户已明确授权本批归档及本地提交。任务和原逐项运行结果保留，主规格已核对；归档后全量严格检查74/74通过（50份主规格、24案）。仅调整规划状态与相对引用，未因归档重跑未变行为。提交范围排除工作区既有Android启动/后台、LAN和HandmadeBistro改动；本机设置与记录元数据依赖的平台存储定义及文件读写保留。不推送，设备、正式平衡和真人体验仍按原未验证边界。
