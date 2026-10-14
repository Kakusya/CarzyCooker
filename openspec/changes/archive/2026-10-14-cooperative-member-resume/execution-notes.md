# 执行说明

## 2026-10-14 实施状态

用户已授权完成三个 change。以下为最新实施与运行事实；文末 2026-10-13 的 NotRun 为规划历史。

恢复凭证归 Match→Participant，随机 32 字节，只在本轮有效；Client 凭证／endpoint／尝试号归 ProductRuntime→ProductResumeIntent，重建原 ProductClient Scene 内 ClientSession／Session，旧 EntityRef 与旧投影失效。失联不停止公共 sender，Stop 才清恢复意图。4 项 CoopMemberResume 核心 UTF 与 CoopMemberResumeUiRealDisconnectManualButtonReopenAndRetiredInput 均 Passed，覆盖原成员／当前关／空手、迟到快照、重复尝试／双连接、错误凭证／Match／Leave 撤销、本地取消、HUD 重开／池复用／旧回调。通用 LogMsg 发现会展开 DTO，已排除所有含凭证的 Join／Control／Resume 消息，待补行为断言。回执丢失、Host 关闭与双 Editor 尚待补验。

运行模式为 PlayMode，过滤为上文实际前缀／FullName，原始输出在 `Unity/Temp/pipeline_test_status.json`，原生 Editor 日志原位保留，均未复制或改写。Passed 运行的 Console 覆盖由 AutoTesting 核对；失败结果不被后续通过覆盖为成功。

接受的未验收范围：独立 Player／手机／LAN／公网／持久凭证／Host 迁移在本案范围外。三个 change 的双 Editor 增量仍是必须完成项，不缩成同 Editor；主工作区提交与第二工作区同步尚需对应授权。没有运行证据的任务不勾选。

## 本轮状态与决策

2026-10-13：用户授权“由浅到深写成 change”，并要求真机测试最后。本案目前仅为规划；所有 tasks 未勾选，未实现业务、未执行 UTF、未启动 Unity/Player、未构建或操作第二工作区。第三关保存完成后返回选档、无装修和第四关；橙汁加工锁壶、完成可搬壶分杯、产量/耗时配表，已获用户确认。具体相关合同以本案 specs/design 为准。

## 相称的严谨程度

恢复入口涉及冒认成员、重复绑定和迟到回复。用随机能力凭证、Host 单写提交及代表性真实 KCP 负例和失联恢复覆盖；不扩成跨设备身份系统。 所选验收按下表覆盖本次改变的行为，已有结果只有在实现/行为未变且明确对应该点时才复用，并填写原执行记录路径。不因提案完成或阶段切换重跑全仓；测试失败、空集、Skipped、缺输出均不算成功。

接受的未验证范围：本轮所有运行行为 NotRun；除最后设备层外，手机/独立 Player/LAN 仍待对应案。没有玩家、设备或已授权运行条件时保留相关任务未完成。若改到共享 owner、文件格式、协议、行为断言或出现实际回归，则补对应局部验证，不机械放大为完整矩阵。

## 任务与验收映射

| 任务 | 功能／交付 | 拟使用的最小验收 | 本轮结果 |
| --- | --- | --- | --- |
| 1.1 | 凭证合同 | 生成协议后真实接入测试错误 Match/随机凭证/Leave 撤销/自报编号拒绝，检查凭证不落盘不入日志，记录 owner。 | NotRun（规划阶段） |
| 2.1 | 绑定切换 | 真实 KCP 用例断言成员数不增、代次递增、单一绑定和不误停公共发送器；记录失联/Stop 分界。 | NotRun（规划阶段） |
| 3.1 | 就绪和 UI | 恢复当前关空手，完整投影前禁输入，取消/超时不假报 Leave；UI 关闭重开无旧回调，更新操作说明。 | NotRun（规划阶段） |
| 4.1 | 恢复负例集成 | 运行重复尝试、双连接竞争、丢回执后新尝试、旧请求/快照与 Host 关闭用例；只清自有资源。 | NotRun（规划阶段） |
| 5.1 | 双 Editor 恢复 | 实际断开 Client 接入再手动恢复，记录原成员/当前关/物品和其他成员连续运行；进程重启明确范围外。 | NotRun（规划阶段） |

UTF 执行阶段使用现有 Tools/AutoTesting，先发现模式与方法再记录实际过滤范围；涉及产品消息/UI 的候选集按 PlayMode 核对，目录名不能证明模式。原始输出保留原位，只在此引用运行路径及功能对应关系。纯正文任务只检查文本、链接与严格格式，不制造占位 UTF。

## 实施时回填

每项记录“实际方法或真实操作 → 原输出路径 → Passed/Failed/NotRun/Skipped → 未覆盖点”。文档格式验证只证明规划可解析，不证明功能已完成。当前不预填执行日期、通过计数或不存在的测试结果。

## 本轮规划检查

2026-10-13：`openspec validate cooperative-member-resume --strict --no-interactive` 退出码 0，规划格式通过。当前相关文档检查共 80 份 Markdown，链接缺失 0、编码/尾随空格/原规格场景遗漏 0；新案依赖无环。以上均为静态规划检查，表内功能仍全部 NotRun，任务未勾选。

## 最新逐任务映射

| 任务 | 实际运行 | 结果 |
| --- | --- | --- |
| 1.1 | RejectsWrongMatchRandomCredentialAndRevokedLeave + BoundaryCredentialMessages + OtherPlatform + WireInternalMessageCannotJoinOrTakeLocalIdentity | Passed；凭证不落盘、日志屏蔽、错误Match／平台／凭证及Leave撤销 |
| 2.1 | CurrentLevelEmptyHandsOneMemberAndOldProjectionRejected／DuplicateAttemptAndTwoConnectionsKeepOneBinding | Passed；同成员、新代次、唯一角色，不停公共 sender |
| 3.1 | 上述当前关／投影门槛 + LocalCancel + BoundaryTimeout + CoopMemberResumeUiRealDisconnectManualButtonReopenAndRetiredInput | Passed；空手、完整就绪、超时未知、本地取消／重开／旧回调 |
| 4.1 | CoopMemberResume 最新11项小集，含重复、竞争、较早连接、丢回执、旧请求／快照与Host关闭 | Passed；只释放所属会话／Fiber |
| 5.1 | 双 Editor 真实失联与恢复，详见末尾逐任务记录 | Passed；原成员／当前关／落物与其他成员连续运行 |

首版不建立账号或跨设备凭证，不扩大为自动重连。UI 保留原受管 owner，原 Session 销毁与新 ClientSession 实例重建的边界各有真实运行证据。

补验首轮 4 项结果：凭证消息日志屏蔽、回执未读即断线后新尝试、旧业务请求拒绝和 Host 关闭均 Passed；超时反馈被延后 Session.Destroy 覆盖为 ProductConnectionClosed，修复已失联时可恢复 FailClient 重入后，该超时单例新运行 Passed。不可恢复的 Host 关闭仍能撤销资格，不吞此通知。

串行恢复代次修正后的完整相关小集：`--mode playmode --filter_type testName --filter CoopMemberResume`，10/10 Passed，Console 覆盖完整，编排退出0。9核心＋1真实Launcher/HUD包括：较早未绑定连接晚提交仍取得更大代次、重复／双连接／丢回执／旧请求与快照、错误凭证／Match／Leave、Host关闭、超时未知反馈、本地取消以及HUD关闭重开／旧回调。旧4项及原UI结果由此最新运行覆盖；其余无关套件不扩大。

按 design 补实际平台核验后，最新 `CoopMemberResume` 小集 11/11 Passed、Console 完整、编排退出0：其中10核心＋1真实受管Launcher。新增 RealKcpOtherPlatformCannotUseValidCredential 验证合法凭证声明另一生态仍被拒绝且原角色／名单不变。平台事实由 Game.RuntimeInstanceContext 提供，ET Model 不引用 UnityEngine；仅恢复核验，不宣称 Android／完整跨平台 Join 或 LAN 已验收。

最小相关回归当前结果：WireInternalMessageCannotJoinOrTakeLocalIdentity 1/1 Passed；CoopCoreWire 4/4 Passed（未知动作、旧协议／内部Submit、物理RPC与业务编号关联）；SoloRenovationConfig 6/6 Passed（新旧池与非法配置无Host建树）。所有 Console 覆盖完整、编排退出0；未扩大到全餐厅／全仓。

任务1.1–4.1对应的实现与当前运行证据均已覆盖，已勾选4/5；尚缺5.1真实双Editor。WireInternalMessageCannotJoinOrTakeLocalIdentity也已在当前协议运行Passed，证明自报可信Actor／成员绑定字段不能从wire接管身份；凭证只由运行内owner持有，未增加任何保存读取路径。

## 本轮本机核对结论（双 Editor 之前的历史记录）

本轮新增22个唯一功能UTF有适用当前实现的Passed证据（装修4核心＋1UI，恢复10核心＋1UI，饮品5核心＋1UI）；11个直接相关回归Passed。严格OpenSpec验证3个change及36主规格有效，diff空白与18份当前文档链接／UTF-8检查通过。未触碰ProjectSettings／Packages或用户已有未跟踪截图。目标仍未完成：每案最后一项双EditorNotRun，未提交、未同步、未归档、未push。

拟收口动作：只将本轮代码／协议源和正常生成结果／Excel与派生数据／行为UTF／对应规格及说明提交到本地main，标题“接通合作装修、成员恢复与饮品贴单”，基于当前main bd0badff。第二工作区当前fed20ee2（协议17）、工作树干净，保留6项执行技能；必要前置候选ebb6d15f（协议源）、9c435906（消息边界）、bd0badff（已交付饮品／升级／UI）及本轮新提交，明确清单在执行前复核。纯文档／截图提交不默认加入；不做merge／pull／DLL复制。最新主工作区规则优先于第二端旧放宽规则，源码冲突中止并回报，不在执行端开发。main新提交仍须用户授权；双Editor验收保持原目标，不降格为同Editor通过。

### 同步预检补充

2026-10-14：仅使用 `git merge-tree --write-tree --merge-base=ebb6d15f^ multiplayer-baseline ebb6d15f` 预检首个候选，不改分支、索引或第二工作区文件。预检返回冲突：AGENTS.md、Book/快速开始.md、CarzyCooker当前进度.md。故以上仍是候选清单，不能直接作为可执行同步指令；执行前须由主工作区收敛文档交接方式，遵守第二端不开发、不修源码冲突的边界。第二端真实运行仍 NotRun。

本地提交／指定同步授权问题已提出但尚未收到明确答复；自动目标续跑不作为提交授权。主工作区另有逐帧优化相关 AGENTS、audit 和 reduce-per-frame-allocations 改动，属于其他工作，保留并排除在本次拟提交范围之外。未因此重跑已有充分覆盖的功能测试。

## 双 Editor 验收开始

2026-10-14：用户明确要求进行双Editor验收，按此前已说明的前置条件执行本次功能的本地提交、指定同步和验收，不扩大到push或归档。排除其他逐帧优化改动及已有截图。补齐05d3c79e、b2848b0d、4bfe1a2c、6d56bd0d等历史文档依赖后，候选序列的临时索引预检仅有Markdown冲突；运行源码、协议、Excel和生成配置相对main无差异。同步中仍禁止第二端修源码冲突；实际运行结论待回填。

## 双 Editor 最终验收（2026-10-14）

本轮使用真实普通 Launcher：主工作区 Host（PID72252、Pipeline7800），第二工作区 Client（PID87600、Pipeline7801）。业务 KCP 实际监听 127.0.0.1:57766；工具端口不作为业务端口。主端代码基线 main 32f2c9f1；第二端指定同步结果239882ce，短执行指令维护后aab4a08d。必要清单顺序为05d3c79e、b2848b0d、4bfe1a2c、6d56bd0d、ebb6d15f、389cc1e0、9c435906、54e5804f、bd0badff、32f2c9f1。前四项及389/54补齐历史文档依赖；临时索引预检仅Markdown冲突，明确采用Git theirs自动处理文档后恢复短AGENTS。运行源码／协议／Excel／配置与main一致，第二端没有开发或修源码冲突，仍只有6项执行技能，工作树干净。

两端产品身份为GDK/GameDevelopmentKit与GDK/GameDevelopmentKit-NetTest，persistentDataPath各自独立，场景均Assets/Launcher.unity。通过Pipeline run_script在既有Fiber同步上下文操作真实受管按钮和地图；没有临时远端Fiber、业务事实注入、暂停计时或修改配置。运行回执保留于本会话CLI原输出；原生日志主端 `C:/Users/Administrator/AppData/Local/Unity/Editor/Editor.log`，第二端 `D:/MyWork/CarzyCooker-NetTest/Unity/Logs/Multiplayer-Peer-Editor.log`。实际GameView帧捕获已检查：[Host](../../../../Unity/Temp/pipeline-screenshots/coop-three-dual-game-20261014.png)、[Client](D:/MyWork/CarzyCooker-NetTest/Unity/Temp/pipeline-screenshots/coop-three-dual-game-20261014.png)。Pipeline默认screenshot只离屏渲染MainCamera，得到黑图，未将其当作画面通过；改经同一Pipeline在帧末读取实际GameView，确认Host控制与Client只读呈现。

Console如实区分：两端compilationFailed=false；主端原生Console当前Error=0，Pipeline历史缓冲含2条本轮此前已修复的平台取值编译错误。第二端保留1次 `UnityConnectWebRequestException: Token Exchange failed due a failure with the web request.`，属于Editor账号服务，未阻止真实Join、恢复、玩法或清理；本次未修复该账号服务异常，不声称Console全绿。已有22项新增UTF和11项相关回归结果完整覆盖未变行为，本轮不重跑。Player/AOT、Android、两台物理PC LAN、进程重启恢复、Host迁移及联机存档仍未验收／不在范围内。

两端本轮Play已通过editor_stop正常结束，Editor与长期工作区保留；KCP端口57766已释放。以下逐任务结论取代上方实施阶段的NotRun／待授权历史状态。三案实施及约定验收合计13/13，尚未归档；其他逐帧优化改动及已有截图保持原样。

| 任务 | 双Editor实际行为 | 结果 |
| --- | --- | --- |
| 5.1 | Client成员2、绑定2，在关卡1284191765397539通过HUD拿碗1284191765397511后Dispose其真实Session；Client呈Disconnected、Confirmed=false、Ready=false且只剩恢复／取消按钮。Host名单显示成员2离线，同时继续通过HUD采购橙子成功，Tick1547→1920。Client真实点手动恢复后仍成员2、同Match同Level，绑定4、名单仅1/2两名成员、空手且Confirmed/Ready=true；原碗在Host世界Ground/Holder0，唯一原物没有复制 | Passed；后续饮品均由恢复后的成员2操作，恢复案5/5 |

## 归档收口（2026-10-14）

用户明确授权归档、commit和push。本案全部任务及双Editor验收完成；delta需求与场景已逐项核对主规格，复用原有功能证据，不重跑测试。三个change各严格验证通过，36份主规格严格验证通过。使用CLI --skip-specs归档仅避免重复应用已同步delta，不是跳过需求核对。归档后同步正文引用、保留历史失败和设备未验收范围；原生日志、缓存及其他任务现场不纳入提交。
