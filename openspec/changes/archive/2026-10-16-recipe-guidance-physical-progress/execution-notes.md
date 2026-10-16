# 执行说明

主要风险为错误引导、无效奖励和旧作业耗时变化，后果可观察且可定位。用最小行为 UTF 加普通入口验证新增范围；原提交、权限、暂停、退出与未改变的文件兼容结果可引用原 change，不重跑全仓。接受真机、Player/AOT、物理 LAN 和正式平衡未验证；新增对应交付要求或出现具体回归时补验。

2026-10-16：实现及新增功能已运行。原始结果通过 Pipeline `test_status` 即时读取；工具报告的原生位置为 `Unity/Temp/pipeline_test_status.json`（当前未发现持久文件，不能承诺它一直存在），Editor 原日志为 `C:/Users/Administrator/AppData/Local/Unity/Editor/Editor.log`。不复制原始结果或业务状态。

| 任务/功能 | 实际用例或操作 | 结果 |
|---|---|---|
| 1.1 锁定原料不可用于另一菜 | `RemainingBusinessGuideDoesNotOfferLockedSoupInputsAsSaladMaterials`：真实煮汤保留的番茄不使沙拉跳过备料 | Passed |
| 1.1 精确缺口与脱模 | `RemainingBusinessRecipeGuideUsesPartialRealMixAndCookedMoldInsteadOfRepeatingMaterials`：真实预拌粉投入只缺蛋/奶；实际烤熟原模具指向脱模 | Passed |
| 1.1 绑定与旧订单 | `RemainingBusinessGuideKeepsBoundOrderAndExplainsExpiredBindingWithoutSendingCommands`：实际制作咖啡后，以两个订单的只读值投影测试绑定选择和失效提示；不冒充权威贴单验收 | Passed |
| 1.2 Solo/KCP普通界面 | `RemainingBusinessUiActualSoloMenuBuffPartialRecipeAndMixerUpgradeContinue`、`RemainingBusinessUiActualKcpClientReadsPartialRecipeAndApplicableBuffWithoutControl`：普通 Launcher 部分备料提示、不发送动作及远端只读 | 2/2 Passed |

新增范围共5个不同UTF通过。旧汤/吐司受管重开回归结果补在下文。只更新原派生文本，没有新增业务状态、协议、Prefab 或表字段。主规格新增两项要求，保留原场景。

受变回归 `KitchenRecipeGuideActualDefinitionsPrepreparedDishPauseAndManagedReopenStayReadOnly` 已运行1/1Passed，退出码0：真实汤/吐司定义、预制成品、暂停停钟、受管重开/旧回调均保留。本案6个不同相关UTF最新Passed。

最终自审：2/2任务、2项新增要求及3个场景均有实现与运行对应；只读解释保持原owner，权限、数据及消息无新增写入路径。严格格式、主规格和引用检查通过。无范围内未完成验收；真机、独立Player、物理LAN和真人理解仍未验证。

初轮两个普通UI用例因测试将带效果说明的按钮当作完整名称匹配而未找到按钮；修正为精确前缀后新运行2/2通过。编译阶段两次误用生成枚举成员名已修正，最新 `recompile_status` 完成、failed=false、compilationFailed=false。域重载后的原生UTF只读检查曾遇5000ms超时，均在开跑前退出；实际新运行按终态报告，不把拒绝/超时计为通过。
