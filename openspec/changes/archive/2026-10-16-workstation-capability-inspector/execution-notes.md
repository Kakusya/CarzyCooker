# 探索与执行说明

## 2026-10-16 实施与真实验收

已沿原1101 HUD接入“工位说明”。通过工位条目只读选择，地图保持原点击规则；普通工位详情同时补充定义能力、本关许可匹配工序、必要载具、实际朝向/占地及候选操作格。选择工位后收起正文露出地图，可按需展开详情或隐藏标记。原Board持有普通表现子节点，布局变化复用，关闭/失联清除显示，运行退出由原owner释放；不新增Entity、Prefab、表或资源池。

实际发现PlayMode过滤 `EntryStationInspection` 共3项。首轮成长链和KCP项Passed；单机一处名称断言修正后仅重跑 `EntryStationInspectionSolo`，最新1/1 Passed。三个不同用例均有适用的通过结果，Console覆盖完整、退出码0；没有重复运行未受变的成长与KCP链。即时原始输出在 `Unity/Temp/pipeline_test_status.json`，保持原位自然更新，无副本或hash。

| 任务／功能及Scenario | 实际用例／操作与断言 | 结果 |
|---|---|---|
| 1.1/1.2 只读选择、旋转/移位、原制作 | `EntryStationInspectionSoloRotatesMovesReusesMarkersAndReturnsToOriginalBaking`：查看零命令，蓝/绿标记实际坐标分别对应当前Footprint/Approaches；原布局请求旋转90°并移到(5,7)，标记实例复用；关闭说明后原烘烤真实生成烤面包 | Passed |
| 1.1/1.2 暂停、标记开关、旧回调、关闭重开/退出释放 | 同一单机项：暂停阅读零请求，隐藏/显示不重建标记；旧切换按钮重开后失效；实际StopProduct后原Board标记节点释放 | Passed |
| 1.1/1.2 多能力设备、必要容器、远端只读 | `EntryStationInspectionActualKcpSteamCapabilitiesAndHostGeometryRemainReadOnly`：普通Host咖啡餐厅→真实KCP Client，显示实际热奶/打泡工序及配方容器，远端无调整按钮/无额外请求；Host原本地布局请求旋转后远端说明/几何更新，旧按钮失效 | Passed |
| 1.1/1.2 解锁、扩建、许可缩小及旧关Scenario | `EntryStationInspectionActualGrowthUnlockExpansionAndMenuNarrowingUpdateExplanation`：经营成长801→802→803；真实结算奖励解锁后榨汁工序出现，实际扩建9宽/第三桌；原许可缩小后保留榨汁设备定义能力但不列受限工序，新增桌位几何匹配，旧关行零命令 | Passed |
| 2.1 普通Launcher 540×960及返回 | 原主页→单机→内存试玩→确认→工位说明→烤箱：查看/标记/详情期间命令0；地图可见蓝色两格占地及绿色候选格，详情可读原烘烤与无容器要求；关闭说明→全部动作→原拿面包，一次请求、命令1/Executed/手持实物，标记数0 | 已实际操作并检查画面 |

画面位于 `Unity/Assets/Temp/entry-station-inspection-map.png` 与 `entry-station-inspection-details.png`。绿色按当前工位投影显示候选操作格，不承诺墙边候选或动态路线可到达；真实动作继续完整验证。截图证明布局，几何、请求数、生产与释放由上述行为断言证明。手动Play已按本轮所有权停止。

开发失败保留：Hotfix静态只读数组触发ET0015/ET0004，改为详情刷新时从具名能力bit派生，不绕过分析器；测试中Match的EntityRef访问经原类型解引用后编译通过。首轮单机断言错把菜单“吐司”当作产物定义名，实际为“烤面包”；断言改读有效定义名，产品名称/配置未为通过测试修改，新单机运行Passed。

投入沿下文局部UI/表现生命周期风险判断：代表性几何与真实成长链已覆盖，不扩大全仓或设备构建。原业务裁决未改变，复用主规格Sources原结果；真人理解、Player/物理LAN/Android真机均NotRun。严格格式校验通过，5份Markdown/14个本地引用以及新源文件编码/空白检查通过。下文保留建案记录，当前结果以上方为准。

## 2026-10-16 依据与决定

工位详情有位置、内容、加工与等级，但对能做哪些工序、需要什么容器、从哪侧操作缺少统一说明。图形相近设备也容易混淆。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationInformation.cs)、[主规格](../../../specs/kitchen-workstations/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：复用 GridStationProjection 的 Footprint／Approaches／Capabilities／Orientation 与生效配方，Board 装饰节点归原 GridKitchenBoard view；不新建 UIEntity／资源池，不改原生成图集。扩建或旋转后重取当前几何。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为把定义能力等同当前生产许可或把高亮当路径保证，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | 多能力设备、旋转／移位／扩建、许可缩小、Client 只读与退出释放；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate workstation-capability-inspector --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
