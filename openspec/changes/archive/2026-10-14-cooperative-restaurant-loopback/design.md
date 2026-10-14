# Design

## Context

见 [proposal.md](proposal.md)。用户要求创建缺失前置，原 0/7 大案的开发任务已迁入三个前置。本案仍未实施，保留完整本机合作交付目的，集中完成普通双 Editor 的集成验收。

## Goals / Non-Goals

**Goals:** 在真实独立进程中检验前置能力组合与角色排列；使进度记录和实际运行范围一致。

**Non-Goals:** 不重复开发三个前置，不以验收名义引入新大厅、传输、状态副本、私有等待表或测试平台。不创建联机装修／存档／重连候选的实现。

## Decisions

### 1. 依赖与唯一任务归属

```text
Solo restaurant --> cooperative-restaurant-core
                         |
                         v
                   cooperative-front-service
                         |
Solo cleanup + supply --> cooperative-cleanup-supply
                                  |
                                  v
                     cooperative-restaurant-loopback
```

前三案分别持有其新增行为与局部真实 KCP 验收，本案持有双 Editor 集成。原 product-session delta 移到核心；本案 cooperative-restaurant 只保留组合体验合同。完整旧任务映射见 [execution-notes.md](execution-notes.md)，没有删除待做行为或把迁移算成完成。

### 2. 实际 owner 树及消息边界

```text
Host Main Fiber / ProductRoot
  ProductRuntime / Timer / ProcessInnerSender
  Child Scene(ProductHost)
    Child Match
      Child Participant...
      Child RestaurantRuntime
        Component GridKitchenComponent
          Item / Station / Process / SupplyComponent -> Shipment
        Child Level -> Actor / Customer / Order / FrontWork / Buff
        Companion
    Component HostSession + Ordered MailBox
      Child HostPeer(local/remote) + Ordered MailBox
    NetComponent -> Session + trusted PeerBinding
  Child Scene(ProductClient)
    ClientSession + MailBox -> independent GridClient
    UIComponent / GFEntityComponent / GridPresentation

Client Main Fiber / ProductRoot
  ProductRuntime / Timer / ProcessInnerSender
  Child Scene(ProductClient)
    ClientSession + MailBox / NetComponent -> Session
    independent GridClient / UIComponent / GFEntityComponent
```

树、创建／停止顺序与完整消息责任复用 [核心设计](../2026-10-14-cooperative-restaurant-core/design.md)、[前厅设计](../2026-10-14-cooperative-front-service/design.md)、[物流设计](../2026-10-14-cooperative-cleanup-supply/design.md)。本案不增加 owner；实际验收仍核对：Host 是唯一 Restaurant 驱动，Client 只有独立投影，Solo 无监听。

本地 ClientSession 经 LocalMessage 捕获 Peer ActorId，由根 sender.Call 发给 ProductHost Peer MailBox；远端 Session 经 ProductWire 可信绑定转交同一 Peer。Peer 发内部 Submit 到 authority.GetActorId／HostSession Ordered MailBox，Restaurant 服务步裁决。两段答复回原等待者，外层保留 wire RpcId；每 Peer 独立通知经 LocalDelivery.sender.Send(ClientSession ActorId) 或 RemoteDelivery.Session.Send，到 ProductClient 复制值并释放 DTO 后通知视图。

已有等待者／handler 各按其责任收尾，await 后验证原实例与绑定；单成员退出不 Stop 共享 sender。整体先停输入／业务、关闭所属等待与视图，再 Stop sender 和释放网络／树。观察事件只在本地提交后发布，不承担网络答复。验收不通过直接写领域状态或复制第二份厨房跳过这些路径。

### 3. 前置证据复用与两种排列

先核对三案逐任务 evidence、实际协议和当前源行为。未改变且覆盖相同验收点的 UTF 直接复用；缺口只运行相关最小 PlayMode，不因集成阶段重跑整仓或完整历史套件。

按分支 multiplayer-baseline 发现第二工作区，再按完整 projectPath 匹配两个 Editor。用已授权 main 提交及明确同步清单统一基线；第二工作区只执行与回报。存在未保存工作、忙碌测试或基线前置不足时保留相应 NotRun，不强停、不自行提交或 cherry-pick。

产品监听继续使用实际 loopback 非零 endpoint，工具 Pipeline 端口分别从实例发现；不固定端口，不扫描，不创建 PortRegistry。

两种排列分别是主工作区 Host／第二工作区 Client，以及主工作区 Client／第二工作区 Host。每种均经普通入口真实操作：加入、Host 布局／Buff、两菜分工和交付、设施清理、补货、暂停恢复、原餐具回收和自然换关。失联接续、旧世界隔离等边界已有适用证据可复用；至少在双进程中检查一次真实 Client 连接失联与 Host 继续操作，再检查整轮退出重开。

### 4. 集成失败归回实际能力

若发现属于前置合同的具体缺陷，修复记录仍归相应前置，关联本案发现；已归档前置则按实际差异创建小修复案，不改历史运行结果。再次验证只覆盖改动行为与本次集成受影响步骤。本案不以重写一遍联机实现来“完成集成”。

## Risks / Trade-offs

- [两端基线或角色不一致] → 记录实际工程路径、角色、协议和明确交接范围。
- [同 Editor 通过掩盖独立进程问题] → 保留两个普通 Editor 的真实运行，不降格为截图／fixture。
- [任务重复或已验收范围扩大] → 三案各负开发，本案负集成，记录单机与联机证据范围。

严谨程度及最小验证见 [execution-notes.md](execution-notes.md)。接受 Player／LAN／公网及长时间压力未验证，不添加本案之外的完成门槛。

## Migration Plan

完成三个前置并取得对应证据后进入本案。外部执行条件不足时只整理可复用证据和缺口，双 Editor 任务不勾选。通过后更新当前进度中联机的实际交付行；主规格同步、归档和提交按各自授权处理。
