# 餐厅美术制作与表现

**非规格**；冲突以代码／表、进行中 change、已同步主规格及用户最新意图为准。**阅读 ≠ 授权**新增菜品、改变地图或实现未做能力。

## 制作源

统一源在 [Art/CarzyCooker](../Art/CarzyCooker/README.md)。Blender 4.5 LTS 导出透明斜俯视精灵，Inkscape 导出 HUD 组件。软件为离线制作依赖，不进入游戏包。模型与矢量均为本项目制作。

`export_art.py` 同时生成图集、资产清单、模型工作台和 `MonoGridKitchenBoard.ArtLayout.cs`。图集和 C# 布局必须来自同一次源导出；不能手改生成布局。方向为北／东／南／西，镜头正对地图边，保持 45° 正交效果；地面未旋转成菱形。

## 已有 owner 接入

- Board 仍是原 `GridKitchenBoard` GF 托管 Entity；图集纹理通过 Prefab 的 `m_ArtAtlas` 引用，由原 GF 资源链持有。
- 本轮第一次 Configure 创建并缓存 Sprite 切片；同一世界几何变化复用切片/已有地砖，更新边界、窗位与镜头；尺寸不变立即返回。ReleaseBoard 清表现节点并释放切片。子图层均归该 view，不建立业务状态或私有池。
- HUD 的 `m_ArtAtlas`／`m_HandIcon` 是直接序列化美术引用，原 CodeBind 字段保持；关闭释放本轮手持图标切片，重开重新创建，原输入 guard 和按钮 owner 保持。
- 手持、工位方向、容器内容、卫生、批次份数与贴单均来自当前投影。镜头、图片或动画不修改厨房、角色、工作与订单。

## 位置和遮挡

人物以脚底定位，柜体以占地前沿排序。台面、手持和餐具池物品位于相应宿主上方；容器可叠加两种原料，完成批次改为真实成品，脏污和票据单独显示。

椅子使用 `GridTableProjection.Seat` 的真实座位格，不占新逻辑格。窗与绿植只在地图边缘显示。固定帧与重复投影复用图片和子节点；动画帧使用现有传入 tick，暂停传入冻结 tick。

## 编辑和验证

Unity 导入参数与 Prefab 引用通过已接入 Pipeline 的 Editor API 保存，保留未保存场景。图集需保留原尺寸、不启用 mipmap；HUD 面板和按钮使用九宫格边框。资源集合刷新继续用现有 With Optimize 菜单，不手改 XML。

本轮资产／姿态／分层／回收 UTF 位于 `RestaurantArtUiTests.cs`；最小集类别为 `RestaurantArtRegression`，后续局部变更只选择受影响部分。真实制作、服务、布局和竖屏图也要对应运行证据，不能只看清单数量。具体已运行范围见 [change 执行说明](../openspec/changes/archive/2026-10-15-restaurant-art-production/execution-notes.md)。

沙拉批次从同一模型源扩展到31个物品、214切片。物品范围用生成的ArtItemCount，新增物品会移动后续人物/效果起点；必须图集与布局同次生成。对应验证见[多输入组装记录](../openspec/changes/archive/2026-10-15-kitchen-multi-input-assembly/execution-notes.md)，不重跑未变全套美术测试。

有限扩建沿同一Board及已提交Snapshot.Width/Height重配，不重复注册场景相机；原厨房仍在有效竖屏区域，前厅可纵向拖看。实际点击与新桌服务验收见[扩建记录](../openspec/changes/archive/2026-10-15-restaurant-space-expansion/execution-notes.md)。

热餐增量沿同一源增加煎台四向和32–42物品，当前229切片/15行/42物品，后续人物与效果起点由生成布局持有。42物品及48工位方向的实际受管渲染通过；原图集/布局同次导出，见[热餐运行](../openspec/changes/archive/2026-10-15-kitchen-hot-food-batch/execution-notes.md)。

甜品增量沿同一源增加搅拌机四向和43–51物品，图集242切片/16行/51物品；甜品盘与原盘采用低位内容叠加，模具内容独立于外壳，后续人物和效果起点继续由生成器持有。实际加工、保存与普通Solo/KCP运行见[甜品记录](../openspec/changes/archive/2026-10-15-kitchen-dessert-batch/execution-notes.md)。

咖啡增量沿同一源增加磨豆/萃取/蒸汽设备四向及52–63物品，图集266切片/17行/63物品。热饮杯为带袖套纸杯，内容层按原有序Contents及自动批次投影，最终卡布有明确奶泡顶。三款制作、贴单、半杯/模式保存及全定义受管渲染范围见[咖啡记录](../openspec/changes/archive/2026-10-15-kitchen-beverage-batch/execution-notes.md)，数量和文件存在不替代实际验收。
