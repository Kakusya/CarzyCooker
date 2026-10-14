# Design

## Context

变更前合作核心已接通两菜与 Host 控制，装修仅限 Solo。依赖现有前厅/补货/loopback 案的普通双端链，扩展一次新增工位选择与安装。 动机见 [proposal.md](proposal.md)。本案已完成实施与约定验收，运行范围见 execution-notes。

## Goals / Non-Goals

目标模式：Host/Client 内存餐厅。不接单机升级奖励、联机存档、投票或收益共享凭证；无需等待设备升级。

依赖：[cooperative-restaurant-loopback](../2026-10-14-cooperative-restaurant-loopback/proposal.md)、[solo-interlevel-renovation](../2026-10-14-solo-interlevel-renovation/proposal.md)、[product-local-message-boundaries](../2026-10-13-product-local-message-boundaries/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 沿实际 Host 身份授权

仅绑定 Host 本地成员可选择/继续；远端请求在 Peer 接入和最终服务步都核对当前角色、关卡和绑定。Client HUD 只读候选、选择和结果，隐藏/禁用按钮不是权限实现。

### 2. 保留 Solo 安装算法

联机首版只列原新增工位奖励，不列升级类型。一次 Continue 预检后安装，重复请求/旧 Level 不再造工位；没有合法放置区域保持原状态并显示原因。联机不借 SoloSaveBinding 写个人档。

### 3. 同版协议与独立投影

只给公开装修动作增加显式 wire 支持，字段逐项值复制；协议在实施时以最新基线递增，不固定从 17 直接改某值。双方显示同一候选/选择/Station，后加入读取当前事实；不增加临时 UI 权威。

### 状态归属与交接路径

```text
Product Fiber / RootScene (SceneType.ProductRoot)
├─ Component: ProductRuntime、ProcessInnerSender、Timer、CoroutineLock
├─ ChildScene: ProductHost (SceneType.ProductHost)
│  ├─ Component: HostSession + Ordered MailBox
│  │  └─ Child: HostPeer + Ordered MailBox（绑定 Participant，非其 owner）
│  └─ Child: Match
│     ├─ Child: Participant（成员事实）
│     └─ Child: RestaurantRuntime
│        ├─ Component: GridKitchen → Child 工位／物品／加工
│        ├─ Component: RestaurantRenovation（跨关奖励）
│        └─ Child: 当前 Level → Child 角色／顾客／订单／前厅工作
└─ ChildScene: ProductClient (SceneType.ProductClient)
   └─ Component: ClientSession + MailBox → Component: GridClient（独立值投影）
      同 Scene 受管 GridPresentation、UIComponent、GFEntityComponent
```
Host/Solo 先建立权威及餐厅，再建立接入、客户端和视图；远端 Client 只有客户端一侧。不同运行使用独立 Fiber/世界身份，UI 不持有权威实体。

本地 UI 从当前 LocalMessage.Peer 取得本地 HostPeer ActorId，并由 ClientSession 提供本轮命令上下文，经 ProductRoot 的 ProcessInnerSender/本地队列投递；HostPeer 的 ProductHost handler 校验真实发送者与绑定，再发往同 Scene 的 HostSession Ordered MailBox。远端经 Session 和 ProductWire 显式白名单复制为同一内部请求，再进入该 Peer；Host 不向自己发 UDP。

入口快速答复接受／拒绝，服务步由持有相关状态的 owner 完成验证后提交；接收、启动、完成分别返回，状态先提交再发布投影或观察 Event。通知经 LocalDelivery／RemoteDelivery 回到 ProductClient handler，按运行、Level、绑定代次安装独立值；网络 RPC 号不代替业务编号。框架 Invoke 仅承担既有启动/生命周期，Event 不充当请求答复，不增加 SceneType.All 写状态消费者。

请求池对象只在本次处理内使用，跨 await／队列复制值；每一跳答复对象由该跳调用者收尾，领域快照不引用消息池。视图关闭取消业务等待但原调用继续按答复／有限超时收尾，await 后核对 owner InstanceId 和绑定。成员退出只撤自身接入、取消旧动作并执行既有落物；整轮停止才结束公共发送器、剩余等待和子树。

奖励和已安装结果归 RestaurantRenovation/持续厨房。Host 本地选择走本地消息，远端只读通知走 RemoteDelivery；单成员退出不清除已提交选择，Host 整体退出释放内存餐厅。

## Risks / Trade-offs

主要是远端越权和重复安装。真实 KCP 双端选择／拒绝／重放／换关与实际 UI 足够，复用 Solo 布局算法；存档与 Player 不在本案。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RenovationMessages.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RenovationMessages.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRenovationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationRenovationSystem.cs)

## 饮品布局的安装位置

实际 Launcher 饮品地图无法合法安装原 1／2 奖励；保留旧两菜池，另用配置 5／6 的同类型新增台面／切配台，安装位 (2,16)／(4,16)，无免费物品、无升级。真实 KCP 安装与移动已验证；普通受管 Host 选择／Client 只读／继续换关也已运行。

安装预检失败归 RestaurantRenovation.Failure，经 GridWorldSnapshot66 值复制到双方 GridClient.RenovationFailure，HUD 只读显示；不是 Client 私有状态。新合法准备与成功 Continue 清除该失败，拒绝不改厨房实物／布局／当前 Level。
