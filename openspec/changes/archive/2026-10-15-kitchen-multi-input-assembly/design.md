# Design

## Context

DishStage 只有 Raw/Manual/Batch/Bake/Portion/AddToContainer；AddToContainer 终阶段校验容量必须为 1，当前手工配方是单输入切配/打蛋，无法实际完成沙拉等多组件成品。 动机见 [proposal.md](proposal.md)。此段为创建时事实；实际实施已从00–07交付接入，运行结果见execution-notes。

## Goals / Non-Goals

目标模式：Solo 与已支持的本机合作。业务范围见 proposal；复用原 ET Model/Hotfix 与 ModelView/HotfixView 分层，无新业务架构/消息总线。

前置合同：

- 无新案硬前置，复用已交付主规格和相应原执行证据。

不要求 PC LAN、Android、Player/AOT 或全部其他 P1/P2 完成。涉及同一共享 owner 时依次交接。

持久化首轮验收仅扩展已有 Solo 成功档，合作先覆盖内存世界；合作基线/领取交付后才补对应持久验收，不能把共享代码当远端已保存。

交接责任：本案先于领取案交付时，由领取案集成覆盖已落地持久字段；本案后于领取案交付时，由本案持久化任务补现有合作保存入口的一次往返。同一运行可覆盖多个字段，不重跑未变内容。

## Decisions

### 1. 沿既有事实扩展

将 DefinitionId/数量/允许顺序放入现有配方源或必要的最小阶段字段，不按菜名建立专属动作分支。

### 2. 保留原子效果

普通台面仍只占一个外层物品；多组件在定义容器/加工载体内部表达，不能把台面变成无限槽。

### 3. 共享边界与兼容

完整预检后一次消费精确输入并产一个新身份结果，阶段未齐不能伪装成品。独立切配分支可由不同人完成并交接。

### 4. 分批与模式交付

菜谱从同一阶段图解释实际份数和缺口；旧文件读取保留，只有新增记录才写新增阶段信息。

### 首批配方与共享交接

| 菜品 | 精确投入 | 完成路径 |
| --- | --- | --- |
| F01 田园沙拉 | 生菜片×1、番茄块×1、黄瓜片×1 | 分别切配、餐盘内合流、显式完成组装 |
| F02 芝士蔬菜沙拉 | 上述三种切配料各1、芝士片×1 | 蔬菜合流后添加芝士，完成对应配方 |

复用餐盘作为有限组装载具；其定义容量/可接收内容由源配置明确，普通台面仍一外层物。原吐司盘装识别继续要求精确内容，不能因放大容器容量就接受多余材料。以目标配方区分F01/F02，F02的蔬菜齐全时仍等待芝士，不自动先结算成F01。未完成载具可交接；开始/完成分别验证投入身份与数量，非法开始不消费，完成仅一次替换为新成品内容，保留外层盘。

这是后续热餐、甜品、饮品共用的最小多输入/数量/前序表达交接，不预做它们的设备。先扩展现有 DishStage/配方校验；第一批新增内容同时核对 GridPrep、初始布局、Supply、Drink、SoloSaveValidation 的 MenuVariant 消费者，旧0/1语义保持。有限新增菜单必须可从普通入口选择或到达，不能仅在测试注入。

### 状态与 ET 释放归属

```text
Product Fiber / Root Scene (SceneType.ProductRoot)
  Component: ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Child Scene: ProductHost (SceneType.ProductHost; Solo/Host)
    Component: HostSession + Ordered MailBox
      Child: HostPeer + Ordered MailBox
        Component: LocalDelivery or RemoteDelivery
    Child: Match
      Child: Participant (connection-independent)
      Child: RestaurantRuntime (absent while waiting in room)
        Component: GridKitchen -> Child Station / Item / Process
        Component: RestaurantRenovation
        Child: Companion (persistent definition identity)
        Child: Level -> Child GridActor / Customer / Order / FrontWork / CompanionRole
          Component: FrontLevelState / FrontWorkCommands / CompanionGrowth / LevelBuff
    Component: NetComponent -> Child Session (Host only)
  Child Scene: ProductClient (SceneType.ProductClient)
    Component: ClientSession + Ordered MailBox -> Component GridClient (independent values)
    Components: GridPresentation / UIComponent / GFEntityComponent
```

房间、基线及SaveResult/Claim均复用00–07已交付入口。新增阶段/输入引用只归 GridKitchen 下的原 Process 体系；必要时增加有限组装 Process 类型，引用稳定 ItemId/载具而不由 Station 释放。部分组件在真实容器中，完成前不额外创建一份抽象成品库存。

### 发送、接收、裁决与返回



本地操作由 ProductClient 的当前 ClientSession/LocalMessage 取得真实 HostPeer EntityRef，并使用 peer.GetActorId()；ProductRoot 的 ProcessInnerSender.Call 入 MessageQueue，ProductHost SceneType 对应 Peer handler 接收。handler 校验绑定，再转到 authority.GetActorId() 的 HostSession Ordered MailBox；领域 owner 在允许业务步复验上下文和实物后提交。自动后继仍经现有内部消息 handler 接入原 owner，不能由观察事件直接提交。

远端由 Session.Call 发公开协议，ProductHost NetComponentOnRead/ProductWire 只在显式白名单内复制值、捕获原 wire RpcId、取得可信 HostPeer 再进入同一 Peer/HostSession 路径。本地 Host 不向自己发 UDP；远端不能自报内部 ActorId 直达。本案从实际wire27演进为28；GridCommand新增RecipeId21、私有GridSubmit对应RecipeId24，两跳及本地/远端复制均传递独立值。

入口答复只表示 Accepted/Rejected；实际动作或阶段的成功/拒绝/取消沿 ProductCommandOutcome/GridCommandResult 或明确阶段通知返回。Host 提交同版快照后，各接收者独立 LocalDelivery/RemoteDelivery 通知 ProductClient handler，复制独立值后释放 DTO，再发布只读观察 Event；暂停期间控制邮箱仍工作。

### 创建、退出和消息所有权

创建时先准备 Root sender 与权限上下文，再创建权威/可选餐厅、可信 Peer 和客户端，最后加载原受管 HUD/Board；没有匹配世界、本人确认和视图时不开放地图输入。关卡变化只重建关内角色/工作，持续厨房沿原 owner 留存。

请求/答复每跳由其原调用/handler 收尾，跨 await/队列只留独立值和 EntityRef/InstanceId；同时核对 Match/World/Level/绑定代次。UI 关闭取消对外等待，原物理 Call 仍按答复/有限超时收尾，不提前重复回收池消息或另建等待字典。

成员失联仅撤旧绑定/未执行输入，按既有最后提交格落物和工作接续规则收尾，不停止 Root sender。关闭餐厅回房间只退役餐厅所属对象；整个运行解散才拒绝新入口、处理适用业务关闭、结束原等待并释放 sender/Scene。Root 释放不临时补成功/存档。

### 方案取舍

采用现有 owner 上的最小能力扩展，避免把有限首批内容变成独立框架。替代方案是为每菜/每模式复制流程，或一次实现完整候选目录；两者都会增加维护或推迟玩家可见交付。沿当前真实第二消费者增加必要复用字段，避免仅为潜在未来搭平台。

### 实施门槛与决策身份

2026-10-15 已确认首批 F01 田园沙拉、F02 芝士蔬菜沙拉，按目录配方收敛。具体配置 ID、耗时及布局是实施参数；其余目录不属于首批。


### 实际源配置及持久交接

首批物品22–31、切配配方4/5、组装配方6/7、菜品4/5；已有番茄块2继续共享。GridManualMode由枚举源生成Chop1/Beat2/Assembly3，Assembly为DishStageKind6。切配60Tick、组装40Tick，均沿业务100ms步；组装开始冻结RequiredTicks，暂停/搬运不重置。盘10容量4，ExclusiveDefinitionIds为原吐司及两沙拉成品，成品独占，原吐司追加仍拒绝。供应6/7/8只提供有限生菜/黄瓜/芝士包装，每包2份、库存5包、运输40Tick；不提供切好材料。

菜单资源批次Salad2保留原0/1。普通Solo“新开沙拉餐厅”及Host房间“开启沙拉餐厅”可达；Solo501→502→503、合作601→602→603，原菜单不改。该批次先使用两沙拉菜单及有限更长经营时间，后续章节案再整合已确认的两大关节奏，不把这些资源批次算额外首发章节。

Assembly仍是GridKitchen下的原GridManualProcess。InputItemId/TargetItemId都指同一实际载具，盘内内容作为唯一投入；开始完整核对RecipeId、本人手持、完整ExpectedContentIds、精确定义/数量、干净及作业代次。工作/暂停后限制追加/抽取，允许整盘搬运；显式设施清理按原规则取消原作业。Tick直接遍历最多四份实物、不创建临时集合。完成确认沿原KitchenCompletionRequest→HostSession/业务步→原ManualProcess，完整预检后只创建一份新成品并消费内部投入；外盘不变，先提交再发观察事件。

文件v7新增内容语法但Header仍28成员，未完成ManualWork沿原InputKey/TargetKey重映射盘，Plate.ContentKeys保留真实投入；不复制材料库存。v1–v6只读适配，旧目录不开新台/不补料；v7菜单和过程经过校验后可经Solo检查点及实际合作结果票/Root领取完整往返。Root repository的文件职责、Host/Owner隔离和有损替换沿07，不增第二套写入路径。

美术扩展原export_art.py可编辑模型，31个物品/214切片；原Prefab引用未换，布局起点及ArtItemCount由同次源导出。Board/HUD根据生成范围读取同一图集，生命周期和CodeBind均沿现有owner；不新建UI/Entity资源或私有池。

## Risks / Trade-offs

多输入易重复消费或在失败时丢料。相关 UTF 验证数量、错料拒绝、多人抢料、半组装交接与保存恢复；用普通 Editor 实际制作交付首批菜。 任务到最小验收的对应与接受的未验证项见 [execution-notes.md](execution-notes.md)。

## Migration Plan

先修改对应手写源和必要 Product Excel/Proto/CodeBind 源，再经既有生成入口更新消费者；不手改派生 C#/JSON/bytes 或 Unity YAML。旧有效配置/存档明确兼容，新增内容只在选定模式开放。

按实际变更选择相关 UTF 和普通 Editor 入口验收；已有行为未变结果直接引用。不对用户现场清空/回滚，不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)
- [Docs/Product/catalog.md](../../../../Docs/Product/catalog.md)
- [业务目标与本轮优先级](../../../../Docs/Product/full-version-roadmap.md)。
