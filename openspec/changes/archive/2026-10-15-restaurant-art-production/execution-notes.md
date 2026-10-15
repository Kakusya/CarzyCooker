# 执行说明

## 严谨程度与授权

本轮是可逆的美术与客户端表现改动。主要风险是比例／遮挡导致目标不可辨、容器与加工状态图错误，以及重复进入后的原生精灵引用残留。使用固定相机源文件保证方向一致；UTF 断言真实资源、状态与 owner 回收，真实 Launcher 检查制作链和竖屏画面，足以覆盖本轮范围。未变的玩法裁决证据可复用；不以旧截图证明新美术通过。

接受本轮不验证 Android 真机、独立 Player、两机 LAN 与复杂动画设备性能。若 Editor 画面异常、帧耗时明显退化或后续设备验收发现问题，则补对应范围。初始授权为创建 change、下载必要软件并完成实施；2026-10-15 用户查看切片后追加授权 commit、归档与 push。

风格采用前轮推荐的温暖圆润立体卡通；软件用于制作，游戏继续 2D 精灵。首个汤链样板之后必须覆盖当前吐司、橙汁、前厅与界面，不能把样板当整个目标完成。

## 现状

- 主工作区已有未提交的 module-index、grid-kitchen、runtime-instances 及截图，保留并避免覆盖。
- 现有 Board 使用 Palette 的单一方块，HUD 保留现有 CodeBind／按钮／消息入口。
- 本轮已创建 change；软件下载、资源制作与接入已完成，相关编译、UTF 和真实入口结果如下。完整任务收尾继续对照 tasks。

## 任务与运行结果

后续按 `任务／功能 → 用例或实际操作 → 结果` 简短补充，原输出留原位置。

- 1.1 软件 → Blender 4.5.9 LTS 版本及真实 8 图渲染；Inkscape 1.4.2 版本及 Panel／Button／StarBadge SVG 导出 → Passed。软件位于 `D:/MySoftware/Blender/blender-4.5.9-windows-x64/`、`D:/MySoftware/Inkscape/inkscape/`。Blender 来自 download.blender.org，Inkscape 主站下载入口 502，使用项目 SourceForge 发行包。无需 Krita，本轮没有分层绘画依赖。
- 1.2 样板目视 → `Art/CarzyCooker/sample-sheet.png` → Passed，厨师、切配台、灶、番茄／切块、碗、锅、汤轮廓与光向协调、背景透明。完整导出 204 个切片（6 环境、44 工位方向、21 物品、128 角色姿态、5 反馈），保留 `RestaurantModels.blend`／模型脚本、`assets.csv`、`hud.svg` 与实际可运行的导出命令；源脚本已重新全量导出，HUD 脚本三图导出 Passed。

### UTF 与编译

- PlayMode，`category=RestaurantArtRegression`，发现并执行 9 项 → 9 Passed、0 Failed／Skipped／Inconclusive，Console 本轮覆盖完整，退出码 0；原编排输出 `Unity/Temp/CarzyCookerArt-UTF.log`。
- 添加椅子并将厨房淡格线对齐真实格边后，只补受影响范围：PlayMode，`category=RestaurantArtFinal`，3 项 → 3 Passed、0 Failed／Skipped／Inconclusive，Console 覆盖完整，退出码 0；输出 `Unity/Temp/CarzyCookerArt-final-UTF.log`。两项新美术用例已用最终版本重跑，未变的其余相关结果复用；共 10 个唯一用例。Pipeline 原始终态保持 `Unity/Temp/pipeline_test_status.json`，未复制或改写。
- 最初一处 `GridConstants` 名称错误已修成实际 `GridKitchenRules`；最后 recompile_status 为 completed、failed=false、compilationFailed=false。旧诊断保留，未清 Console。

| 任务／功能 | UTF 用例或实际操作 | 结果 |
| --- | --- | --- |
| 2.1 场景、格线、镜头和座位 | LifecycleInputAndProjection；ArtAllDefinitions 的真实桌位；普通 540×960 画面与拖动 | Passed；拖动前后命令号不变，厨房完整可见 |
| 2.2 工位 44 方向、占地、升级与加工 | ArtAllDefinitions、PerFrameBoardStableProjection、StationUpgradeUiManagedTarget、ActualBaking | Passed，真实升级移动／换关及原进度保留；烘烤接受、进度、暂停、完成分开 |
| 2.3 21 物品、双内容、脏污、份数与贴单 | ArtAllDefinitions；FirstDrinkUiRealLauncher；CoopFrontProjectionUiRemoteServeAndClear | Passed，真实橙汁全链、汤制作交付和原餐具清理；普通入口三种成品另已完成 |
| 2.4 四方向、持物行走、冻结与回收 | ArtAllDefinitions；ArtRealHudReopenAndWholeRunExit；ActualBaking | Passed，8 姿态与冻结 tick、手持跟随、视图和原生切片释放 |
| 2.5 HUD、图标、一次请求、重开与恢复 | InformationHierarchy、LifecycleInputAndProjection、CoopMemberResumeUiRealDisconnect | Passed，手持图标恢复当前内容、装饰不拦输入；断线隐藏旧手持图，真实恢复重新确认 |
| 3.1 资源集合 | 原 With Optimize 菜单、实际成功日志与 XML diff | Passed，新增 4 美术资源，并补录 17 张既有 ClientServer 表；无删除和手改 XML |

### 普通 Launcher

- 原 Launcher → 内存试玩，实际 Screen=540×960，厨房初始完整可见；输入走当前实际 PointerClickHandler，不绕过请求裁决。
- 34 个真实按钮命令完成番茄切配／打蛋／汤煮制盛装、吐司烘烤装盘、橙汁加工／分杯，全部为 Executed；快照检查三种真实成果均存在。截图 `Unity/Assets/Temp/pipeline-screenshots/restaurant-art-kitchen.png`。
- 真正开店、橙汁贴单和交付、汤交付、玩家清桌后交付吐司：三种 DishId 3／1／2 各成功交付，3 单 30 分。部分早期订单超时按既有规则记零分，未改计时／订单规则。最后餐盘清桌、原碗盘洗净后自然进入 Completed：30 分 3 星、dirty=0、干净池 2 件，截图 `Unity/Assets/Temp/pipeline-screenshots/restaurant-art-service.png`。
- 地图真实拖动至前厅，NextCommandId 不变；实际桌位格有椅子、人物与物品分别可辨。截图 `Unity/Assets/Temp/pipeline-screenshots/restaurant-art-front.png`。
- 本轮普通演示自动化第一次选择 buff 使用了会匹配多个按钮的 Single，导致该演示协程异常退出；修成 First 后重新完成准备。第一次服务流程因吐司订单已经过期而退出；合法清桌后服务新吐司订单成功。两条原始异常均保留在 Console／Editor 日志，不是 UTF 失败，也未把过期订单计为成功。
- 普通 HUD 继续 → LevelConfigId=202、Preparing；半锅汤与半壹果汁各剩 1 份。再从实际“调整切配台／旋转”按钮提交，Executed、Orientation=1，渲染切片为 `RestaurantArt 11`，与清单中的对应方向一致。退出 Play 后重新从 Launcher 选内存试玩：Ready、Level201、0 分 0 星、1 玩家、RestaurantAtlas 正常；最终图 `Unity/Assets/Temp/pipeline-screenshots/restaurant-art-final.png`。已停止本轮 Play，Editor 保持打开。

### 性能与边界

- 相同普通 Editor 540×960 条件，准备时 smoothDeltaTime 约 16.64ms，营业／收尾约 16.74ms，新轮约 17.01ms，未观察到明显帧耗时退化；不据此声称 Android 或 Player 性能已验收。图集为 3072×2496、8-bit PNG／RGBA32；Editor Profiler 报告纹理总占用约 58.5 MiB（含 Editor 侧统计），未伪称手机内存预算已验证。
- 不在稳定帧创建 Sprite、列表或新的表现节点；切片在配置时创建，姿态、内容与遮挡复用原 view，UTF 检查稳定 renderer 和关闭回收。没有宣称绝对零 GC。
- 新增末尾空白仅见 Unity 原生 Prefab／meta 序列化的空字段行，未手改 YAML 或生成物清格式。所有源引用和正式规划格式另行检查。

## 最终核对

- 完整性：6 项新增要求逐项对应上述实现与运行；当前 21 个配置 ID、11 类工位、4 方向与人物姿势均有清单和实际渲染。软件命令、源文件再导出、资源集合均已实际执行。
- 正确性：10 个唯一相关 UTF 全部 Passed；最后改变的椅子和图集布局由最终 3 项覆盖，其他未变的场景与 UI 结果复用。普通入口补齐三菜真实交付、清理、换关、旋转与新轮；不会把两次演示脚本异常或过期订单写为 Passed。
- 一致性：仍沿原 ProductRoot／ClientScene、Board／HUD 与 GF 资源 owner，无新业务状态、消息、表字段或框架；制作说明、Book、模块索引、当前进度和 KnownIssues 已同步。新 `restaurant-art` 主规格从本 delta 同步 6 项要求／6 场景，无 TBD；43 份主规格与本 change 严格验证通过。初始实施收尾未归档、提交或 push；本次追加收尾见下。
- 交付：四面板 HTML 位于 `C:/Users/Administrator/.answer-me-with-html/pages/CarzyCooker-美术交付-20261015-010000.html`，包含真实厨房／前厅截图；已使用 `--no-open` 渲染，STE 无警告，中文正文与字体链验证通过。源码／文档 diff 空白检查通过，Unity 原生 Prefab／meta 空字段行按上述说明保留。

## 2026-10-15 授权归档与提交收尾

- 用户查看切片后明确要求 commit、归档并 push。`restaurant-art-production` 的 proposal／design／specs／tasks 全部完成，CLI 计数为 9/9；本轮仅整理归档和引用，未改变运行代码或美术资源。
- 核对主规格的 Purpose 和 6 项要求／6 场景与 delta 逐字一致，无待同步内容；change 严格验证 Passed；归档更新引用后，全部 43 份主规格严格验证 Passed，美术相关链接检查 Passed。CLI 使用 `--skip-specs` 避免重复应用已同步的 ADDED 要求，已归档到 `openspec/changes/archive/2026-10-15-restaurant-art-production/`。
- 完整性、实现映射／场景覆盖、设计与原有 owner／代码模式复核均无新阻断项；逐项运行证据见上表。复用初轮 9 项与最终补测 3 项（10 个唯一 UTF）Passed 和普通 Launcher 结果，不因归档重复运行。Android／Player／AOT／两机 LAN 仍为 NotRun。
- 版本范围包含美术源、运行资源及 meta、表现接入、相关测试与说明。原始日志、缓存、Temp 截图与其他未提交改动不纳入提交；main 上此前两条未推送提交随本次正常 push 一并送达。
