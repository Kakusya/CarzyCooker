# 手作小馆动画素材

2026-10-20 汤/吐司有限样板、完整HUD映射与244帧同次导出见[COHERENCE.md](COHERENCE.md)；下方数量为原批次历史记录。

本目录保留独立美术源和离线预览；对应素材另经`export_unity.py`接入原Board。离线预览中的小馆陈列仍不是游戏截图或功能验收，实际接入与运行范围见[接入记录](../../../openspec/changes/archive/2026-10-16-integrate-handmade-bistro-animation/execution-notes.md)。原`RestaurantAtlas.png`继续保留未覆盖的63物品／16工位表现，不被旧样板覆盖。

直接打开 [preview.html](preview.html)，可播放、暂停、逐帧、切方向及改变速度。页面自带全部图片，无需服务或网络。

## 交付内容

| 内容 | 范围 |
| --- | --- |
| 厨师动画 | 待机、行走、端物行走、切配、递餐；每类四方向八帧，共160帧 |
| 食物／效果动画 | 汤面、橙汁液面、蒸汽；每类八帧，共24帧 |
| 静态样板 | 空碗／锅／杯／盘、吐司、窗、香草盆；切配台／炉台／水槽各四方向，共19帧 |
| 总计 | 203张透明PNG单帧、18张精灵表；动画部分184帧 |
| 原生源 | `build_bistro.py` 与 `source/HandmadeBistro.blend`；可编辑模型、材质、层级和动作时间轴 |
| 清单 | `clips.csv` 记录每帧的方向、矩形、帧率、循环、锚点和手持绑定点；`stage.csv` 只供陈列合成 |

模型使用奶油色棉布、鼠尾草绿围裙、蓝边搪瓷和木柄。木头、瓷釉、液面和食物分别设置材质；汤面保留蛋花与高光，吐司有焦边、气孔和黄油，蒸汽使用透明层。

## 帧约定与交接

- 单帧192×192，RGBA透明背景；精灵表从左到右是0–7帧，从上到下是北、东、南、西。单方向素材只有一行。CSV矩形原点在左上；Unity Rect需要转换到左下。
- 动画推荐10fps；待机／行走／端物／切配及三个效果循环，递餐是单次动作。预览动作库会重播递餐以便观察；游戏不得据此重复提交交付。
- CSV的pivot从左下归一化。厨师固定脚底pivot约为(0.5,0.26067)，不是现有生产图集的0.17；按清单设置，不直接沿用旧切片索引或旧pivot。表长及动作语义也不同，因此本批不是旧图集的直接替换文件。
- 角色PNG不包含碗和食物。`hand_x_from_left`／`hand_y_from_bottom` 是每帧双手中心，供正式接入时跟随绑定。角色与工位接收哪类动作仍由现有投影决定，不能靠动画完成时间写业务状态。
- 空碗、空锅、杯与汤／橙汁液面分别交付。液面要绑定到实际内容／成品状态；不能在加工尚未完成时以成品图片替代真实内容。预览组合不是业务映射方案。
- 静态工位的方向顺序与角色相同。尺寸、相机和接触点须在接入后对照真实占格、台面高度及遮挡调整。
- Blender时间轴1–8待机、9–16行走、17–24端物、25–32切配、33–40递餐。厨师在原点，其余模型在右侧工作台排列，液面和蒸汽保留1–8帧变换。渲染相机固定45°正交；当前默认只渲染厨师。

## 复现命令

在仓库根目录执行。所有输出和临时文件都限定在本目录，不写Unity/Assets，不操作Editor。

```powershell
& 'D:/MySoftware/Blender/blender-4.5.9-windows-x64/blender.exe' --background --python-exit-code 1 --python 'Art/CarzyCooker/HandmadeBistro/build_bistro.py'
node Art/CarzyCooker/HandmadeBistro/build_preview.mjs
& 'D:/MySoftware/Blender/blender-4.5.9-windows-x64/blender.exe' --background --python-exit-code 1 --python 'Art/CarzyCooker/HandmadeBistro/check_exports.py'
```

`-- --smoke` 可只渲染代表组；它会覆盖同名代表组的样板输出，因此完整交付必须再运行不带该参数的导出。日志保留原位并被本目录.gitignore忽略；不提交原始浏览器DOM或运行日志。

`preview.html?qa=1` 在实际浏览器中检查加载、播放时钟、暂停、逐帧、拖动定位、四方向、速度、五动作、单次重播与恢复。`review/preview-desktop.png` 保留桌面查看结果。

离线制作时的验收见 [样板记录](../../../openspec/changes/archive/2026-10-16-handmade-bistro-animation-samples/execution-notes.md)。游戏接入的UTF、真实入口及未运行范围独立记录在接入change；Player与设备结果不由本页预览推断。

## Unity运行时输出

在同一Blender命令中将脚本替换为`Art/CarzyCooker/HandmadeBistro/export_unity.py`，会读取已保存模型，为现有洗碗／打蛋派生不含刀具的四方向32帧，生成`Unity/Assets/Res/Entity/Product/Art/BistroAtlas.png`及`MonoGridKitchenBoard.BistroLayout.cs`。运行时共235切片，旧203帧预览和CSV保持。

此命令会写Unity资源和生成代码，须先确认Editor空闲及生产接入授权。生成布局禁止手改；首次引用与TextureImporter通过Pipeline的Editor API设置到原Board Prefab，保留未保存场景。后续按项目With Optimize入口刷新资源集合。所有动态切片仍归原Board生命周期，不增加业务owner或私有资源池。

2026-10-16接入已验收：10个唯一相关PlayMode UTF通过，普通Launcher实际制作汤品、持物与暂停，真实交付另由普通入口UTF通过；With Optimize已刷新。实际540×960含HUD截图见`review/integration-portrait.png`，逐项和未运行范围见[接入记录](../../../openspec/changes/archive/2026-10-16-integrate-handmade-bistro-animation/execution-notes.md)。
