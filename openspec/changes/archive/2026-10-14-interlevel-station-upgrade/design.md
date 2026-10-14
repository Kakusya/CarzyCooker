# Design

## Context

当前装修在结算生成新增工位奖励并于 Continue 一次安装。本案复用选择和安装门槛，新增同占地单级耗时升级；不替换原 Station 实体。 动机见 [proposal.md](proposal.md)。本案实施中，运行证据见 execution-notes。

## Goals / Non-Goals

目标模式：Solo 首版。不做购买经济、多级树、扩占地、联机升级或升级中途重算；终关不发奖励。

依赖：[solo-first-chapter](../2026-10-14-solo-first-chapter/proposal.md)、[level-buff-variety](../2026-10-14-level-buff-variety/proposal.md)、[solo-interlevel-renovation](../../solo-interlevel-renovation/proposal.md)、[solo-success-checkpoint](../../solo-success-checkpoint/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 选择具体原工位

StationUpgradeConfig 定义切配／烘焙等级 1、3/4 耗时；装修 RewardKind=2 引用升级定义。奖励配置给出可升级设备种类、目标等级与正收益比例；首版每工位最多一次升级，不改变占地/容量/可用配方。选择携带目标 StationId 和预期等级，已升级或无匹配工位不列候选，终关无装修。

### 2. 预检后一次提交

Continue 先验证存档/奖励/Station 身份和等级，再原位更新等级；保留 StationId、布局、容器和旧作业。失败零变更，最新请求重放只返回原结果。奖励安装归 RestaurantRenovation，不由 UI 更新 Station。

### 3. 时间和保存保持可解释

仅新启动作业读取工位比例；与 Buff 从基础时长相乘后统一 ceil、min 1，不连乘已经缩短的值。保存记录稳定配置/等级及冻结作业参数，旧档适配等级 0；恢复不重新施加奖励或重算作业。

### 4. 与合作装修的交界

本案仅 Solo 候选可出现升级。cooperative-renovation-choice 显式只开放已支持的新增工位奖励；不能因共享候选表让远端遇到未实现升级。

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

Station 等级归持续厨房工位；选择结果归 RestaurantRenovation，Level 完成只提供奖励机会。升级请求仍发 ProductHost 的装修 handler，经 HostSession 检查控制权，在服务步安装。保存沿现有链：ProductClient 的受管入口 → 本地请求 → HostSession/Match 下 SoloSaveResult 捕获纯值 → ProductRoot 的 SoloSaveRepository Ordered MailBox 写文件 → 结果回原 Match/Client。Repository 比 Match 长寿，晚到 I/O 不写新 Match；文件完成后才允许离开原结算。沿用临时文件、Flush(true)、不覆盖发布和 Result/Profile 去重；重试同一结果不重复奖励。Header 不保存 ET ActorId/EntityId。

本案从实际文件 2 升到 3，协议 19 升到 20；后续案基于届时版本继续递增。旧有效格式通过显式只读适配生成内存值，不重写原文件；未知/损坏文件保持拒绝。不支持迁移的新增字段不得静默丢弃。

## Risks / Trade-offs

替换工位可能丢物，重放或恢复可能重复升级。用同身份、旧作业耗时、失败零变更与实际文件往返足够；不扩大全布局组合。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRenovationSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridRenovationLayout.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridRenovationLayout.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/GridKitchen/GridKitchen.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveCapture.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveCapture.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSaveRestore.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSaveRestore.cs)
- [openspec/specs/kitchen-workstations/spec.md](../../../specs/kitchen-workstations/spec.md)
