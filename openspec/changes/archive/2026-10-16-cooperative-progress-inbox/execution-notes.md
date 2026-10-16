# 探索与执行说明

## 2026-10-16 实施与实际验收

已接入主页/房间/合作餐厅的“合作待领”入口，数字只由本端已有repository的原Browse答复派生；无上下文、查询中和查询失败分别显示，不把未知当零。Root的ProgressInboxChanged只使计数失效，后台合并查询与领取页成功Browse共用同一计数；独立scope/序号/repository身份隔离迟到答复，取消只撤销显示等待。Grid只观察Root显示版本，保持ET系统单向依赖，不反调或停止Root共用sender。

领取页可按全部/可领取/已领取/已关闭筛选和查看结果，详情显示关卡/得分/星级/结果/房间及本机Slot；个人领取沿原明确请求。已领先经原记录浏览读当前Slot/修订；合作按原权限查看或读取，空闲离线有明确“前往单机选档”，经原ProductRoot本地事件及SoloSavePresentation完成原Select只读检查，确认前不开局。预期修订变化和旧跳转不会重复创建页面。

只读审查发现原Browse会间接调用OwnIdentity初始化。现仅在存在凭证时由身份owner先验证原私钥、公钥和自身信任存在且一致，缺失直接报查询失败，不补建身份；空inbox不初始化。明确身份操作、资格/签名/发布/ACK合同及wire/file格式保持，未增加修复工具。

PlayMode实际发现702项，最终`EntryInbox`类别3/3 Passed、Skipped=0、退出码0，Console覆盖完整且无本轮新增Error。期间离线选档与Client去向补验分别只重跑受变单项；最终身份只读前置影响共用Browse，按相同3项小集复验通过。Unity编译0错误/0警告。原始Pipeline输出保留`Unity/Temp/pipeline_test_status.json`及本轮工具输出，后续自然更新；未复制或改写结果。

领取页重新查询失败撤下旧列表的补强后，仅再跑文件用例1/1 Passed，另外两项沿未变分支结果复用。查询阶段失败清理旧显示并保留刷新；领取发布本身失败不清除既有合法重试入口。一次域重载期间的editor_status预检查400未启动测试，Editor恢复ready后才执行本单项；未重复启动活跃作业。

| 任务／功能与Scenario | 实际用例／操作与断言 | 结果 |
|---|---|---|
| 1.1/1.2 真实持久计数、通知、筛选、详情与主动领取 | `EntryInboxActualFilesCountsFiltersClaimDestinationFailureCancelAndStaleCallbacks`：无repository的主页保持未查询且不初始化；自有文件三状态只计可领，通知无凭证不增数，查询不新增文件；分类/详情/旧确认不领取，明确提交后只新增一档并计数归零 | Passed |
| 1.1/1.2 失败/取消/关闭、重开与迟到 | 同一文件用例：真实坏凭证使查询失败而非零，前台刷新失败撤下旧结果及领取控件；移除自有坏文件后显式重试；查询中实际取消后晚到答复不覆盖；已关闭无领取控件，旧详情及重开回调无写入 | Passed |
| 1.1/1.2 Slot/修订、离线原选档与不自动开局 | 同一文件用例：原浏览取得当前header，明确跳转到原Solo检查后Host/Client仍为空；旧跳转无效；向同一Root接收器交付不同预期修订，实际header不符返回RecordChanged，不读取/创建餐厅 | Passed |
| 1.1/1.2 原已领取/已关闭页面兼容 | `CooperativeClaimsUiOfflineMenuShowsClaimedAndClosedFactsFromActualFiles`：原签名文件、已领及已关闭两状态，原反馈/返回仍可用；关闭详情返回列表后可读另一状态 | Passed |
| 1.1/1.2 正常Scenario：有效持久凭证回房间后双方领取 | `EntryInboxActualKcpDurableResultReturnRoomThenBothExplicitlyClaimAndLocateOriginalRecords`：普通Host/真实KCP双方验证身份，真实自然结算及明确本关不选装修后各自持久凭证；餐厅/房间入口显示1且尚无个人记录；先退役餐厅保留原房间，再分别明确领取、显示0，双方同一结果不同Slot，各仅一档，旧请求不重复 | Passed |
| 1.2 Host/Client去向权限 | 上述KCP用例：Host和Client均可从原记录页读自己的Slot/修订；Client没有房间读取或单机跳转控制，查看不新开餐厅，返回房间保留共享sender | Passed |
| 1.1 只读身份缺失Scenario | 文件用例移走自有密钥/公钥，查询失败且未补建；KCP用例移走Client自身信任，外来Host凭证仍不触发自动信任；还原自有资料后读取原记录恢复实际计数 | Passed |
| 2.1 普通Launcher540×960及原流程 | 自有临时repository用原签名API准备一有效凭证；主页待领1→查看详情时仍0档/无Host和Client→明确领取后1档/待领0→Slot/修订1去向→明确跳转原Solo完整检查仍未开局→明确确认恢复关卡2，原拿碗command1 Executed、手持非零、记录仍1档 | 已实际操作并检查画面 |

人工画面：`Unity/Assets/Temp/entry-inbox-mode-badge.png`、`entry-inbox-result-detail.png`、`entry-inbox-record-destination.png`、`entry-inbox-solo-inspection.png`，实际540×960可读可点。人工数据位于自有`Unity/Temp/entry-inbox-manual/53287660ba5a4be482eb8bffbca7a65a/`；准备文件复用本案夹具调用原签名API，不代表人工跑过KCP，KCP以上述UTF为证。有效身份路径界面在只读前置补强后行为未变，复用该人工结果；缺失分支已补跑UTF。本轮自有Play已停止。

首轮失败及修复：坏凭证的预期错误曾在CallLocalValueAsync读取回调中抛出，被所属请求观察器报告Console Error；改为先复制Error/Count值，await后在页面任务内处理。没有用LogAssert掩盖，修复后相关3项全通过。初期编译按既有ET约束补充依赖命名空间/receiver FriendOf，并改用显示版本观察保持单向依赖，无分析器抑制。

严谨程度：主要后果是把未持久/未知误称可领，或被动查询补建身份，以及旧页面越权加载。真实签名临时文件、身份缺失/取消/错误修订、KCP资格/权限与普通入口覆盖本次实际变化；原领取发布/ACK/整个Match关闭的未变行为沿[原领取验收](../2026-10-15-cooperative-result-claims/execution-notes.md)复用，不重跑无关套件。未验证真人理解、独立Player、物理LAN、Android真机及查询期间外部并发改写身份文件；出现设备遮挡、实际并发文件竞争或身份/发布规则变更再补对应范围。下文保留建案和实施前记录，当前结果以上方为准。

收尾：本案5份Markdown、21个本地引用及编码/行尾检查通过，严格校验通过。三个任务按CLI返回行逐项核对后勾选并刷新确认3/3、all_done；原18案当前14/18、42/54。

## 2026-10-16 实施前核对

前一案结算导航已完成13/18，本案apply仍0/3，尚未修改本案业务代码。已读规划上下文、progress-claims主规格和原Root事件、repository/Claims/Progress页面代码。

两项源事实需要在实施时保留：ProgressTicketSummary有SlotId但没有revision，已领去向应经原记录浏览请求读取当前header，不扩凭证格式或自行猜修订；原ProgressRepository()会在缺repository时创建本端Profile，因此后台待办统计不能无条件调用该getter初始化文件。复用已经存在的repository做只读Browse，无有效上下文时显示未查询，玩家显式打开原领取页后再沿原初始化入口；未知/查询失败不能显示零。

已领导航接原Progress记录选择页并按Slot定位，读取当前header说明修订/继续或完成状态；只查看不调用ReadProgressRecord或替玩家退役活动餐厅。计数归原ProductEntryPresentation的短寿命派生显示，使用Root限定的ProgressInboxChanged失效和原Browse请求，后台查询不能借用已关闭Grid HUD的scope，也不停止Root共用owner。相关UTF与普通540×960仍NotRun，待实现后记录。

## 2026-10-16 依据与决定

合作领取页已有可领取／已领取／已关闭列表，但入口只写合作进度领取，平时不显示本端待领数量，记录标题主要是关号和凭证短码。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryClaimsSystem.cs)、[主规格](../../../specs/progress-claims/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：复用 Root 的 ProgressInboxChanged 事件及原 Browse repository 请求；对原 owner 做可失效的显示计数，订阅限 ProductRoot，不复制持久 inbox。打开详情读已有 ProgressTicketSummary 可表达字段，已领取通过 Slot／revision 导航到选档，不自动加载或领档。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

本案涉及文件操作或运行退出的玩家判断，主要风险为展示通知先于持久化使玩家误认资格，或根事件影响其他运行。保持原发布、权限和生命周期实现，只改变入口或增加只读检查；使用真实临时文件／原控制链及 UI 运行验证，覆盖取消、迟到与失败，不扩大成任意断电或全设备试验。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 持久化前后、领取／已领／关闭、失败取消、重复打开和普通 KCP 结果入口；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate cooperative-progress-inbox --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
