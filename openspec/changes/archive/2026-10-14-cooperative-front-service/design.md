# Design

## Context

见 [proposal.md](proposal.md)。已有 FrontWorkPeerHandler／SubmitHandler、ServeOrderMessages 和 Solo 顾客／伙伴／餐具链，但外部入口没有完整开放。前置核心案负责 Host 餐厅、传输适配及控制权限，本案只补前厅和自然跨关的接入与行为证据。

## Goals / Non-Goals

**Goals:** 原前厅权威可被远端合法操作，各端看到同一工作／订单和换关结果。

**Non-Goals:** 不新增前厅状态机或另一套伙伴调度，不重做核心案，不引入联机存档与装修。

## Decisions

### 1. 沿用当前关和持续厨房的寿命

```text
Host Main Fiber / ProductRoot
  Child Scene(ProductHost)
    Child Match -> Child RestaurantRuntime
      Component GridKitchenComponent
        Child GridStation / GridItem       [table and utensil persist]
      Child Level
        GridActor / Customer / Order / FrontWork / Growth / Buff
      Companion -> current CompanionRole
    Component HostSession + Ordered MailBox
      Child HostPeer + Ordered MailBox
  Child Scene(ProductClient)
    Component ClientSession + MailBox -> GridClient / front projection
    UIComponent / GFEntityComponent / GridPresentation

Remote Main Fiber / ProductRoot
  Child Scene(ProductClient)
    ClientSession + MailBox / Session / independent GridClient
    UIComponent / GridPresentation
```

完整 sender／NetComponent 树见 [核心设计](../2026-10-14-cooperative-restaurant-core/design.md)。本案不改变父对象；当前关拥有顾客、订单与工作，餐具和工位属于持续厨房。先安装当前 Level、加入角色，再装入同版前厅数据并开放目标输入；换关先收口旧工作和角色，再建新关角色，最后通知各端。

### 2. 请求复用既有两段裁决

远端 ClientSession 使用原所属 Session.Call 值适配发送 ServeOrderRequest 或 FrontWorkRequest；ProductHost 的 ProductWire 核对白名单和实际 Session.PeerBinding，保存 wire RpcId，复制到绑定 Peer.GetActorId 的 Ordered MailBox。本地由 ClientSession.Local 捕获相同 Peer ActorId，经根 ProcessInnerSender.Call 到达。

ServeOrder 对应 Peer handler／FrontWorkPeerHandler 取得可信成员，复制内部 Submit 后发 authority.GetActorId；ProductHost 下 HostSession Ordered MailBox 对应 Submit handler 接收，由原 GridServe／GridFrontWork 在 Restaurant 服务步验证并提交。复制必须覆盖当前关、命令号、顾客／订单／工作／容器预期身份；不能只按桌号交付。

CompanionWorkRequest／Result 保持本地内部路径：CompanionRole 所属调用从原 authority ActorId 取得裁决，结果由 ProductHost 的 CompanionWorkResultHandler 接收。它与玩家争同一个 FrontWork，不开放到 wire，不按网络心跳推进伙伴。

两段答复仍归原 Call 等待者；外层恢复捕获 RpcId，仅回有效 Session。请求由接收 handler 消费，响应由原观察者 finally 释放，取消后在途责任保留至物理收尾。await 后检查原 Peer／Session／关身份，不自建等待表。

### 3. 投影为一次安装，UI 为观察者

复用核心案的每 Peer 独立 GridWorldSnapshot／Result，补齐 GridFrontBroadcast 与 GridFrontProjection 的实际字段消费：顾客、订单、工作、伙伴角色／成长、得分、桌位和有限池必须来自同次提交。通知经 LocalDelivery 的 sender.Send(ClientSession ActorId) 或 RemoteDelivery.Session.Send，落到 ProductClient 接收／MailBox，复制值后回收 DTO，再通知原 UI Event。

不建立第二份订单缓存或让 UI 自算工作完成。打开／重开 HUD 使用现有 owner 和当前投影，按钮捕获原世界、关及实例，旧 callback 不发新请求。核心已完整复制的字段直接复用，不为本案重复迁移。

### 4. 连续营业与退出

沿用原 FrontWork 单执行者仲裁：一名玩家停止或失联释放该成员认领，保留合法已开始进度；伙伴或队友按原资格接续。单连接退出不停止共用 sender，不清其他工作；餐具始终保留原身份。Host 整体关闭只做技术终止。

营业到点后由既有业务步自然处理用餐、清桌和必要洗碗；排空后 Completed。Host Continue 仍由核心权限入口校验，原餐厅换关保留厨房，清旧关临时状态；客户端禁用旧输入直到本人成员的新关角色与快照匹配。对旧关高版本、旧请求和旧 UI 分别核对身份，不能只比较数值版本。

如 Solo 装修／检查点在本案实施前已落地，沿其明确模式边界保持 Host 为内存餐厅，不擅自套用 Solo 保存门槛或联机选奖；联机装修仍需独立案。

### 5. 选择直接适配，而不拆出新前厅服务

现有 GridServe／FrontWork 的实际规则已在 Solo 验收。继续使用它们并覆盖真实 KCP 边界，改动小且可复用证据；新建远端“服务管理器”会复制工作归属，因此不采用。

## Risks / Trade-offs

- [桌复用或同时交付重复计分] → 真实 KCP 错顾客／重复交付断言。
- [伙伴与两人都推进同一工作] → 同一工作认领竞争、失联接续与原碗身份断言。
- [换关留下旧目标或混版投影] → 自然两关实操，加旧关请求／通知小集。

复用核心传输和原 Solo 服务算法证据，按 [execution-notes.md](execution-notes.md)只补新增远端与跨关功能，不扩大整仓回归。

## Migration Plan

先开 ServeOrder／FrontWork 的外部请求与原所属客户端调用，再补领域投影和 UI，最后运行工作边界与自然两关。没有前置实际证据时不跳过核心直接安装前厅。变更失败保留原未通过结果，修复后只新跑受影响项。

## Sources

- [前厅请求](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/FrontWorkMessages.cs)、[交付请求](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ServeOrderMessages.cs)。
- [餐厅驱动](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs)、[旧前厅验收](../2026-10-13-solo-customer-order-service/execution-notes.md)。
