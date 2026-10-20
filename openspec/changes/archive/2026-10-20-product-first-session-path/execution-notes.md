# 执行说明与验收边界

2026-10-20归档收口：用户明确授权归档和本地提交。原功能与行为未变，复用本案逐项适用的Passed结果，不重复执行玩法套件；主规格对应关系及归档引用重新检查。

2026-10-20：已实施并完成功能验收，5/5任务完成。本记录替代初始规划及中途状态；未取得真人新手观察，不宣称引导效果已获玩家验证。

## 严谨程度与投入

主要风险是选错章、取消仍写档、提示指向失效实物。复用原预览/确认、文件校验和配方，用导航、兼容、半成品/锁定及一次正常三关旅程验证。未改保存协议、物品或开店门槛；不对全部菜单重复通关。接受Android、Player及陌生玩家理解未验证，出现相应回归/卡点再补对应验收。

## 实施与边界

Solo首页优先正式第一章1101，保留继续、三章选择、九种原试玩及原预览确认。`SoloEntryPage`只持导航；`SoloSaveSelectionRequested.ShowRecords`仅本地事件字段，不改网络或文件格式。

原GridRecipeGuide统一提供当前步骤、缺口、物品和位置，第一章首关自动显示，可跳过/重看。定位只读；半成品继续加工，锁定材料不误报可用。步骤/订单/关卡/绑定变化撤下旧目标。owner及消息沿[design](design.md)的ProductClient与原Host路径。

## 任务／功能 → 实际运行 → 结果

短名均为`ET.SoloRestaurantUiTests`实际PlayMode用例。

| 任务 | UTF或实际操作及断言 | 结果 |
|---|---|---|
| 1.1 | `FirstSessionPathAllChaptersTrialsCancelAndRetiredPreviewRemainReadOnly`：三章/九试玩逐一预览取消，不建世界或写档，旧确认无效；真实确认只开1101/Growth | Passed，最新1/1 |
| 1.2 | `FirstChapterCompatibilityExistingFilesKeepNextLayoutStockAndCompletedScores`、`SoloSaveSelectionUiBrowsesWithoutWorldCancelsValidLoadRejectsInvalidAndRestoresSelectedFile`：实际多文件选择/取消、完成记录只读及旧有效检查点继续，Next/旋转布局/有限库存保持 | Passed，2/2 |
| 2.1 | `RemainingBusinessGuideDoesNotOfferLockedSoupInputsAsSaladMaterials`、`RemainingBusinessRecipeGuideUsesPartialRealMixAndCookedMoldInsteadOfRepeatingMaterials`、`KitchenRecipeGuideActualDefinitionsPrepreparedDishPauseAndManagedReopenStayReadOnly`：真实半成品/熟模具/已备菜及锁定缺口，阅读不发命令、不要求重做；导航用例检查取放后撤旧定位 | Passed，3/3 |
| 2.2 | `FirstSessionPathActualThreeLevelsSoupToastSaladAndNaturalClosing`：暂停冻结、跳过/重看、只读定位、HUD重开；故意让首张汤单按正常60秒期限过期，旧高亮/回调失效，明确改选有效汤单后实际Serve才完成首单 | Passed，1/1 |
| 3.1 | 同一旅程经普通Launcher，540×960真实射线/地图输入，无物品注入或跳时：选加成→切番茄/打蛋/煮汤/盛装/交付→玩家清桌→自然Completed→原扩建确认/保存继续；吐司、沙拉及终关记录同走原入口 | Passed，520.6秒 |

第一关10分/1星，原阈值10/20/30未改。记录1101 served1/unfulfilled2；1102为10分/1星、served1/unfulfilled4；1103实际交付沙拉并自然完成、完成记录终关1103，不虚构未采集的最终统计。此旅程包含有意超时及未服务客人，不是平衡胜率或新手用时样本。

## 卡点、修正及复核

运行发现原规则只允许玩家清桌，小麦负责询问/洗碗，已修正引导及操作驱动。早期整体作业还误预期10分为零星、第二关未显式选择扩建9，均按原规则修正；最终完整新作业通过。零星推进由节奏案核心用例另验，没有为过测试改评分或扩建规则。

完整性5/5任务、5条要求；正确性8场景已映射；一致性符合只读引导、显式确认及原owner。主规格已同步，六案及51份主规格严格校验通过。原输出位于`Unity/Temp/pipeline_test_status.json`，随作业正常更新，未变行为按上表复用。已归档并获授权本地提交，未推送。
