# Execution Notes

## 当前状态与严谨程度

2026-10-15：按用户全部16案及离线协议授权完成合作基线读取。5项核心、2项普通Launcher/受管UI唯一用例的最新有效结果均Passed；当前wire25，旧Solo文件v1–v4不改写。本案只读载入与身份/授权，不写个人合作结果，不冒充领取、ImportBranch或覆盖已交付。

主要风险为越权/旧绑定证明、重复世界及改变用户文件。只用隔离临时repository的真实文件、实际RSA/KCP和普通入口小集，覆盖完整授权、恢复、拒绝与取消。原保存算法未改部分复用已有执行记录；新绑定证明和界面拒绝单独实跑，不凭编译或共享代码推断远端通过。真机、物理LAN、Player/AOT、任意断电及两进程的新入口NotRun；同Editor独立Fiber真实KCP不冒充这些环境。后续相关行为改变或出现具体回归再补最小集。

## 任务／功能 → 实际运行 → 结果

| 任务/功能 | 实际用例和断言 | 结果 |
| --- | --- | --- |
| 1.1 本端密钥、身份绑定、完整授权 | CooperativeBaselineActualRsaIdentityPinsKeysRejectsTamperPurposeAndMissingPrivateKey：实际2048位RSA签名/验签，公钥不含私钥；未知版本、错误平台、用途混用、签名损坏、公钥绑定冲突和私钥缺失均拒绝，原绑定不替换。 | Passed |
| 1.1 Participant和当前绑定证明 | CooperativeBaselineRealKcpProofRequiresTrustedKeyAndFreshBindingChallenge：真实KCP未知公钥拒绝，明确导入后原挑战可证明，重放/旧挑战拒绝；实际断线Resume保留同成员/Profile但新绑定未证明，旧证明无效，重新挑战成功。 | Passed |
| 2.1 非Owner Host/完整合法载荷/独立投影 | CooperativeBaselineForeignOwnerActualFileLoadsOnceThroughHostAndKcpWithoutChangingFile(False/True) 两个参数化用例，分别为当前v4与实际13成员v1文件。Host与Owner为不同随机Profile；新世界/原Next、物品数、远端Owner/Slot/revision一致，远端越权、坏签名、同id不同签名、旧房间版本和餐厅已存在均拒绝；同请求重放不重复世界。源文件逐字节相同，Host本机不新增档。 | Passed |
| 2.1 关闭/再开与真实库存 | 上两例关闭后CooperativeSaveBinding与Selected子对象退役，再开内存世界不带原基线、不补复制物资。 | Passed |
| 3.1 半批次/冻结加工/升级/在途 | CooperativeBaselineRestoresHalfBatchTransitAndUpgradedFrozenBakeWithoutRefill：实际检查点恢复剩1份汤、升级1级原烤箱及1600 Tick旧烘焙、仍在途采购与剩余4包；世界确认后暂停，真实KCP看到相同冻结耗时和余量，旧物总数保持。 | Passed |
| 3.1 文件读取取消与旧回调 | CooperativeBaselineUiOrdinaryBrowseCancelLoadExportAndKcpWorkKeepOriginalFile：普通模式建房并验证身份，实际导出公钥/ReadRun+当前MatchAuthority，浏览不建世界；同帧取消浏览/读取后等迟到答复，仍无餐厅，旧真实按钮不提交。 | Passed |
| 4.1 普通合作选档及双方继续 | 同一普通UI用例最终加载真实文件，源只读提示、数量及原文件不变；Host实际拿碗，真实KCP Client独立受管界面拿另一原番茄、放切配台并完成加工，新输出身份与原材料退役符合既有加工语义。 | Passed |
| 4.1 外来文件确认、错误授权可返回 | CooperativeBaselineUiForeignFileTrustRejectsWrongMatchThenLoadsWithoutTakingOwnership：输入真实长路径，先查看签发者/来源/目标，再明确信任；签名合法但Match错误被拒绝并保留房间，换合法文件后新世界成功，UI来源Owner仍为另一Profile，源文件及修订保持，Host档列表仍空。 | Passed |

## 原输出与失败处理

- `Unity/Temp/CooperativeBaseline-core-UTF.log`：首轮5项全失败；四项因运行时生成的公钥仍1024位而拒绝，一项准备夹具未自然到结算。
- 实际Editor API查询确认RSA.Create后设置KeySize=2048仍为1024位/128字节；改用构造指定2048位的RSACryptoServiceProvider且PersistKeyInCsp=false，查询与后续UTF均证实256字节模数/签名及验签。该查询不单独充当业务验收。恢复夹具禁用下一关来客以构造短在途检查点；真实自然收口已由合作终点案覆盖。
- `Unity/Temp/CooperativeBaseline-core-fixed-UTF.log`：2/5Passed，RSA/绑定及KCP证明先通过；其余因夹具建立新运行前未Reset启动上下文失败。
- `Unity/Temp/CooperativeBaseline-load-UTF.log`：补既有交接后两个现行/旧文件载入Passed，冻结用例在本人世界确认前发Pause被WrongWorld拒绝。
- `Unity/Temp/CooperativeBaseline-frozen-UTF.log`：补真实Confirmed等待，仅复验冻结用例1/1Passed。
- `Unity/Temp/CooperativeBaseline-UI-UTF.log`：2项均失败。预期GrantScopeDenied在底层拥有者read回调抛出，虽外层捕获仍被AsyncRuntime报告Error；改为read返回错误值，外层处理。来源提示移到HUD最后的Hint刷新，避免被后续文案覆盖。
- `Unity/Temp/CooperativeBaseline-UI-fixed-UTF.log`：外来文件UI Passed；分工UI因等待旧材料Id而失败，加工本来会退役输入并建立输出，改按原工位实际输出等待。
- `Unity/Temp/CooperativeBaseline-UI-work-UTF.log`：仅复验上述失败UI，1/1Passed；正常拒绝不新增Console Error。
- `Unity/Temp/CooperativeBaseline-proof-resume-UTF.log`：加入当前HostPeer.ProfileProved边界后，实际KCP断线/空房间Resume/重新证明1/1Passed。逻辑Profile保留与当前绑定证明不混用。
- `Unity/Temp/CooperativeBaseline-authorization-UTF.log`：对实际版本/平台和不同签名/旧版本/已有餐厅新增关键拒绝断言，仅跑相关3项，3/3Passed。其余未改验收点复用上述最新适用结果，不重复全套。

各次先实际发现PlayMode用例，再使用现有Tools/AutoTesting的category CooperativeBaseline/CooperativeBaselineLoad/CooperativeBaselineAuthorization/CooperativeBaselineUi或失败项精确testName；均等待终态。所有Passed结果退出0、failed/skipped/inconclusive均0，原失败不改写、不清Console。原始Pipeline作业输出仍在 `Unity/Temp/pipeline_test_status.json`，无额外JSON证据包或状态副本。

## 生成、编译与文档

协议24→25，追加14个公开/内部/本端请求消息类型及RoomSnapshot末尾概要；正常Proto2CS退出0，日志 `Unity/Temp/CooperativeBaseline-proto.log`、`Unity/Temp/CooperativeBaseline-proto-ui.log`，旧Opcode不重排，内部Submit和repository协议不进远端白名单。无Excel修改，不为纯代码运行导表。原23字段CodeBind/Prefab未改，动态地址框沿原owner隐藏/释放并支持文件路径。

曾由ET0013查出RoomSystem/基线协调静态调用成环，已把清理放回CooperativeSaveBindingSystem，正常重编译成功。新建合作基线主规格；同步设计、协议载荷交接、入口用法、已知限制和当前进度。没有提交、push、归档或操作第二工作区。
