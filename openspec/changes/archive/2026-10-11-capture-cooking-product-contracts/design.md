# Design

## Context

动机和范围见 [proposal](proposal.md)。资料为 [设计交接](../../../../Docs/handoff/Cooking-设计交接-2026-10-09.md) 与 [禁止项交接](../../../../Docs/handoff/Cooking-禁止项交接-2026-10-09.md)，已读取全部内容。现有 [产品设计](../../../../Docs/Product/design.md) 已保留大部分同源语义；本项将其形成可审核场景，不宣称新发现等于新实现。

当前 [进度](../../../../CarzyCooker当前进度.md) 仍为底座和示例。核对了 [ET Entry](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Entry.cs)、[本地 sender](../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Fiber/Module/Actor/ProcessInnerSenderSystem.cs)、[Session](../../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message/Session.cs)、[NetComponentSystem](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Message/NetComponentSystem.cs) 的规划依据，并对照 [network](../../../specs/network/spec.md)、[runtime-foundation](../../../specs/runtime-foundation/spec.md)。它们没有实现 Match、厨房或检查点，也不提供业务重连与幂等保证。

会话早期草案曾把成员作为连接附近的临时名单，并把意外断线删除／保留列为裁决。这与交接 A3／G3 以及现有产品设计明确保留逻辑成员冲突；本项已校准，当前会话规划按 Match 持有 Participant、断线保留成员。定稿补齐交接已经规定的代表汤、离席订单、饮品绑定与供应细节；不恢复旧项目实现、LiteNet 路线、FlowAcceptance 工具或历史端口段。

## Goals / Non-Goals

**Goals:** 捕获已确认与授权补足的目标规则，提供正反场景、来源映射、ET 生命周期和消息边界；便于用户逐域审核并约束后续小范围 change。

**Non-Goals:** 一次实现八个业务域，导入旧代码，安装工具，运行测试，同步业务为当前支持的主规格；代定候选内容、数值、暂停队列、断线手持物、授权安全协议及正式传输。本轮归档仅覆盖已获授权的文档整理。

## Decisions

### 1. 新建目标规格草案，保留资料身份

本 change 的 delta specs 保留本次捕获记录；定稿目标规范交付到 [Docs/Product/specs](../../../../Docs/Product/specs/README.md)，ET 归属和消息设计交付到 [产品架构](../../../../Docs/Product/architecture.md)，不覆盖底座主规格。各 Purpose 明示未实施。按业务寿命和行为分域；产品设计保留背景说明，长期目标规格约束后续实施 change。历史归档用于追溯，后续规则变更以产品长期入口为维护正文。

| 规格 | 主要来源 | 规则身份与范围 |
| --- | --- | --- |
| [cooperative-match](specs/cooperative-match/spec.md) | A2–A5、G3、G5 | 已确认身份和寿命；自动重连协议另案 |
| [authoritative-cooking-runtime](specs/authoritative-cooking-runtime/spec.md) | A4、G1–G3、G5 | 已确认共同裁决；幂等和固定步异常等历史工程细化为授权补足目标，不是底座事实 |
| [restaurant-level-lifecycle](specs/restaurant-level-lifecycle/spec.md) | C1–C5、H2 | 已确认与授权补足；缩小许可的细分属授权补足，不擅自新增 UX |
| [kitchen-item-processing](specs/kitchen-item-processing/spec.md) | D、F1–F3 | 已确认及授权补足动作；容量耗时未定 |
| [orders-front-service](specs/orders-front-service/spec.md) | C2、C6、D8、H2 | 自动服务基线及人工前厅授权补足，区分自然排空和历史自动收口 |
| [fixed-companions](specs/fixed-companions/spec.md) | C7–C8、H1–H2 | 固定身份岗位、成长公式已确认；人工接手属授权补足 |
| [supply-layout-recipes](specs/supply-layout-recipes/spec.md) | E、F1–F5 | 配方完整性、物理供应和布局授权补足；目录不晋级 |
| [save-progress-claims](specs/save-progress-claims/spec.md) | B、G5 | 产品归属、领取和原子提交已确认；安全和完整故障模型未实现 |

旧资料工程禁止项与当前 AGENTS 对照后只沿用适用边界。历史测试、框架、版本与开发授权不延续到这里。旧 source 路径仅作原件出处，不成为现行链接或执行入口。

### 2. ET 树表达释放责任，业务关系用稳定 ID

以下为目标业务树，节点是归属示意，实际类型名留给具体实施 change。第一期通信只需要 Match、Participant 和连接绑定，不实例化未选存档的餐厅，也不同时实现下面所有节点。

```text
ProductRoot Scene（每进程一个适用 Fiber；独立 SceneType）
  Timer / CoroutineLock / ProcessInnerSender / 本轮运行 owner
  ProductHost Scene（Host 进程才有）
    Match（协作 owner；可暂时无餐厅）
      Participant Registry
        Participant（稳定成员；不随 Connection Dispose）
      LoadedSave（仅校验通过的选中基线）
      RestaurantRuntime（同时最多一个未关闭实例）
        Kitchen（持续）
          Item（可具有 Container 能力）
          Station
          Process（稳定引用 Station，不由 Station 负责释放）
          Supply / Shipment / Receiving
        Level（同时最多一个未结束实例）
          PlayerRole（本关角色，引用 Participant）
          Customer / Order / FrontWork
          Clock / Score / CompanionGrowth / Buff
        固定伙伴身份及岗位（本关工作和成长归 Level）
      SaveResult / Claim（运行对象；终止前导出允许的离线凭证）
    HostSessionComponent（连接管理和共同有序控制入口）
      HostPeer（可替换可信入口；引用 ParticipantId／绑定代次）
        MailBox / LocalDelivery 或 RemoteDelivery
    NetComponent
      Session（物理连接；引用 HostPeer，不拥有 Participant）
  ProductClient Scene（Host 本地玩家或独立 Client）
    ClientSessionComponent（本人绑定信息与只读投影）
    LocalMessageComponent 或 NetComponent／Session
```

Host 的 Match 是唯一领域事实，HostSession 的索引只是关联，不能复制一份可写成员名单；Client 的名单和厨房为独立投影。第一期 HostSession 的有序 MailBox 序列化控制请求并修改其引用的 Match，未来烹饪动作必须另经固定业务 Tick，不能把 MailBox 的有序性当作已实现游戏时钟。

Parent 表示释放责任；Participant↔Connection、Process↔Station、Order↔Customer、Profile↔SaveSlot 使用稳定业务身份。跨 await 同时检查 EntityRef／InstanceId 和 Match／Level／绑定代次；不能用业务 ID 相同证明对象仍存活。业务正常收尾在树 Dispose 前完成。

### 3. 写清本地消息、远端消息和提交顺序

```text
Host 本地 Client
  → LocalMessageComponent
  → ProcessInnerSender.Call(可信 HostPeer ActorId)
  → MessageQueue → Peer MailBox → 共用 ProductHost handler

远端 Client
  → Session.Call → 当前 ET UDP-KCP 原型
  → ProductHost SceneType 的 NetComponentOnRead Invoke
  → 复制外层载荷、捕获原 wire RpcId、核对可信绑定
  → ProcessInnerSender.Call(绑定 HostPeer ActorId)
  → 同一个 Peer MailBox／handler

共同处理
  → 以绑定生成内部身份，不信任请求自报 ParticipantId
  → 第一期间接进入 HostSession Ordered MailBox：Join／Probe／Leave
  → 未来玩法：意图入业务队列 → 固定 Tick 预检 → 原子提交
  → 生成明确最终结果 → 发送独立答复／完整快照 → 发布观察事件
```

本地答复经 sender.Reply 和既有等待表返回；远端桥接复制响应、恢复捕获的 wire RpcId，再回原有效 Session。多人通知每接收者独立消息，客户端回收消息前复制状态。不得添加第三份私有等待表，也不让 Host 本地玩家向自身 UDP 发送。

SceneType 用于精确产品入口及适用 handler 匹配；Main 调度不等于 Main 场景类型。Event 只通知已提交事实，Invoke 是精确初始化／网络读／退出入口，Request 是需要结果的消息；审查 All handler 和根级 DynamicEvent，不能仅因新增 SceneType 就假定隔离。

### 4. 校准会话原型，保持分期边界

[product-session-bootstrap](../../product-session-bootstrap/design.md) 已在规划中明确最小 Match／Participant 归属和在线／离线名单。断线关闭 HostPeer／绑定／Session，逻辑成员保留；明确 Leave 才退出逻辑协作，Host 整体 Stop 解散 Match。本轮核对其规划并更新长期规格引用，不实施该会话能力。

原型不做自动重连、Profile 认证或存档。首期每次 Join 仅是当前绑定的新成员加入，不宣称找回旧身份；没有验证凭证，不允许凭自报编号重绑。旧成员可保持离线直到显式离开或 Match 解散。未来原成员恢复协议遵守本组契约，另案设计凭证签发、绑定代次及完整状态恢复。

### 5. 暂不确定的内容不写成任一分支的最终验收

| 项目 | 保留状态 | 本轮处理 |
| --- | --- | --- |
| 暂停未执行队列 | H2：早期明确取消、参考保留、后续终结存在冲突 | 只定义业务时钟暂停，具体队列处理实施前裁决 |
| 断线手持物 | H2：合法位置落地与不自动转移冲突 | 不选择物品去向；确定连接失效和工作释放 |
| NPC 收口模式 | 历史自动模式与人工 Flow 模式不同 | 两者限定范围，不推广瞬间完成 |
| 缩小菜单许可 | 历史授权细化 | 保留旧物和已受理工作，禁新生产；UX 可重审 |
| 87 菜、60 准备状态、72 物料、19 工位 | 候选目录 | 仅捕获定义和依赖规则，不复制为正式配置或首发承诺 |
| 正式传输／公网／主机迁移 | 新项目未裁决 | 保持当前本机 ET 原型，不沿用旧 LiteNet 决策 |
| 人数、价格、Tick 频率、产量、容量、各关内容 | 未定 | 不采用历史测试值为默认平衡或产品上限 |
| 授权签发撤销、关闭认证、密钥、跨设备 UX、中心服务、断电模型 | 未设计 | 捕获产品语义，不声称已有安全保证 |

## Risks / Trade-offs

- [规格数量被误认为实现范围] → 八个领域作为设计索引，后续按最小可观察行为分别立项；tasks 仅审阅和文档收敛。
- [历史补足被误称新项目确认] → 逐域保留身份，未知细节不代定；实施前明确具体验收和适用模式。
- [通信 mailbox 被误认为世界 Tick] → 控制请求和未来玩法提交分开，后续时钟 change 才建立世界推进能力。
- [本地 repository 一次性被扩大为全球一次性] → 明示无中心服务的跨设备限制，安全协议不靠纯设计宣称完成。
- [文档重复漂移] → 同源资料和本次归档用于追溯，Docs/Product/specs 为长期目标正文；后续改变契约同步相关产品说明和实施 change，底座主规格不提前晋级。

## Migration Plan

1. 建立本项八份 delta 和索引，校准会话草案；不改变当前运行行为。
2. 按用户授权由 Agent 对照交接核对、定稿；已有确认不重复要求逐条审阅，真正未决项保持未决。
3. 按既有顺序推进启动项、第二工作区、本地请求退出、最小会话；首个做菜闭环另外提出小范围 change。
4. 只有获得具体实施授权才进入 apply；仅授权本项 apply 时范围是文档审核与收敛，不是实现八个领域。
5. 将目标规格及架构交付到 Docs/Product，验证长期入口、相关会话引用和严格格式后归档文档整理项；不将未实施目标同步成已支持主规格。撤回本项仅调整文档，不回滚用户数据或工作区。
