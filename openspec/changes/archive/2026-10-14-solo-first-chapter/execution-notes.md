# 执行说明

## 2026-10-14 实施与增量验收

新开 Solo 使用 101→102→103→0，显式 ChapterId／ChapterStage／IsChapterTerminal；保留历史 1→2→2 给 v1 有效档和 Host。两组菜单各保持一致，终关必须 Next=0 且无装修，缺失正常 Next 拒绝。Level 唯一订单提交点累计 ServedOrders／UnfulfilledOrders，快照复制，协议由实际源码 18 升到 19。

Header 格式由 1 升到 2，在原十三成员后追加记录类型、关序、两类订单数。MemoryPack 实际十三成员 v1 文件已读入默认 Continue 并恢复历史下一关，原文件字节保持。完成记录只读，不允许 InitializeSavedProduct 创建世界。FinishChapter 经原控制链、SoloSaveResult、Repository、有身份检查的完成回执，再服务步确认；ProductRuntime 持待返回的原餐厅引用，随后在 Root 同步队列只释放该轮 Host/Client，保留公共 sender／Repository 后重新开选择器。

风险集中于旧文件兼容、保存失败和共享 sender 寿命。新增四项真实消息／文件 PlayMode 覆盖三关自然排空、零星、终关无装修和拒绝继续、失败重试同一冻结记录、完成文件只读、新开世界和旧文件不改写。原文件不覆盖算法保持，已验证旧行为可复用；不做任意断电测试，Player／手机／LAN NotRun。

- LevelConfig／CustomerFlow 两份源增加三关及显式字段，Check 五目标成功，正常导出五目标及四语言成功；Proto2CS 由源生成 Client／ClientServer 快照新增计数字段，未手改生成物。
- 新增四例 SoloChapter PlayMode：最终 4/4 Passed，AutoTesting 退出 0、Console 完整；原输出位于 Unity/Temp/pipeline_test_status.json 和 Editor 原日志。
- 首次四例 2 Passed／2 Failed：restore 的 else 绑定错误使合法 payload 抛 MissingSaveHeader；终关捕获误走原装修准备 gate。已修复并创建新运行，保留原失败日志，不称首次通过。
- 受管 UI 首次 Failed：夹具更换自有 Repository 后关闭了选择器，需要按现有入口重新开启；修复夹具后补跑，不扩大产品行为。
- 受管 UI 最终 1/1 Passed：真实 Launcher 选文件进第三关、自然收尾、保存失败保持原结算、重复点击一个命令、重试返回选择器、完成文件只读、选择器重开、旧 callback 不生效、新开 101 独立世界。首次夹具关闭选档未重开、第二次宽泛“选择”匹配两按钮均 Failed；修正夹具后新运行通过。
- 相关既有 SoloLevel 14/14、SoloSave 27/27、SoloCustomerServeLatestReplay 1/1 Passed，AutoTesting 均退出 0；订单结果重放计数为 1，文件损坏／不覆盖／回执丢失重试与冻结实物恢复继续正确。原输出保留 Pipeline 终态及 Editor 日志。
- 普通 Launcher 完成新开 PC Solo 三关，世界 `97d61f8198a249d2a04564a82d934e6c`，三关均自然完成 0 分 0 星／未满足 6 单。真实 GraphicRaycaster 可达与 PointerClick，命令 1–10 选择 Buff／开店／前两关装修及保存继续，物品 7→7→7，工位 16→17→18；未直接推进计时或修改订单。
- 终关命令 11 真实保存失败 `SaveFailed`，原 Level `1284151754884494`、7 件物品／18 工位、0 分／6 未满足保持。只把 Repository 临时指向本次在 Unity/Temp 创建的文件阻挡目录，不覆盖用户文件；恢复原路径后清理自建阻挡，命令 12 重试同一 ResultId `db989eb74d134519ab7c5ca427600275` 成功。
- 新建完成 Slot `0fc7cf6e66034be1a2cd70113c83e4b4`，Version 2／ChapterCompleted／Next=0。返回选择器时 Host/Client 已释放，根 sender 与 Repository 仍活；原两份旧档加本轮两个继续档和一个完成档，共五份，未覆盖。
- 实际 Stop/Play 重入，点击“查看完成 记录1”读出第 3 关／成功 0 单／未满足 6 单／0 分 0 星，不创建世界；再点击“内存试玩”，新世界 `566893b621ae4f47823611db148d91ea`，101 准备态／7 初始物品／16 工位，和旧世界独立。已停止本次运行。当前 Console groundTruth Error=0、compilationFailed=false；保留此前历史累计错误，不清 Console。
- 首关第一次点击早于装修按钮生成，仅工具报 No active button，未发请求；等待实际候选按钮出现后继续同一运行。临时 PowerShell 脚本先需补 UTF-8 BOM 才能在 Windows PowerShell 正确解析，未改业务。新增说明曾受 stdin 编码影响变问号，已从 UTF-8 文件重写并检查。上述失败不冒充通过。

| 任务／功能 | 运行证据 | 结果 |
| --- | --- | --- |
| 1.1 三关／终关／依赖与缺失 Next | 五目标 Check／导出；SoloChapterConfig；启动过程 GridDishConfig 全菜单校验 | Passed |
| 2.1 自然排空／零星／计数一次／无第四关 | SoloChapterThreeNaturalZeroStarLevels；SoloCustomerServeLatestReplay；普通三关 | Passed |
| 3.1 v1／完成文件／失败重试／回执 | SoloChapterReadsActualThirteenMemberV1File；SoloChapterTerminalFileFailureRetry；SoloSave 27 项；普通命令 11/12 | Passed |
| 4.1 关序／完成只读／旧 callback／新开 | SoloChapterUi 1 项；普通重入点击完成记录和内存试玩 | Passed |
| 5.1 真实三关保存退出重入 | 上述普通世界、命令、Slot；SoloLevel 14 项及相关小集 | Passed，Player／手机／LAN NotRun |


## 本轮状态与决策

2026-10-13：用户授权“由浅到深写成 change”，并要求真机测试最后。本案目前仅为规划；所有 tasks 未勾选，未实现业务、未执行 UTF、未启动 Unity/Player、未构建或操作第二工作区。第三关保存完成后返回选档、无装修和第四关；橙汁加工锁壶、完成可搬壶分杯、产量/耗时配表，已获用户确认。具体相关合同以本案 specs/design 为准。

## 相称的严谨程度

终关与文件兼容可能误建世界或丢失有效进度。保留不覆盖发布，强化实际文件旧格式读取、终态重试及一次普通三关保存退出重入；不验证任意断电。 所选验收按下表覆盖本次改变的行为，已有结果只有在实现/行为未变且明确对应该点时才复用，并填写原执行记录路径。不因提案完成或阶段切换重跑全仓；测试失败、空集、Skipped、缺输出均不算成功。

接受的未验证范围：本轮所有运行行为 NotRun；除最后设备层外，手机/独立 Player/LAN 仍待对应案。没有玩家、设备或已授权运行条件时保留相关任务未完成。若改到共享 owner、文件格式、协议、行为断言或出现实际回归，则补对应局部验证，不机械放大为完整矩阵。

## 任务与验收映射

| 任务 | 功能／交付 | 拟使用的最小验收 | 本轮结果 |
| --- | --- | --- | --- |
| 1.1 | 终关配置 | 正常导表并运行三关链/缺失下一关/同菜单依赖校验用例，记录模式选择。 | NotRun（规划阶段） |
| 2.1 | 结算控制 | 真实消息用例验证自然收尾、零星、计数一次、终关无奖励/第四关及旧 Continue 拒绝，更新阶段说明。 | NotRun（规划阶段） |
| 3.1 | 保存兼容 | 实际文件用例覆盖旧有效格式、新完成记录、失败重试不覆盖及回执归属，记录格式适配。 | NotRun（规划阶段） |
| 4.1 | 结果和选档 | 受管 UI 用例及真实选择操作验证普通继续/终关返回/完成只读/新开，更新操作文案。 | NotRun（规划阶段） |
| 5.1 | 三关集成 | 普通 Launcher 完成三关和保存退出重入，逐点引用已有小集结果，缺少真实文件或入口结果不勾选。 | NotRun（规划阶段） |

UTF 执行阶段使用现有 Tools/AutoTesting，先发现模式与方法再记录实际过滤范围；涉及产品消息/UI 的候选集按 PlayMode 核对，目录名不能证明模式。原始输出保留原位，只在此引用运行路径及功能对应关系。纯正文任务只检查文本、链接与严格格式，不制造占位 UTF。

## 实施时回填

每项记录“实际方法或真实操作 → 原输出路径 → Passed/Failed/NotRun/Skipped → 未覆盖点”。文档格式验证只证明规划可解析，不证明功能已完成。当前不预填执行日期、通过计数或不存在的测试结果。

## 本轮规划检查

2026-10-13：`openspec validate solo-first-chapter --strict --no-interactive` 退出码 0，规划格式通过。当前相关文档检查共 80 份 Markdown，链接缺失 0、编码/尾随空格/原规格场景遗漏 0；新案依赖无环。以上均为静态规划检查，表内功能仍全部 NotRun，任务未勾选。

## 2026-10-14 归档核对

用户已授权归档、提交并推送。任务均已闭环；复用上文适用于当前实现的 UTF 与普通入口运行结果，本轮归档未改变功能，不重复运行产品测试。主规格已同步，七案共33项增量要求核对通过，34份主规格严格验证通过。

restaurant-chapters 按三关案先、首饮案后的语义合并：保留两道饭菜、首饮菜单变体及旧档原菜单／布局／库存；主规格已有的 ADDED 要求不重复添加。
