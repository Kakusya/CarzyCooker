# Design

## Context

使用当前 UIGridKitchen、GridKitchenBoard 和 GridPresentationPanels。既有受管 HUD 已覆盖单击、拖动、池复用与旧回调；本案保留其命令合同，调整实际 Prefab 的层次和可读性。 动机见 [proposal.md](proposal.md)。本案实施与 Editor 验收已完成，结果见 [execution-notes.md](execution-notes.md)。

## Goals / Non-Goals

目标模式：Solo 与已开放 Host/Client。不改配方、菜单、经济、命令权限或领域时钟；不新增私有 UI 池、业务状态或结构 runner；首版不引入音频资产包。

依赖：无玩法硬前置。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 按当前任务组织信息

顶区固定阶段/时间/得分与暂停，中区保留 Board，底区固定手持物、选中目标和主要动作；订单与详情按需展开。工位名、内容、加工进度分层，不把所有标签同时铺在格子上。详情开关归 GridPresentation。

### 2. 主要操作只做展示选择

从已安装投影与既有权限挑选当前主要动作，其余合法动作保留入口；既有点击即执行目标规则保持。等待到位、加工中、成功、拒绝分别显示，未知或旧投影时禁用，不以按钮状态代替权威验证。

### 3. 按项目 UI 流程改源

保留 UI/Entity 资源 ID 和 owner；改 Prefab 与 Mono 源后重新生成 CodeBind，核对 Canvas、射线与引用消费者。沿用既有池与订阅生命周期，不引入私有缓存/第二业务状态。使用现有文字、颜色和资源反馈，声音资产留在后续有素材的明确需求。

### 状态归属与交接路径

```text
Product Fiber / RootScene (SceneType.ProductRoot)
├─ ChildScene ProductHost (SceneType.ProductHost)
│  └─ Child Match → Child RestaurantRuntime → 持续厨房 / 当前 Level（唯一业务事实）
└─ ChildScene ProductClient (SceneType.ProductClient)
   ├─ Component ClientSession + Ordered MailBox → Component GridClient（独立只读值）
   ├─ Component GridPresentation（选择与提示等视图状态）
   ├─ Component UIComponent → UIGridKitchen（受管 UI）
   └─ Component GFEntityComponent → GridKitchenBoard（受管视图）
```
先有绑定和匹配的完整投影，再开启 Board/HUD 输入；关闭先解绑回调、取消视图等待，再由原 owner 归还 UI/Entity。局部关闭不释放 ClientSession 或公共发送器。

本地 UI 从当前 LocalMessage.Peer 取得本地 HostPeer ActorId，并由 ClientSession 提供本轮命令上下文，经 ProductRoot 的 ProcessInnerSender/本地队列投递；HostPeer 的 ProductHost handler 校验真实发送者与绑定，再发往同 Scene 的 HostSession Ordered MailBox。远端经 Session 和 ProductWire 显式白名单复制为同一内部请求，再进入该 Peer；Host 不向自己发 UDP。

入口快速答复接受／拒绝，服务步由持有相关状态的 owner 完成验证后提交；接收、启动、完成分别返回，状态先提交再发布投影或观察 Event。通知经 LocalDelivery／RemoteDelivery 回到 ProductClient handler，按运行、Level、绑定代次安装独立值；网络 RPC 号不代替业务编号。框架 Invoke 仅承担既有启动/生命周期，Event 不充当请求答复，不增加 SceneType.All 写状态消费者。

请求池对象只在本次处理内使用，跨 await／队列复制值；每一跳答复对象由该跳调用者收尾，领域快照不引用消息池。视图关闭取消业务等待但原调用继续按答复／有限超时收尾，await 后核对 owner InstanceId 和绑定。成员退出只撤自身接入、取消旧动作并执行既有落物；整轮停止才结束公共发送器、剩余等待和子树。

## Risks / Trade-offs

结构变化可能留下旧绑定、遮挡输入或使池化按钮重复订阅。真实 Prefab／CodeBind 检查、UI 小集与普通入口点击拖动足够；真机尺寸与触控在最后补验。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab](../../../../Unity/Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab)
- [Unity/Assets/Res/Entity/Product/GridKitchenBoard.prefab](../../../../Unity/Assets/Res/Entity/Product/GridKitchenBoard.prefab)
- [Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoUIGridKitchen.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoUIGridKitchen.cs)
- [Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)
- [references/dynamic-ui-sop.md](../../../../references/dynamic-ui-sop.md)
- [openspec/specs/restaurant-ui/spec.md](../../../specs/restaurant-ui/spec.md)
