# 探索与执行说明

## 2026-10-16 实施与实际验收

已在原 ProductEntryPresentation/UI1101 接入“继续他人餐厅、导入独立进度、导出给朋友、替换我的记录”四种任务。具名 enum、检查路径、展开状态和最近 receipt 只属于本轮视图，不成为文件/世界权威。换任务先使原 serial/按钮失效，再清理路径与上轮检查；信任和导入确认独立，检查后修改路径不能沿旧预览信任。只读等待可取消；信任、签发、导入和替换实际提交期间不显示取消读取。

三种用途仍用原文件验证与 repository Ordered MailBox。ReadRun 保持当前 Host/Match 约束，导出页要求已检查/信任的接收者与当前房主 Profile/Key 一致；Client 可检查但无载入入口。导入成功显示原 receipt 的新 Slot/修订，可查看原记录页；页面采用“本机记录选择”以兼容合作领取和独立导入，再明确跳转原 Solo 检查，不自动开局。替换先选仍可领取结果和原目标修订，再独立有损确认；发布失败/冲突不消费资格，成功显示实际目标修订。高级身份验证及原文件功能保留，未改文件格式/协议/Prefab/CodeBind。

PlayMode 真实发现702项。本案沿已有4个普通UI/文件用例补充行为断言，按 EntryAuthorization 4项、AuthorizationFlowEdges 2项和 AuthorizationResultViews 2项的小集推进；每次改变只补跑受影响项。首次4/4、取消/中文用途2/2、成功结果2/2 Passed。540正文分行后4项运行3 Passed/1 Failed：失败断言仍要求完整路径出现在明细正文，改为明细核对文件名、原输入框核对完整路径，单项复跑Passed；共用记录去向文案及实际查看补验后导入单项Passed。最后新增房主/接收者核对时，编译先把原HostIdentity字节数组误作身份实体，按原DecodeIdentity修正。负向身份检查已通过，但新增预先浏览使原取消用例不再从空列表开始；改由实际返回任务/重新进入清理本轮视图后，仅复跑该KCP单项Passed。域重载期间一次recompile返回400未启动测试，ready后执行。最终4个不同用例均有当前适用Passed结果、Skipped=0，Console覆盖完整且无新增Error；未把重复运行累计成新的功能用例数。

| 任务／功能与 Scenario | 实际用例／操作和行为断言 | 结果 |
|---|---|---|
| 1.1/1.2 正常 Scenario：导入、取消再确认 | `SaveAuthorizationUiImportCancelsWithoutWriteThenRunsIndependentBranchThroughOrdinarySoloEntry`：原签名分支文件；任务切换不写档/不信任，改路径后的信任拒绝；单独信任后取消导入仍0档，旧确认无效，再确认只新建1档且来源/根归属正确，源字节保留 | Passed |
| 1.1/1.2 导入结果与原独立运行 | 同一导入用例：receipt 新Slot/修订、查看本机记录不建世界；原档主退出后，原单机选择/完整检查/明确继续恢复实际下一关并拿碗；旧确认不再新增记录 | Passed |
| 1.1/1.2 导出三种用途与身份明细 | 导入用例实际导出可验证的 ImportBranch 和 Replace 文件；`CooperativeBaselineUiOrdinaryBrowseCancelLoadExportAndKcpWorkKeepOriginalFile` 实际导出绑定本Match的ReadRun及公共身份。明细只显示公共字段，未暴露私钥/XML/授权字节；不同接收者身份不冒充当前房主 | Passed |
| 1.1/1.2 变化 Scenario：只读许可不能导入 | `CooperativeBaselineUiForeignFileTrustRejectsWrongMatchThenLoadsWithoutTakingOwnership`：实际ReadRun放入导入任务显示用途不符，无导入/信任捷径，原导入API也拒绝；无新档，源字节保留 | Passed |
| 1.1/1.2 继续他人餐厅、权限与当前房间 | 同一外来文件用例：错误Match明确拒绝，正确许可由已验证Host载入真实世界，源Owner/修订不变。普通基线用例中真实KCP Client原拿番茄/放台/切配成块，Client检查并信任实际许可后仍无载入控制，世界和源文件不变 | Passed |
| 1.1/1.2 变化 Scenario：替换修订、取消与发布失败 | `SaveAuthorizationUiReplacementCancelsRejectsStaleTargetRetriesPublicationAndRetiresOldConfirmation`：目标只读查询可取消；原有损确认取消无写入；真实另一替换使目标1→2，旧预览被拒绝；实际Windows文件锁使发布失败且资格/旧档保留，重试一次到修订3，receipt真实、无恢复备份/临时文件，源保留 | Passed |
| 1.1/1.2 关闭重开/旧回调 Scenario、一次操作一次提交 | 上述导入/替换与普通基线用例捕获原按钮delegate：任务换轮、取消、晚到读取、世界建立后再触发均不覆盖新上下文、不重复建档/替换/开局；原高级浏览/验证/读取入口继续实际使用 | Passed |
| 2.1 普通 Launcher 540×960、原记录流程 | 自有签名临时源文件；任务主页→导入检查→明确信任→取消(0档、无Host/Client)→再明确导入(1档、receipt修订1、仍无Host/Client)→查看导入记录→明确原Solo选档检查(仍无世界)→原确认继续→实际关卡2拿碗command1 Executed，手持非零，记录仍1档 | 已实际操作并查看画面 |

普通入口最终接收Profile为 `a84ddb4c1e8742bc9f8516a68ec7fe6a`，导入Slot为 `bab81518ef52452e899e9923ed8b3a01`，源Slot为 `dd3e029e2b7a482eae43f633d5ea4f8b`。人工目录为自有 `Application.temporaryCachePath/auth-task-manual/39e4fe17be2949cd9e2122fc0f479745/`，实际落在本机Local Temp；只准备自有文件，未改玩家目录。准备时一次把repo挂在Runtime而非其Root，发现页面未取得该repo后移除自有误挂组件并挂回正确Root；另一次启动未就绪的eval未执行准备。不将这些eval失败记为功能通过。

人工画面在 `Unity/Assets/Temp/entry-authorization-task-home.png`、`entry-authorization-import-preview.png`、`entry-authorization-import-result.png`、`entry-authorization-original-inspection.png`。初次长来源行导致正文过小，已改按完整字段分行并重新查看预览/结果/原检查画面；完整路径保留原输入框。后续共用去向标题由“已领记录”改成“本机记录”只改同长度文案，原手动流程证据复用，实际查看行为已补UTF。本轮自有Play已停止。

严谨程度：主要风险是混淆三种用途、把信任当导入，或旧确认/取消误示文件效果；实际签名临时文件、用途/Host/修订约束、真实文件锁失败、晚到回调、实际KCP和普通入口覆盖本次影响。原签名/原子发布/消费及平台边界未改，沿[原导入替换验收](../2026-10-15-save-branch-import-and-replace/execution-notes.md)及[原基线验收](../2026-10-15-cooperative-save-baseline/execution-notes.md)复用。未验证真人理解、独立Player、物理LAN、Android真机及任意断电；出现具体设备遮挡、授权/发布规则改变或真实文件竞争新问题再补对应范围。原始Pipeline结果保留 `Unity/Temp/pipeline_test_status.json` 和工具原始输出，后续自然更新，未复制/改写证据。

收尾：本案5份规划/执行Markdown及操作手册、当前进度共7份文档，171个本地引用和编码/行尾检查通过；严格格式校验通过。三个任务按CLI返回行逐项核对并刷新确认3/3、all_done，原18案为15/18、45/54。未提交、推送、同步主规格或归档。

下文保留建案时的规划记录；当前实施和验收以上方为准。

## 2026-10-16 依据与决定

现有页面混合身份导出、信任、只读运行、导入、替换等动作及路径输入。合同完整，玩家却需要先理解工程术语才能选正确用途。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryProgressSystem.cs)、[主规格](../../../specs/save-authorization/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：复用 ProductEntryPresentation 中 Progress／ConfirmImport／ReplacePreview 状态及原 repository 操作，增加具名视图任务枚举，不重写签发／验证／原子发布。显示用户易读术语及可展开身份明细，不暴露私钥／凭证载荷，文件路径仍由玩家主动提供。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

本案涉及文件操作或运行退出的玩家判断，主要风险为简化流程时合并了原来独立的信任与有损确认。保持原发布、权限和生命周期实现，只改变入口或增加只读检查；使用真实临时文件／原控制链及 UI 运行验证，覆盖取消、迟到与失败，不扩大成任意断电或全设备试验。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 各用途导航与原实际文件链、取消／失败／修订冲突及源档保留；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate save-authorization-task-flow --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
