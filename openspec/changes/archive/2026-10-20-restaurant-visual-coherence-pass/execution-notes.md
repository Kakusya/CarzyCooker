# 执行说明与验收边界

2026-10-20归档收口：用户明确授权归档和本地提交。原功能与行为未变，复用本案逐项适用的Passed结果，不重复执行玩法套件；主规格对应关系及归档引用重新检查。

2026-10-20：有限样板已实施并完成功能验收，用户明确接受方向，5/5任务完成。没有扩产其他章节。

## 严谨程度与投入

主要风险是图集映射错位、状态画错、挂点偏移或切片未释放。沿现有美术/动画UTF检查受影响定义和生命周期，再用正常两条生产链及同场景图确认；全94物品只查映射，不重画/重跑全部生产链。原未变远端身份及加载边界复用本轮适用证据。接受非样板一致性、商业美术质量、全部设备观感未验证，实际错图/遮挡/回收回归再补对应项。

## 交付与生成边界

沿既有Bistro材质/几何/相机/灯光，新增5种原料/半成品与4向烤箱；厨师原四向八帧、器具、切配台、炉台及汤/吐司状态保留。同次`export_unity.py`生成RestaurantCoherence.blend、244帧3072×3072图集、布局、挂点和runtime-layout.csv，未手改生成布局。覆盖、相机及锚点见[COHERENCE](../../../../Art/CarzyCooker/HandmadeBistro/COHERENCE.md)与[美术参考](../../../../references/restaurant-art.md)。

样板优先Bistro映射，其余原生产图集回退；HUD从生成布局取得完整94定义范围，未知显示问号而非上一物品。订单按菜品定义取图，不借用手持。原UI/GFEntity owner各释放自建Sprite，几何重配复用；原投影/消息/占地/碰撞/输入/权威无变化，见[design](design.md)。

## 任务／功能 → 实际运行 → 结果

短名均为`ET.SoloRestaurantUiTests`实际PlayMode用例。

| 任务 | UTF或实际操作及断言 | 结果 |
|---|---|---|
| 1.1 | 普通Cafe1401实际拿热饮杯59，原HUD enabled=false/sprite=null；修复后同场景同定义Executed且enabled=true/sprite=RestaurantAtlas，前后图在Unity/Assets/Temp/visual-coherence-hand-icon-before.png及after.png；原生风格对照/可编辑源与同次清单交付 | Passed |
| 2.1 | `RestaurantArtAllDefinitionsDirectionsAndLayeredContentsFollowProjection`：94物品、21设备×4=84方向、内容/空脏/加工/份数/绑定/挂点；`BistroFramesSocketsWorkAndSingleCommittedServeFollowProjection`四向八帧、手部挂点、工作/递餐去重和暂停；普通三关旅程真实制作汤及吐司交付 | Passed |
| 2.2 | `VisualCoherenceEveryDefinitionAndActualHotCupEmptyOtherItemKeepCorrectIcons`：94有效切片独立、边界正确、未知问号、实际59→空手→19无残留；订单/手持映射按各自定义。HUD两分辨率/最大字号相关用例可读 | Passed |
| 2.3 | `RestaurantArtRealHudReopenAndWholeRunExitReleaseOwnedSlices`：HUD94切片、Board244样板帧及生产切片，重配复用、关HUD只释放自身、全退出释放Board；HUD加载中退出、真实KCP共享布局/权限和目标变化、动画暂停/去重用例复用原相同owner行为，三关旅程覆盖换关 | Passed |
| 3.1 | 同场景前后图、原生风格对照、可玩样板交付，用户明确回复“接受这个有限样板方向” | 技术Passed；人工接受 |

最终VisualCoherence小集3/3 Passed；Bistro动画相关1/1 Passed，均在最后烤箱排气栅小调整导出后执行。两条正常生产链此前已通过；最后栅格只改外观，不重复8.7分钟旅程。KCP为同Editor独立Fiber/客户端和真实套接字，不代表物理LAN/双进程。样板确认页为[C:/Users/Administrator/.answer-me-with-html/pages/六案实施样板-视觉与试听-20261020-184602.html](C:/Users/Administrator/.answer-me-with-html/pages/六案实施样板-视觉与试听-20261020-184602.html)。

## 修正及最终复核

原导出切配刀具动画污染静态原料，已在临时无刀工作渲染与静态帧恢复原源第一帧，保存源保留厨师动画；设备/静态切片使用中心锚点，不能误用角色脚底锚点。旧测试固定64设备方向已改为当前枚举全量×4，实际84方向新作业通过，没有缩减断言范围。

完整性5/5任务、3条要求；正确性5场景已映射；一致性符合原源/生成、投影及资源owner边界。主规格已同步，六案及51份主规格严格校验通过。原输出保留`Unity/Temp/pipeline_test_status.json`并随作业更新。用户接受有限方向不代表全章节美术定稿；已归档并获授权本地提交，未推送。

提交包含样板必需的既有手作动画源、运行图集/映射及两份2026-10-16已完成归档记录；这两案此前已归档，本轮没有再次归档或扩大样板制作范围。
