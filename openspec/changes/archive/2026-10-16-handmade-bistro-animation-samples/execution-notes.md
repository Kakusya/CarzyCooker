# 执行说明

## 授权与投入

用户授权独立美术制作，并要求尽量使用动画帧。主要风险是误覆盖正在使用的图集／代码，以及帧间锚点跳动、透明边缘裁切。新增目录隔离输出，固定相机、尺寸和方向，并实际导出、检查帧内容与播放预览，足以覆盖本次离线资产交付。

不改 Unity 工程，因此本轮不运行共享 Editor 或产品 UTF；游戏中动作绑定、遮挡、暂停和移动端表现为 NotRun，正式接入时触发相应 UTF 与真实入口验收。旧游戏美术运行证据仅证明原版本，不能代替本批素材接入。

## 验收映射

2026-10-15，本轮均为独立美术资产及离线预览验收。

| 任务／功能 | 实际执行 | 运行结果 |
| --- | --- | --- |
| 1.1 四方向五类厨师动作 | Blender完整导出，逐帧PNG及CSV检查 | Passed：160张角色帧；五动作×四方向×八帧 |
| 1.1 食物与效果循环 | 同次导出与相邻帧／首尾差异检查 | Passed：汤面、橙汁、蒸汽各八帧；未以同一静帧重复填数 |
| 1.1 静态器具与工位 | 同次导出并查看contact-sheet | Passed：19帧，空容器／吐司／窗／香草盆及三工位四方向 |
| 1.1 文件与切片契约 | `check_exports.py` 在Blender执行 | Passed：203张单帧、18张精灵表、42组序列；192×192、alpha、非裁边、单帧与表像素匹配、固定pivot、手持点有效 |
| 1.1 可编辑工程 | 重新加载已保存.blend，切换时间轴帧 | Passed：五动作marker、真实变换以及切配刀具显隐 |
| 2.1 离线预览行为 | Edge headless直接打开`preview.html?qa=1`，实际DOM检查 | Passed：20项；19图片加载、203帧清单、播放推进、暂停冻结、前后逐帧、定位、四方向、速度、五动作、单次重播、恢复与绘制 |
| 2.1 人工视觉检查 | 192像素角色／联系表／切配帧表；1440×1250和540×960浏览器截图 | Passed：主体和台面内容可见；桌面控件与窄屏重排可访问。初次陈列窗顶裁切已调整构图后重新查看 |
| 3.1 文档和规格 | README交接约定、delta、实际输出与严格CLI验证对照 | Passed：`openspec validate handmade-bistro-animation-samples --strict` |

## 原始输出与交付入口

- 资产：[预览](../../../../Art/CarzyCooker/HandmadeBistro/preview.html)、[制作与接入说明](../../../../Art/CarzyCooker/HandmadeBistro/README.md)、[CSV](../../../../Art/CarzyCooker/HandmadeBistro/clips.csv)。
- 完整生成：`Art/CarzyCooker/HandmadeBistro/review-export.log`，终态`BISTRO EXPORT COMPLETE clips=18 frames=203`，进程退出0。
- 已保存文件检查：同目录`review-check.log`，终态`BISTRO CHECK COMPLETE frames=203 sheets=18 sequences=42`，进程退出0。
- 浏览器行为原始DOM：`review/preview-qa-dom.html`，`data-qa="passed"`且20项PASS；浏览器原始stderr在`review/browser.log`。这些原始输出保留原位，由本目录.gitignore排除。
- 视觉结果：[桌面](../../../../Art/CarzyCooker/HandmadeBistro/review/preview-desktop.png)、[窄屏](../../../../Art/CarzyCooker/HandmadeBistro/review/preview-portrait.png)。

## 交接与未运行项

- 本会话新增文件限定于本change和`Art/CarzyCooker/HandmadeBistro/`；未运行生产导出器，未写正式图集、Prefab、表、协议或业务代码，未操作共享Editor。
- 新帧表不直接兼容旧生产索引；角色pivot由CSV明确给出，不能套用旧0.17。手持物、容器与内容分开；递餐动作不得触发第二次业务交付。
- 游戏接入、UTF、真实游戏入口、运行时遮挡／暂停、Player／Android均为NotRun，属于后续接入范围。预览540×960仅证明本地浏览器排版，不是手机游戏证据。
- 生成与检查后只补了文档／任务状态，复用上述完整结果，不因审查重跑全部导出或浏览器行为。

## 规格自审

两项ADDED Requirement及两个Scenario都有实际资产／浏览器行为证据。源和输出路径隔离、原生模型连续帧、内容分层及无服务预览均遵循design；脚本与CSV处于美术目录，不引入游戏运行时owner或消息。离线交付范围未发现阻断项；不据此宣布游戏美术功能完成。

| 自审维度 | 结果 |
| --- | --- |
| 完整性 | 3/3 tasks，2/2新增Requirement有交付 |
| 正确性 | 两个Scenario分别由203帧文件检查及实际浏览器20项行为覆盖 |
| 一致性 | 独立目录、原生帧、分层与离线运行符合design；复用既有Blender／Node，无新运行时依赖 |

本change范围内无CRITICAL／WARNING；无跳过的离线交付检查。游戏接入明确在范围外，仍为NotRun。本轮未归档、提交或推送。

## 2026-10-16 归档

按用户明确授权归档。本次仅同步主规格、移动change和修正文档引用，未改变运行行为；复用上述已记录验收，不重跑Unity。两份动画delta按样板、接入顺序合并至restaurant-art主规格，原Purpose和既有需求保持。归档前两案严格校验及48份主规格严格校验均通过；各案3/3任务完成。Player／Android／物理LAN未运行范围保持，未提交或推送。
