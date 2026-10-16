# 执行说明

主要风险为错误引导、无效奖励和旧作业耗时变化，后果可观察且可定位。用最小行为 UTF 加普通入口验证新增范围；原提交、权限、暂停、退出与未改变的文件兼容结果可引用原 change，不重跑全仓。接受真机、Player/AOT、物理 LAN 和正式平衡未验证；新增对应交付要求或出现具体回归时补验。

2026-10-16：实现与新增功能已运行。原始结果通过 Pipeline `test_status` 读取；工具报告位置为 `Unity/Temp/pipeline_test_status.json`（当前未发现持久文件，不承诺它一直存在），原 Editor.log 保留在 `C:/Users/Administrator/AppData/Local/Unity/Editor/Editor.log`，不复制状态。

| 任务/功能 | 实际用例或操作 | 结果 |
|---|---|---|
| 1.1 实际菜单闭包 | `RemainingBusinessNewMenuBuffsAllHaveARealAllowedRecipeAndLegacyPoolsStayStable`：检查全部新菜单每项效果都有实际配方，旧菜单池保持 | Passed |
| 1.1 源表与输出 | Buff源新增6–18，Level新菜单池；现有Tool Check五目标及正常导出/四语言均成功，退出码0；Client/ClientServer/Editor及Config只变四张受影响表 | Passed |
| 1.2 搅拌新旧冻结/暂停 | `RemainingBusinessMixBuffAndUpgradeActuallyFreezeNewBatchAndProjectToBothClients(False/True)`：旧80 Tick不变，新混合80×3/4×3/4=45，两端同值且暂停停钟 | 2/2 Passed |
| 1.2 咖啡模式真实生效 | `RemainingBusinessCoffeeDevicesActuallyUseNewBuffAndInstalledUpgradeForTheirModes`四例：研磨23、萃取45，热奶/打泡匹配34及另一模式45；含真实KCP独立投影 | 4/4 Passed |
| 1.2 普通玩家入口 | `RemainingBusinessUiActualSoloMenuBuffPartialRecipeAndMixerUpgradeContinue`、`RemainingBusinessUiActualKcpClientReadsPartialRecipeAndApplicableBuffWithoutControl`：甜品候选13/14、选择13、按实际面糊显示效果，远端只读 | 2/2 Passed |
| 1.2 旧配置受变回归 | `SoloBuffConfigActualSourceHasThreeCookingVariantsAndRequiredTwoCandidates`：18个定义和原五项/旧关池保留，正常建关 | Passed |

本案11个不同相关UTF最新Passed，不累计重跑。加成仍是可调首包参数；生产资格筛选不保证玩家当下材料齐备或工位已摆到合适位置，不据此声称正式平衡。

追加自审发现：经营分支的饮品尚未解锁时，配置闭包内效果仍可能不可用。现已在 LevelBuff 初始化时按原 Level.ProductionPermit 一次筛选；新增 `RemainingBusinessGrowthBuffPoolExcludesLockedDrinkAndKeepsItsDrawAfterProjectionRefresh` 已单独运行1/1Passed（退出码0），证明未解锁分支效果被排除，投影刷新不重抽。此前因同一Editor外部动画验收/Play/测试锁而拒绝的尝试均未计为通过；未停止或取消其作业。

生产资格筛选只新增经营分支的过滤行为。已运行甜品13/14与咖啡对应目标都在其有效许可内，既有选择/加工/普通UI结果复用，不因追加检查重新跑整个小集。

最终自审：2/2任务、1项新增要求和3个场景均有运行对应；旧ID、关内owner、Host权限与旧作业参数保持。源工作簿只有sheet1.xml数据部分改变，样式/关系/其他分件保留；中文名和实际五目标输出已核对。严格格式、主规格及引用检查通过；不新增文件/协议版本，不把平衡或真机记为通过。

初轮核心18例中16通过、2个搅拌例因测试把模具放上已有番茄的台面而被合法拒绝 `TargetChanged`（RemainingBusinessTests.cs原78行）；改为原空模具台并按真实有限供应补料，新运行仅补这两例2/2Passed。两个UI初轮按钮完整文本匹配失败，改用实际前缀后2/2Passed。新批次此前绕过冻结参数的缺口已修正；冷启动前置eval超时未启动套件，未计通过。
