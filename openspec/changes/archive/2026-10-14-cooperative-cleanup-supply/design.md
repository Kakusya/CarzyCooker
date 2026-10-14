# Design

## Context

见 [proposal.md](proposal.md)。当前Solo清理／供应、联机核心7/7与前厅5/5均已有真实交付。本案已沿协议17和原SupplyCommand／ReceiveSupply／TakeSupplyPortion接入网络与HUD，4/5已有15项实际UTF证据；普通补货与在途换关尚待完成，不将文件存在当作完成。

## Goals / Non-Goals

**Goals:** 网络参与者能使用原设施与公共供应，数量、身份和跨关货物在同一权威提交。

**Non-Goals:** 不另建库存，不增加采购价格、材料品类或自动重试，不改变 Solo 供应平衡，不开放内部 Submit。

## Decisions

### 1. 物流归持续厨房

```text
Host Main Fiber / ProductRoot
  Child Scene(ProductHost)
    Child Match -> Child RestaurantRuntime
      Component GridKitchenComponent
        Child GridStation / GridItem / processing records
        Component KitchenSupply
          Child SupplyShipment...
      Child Level -> GridActor / FrontWork
    Component HostSession + Ordered MailBox
      Child HostPeer + Ordered MailBox + LocalDelivery or RemoteDelivery
    NetComponent -> Session + ProductPeerBinding
  Child Scene(ProductClient)
    ClientSession + MailBox -> GridClient / supply projection
    UIComponent / GridPresentation

Remote Main Fiber / ProductRoot
  Child Scene(ProductClient)
    ClientSession + MailBox / Session / independent GridClient
    UIComponent / GridPresentation
```

完整基础树见 [核心设计](../2026-10-14-cooperative-restaurant-core/design.md)。新增接入不改变 Solo SupplyComponent／Shipment 的实际挂载方式。配置先验证，厨房创建供应 owner，再加入玩家和安装状态；换关保留供应与物品，只重建当前关角色。成员离线不释放公共货物，整轮停止才释放供应树。

### 2. 白名单和完整预期值

在核心已开放动作上追加 FillWater、DrainLiquid、DiscardSolid、ReceiveSupply、TakeSupplyPortion 与 SupplyCommandRequest。名称以已交付源为准，未知动作仍拒绝。来源、包装、货物、工位、完整内容、剩余份数、布局及当前关预期值都从独立投影冻结并复制，不能传可变 DTO 列表。

远端 ClientSession 所属 Session.Call → ProductHost ProductWire／实际 PeerBinding → 保存 wire RpcId、复制独立请求 → Peer.GetActorId／Ordered MailBox 的 GridPeerHandler 或 SupplyPeerHandler。本地 ClientSession.Local 捕获 Peer ActorId → 根 sender.Call → 同一 handler。Peer 校验身份后复制内部 Submit → authority.GetActorId／HostSession Ordered MailBox 的 GridSubmitHandler 或 SupplySubmitHandler → 原 Restaurant 服务步执行。

共享控制沿用核心 Host 权限；采购、收货、清理、拆份属于合法个人操作，不误加 HostOnly。远端不能提交内部 SupplySubmit 或绕过服务步直接调用清空函数。

内外答复用原等待者，响应回传恢复外层 RpcId；handler 消费请求，物理调用观察者 finally 释放响应与在途责任。await 后检查原 owner／绑定；UI 退出不撤销已扣额事实，也不向新绑定交付旧回执。

### 3. 保持原设施与供应规则

GridCleanupSystem 仍负责真实设施到位、卫生、锁定与处理后引用收尾。清桌洗碗共址冲突按前厅已有工作裁决；不能因为新增 wire 就允许随身清理。

SupplyCommand 先按原阶段与余额预检，完整提交一次预留；Shipment 随唯一业务时间推进，接收按真实收货槽和当前工位位置重验。满槽不丢货，货物与包装只创建一次。TakeSupplyPortion 移动真实内容而不重新生成材料；包装丢弃按 Solo 已裁决规则，不退款。

选择复用现有来源与实体，而不在 Client 预扣余额或显示本端“待生货”；后者容易在失联后形成两份事实。客户端只显示已提交状态，已接收但未到位仍显示进行中。

### 4. 同版通知、暂停与跨关

按每 Peer 独立 GridWorldSnapshot／Result，复制供应余额、Shipment 进度／待收、工位与包装内容。LocalDelivery.sender.Send(ClientSession ActorId) 或 RemoteDelivery.Session.Send → ProductClient 接收／ClientSession MailBox → 核对世界／关／绑定／版本 → 复制值／Dispose DTO → 原 UI 观察事件。没有独立版本供应缓存。

暂停及 Completed 使用 Solo 实际时钟门槛，恢复不重放被取消的采购；前厅案的 Continue 保留同一物流。后加入取得当前完整快照，不补初始库存。采购已提交后失联保留公共货物；旧关收货意图拒绝，队友可用当前身份合法收取。

## Risks / Trade-offs

- [满槽吞货、重复扣额或跨关补满] → 真实 KCP 重放／满槽／在途换关断言。
- [设施远端绕过到位或占用] → 错设施、接近中与共址洗碗用例。
- [共享数组回收破坏包装或货物显示] → DTO 回收后投影和后加入状态用例。

取舍见 [execution-notes.md](execution-notes.md)。沿用有限样机配置，只需三种既有原料的来源有效性及一条代表性补货实操，不扩展全部菜品矩阵。

## Migration Plan

以 Solo 供应的最终逐功能证据为入口，接协议／客户端调用，再接投影／UI，最后验证真实补货与连续经营。不修改另一会话的 Solo tasks，不手改生成物；仅必要协议变化才改源生成。若实际需要改表，先加载 Luban 并说明具体源字段，不能复制当前中间生成输出当作完成。

## Sources

- [供应请求](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SupplyMessages.cs)、[设施实现](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridCleanupSystem.cs)。
- [Solo 供应设计](../2026-10-14-solo-supply-delivery/design.md)、[前厅设计](../2026-10-14-cooperative-front-service/design.md)。
