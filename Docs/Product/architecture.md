# ET 业务目标结构与消息路径

本文为已定稿的目标设计，业务尚未实现，不是底座已有能力证明，也不自动授权开发。行为契约见 [八份目标规格](specs/README.md)，当前源码事实仍以实现及 [OpenSpec 主规格](../../openspec/README.md) 为准。

## ET 树表达释放责任，业务关系用稳定 ID

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

## 本地消息、远端消息和提交顺序

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

## 最小会话与完整产品的边界

[最小会话 change](../../openspec/changes/product-session-bootstrap/design.md) 只规划 Match、Participant、连接绑定、加入、探测、名单和退出；本地与远端共用有序控制入口。连接断开保留离线成员，已提交显式 Leave 移除成员，Host 整体退出解散 Match。

该原型不创建 LoadedSave、RestaurantRuntime、Kitchen 或 Level，不实现 Profile 认证、成员恢复凭证、自动重连、存档或固定业务 Tick。新连接的 Join 仅加入新成员，不允许凭自报编号接管原成员。完整产品的原成员恢复规则是后续目标，不作为本原型已实现能力。

## 候选和待裁决范围

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

## 来源与维护

- [设计交接 A、G、H](../handoff/Cooking-设计交接-2026-10-09.md)
- [禁止项交接](../handoff/Cooking-禁止项交接-2026-10-09.md)
- [产品待办与未决](backlog.md)

本页与目标规格共同作为长期产品入口；对应文档 change 的归档只记录整理交付。具体 SceneType 位值、字段、ActorId、超时和事件注册以获准实施 change 及实际 ET 源码核对，不从归属示意推断已经注册或隔离。
