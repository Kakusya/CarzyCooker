# Execution Notes

2026-10-12：6/6 任务完成，尚未归档。配置、领域行为与普通 Launcher 操作各有对应证据。九案现已完成；本页记录菜品交付，订单、伙伴及餐厅整链证据见各自change和UI最终记录。

主要风险是依赖图缺口、重复产出和装盘部分写入。选两条代表菜、实际本地请求与身份／数量／位置／进度断言，加普通入口足够；没有引入测试平台或全菜目录。源布局和关卡字段改变，补受影响的容器、汤、关卡和工位小集；未改变备料和所属消息寿命复用各自既有记录。接受有限初始库存、可调 80 Tick 烘烤、无订单即不计分、无持久保存。新增菜品阶段、供应、容器卫生或许可规则时再补受影响验收；手机／Player／AOT／新动作 KCP 本次 NotRun。

## 源与生成

DishConfig 两菜、DishStageConfig 十二阶段、GridBakeRecipeConfig 一配方，注册 c,s,e。原物品源追加 8 面包片、9 烤面包、10 盘（容量 1，只接受 9）；InitialLayout 追加有限一片面包 (1,14) 和一个盘 (3,16)，没有初始烤面包。LevelConfig 两关 AllowedDishIds 均 1,2。五目标 Check 5/5、正常生成 5/5、四语言本地化均成功，工具退出码 0；Client／ClientServer／Editor DT／DR 与数据、Config/Luban 副本从源生成。

协议 8 扩展原 Station／Item／Snapshot 字段，新 GridBakeProcessState 在排序末尾 ZZToastBake.proto，Opcode 10082；原 10000～10081 保持，Client／ClientServer 正常 Proto2CS，退出码 0。StartBake=12 仅本地，既有 wire 的 Action > StopWork 仍明确拒绝。

## 功能与 UTF

在现役本工程 Editor 实际发现并执行 PlayMode SoloDish：19 Passed，0 Failed／Skipped／Inconclusive，退出码 0，Console 覆盖完整；原生输出留在 Unity/Temp/pipeline_test_status.json，不复制或改写。新源码编译 completed、failed=false、errors=[]。下表用例均属 ET.ProductSessionTests，源在 SoloDishTests.cs。

| 任务／功能 | 实际用例／操作 | 结果 |
| --- | --- | --- |
| 1.1 菜品、菜单、有限供应和无免费成品 | SoloDishConfigActualMenusAndFiniteRawInventoryHaveNoFreeDish | Passed；两关菜单、真实供应与空盘显示均断言 |
| 1.2 图闭包与创建前拒绝 | SoloDishGraphRejectsMissingClosureAndCycleWithoutCreatingAnything 六种；SoloDishGraphActualBootstrapRejectsBeforeCreatingWorld 四种 | 10 Passed；缺原料／烤箱／容器／依赖、循环和假半成品拒绝，Host／Authority 未创建 |
| 2.1 重放、输入锁与唯一新产物 | SoloDishBakeLocalReplayLocksInputAndProducesOneNewIdentity | Passed；提前拿取、追加、再次启动拒绝，输入只消费一次 |
| 2.1 共用时钟及暂停 | SoloDishBakeAndActualSoupSharePauseAndOneBusinessTick | Passed；真实汤与烤面包同时停钟，恢复同一步各推进一次 |
| 2.1 工位变换保留 | SoloDishBakeLayoutPreservesOriginalInputProcessAndFrozenDuration | Passed；原输入、ProcessId、RequiredTicks 保持，新位置完成 |
| 2.1 Completed／下一关／整体退出 | SoloDishBakeCompletedLevelFreezesAndContinuesSameBatchWithoutNewStock；SoloDishBakeSnapshotsCopyValuesAndWholeExitDisposesProcess | 2 Passed；同一批次跨关、不补面包，快照普通值独立，整体释放 |
| 2.2 装盘、暂存和拒绝 | SoloDishPlateUsesSameToastAndContainerAndDoesNotScoreBeforeOrder；RejectsWrongRawInputWithoutTransferringEitherSide；FullPlateRejectsSecondActuallyBakedToast | 3 Passed；真实第二片烘烤后满盘拒绝，物品和内容不交换／丢失 |
| 3.1 受影响的既有行为 | GridPrepContainer 2；GridSoup 11；SoloLevel 10；SoloStation 22 | 45 Passed，四次运行均退出码 0、Console 完整；源布局和 Level 字段迁移后的实际证据 |
| 2.2／3.2 普通制作入口 | 下述 HUD／Board 两菜操作 | Passed；手持内容、进度、唯一身份及实际拿放 |

本次相关回归是源配置／地形／投影字段变化后首次验收，不因文档整理重跑。没有扩大到全仓、产品构建或新网络动作。

## 普通 Launcher Solo

Editor PID 39328、项目 D:/MyWork/CarzyCooker/Unity、场景 Assets/Launcher.unity。临时 Temp/SoloDishAcceptance.cs 通过实际 HUD PointerClick 和 ProcessGridPointer 操作，并从已提交投影读取结果；地图点击先核对无 HUD 遮挡，一次输入一次业务编号。World 4c34f53c2b714ae68a88029e4ad20f6d。

首段 11 命令：拿面包，点击烤箱第二占地格放置，确认尚未自动加工；显式烘烤，Board 显示进度；暂停 500ms 业务 Tick 和烘烤进度保持，恢复后提前拿取 WorkBusy、原面包仍在原槽。80/80 完成后原面包被消费，新 ToastId 不同。取出、装原盘、拿盘、台面暂存再拿，内容仍为同一烤面包，手持显示“盘（烤面包）”，分数保持 0。

第二段累计 26 命令：把吐司暂存回盘台，真实切番茄与打蛋，向原锅添加番茄块并倒蛋液，原碗保留。显式煮汤、等真实 Ready、拿原碗盛一份，手持显示“碗（番茄蛋花汤）”，原批次剩 1 份；吐司仍在原盘中，两菜共存且没有订单计分。两次 run_script success=true、diagnostics=[]。截图 Unity/Assets/Temp/pipeline-screenshots/solo-dish-toast-and-soup.png（1280×720）已查看，展示盘内容、手持汤与剩余份数；截图只证明表现，操作断言来自上述运行。

运行及退出 Console session 68cf1167f0ba4ee692e314b87dcdb762，seq 987～1014 共 28 条连续，reset=false、dropped=false、原生 Error=0、compilationFailed=false；有一条 GridCommandRequest 1121ms 警告，若普通持续操作出现可见停顿再定位，未据此建立性能门槛。Play 已由本轮退出。原始 Editor 日志保留 Unity/Temp/solo-nine-changes-editor-interactive.log。

## 交接

菜单和实体依赖已补齐关卡 task 1.1，关卡仍需真实顾客排空、成长及 buff 的交接。订单模块可以用真实容器／内容识别汤和吐司；裸烤面包不算菜品，提前制成并暂存不消费或加分。工作台的伙伴／顾客真实路径仍 NotRun，待各模块接入。

规划建立时 strict validate 与本地引用检查通过；本页以当前运行结果为准。归档、主规格 sync、提交和推送未执行。
