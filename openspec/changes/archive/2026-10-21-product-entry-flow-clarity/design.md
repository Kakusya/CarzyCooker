# Design

## Context

动机见 [proposal.md](proposal.md)。已核对 `ProductEntryNavigation.cs`、`ProductEntryPresentationSystem.cs`、`SoloSavePresentationSystem.cs`、`MonoUIGridKitchen.Layout.cs`、原 Prefab 和[入口主规格](../../../specs/product-entry/spec.md)。当前已经有 Entry 布局、单人分层、开局预览及 UI1101 的唯一实例交接，本案在这些基础上调整。当前有效目录为三章；不能沿用较早夹具里固定两章的假设。

当前 `ChooseSoloEntry` 关闭入口后发布 `SoloSaveSelectionRequested`；Solo 选择页负责浏览记录，直到确认才初始化餐厅。合作角色通过 Loader 的 `SwitchInitialization` 完整切换，不能在活运行中改 Role。

## Goals / Non-Goals

**Goals:** 在原页面模型内表达首页主次、新开同页选择与预览、继续读取状态；保持原持久化及合作裁决，导航本身不建立世界。

**Non-Goals:** 不创建第二套菜单框架、独立全局导航服务或新 UI 类型，不改经营 HUD、表内容、协议和保存格式。测试开局归[开发测试入口案](../2026-10-21-development-test-level-entry/design.md)，加载阶段说明归[现有就绪案](../../product-startup-readiness-guidance/design.md)，各案可独立实施。

## Decisions

### 页面与视觉组织

首页采用三项主要操作和次级导航。次级区域为更多玩法、进度管理、设置；合作领取提示归进度管理，声音与字号沿原设置入口。保留原只读授权检查与显式导入／替换动作，不将重排当作业务授权。

单人新开复用原选择页与预览数据，默认选中章1，切换章节同时更新菜单、关链、模式和保存说明。主要确认只保留一处，返回回到首页；不再要求经过一个通用单人首页。继续页仍先明确选择记录、走原详情校验、再确认继续，不能自动选最近一份。

`ProductEntryPage`、`SoloEntryPage` 继续以具名 enum 表示有限页面，新增只读浏览状态同样使用具名 enum。页面只保存选中 ID、当前状态与有限派生值，不复制记录载荷或运行世界。首页只读浏览复用原 Repository 请求：成功才显示数量；加载、失败、空集分别呈现，重试不创建身份、成功记录或授权。

入口保留同一个 `UIGridKitchen` 的 Entry 布局：标题／背景、主要操作、内容／预览与固定导航。首页隐藏无用途的经营指标及空动作框；仅改变内部子区域，不直接失活受管 UI 根。竖屏纵向、宽屏列表与预览并排，长正文／列表独立滚动。沿用既有美术与序列化引用；需要绑定变更时修改 Prefab／CodeBind 源并重生成，禁止手改生成 partial。没有新增 UI 表行或新美术生成依赖。

采用原容器能控制消费者迁移和 owner 风险；新增独立 UIForm 会同时增加表、资源注册和交接成本，本案不采用。

### 实际 owner 树与创建退出

```text
Fiber -> Scene(ProductRoot)
  Components: Timer / ProcessInnerSender / ProductRuntime
  Components: ProductEntryPresentation / SoloSavePresentation (entry phases)
  Component: SoloSaveRepository + Ordered MailBox (on demand)
  Component: UIComponent -> UIGridKitchen (entry owner, one active owner)
  Child: Scene(ProductHost) [Solo / Host, after explicit start or room creation]
    Component: HostSession + Ordered MailBox
      Child: HostPeer + Ordered MailBox + LocalDelivery / RemoteDelivery
    Child: Match -> Children: Participant
      Child: RestaurantRuntime
        Component: GridKitchen -> Stations / Items / Processes
        Child: Level -> Actors / Customers / Orders / FrontWork / CompanionRole
  Child: Scene(ProductClient)
    Component: ClientSession + Ordered MailBox -> GridClient / ProductRoomClient
    Components: GridPresentation / UIComponent / GFEntityComponent
      owned: UIGridKitchen / GridKitchenBoard
    Remote only: NetComponent -> Session -> ProductClientBinding
```

入口阶段不创建下面的玩法树；合作创建房间可以先有 Match、没有 RestaurantRuntime。新开确认后建餐厅及客户端独立投影，再由 GridPresentation 接 Board／HUD；入口与地图沿原 UI1101 单实例交接，不同时强开两份。`GridReady` 仍检查本人投影、Board/HUD 可用及无展示失败，不以页面文字代替。

关闭先使原页面 epoch／render 选择失效，再走原 UIComponent／GF owner 释放。await 后检查 EntityRef、页面轮次及当前 Client／房间身份。返回模式沿 Loader 原整体退出；单成员退出不 Stop 共用 sender，整个运行退出才停止其根发送器和所属树。

### 发送、接收与事实写入

纯页面切换不发送业务写命令。继续记录沿根 `ProcessInnerSender.Call(repository.GetActorId())` 到 ProductRoot 的 `SoloSaveBrowseHandler`／`SoloSaveSelectHandler`，响应值化后按页面身份应用，原 owned request 和 handler 各自回收所属消息；UI 不保存池化响应、不新增等待表。文件读取失败保持原错误，不返回空集。

本地合作操作沿 Client 的 LocalMessage，取得当前 HostPeer ActorId，经根 sender／队列到 SceneType.ProductHost 的 `ProductRoomPeerHandler`；Peer 填可信身份，再 Call 当前 HostSession ActorId，由 `ProductRoomSubmitHandler` 统一裁决。远端通过实际 Session／KCP 与绑定到相同 Peer，再进同一提交端。房间提案、Ready 与 Start 规则均不改；Host 提交后发布房间快照，`ProductRoomSnapshotHandler` 在 ProductClient 安装独立投影，原观察事件只刷新显示。

新开确认沿原 runtime 初始化；恢复沿原已校验记录入口。原事件 `SoloSaveSelectionRequested` 只在实际 ProductRoot 消费范围导航，Event 不充当请求答复或网络传递，不新增 SceneType.All 订阅。Invoke 沿原路由职责，本案不新增。

### 与相邻 change 的交接

本案提供首页次级入口位置。测试案未实现时不挂无效按钮，测试案先实现则先挂原首页，后续迁移位置。就绪案负责真实装载阶段说明，本案只保留其可显示区域及当前合法取消／返回入口；不强制其先完成，也不复制阶段状态机。共享文件按案顺序整合。

## Risks / Trade-offs

- [导航重排漏入口] → 清点现有章节、记录、试玩、合作、领取和授权路径，使用最小相关 UTF 逐项断言可达。
- [取消／旧回调重复开局] → 复用原 epoch／EntityRef／忙碌保护，覆盖取消、关闭重开及一次点击一次请求。
- [布局破坏经营 HUD] → 限定 Entry 布局及其消费者；从普通 Launcher 进入准备并执行一次原操作，验证地图恢复。
- [记录显示误报] → 将读取中、失败与空集分开；数量只读，不扩充存档架构。投入判断见 [execution-notes.md](execution-notes.md)。

## Migration Plan

实施已在原 Mono 布局中调整 Prefab 实例的内部区域，保留序列化绑定，无需改 Prefab 结构或重生成 CodeBind。章节分栏同时检查逻辑宽度和宽高比例，避免竖屏参考分辨率较大时误判；首页主按钮按可用高度分配，空导航条收起。无持久数据迁移；撤回本案页面／布局改动即可回退，不能回写或删除用户记录。相关编译、UTF 和普通 Launcher 结果见 execution-notes；构建、提交、主规格同步和归档不属本轮实施。
