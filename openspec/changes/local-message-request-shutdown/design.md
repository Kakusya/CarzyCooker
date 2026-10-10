# Design

## Context

目标见 [proposal.md](proposal.md)，验收见 [network 增量规格](specs/network/spec.md)。用浅显的话说：发送器已有一份“等答复的名单”，本项在关门时把名单上的人全部通知到，并防止旧回信打扰下一轮。

当前源码事实：

- ProcessInnerSenderSystem.Destroy 只按 Fiber.Id 删除队列。Call 的等待者保存在 requestCallback，未被 Destroy 结束或清空。
- 正常响应先从名单移除 RpcId，再完成等待。Call 的超时为 40 秒，异步任务持有 self 和 fiber；退出后不能依赖该计时继续运转。
- RpcId 当前由每个发送器递增。只清空旧字典不足以保证同一 Fiber 标识重建后不会重复编号。
- MessageSenderStruct 已支持 SetResult／SetException，无需再建回调表。MessageHelper.CreateResponse 依赖 OpcodeType 和 ObjectPool，而 World.Dispose 会销毁这些单例；兜底不能默认它们仍可用。
- Entity.Dispose 先使 InstanceId 失效，再销毁下级，最后执行 IDestroy。因此父对象的 Destroy 不能作为唯一正常停止入口，也不能假定兄弟计时器或根资源仍存活。
- 代码属于 ET.Core，也由 DotNet.Core 链接。当前源码扫描没有发现可直接复用的项目 UTF 测试 asmdef；Tools/AutoTesting 是既有测试编排入口，不包含本次测试夹具。

这些是静态发现，不是已运行的故障复现。现有 network 主规格描述的是 Session 收尾，没有声明 ProcessInnerSender 已具备同样保证，本项以 ADDED Requirements 补充。

## Goals / Non-Goals

**Goals:** 在原发送器实现可重复的停止、直接销毁兜底、未完成请求退出终态及旧生命周期隔离；保持既有运行中结果。

**Non-Goals:** 不建设本地通道／网络通道的产品适配，不创建 SceneType、Host 成员或业务协议，不实现重连、撤销远端操作、跨域恢复或新的异步框架。也不把单个 Peer 断开解释为全 Fiber 请求停止。

## Decisions

### ET owner 树与消息路径

本项修既有发送器，不创建产品 Scene；测试必须显示其所属树及真实收发路径：

```text
Fiber.Root（沿用调用方 SceneType）
  Component: TimerComponent / CoroutineLockComponent
  Component: ProcessInnerSender
    本 Fiber 请求等待名单和消息批次
  Child 或下级 Scene: 接收 Actor
    Component: MailBoxComponent
```

发送方从实际接收 Actor 的 GetActorId 取得含 InstanceId 的目标，Call／Send 经 MessageQueue 入队；目标 Fiber 的 ProcessInnerSender.Update 找到 MailBox，再由 MessageDispatcher 的适用 Handler 接收。答复按 FromAddress 经队列回到原发送器，移除匹配等待者后完成 await。收尾直接结束名单中的等待，不向已撤下的队列再发取消消息；不为纯框架测试构造 UDP 回环或产品身份绑定。

ProductRoot／HostSession／ClientSession 的实际业务树、消息对象与单成员退出交接见独立 [产品会话设计](../product-session-bootstrap/design.md)。本项提供整份 sender 的 Stop，不提供单成员停止整个 sender 的借口。

### 1. 在原 owner 增加 Stop，Destroy 共用

在原 ProcessInnerSender 上持有必要停止状态，由 System 提供幂等 Stop。状态只表示已停止，不复制 Entity 的实例身份。Awake 初始化本实例状态和等待名单；同一 Fiber 保持一份 sender，不改变队列布局。

Stop 按下列顺序处理：

1. 标记停止，之后的新 Call／Send 明确抛 ObjectDisposedException，且不注册或发出消息。
2. 取出当前未完成等待的稳定快照，并清空原 requestCallback。
3. 撤下本实例消息入口，防止继续收件；这属于“停止接单”。队列撤下要早于完成通知，避免通知中的调用者重建同 Fiber 队列后又被旧收尾删除。
4. 直接完成快照中的等待者，再清理剩余本地状态。不会把退出结果投递进队列，也不等待另一端确认。

Destroy 调用相同收尾；已经 Stop 后再 Destroy 不重复删队列或通知。正常运行方应在释放其本轮树前显式 Stop；直接 Dispose 则由 sender 自身 IDestroy 兜底。本项提供能力及验证，不修改尚不存在的产品退出编排。

队列清理由初始化时捕获的所属 MessageQueue 实例完成，不在 Destroy 中重新取得或创建 World／全局队列。原队列已退出时仍直接结清本地等待；World 重建后，旧发送器不得按相同 Fiber.Id 删除新 World 的队列。

完成通知可能使 await 后面的代码立即继续，产生重入。因此先清空名单与撤下旧入口，再通知；Update 处理批次时核对捕获的实例身份及停止状态，失效就停止该批次，不能在完成通知后继续使用已销毁／复用的 self，也不能使当前迭代因清理缓冲区而抛异常。

### 2. 退出结果直接完成，保持消费模式

已提交请求采用既有 ERR_Cancel 表示本地等待终止，Message 明确说明发送器已停止：

| 调用方式 | 退出结果 |
| --- | --- |
| needException=true | SetException，返回携带 ERR_Cancel 的 RpcException |
| needException=false | SetResult，返回原请求对应的 IResponse，Error=ERR_Cancel，RpcId 保留该请求关联 |

这是本项对新增退出情形的设计约定。停止之后再发起的新调用属于无效 owner 使用，统一拒绝，不适用已提交请求的结果模式。

为避免销毁期间找不到 OpcodeType／ObjectPool，提交有效请求时保存已核对的响应类型；必要时在 MessageSenderStruct 扩展该元数据。收尾利用捕获类型直接创建非池化失败响应，或采用同等不依赖已销毁单例的方式；不缓存第二份协议注册表、不手改生成物。响应映射缺失在提交阶段明确失败，不留下等待者。

本项不改变正常成功、业务失败、ERR_NotFoundActor 和运行中 40 秒超时的消费方式。正常答复／超时／Stop 都先移除记录再完成，只有仍在名单中的请求能获得终态。已完成成功结果不被退出改写。

结束等待不保证撤销接收端已经执行的操作；该业务问题留给具体产品会话契约。

### 3. 旧计时和旧答复都要挡住

旧计时依靠 EntityRef 或捕获的 InstanceId 校验所属生命周期，await 返回后先确认该实例仍有效，才能读取字典、计时或耗时统计；无需新增 runId、世代字段或另一份 alive 状态。等待返回路径同样不访问已销毁 TimeInfo 或旧 Fiber。

旧答复还需解决编号重用：响应只凭 RpcId 查名单，旧发送器的编号 1 不能被新发送器再次使用。建议在原 System 内采用当前托管域共用、线程安全且不回绕的整型编号分配；遵循项目静态字段规则。编号不随 Stop、对象池复用或 World 重建归零，耗尽时明确拒绝，不截断为可重复编号。

这只改本地请求编号来源，不增加消息字段、持久编号服务或回调表。托管域重载不恢复旧请求，本项不声明跨域／跨进程隔离恢复。网络 Session 的 RpcId 与外层桥接由未来产品接入处理，本项不修改它。

### 4. 文件、程序集和文档

| 候选位置 | 改动与验证 |
| --- | --- |
| ET.Core/Runtime/Fiber/Module/Actor/ProcessInnerSender.cs | 停止状态与必要编号数据，核对实例初始化／复用 |
| 同目录 ProcessInnerSenderSystem.cs | Stop、Destroy、入口拒绝、完成与超时身份检查、编号分配；验证正常与退出交错 |
| 同目录 MessageSenderStruct.cs | 必要的响应类型元数据，保持已有等待者和消费接口 |
| Unity/Assets/Tests/Editor/ET（拟新增） | UTF 测试夹具和必要 asmdef，引用 ET.Core／UniTask 等实际依赖；不增加业务程序集 |
| references/reactive-and-network.md、Docs/AI/module-index.md | 写清本地停止能力与消费方式，明确与 Session、产品会话的区别 |

本项不改 Excel、Proto、Scene、Prefab 或生成代码。测试使用非生产的消息夹具，注册到受测试管理的 ET 环境，不为了测退出新增生产协议。

### 5. 有意义的验证

测试通过真实 Call、消息队列和 ET 生命周期组织，覆盖停止后请求拒绝、多等待者结清、两种结果模式、直接销毁和重入。不能只检查字典数量或通过反射调用一段完成函数冒充端到端收尾。

用可控的测试时间与实际 Timer 更新触发超时，不依靠等待真实 40 秒，也不降低生产超时来让测试通过。让旧计时实际恢复、旧回复实际进入重建后的队列，确认它们不影响新请求；同时验证正常答复与当前超时仍按原方式完成。

另做 sender／所属树 Dispose 的验证，包含计时器先退出的情形，保证收尾不依赖 sibling 顺序或存活单例。夹具在 finally 清理自身环境，不运行于用户已有 Play 或测试现场，不替换活跃产品 World。

编译分别覆盖 Unity ET.Core 和引用同源文件的 DotNet.Core，按实际依赖报告。UTF 通过只证明本地消息收尾，不证明双 Editor、网络或产品玩法。

## Risks / Trade-offs

- [通知等待者时发生重入] → 先停止、清空名单、撤下旧入口，再完成快照，重复调用不重复收尾。
- [只清字典，下一轮编号重复] → 当前托管域编号不重用，旧计时另做实例检查。
- [销毁时创建失败响应仍依赖已退出单例] → 提交时捕获响应类型，收尾直接构造独立失败结果。
- [测试自行实现另一套消息逻辑] → 使用真实发送器、队列、计时与 Dispose，只在测试时间和消息源做夹具。
- [共享源码导致只在 Unity 通过] → 核对并分别报告 Unity／DotNet 的编译范围，缺依赖记 NotRun。

## Migration Plan

1. 审核本项规划后按明确 apply 请求实现，先写真实生命周期回归并修原 owner；本轮不动代码。
2. 分步验证停止终态、直接销毁、交错／重入和旧生命周期隔离，同步对应使用说明。
3. 按当轮授权执行精确工程的 Pipeline 编译及 UTF，并核对 DotNet 适用编译；没有环境的项目记 NotRun，相关任务保持未完成。
4. 完成验证后按项目流程同步 network 增量并归档；不自动提交、推送或开工产品会话。
5. 如需回退，只撤回本项实现与对应说明，不删用户数据、不停止其他工程；旧收尾缺口随回退重新存在，须明确记录。

## Sources

- [发送器](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSender.cs)、[发送器 System](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSenderSystem.cs)、[等待者](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/MessageSenderStruct.cs)
- [响应构造](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/MessageHelper.cs)、[错误码](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Network/ErrorCore.cs)
- [消息队列](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/Actor/MessageQueue.cs)、[计时器](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Timer/TimerComponent.cs)
- [Entity 销毁](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Entity/Entity.cs)、[World 销毁](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/World/World.cs)
- [ET.Core 程序集](../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/ET.Core.asmdef)、[DotNet.Core 同源引用](../../../DotNet/Core/DotNet.Core.csproj)
- [当前网络规格](../../specs/network/spec.md)、[数据与网络约定](../../../references/reactive-and-network.md)、[现有测试入口](../../../Tools/AutoTesting/README.md)
