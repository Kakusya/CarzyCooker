# Execution Notes

## 当前状态

2026-10-15 用户随后授权完成全部 16 个 change。本案已实现并完成相关 Editor PlayMode 验收，八个唯一用例均有当前适用的 Passed 结果。使用主工作区 Unity 6000.3.18f1，projectPath=`D:/MyWork/CarzyCooker/Unity`，Pipeline 端口由实例发现为 7800；没有操作第二工作区。

GridAction 新增 TakeContainerContent=22，ProtocolVersion 从 22→23；复用原预期实物/内容/布局字段并接入 wire 白名单，无 Proto/Excel/CodeBind 结构变化，无手改生成物。饮品动作改为显式集合。HUD 详情提供指定内容取出，默认整体拿放保留；主规格已同步。本案无资源/场景/Prefab 改动，无产品构建；真机/Player/AOT/物理 LAN 不纳入，无提交、push 或归档。

## 严谨程度

低到中等局部实物转移风险，重点防重复位置与错取。用未加工鸡蛋/盘中固体、满手/错目标/锁定拒绝、双人抢取和旧请求的小集，再检查普通 Editor 操作；不为此重构容器体系。 新动作的唯一位置和加工锁定须有真实消息断言，已有拿放/包装/倾倒行为未变时复用原记录。接受液体/作业拆解等范围外行为未实现；若以后扩大取出能力，再明确规则并补相关最小验证。

## 任务与验收

| 任务/功能 | 验证 | 当前结果 |
| --- | --- | --- |
| 1.1 动作/白名单 | `ContainerExtractionBothMenusKeepOriginalEggAndBowlAndReplayOnce(false/true)`、`ContainerExtractionRejectsFullHandStaleContentsAndUnknownActions` | Passed，真实请求；基础/饮品菜单均合法，未知动作拒绝，重放不多取 |
| 2.1 唯一转移/保护 | 上述三例，加 `ContainerExtractionRejectsPausedHeldWorkAndLiquidWithoutBreakingProcess`、`ContainerExtractionDoesNotDismantleCookingOrReadyBatch`、`ContainerExtractionRealKcpRaceMovesOneOriginalAndKeepsIndependentProjection` | 六个核心用例 Passed；满手/旧内容/暂停作业/液体/煮制/完成批次拒绝，原批次和另一液体保持，真实双人竞争仅一个胜者 |
| 3.1 UI 单次操作 | `ContainerExtractionUiRealLauncherReopenAndOldCallbackKeepOneRequest` | Passed，真实 Pointer；受管重开旧回调无效、同次单请求、默认拿原碗、自然结算换关后旧动作无效 |
| 4.1 集成 | 上述 Solo Launcher UI，加 `ContainerExtractionUiOrdinaryHostAndRealRemoteDetailsKeepOriginalItem` | 两个普通入口用例 Passed；Solo 入碗/取回及整碗拿放，普通 Host 入碗、真实 KCP Client 详情取回原鸡蛋，双方独立投影一致 |

真实物品唯一位置/锁定/竞争 → 本地/KCP UTF；指定内容与整容器区分 → 受管 UI UTF + 普通 Editor。

## 实际运行与修复

使用已授权 `Tools/AutoTesting/autotesting/run.py`，mode=`playmode`，先发现真实用例。首轮 category=`ContainerExtraction` 选中 8 项，7 Passed/1 Failed；失败为远端首次详情入口。随后 category=`ContainerExtractionFinal` 选中新增/改变的 3 项，2 Passed/1 Failed：增强后的加工批次和跨关 UI 已通过，远端入口仍失败。定位期间仅重跑该失败 testName，未扩大套件；未取消作业、清 Console 或改写失败结果。

根因是目标说明函数在“尚未选择工位”时写回 DetailsVisible=false，远端新 HUD 打开详情便被说明刷新关闭。删除说明函数的状态写入；选择失效/关代次变化统一在 HUD 刷新处处理。最后仅运行 `ET.SoloRestaurantUiTests.ContainerExtractionUiOrdinaryHostAndRealRemoteDetailsKeepOriginalItem`，1/1 Passed，退出码 0。其余未改变且适用的七项结果复用，无需再次整组运行。

原始 stdout 保留在本次工程 Temp 的 `ContainerExtraction-UTF.log`、`ContainerExtraction-final-UTF.log`、各次 `ContainerExtraction-remote-*.log`；最终为 `ContainerExtraction-remote-fixed-UTF.log`。Pipeline 最新原始作业输出位于 `Unity/Temp/pipeline_test_status.json`，未复制或改写。最终 recompile completed/failed=false；原生 Console groundTruth compilationFailed=false、consoleErrors=0。历史编译失败与 UTF 失败保留在原输出中，不当作本次通过。

纯文档检查：本案 strict 通过，主规格 strict 43/43 通过；当前协议摘要已改为 23，基础操作说明和逐功能结果已同步。

## 决策与复用

无需新增必要产品决定；本案有限子集明确排除加工拆解与液体，未来若扩展另定规则。 [既有加工主规格](../../../specs/kitchen-item-processing/spec.md)及[供应主规格](../../../specs/kitchen-supply/spec.md)保留其原实现证据，本案新增取出没有因此通过。
