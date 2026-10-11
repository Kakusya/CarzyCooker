# Design

## Context

动机和功能边界见 [proposal.md](proposal.md)。当前源码事实：

- `GridKitchenComponent` 是 Match 的 Component，角色、工位和物品是其 Child，固定 Tick 50ms；Match 的 Children 仍只用于 Participant。`GridItem.Contents` 是 `List<string>`，初始碗写入“占位食材”。
- `GridAction` 目前仅有 Move／TakeCounter／PlaceCounter／PickupDrop。`GridIntent.ExpectedItemId` 只能描述一端物品；添加操作还须核对目标碗，不能沿用“只看手中物品”。
- `GridPeerHandler` 将可信 Peer 身份交给 `GridSubmitHandler`；接收意图先快速答复，世界修改只在 Tick。广播每端独立消息，客户端复制数据后释放 DTO。
- HUD 目前按空手／持物显示全部“拿／放”按钮；`HudTargetsKey` 只看手持 ID 和同格落物，不足以响应容器内容和作业变化。Board 使用颜色方块，没有可读工位标签。
- `ET.Entry` 在产品根创建前执行 `ConfigComponent.LoadAllAsync`，可使用已有 `Tables.Instance`。测试 fixture 当前没有加载 Tables；增加必需配置后要为 fixture 加载真实导出数据，不能在生产入口补假配置。
- ET Luban 已启用五目标，表注册使用 `DT*`／`DR*`。`ProductSessionRules.ProtocolVersion` 为 2。详见 [配置约定](../../../../references/data-generation.md)、[消息约定](../../../../references/reactive-and-network.md) 与 [grid-kitchen 主规格](../../../specs/grid-kitchen/spec.md)。

## Goals / Non-Goals

**Goals:** 保持原权威树、消息等待表及四程序集分工，补齐可交接的真实材料和两个手工作业；一台电脑真实验证 Solo 和 loopback KCP。进度、执行者和结果归 Host，展示端只消费投影。

**Non-Goals:** 不为本轮创建正式餐厅、假 LoadedSave 或 Level；不引入通用调度器、第二份配置缓存、私有物品池、私有 RPC 回调表或另一套异步模型。完整产品范围仍见 proposal 的范围外说明。

## Decisions

### 1. 延用临时格子 owner，Process 与工位并列

实际所属树及本轮新增节点如下，类型名可随实现命名调整，但释放责任不变：

```text
主 Editor / Main Fiber / Scene(ProductRoot)
  Components: ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Child Scene(ProductHost，Solo 或 Host)
    Child Match
      Child Participant...（逻辑成员；断线保留，Leave 删除）
      Component GridKitchenComponent（唯一世界 writer / Tick）
        Child GridActor...（手槽、绑定代次、当前工作引用）
        Child GridStation...（位置、类型、台面物品引用）
        Child GridItem...（DefinitionId、唯一位置、容器内容索引）
        Child GridManualProcess...（配方、输入、目标、进度、执行者、结果）[新增]
    Component HostSession + Ordered MailBox
      Child HostPeer + Ordered MailBox + LocalDelivery/RemoteDelivery
    Component NetComponent -> Child Session（仅 Host）
  Child Scene(ProductClient，本地玩家)
    Component ClientSession + Ordered MailBox
      Component GridClient（独立值投影，增加材料与作业信息）
    Component LocalMessage
    Component GridPresentation
    Component UIComponent -> UIGridKitchen（托管 HUD）
    Component GFEntityComponent -> GridKitchenBoard（托管地图）

同一个 Editor / 另一个独立 Fiber / Scene(ProductRoot，KCP Client)
  Components: ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Child Scene(ProductClient)
    Component ClientSession + Ordered MailBox -> Component GridClient
    Component NetComponent -> Child Session
    Component GridPresentation / UIComponent / GFEntityComponent
```

Process 由地图释放，关联工位、容器和输入使用稳定 ID。工位只索引当前作业，不持第二份进度；PlayerRole 不成为加工的释放父对象。暂停后番茄或碗被搬动，Process 仍随其输入／目标身份保留，重新关联合法切配台或新持碗者后可以继续。

创建顺序：既有配置加载 → 根／Sender → 权威地图、初始物品及入口 → 可选 KCP 监听 → Client Join → Tick 创建角色／发布快照 → 受管 Board／HUD 加载。Process 仅在合法开始提交时创建，不在 UI 点击时预建业务对象。备选的正式 Kitchen／Level 重构会扩大存档和跨关边界，本轮不采用。

### 2. 两张最小表与真实内容身份

新增 `Design/Excel/ET/Datas/Product/GridItemConfig.xlsx` 与 `GridManualRecipeConfig.xlsx`，注册为 `DTGridItemConfig/DRGridItemConfig`、`DTGridManualRecipeConfig/DRGridManualRecipeConfig`，组为 `c,s,e`，由现有 ET CLIENT／CLIENTSERVER 表读取，Editor 可查看；不注册成公共 GF 表或另造 UGF ID。注册及字段取实际表头格式，不修改生成物。

物品定义最少包含 Id、Name、Kind、ContainerCapacity、AcceptedDefinitionIds；配方最少包含 Id、Mode（切配台／手持容器）、InputDefinitionId、OutputDefinitionId、RequiredTicks。校验定义引用存在、正耗时、合法类型及容器兼容；配方各为一入一出。显示名只用于展示，不用字符串名字判定业务。

样机数据是番茄、番茄块、鸡蛋、蛋液、碗，以及两条手工配方。碗容量 1，只接受鸡蛋／蛋液；两种加工初值各 60 Tick，冻结到本次 Process，作为可调初值而非正式平衡。固定地图初始一份番茄、一份鸡蛋及一个空干净碗；原“空台面”改为切配台，其余备料台、碗台、食材台与交接台保留。没有自动补货。

GridItem 保留运行 ItemId，新增 DefinitionId。材料进入碗后仍为同一个 GridItem，Location=Container、HolderId=碗 ItemId，容器仅保存内容 ItemId 索引。所有 Item 都由同一地图持有；内含材料不再同时占手槽／台面。搬碗只改碗的外部位置，内容位置由容器身份解析。本轮拒绝容器套容器，不建立通用嵌套系统。

添加完整核对来源手槽、目标碗、四邻位置、容量、兼容和作业锁定后一次提交：清源手槽、更新材料位置、加入碗索引。拿放带内容碗沿用外层转移；不复制内部材料。定义缺失使初始化明确失败，不退回名字判断或占位数据。

现有 UTF fixture 在创建产品树前，使用生成的 Tables 加载当前 CodeMode 的实际导出 bytes，随测试 World 释放；不能全局替换正在运行的 ET World。保留 fixture 的现有占用拒绝保护。备选的硬编码配方会形成正式表之外的第二定义源，本轮不采用。

### 3. 默认动作与明确作业动作

动作解析集中在现有业务／投影共享的选择规则中，场景目标与 HUD 对应目标使用相同条件、动作、目标 ID 和预期物品。它只推导意图，不能提交权威状态。

| 当前状态 | 一次点击目标的动作 |
| --- | --- |
| 持物、空台面 | 放置外层物品 |
| 持鸡蛋、台面上是兼容空碗 | 添加到碗；不是交换 |
| 持物、有物且不兼容／满碗／正在加工 | 明确拒绝，保留物品 |
| 空手、普通台面有物 | 拿取外层物品，包括带内容碗 |
| 空手、切配台有合法番茄且无人加工 | 开始／继续切配；相同目标两入口相同动作 |
| 本人正在该切配台工作 | 停止工作 |
| 队友正在该切配台工作 | 工作中／拒绝认领，不自动加速 |
| 空手、切配台为已完成番茄块 | 拿取番茄块 |

暂停的切配材料需要搬运时提供明确“拿取”动作按钮，不弹确认；再次放到合法切配台后沿原 Process 继续。正在推进的切配输入不可拿走，先停止或移动释放工作。

手持含鸡蛋碗时，HUD 提供“开始打蛋／继续打蛋”，工作时提供“停止打蛋”；无需工位或长按。所有动作都是一次点击直接发意图，不把“选择动作”再变为确认环节。使用既有 TargetTemplateButton 动态展示，不新增业务 UI owner。

GridIntent／协议保留 ExpectedItemId 描述操作输入，增加 ExpectedTargetItemId 描述目标碗或切配输入，并在针对已有作业的操作中携带预期 ProcessId。Action 区分添加、工位作业、手持作业及停止；发起时固定动作与身份，到达再核对，条件变了明确拒绝，不改做拿取或交换。添加和工位作业复用四邻自动接近；手持作业校验本人的手槽，不规划虚构工位。

### 4. Host Tick 推进单一执行者，结果只提交一次

Process 记录 RecipeId、InputItemId、TargetItemId、StationId、RequiredTicks、ProgressTicks、State、WorkerMemberId／WorkerGeneration、OutputItemId。成员、工位和输入只存关系 ID，必要存活引用使用 EntityRef；不能仅凭 MemberId 相同接受旧绑定。

Tick 先处理成员变化和合法新意图，再推进作业。开始／继续完整预检后认领，开始提交的 Tick 不增加进度，后续合法 Tick 每次增加一；一个 Actor 同时一个工作，一个工位同刻一个工作。第二人开始被拒绝；同编号重放沿现有业务编号机制，不再认领、计时或生成结果。新编号重复开始正在工作的目标也不重置／加速。

显式停止、安装合法移动意图或合法搬运暂停作业物品前，释放执行资格并保留进度。入口拒绝的非法指令不改变现有作业。换地面目标仍遵守“完成当前一步后替换路线”；正在手工工作的角色必须停驻，移动开始前暂停工作。工作者、手持目标、相邻位置或绑定失效时也暂停。旧工作者之后的 Stop／Start 不能控制新绑定或别人的认领。

切配完成一次性消费原番茄并创建新番茄块，番茄块落在同一切配台槽；暂停期间材料仍是原番茄，进度属于 Process。打蛋完成消费碗中的原鸡蛋，创建新蛋液 GridItem 并放入同一碗，碗身份／外部位置不变。两类结果都先完整校验再消费／创建，不能中途创建一个可拿取成果。完成后 Process 为 Completed，释放 Worker 和工位占用，记录输出；不能重新开始同一已完成配方。

开始命令的 Executed 表示“工作已成功开始”，停止表示“已停止”；加工完成由权威 Process／新输出事实表示。HUD 根据动作与作业状态显示“加工中／暂停／完成”，不能把开始命令的成功当作原料已加工完成，也不让动画结束决定成果。

### 5. 完整消息路径、投影与释放

本地：`GridPresentation` → `GridClient.CreateGridCommand` → 所属异步入口 `ClientSession.CallProductValueAsync` → `LocalMessageComponent` 的可信 `HostPeer` EntityRef → 根 `ProcessInnerSender.Call(peer.GetActorId())` → MessageQueue → Peer Ordered MailBox → `GridPeerHandler`（SceneType.ProductHost）。

远端：相同命令 → `Session.Call` → loopback UDP-KCP → ProductHost 精确网络 OnRead Invoke／`ProductWire` 白名单 → 复制载荷，捕获原 wire RpcId 和 Session 实例 → 同一 HostPeer 的本地 Call 与 handler。成员身份来自 Peer，客户端自报字段不能选另一工作者或内部 Actor。

共同：Peer 生成带可信实例／代次的 `GridSubmitRequest` → `ProcessInnerSender.Call(HostSession.GetActorId())` → HostSession Ordered MailBox → `GridSubmitHandler` 快速验格式、世界、绑定与编号 → 答复 Accepted／Rejected。固定 Tick 再验材料、容器、位置及认领，提交添加／作业／移动。公共入口不 await 完整加工时长。

立即答复沿既有两段 Rpc 等待返回；wire 桥恢复捕获的外层 RpcId。Tick 提交后，`GridBroadcast` 为每个有效 Peer 创建独立 `GridWorldSnapshot` 和本人 `GridCommandResult` → LocalDelivery 的 sender.Send 或 RemoteDelivery 的 Session.Send → ProductClient OnRead（远端）／ClientSession MailBox → `GridSnapshotHandler`／`GridResultHandler` → 复制 ItemId 内容、定义与 Process 值 → 回收 DTO → 发布 `GridProjectionApplied`。`GridWorldCommitted` 仅在 Host 提交后供观察，Event 不承担请求答复或网络传播。

协议修改仅改 `ZZGridKitchen.proto` 源并走现有 Proto2CS，增加目标身份、DefinitionId、内容 ItemId 和作业状态；如需新增作业状态 DTO，仅追加到该文件末尾，不重排既有类型或插入旧 opcode 区间。移除字符串占位的运行使用，旧字段若保留只作兼容占位，版本 3 两端不依赖它。内部 GridSubmit 仍不进入 wire 白名单；协议版本由 2 升 3，旧版本 Join／Command 明确拒绝。

接收者在回收消息前复制独立集合，不能引用 Host Item、Process 或池化 DTO。按原 WorldId／WorldGeneration／绑定／Version 拒绝旧数据。同一内容图及 Process 由同一快照版本安装，HUD 不建立私有进度计时。已有 UI owner 取消等待、EntityRef 与世界代次检查继续使用。

actor handler 释放输入，Call 调用方用既有 using／finally 消费响应；wire 复制消息与发送通知沿原 ProductWire／ProductDelivery 释放，不把共享 DTO 发送给多端。本轮不新增回调字典或等待加工完成的长期 RPC。

### 6. 成员退出与整轮退出

单成员失联先撤销入口；在本次 Tick 的成员变化阶段释放其手工作业认领，保留已提交进度，再沿原规则移除角色、将手持碗落在最后提交格。内容仍归同一碗；队友同格拾取后可继续打蛋。切配台上的番茄及 Process 留在地图，合法队友接手。重复清理不重复落物或加工。显式 Leave 同样收尾实物，并按原规则删除 Participant。

整体停止先拒绝新工作／停止地图 Tick，结束本轮意图和认领，再关闭受管展示、本地／网络等待及所属树。Process、材料和内容随 GridKitchenComponent 一起释放，不转为存档；晚到请求／加载不得在新世界重新创建作业。单成员退出不得停止公共 Sender。

### 7. 展示与单电脑验收

复用 `UIGridKitchen` 的 Hint／Status／HandText、TargetsRectTransform 和按钮模板，增加工位标签、真实物品／碗内容、当前 Process 状态与进度。标签和色彩只是帮助区分，不以颜色作为业务类型。按钮刷新条件纳入目标物品、内容及作业状态；不沿用只看手持 ID 的 HudTargetsKey，也不每个 Tick 销毁重建所有按钮。

保持原 Prefab／CodeBind 结构与类型 ID，本轮不做独立结构重构。如实际实现确需增加绑定，先经 Unity CLI 安全修改 Prefab 源再重生成；不手改 `.Bind.cs`。UI 关闭清除原按钮订阅，重开不叠回调；不直接 SetActive 受管根。

使用现有 AutoTesting 和 `ProductSessionTests`，拟新增 GridPrepContainer／GridPrepWork／GridPrepWire 前缀小集。发现真实 PlayMode 用例后再过滤执行。已改变容器／协议的旧 GridCore／GridWire 做最小相关回归，更新字符串占位断言，保留原身份／拿放／落物验证意义。

真实操作包含普通 Launcher 的 Solo 两条加工链，以及同一电脑、同一 Editor 内两个独立 Fiber 的本地／真实 KCP 合作交接和接续。依次查看并操作两端受管视图；不能用直接改状态代替输入验收，不需要增加双画面产品 UI。Host 实际监听 loopback 动态非零端口，Client 显式用发现结果，不用 Pipeline 端口。未请求两台设备、第二工作区或手机构建。

## Risks / Trade-offs

- [真实容器关系产生重复位置] → 完整预检后唯一位置转移，UTF 同时断言源槽、碗索引及 Item 关系。
- [暂停或失联后继续推进] → Tick 先收尾成员／移动，再按当前 Worker 代次、位置和目标推进；UTF 验旧绑定与队友接续。
- [开始成功误报加工完成] → 命令结果与 Process 状态分开展示，真实 Launcher 验开始、进行中、暂停和完成。
- [配置源覆盖或部分生成] → 先回读源工作簿与注册，Check 失败停止，核对五目标及真实日志，不用旧产物掩盖失败。
- [临时样机扩大成正式厨房] → 只做两配方和固定初始物资，保留正式餐厅／跨关／存档独立边界。

详细验证取舍、初值和接受的未验证项见 [execution-notes.md](execution-notes.md)，不另建审查平台或性能门槛。

## Migration Plan

1. 经后续 apply 授权后修改两张源表和注册，Check 通过再导出；接入真实 Tables 加载，保留空的旧示例表和原 UI／Entity 注册。
2. 扩展材料／Process、原 Tick 与消息投影，协议源正常生成并提升版本；移除运行中的“占位食材”语义。
3. 接入原受管 Board／HUD，完整 Unity 编译后执行最小相关 UTF、普通 Launcher 和同机 KCP 操作；逐功能记真实结果。
4. 同步本轮能力涉及的当前进度、产品状态及配置／网络说明，严格检查 delta；不自动同步主规格、归档、提交或第二工作区。

样机没有持久数据迁移。异常时先停止本轮，再由修复后的新运行验证；不丢弃用户已有修改。需要撤回时按用户指令撤回本 change 的具体源码／表／协议改动，两端回到一致协议版本，不使用全局 reset。
