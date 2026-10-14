# Design

## Context

当前移动平台在 SoloSavePresentationSystem 明确拒绝保存，540×960 Editor 已显示可用但未证明触控/后台/AOT。本案保留热更与 ET 分层，在全部选定 Editor 功能后交付 Android Solo。 动机见 [proposal.md](proposal.md)。本案为待实施规划。

## Goals / Non-Goals

目标模式：Android Solo。不做 Android 联机、跨平台互通、跨设备存档、自动后台保存或任意断电保证；不关闭 HybridCLR 规避问题。

依赖：[solo-first-chapter](../archive/2026-10-14-solo-first-chapter/proposal.md)、[kitchen-recipe-guide](../archive/2026-10-14-kitchen-recipe-guide/proposal.md)、[solo-playability-tuning](../archive/2026-10-14-solo-playability-tuning/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 平台存档显式隔离

平台入口传入 Android 标识，使用 Unity 本平台 persistentDataPath 和现有文件仓库；Header 校验拒绝 PC 档，不做互导或联网存档。使用同一成功检查点合同，不做后台任意时刻快照。

### 2. 输入仍只产生一套意图

通过既有 Board/HUD 接入单指点击与拖动；拖动结束不触发点击，UI 射线/安全区域/可读字号在实机检查。关闭重开不重复注册触控，投影未就绪或暂停时不提交业务动作。

### 3. 生命周期桥只发通知

稳定壳接收 OnApplicationPause/焦点通知，给 ProductRoot 控制消息入口提交暂停意图；若 OS 已挂起消息泵，则稳定壳保留待暂停标志，恢复后的第一业务步前必须消费。Hotfix 仍是唯一写业务暂停的 owner，不让 Mono 直接改 Level。

### 4. 恢复不补算后台时间

后台开始停止累计可推进时间，恢复时重设采样基点并处理待暂停意图，确认暂停后才允许业务步；前台保留暂停，用户手动继续。OS 强杀只恢复最后成功文件，不承诺保存未结算进度；文件失败不显示保存成功。

### 5. 保留实际平台证据

沿已有入口生成 AOT/资源/热更并读取真实返回码；不能因 KnownIssues 中忽略返回码而当作成功。代表性一台设备足以首版，但具体系统/屏幕/ABI 与未验证范围记录；不把其他机型推断为通过。

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

稳定层生命周期桥只拥有订阅、运行代次和待暂停标志；启动/退出按本轮身份注册/注销。桥 → ProductRoot 控制入口 → 本地 HostSession 暂停请求 → Restaurant 服务步提交 → Client 投影反馈。暂停期间消息泵可答复恢复/保存结果；换运行后旧后台回调不得暂停新餐厅。Android Solo 无 NetComponent 监听。

## Risks / Trade-offs

主要是触控误触、后台继续计时和文件平台错误。Editor 验证可控平台合同，真机验证触控／切后台／杀进程后读最后成功档；不以 Editor 成功替代。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)
- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/SoloSaveValidation.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs](../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)
- [Unity/Assets/Scripts/Game/HybridCLR/HybridCLRHelper.cs](../../../Unity/Assets/Scripts/Game/HybridCLR/HybridCLRHelper.cs)
- [Book/打包与包体验收SOP.md](../../../Book/打包与包体验收SOP.md)
