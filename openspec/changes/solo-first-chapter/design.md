# Design

## Context

当前关卡校验要求有效 NextLevel，当前保存也依赖下一关。已有成功检查点与装修可复用；三关终点按用户本轮明确裁决，必须同时改变结束、文件与选档语义。 动机见 [proposal.md](proposal.md)。本案为待实施规划。

## Goals / Non-Goals

目标模式：Solo。不实现联机三关、账号解锁、跨设备领取、覆盖旧档或强制失败；不借菜单变化裁决跨关许可缩小。

依赖：[solo-success-checkpoint](../solo-success-checkpoint/proposal.md)、[solo-interlevel-renovation](../solo-interlevel-renovation/proposal.md)、[solo-supply-delivery](../solo-supply-delivery/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 明确终关配置

首版三关使用同一两菜菜单、有限供应和现有工位，允许调节来客/耗时/阈值。终关用明确字段/合法终端标识表达，配置验证辨别终关与错误引用，不把缺表当通关。零星仍成功；未满足订单仍自然离席。

### 2. 完成事实留在原 Level

订单成功/未满足计数在既有提交点各累计一次；结算显示分数、星级与数量解释。第三关 Draining 排空后 Completed，停止产生装修候选，禁止 Continue 建第四关；提供保存完成并返回选档的显式动作。保存失败停留结果页可重试。

### 3. 保存的两种结果不混淆

新格式区分可继续检查点与 ChapterCompleted，后者无 NextLevel，浏览可看结果，不能恢复旧餐厅；另选新开建立新运行。旧格式依据已保存 NextLevel 适配为可继续，不用后来三关表把历史档追认为通关；保留兼容的历史关定义或明确只读适配关系，使原有效档可读。

### 4. 分模式交付

本案只启用 Solo 三关配置；Host/Client 使用现有内存餐厅定义，不借共享表使未接线合作突然进入新终关。共同底层校验可以复用，但模式配置和验收分开。

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

保存沿现有链：ProductClient 的受管入口 → 本地请求 → HostSession/Match 下 SoloSaveResult 捕获纯值 → ProductRoot 的 SoloSaveRepository Ordered MailBox 写文件 → 结果回原 Match/Client。Repository 比 Match 长寿，晚到 I/O 不写新 Match；文件完成后才允许离开原结算。沿用临时文件、Flush(true)、不覆盖发布和 Result/Profile 去重；重试同一结果不重复奖励。Header 不保存 ET ActorId/EntityId。

文件格式在实施时基于届时最新版本递增，不能多个 change 各自假定“都用版本 2”。旧有效格式通过显式只读适配生成内存值，不重写原文件；未知/损坏文件保持拒绝。不支持迁移的新增字段不得静默丢弃。

新增关序和结果计数归当前 Level，持久完成事实归文件 Header；客户端只有复制值。正常换关先保存/校验再安装新 Level，终关先成功发布文件再释放餐厅回选择器。

## Risks / Trade-offs

终关与文件兼容可能误建世界或丢失有效进度。保留不覆盖发布，强化实际文件旧格式读取、终态重试及一次普通三关保存退出重入；不验证任意断电。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/RestaurantRuntimeSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Product/SoloSaveData.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)
- [openspec/specs/restaurant-level-lifecycle/spec.md](../../../openspec/specs/restaurant-level-lifecycle/spec.md)
