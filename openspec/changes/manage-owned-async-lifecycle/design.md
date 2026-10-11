# Design

## Context

动机见 [proposal.md](proposal.md)。当前 `ResourceContainer` 只在 Create 时更新版本，成功回调以外的出口没有同等检查；资源 await 在底层尚未完成时先继续轮询，取消与池记录回收可能依赖后续帧和 GetResult。`ADynamicUIWidgetLoader` 和 `UGFComponent.LoadAssetAsync` 另有直接加载路径。已有 CodeLoader 的 World 身份检查和 ET EntityRef 仍有价值，不能用一个 token 替掉它们。

Runtime GameEntry/Base 拆卸会关闭 GF 模块，资源队列和代理 Reset 会清掉业务回调；默认资源 helper 将进行中的 AssetBundle 请求引用置空，不等于已经证明原生操作停止。当前 Game 稳定程序集能引用 UniTask，Game.ET.Loader 同时引用 Game 和 ET.Core；公共契约放在这条现有依赖方向上。

本设计、[test-conditions.md](test-conditions.md) 和 [execution-notes.md](execution-notes.md) 是拟实施契约，不表示这些能力已经存在。

## Goals / Non-Goals

**Goals:**

- 调用者只需选择正确 owner、使用统一托管入口和受控写回；登记、取消、错误观察及加载收尾由公共机制承担。
- Close/Hide/Dispose 和运行退出立即结束未完成的业务等待，旧轮次不可写入新轮次。
- 将业务任务终态、底层请求退役和已取得资源的引用所有权分别记录，避免为“立刻退出”虚报原生完成。

**Non-Goals:**

- 强行中止任意 C# 函数、同步死循环、外部包内部 async，或保护绕过契约的直接字段写入。
- 新线程调度器、ET RPC 表、资源池/缓存、世界状态权威或网络消息类型；不改变取消之前已经提交的业务事实。
- 全仓异步替换、资源更新流程重写、Scene 加载事务、Player/AOT/物理 LAN 验收。现有 UI/Entity 显示等待器保持各自显示/隐藏契约，本轮重点是 owner 所属工作和资源加载。

## Decisions

### 1. owner 接口与操作内处理组合使用

在 Game 现有程序集内引入小型 `IAsyncOwner`、每轮 `AsyncScope` 和捕获本轮身份的 `AsyncContext`。业务入口采用 `owner.RunAsync(context => ...)`；任务在启动委托之前登记，统一观察正常、取消及错误。context 提供受管资源加载、延时、请求适配和同步 `Apply(Action)`。普通任务通过同一入口登记，不只保护图片加载。

每个登记记录至多提交一次终态。关闭直接将尚未完成的对外任务设为取消并撤销业务登记，不等待委托返回或原生回调；已经完成的结果保持原事实。取消信号促使协作式内部工作停止。若内部工作忽略取消，公共层仍观察它的晚到结束和必要清理，但不允许它通过旧 context 写状态；不把“对外取消”说成任意函数已停止执行。

只给 owner 加一个接口不能迫使每个异步函数自行收尾；只在加载函数里处理则看不到对象复用和普通工作。因此选择两者组合。迁移后的业务入口缺少有效 owner 时明确拒绝，不能隐式绑定永久存活的全局 owner。原始底座 API 保留给具有明确所有权的框架内部使用。

### 2. 轮次、ET 实例和执行上下文分别保留

每次真实 Open/Show 创建新 scope；Close/Hide 先失效并分离旧 scope，再完成取消，Recycle/Destroy 做幂等兜底。Visible、遮挡或暂停表现不自动代表退出。旧 context 捕获运行身份和 owner 轮次；ET 适配再捕获 EntityRef/InstanceId，不能仅比较业务 Id、对象引用或 IsDisposed。

`Apply` 在 owner 原有执行上下文同步检查身份并执行短写回，不允许其中再次 await；await 后再次使用 Apply 或受管操作。Unity 视图保持 Unity 主线程，ET owner 保持其 Fiber/上下文；公共层不把所有续体挪到 Task.Run。关闭在原上下文提交终态，不用排队到未来帧才承认取消。支持的是这些现有上下文中的工作，不扩展任意跨线程写状态。

资源容器还有独立的加载轮次：活着的 owner 调用 UnloadAllAssets 时，只取消这批资源工作，不误停它的延时或请求。Clear 也使旧加载轮次失效，但仍不是正常资源卸载的替代品。关闭时先捕获并移走旧资源列表/取消记录，再通知取消或卸载，避免重入创建的新轮次被旧收尾清掉。

### 3. 清理独立于调用者读取结果

受管操作的完成/取消通知自行移除登记并处理基础引用，不把基础清理只放在调用者 finally 或池化 UniTask source 的 GetResult。内部任务由一个明确观察者消费，避免同一 UniTask 被反复 await；对外等待的记录在其安全有效期内保留，不能提前池复用使尚未消费的旧句柄指向新任务。

资源回调记录捕获原加载轮次、原资源 backend 和本次请求的所有权，不再闭包访问复用后的 `this` 或当下静态 GameEntry。成功、失败、进度、依赖回调均先检查身份。旧成功只释放本请求取得的引用；旧失败/进度不再通知旧业务。对旧成功先判归属再检查业务类型，避免给已退出对象投递类型错误。有效成功的引用只登记一次，随后由原容器正常卸载；独立于调用者是否 await 或读取结果。

受管 fire-and-forget 仍由入口观察错误；预期取消与真实错误分开处理。禁止用 `.Forget()` 丢掉基础收尾责任。原接口显式转移资源所有权的情况要在适配器里说明，不能同时由容器和调用者各卸载一次。

### 4. 底层加载采用独立退役责任

普通 owner 关闭不停止整个资源管理器。已经发出的请求可以继续运行；原记录进入退役状态，晚到成功通过原 backend 归还本请求引用，且不碰其他 owner 的共享引用。业务记录取消和原生记录退役是两件事，后者不能因为业务等待结束就池复用。

全运行退出时，GF 资源代理不再承诺继续投递业务回调。必要的最小修改放在现有 agent/helper 的 Reset/Shutdown 交接点：保留进行中的原生请求和原资源释放责任，由原操作的完成通知处理退役，不再使用已拆掉的 agent、对象池或新一轮 GameEntry。AssetBundle 创建成功但未交给管理器时由该退役记录释放；已经交给管理器的 bundle/资产由原管理器释放。正在 LoadAsset 的请求不得再独立全量卸载同一共享 bundle；交接需使资源依赖和最后释放归唯一所有者。

若原操作已完成就同步退役，否则保留原请求直到原生完成；UnityWebRequest 等可明确结束的操作使用自己的停止/Dispose。不能把清空 handle 或业务 token 取消视为原生完成。运行仍继续时允许原生尾部稍后完成；完整进程结束后不要求 C# 回调再次运行。业务退出始终不等待这一尾部。

Unity 的完成事件可能延后到后续帧，因此它只能承担原生尾部责任，不能作为立即结束业务等待的前提。[Unity AsyncOperation.completed](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AsyncOperation-completed.html)；共享 AssetBundle 的释放需尊重原资源引用关系。[Unity AssetBundle.Unload](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AssetBundle.Unload.html)

### 5. 实际 owner 树与退出连接点

保留既有 ET 树，新增组件只持有异步生命周期，不持有第二份产品状态。下图中的 AsyncOwnerComponent 为拟新增的按需组件；树中其他产品节点已存在，现有 legacy 场景按同样适配而不改 SceneType。

```text
Unity Runtime Base / 本轮 GF 运行根
├─ 本轮运行登记：引用 scopes，仅用于退出广播，无业务状态
├─ AExUIForm / UIWidget / AExEntity（GF 所有者，各有本轮 scope/容器）
└─ ET.Init 捕获的 World
   └─ Fiber.Root：SceneType.ProductRoot
      ├─ Component：ProcessInnerSender、Timer、ProductRuntimeComponent
      ├─ Child Scene：SceneType.ProductHost
      │  ├─ Child：Match → Child Participant（既有状态）
      │  ├─ Component：HostSessionComponent → Child HostPeer
      │  │  └─ Component：Ordered MailBox、LocalDelivery 或 RemoteDelivery
      │  └─ Component：NetComponent → Child Session（远端连接）
      ├─ Child Scene：SceneType.ProductClient
      │  ├─ Component：ClientSessionComponent、Ordered MailBox
      │  └─ Component：LocalMessageComponent 或 NetComponent → Child Session
      └─ 现有适用 Scene/业务 Entity
         ├─ Component：UGFComponent 或 GFEntityComponent / UIComponent
         └─ Component：AsyncOwnerComponent → Game.AsyncScope（按需）
```

组件通过 ET Component/Child API 创建、移除，原 EntityRef/InstanceId 控制其有效性。Entity Dispose 在子组件 IDestroy 之前已可能使实例无效，故操作入口与 Apply 均检查原 owner 身份；IDestroy 完成取消兜底。GF Hide 只结束视图 scope，不能顺带销毁仍存活的 ET 逻辑 owner。

创建顺序是捕获本轮运行根 → 创建 owner scope/ET 适配 → 登记任务 → 启动操作。单对象退出是失效本轮身份 → 分离旧登记 → 取消业务等待 → 原 owner 资源/视图清理 → 回收。

整体退出在 Runtime GameEntry.Shutdown 及 Base.OnDestroy 兜底的 **GF 模块拆卸之前**发出本轮停止通知；底层 Runtime 不反向依赖 Game，只暴露必要的中性连接点。Game 的订阅捕获原 Base/运行身份，停止时先标记该运行所有 scopes 失效并禁止新增，再分离登记、逐个提交取消，最后走既有资源、World/Fiber 拆卸。ET.Init.Shutdown 也先关闭其所属 scopes 再销毁 World。双入口幂等，旧运行的迟到销毁不能停止新运行。

### 6. 请求等待复用原传输与等待表

Session 请求适配使用现有 `Session.Call(request, CancellationToken)`，在 scope 内连接取消和已有超时需求；不另建 RpcId → 等待者字典。已有 ERR_Cancel 转为受管取消，不能作为成功业务响应。适配器在原响应安全期内取独立值或明确移交所有权；无人读取或 scope 退出后的响应由适配层正确 Dispose。取消调用者等待不撤销接收端已经提交的动作。

本地 `ProcessInnerSender.Call` 目前没有逐请求 token。本轮适配器先结束所属调用者的等待，再由单一内部观察者等待原发送器的答复/既有有界超时并消费消息；它没有第二份 RPC 映射，也不以单 owner 退出调用 sender.Stop。整个运行退出仍使用已有 sender.Stop 收尾原物理等待。不能把调用者取消虚报成原发送器等待表已立即清空。

用已有 Probe 说明完整路径，不新增聊天或玩法：

```text
本地：ClientSessionComponent.CallProduct
  → LocalMessageComponent 的原 HostPeer EntityRef → peer.GetActorId()
  → ProductRoot.ProcessInnerSender.Call → MessageQueue → HostPeer Ordered MailBox
  → SceneType.ProductHost / ProductPeerProbeHandler
  → 原 HostSessionComponent ActorId / Ordered MailBox / ProductControl Handler
  → 既有 authority 裁决、状态提交 → Reply 原等待者 → 值响应/客户端 Apply

远端：Session.Call → 原 UDP-KCP Session
  → NetComponentSystem.OnRead / SceneType.ProductHost / ProductWire
  → 原 ProductPeerBinding.HostPeer → 同一本地 Call/MailBox/authority
  → 原 wire RpcId 答复 → Session 等待表 → 原调用者 scope / 值响应 / Apply
```

响应及池化消息由现有 Handler/发送器/桥的既有责任收尾；新增适配只管理自己拿到的等待与返回对象，不抢收件端所有权。名单通知仍走既有本地 Send 或 Session 传输后进入 ProductClient MailBox/Handler。ET Event/Invoke 负责已有进程内分派，SceneType 保持实际匹配；不新增 All 消费者，不把 Event 当请求答复或网络通知。

### 7. 迁移边界用具体入口限定

- `ResourceContainer`、其 AExUIForm/UIWidget/AExEntity 使用者，以及 `ADynamicUIWidgetLoader`：绑定父界面本轮 owner，加载中父级关闭也取消；Mono 销毁再兜底。
- `UGFComponent.LoadAssetAsync`：默认明确绑定其原 Scene owner；较短命 Entity/视图调用时传实际 owner/context，不能全都绑到 Scene 存活期。必要业务回写路径包括当前 Unit/LSUnit 的 view 创建调用。
- CodeLoader、ConfigReader、Recast、UGFHelper、Tables/HybridCLR 等已定位原始资源入口：仅迁移本轮运行所属加载，保留原 World 检查与明确资源转移。启动中途退出纳入取消；不顺手修复独立热更/AOT 问题。
- 普通延时和一个既有 Session 请求消费者用于证明统一入口。其他未迁移业务、第三方和工具 async 在文档中明确边界，不声称全仓已统一安全。

不生成分析平台或扫描器。实施时核对上述文件的直接消费者即可；若发现不可保留既有契约的接口破坏，再据具体调用方修订规划。

### 8. 先测试条件，再实现公共机制

按 [test-conditions.md](test-conditions.md) 的 T1–T9 编写受控测试，先锁定未完成加载、关闭/复用、无人消费、共享 owner 和无后续帧退出的观察结果。测试替身只控制底层时序；被测取消、登记、释放和 Apply 不能由替身代做。随后实现核心、资源和 owner 适配，用一个真实 Unity 资源路径补充原生引用证据。

测试条件不等于测试通过。复用既有本地发送器/产品会话结果的适用部分；只补这次新适配和实际风险，不重跑双 Editor、产品构建或全部矩阵。真实 AssetBundle 证据缺失时写 NotRun，不能用模拟计数冒充原生验证。

## Risks / Trade-offs

- [业务绕开入口或直接写字段] → 迁移明确路径、提供统一 RunAsync/Apply 用法并列出边界；不声称接口能强制约束所有 C# 代码。
- [关闭触发同步续体重入] → 先失效并分离旧批次，再取消和清理；旧收尾只操作捕获记录，不清当前集合。
- [池化源过早复用或无人消费泄漏] → 对外记录不依赖 GetResult 才收尾，底层记录保留到原操作退役；用 T4/T5 检查。
- [原生共享引用重复卸载] → 唯一交接责任、引用边界检查和一条真实 Unity 加载退出验证；业务任务清零不能替代原生资源证据。
- [退出期间没有下一帧] → 在 GF/ET 拆卸前同步提交取消，原生尾部独立；不 await 全部原生完成才允许退出。

## Migration Plan

先落测试契约及必要编译骨架，再实现公共 scope、资源适配、普通工作/请求和 owner 连接，最后接早期退出及原生交接。迁移后的业务方法内部使用统一入口，原始 framework API 由明确 owner 的基础代码保留。同步相关 Book/引用、KnownIssues 和 delta specs；不在提案阶段修改主规格。

第一项路由/导表 change 可独立实施，本 change 不依赖它的代码；两项各自修改 entity 规格的不同 Requirement。此处不引入表/协议/存档迁移，也无自动回滚或重置用户文件步骤。若真实原生验收发现交接责任无法成立，保持失败/NotRun 事实，只针对具体缺口修订此 change，不能降低“立即结束业务等待”的验收标准。
