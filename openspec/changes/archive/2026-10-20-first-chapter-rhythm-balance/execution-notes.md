# 执行说明与验收边界

2026-10-20归档收口：用户明确授权归档和本地提交。原功能与行为未变，复用本案逐项适用的Passed结果，不重复执行玩法套件；主规格对应关系及归档引用重新检查。

2026-10-20：源表、正常导出及实际三关验收完成，4/4任务完成。没有真人新手样本，不宣称平衡成熟。

## 严谨程度与投入

主要风险是共享表误改、有限库存不匹配以及把自动化完成当作体验结论。只改12个指定数值，核对源到输出，跑精确配置/生产闭包、旧文件及一次正常计时三关。原加工、评分、保存与合作规则未变；不建统计平台或重复全菜单。接受多人平衡、设备时延与真人理解未验证，实际库存无解或玩家卡点再补验。

## 参数及生成链

[LevelConfig.xlsx](../../../../Design/Excel/ET/Datas/Product/LevelConfig.xlsx)的D32–D34、[CustomerFlowConfig.xlsx](../../../../Design/Excel/ET/Datas/Product/CustomerFlowConfig.xlsx)的D/E/G32–34，共12单元格。50ms/tick：

| Solo关 | 营业tick/秒 | 首客tick/秒 | 间隔tick/秒 | 等待tick/秒 |
|---|---|---|---|---|
| 1101 | 1800 / 90 | 120 / 6 | 600 / 30 | 1200 / 60 |
| 1102 | 2100 / 105 | 120 / 6 | 440 / 22 | 1000 / 50 |
| 1103 | 2400 / 120 | 120 / 6 | 360 / 18 | 900 / 45 |

保留QueueCapacity2、EatTicks60和原配方/供应/布局。工作簿其他ZIP部件、样式/公式/验证保持。ExcelExporter实际Check及五目标正常生成成功，Editor JSON差异仅12字段，Client/ClientServer及Config输出正常生成；未手改生成物。合作2101–2103仍500/650/800tick，试玩201仍400tick。

## 任务／功能 → 实际运行 → 结果

| 任务 | UTF或实际操作及断言 | 结果 |
|---|---|---|
| 1.1 | `ET.ProductSessionTests.FirstChapterRhythmActualTablesLimitTimingToSoloAndKeepProductionClosure`：精确数值、其他模式原值及全部菜单生产闭包/章节/客流配置验证 | Passed，1/1 |
| 2.1 | `ET.SoloRestaurantUiTests.FirstSessionPathActualThreeLevelsSoupToastSaladAndNaturalClosing`实际汤/吐司/沙拉生产交付，沙拉实际采购→运输→接收拆份、余额减1；三次自然收口。另`ET.ProductSessionTests.RestaurantContentChapterRealLocalAndKcpSelectThreeLevelsAndReplayAsAnIndependentKitchen(False,1)`断言三关零星继续、终关无第四关、独立复玩 | 两项各Passed 1/1 |
| 2.2 | `ET.SoloRestaurantUiTests.FirstChapterCompatibilityExistingFilesKeepNextLayoutStockAndCompletedScores`及`SoloSaveSelectionUiBrowsesWithoutWorldCancelsValidLoadRejectsInvalidAndRestoresSelectedFile`：真实文件选择/继续，旧1101→Next1102、旋转布局、库存2及对应3在途货物保持，目标关2100tick；旧终关70分/3星不重算、成功文件字节不变 | Passed，小集2/2 |
| 3.1 | 普通Launcher第一章三关540×960真实输入，正常计时、无物品注入/跳时，包含实际清桌、扩建8/9预览确认、保存继续及完成记录；完整旅程520.6秒 | Passed，1/1 |

## 实际节奏观察与修正

首张汤单有意等到正常60秒超时，随后改选另一单并交付；1101最终10分/1星、served1/unfulfilled2，1102为10分/1星、served1/unfulfilled4。1103实际一份沙拉交付并自然完成，终关记录1103；未采集其最终分项，不补造数字。未服务客人也按原规则超时，三关均可自然收尾。此操作脚本包含验证性等待，不用于声称成功率或新手难度合适；无具体配置阻断，未再调参数。

原误以为小麦会清桌、10分应零星、扩建只选8造成早期作业失败，修正提示/驱动后完整新作业通过，未改领域规则。核心章节用例使用显式AdvanceTicks，只证明零星等逻辑；正常时间体验由上述520.6秒旅程证明。旧文件夹具曾缺少Growth工位/库存对应货物等而被校验拒绝，已修正有效fixture，未放松生产校验。

完整性4/4任务、3条要求；正确性3场景均覆盖；一致性符合局部调参及原生成链。主规格已同步，六案及51份主规格严格校验通过。原输出保留`Unity/Temp/pipeline_test_status.json`并随作业更新。已归档并获授权本地提交，未推送。
