# Design

## Context

普通 Launcher 原来直接进入 Solo 选档，显式 Host 在 Join 前创建餐厅。本案已接入模式菜单、无餐厅房间及返回重开；保留原显式配置行为。用户已授权全部16案，本案仅内存餐厅，合作选档及持久授权交由后续案。具体运行结果见 [execution-notes.md](execution-notes.md)。

## Goals / Non-Goals

普通入口选择单机、创建合作或填写地址加入；失败和取消可返回。只使用现有 loopback/KCP 与 ET/UGF owner，不引入大厅发现、Ready 投票、公网、物理 LAN、账号服务、存档或新的消息框架。

## Decisions

### 状态归属与实际 owner 树

```text
ProcedureET -> ProductBootstrapBridge -> ET.Init / World
  Product Fiber / Root Scene (SceneType.ProductRoot)
    Components: ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
    Components: UIComponent / ProductEntryPresentation (菜单或房间 HUD，只有显示状态)
    Child Scene: ProductHost (SceneType.ProductHost; Solo/Host)
      Component: HostSession + Ordered MailBox
        Child: HostPeer + Ordered MailBox / LocalDelivery or RemoteDelivery
      Child: Match (RoomVersion、RoomMenuVariant)
        Child: Participant (独立于连接)
        Child: RestaurantRuntime (房间阶段不存在)
          Component: GridKitchen -> Station / Item / Process
          Component: RestaurantRenovation
          Child: Companion
          Child: Level -> GridActor / Customer / Order / FrontWork / CompanionRole
            Components: FrontLevelState / FrontWorkCommands / CompanionGrowth / LevelBuff
      Component: NetComponent -> Session (Host only)
    Child Scene: ProductClient (SceneType.ProductClient)
      Component: ClientSession + Ordered MailBox
        Components: ProductRoomClient (Current/Candidate独立值) / GridClient
      Components: UIComponent / GFEntityComponent
      Component: GridPresentation (只在本人真实世界投影就绪后建立)
```

模式菜单还没有 Match；创建合作先建立 Match、可信 Peer 和客户端，保留真实 Join。GridClient 可以存在而没有世界身份，无餐厅时不创建假厨房、关卡或 Board。显式 Host 仍走原立即建餐厅入口。

### 模式交接和受管 UI

默认配置为 ShowModeMenu 的 Solo，只显示菜单。选单机在原 Root 中转入既有选档；选合作通过启动桥捕获本轮不可变配置与 World，等待 LastPostLateUpdate 后校验身份，停止原 ET.Init、清理上下文并准备新配置再启动。不能在 Fiber 遍历内释放 World，也不原地修改 Prepared 配置。

菜单与房间复用原 UIGridKitchen form；动态地址输入框由该 Mono view 建立和释放，无 Prefab/CodeBind 变更。异步显示与按钮核对 owner、epoch、Client 实例和 RoomVersion；挂起视图不会被刷新自动重开。物理 Call 沿原 handler/超时收尾，取消只退役观察等待。房间快照已提交后才发布视图事件。

### 房间请求的发送、裁决和返回

公开 ProductRoomControlRequest 只带协议、Match/Member/绑定代次、CommandId、RoomVersion、操作和菜单；无餐厅不要求伪造 World/Level。本地 ClientSession 从真实 HostPeer 取得 ActorId，经 Root ProcessInnerSender.Call 入 MessageQueue，ProductHost 的 Peer Ordered MailBox handler 校验绑定，再以内部 ProductRoomSubmitRequest 发到 HostSession Ordered MailBox。后者独占裁决 Match 状态；内部 Submit 不在远端白名单。

远端 Session.Call 经 NetComponentOnRead / ProductWire 显式复制字段、保留 wire RpcId，并取可信 Peer 进入同一路径。本地 Host 不对自身发 UDP。只有本地可信 Host 成员可开/关餐厅；远端即使伪造相同控制字段也拒绝。CommandId 与完整房间意图用于重复重放/冲突判断，旧房间版本不再次创建世界。

开局先创建唯一 RestaurantRuntime，成功后挂入 Match 并排队所有在线成员；失败清理部分对象。关闭先停止该餐厅再释放所属树，保留 Match/Participant/Peer/Session 和 Root sender。房间控制是立即提交的有序控制，答复 Executed 表示已提交；与格子动作的延后结果区分。提交后递增房间版本，再广播各接收者独立 RoomSnapshot；发送失败只记实际通知失败，不把已提交业务改写为失败。

客户端 Joining 时暂存 Candidate，实际 Join/Resume 确认后才安装。只接受当前协议/身份/绑定与不倒退版本；世界关闭或切换时清旧 Grid 值。Grid 快照还须匹配已确认 Room 的世界，旧世界即使携带更高 GridVersion 也不能重开。状态提交后发布 ProductRoomApplied，关闭旧 Board 并打开房间 HUD；开局则待本人角色确认再开放地图输入。

### 创建和退出边界

Root sender 先建立，随后权威/Match/Peer/Client 与真实加入，最后建立相应 HUD；无餐厅世界命令明确拒绝。单成员断开仅撤绑定并沿原规则收尾，不停公共发送器。关闭餐厅仅释放 Restaurant 子树；解散运行/模式切换才收尾整个 Root 与原请求。跨 await 保留独立值和 EntityRef，拒绝旧实例回调；请求、答复及池化 DTO 都由原消息跳的持有者释放，无新等待字典。

## Risks / Trade-offs

主要风险是关闭餐厅误停 sender、旧快照重开世界、切角色时在更新迭代内释放 ET。四项核心真实请求与四项普通 Launcher/UI 用例已覆盖本地/真实KCP、重开、越权、旧上下文、取消/超时和原显式入口。只跑相关小集，设备/包体验收 NotRun；详见 execution-notes。

## Migration Plan

协议从23递增到24。新增房间 Proto 源按排序追加在 MemberResume 文件之后，正常 Proto2CS 生成 Client/ClientServer 与5个 Opcode，旧号不重排。没有 Excel 改表、UI绑定或资源结构改动；动态输入框随原 Mono owner 释放。主规格同步 product-entry/product-session/runtime-instances/startup，原有效场景保留。不提交、push、归档或操作第二工作区。

## Sources

- [启动与房间实现](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRoomSystem.cs)、[客户端投影](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductRoomClientSystem.cs)。
- [受管入口](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryPresentationSystem.cs)。
- [入口用法](../../../../Book/快速开始.md)、[本案逐功能运行证据](execution-notes.md)。
