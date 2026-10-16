# 探索与执行说明

## 2026-10-16 实施与实际验收

四种离开意图已接入原 ProductEntryPresentation/UI1101：Solo 返回模式、Client 离开房间、Host 仅返回房间、Host 结束整个房间。确认只交接受管 HUD，不发 Pause/保存/领取；取消归还原地图，提交后撤下确认和取消。视图捕获 Client EntityRef、Match/binding、room version、world/关与 generation，旧按钮/旧结果不能作用于新上下文。

Host 仅回房间仍发送原 CloseRestaurant；正常结束整个 Match 复用 LeaveRoomNormally 的关闭回执/ACK 等待。Client 改为等待原 LeaveProduct 的 committed/失败，再沿原 Loader 切模式，反馈区分 Host 已确认与只结束本地连接。签名/文件发布、业务协议、原成员落物与认领释放规则不变，无新表、Prefab、CodeBind 或监听工具。

PlayMode 实际发现707项，本案新增5个不同用例，各有当前适用 Passed 结果、Skipped=0，相关成功运行 Console 覆盖完整且无新增 Error。按受变范围分别运行，未把多次重跑累计成新用例数。原始输出在 `Unity/Temp/pipeline_test_status.json`、Editor 原始日志和工具记录，后续自然更新，未复制、改写或清 Console。

| 任务／功能与 Scenario | 实际用例／操作与行为断言 | 当前结果 |
|---|---|---|
| 1.1/1.2 正常 Scenario：Solo 营业中取消，原记录保留 | `EntryLeaveSoloOperatingCancelKeepsClockAndFilesThenExplicitReturnRetiresOldCallbacks`：真实已发布临时记录；营业中打开确认不加业务命令，时钟继续、无自动暂停，取消仍同世界；原拿碗实际执行。再主动暂停后重开确认，Tick保持、不会自动恢复；明确返回后源字节不变 | Passed |
| 1.1/1.2 正常 Scenario：Host 仅回房间再开 | `EntryLeaveHostRoomReturnPreservesUnclaimedFilesThenWholeMatchClosesActualReceipts`：普通Host及真实KCP双方证明身份，自然结算/明确不选装修后各自持久凭证；取消不退役，确认仅关闭餐厅后保留同Match/两成员/发送器及双方Available凭证，新开世界独立 | Passed |
| 1.1/1.2 变化 Scenario：结束整个 Match 与真实回执 | 同一Host用例：新世界另行确认结束整个房间，提交期间无取消、重复旧确认不重复提交；原关闭回执实际在双方落盘，重新打开Host原目录后两票均Closed，已有成功记录字节保留，没有代领新档 | Passed |
| 1.1/1.2 变化 Scenario：Client 持物正常离开 | `EntryLeaveOrdinaryKcpClientDropsHeldItemWithoutStoppingHost`：原Launcher加入独立Fiber真实KCP Host，实际拿碗、取消仍持有、再确认原Leave；Host移除角色，原碗落地，Host发送器及本地成员保留；普通模式显示已获Host确认 | Passed |
| 1.1/1.2 认领释放 | `EntryLeaveOrdinaryKcpClientReleasesFrontClaimThroughOriginalLeave`：自有fixture暂缓伙伴调度，为玩家真实KCP认领询问提供确定条件；实际取得FrontWorkId后确认Leave，Host移除角色并释放玩家认领，未停止其发送器 | Passed |
| 1.1/1.2 失败与真实本地状态 | `EntryLeaveRejectedByRealHostReturnsModesWithUnconfirmedFeedback`：自有Host Peer实际CloseEntry，由原接收handler拒绝Leave；Client仍完成本地退出并返回模式，明确显示未获Host确认，Host仍在 | Passed |
| 1.2 关闭重开/旧回调 Scenario | Solo用例实际取消/重开、池化归还地图；Host旧确认在新世界和新模式触发无影响；原确认delegate重复调用无重复退出，Client取消保留原手持物 | Passed |
| 2.1 普通入口540×960 | 普通Launcher单机内存201打开说明/取消→原拿碗command1 Executed、手持非零→主动暂停command2→确认页两次读取Tick均801→明确返回模式。再普通创建Host/开店→取消关闭说明→确认仅回房间，同Match/1成员/发送器保留→房间返回模式单独显示整个房间后果→取消再确认，实际回模式且无Host/Client | 已实际操作并查看画面 |

三张实际540×960画面：`Unity/Assets/Temp/entry-leave-solo-confirmation.png`、`entry-leave-host-room-confirmation.png`、`entry-leave-host-end-confirmation.png`。正文和确认/取消可读可点，普通Host的仅回房间前后Match均为 `59a57967bd9e45b7a7d181cd393efb59`。手动场景是准备态，营业中的取消/推进由上述Solo UTF证明；KCP以真实UTF为证，不将手动单Editor当作物理LAN。本轮自有Play已停止。

失败与修正：初轮4项为2 Passed/2 Failed；Host夹具在退役业务状态已到、关闭答复尚未归还HUD时就关闭页面，导致重开看不到菜单。保留两次失败后，原ReturnToRoom测试交接改为同时等待真实关闭答复和房间HUD，Host单项通过。旧相关旅程均明确经过新确认，不让测试绕过玩家入口。

原合并Client用例在本机初始化重启后继续驱动同Editor临时Host，其伙伴AsyncOwner继承原Init scope而已停止，产生CompanionRole错误。拆成独立启动的持物与认领两项；认领退出提交后先检查原Host状态，再退役自有fixture，之后仅等待本机模式页，不改产品异步生命周期。认领准备中还曾与小麦竞争失败/等待超时；最终仅对自有fixture的NextScheduleTick延后1000，实际客户/工作/认领/Leave均走原请求，不伪造状态、进度或结果。默认伙伴竞争沿[原前厅看板验收](../2026-10-16-front-service-workboard/execution-notes.md)复用，不以此夹具证明新的调度玩法。

编译时修正局部函数捕获的client声明顺序、测试命名空间及原枚举成员；最终编译0错误/0警告。域重载期间的recompile 400未启动测试，ready后才运行。随后只补跑受影响的Host/Client和暂停单项；失败结果原位保留，没有LogAssert掩盖错误。

严谨程度：主要后果是退出范围误解、未保存运行被误称成功、未领取资格误关闭，以及迟到确认关闭新世界。真实文件、两种正常关闭、KCP落物/认领/拒绝、旧确认与普通入口覆盖本次风险；原发布和关闭回执规则沿[原领取验收](../2026-10-15-cooperative-result-claims/execution-notes.md)复用。接受未验证真人理解、Player/Android/物理LAN、任意提交中强制卸载整个UI模块；有具体设备遮挡、关闭协议变化或强制模块卸载需求时再补对应范围。不扩大成全仓或全设备回归。

收尾：本案5份规划/执行文档和操作手册、当前进度共7份Markdown，172个本地引用与编码/行尾检查通过；严格校验通过。三个任务按CLI返回行逐项核对并刷新确认3/3、all_done，原18案16/18、48/54。未提交、推送、同步主规格或归档。

下文为建案时的规划记录，当前结果以上方为准。

## 2026-10-16 依据与决定

Solo 营业中缺少清楚的返回模式入口，合作有 Host 返回房间及房间返回模式，但不同关闭范围对未保存运行和待领资格影响不同。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRoomSystem.cs)、[主规格](../../../specs/product-entry/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：复用 ReturnToModeMenu、原 CloseRestaurant 房间命令及 LeaveRoomNormally 的 ProgressCloseMatch 流程。选择和确认归当前 Presentation／HUD，捕获世界／关／房间／binding；Client 的正常 Leave 沿已有控制入口收尾后切模式，不直接销毁共享 sender。原存档与资格结果只查询、不临时补写。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

本案涉及文件操作或运行退出的玩家判断，主要风险为混淆关闭餐厅与正常关闭整个 Match，导致不符合原合同的资格或存档处理。保持原发布、权限和生命周期实现，只改变入口或增加只读检查；使用真实临时文件／原控制链及 UI 运行验证，覆盖取消、迟到与失败，不扩大成任意断电或全设备试验。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | Solo／Client／Host各关闭范围、取消／失败／重复及正常关闭回执；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate restaurant-leave-confirmation --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
