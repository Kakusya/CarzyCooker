# Execution Notes

本轮末尾格式检查：全工作区`git diff --check`非零，仅检出19处Luban Editor JSON枚举生成行的尾随空格，无其他诊断；不手改生成文件，也不为本次枚举功能扩张成通用模板格式整理。此项不冒充全工作区格式通过，相关编译、导出及运行结果如下。

## 当前状态与严谨程度

2026-10-16：用户授权对必要及未完成 change 实施。已完成配置与运行值迁移；本案9项任务的功能验收已覆盖。保留用户已有业务、表格、动画与文档修改，未提交、推送或归档。

风险主要是旧数字映射、Flags组合、方向混用及消息/存档误转。采用实际生成数据、相关行为UTF、本地/真实KCP、旧有效文件与普通Launcher验证。没有扩大为全仓测试；首轮已通过的相同实现结果复用，失败修正后只重跑受影响项并补覆盖缺口。接受Android/Player/AOT/物理LAN与全量历史版本组合NotRun；本案没有平台或格式升级，出现端侧具体回归再补局部验证。

## 配置与编译

- 实施基线已有GridManualMode、ProductMenuVariant、RenovationRewardKind，保留既有成员及全部新内容。另注册八种枚举，共11种；实际核对14表22字段。源表用成员名，Flags用`|`，保留所有数值。
- Bin工作目录执行`Tool.exe --AppType=ExcelExporter --Console=1 --Customs=Check,ShowCmd`：五目标完成、明确check success、退出0。随后正常导出：五目标及四语言完成、退出0。
- Client、ClientServer、Editor生成代码分别22/22字段类型核对通过。Config/Luban按原复制链生成；未手改C#/bytes/JSON/ID。
- 10张非本轮业务扩充的源表：非目标单元格和样式与HEAD相同，其Client/ClientServer bytes逐字节相同。Buff/Level/StationUpgrade/Renovation四表保留先前业务扩充，与HEAD的差异不回退；已有单元格样式保持。本案工作簿编辑仅改枚举列与登记表。
- 隔离副本`Unity/Temp/SemanticEnums/invalid-enum-zp8lxudy/ET`中把GridItemConfig.Kind写为UnknownItemCategory，真实Luban加载退出1，明确报枚举无效及D4位置；原输出`check-output.txt`保留原位。没有破坏真实源表。
- 当前Unity所有脚本及测试编译通过。`dotnet build DotNet/Model/DotNet.Model.csproj --no-restore --nologo`退出0，零警告/错误。

## 实际运行与功能对应

Unity6000.3.18f1，精确工程`D:/MyWork/CarzyCooker/Unity`。通过现有Tools/AutoTesting发现PlayMode实际用例，执行类别ProductSemanticEnums及ProductEnumDirectionAndLegacy。首轮35项：30Passed、5Failed；后轮9/9Passed，包含五项修正后的重跑及四项补验。共39个不同用例最新结果Passed，Skipped/NotRun均0；本轮Console无新增Error，历史1条未清除。

| 任务/功能 | 实际UTF或操作 | 最新结果 |
|---|---|---|
| 1.1–1.2，枚举源/原值/合法零值 | ProductEnumsActualGeneratedConfigKeepsOriginalValuesAndLegalZero；五目标Check/生成、三目标22字段核对、DotNet.Model及Unity编译 | Passed |
| 1.2/3.1，物品/形态/两种手工/菜谱 | GridPrepContainerDefinitionsAndUniqueContents；GridPrepWorkChoppingStopsResumesMovesAndProducesOnce；GridPrepWorkHeldBowlHandoffDisconnectLeaveAndNewWorld；SoloDishBakeLocalReplayLocksInputAndProducesOneNewIdentity | Passed |
| 1.2/3.1，Flags/岗位/接水排水/洗碗 | ProductEnumsRejectUnknownAndMismatchedConfigCategories；SoloCleanupConfigActualFormsFacilitiesAndInitialInventory；SoloSinkWaterRealApproachFillDrainAndRefillKeepSameBowl；SoloCompanionOwnershipUsesActualGeneratedConfigAndOrderedRoleMailbox；SoloCompanionGrowthCancellationAndDuplicateNotificationsNeverAddCompletion | Passed |
| 1.2/3.1，三加工管线Buff/升级/两类装修 | SoloBuffCookingNewRealSoupFreezesSelectedDurationAndPausesNormally；StationUpgradeRealManualNewWorkUsesCombinedTwentyThreeTicksAndOldWorkKeepsSixty；StationUpgradeRealBakingRetainsOldFrozenWorkAndNewCombinedBuffDurationAfterMove；RemainingBusinessNewUpgradeRealSelectionAndActualFileRestoreKeepItemsAndSingleLevel八例；SoloRenovationInstallKeepsContainerManualIdentityAndOnlyAddsOneStationOnReplay | Passed |
| 2.1，四朝向/四旋转/真实布局与图集 | ProductEnumsFourFacingMovesAndCounterClockwiseGeometryKeepDistinctMeanings；SoloStationLayoutRealMailboxRotatesOnceAndReplaysOnlySamePayload；BistroFramesSocketsWorkAndSingleCommittedServeFollowProjection；ProductEnumsOrdinaryLauncherRotationButtonUsesTypedLayoutAndProjection | Passed |
| 2.2，菜单/房间/伙伴通知/标签缓存 | RemainingBusinessNewMenuBuffsAllHaveARealAllowedRecipeAndLegacyPoolsStayStable；ProductRoomRealKcpJoinWithoutWorldStartCloseAndRestartKeepMembersSessionsAndSender；伙伴上述真实工作用例新增请求结果与完成通知枚举断言；Bistro与两普通Launcher UI用例 | Passed |
| 2.3，整数拒绝与旧数字兼容 | ProductEnumsInvalidIntegerSnapshotDoesNotReplaceValidProjection四例；ProductEnumsUnknownMenuAndOrientationRequestsLeaveCurrentWorldUnchanged；GridPrepWireLocalKcpDuplicateAndIndependentVersionedGraph；FirstDrinkLegacyActualTwelveMemberSoupBatchFileRestoresWithoutAddingDrinksOrRewriting版本1/2/3三例；上述八例真实文件恢复保留冻结值 | Passed |
| 3.2，真实普通入口/输入/制作交付 | FirstDrinkUiRealLauncherRecipePreparationLabelAndOrderDeliveryUseActualButtons；SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen；真实旋转按钮及KCP房间用例 | Passed |
| 1.3/3.3，文档/剩余数字/规格格式 | Book与references已同步；配置/消费者及生成diff核对；严格格式检查见下 | Passed（严格格式与正文/引用检查通过） |

首轮失败保留：三条旧档用例与一条旋转用例仍用整数零比较枚举；新方向用例选中被工位占用的格子。修正为具名成员断言及从实际地形选择可走2×2区域，第二轮全通过。首轮五项不得描述为首跑通过；未重跑原30项。

## 边界与保留理由

- Proto/MemoryPack消息和SoloSaveData整数DTO不改；file12/wire34保持。Capture显式转整数，Validation/接收先校验后构造强类型值；非法快照整份拒绝，保留前一投影。旧有效文件只读恢复，源文件逐字节保持。
- GridFacing是人物Up/Right/Down/Left，GridOrientation是工位逆时针次数；只在几何循环、图集索引与序列化边界转换。UI标签缓存保留各自枚举，不用一个int Kind混装多种状态。
- 表行/物品定义/实例ID、数量、耗时、比例、坐标、图集偏移/索引、渲染Layer、版本和ProductError注册常量保留整数。CurrentPlatformId是稳定壳到既有协议/存档的平台标识边界，未改变编码。ProgressDeliveryValue.Kind已有ProgressDeliveryKind，沿用。
- 原始Editor日志`Unity/Logs/Business-16-Editor.log`及原生UTF的`C:/Users/Administrator/AppData/LocalLow/GDK/GameDevelopmentKit/TestResults.xml`保留原位。Pipeline终态通过即时API读取；runner显示的Temp/pipeline_test_status.json路径未保证文件实际落地，不建立证据副本。

## 实施核验

按openspec-verify-change核对：完整性9/9任务、3条新增需求均有实现；正确性3/3需求及7/7场景有上述运行或真实导表证据；设计一致性核对生成单一来源、类型分离、原编码、现有owner/消息路径及代码组织。无关键未完成项。严格格式验证退出0；本案与Book/references共7份Markdown编码和本地引用检查通过。平台等范围外项如上保留NotRun，未把它们计入通过。
