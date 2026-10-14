# 执行说明

## 2026-10-14 实施状态

用户已授权完成三个 change。以下为最新实施与运行事实；文末 2026-10-13 的 NotRun 为规划历史。

新增协议 22 的 ChooseRenovation wire 字段复制／回执；Host 资格在 Peer、共同入口与服务步检查，Client HUD 只读。原生成协议正常执行。4 个核心 UTF（CoopRenovation 前缀）及 CoopRenovationUiOrdinaryHostChoiceClientReadOnlyAndManagedContinue 已 Passed：越权、旧关／绑定、晚加入、一次安装／重放、布局失败零安装、饮品配置安装／移动、真实 Host 选择与 Client HUD 显示。首轮 3 项因夹具漏选必需 Buff Failed，修正后 3/3；首轮普通 UI 暴露旧安装位堵路，补饮品配置后 1/1 Passed。双 Editor 未运行。

运行模式为 PlayMode，过滤为上文实际前缀／FullName，原始输出在 `Unity/Temp/pipeline_test_status.json`，原生 Editor 日志原位保留，均未复制或改写。Passed 运行的 Console 覆盖由 AutoTesting 核对；失败结果不被后续通过覆盖为成功。

接受的未验收范围：独立 Player／手机／LAN／公网／持久凭证／Host 迁移在本案范围外。三个 change 的双 Editor 增量仍是必须完成项，不缩成同 Editor；主工作区提交与第二工作区同步尚需对应授权。没有运行证据的任务不勾选。

## 本轮状态与决策

2026-10-13：用户授权“由浅到深写成 change”，并要求真机测试最后。本案目前仅为规划；所有 tasks 未勾选，未实现业务、未执行 UTF、未启动 Unity/Player、未构建或操作第二工作区。第三关保存完成后返回选档、无装修和第四关；橙汁加工锁壶、完成可搬壶分杯、产量/耗时配表，已获用户确认。具体相关合同以本案 specs/design 为准。

## 相称的严谨程度

主要是远端越权和重复安装。真实 KCP 双端选择／拒绝／重放／换关与实际 UI 足够，复用 Solo 布局算法；存档与 Player 不在本案。 所选验收按下表覆盖本次改变的行为，已有结果只有在实现/行为未变且明确对应该点时才复用，并填写原执行记录路径。不因提案完成或阶段切换重跑全仓；测试失败、空集、Skipped、缺输出均不算成功。

接受的未验证范围：本轮所有运行行为 NotRun；除最后设备层外，手机/独立 Player/LAN 仍待对应案。没有玩家、设备或已授权运行条件时保留相关任务未完成。若改到共享 owner、文件格式、协议、行为断言或出现实际回归，则补对应局部验证，不机械放大为完整矩阵。

## 任务与验收映射

| 任务 | 功能／交付 | 拟使用的最小验收 | 本轮结果 |
| --- | --- | --- | --- |
| 1.1 | 权限与协议 | 生成后真实 KCP 用例覆盖字段复制、远端越权/旧绑定，UI 与消息层都呈现正确权限，更新模式说明。 | NotRun（规划阶段） |
| 2.1 | 候选和安装 | 测试仅新增工位候选、一次安装/失败零变更与后加入投影，记录禁止升级边界。 | NotRun（规划阶段） |
| 3.1 | 同 Editor 集成 | 真实 KCP 小集及受管双端选择/换关/移动操作通过，保留双方结果。 | NotRun（规划阶段） |
| 4.1 | 双 Editor 增量 | 按明确同步基线运行新增链并记录；复用未变两菜/前厅证据，不修改第二工作区源码。 | NotRun（规划阶段） |

UTF 执行阶段使用现有 Tools/AutoTesting，先发现模式与方法再记录实际过滤范围；涉及产品消息/UI 的候选集按 PlayMode 核对，目录名不能证明模式。原始输出保留原位，只在此引用运行路径及功能对应关系。纯正文任务只检查文本、链接与严格格式，不制造占位 UTF。

## 实施时回填

每项记录“实际方法或真实操作 → 原输出路径 → Passed/Failed/NotRun/Skipped → 未覆盖点”。文档格式验证只证明规划可解析，不证明功能已完成。当前不预填执行日期、通过计数或不存在的测试结果。

## 本轮规划检查

2026-10-13：`openspec validate cooperative-renovation-choice --strict --no-interactive` 退出码 0，规划格式通过。当前相关文档检查共 80 份 Markdown，链接缺失 0、编码/尾随空格/原规格场景遗漏 0；新案依赖无环。以上均为静态规划检查，表内功能仍全部 NotRun，任务未勾选。

## 最新逐任务映射

| 任务 | 实际运行 | 结果 |
| --- | --- | --- |
| 1.1 | CoopRenovationHostChoosesRemoteReadsLateJoinAndInstallsExactlyOnce／OldLevelAndOldBinding + 真实 CoopRenovationUi | Passed；协议生成／权限字段与 Client 无按钮 |
| 2.1 | 上述晚加入／一次安装 + LateGroundConflict + DrinkLayoutInstallsSupportedAdditionWithoutMintingItems | Passed；旧池1／2、新饮品池5／6，无升级与免费实物 |
| 3.1 | 核心4项 + CoopRenovationUiOrdinaryHostChoiceClientReadOnlyAndManagedContinue | Passed；普通 Launcher Host 选择／Client 只读／继续，实际 KCP 新工位移动 |
| 4.1 | 双 Editor 新增链 | NotRun；需要 main 必要提交及第二工作区明确同步 |

布局算法、Solo 升级与保存未改，复用原对应领域证据；新增饮品安装位置单独运行验证，不扩大为全餐厅回归。

规格回查发现双方安装失败原因需要共同事实，已追加 Snapshot66.RenovationFailure 并由原领域 owner 持有／清除。`CoopRenovationLateGroundConflictRejectsContinueWithoutPartialInstallation` 最新单例 Passed（Console 完整）：Host／远端两份投影都为 RenovationOccupied，重复拒绝仍无部分工位／新Level／物品变化。普通UI新分支及地图移动待新运行验证。

最新 `CoopRenovationUiOrdinaryHostChoiceClientReadOnlyAndManagedContinue` 1/1 Passed、Console 完整、编排退出0：真实Host选择后专属负例设置晚到落物，真实Continue整体拒绝，Host与Client受管HUD都显示安装失败原因；移除测试阻碍后合法Continue只安装一次，再用地图指针移动新工位，Client读取相同新格。旧选择回调不生效。负例设置只作用本UTF拥有的内存世界，不改用户文件或成功链规则。

最小相关回归当前结果：WireInternalMessageCannotJoinOrTakeLocalIdentity 1/1 Passed；CoopCoreWire 4/4 Passed（未知动作、旧协议／内部Submit、物理RPC与业务编号关联）；SoloRenovationConfig 6/6 Passed（新旧池与非法配置无Host建树）。所有 Console 覆盖完整、编排退出0；未扩大到全餐厅／全仓。

## 本轮本机核对结论

同步前置补充：首个候选 ebb6d15f 的 Git 内存预检发现 AGENTS.md、Book/快速开始.md、CarzyCooker当前进度.md 冲突；没有实际 cherry-pick 或修改第二工作区。候选清单须收敛交接方式后才能执行，提交／同步授权仍待明确答复。详细预检见 [成员恢复记录](../cooperative-member-resume/execution-notes.md#同步预检补充)。本案4.1保持 NotRun。

本轮新增22个唯一功能UTF有适用当前实现的Passed证据（装修4核心＋1UI，恢复10核心＋1UI，饮品5核心＋1UI）；11个直接相关回归Passed。严格OpenSpec验证3个change及36主规格有效，diff空白与18份当前文档链接／UTF-8检查通过。未触碰ProjectSettings／Packages或用户已有未跟踪截图。目标仍未完成：每案最后一项双EditorNotRun，未提交、未同步、未归档、未push。

拟收口动作：只将本轮代码／协议源和正常生成结果／Excel与派生数据／行为UTF／对应规格及说明提交到本地main，标题“接通合作装修、成员恢复与饮品贴单”，基于当前main bd0badff。第二工作区当前fed20ee2（协议17）、工作树干净，保留6项执行技能；必要前置候选ebb6d15f（协议源）、9c435906（消息边界）、bd0badff（已交付饮品／升级／UI）及本轮新提交，明确清单在执行前复核。纯文档／截图提交不默认加入；不做merge／pull／DLL复制。最新主工作区规则优先于第二端旧放宽规则，源码冲突中止并回报，不在执行端开发。main新提交仍须用户授权；双Editor验收保持原目标，不降格为同Editor通过。

## 双 Editor 验收开始

2026-10-14：用户明确要求进行双Editor验收，按此前已说明的前置条件执行本次功能的本地提交、指定同步和验收，不扩大到push或归档。排除其他逐帧优化改动及已有截图。补齐05d3c79e、b2848b0d、4bfe1a2c、6d56bd0d等历史文档依赖后，候选序列的临时索引预检仅有Markdown冲突；运行源码、协议、Excel和生成配置相对main无差异。同步中仍禁止第二端修源码冲突；实际运行结论待回填。
