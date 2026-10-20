# Design

## Context

动机和范围见 [proposal.md](proposal.md)。`RestaurantChapterConfig` 的第一章是 Id=1「家常开张」，Solo ChapterId=11、首关=1101、菜单变体=Growth；该首关实际只点汤品1。独立首饮试玩201不是正式第一章。`SoloSavePresentationSystem` 当前把各变体与章节平铺；`GridPresentationRecipeSystem` 已有跳过/重看和 `GridRecipeGuide.CurrentNeed`，不从零建立教学系统。

## Goals / Non-Goals

**Goals:** 玩家明确进入正式章节，并从现有事实得到一个可执行的下一步。

**Non-Goals:** 不引入强制教程、教学存档、自动移动/制作/暂停、新配方或新开店门槛；不把合作客户端变成控制方。

## Decisions

### 入口内容与已有预览保持单一来源

首页突出单人入口，合作/进度/设置仍可访问。Solo 页面主区域展示“开始第一章”和“选择其他章节”，有合法继续记录时同时显示“继续记录”入口；不擅自选择多份存档中的一份。菜单试玩进入次级页，原所有变体、内存试玩和存储方式保留。推荐操作仍调用 `PreviewSoloStart`，不绕过确认；取消保持文件、运行和原选择。

章节名、菜单和首关从表读取，明确推荐内容章 Id=1，不按排序意外选中测试配置。存储不支持时使用现有内存方式并明确说明，不伪造可保存状态。合作已分章节/试玩，不重复实现房间流程，只接入布局案的最终区域。

### 首单提示是当前事实的解释

第一章1101的新开或合法恢复进入准备时显示汤品1的分步提示；自动展示只针对该首关，不在合作自动开启。其他场景保留原手动菜谱/重看入口。跳过仅存本轮 Presentation 的显示选择，HUD 重开沿同一 owner 保持，退出该轮不记录永久教学状态。

从原配方依赖和 `CurrentNeed` 抽出可供文字与定位共用的只读结果：步骤种类、当前缺口、可用目标实例/宿主、说明。有限分类使用 enum。优先已有可用成品/半成品，再指向缺失工序；加工锁定、他人手持、容器内容与批次份数沿原语义。没有材料时指向实际补货/清理说明，不显示不存在的高亮。

推荐步骤序列按事实缩短：查看本关目标→完成必要加成→可选备料→显式开店→真实接待/询单→制作/盛装→交付→自然收尾和原继续入口。备料建议不阻止开店，未发生的订单不被教学虚构。第一张有效汤单明确绑定为阅读上下文；过期后取消目标并允许选择新有效单，不暗中执行。首单真实 Served 后提示已完成，可关闭/重看；阅读当前实际菜谱始终可用。

“查看位置”只高亮当前有效物件/工位或其宿主并必要时移动镜头；玩家仍自行点击原动作。主动作内容交由情境动作案，首单提示不维护另一套命令候选。

### 实际 owner 树

```text
ProductRoot Fiber / SceneType.ProductRoot
  Components: ProcessInnerSender, ProductRuntimeComponent
  Components: ProductEntryPresentation / SoloSavePresentation, UIComponent
    UIComponent -> Component UIGridKitchen（入口阶段）
  Child Scene ProductHost / SceneType.ProductHost（Solo 权威）
    Component HostSessionComponent + Ordered MailBox
      Child HostPeer + Ordered MailBox -> Component LocalDelivery
    Child Match -> Child RestaurantRuntime
      Component GridKitchenComponent（实物、加工）
      Child Level（阶段、顾客、订单）
  Child Scene ProductClient / SceneType.ProductClient
    Component ClientSessionComponent + Ordered MailBox
      Component GridClientComponent（独立投影）
    Component LocalMessageComponent（本地 peer 绑定）
    Component GridPresentationComponent（引导可见/选中目标）
    Component UIComponent -> Component UIGridKitchen
    Component GFEntityComponent -> Component GridKitchenBoard
```

入口确认后原 runtime 创建 Host/Client，确认身份和安装投影后打开厨房 HUD；入口与厨房按原 owner 交接单例1101。引导不持有 Host 实体。引导选择与高亮随当前运行、binding、Level、HUD epoch 失效；关闭只清显示，退出由原 runtime 拆树，不停止其他成员共用发送器。

### 发送、接收与等待收尾

引导查看/跳过不发送业务消息。开局确认沿 `PreviewSoloStart`→`InitializeSavedProduct/InitializeProduct` 原入口；保存/读取仍归原 repository，不新增存储协议。实际动作复用 GridClient 原请求：本地 `LocalMessageComponent.Peer.GetActorId()`→Root `ProcessInnerSender.Call`→ProductHost 的 HostPeer Ordered MailBox/`GridPeerHandler`。它复制独立 `GridSubmitRequest`，向 `peer.Authority.GetActorId()` 再次本地Call，经HostSession Ordered MailBox的 `GridSubmitHandler` 统一验证并提交 `SubmitGridIntent`。内部应答由GridPeerHandler读取并Dispose，外部应答经原request handler回送等待方；`CallProductValueAsync` 复制需要值、结束原peer entry并处理await后上下文。

Host 提交后经 `ProductDelivery`：本地使用 `LocalDelivery.Client.GetActorId()` 入 ProductClient 的 ClientSession MailBox；远端由 `RemoteDelivery.Session` 序列化，经 `ProductClientBinding` 进入同一接收 handler。`ApplyGridSnapshot` 与 outcome handler 安装独立值后引导刷新。当地 DTO 由 handler finally Dispose，远端发送 DTO 在序列化后 Dispose；UI 不保留消息。Accepted 不前进步骤，只有当前投影事实才更新。

Event 仅用于原范围的显示通知，Invoke 保持原初始化用途；不新增 All 订阅、不把 Event 当业务请求。异步读取后检查 EntityRef、运行/绑定/关及页面 epoch，旧结果只结束原等待。

## Risks / Trade-offs

- [推荐错误破坏信任] → 先覆盖汤链和已备料/缺料/失效单；其他菜单沿原菜谱能力，不声称全部菜单拥有定制教程。
- [入口整理隐藏功能] → 对现有章节、继续/完成记录、各试玩逐项检查可到达，复用旧档验证。
- [自动通过不等于新手会玩] → 工程验收包括真实首单，另记录一次未接触项目者的观察；未观察明确保留体验未验证，不冒充技术失败。

## Migration Plan

先接入布局案 API 和入口内容，随后扩展既有只读引导并验证首单。无需表、协议或存档迁移；撤回显示调整可恢复旧入口。应用时更新当前进度与对应说明，运行映射见 [execution-notes.md](execution-notes.md)。
