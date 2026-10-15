# Execution Notes

## 已完成交接（2026-10-15）

10已完成4/4，00–10已交付。8个不同UTF最新有效结果Passed：配置1、本地/真实KCP安装与布局2、实际新旧/合作文件3、普通Launcher Solo/合作UI2。文件v9/wire30；有限9×18与单客桌，不增物资。历史失败和当时NotRun保留；未包括真机/物理LAN/Player/AOT，不提交、push或归档。

## 实施前交接（2026-10-15）

00–09已经完成；本案接文件v8/wire29与持续解锁/逐关许可。采用原坐标向右扩为9×18的单个有限区域（新增右侧两列内部格），旧7×18不改坐标；新增一张单客桌于(7,3)，座位(6,3)。扩建奖励8沿现有Growth的前两次结算候选提供，已安装不重复出现；原解锁7仍可选，不引入额外首发大关或新菜单预设。桌子不附加顾客/餐具/食物，原池容量仍须满足实际有限餐具。

主要风险是三类角色/Board/文件使用不同尺寸或部分安装。选择有界单次扩建，完整预检后在原装修继续提交点安装；路径缓冲只在几何变更时扩容，原物/加工身份保留，旧路线清理走既有换关/布局边界。配置及本地/KCP请求小集覆盖几何/冲突/引用，真实新旧文件及普通Launcher两模式覆盖点击、原厨房、伙伴服务与真实新桌订单；不重复未改变的09许可与签名套件。接受真机/物理LAN/Player/AOT、任意自由尺寸/多桌与全目录平衡NotRun，出现越界/穿墙/重复安装/库存变化则补相应最小验证。

新增字段从RenovationConfig/TableConfig/LevelConfig/CustomerFlowConfig及共享枚举源导出；Table3的InitialLayoutId=0明确表示仅由扩建奖励安装，不放入初始布局。持久几何在原成功载荷追加值记录，旧v1–v8只读采用原尺寸；实际版本递增由实现后记录。协议沿原Snapshot的Width/Height，无独立网络通道。所有功能当前NotRun，任务0/4。

## 2026-10-15 规划优化

本轮仅修订未开工计划，统一已确认范围、真实前置和逐功能验收；以下早期“仅规划/待裁决”措辞属于创建记录，当前交接以本节、design与tasks为准。没有实施业务或执行UTF/Editor/构建，功能结果仍为NotRun。本案严格格式、正文/引用、任务到验收映射检查通过；全批汇总见[路线记录](../../../../Docs/Product/full-version-roadmap.md)。

## 当前状态与范围

2026-10-15：用户授权继续扫描业务、按优先级建立 change，明确不考虑真机实测，并确认“按已确认目标，菜品分批扩展”。本案只完成规划，任务未勾选；功能实现、UTF、Unity Editor 运行、改表/导表、构建均 **NotRun**。规划检查结果统一见[本轮路线记录](../../../../Docs/Product/full-version-roadmap.md#本轮规划检查)。

PC LAN 与 Android 原案保留，不是本案依赖。没有提交、push、归档或操作第二工作区；此前授权的美术提交不扩展成本轮业务实施许可。

## 严谨程度与已接受边界

共享几何变更会影响输入与三类角色寻路，需小范围跨消费者验证。选择一次代表性扩建，覆盖占位拒绝、旧物/加工保留、顾客入座和保存恢复，接受其他候选地图 NotRun。

只选择实际改变行为的最小相关 PlayMode UTF 与普通 Editor 入口；可复用下列已交付主规格关联的原证据，前提是实现、消费者和验收点均未变。 不建立全目录矩阵、额外状态副本或通用测试平台。

接受未验证：真机、设备输入、Player/AOT、物理 LAN/公网不在此次规划内；全目录平衡、未选择内容批次及跨设备持久故障不由本案证明。 需要相关产品交付时另定范围；本案新增/修改行为或出现具体回归时补相应最小验证，不因整理/审查重复跑旧套件。

## 任务与未来验收映射

| 任务/功能 | 最小验证与断言 | 当前结果 |
| --- | --- | --- |
| 1.1 单次扩建配置 | core配置1条及五目标Check/Gen。 | Passed；见当前运行结果 |
| 2.1 原子安装与寻路 | core/layout本地与KCP + 合作普通UI角色服务。 | Passed；失败保旧/去重/绕路/引用均有断言 |
| 3.1 表现与保存 | 合作UI像素/拖看/厨房 + files三个不同真实用例。 | Passed；新旧档及待安装/已安装均往返 |
| 4.1 普通入口集成 | 普通Launcher Solo与合作；Solo实际选档重入。 | Passed；两个不同UI用例 |

同一运行可覆盖多行；先发现真实 PlayMode 用例并记录过滤范围，再执行改变行为的小集。任务中写的是验收目标，不是已经存在或已通过的测试。已有通过结果只在实现和验收点不变时复用，缺口补跑；失败、空集、Skipped及缺输出仍未验收。

## 可复用的既有合同

已有运行记录：[2026-10-13-solo-workstation-layout](../2026-10-13-solo-workstation-layout/execution-notes.md)、[2026-10-14-solo-interlevel-renovation](../2026-10-14-solo-interlevel-renovation/execution-notes.md)。只复用未变的原行为；本案新增功能不因引用它们而通过。


- [kitchen-workstations](../../../specs/kitchen-workstations/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。
- [restaurant-renovation](../../../specs/restaurant-renovation/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。

## 实施交接

2026-10-15 已确认首批一块有限扩建区加一张普通单客桌。前置为解锁/许可合同，具体格坐标与尺寸按现有地图可达性选定；不扩大最终规模。

参数按配置和实际消费者核对；运行后回填真实方法、结果、原日志路径和失败原因，不保存额外业务状态副本。


### 当前运行结果

- 源表为__enums__、RenovationConfig、TableConfig、LevelConfig、CustomerFlowConfig五份；临时编辑脚本`Unity/Temp/assembly-config-edit/space-config.mjs`只改源。`Space-source-edit.log`、`Space-luban-check.log`、`Space-luban-gen.log`记录五目标Check/Gen及本地化success，伴生inspect输出已删。奖励8 ExpandSpace具名枚举，9×18，桌(7,3)/座(6,3)，无初始布局的Table3只由奖励安装。Growth前两关可选7/8，已获不再显示；第三关OrderWait600为有限新桌服务观察留出时间。
- `RestaurantExpansion-core-UTF.log` PlayMode3/3 Passed：配置/原尺寸/不免费出桌1条；Host本地/真实独立Fiber KCP各1条。真实打蛋暂停后自然结算选8，仅选择仍为7×18；源夹具暂将安装点指向真实旧工位(5,11)，有序Continue拒绝，旧关/选择/物品/进度不变；恢复源后实际继续安装，重复Continue返回既有成功且只多一张空桌。两端Width9/Height18同版投影，真实走到(6,3)，BFS可到(7,4)，原物/暂停作业身份保持，路径数组扩容后查询复用。冲突为明确源夹具，未伪称玩家可在旧边界外放物。
- 尺寸消费者：GridKitchen持已提交Width/Height，原7×18作为初始/旧档默认；玩家/伙伴/顾客共用原Grid路由，BFS索引与出生回退改读owner尺寸；工位/前厅/装修可达性传实际尺寸。Snapshot沿原Width/Height复制，两端没有第二套地形。路径缓冲只在ResizeGridGeometry扩容，未测量或声称零GC/帧率改善。
- C#重编译通过，09:23:26UTC原生Console为0 Error/0 Warning；测试无新增Error。任务1已满足配置/导出/边界/有限容量覆盖。任务2尚补布局旧路线/三角色观察交接；任务3/4的几何持久化、Board重配和普通入口仍NotRun。当前文件仍v8/wire29，尚不能报告已支持含扩建的成功档。

- `RestaurantExpansion-layout-UTF.log` 本地/真实KCP 2 Passed：在v9/wire30状态复用原安装/冲突/物品断言，新增区域内用实际Layout请求搬空交接台到(6,4)，旧路线包含该格，新查询绕开它，BFS数组仍为同一实例。配置1条未改变行为，复用首轮Passed。
- `RestaurantExpansion-files-UTF.log` 首轮2 Passed/1 Failed：Solo新格式文件往返及真实12成员v8文件/旧签名结果Passed，合作新文件在Root异步保存中返回Accepted而夹具误断言一步Executed。只修等待并重跑True，`RestaurantExpansion-file-KCP-ready-UTF.log` 1 Passed。三个不同文件用例最新有效结果均Passed：新v9保存/领取→恢复已提交9×18及唯一桌/暂停工作/有限余额/待收货；首关7×18的Pending8恢复时只新增一张空桌、物品数保持；真实v8旧文件保持7×18/两桌，旧签名结果首次领取输出当前v9，重复仍原Slot，源文件原字节不改。
- Source已升级为文件v9/wire30；Header仍28成员，Geometry为Payload第13成员。普通UI当前运行中，任务2/3/4保持未完成，尚不把编译和文件Passed当点击/顾客服务通过。

- 普通入口首轮`RestaurantExpansion-UI-UTF.log` 0/2：两模式已走通真实新增格点击、原厨房F01制作、新桌顾客/伙伴询问及交付10分，随后夹具错误期待伙伴自动清桌，停在Draining。核对GridCompanionSchedule只调度Ask/Wash，按原规则增加玩家真实清桌按钮，再等待伙伴洗净原盘；没有修改伙伴分工或取消真实前厅活动。重跑仅两条UI，结果尚待终态。
- 合作文件True是Host/Root真实签名领取及恢复，不冒充远端KCP包体验收；独立KCP动作/布局和普通界面由另外两组用例分别证明。没有运行真机/物理LAN/Player/AOT。

- `RestaurantExpansion-UI-clear-UTF.log` 合作普通入口Passed（138.78秒），Solo因原Fixture默认180秒总限超时，保留Failed。合作已证明：真实像素点新增格移动/竖向拖看、厨房全角可见、同原厨房制作F01、独立KCP观察新增桌位、真实第三桌顾客/伙伴问单、交付10分、玩家清桌、伙伴洗净同一盘。Solo还有第三关自然收尾、完成返回选档和读取检查点，给该一条明确Timeout300000；逐步20–90秒等待不变，只重跑Solo，合作不重跑。
- 任务2.1 → core/layout本地与真实KCP、合作UI三类角色 → Passed，冲突/重复/旧路径/引用保持均有实际断言；任务3.1 → 合作普通UI像素映射/原厨房/竖向查看与Files三个不同实际文件用例 → Passed。两任务不把Solo的超时算通过，任务4.1仍需Solo完整终态及普通选档重入，保持未完成。
- 规格48/48与本案严格通过；13份文档224条本地链接有效。当前全仓diff-check退出2，33处尾空白仍只在四份原Luban Editor生成文件，来源为既有模板；不手改生成物，不谎报全仓clean。已同步原参考“固定7×18 BFS”为提交几何扩容复用，Board切片为首启创建/几何变更复用/本轮释放。

### 普通入口最终交接

`RestaurantExpansion-UI-Solo-terminal-UTF.log` Solo1/1 Passed：只为本条三关自然经营+保存+重入扩展总Timeout至300秒，逐步等待界限不改；真实完成新增格点击/厨房F01制作/第三桌客户和伙伴问单、交付10分，玩家清桌及伙伴洗净原盘，第三关自然收口后点击完成保存返回选档，再从第二关成功检查点按钮读取9×18、三张桌及原物数量，原文件字节保持。合作完整结果复用`RestaurantExpansion-UI-clear-UTF.log`已Passed一条，不重跑。

任务4.1 → 上述Solo普通选档重入 + 合作真实KCP新桌服务 → Passed。任务1至3的精确映射见前述config/core/layout/files/currentUI原日志；8个不同用例不将重复运行重复计数。伙伴仍沿原Ask/Wash分工，玩家负责Clear，不因测试误期待扩充行为。
