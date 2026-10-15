# Design

## Context

GridAction 没有普通内容取出动作；GridSupplySystem 的 TakeSupplyPortion 要求来源匹配 SupplyConfig 包装定义。当前普通碗/盘/锅只能整体拿放或沿倾倒/加工/设施清理处理，尚不能从未加工容器取回指定固体。 动机见 [proposal.md](proposal.md)。普通容器整体拿取与包装拆份不是本案所需的指定固体取出，不重复实现已有路径。

## Goals / Non-Goals

目标模式：Solo 与本机 Host/Client。无新案硬前置，保留现有普通容器、打蛋/汤加工和包装语义；不扩展液体和作业拆解。

## Decisions

### 1. 取出边界

首版只处理干净普通容器内、没有关联加工记录的固体；碗内原鸡蛋等未开始加工输入可取。有关联 Working/Paused/完成批次的内容仍拒绝，避免替用户增加取消作业语义。

### 2. 取出边界

命令携当前 Context、来源容器/工位、选中内容身份及预期内容状态；到位重验后一次移除该内容关系并进入空手，不生成新物件。

### 3. 取出边界

复用 GridIntent 现有字段和 ProcessInnerSender/Peer/HostSession/Grid 提交路径；需要新增公开动作则显式维护 wire 白名单与当前协议。

### 4. 取出边界

场景默认动作继续可拿整个容器；详情提供清楚的指定内容取出入口，一次点击一次命令，不做隐式倒空或自动换目标。

### 状态与实际 owner

```text
Product Fiber / Root Scene (SceneType.ProductRoot)
  Component: ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Child Scene: ProductHost (SceneType.ProductHost; Solo/Host)
    Component: HostSession + Ordered MailBox
      Child: HostPeer + Ordered MailBox
        Component: LocalDelivery or RemoteDelivery
    Child: Match
      Child: Participant (connection-independent)
      Child: RestaurantRuntime (optional after room-flow)
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

内容仍由原 GridItem.Contents 和 Location/HolderId 唯一持有；不新增取出库存或 Process。取出只修改原容器关系、同一子物件位置和玩家手槽，外层工位关系保持。 图中的 optional 房间是独立入口案的计划，本案不依赖其已实施。

### 发送、接收与返回

本地操作由 ProductClient 的当前 ClientSession/LocalMessage 取得真实 HostPeer EntityRef，并使用 peer.GetActorId()；ProductRoot 的 ProcessInnerSender.Call 入 MessageQueue，ProductHost SceneType 对应 Peer handler 接收。handler 校验绑定，再转到 authority.GetActorId() 的 HostSession Ordered MailBox；领域 owner 在允许业务步复验上下文和实物后提交。自动后继仍经现有内部消息 handler 接入原 owner，不能由观察事件直接提交。

远端由 Session.Call 发公开协议，ProductHost NetComponentOnRead/ProductWire 只在显式白名单内复制值、捕获原 wire RpcId、取得可信 HostPeer 再进入同一 Peer/HostSession 路径。本地 Host 不向自己发 UDP；远端不能自报内部 ActorId 直达。协议版本按实施时当前版本演进，当前扫描值为 22，不预定多个案的统一下一版。

入口答复只表示 Accepted/Rejected；实际动作或阶段的成功/拒绝/取消沿 ProductCommandOutcome/GridCommandResult 或明确阶段通知返回。Host 提交同版快照后，各接收者独立 LocalDelivery/RemoteDelivery 通知 ProductClient handler，复制独立值后释放 DTO，再发布只读观察 Event；暂停期间控制邮箱仍工作。

取出携指定内容 ItemId 和容器/工位身份，到位时重新核对该内容仍在该容器、为空手、卫生/固体及无加工关联。不同定义不是不同入口，纯转移辅助方法可与包装拆份复用，但不可因此放宽包装来源校验。

已核实 GridKitchenSystem.SubmitGridIntent 使用 `intent.Action >= GridAction.StartJuice` 施加饮品 MenuVariant=1 门槛。若直接把取出追加为较大枚举，会使基础菜单错误拒绝。本案须将该门槛改为明确饮品动作集合，并用 MenuVariant=0/1 各一次取出断言覆盖；未知动作仍拒绝，不借此开放其他动作。

### 创建退出与所有权

创建时先准备 Root sender 与权限上下文，再创建权威/可选餐厅、可信 Peer 和客户端，最后加载原受管 HUD/Board；没有匹配世界、本人确认和视图时不开放地图输入。关卡变化只重建关内角色/工作，持续厨房沿原 owner 留存。

请求/答复每跳由其原调用/handler 收尾，跨 await/队列只留独立值和 EntityRef/InstanceId；同时核对 Match/World/Level/绑定代次。UI 关闭取消对外等待，原物理 Call 仍按答复/有限超时收尾，不提前重复回收池消息或另建等待字典。

成员失联仅撤旧绑定/未执行输入，按既有最后提交格落物和工作接续规则收尾，不停止 Root sender。关闭餐厅回房间只退役餐厅所属对象；整个运行解散才拒绝新入口、处理适用业务关闭、结束原等待并释放 sender/Scene。Root 释放不临时补成功/存档。

### 方案取舍与门槛

无需新增必要产品决定；本案有限子集明确排除加工拆解与液体，未来若扩展另定规则。 采用一项有限原子转移而非通用容器编辑器；不复用 Pour 以免把液体/整批转移规则混在一起。普通台面默认动作仍拿外层容器，指定内容入口仅在详情呈现。

## Risks / Trade-offs

低到中等局部实物转移风险，重点防重复位置与错取。用未加工鸡蛋/盘中固体、满手/错目标/锁定拒绝、双人抢取和旧请求的小集，再检查普通 Editor 操作；不为此重构容器体系。 对应覆盖见 [execution-notes.md](execution-notes.md)。

## Migration Plan

修改手写动作和必要协议源，正常生成，不手改 DTO/ID。仅在实际需要新字段时更改载荷；本案不改变持久结构，但须核对取出后已有成功捕获/恢复仍表达同一唯一位置。不提交、push、归档或操作第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSupplySystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSupplySystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridPrepSystem.cs)
- [Docs/Product/design.md](../../../../Docs/Product/design.md)
