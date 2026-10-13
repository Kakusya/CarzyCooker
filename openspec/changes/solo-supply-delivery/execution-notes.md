# Execution Notes

## 本轮续建与逐功能运行（2026-10-13）

已实现持续 `GridKitchenComponent → KitchenSupply(Component) → SupplyShipment(Child)`，同一 Restaurant 服务步处理采购与供应命令，原 Grid Tick 推进在途货物。暂停取消未执行采购／接收／拆份，已预留货物冻结；Completed 停钟，Continue 保留原 owner、余额、货物和实物。收货台按原 StationId 解析当前位置，满槽保留 Ready；到合法操作格才创建一次包装与真实材料，拆份只转移原 ContentItemId。

协议递增到14；`ZZZZZZSupply.proto` 正常 Proto2CS 追加10103–10108，原 opcode 保留；Snapshot追加47/48。SupplyCommandRequest经原ClientSession所属等待→LocalMessage.Peer ActorId→根ProcessInnerSender→ProductHost/Peer Ordered MailBox的SupplyPeerHandler→可信SupplySubmitRequest→authority ActorId/HostSession Ordered MailBox的SupplySubmitHandler→唯一服务步，再复用原Grid行走／到位互动。两段输入和答复沿原handler收尾，最终事实通过原独立Snapshot回ProductClient，安装纯值后Dispose DTO。未开放wire或新增监听。

| 任务／功能 | 本轮真实 PlayMode 运行 | 结果 |
| --- | --- | --- |
| 1.1 源定义与无免费物资 | SoloSupplyConfig原九项随新增owner运行 | 9/9 Passed |
| 1.2 唯一钟／暂停／Draining／Completed／换关 | SoloSupplyLifecyclePauseCompletedDrainingAndNextLevelKeepSameOwnerAndShipment | Passed；原owner、Shipment身份和剩余配额保留 |
| 2.1 预留／最新重放／冲突／不足 | SoloSupplyPurchaseRealMailboxReservesOnceAndRejectsShortageWithoutPartialState | Passed；接收前不生成实物，超额零写入 |
| 2.1 身份／内部入口／旧关／暂停取消 | SoloSupplyMessagesRejectInternalProtocolOldLevelAndCancelQueuedPurchaseOnPause | Passed |
| 2.2 满槽／腾空／移动收货台／重复 | SoloSupplyReceiveFullSlotKeepsCargoThenCreatesOnePackageAtMovedStation | Passed；包装和真实内容仅生成一次 |
| 2.3 拆份／满手／内容变化／空壳到位／不退款 | SoloSupplyPackageMovesSameMaterialsRejectsFullHandAndDiscardsOnlyEmptyShellAtTrash | 修正用例的合法暂存步骤后单项新运行1/1 Passed |
| 3.1 DTO回收后的独立供应／物品投影 | SoloSupplyProjectionIsIndependentAfterDtoDisposeAndNextLevelRetainsPackages | Passed |
| 2.3 含料包设施处理 | SoloSupplyPackageFilledDisposalRequiresTrashAndKeepsShellWithoutRefund | 新运行1/1 Passed；水池拒绝固体，垃圾桶只清内容并保留原包装，不退配额 |
| 3.1 实际HUD／重开 | SoloSupplyUiActualPurchaseReceiptPortionsAndEmptyShellWithManagedReopen | Passed；单击单请求，显示真实运输／待收货，重开复用原Mono且旧采购callback无效，原材料身份拆份后只空壳消失 |
| 3.1 旧关按钮 | SoloSupplyUiOldLevelPurchaseCallbackCannotWriteNextKitchen | 首轮从营业开始等自然结算超过45秒；按客流队列／每单200 Tick等待将用例上限调为75秒并补实际阶段诊断，未改业务；新运行1/1 Passed，旧采购callback不增加新关编号／货物 |
| 3.2 普通Launcher补货制作与跨关 | 下述四段真实指针调用 | Passed；原料耗尽、满槽保留、实物收货、原身份拆份烘烤及在途跨关均实际运行 |

首轮供应小集15项为14 Passed／1 Failed，失败是用例将原始番茄装进不兼容的碗（SoloSupplyTests.cs原145行），未暴露供货业务失败。改为原合法空台面暂存，单项新运行Passed，没有放宽领域兼容规则。原始输出保留`Unity/Temp/pipeline_test_status.json`和原Editor日志，未复制或改写失败结果。

正常协议生成使用现有.NET10运行时：`dotnet --roll-forward Major Tool.dll --AppType=Proto2CS --Console=1`，退出0。默认Tool.exe要求本机未装的.NET8，首次启动退出1，没有安装新运行时。首次编译ET0013因GridBroadcast反向调用GridKitchenSystem产生环，改为读Station身份；测试用例未存在的CleanupReply改用原CallProduct。最终recompile completed、failed=false、compilationFailed=false。

前两次测试由原Editor PID68792执行，日志`Unity/Temp/solo-five-changes-editor.log`。随后该进程确已退出，UI测试尚未启动且返回缺Pipeline实例；核对进程后重开主工程Editor PID59672，日志`Unity/Logs/solo-five-changes-editor-20261013.log`，没有清锁、删除恢复文件或操作第二工作区。后续验收以新运行终态为准。

风险与验证仍按下文Rigor and scope：覆盖有限配额、满槽、重复、身份转移、设施消费和跨关时钟；已有未改加工证据复用。当前接受仅Solo、无写盘／LAN／Player，UI与普通入口完成前不报告供应功能完整验收。

### 普通 Launcher 与最终恢复

Editor PID59672、主工程精确projectPath，普通editor_play启动已保存的Assets/Launcher.unity。临时`Unity/Temp/SoloSupplyAcceptance.cs`经现有run_script内存编译；只经真实PointerClick按钮发送请求并读独立投影，没有改领域状态或直接推进业务钟。同一世界`deadf32d2d5a4ba5ae6a5a5c6f98f862`：

| 实际操作 | 结果 |
| --- | --- |
| ExhaustAndOrder | 先烘烤并到垃圾桶消耗唯一初始面包，再持原碗占满收货台；采购面包后实际到货，接收ReceivingFull保持6件物品及Ready货物，配额只减到4。executeMs=10904、success=true |
| ReceiveAndBake | 原碗归位后接收包装1284089574326583；拆出的原面包身份真实烘烤／装盘为吐司1284089580618169，另一份1284089574326585仍在原包装，配额4。executeMs=7168、success=true |
| StartAndOrderForCarry | 真实选择buff／开始营业，在最后三个营业Tick采购番茄，货物1284089614173051处于InTransit、0/1600。executeMs=20144、success=true |
| Carry | 真实顾客自然收口至Completed，货物进度524冻结；真实下一关将原Level1284089530286107换成1284089642485143，同一厨房／货物接续525，9件实物身份与配额不补满。executeMs=5084、success=true |

默认40 Tick运输太短，不能在真实前厅排空期间保持在途；仅此代表性验收将源SupplyConfig.xlsx的H4设为1600，保留全部其它ZIP部件，真实五目标Check／正常导出5/5及四语言均成功，未改生成物。结束本轮自有Play后恢复H4=40，再正常Check／导出均通过。新普通入口首次仍读Editor已导入的1600缓存，经AssetDatabase强制同步刷新后再次进入，实际Tables读40、三源、初始7件物品，证明恢复已被入口消费。未把临时参数当正式平衡。

重开Editor的GameView原为Free Aspect；540×960输出只缩放图像不能证明实际纵屏。用现有Pipeline内存脚本选择真正固定540×960后，读Screen.width=540／height=960并查看原生screen图`Unity/Assets/Temp/pipeline-screenshots/solo-supply-delivery-portrait.png`，采购按钮在原滚动目标区，保留原样机标签密度。截图不替代行为结果。普通入口最终Console groundTruth无编译失败、consoleErrors=0；自有Play已结束，未停其它工作区或其它用户入口。

### 验证收口

任务7/7；七项新增requirement均有对应实现、行为UTF和上述必要入口证据。完成性／正确性／设计一致性均已逐点核对，无待解决功能项；16项供应业务／配置的最新有效结果与2项真实UI均Passed，Console覆盖完整，按新增行为复用各次结果，没有为收尾再跑全套。未验证项沿范围保留：仅本机Solo Editor，无wire扩展、产品写盘、LAN／公网／Player／AOT；接入联机、存档或改材料／设施合同再补相关小集。不提交、不归档本案。

## 早期配置阶段记录（2026-10-13，已由本轮续建推进）

用户已授权完成五案，前置九案及设施清理已有逐功能运行证据。当前只完成任务1.1的源、来源／菜单校验和相关运行；Supply／Shipment owner、采购／接收／拆份、投影／UI与普通 Launcher补货链尚未实施，仍 NotRun。

新增 `Product/SupplyConfig.xlsx`，按实际 `__tables__.xlsx` 的 B/C/D/E/H 列注册 `DTSupplyConfig`／`DRSupplyConfig`／读取源结构／输入路径／c,s,e。包装定义12、13、14复用 Kind=2、Form=3、容量2、对应原料兼容和可丢弃空壳；默认番茄、鸡蛋、面包各5包／包内2份／运输40 Tick为可调样机参数。具名收货点复用原交接台布局4、单槽与原坐标，后续按工位实例解析当前位置；未增加初始包装或材料。

Python标准库只改相关sheet XML，保留其他ZIP部件／样式／公式／验证；未安装工作簿工具。真实五目标 Check 5/5、正常导出5/5与四语言导出均成功，退出0；Client／ClientServer／Editor DR／DT、bytes／JSON及Config/Luban来自正常生成，没有手改生成物。

`SupplyConfig.Validate`完整校验有限正参数、非加工产物、包装兼容／容量／空壳规则和收货Counter。`GridDishConfig.ValidateAllMenus`按真实供应定义扩展Raw可生产来源，校验不生成食物；纯隔离菜单入口仍可显式使用初始来源。旧bootstrap的缺面包来源用例同时移除初始和补货来源，不再把合法补货配置误判为失败。

| 任务／功能 | 实际运行 | 结果 |
| --- | --- | --- |
| 1.1 合法源／包装／具名单槽／没有免费物资 | PlayMode SoloSupplyConfigActualSourcesPackagesAndMenuClosureCreateNoFreeFood：移除三种初始原料后菜单合法，初始化只有原4件餐具／锅，无食品、半成品或包 | Passed |
| 1.1 必需来源／引用／参数／能力拒绝 | SoloSupplyConfigInvalidSourceOrMenuRejectsBeforeCreatingHost：缺来源、半成品、错误包装、包量、配额、运输、收货位置、烤箱能力八参数 | 8/8 Passed，Host树创建前拒绝 |
| 1.1 受影响菜单预检 | SoloDishGraphActualBootstrapRejectsBeforeCreatingWorld：raw／oven／container／cycle | 4/4 Passed |
| 1.2–3.2 供应业务／UI／真实入口 | 尚未实施和运行 | NotRun，任务不勾选 |

上述两个真实运行工具均退出0，Failed／Skipped／Inconclusive=0，Console覆盖完整。原生输出留在 `Unity/Temp/pipeline_test_status.json`，Editor日志在 `Unity/Temp/solo-five-changes-editor.log`；不建证据或状态副本，不以导表／编译冒充采购功能验收。原配方、清理和发送器未改变的结果可复用，新增owner／命令／包装转移必须继续补验。

## Planning status

2026-10-12 用户授权先写五个 change；本轮只创建规划，未实施、未导表、未编译，Unity/UTF/包体均 NotRun。以现有 Solo 九案按契约交付为规划假设；apply 前核对前置的逐功能运行证据，不修改或替其它工作勾任务。

## Rigor and scope

主要风险是满槽吞货、重复生成或跨关补满余额，影响原料与连续经营。有限三种原料、真实包装的数量/身份断言，加同一 Launcher 耗尽补货与在途跨关足够；不扩展价格、无限来源或全菜矩阵。可复用未改变的两菜加工及 sender 证据，受菜单来源/新布局影响部分必须重验。接受样机配额/包量/Tick、当前 Solo；接网络/存档或新材料路径时补对应验收。

## Acceptance mapping

以下名称是拟补用例范围，不是已经发现或运行的用例；实施时发现真实 PlayMode 过滤并记录原始结果。结果留现有工具原位，不复制证据包。

| 任务 | 功能 | 对应运行 | 结果 |
| --- | --- | --- | --- |
| 1.1–1.2 | 源闭包、owner/阶段/钟/跨关 | 拟 SoloSupplyConfig/Lifecycle | NotRun |
| 2.1–2.3 | 预留、满槽、唯一包装/拆份、垃圾桶到位与损耗 | 拟 SoloSupplyPurchase/Receive/Package | NotRun |
| 3.1 | 同版 UI与生命周期 | 实际 UI UTF | NotRun |
| 3.2 | 耗尽再制作、在途换关 | 普通 Launcher + 相关小集 | NotRun |

## Planning validation

2026-10-12 设施清理规则交接：液体到水池、固体/空包装到垃圾桶，必须到真实四邻操作格；同步本案的相关规格、设计及验收任务。`openspec validate solo-supply-delivery --strict` 再次valid、退出码0，三案与两份产品目标共18份Markdown引用/UTF-8/空白检查通过。仅规划更新，新增设施行为仍NotRun。

2026-10-12：`openspec validate solo-supply-delivery --strict` 退出码 0，返回 valid；proposal/specs/design/tasks 齐全。本轮五案共 26 份 Markdown 的 UTF-8、行尾空白、本地引用及任务未勾选检查通过。上述检查仅证明规划格式/引用有效，功能运行仍全部 NotRun。

## 2026-10-13 本地提交收尾

用户确认授权后，本案实现随本地main提交906829ef保存，原验收记录随4bfe1a2c保存；此前“未提交”描述保留其当时阶段语义。源码与行为未再改变，逐功能UTF／普通Launcher结果直接复用。本轮五案均已实际验收；归档和push未执行。
