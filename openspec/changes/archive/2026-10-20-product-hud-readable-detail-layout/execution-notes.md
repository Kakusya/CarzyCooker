# 执行说明与验收边界

2026-10-20归档收口：用户明确授权归档和本地提交。原功能与行为未变，复用本案逐项适用的Passed结果，不重复执行玩法套件；主规格对应关系及归档引用重新检查。

2026-10-20：已实施并完成功能验收，5/5任务完成。本记录替代初始规划及中途NotRun状态；已归档并获授权本地提交，未推送。

## 严谨程度与投入

主要风险是Prefab引用失效、遮挡、指针穿透及旧回调操作新上下文。核对原UI1101、CodeBind、Canvas及消费者，用相关PlayMode UTF和两种分辨率、非零安全区的真实射线/指针验证。原绑定路径未变，无须重生成CodeBind；未改表。未变角色和加载生命周期结果复用本轮适用证据，不因文档整理重复跑套件。

接受Android触感、独立Player、全设备适配和真人易用性未验证；发现实际尺寸/安全区故障再补对应视口。

## 实施与owner

入口、经营、详情沿原UIComponent→UIGridKitchen→MonoUIGridKitchen分区。正文、动作和入口文本各自滚动，返回固定；详情消费指针，关闭后恢复地图。订单文字保留原绑定并独立滚动，原订单动作进入情境动作/更多区域，避免覆盖文字。没有新增业务状态或请求入口。

接口及消费者已同步至[UI开发](../../../../Book/UI开发.md)，实现见[Layout](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoUIGridKitchen.Layout.cs)。原GF/ET owner管理加载、关闭与池化，await后及回调核对当前身份。

## 任务／功能 → 实际运行 → 结果

短名均为`ET.SoloRestaurantUiTests`实际PlayMode用例。

| 任务 | UTF或实际操作及断言 | 结果 |
|---|---|---|
| 1.1 | `SoloRestaurantUiBindingsRealPrefabHasGeneratedFieldsAndOriginalCanvas`、`EntryNavigationReturnsFromProgressAndRejectsRetiredPageButtons`：原绑定/Canvas有效，入口与记录往返、旧页面失效；首局导航补齐三章/九试玩预览 | Passed |
| 1.2 | `ReadableHudActualEntryIndependentScrollFixedReturnAndReopen`分别在540×960、1920×1080执行，最大字号与模拟非零安全区；宽屏经营/结算可达、订单无动作覆盖；`SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen`真实地图拖动不发命令 | Passed |
| 2.1 | 同一ReadableHud用例：最长已交付菜谱和长动作列表同时存在，分别真实滚到底且互不改变位置；固定返回射线命中，详情不穿透、镜头恢复、下一次取碗仅一请求 | Passed |
| 2.2 | `SoloRestaurantUiLifecycleExitDuringActualInitialLoadDoesNotShowOrphanHud`及关闭/重开用例：加载中退出无孤儿HUD，旧回调无效；第一章旅程覆盖实际换关 | Passed |
| 3.1 | `ReadableHudHostAndRealKcpClientKeepSharedLayoutAndRestrictedControls`验证共享布局和角色权限；`FirstSessionPathActualThreeLevelsSoupToastSaladAndNaturalClosing`经普通Launcher完成Solo入口、经营、详情及三次自然结算 | Passed |

ReadableHud原小集5/5 Passed；随后只补跑改变的布局用例，两种分辨率各1/1 Passed。真实KCP为同Editor内独立Fiber/客户端和实际套接字，不代表物理LAN或双进程。最终宽屏图为`Unity/Assets/Temp/hud-layout-final-wide-orders.png`；输入结论来自行为断言。

## 修正及最终复核

早期夹具未查找非活动父节点、Host重建竞争HUD均已修正；安全区变化后的负滚动位置已在生产布局恢复/夹取；宽屏旧订单动作压文字已迁入原动作列表。对应新作业均通过，失败结果未改写。

完整性5/5任务、5条要求；正确性6场景均有实现与运行映射；一致性符合原UI/owner/输入边界。主规格已同步。六案及51份主规格严格校验通过。原输出保留`Unity/Temp/pipeline_test_status.json`，由Pipeline随新作业正常更新，不复制状态或清Console。

提交边界：共享文件只收本案行，保留HEAD原平台入口语义以隔离未完成Android存储/应用生命周期工作；当前PC行为不变。Unity序列化空字段及Luban二进制保留正常生成格式，不为文本空白提示手改。
