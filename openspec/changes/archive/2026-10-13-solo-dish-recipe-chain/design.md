# Design

## Context

见 [proposal.md](proposal.md) 和 [菜品合同](specs/dish-recipes/spec.md)。当前番茄切配与碗内打蛋由 GridManualProcess 保存真实进度，汤批次尚为独立 change。本设计增加第二道烤吐司，不把候选菜品目录解释为已确认生产配置。

## Goals / Non-Goals

**Goals:** 同一物品体系表达两条真实制作链，菜单在开局前验证原料、工位和容器可用。

**Non-Goals:** 不建通用配方编辑器、订单副本、自动投料或免费半成品；不改汤批次行为。

## Decisions

### 1. Owner 与创建退出

```text
Main Fiber / SceneType.ProductRoot
  Component ProcessInnerSender / Timer / ProductRuntime
  Child SceneType.ProductHost
    Child Match -> Child RestaurantRuntime
      Component GridKitchenComponent
        Child GridStation (counter / chopping / stove / oven)
        Child GridItem (raw / intermediate / bowl / plate)
        Child GridManualProcess / GridAutoProcess / GridBakeProcess
      Child Level -> Child GridActor (hand ItemId)
    Component HostSession + Ordered MailBox -> Child HostPeer + Ordered MailBox
  Child SceneType.ProductClient
    Component ClientSession + Ordered MailBox -> Component GridClient (values)
    Component GridPresentation / UIComponent / GFEntityComponent
```

采用流程 change 的树，Process 是厨房 Child，以稳定 StationId／ItemId 关联工位与材料。工位不拥有第二份作业进度，玩家不拥有作业实体。配置闭包验证成功后才创建一次初始物品；新关复用厨房，不重发面包和盘。整体退出沿原运行关闭顺序停止驱动、等待和视图，再释放树；单玩家退出只释放其认领及手持关系。

### 2. 菜品与阶段依赖

DishConfig 保存菜品定义、交付内容定义、容器定义和阶段引用。阶段引用明确区分 ManualRecipe、BatchRecipe、BakeRecipe、Portion 与 AddToContainer，不为每一道菜写新系统。起始叶子必须指向真实初始供应；深度遍历拒绝循环、缺失引用和缺少设备／容器的菜单。依赖图是定义索引，运行时仍只操作真实 GridItem 和 Process。

汤链为番茄切配、碗内打蛋、批次煮汤、盛碗。煮熟批次不是凭空的独立汤物件，盛装时才形成真实碗内容。吐司链为面包片、烤箱加工、烤面包、添加到盘。烤箱输出是独立半成品，不能直接作为盘装菜交付。两菜菜单闭包包含碗、盘、面包片及汤材料的有限初始库存。

### 3. 烘烤与盘装

新增最小 GridBakeRecipeConfig：输入／输出物品定义、工位能力与必要 Tick。放面包后按材料自动匹配，玩家显式开始；开始完整检查后锁定输入和工位，冻结 RequiredTicks。按餐厅统一 Tick 推进，暂停停钟；未熟拿取、追加输入和重复开始均拒绝。

完成一步内消费原面包、生成一次不同身份烤面包并放回同工位槽，释放作业关系，然后发布观察。不把批次汤强行转成普通整物烘烤，不用 UI 改图标代替消费。盘容量为一，接受兼容烤面包；添加完整校验后一次移动内容，满盘或不兼容失败零写入。容器移动沿当前规则携带内容。

### 4. 实际消息路径

原 HUD／场景意图 → ClientSession 所属请求 → LocalMessage 捕获 HostPeer ActorId → root sender.Call → Peer Ordered MailBox／ProductHost GridPeerHandler → 带可信 Peer 实例及代次的 GridSubmitRequest → authority ActorId → HostSession MailBox／GridSubmitHandler。沿现有动作族增加 BakeStart 与适用容器添加字段，接收答复走原两段等待表；统一 Tick 最终验证和写入。

每个 Peer 的独立 Grid snapshot/result → LocalDelivery sender.Send(ClientSession ActorId) → ProductClient 的原 Grid handler → 复制内容及烘烤进度值 → 回收消息 → GridProjectionApplied。输入／内部请求由原 handler 边界释放，响应由原所属值观察者释放；await 后校验原 owner、世界、关与绑定。完整编号／关闭边界沿 [消息设计](../2026-10-13-solo-local-message-contracts/design.md)，Event 只在提交后观察。

### 5. 源配置与实施依赖

在 Design/Excel/ET/Datas/Product 新增 DishConfig、GridBakeRecipeConfig 源工作簿，注册 __tables__ 和必要枚举，扩充原 GridItemConfig 的面包／烤面包／盘定义。领域表组为 c,s,e，走实际 active ET 五目标 Check／生成；不手改生成代码或 bytes。Model 保存状态、Hotfix 保存规则，View 层只读取投影。协议只追加现有 ZZGridKitchen.proto 末尾，按实际基线调整版本并生成，单机动作不自动加入 wire 白名单。

实施先完成汤交接、流程迁移和工作台的定义／烤箱基础，再做本链；工作台移动／旋转不是本链的前置，完整订单由顾客 change 负责。真实备料证据只复用行为未改变部分；改到的容器规则需要重验。

## Risks / Trade-offs

- [定义图通过但没有真实物资] → 菜单闭包同时检查初始库存、工位和容器，不生成期望半成品填洞。
- [完成或重复请求多产一件] → 一次 Tick 提交输入消费与输出创建，UTF 断言身份、数量、位置和重复调用。
- [图覆盖扩大到完整目录] → 只交付汤与吐司代表链；其他菜、产量和经济仍另案。

## Migration Plan

后续按配置闭包、烘烤、盘装三组接入，每组有行为 UTF；普通 Launcher 实走面包到盘装吐司并检查手持内容。加工时间样机值可调，不承担性能平台或完整目录矩阵。具体覆盖见 [执行说明](execution-notes.md)。

### 当前接入细节

菜品两条、分型阶段十二条、烘烤配方一条，三张源表与物品 8／9／10 正常生成；两关 AllowedDishIds 均为 1／2。`GridDishConfig` 只读真实定义与初始供应，深度遍历拒绝循环及缺口；Raw 不可直接指向加工输出，整物加工和批次／容器输出形态分开验证。菜品是实际内容定义和指定容器的组合，配方是加工规则，订单由前厅独立创建。源配置图不创建物品或改运行库存，厨房只有首次初始化创建面包／盘，换关复用原厨房。

`GridBakeProcess` 是 Kitchen Child，Station／输入 Item 只保存 BakeProcessId；开始时冻结 RequiredTicks，唯一业务 Tick 推进。完成一次消费原输入并创建新输出，清原槽锁，保留完成记录供同版投影展示；暂停／Completed 停钟，下一 Preparing 接续。合法工位搬迁保留同一烘烤实体与原输入，新位置完成后产物留原工位。裸烤面包不识别为菜品，装盘沿原 AddToContainer；识别不消费、不开单也不加分。

协议 8 扩展原 Station／Item／WorldSnapshot，包含 BakeProcessId、独立烘烤值和菜单；实际 `GridBakeProcessState` 放在排序末尾的 `ZZToastBake.proto`，Opcode 10082，原 10000～10081 不重排。原 Grid 请求增加 StartBake 动作，仍走 ClientSession 所属值等待 → LocalMessage 捕获 Peer ActorId → Root sender.Call → Peer Ordered MailBox／GridPeerHandler → 可信 Submit → HostSession ActorId／Ordered MailBox／GridSubmitHandler → 厨房 Tick。回执与最终加工结果分别返回；每 Peer 独立 DTO，经原 ClientSession handler 复制普通值、回收，再发布观察事件。没有新 SceneType 或网络监听，StartBake 不进入既有 wire 动作白名单。原受管 HUD／Board 显示进度、成品和盘内容，await 与旧关结果沿既有 owner／绑定／关身份校验。

## Sources

- [当前备料](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs)、[厨房模型](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)。
- [产品菜品设计](../../../../Docs/Product/design.md)、[汤 change](../2026-10-13-grid-kitchen-soup-batch-cooking/design.md)、[工作台基础](../2026-10-13-solo-workstation-layout/design.md)。
