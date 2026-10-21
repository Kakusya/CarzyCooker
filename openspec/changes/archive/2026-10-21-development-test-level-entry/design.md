# Design

## Context

动机见 [proposal.md](proposal.md)。已核对 `ProductRuntimeBootstrap.cs`、`RestaurantChapters.cs`、`RestaurantRuntimeSystem.cs`、`GridKitchenSystem.cs`、`RestaurantUnlockValues.cs`、`CompanionSystems.cs`、RuntimeInstanceConfig 和 Loader 的 `ProductBootstrapBridge`。当前 `InitializeProduct` 经 `RestaurantChapters.First` 选择章节或菜单首关；不能声称已有任意小关新开接口。GridKitchen 根据菜单加载 InitialLayout，RestaurantRuntime 从传入 Level 定义初始化许可、前厅、伙伴和关时钟，适合作为有限测试起点。

当前章表包含「家常开张／进阶小馆／蒸茶小馆」，分别以 SoloFirstLevelId 1101／1501／1801 指向当前三关链。既有独立菜单首关来自原菜单目录；这些编号是调查事实，运行仍读当前有效表，不硬编码三章菜单内容。

## Goals / Non-Goals

**Goals:** 给原启动入口增加有限测试目的和目录，独立创建无保存绑定的 Solo 临时餐厅，复用既有 owner、真实行为及退出链路。

**Non-Goals:** 不把测试变为新的运行角色／SceneType，不新增网络消息族、监听、CLI 或测试框架，不从 UTF 夹具直接调用私有注入方法；不制作可导出的测试世界快照，不在运行中任意改菜谱、跳阶段或赋予无限物资。

## Decisions

### 目录和配置来源

目录逻辑位于 Hotfix 的 Product 域，Model 保存有限条目值；ModelView 只持页面选择，HotfixView 显示和触发。条目种类使用具名 enum，关卡／章节 ID 使用数值；共用菜单分类继续使用现有 Luban 生成的 ProductMenuVariant。

章节组从当前 RestaurantChapterConfig 的 SoloFirstLevelId 沿原 Next 链读取，校验章节、菜单、顺序及终关；不将合作关、历史未公开关或任意 ID 纳入章节目录。功能组先开放 Basic 基础操作，以及原公开试玩的 Drink、Salad、Growth、Hot、Dessert、Cafe、Steamed、Tea 的首关。功能说明与初始目标为少量手写描述，玩法定义仍来自原表；不复制库存／配方表或增加第二配置管线。

对条目选择和确认分别校验当前配置。缺关卡、工位、材料、合法供应路径或生产许可时，条目失效并显示原因；不能以回退默认关伪装成功。目录验收按实际开放条目枚举，不用固定总数代替覆盖。

现有 ET Luban 为 active，五目标 gameclient／gameeditor／client／clientserver／editor；本案只读现有章、Level、InitialLayout、Supply 等定义，不新增工作簿或生成物改动。实施前仍加载 Luban 技能；若发现必须调整正式数据，先说明实际差异及影响，修订本案范围后再改源及正常 Check／gen，不顺手改平衡。

### 有限启动意图与环境门禁

在 Game/Base/RuntimeInstance 的既有不可变配置上增加有限测试目的、目录／运行目标及上次选择的值；采用具名 enum 与必要的 ID，不携带 ET Entity、Profile、文件路径、委托或世界载荷。Loader 不引用 Model 业务类型，ET 装载后再解析实际条目。

可用条件在稳定 Game 边界读取 `Application.isEditor || Debug.isDebugBuild`，UI 控制入口可见性，实际测试初始化再次校验环境、Solo、目标及无正式记录绑定。现有角色请求、普通 Launcher 默认值不变；没有测试意图就不进入测试。首期不增加命令行开关或 Editor 窗口，避免扩展请求来源。

选择测试、重开或返回经原 `ProductBootstrapBridge.SwitchInitialization` 交接新不可变配置：它在当前帧退出后 StopRun、Reset、Prepare 并重新进入 ET.Init。跨交接只传有限选择值，应用结束自然丢弃；不写 PlayerPrefs、正式存档或新业务文件。沿用原切换忙碌及 owner 身份检查，不建立第二份根树引用。

### 新建测试世界与持久化隔离

增加独立的受限测试初始化入口，与原正式新开／恢复入口分开验证。解析目录条目后选定真实 DRLevelConfig，再按原 RestaurantRuntime 创建厨房、目标 Level、前厅与伙伴；不用 `InitializeSavedProduct`，不调用 `BindSoloSave`，不传入成功记录载荷。

菜单布局和初始实物沿 GridKitchen 的 InitialLayout 选择；目标许可沿 `RestaurantUnlockValues.Initial(target)`，有限供货沿原 KitchenSupply。目录确认前后调用原 `GridDishConfig.ValidateMenu` 核对制作链、设备和有限材料／采购路径；正式章节的指定小关要求菜单许可完整，独立经营成长场景保留原始部分解锁，不替玩家完成成长。中间小关从这些配置基线新建，不伪造前序得分、装修或持久成就；原 Buff 选择、准备、营业、终关和 Next 语义继续有效。实际现有配置已满足起点，不需额外赋予库存或许可，不改生成行或运行中的规则。

运行目的归 ProductRuntime，Match 无 SoloSaveBinding，也不取得合作保存／领取资格。HUD 的保存、授权、导入导出和正式领取入口在测试模式不提供；运行层也拒绝为测试建立正式写入关系，不能只依赖隐藏按钮。结算沿原内存继续／结束路径，分数只属于本轮临时世界。普通新开、菜单试玩及合法恢复保持原接口和存储语义。

选择复用真实运行而不复制 UTF 夹具或伪造完成状态，因为本案要验证实际玩法；测试起点只承诺配置基线，不承诺与正常打完前序关完全相同。

### 实际 owner 树与创建退出顺序

```text
Fiber -> Scene(ProductRoot)
  Components: Timer / CoroutineLock / ProcessInnerSender / ProductRuntime
  Component: ProductEntryPresentation (catalog selection fields)
  Component: UIComponent -> UIGridKitchen (catalog owner)
  Child: Scene(ProductHost) [Solo local authority; no NetComponent listener]
    Component: HostSession + Ordered MailBox
      Child: HostPeer + Ordered MailBox + LocalDelivery
    Child: Match -> Child: Participant
      no SoloSaveBinding / no cooperative save eligibility
      Child: RestaurantRuntime
        Component: RestaurantUnlocks
        Component: GridKitchen
          Children: Stations / Items / Processes
          Component: KitchenSupply -> Children: Shipments
        Child: Level -> Actor / Customer / Order / FrontWork / CompanionRole
  Child: Scene(ProductClient)
    Components: ClientSession + Ordered MailBox / LocalMessage(HostPeer ref)
      Components: GridClient / ProductRoomClient (independent projections)
    Components: GridPresentation / UIComponent / GFEntityComponent
      owned: UIGridKitchen / GridKitchenBoard
```

目录阶段没有餐厅树；确认并通过门禁后，建权威和临时餐厅、本地 Peer／Client、独立投影，再加载 Board／HUD，最后满足原 GridReady。测试标识来自本运行目的，不依赖残留静态 bool；菜单和结算持续可辨。

重开／返回先使当前按钮与加载等待失效，再沿原根 Stop 取消本轮等待、停止 sender、退出 HostPeer／Client 绑定并释放 Match／Restaurant／Level／视图。捕获的旧 Init World 确认已退出后才建立新根，新 WorldId／实例身份独立。重开重用条目值，不重用旧实体、手持物、作业或订单。单人测试整体重开可以停止其整个 sender；不得把该做法用于合作的单成员退出。

### 消息完整路径与异步所有权

测试目录只读当前表，浏览不试发业务命令。新建测试是原 runtime 初始化边界，根尚未建业务对象时直接解析有限意图，不新增“测试 RPC”。进入游戏后，本地玩家通过 ProductClient.LocalMessage 取得当前 HostPeer，根 ProcessInnerSender.Call(peer.GetActorId()) 入队，SceneType.ProductHost 的原 Peer handler 收到后补可信身份，Call HostSession.GetActorId() 进入同一有序提交 handler；RestaurantRuntime 业务步完整校验后写状态。

提交后原 GridBroadcast／LocalDelivery 向 client.GetActorId() 经 sender.Send、ClientSession Ordered MailBox，交给 SceneType.ProductClient 的 GridSnapshotHandler 安装独立值；原命令结果／终态随后返回。响应 Accepted 不显示成加工完成。Event 只在原 ProductRoot／ProductClient 实际消费范围刷新表现，Invoke 保持原本地路由用途，不新增 SceneType.All 接收者。

等待归原 UI／运行的 AsyncOwner 和 OwnedRequest；handler 及调用适配器回收各自消息，UI 仅持值化结果。关闭／重开使原 epoch、EntityRef、世界／关／绑定失效，await 返回后复核，不创建额外等待字典或保存池化 DTO。

### 与入口案的整合

[入口优化案](../2026-10-21-product-entry-flow-clarity/design.md)实施后，测试按钮挂到其首页次级区域；此前可挂原首页，不建占位页面。两案共享 UI1101 的原布局与按钮 owner。正式菜单试玩仍是原模式，测试页另有显著不计正式进度说明，避免用户混淆。

## Risks / Trade-offs

- [测试误写正式记录] → 无保存绑定加实际写入边界校验；用已有正式记录／领取状态验证成功、换关、终关及退出不产生写入。
- [指定中间关缺前置] → 检查每个开放条目的目标、许可、布局、有限实物和采购路径；每项实际入场并执行适用操作，失效不回落。
- [重开残留／迟到加载] → 采用已有整个 Init 交接而非局部清字典；覆盖加工中重开、目录返回及加载中退出。
- [开发包门禁结论越界] → UTF 验证环境判定和拒绝路径，Editor 验证实际入口；未构建的开发／发布 Player 单列 NotRun，不能宣称包内已验收。投入及补验触发见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先增加有限运行意图和环境校验，再接只读目录与受限初始化，随后整合标识、无保存边界、重开返回和相关 UTF。同步 `Book/UI开发.md`／入口用法，不修改当前主规格或历史验收结论。无文件迁移；撤回入口与测试分支即可回退，测试退出不写档。现有五目标生成和独立 Player 构建均不作为本轮规划动作；若后续交付开发包，按既有构建验收流程补包内门禁及进入测试检查。
