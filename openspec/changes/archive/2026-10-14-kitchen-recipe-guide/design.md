# Design
## 2026-10-14 用户试玩豁免

用户对“真实新玩家操作记录”的安排答复“忽略这项”。本轮跳过新玩家理解、求助点与再次经营意愿观察，标记 Skipped（用户豁免）；实际效果仍未验证。UTF 和普通 Launcher 操作验收继续，不把脚本当人类理解证据。试玩调校仅针对可复现的实际操作问题；没有依据时不改参数。此决策覆盖下文原规划中要求无玩家仍待完成的条款，历史记录保留。

## Context

GridDishConfig 和当前两菜配置已经给出可执行配方，表现缺少连接原料、设备与动作的解释。前置 UI 案提供固定详情区域，本案仅使用该结构。 动机见 [proposal.md](proposal.md)。当前定义／提示／边界已实施并运行，真实玩家理解按用户要求跳过，见 [execution-notes.md](execution-notes.md)。

## Goals / Non-Goals

目标模式：Solo 引导；已开放联机只读菜谱。不做通用任务系统、账户新手标志、全菜目录、自动代操作；不重构 UI 结构，使用前置保留的内容区域。

依赖：[restaurant-ui-information-hierarchy](../2026-10-14-restaurant-ui-information-hierarchy/proposal.md)。层次只表达从局部到跨模块的深度；仅上述实际合同构成依赖。

## Decisions

### 1. 菜谱来自实际定义

名称、容器、原料、耗时与设备要求从当前有效配方/物品定义读取；必要步骤排序与说明作为有限描述数据，不建通用任务引擎。表字段和本地化由源配置生成，非法引用在 Check/配置验证中失败。

现有 DishStageConfig 图已包含两菜说明所需字段，直接按真实依赖图生成说明；不另建重复步骤表或本地化字段。固定动词沿当前中文 UI 约定，名称与参数来自现有定义，正常 Check／导出验证链路。

### 2. 引导随事实推进

Solo 一次运行内可开启、跳过和重新打开首单引导；完成的备料从当前投影识别，不强迫重做。按原料→加工→盛装→询问/交付提示所缺条件，拒绝不推进，旧 Level 或目标消失立即重算。暂停时只说明现状。

### 3. 只读解释与真实控制分离

提示不自动选择目标、发命令、改物品或暂停世界；玩家仍用现有操作。联机提供相同菜谱阅读，首单步骤不驱动其他人或授予 Host 控制。无需账户标志或保存引导进度。

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

提示可能错误推断工作完成或成为第二业务权威。用纯值判断和真实 UI 指令断言，加一次无口头操作说明的首单试玩；外部试玩未进行就保留未完成。 → 具体覆盖与接受的 NotRun 见 [execution-notes.md](execution-notes.md)。不引入额外测试平台、全面压测或理论完备性门槛。

## Migration Plan

实施前以最新主规格、依赖 change 和源码核对差异；保持本案可独立审阅。涉及配置/协议/CodeBind 时修改源并正常生成，不手改生成物。需要格式适配的改动先验证旧有效输入与失败保留，再开放入口。若失败则保留原用户文件和原失败结果，只撤回本案未发布改动，不重置其他人的工作；不自动提交、归档或同步第二工作区。

## Sources

- [Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridDishConfig.cs)
- [Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationPanels.cs)
- [Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/GridPresentation.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/GridPresentation.cs)
- [Design/Excel/ET/Datas/Product](../../../../Design/Excel/ET/Datas/Product)
- [openspec/specs/dish-recipes/spec.md](../../../specs/dish-recipes/spec.md)
