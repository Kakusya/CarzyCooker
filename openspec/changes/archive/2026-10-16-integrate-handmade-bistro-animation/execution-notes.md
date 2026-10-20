# 执行说明

用户2026-10-15明确授权将已交付样板接入项目。主要风险为覆盖已扩展63物品、手持锚点不齐、短动作重放和动态Sprite泄漏。选择新增图集、生成切片布局及原Board owner接入；相关最小UTF覆盖逐帧、身份／暂停、内容与回收，再用真实Launcher检查实际画面和命令。复用独立203帧文件验证；游戏接入新增行为全部需本轮运行，不借旧样板预览结果声称通过。

不验收Player、Android或物理LAN；这些不因编辑器表现接入提升范围。首轮测试发现遇到Pipeline eval主线程5秒超时，未启动测试、未取消／清除其他作业；后续待空闲状态重新发现。

## 功能与运行映射

| 功能 | 本轮实际执行 | 结果 |
| --- | --- | --- |
| 新图集与原Prefab引用 | `export_unity.py`终态235帧、3072×2880；Pipeline导入／保存Prefab，实际读回纹理尺寸及引用 | Passed；原生产图集未覆盖 |
| 本地四方向动作／手持点／无刀具作业 | `BistroFramesSocketsWorkAndSingleCommittedServeFollowProjection`，最新`utf-bistro-current.log` | Passed；含四方向八帧、同tick冻结与真实挂点、非切配作业映射 |
| 稀疏快照／短动作／旧身份 | 同用例新增显示tick与位置tick分离断言；Accepted、Rejected、重复结果、旧关／绑定验证 | Passed；未写权威投影 |
| 原63物品／64工位方向、原料与成品、脏污与票据 | `RestaurantArtAllDefinitionsDirectionsAndLayeredContentsFollowProjection` | Passed |
| HUD重开、几何重配、退出回收 | `RestaurantArtRealHudReopenAndWholeRunExitReleaseOwnedSlices`，新增235切片复用与释放断言 | Passed |
| 原逐帧插值与挂点跟随 | `PerFrameBoardStableProjectionInterpolationChangesAndRemovals`，修改固定偏移旧断言后新运行`utf-socket-fixed-run.log` | Passed |
| 既有橙汁真实制作／贴单／交付 | `FirstDrinkUiRealLauncherRecipePreparationLabelAndOrderDeliveryUseActualButtons`，最终`utf-drink-final.log`再运行1/1 | Passed，普通Launcher和真实按钮完成，订单Served、手槽清空、分数及物品回收均有断言 |
| 其余受影响既有UI | 信息层次、单机输入与重开、实际烘烤、升级与保留旧回调四例 | Passed |
| 同Editor远端恢复夹具 | `CoopMemberResumeUiRealDisconnectManualButtonReopenAndRetiredInput`，最新`utf-coop-final-window.log` | Passed；修正夹具保留Host owner并关闭HUD／隐藏Board后，恢复、重开及旧输入全部通过 |
| 普通Launcher实际操作与暂停 | 普通模式菜单→单机餐厅→内存试玩（饮品菜单）；真实指针执行命令1–29，完成拿放／切配／打蛋／煮汤／分装／持汤。切配暂停前后均为tick674、BistroArt113，恢复后继续；打蛋实际显示无刀具BistroArt231 | Passed；仅本次内存试玩，无产品存档写入；交付使用下述真实入口UTF |
| 540×960实际画面 | GameView设为实际540×960，Screen读回并通过ScreenCapture采集含HUD画面；`Art/CarzyCooker/HandmadeBistro/review/integration-portrait.png` | Passed；检查厨师、手持汤碗及锅内汤面，随后恢复原GameView尺寸并删除本次临时尺寸项 |
| 资源集合刷新 | 实际With Optimize菜单成功，Console出现`Refresh ResourceCollection.xml success.` | Passed；XML仅新增BistroAtlas的Entity资产及另一会话已生成的章节配置资产，无资源删除；ET.Code／HybridCLR规则保留，无DLL输入的空目录仍不等于已构建 |

已有7个美术组Passed结果验证的实际渲染／回收行为在随后修正中未改变；新增显示tick分离只影响传入美术相位，位置插值及直接Board.Render的默认语义保持，新增UTF已单独覆盖。避免重新全跑美术组。

最终10个唯一相关PlayMode UTF全部有Passed结果（首组7项＋修正后逐帧1项＋新动画1项＋远端恢复1项；其中真实橙汁交付又在最终实现上补跑1项）。两项delta需求的四个场景分别由新动画／真实交付、全定义内容和生命周期用例覆盖，生成布局与owner符合design。普通Launcher人工操作补足实际画面、动作与暂停，不以截图代替行为断言。严格`openspec validate integrate-handmade-bistro-animation --strict`通过；未提交、未推送、未同步主规格、未归档。

普通入口人工操作期间首单因检查画面耗时而过期，未宣称人工交付成功；真实交付由上述最终普通入口UTF完成。Pipeline的game截图只含相机画面，最终交付图改用实际ScreenCapture包含HUD。原HUD文字布局未在本次改动，540×960现有顶部说明仍有挤压，接受为既有显示范围，若后续开展HUD排版优化再处理。

## 原始输出与失败

- `Art/CarzyCooker/HandmadeBistro/review-unity-export.log`：`BISTRO UNITY EXPORT COMPLETE frames=235 atlas=3072x2880 hands=235`。
- Blender逐片读取运行时图集并比对203原帧及32无刀具工作帧：`check-runtime-atlas.log`为`PASS runtime atlas matches all 235 source frames`，透明边界检查通过。
- 原始UTF标准输出保留于`Unity/Temp/BistroIntegration/utf-*.log`；Pipeline原始结果仍在`Unity/Temp/pipeline_test_status.json`，未复制或改写。
- 首轮`utf-art-regression.log`为9例7Passed／2Failed。旧挂点断言期望2.72、实际2.663993，已按真实帧挂点修正并单独通过。远端夹具的旧处理删去Host显示组件，新快照会自动重建该组件；目前夹具保留原owner，按正式Visible API隐藏Host Board并关闭原HUD。
- `utf-coop-preserved-owner.log`中的实际失败为测试期间新增外部编译错误：`RemainingBusinessTests.cs(42,65) ProductMenuVariant.Salads`不存在。之后观察到同处`Drinks`编译错误及修正；这些文件不由本次美术工作修改。
- 多次前置查询遇到Pipeline eval主线程5000ms超时，日志保留，未视为测试通过；未启动作业时重新发现，未取消已有测试、未清Console。
- 临时调用适配器只为原SOP的只读eval前置传入Pipeline公开的20000ms参数；项目匹配、发现、互斥锁、实际UTF、600秒用例时限、Console覆盖和判定均沿用原工具，未修改公共测试基础设施。前置超时和已有锁拒绝均不计运行通过。
- 已向用户异步请求约5分钟稳定窗口；不接管其他会话的业务表／枚举修改。设备／Player／物理LAN仍NotRun。

## 2026-10-16 归档

按用户明确授权归档。本次仅同步主规格、移动change和修正文档引用，未改变运行行为；复用上述已记录验收，不重跑Unity。两份动画delta按样板、接入顺序合并至restaurant-art主规格，原Purpose和既有需求保持。归档前两案严格校验及48份主规格严格校验均通过；各案3/3任务完成。Player／Android／物理LAN未运行范围保持，未提交或推送。
