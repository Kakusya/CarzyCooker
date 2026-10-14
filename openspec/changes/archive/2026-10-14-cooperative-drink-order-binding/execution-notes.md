# 执行说明

## 2026-10-14 实施状态

用户已授权完成三个 change。以下为最新实施与运行事实；文末 2026-10-13 的 NotRun 为规划历史。

协议 22 公开 Grid 18–21（StartJuice／PortionJuice／BindDrink／UnbindDrink），逐字段值复制原上下文及 ExpectedOrderId，内部推进消息仍拒绝。新增合作 3→4→4 关卡、橙子／杯有限来源，旧两菜及 Solo 保持。Luban Check 与正常导出各 5/5 成功，只改源表及派生数据；Proto2CS 正常生成成功。3 项 CoopDrink 核心 UTF Passed（首轮最后份数夹具在非空台面注入失败，改空台面后该单例 Passed；其余两项复用已通过结果）：远端加工锁定／搬壶／传杯、独立份数、最后份竞争、贴单拒绝保物／解绑／交付、杯落下与原成员空手恢复。真实采购到清桌 UI 首轮因测试采购按钮名称错误 Failed，正在修正；双 Editor 未运行。

运行模式为 PlayMode，过滤为上文实际前缀／FullName，原始输出在 `Unity/Temp/pipeline_test_status.json`，原生 Editor 日志原位保留，均未复制或改写。Passed 运行的 Console 覆盖由 AutoTesting 核对；失败结果不被后续通过覆盖为成功。

接受的未验收范围：独立 Player／手机／LAN／公网／持久凭证／Host 迁移在本案范围外。三个 change 的双 Editor 增量仍是必须完成项，不缩成同 Editor；主工作区提交与第二工作区同步尚需对应授权。没有运行证据的任务不勾选。

## 本轮状态与决策

2026-10-13：用户授权“由浅到深写成 change”，并要求真机测试最后。本案目前仅为规划；所有 tasks 未勾选，未实现业务、未执行 UTF、未启动 Unity/Player、未构建或操作第二工作区。第三关保存完成后返回选档、无装修和第四关；橙汁加工锁壶、完成可搬壶分杯、产量/耗时配表，已获用户确认。具体相关合同以本案 specs/design 为准。

## 相称的严谨程度

远端字段漏拷贝、杯／订单重复消费和旧绑定是主要风险。用真实 KCP 请求加两端真实输入，复用 Solo 配方算法证据。 所选验收按下表覆盖本次改变的行为，已有结果只有在实现/行为未变且明确对应该点时才复用，并填写原执行记录路径。不因提案完成或阶段切换重跑全仓；测试失败、空集、Skipped、缺输出均不算成功。

接受的未验证范围：本轮所有运行行为 NotRun；除最后设备层外，手机/独立 Player/LAN 仍待对应案。没有玩家、设备或已授权运行条件时保留相关任务未完成。若改到共享 owner、文件格式、协议、行为断言或出现实际回归，则补对应局部验证，不机械放大为完整矩阵。

## 任务与验收映射

| 任务 | 功能／交付 | 拟使用的最小验收 | 本轮结果 |
| --- | --- | --- | --- |
| 1.1 | 远端入口 | 正常生成后真实 KCP 测试所有公开饮品字段及内部动作拒绝，记录白名单。 | NotRun（规划阶段） |
| 2.1 | 双方投影 | UI/投影用例验证锁定、份数、杯和绑定独立复制、就绪与旧关隔离，更新双方操作说明。 | NotRun（规划阶段） |
| 3.1 | 竞争集成 | 真实 KCP 小集验证最后一杯、原子重绑、错单保物和断线落物；只有已接入恢复时才验原成员恢复。 | NotRun（规划阶段） |
| 4.1 | 双端全链 | 普通双端与双 Editor 完成新增饮品链，复用 Solo 制作和未变合作结果，明确是否含恢复。 | NotRun（规划阶段） |

UTF 执行阶段使用现有 Tools/AutoTesting，先发现模式与方法再记录实际过滤范围；涉及产品消息/UI 的候选集按 PlayMode 核对，目录名不能证明模式。原始输出保留原位，只在此引用运行路径及功能对应关系。纯正文任务只检查文本、链接与严格格式，不制造占位 UTF。

## 实施时回填

每项记录“实际方法或真实操作 → 原输出路径 → Passed/Failed/NotRun/Skipped → 未覆盖点”。文档格式验证只证明规划可解析，不证明功能已完成。当前不预填执行日期、通过计数或不存在的测试结果。

## 本轮规划检查

2026-10-13：`openspec validate cooperative-drink-order-binding --strict --no-interactive` 退出码 0，规划格式通过。当前相关文档检查共 80 份 Markdown，链接缺失 0、编码/尾随空格/原规格场景遗漏 0；新案依赖无环。以上均为静态规划检查，表内功能仍全部 NotRun，任务未勾选。

## 最新逐任务映射

| 任务 | 实际运行 | 结果 |
| --- | --- | --- |
| 1.1 | CoopDrink公开动作＋CoopCoreWire 4项＋WireInternalMessageCannotJoinOrTakeLocalIdentity | Passed；公开字段／回执、未知动作／内部转交拒绝与协议生成 |
| 2.1 | LocksJugCopiesPortionsTransfersCupAndResumesEmptyHanded + AtomicBindingWrongOrder最新旧快照分支 + 真实CoopDrinkUi | Passed；独立锁定／份数／杯／绑定、确认门槛及旧关隔离 |
| 3.1 | 上述制作／落物与恢复 + LastPortionRace + AtomicRealKcpRebind + WrongDishRealKcpFoodOrder | Passed；最后份／原子重绑／错菜保物与空手接续 |
| 4.1 | CoopDrinkUiOrdinaryPurchaseSplitPreparationBindingDeliveryAndClear；随后双 Editor | 普通双端及真实双Editor全链Passed；详见末尾逐任务记录 |

普通 UI 失败诊断已修正采购名称（空杯补货），并发现满固体包装倾倒后保留空包装，须再丢空包才能空手拆杯；这是原清理合同，未放宽规则。新增测试专属远端 Fiber 在 UTF teardown 前回收，避免嵌套迭代器失败遗留 view／Fiber 的二次异常。原错误仍保留；每次修复新建运行。

普通饮品 UI 最新运行行为 1/1 Passed，但 AutoTesting 给 Conclusion: Error：50秒流程的逐帧 GridWorldSnapshot 调试日志超出2000条 Console 缓冲，覆盖不完整，未按完整通过记账。已在既有 LogMsg 高频排除集合中禁止展开该快照，动作消息、Warning／Error 保持；须新运行覆盖完整后才能勾选。

CoopDrinkAtomicRealKcpRebindRejectsOccupiedOrderAndPreservesBothCups 新运行 Passed：远端从一张合法订单原子换到另一张，再验证占用订单换绑拒绝仍保留两杯原内容／原关系。原配方、清理和旧关过滤未改，复用原首饮 execution-notes 中适用于当前实现的对应行为，不重跑全套。

关闭高频 Snapshot 调试展开后的新运行：`CoopDrinkUiOrdinaryPurchaseSplitPreparationBindingDeliveryAndClear` 1/1 Passed，Console 覆盖完整，编排退出0。真实 Host Launcher 菜单3，Remote 实际采购橙子／杯、接收／拆份，Host 用本地消息切配所购橙子，Client 榨汁／盛所购杯／贴单／交付／清桌；原一次性杯消失、不创建Wash、两个来源库存各减一。余料与空包装各自按原规则处理。与4核心UTF合计5个唯一新增用例已有效通过；双Editor仍NotRun。

最小相关回归当前结果：WireInternalMessageCannotJoinOrTakeLocalIdentity 1/1 Passed；CoopCoreWire 4/4 Passed（未知动作、旧协议／内部Submit、物理RPC与业务编号关联）；SoloRenovationConfig 6/6 Passed（新旧池与非法配置无Host建树）。所有 Console 覆盖完整、编排退出0；未扩大到全餐厅／全仓。

最终局部补验：CoopDrinkWrongDishRealKcpFoodOrderKeepsFinishedCupAndScore Passed，真实远端向合法食物订单贴单／交付橙汁均WrongDish拒绝，原杯及饮品不消失、订单仍Waiting、得分／成功数仍0。AtomicBindingWrongOrderKeepsCupAndRemoteServesOriginalContents 追加旧快照分支的新运行 Failed：前厅仍 Draining，尚未到快照断言。旧贴单／交付部分此前已通过，新增旧关分支未验收，不能把该新运行写为Passed；任务2.1保持未完成。

旧关快照补验失败诊断：仍有 Clear/Available，伙伴调度只认领Ask／Wash，测试未执行玩家清桌。修正为真实远端认领清桌，并仅给此隔离用例单个顾客；多订单并发仍由另一个真实KCP用例覆盖。清桌后自然完成已到达，但旧零星装修夹具错误要求0星（实际成功交付10分／1星），再次Failed。现直接断言真实Completed、装修准备及1星，再检查旧快照，不增加等待预算或改业务规则。以上失败不算Passed。

旧快照最终有效运行：`CoopDrinkRealKcpAtomicBindingWrongOrderKeepsCupAndRemoteServesOriginalContents` 新运行1/1 Passed，Console覆盖完整、编排退出0。远端完成真实清桌并自然结算10分／1星，合法Continue后上一关含贴单杯的高版本快照被拒绝，当前Level／版本保持且不复活旧杯／订单。任务2.1据此重新勾选；任务1.1–3.1为3/4，4.1双Editor仍NotRun。5核心＋1真实HUD为6个唯一新UTF已有效覆盖；历史失败保留。

## 本轮本机核对结论（双 Editor 之前的历史记录）

同步前置补充：首个候选 ebb6d15f 的 Git 内存预检发现 AGENTS.md、Book/快速开始.md、CarzyCooker当前进度.md 冲突；没有实际 cherry-pick 或修改第二工作区。候选清单须收敛交接方式后才能执行，提交／同步授权仍待明确答复。详细预检见 [成员恢复记录](../2026-10-14-cooperative-member-resume/execution-notes.md#同步预检补充)。本案4.1保持 NotRun。

本轮新增22个唯一功能UTF有适用当前实现的Passed证据（装修4核心＋1UI，恢复10核心＋1UI，饮品5核心＋1UI）；11个直接相关回归Passed。严格OpenSpec验证3个change及36主规格有效，diff空白与18份当前文档链接／UTF-8检查通过。未触碰ProjectSettings／Packages或用户已有未跟踪截图。目标仍未完成：每案最后一项双EditorNotRun，未提交、未同步、未归档、未push。

拟收口动作：只将本轮代码／协议源和正常生成结果／Excel与派生数据／行为UTF／对应规格及说明提交到本地main，标题“接通合作装修、成员恢复与饮品贴单”，基于当前main bd0badff。第二工作区当前fed20ee2（协议17）、工作树干净，保留6项执行技能；必要前置候选ebb6d15f（协议源）、9c435906（消息边界）、bd0badff（已交付饮品／升级／UI）及本轮新提交，明确清单在执行前复核。纯文档／截图提交不默认加入；不做merge／pull／DLL复制。最新主工作区规则优先于第二端旧放宽规则，源码冲突中止并回报，不在执行端开发。main新提交仍须用户授权；双Editor验收保持原目标，不降格为同Editor通过。

## 双 Editor 验收开始

2026-10-14：用户明确要求进行双Editor验收，按此前已说明的前置条件执行本次功能的本地提交、指定同步和验收，不扩大到push或归档。排除其他逐帧优化改动及已有截图。补齐05d3c79e、b2848b0d、4bfe1a2c、6d56bd0d等历史文档依赖后，候选序列的临时索引预检仅有Markdown冲突；运行源码、协议、Excel和生成配置相对main无差异。同步中仍禁止第二端修源码冲突；实际运行结论待回填。

## 双 Editor 最终验收（2026-10-14）

本轮使用真实普通 Launcher：主工作区 Host（PID72252、Pipeline7800），第二工作区 Client（PID87600、Pipeline7801）。业务 KCP 实际监听 127.0.0.1:57766；工具端口不作为业务端口。主端代码基线 main 32f2c9f1；第二端指定同步结果239882ce，短执行指令维护后aab4a08d。必要清单顺序为05d3c79e、b2848b0d、4bfe1a2c、6d56bd0d、ebb6d15f、389cc1e0、9c435906、54e5804f、bd0badff、32f2c9f1。前四项及389/54补齐历史文档依赖；临时索引预检仅Markdown冲突，明确采用Git theirs自动处理文档后恢复短AGENTS。运行源码／协议／Excel／配置与main一致，第二端没有开发或修源码冲突，仍只有6项执行技能，工作树干净。

两端产品身份为GDK/GameDevelopmentKit与GDK/GameDevelopmentKit-NetTest，persistentDataPath各自独立，场景均Assets/Launcher.unity。通过Pipeline run_script在既有Fiber同步上下文操作真实受管按钮和地图；没有临时远端Fiber、业务事实注入、暂停计时或修改配置。运行回执保留于本会话CLI原输出；原生日志主端 `C:/Users/Administrator/AppData/Local/Unity/Editor/Editor.log`，第二端 `D:/MyWork/CarzyCooker-NetTest/Unity/Logs/Multiplayer-Peer-Editor.log`。实际GameView帧捕获已检查：[Host](../../../../Unity/Temp/pipeline-screenshots/coop-three-dual-game-20261014.png)、[Client](D:/MyWork/CarzyCooker-NetTest/Unity/Temp/pipeline-screenshots/coop-three-dual-game-20261014.png)。Pipeline默认screenshot只离屏渲染MainCamera，得到黑图，未将其当作画面通过；改经同一Pipeline在帧末读取实际GameView，确认Host控制与Client只读呈现。

Console如实区分：两端compilationFailed=false；主端原生Console当前Error=0，Pipeline历史缓冲含2条本轮此前已修复的平台取值编译错误。第二端保留1次 `UnityConnectWebRequestException: Token Exchange failed due a failure with the web request.`，属于Editor账号服务，未阻止真实Join、恢复、玩法或清理；本次未修复该账号服务异常，不声称Console全绿。已有22项新增UTF和11项相关回归结果完整覆盖未变行为，本轮不重跑。Player/AOT、Android、两台物理PC LAN、进程重启恢复、Host迁移及联机存档仍未验收／不在范围内。

两端本轮Play已通过editor_stop正常结束，Editor与长期工作区保留；KCP端口57766已释放。以下逐任务结论取代上方实施阶段的NotRun／待授权历史状态。三案实施及约定验收合计13/13，尚未归档；其他逐帧优化改动及已有截图保持原样。

| 任务 | 双Editor实际行为 | 结果 |
| --- | --- | --- |
| 4.1 | 恢复后的Client接收／拆分Host所购橙子，放切配台；Host真实HUD切配，Client装壶／榨汁并自行采购／接收／拆杯、盛装与贴单。新关杯1284192007619127绑定订单1284192201606915，经真实拿杯／前往／交付／清桌按钮完成；两端独立断言订单Served、成功数1、原杯消失、Clear Completed、餐桌1 Free、无杯Wash、来源4/5库存各4。自然结算10分1星 | Passed；本次明确包含手动恢复后的制作与交付，饮品案4/4 |

失败保留：第一次杯1284192007619126贴单后，验收驱动准备耗时跨过营业结束，前往被LevelCompleted取消，未算交付通过。新关先备好再开店，第二次正确交付后，驱动错误按已离座顾客的TableStationId查找清桌任务，返回等待超时；原输出明确Clear/Available且Served=1。改为按FrontWork.StationId定位，真实点清桌后两端新核对均Passed。未改玩法源码、未延长营业或伪造订单，不改写上述失败回执。

## 归档收口（2026-10-14）

用户明确授权归档、commit和push。本案全部任务及双Editor验收完成；delta需求与场景已逐项核对主规格，复用原有功能证据，不重跑测试。三个change各严格验证通过，36份主规格严格验证通过。使用CLI --skip-specs归档仅避免重复应用已同步delta，不是跳过需求核对。归档后同步正文引用、保留历史失败和设备未验收范围；原生日志、缓存及其他任务现场不纳入提交。
