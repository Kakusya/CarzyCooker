# Design

## Context

2026-10-16 实施复核：登记表已有 GridManualMode、ProductMenuVariant、RenovationRewardKind。保留这些名称及新增内容，扩展到 14 表／22 字段，不回退已交付能力。ProgressDeliveryValue.Kind 已是 ProgressDeliveryKind，沿用现有契约。

动机与用户确认范围见 [proposal.md](proposal.md)。这是横跨源表、四层消费者和序列化边界的重构，因此保留设计。

实施前核对了所有 Product 工作簿及消费者。`luban.conf` 启用 ET，五目标为 gameclient／gameeditor／client／clientserver／editor；Product 表使用 c,s,e。将剩余手写配置枚举迁入现有 Excel 登记表，生成后删除重复定义，保持数字编码。

Buff 手册已随业务扩展更新到三类管线，本案补充具名枚举填表方式。章节与菜单当前包含七种分类；本案只迁移类型，不新增内容组合。工作区已有房间、升级、基线、内容和表现成果，实施保留当前文件，不从 HEAD 覆盖。

## Goals / Non-Goals

**Goals:** 配置和运行域共用分类定义；强类型贯穿内部字段与参数；源表可读；原行为和数字兼容；未知分类拒绝。

**Non-Goals:** 不更换 owner、消息路由、存档格式、CodeMode 或生成工具，不把可扩展的数据行集合封闭成 enum，不扩大底座注册系统和第三方代码改造。

## Decisions

### 1. 单一枚举源与具体映射

配置相关枚举注册到 `Design/Excel/ET/Datas/__enums__.xlsx`，显式成员值、group=c,s,e，能力类型 flags=true。表头改为对应枚举类型，数据改单个成员名或 `|` 组合。通过现有导表获得 ET 运行时与 Editor 的生成类型，删除迁移类型的手写重复声明。不同时在 XML、Excel、手写 C# 维护三份映射。

以下为实施时核对的 14 张产品表、22 个分类字段，包含已有枚举字段的一致性核对。数值以当前消费者为准，禁止重新编号。

| 源表 | 字段 → 类型 | 保留值与含义 |
|---|---|---|
| GridItemConfig | Kind → GridItemKind；Form → GridMaterialForm；MenuVariant → ProductMenuVariant | Material=1／Container=2；Solid=1／Liquid=2／Container=3；菜单 Basic=0、Drink=1、Salad=2、Growth=3、Hot=4、Dessert=5、Cafe=6 |
| GridManualRecipeConfig | Mode → GridManualMode | Chop=1、Beat=2、Assembly=3，保留已有生成类型 |
| StationConfig | Kind → GridStationKind；Capabilities → GridStationCapability | Counter=0 到 SteamMachine=15；能力位 None=0、Chop=1 到 FoamMilk=131072，保留所有原成员 |
| GridAutoRecipeConfig | StationCapability → GridStationCapability；MenuVariant → ProductMenuVariant | Cook、Juice、Mixing、Extract、HeatMilk、FoamMilk；使用同一菜单枚举 |
| GridBakeRecipeConfig | StationCapability → GridStationCapability | Bake=4、Fry=4096、Grind=16384 |
| DishStageConfig | Kind → DishStageKind | Raw=0 到 Grind=11，保留 Assembly、Fry、BakeInCarrier、Unmold、Blend 等新增阶段 |
| CompanionConfig | Post → CompanionPost | FrontService=1 |
| BuffConfig | EffectKind → StationWorkKind | Batch=1、Chop=2、Bake=3 |
| StationUpgradeConfig | StationKind → GridStationKind；StationCapability → GridStationCapability；WorkKind → StationWorkKind | 与工位类型、能力及上述加工类别一致 |
| RenovationConfig | RewardKind → RenovationRewardKind；Orientation → GridOrientation | InstallStation=1、UpgradeStation=2、UnlockDefinitions=3、ExpandSpace=4；Unrotated=0、CounterClockwise90=1、HalfTurn=2、CounterClockwise270=3 |
| InitialLayoutConfig | Orientation → GridOrientation；MenuVariant → ProductMenuVariant | 同上旋转枚举；菜单 0/1 |
| LevelConfig | MenuVariant → ProductMenuVariant | 使用完整菜单枚举 |
| SupplyConfig | MenuVariant → ProductMenuVariant | 使用完整菜单枚举 |
| RestaurantChapterConfig | MenuVariant → ProductMenuVariant | 保留现有生成类型，数据改用具名成员 |

`StationWorkKind` 表示 Buff／升级针对的加工类别，两个现有消费者已有相同映射。它不同于 `KitchenProcessKind`（Manual=1、Soup=2、Bake=3、Juice=4），禁止因为都带 Kind 就强转复用。MenuVariant 在布局／供应中的零值仍按原规则属于公共来源，在关卡中表示基础菜单；类型化不改变这项过滤约定。

不采用“只把数字提成 const”的方案，因为表仍缺少字段类型约束；也不为每张表制造重复 enum 和转换层。保留既有生成目标及命名空间：运行域为 ET，Editor 为 ET.Editor；按当前 CodeMode 的实际排除规则编译，不同时启用 Client 与 ClientServer 两份定义。DotNet.Model 本来链接 ClientServer 生成目录，不能引入对 UnityEngine 或 !DOTNET 产品手写类型的生成依赖。

### 2. 运行域和表现消费者

迁移上述类别的 Model 字段、意图值、投影、构造函数、Hotfix 参数、HotfixView 分支及 ModelView 状态。重点入口是 GridPrepSystem、GridDishConfig、GridStationConfig、LevelBuffSystem、StationUpgradeConfig、RestaurantRenovationSystem、RestaurantConfig、GridSoupSystem、ProductRoom 系列和选档／入口表现。

新增纯运行域 `GridFacing`，明确 Up=0、Right=1、Down=2、Left=3；用于 GridActor、GridActorProjection、朝向计算和表现读口。工位 `GridOrientation` 是逆时针旋转次数，角色 Facing 是方向，两者不能共用枚举。几何算法可在局部把旋转转成整数循环；图集寻址可在表现边界转索引，索引不反向成为业务分类。

伙伴 `LastNotificationKind` 与 `NotifyFrontResult` 的 kind 也在范围内：新增 `FrontWorkNotificationKind`，None=0、RequestResult=1、WorkFinished=2；消息 EventKind 仍是整数边界。Board 的 labelStates 当前把工作类别、顾客阶段和加工状态强转后装入同一 int 元组；改成保留各枚举类型和空态的轻量值快照，继续由原 view 缓存和释放，不新增业务状态或逐帧分配。

现有产品状态／动作枚举继续复用，参与消息／保存的成员补显式原值；不重排、不统一补 `Unknown=0`。`GridStationConfig` 当前按 `1 << (Kind - 1)` 推导能力，改为有语义的映射并保持原允许组合；避免枚举序号继续隐式决定规则。类型验证在配置加载、请求和恢复边界执行，逐帧路径不引入反射枚举、LINQ 或装箱检查。

### 3. 数字保留边界

- 表主键、外键、实例 ID、配方 ID、关号、成长次数、等级、数量、比例、Tick、版本和图集索引保持数值。`GridKitchenRules.Tomato` 等是定义引用，不是物品类别；保留现有具名 const。
- Proto 的 int32 字段、字段顺序、Opcode 和存档 MemoryPack DTO 的整数属性保持原状，在现有消息收发和存档 Capture／Validation／Restore 转换处使用枚举。仅允许这些实际边界存在对应 int 转换，不在领域层保留整套 int API。
- `ProductError` 依赖 `[UniqueId]`；当前属性只允许 Class，并检查 const int。它属于框架错误码注册，本案保留，避免为外观一致而连带改分析器。平台编号的稳定 Game 层适配同样保持现有整数契约。
- 生成 C#、bytes、JSON、ID、Proto 派生物不手改。产品表字段类型变化须正常生成后检查所有适用消费者；gameclient／gameeditor 五目标照常检查，但不增加 Product 的 gc/ge 分组。

边界读取先核对适用枚举域或合法位掩码，再沿原错误路径拒绝；Flags 不能使用“是否为单一枚举成员”来判定组合。零值按各类型的现有有效性处理，不能笼统拒绝零或静默映射默认值。协议版本和存档版本不因类型重命名递增；发现确实需要编码改变时停止该扩大部分，先修订设计。

### 4. 现有 owner 与消息边界

本案不新建运行节点，只改变值的类型；实际结构保持：

```text
Product Fiber / Root Scene (SceneType.ProductRoot)
  Component ProductRuntime + ProcessInnerSender + Timer
  Child Scene ProductHost (SceneType.ProductHost, Solo/Host)
    Component HostSession + Ordered MailBox
      Child HostPeer + Ordered MailBox
        Component LocalDelivery / RemoteDelivery
    Child Match (room/menu selection)
      Child Participant
      Child RestaurantRuntime (optional while in room; menu/runtime owner)
        Component GridKitchen -> Child Station / Item / Process
        Component RestaurantRenovation
        Child Companion
        Child Level -> Child GridActor / Customer / Order / FrontWork / CompanionRole
          Component LevelBuff / FrontLevelState / FrontWorkCommands / CompanionGrowth
    Component NetComponent -> Child Session (Host remote)
  Child Scene ProductClient (SceneType.ProductClient)
    Component ClientSession + Ordered MailBox
      Component GridClient (independent projection)
    Components GridPresentation / UIComponent / GFEntityComponent
```

源表加载和配置检查在创建餐厅前完成。新开／恢复从同一枚举数据建权威，再把类型化值投影给客户端。关切换仅重建原关内 owner；关闭餐厅保留房间，整轮退出按原生命周期释放。

以菜单选择和布局操作为代表：ProductClient 从当前 ClientSession 的可信本地连接取得 HostPeer，再以 `peer.GetActorId()` 经 Root ProcessInnerSender.Call／MessageQueue 投递；ProductHost 的 Peer handler 取得 `peer.Authority.GetActorId()`，再交 HostSession Ordered MailBox。对应房间／布局 handler 验证原绑定、代次及新迁移分类，把独立值交原领域 owner；Host 在原提交时机写状态，答复和结果通知保持原区分。

远端仍由 Session.Call → ProductWire 白名单 → 同一可信 Peer／authority 路径；本地 Host 不向自身发 UDP。Host 提交后通过 LocalDelivery／RemoteDelivery 将独立值送回 ProductClient handler，客户端复制后再发观察事件。转换不增加发送路径、第二份业务状态或请求等待字典。

跨 await 继续保留 EntityRef／InstanceId 和 Match／World／Level／绑定代次检查。请求与答复每跳由原 handler／调用方释放；队列与投影不保留池化 DTO。单成员退出不停止公共 sender，整体退出仍由原 Root 收尾。

### 5. 决策账本

已确认：全部产品代码和产品表的分类枚举化，保留数值参数及 ID。技术选择：配置枚举由 Luban 单一源生成，纯运行枚举留 Model，传输／保存保持 int 编码。未采用全仓替换、所有数字 enum 化或修改协议生成器。剩余产品裁决：无；具体运行过滤与 Editor 可用状态在实施时按现有工具发现。

工作区在本轮调查期间新增了 `CooperativeClaims.cs`，其中 `ProgressDeliveryValue.Kind` 尚未检索到生产／消费代码，不能推定 1/2 的含义。实施前针对该字段读取届时已落地的领取投递契约，若仍未闭合则列为该在制功能的明确未验证项；不能借本案实现领取或自定编号。其余已核实分类不因此阻塞迁移。

## Risks / Trade-offs

- 原值重排或误合并分类 → 显式原值、类型表及旧数字边界断言；特别区分 StationWorkKind／KitchenProcessKind 和 Facing／Orientation。
- 组合能力误判 → 真实导出洗碗位 400 的加载与接水／洗碗／排水行为；未知位另测一个反例。
- 只改表头遗漏读口 → 同批生成及迁移消费者，用编译定位类型遗漏，再运行覆盖各分类的行为小集。
- 误动用户源表或未提交工作 → 仅改清单字段，保留其余行、公式、样式和验证；检查源及生成 diff，不做整仓回退。
- 生成有临时不完整窗口 → 不在旧生成物上继续验收；Check 成功才正常生成，编译与相关行为验证通过才交付。

严谨程度、具体运行映射和可接受未验证范围见 [execution-notes.md](execution-notes.md)。不新增静态分析器、通用枚举框架或穷尽测试平台。

## Migration Plan

1. 按上表更新枚举源与指定字段，保留数值；使用现有可用工作簿编辑方式，先检查文件占用，保留非目标内容。
2. 运行五目标 Check 后正常生成，再迁移所有消费者及现有测试 fixture。不得以手写生成类型或保留双份枚举绕过编译。
3. 验证边界、配置和各行为小集，执行普通 Launcher 菜单／加工／方向入口；更新短记录与用法文档。
4. 若失败，修复源后重新生成和新建测试运行；需要撤回时仅撤本案可辨认的编辑，不能覆盖用户其他变更或旧存档。

## Sources

- [源表与注册](../../../../Design/Excel/ET/Datas)
- [现有生成目标](../../../../Design/Excel/ET/luban.conf)
- [产品 Model](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product)
- [产品 Hotfix](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product)
- [几何与旋转](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridStationGeometry.cs)
- [UniqueId 注册约束](../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Analyzer/UniqueIdAttribute.cs)
- [章节内容的现有菜单边界](../2026-10-15-restaurant-chapter-content/design.md)
- [当前进度与原运行记录](../../../../CarzyCooker当前进度.md)
