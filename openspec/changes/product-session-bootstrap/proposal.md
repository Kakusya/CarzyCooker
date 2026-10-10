# Proposal

## 快速审核

**目标：先让两个实例真正连上，完成加入、消息来回和退出。**

- Host 本地玩家走 ET 本地消息，远端 Client 走网络消息，接收后的处理共用。
- Match 持有逻辑成员，连接只绑定成员；断线保留离线成员，Host 整体结束时解散协作。
- 本期交付最小通信与一致成员名单，厨房、存档和自动重连由后续 change 承接。

```text
Host 本地玩家 -- 本地消息 --+
                           +--> 同一权威处理 --> Match 成员名单
远端 Client  -- 网络消息 --+
```

上述方向已在会话中确认，无需逐条重审技术细节。详细设计供实施与一致性检查使用；若发现影响行为的新取舍，再单独说明。当前只完成规划，业务尚未实施。

## Why

已有启动项、稳定第二工作区和本地请求退出的独立规划，但还没有能让 Host 本地玩家与远端 Client 真正加入的产品会话。需要一个最小本机直连原型，以明确的 ET owner 树和发送／接收路径复用同一套成员管理与命令裁决，为后续联机验证提供实际目标。

首期可观察结果是：启动 Host 并让本地玩家通过本地消息加入，再启动一个远端 Client；双方观察同一 Match 的完整名单，完成 Probe 请求／答复，断线后原逻辑成员显示离线，Host 退出后本轮资源释放。以上是验收目标，当前没有对应业务实现。

## What Changes

- 消费单实例不可变启动配置，普通／legacy 保留示例，显式 Host／Client 进入独立 ProductRoot、ProductHost、ProductClient SceneType，不发布旧 Main 示例启动事件。
- Host 进程创建 Host 和本地 Client 两棵子树，远端进程只创建 Client 子树；每个进程一个 Main 调度的产品 Fiber，根上复用 Timer、CoroutineLock 与 ProcessInnerSender。
- Host 本地玩家通过 ET 本地队列和 MailBox 加入，不向自己发 UDP；远端暂用现有 Session／UDP-KCP 本机直连，可信入口转交到同一 HostSession 有序处理。
- 实现最小 Match／Participant、加入确认、协作内成员编号、在线／离线完整名单及无玩法探测请求／答复。连接只绑定成员，断线保留逻辑身份；客户端只持独立投影，Host 共用入口唯一修改 Match 名单。
- 区分连接建立、监听可用与加入成功；补齐协议不兼容、连接失败、接入超时、心跳、主动退出和本轮整体收尾，不回落 Demo，不自动更新稳定基线。
- 写清 ET 树、ActorId／Mailbox 路由、消息对象与答复编号的所有权、Event／Invoke 边界，验证本地及远端都进入同一裁决，并对接已有本地请求停止能力。
- 将逻辑成员状态、可替换连接绑定和客户端投影分别列出唯一 owner；补齐 Join 提交前后、通知先到、迟到请求和单连接关闭期间的顺序与验收。
- 单连接关闭先撤销入口权限，再让已接入本地请求完成拒绝或既有超时，最后释放 Peer；不对共用 sender 执行 Stop，也不扩展本地 Call 的单请求取消接口。

## Capabilities

### New Capabilities

- `product-session`: 产品最小会话的 owner 树、本地／远端共用裁决、成员加入及投影、连接生命周期与验收边界。

### Modified Capabilities

- `startup`: 新增显式产品角色的 ET 根入口分流及准确启动状态；既有装载、普通／legacy 和失败拒绝契约保持。

## Impact

- 使用 Game.ET.Code.Model／Hotfix 的 Share 产品目录，保留既有 Model／Hotfix 分层和 CodeLoader；入口涉及 ProcedureET、ET.Entry 及必要 Loader 交接，复用已有程序集，不让 Game 稳定层反向引用 Hotfix。
- 新 SceneType 落在既有枚举。协议修改 Design/Proto 的启用源及既有生成入口，不手改消息／Opcode 生成物，不改 Excel、UI、Prefab、存档或厨房玩法；两端核对实际 CodeMode、程序集及协议版本。
- 依赖 [启动项](../multiplayer-instance-bootstrap/proposal.md) 和 [本地请求退出](../local-message-request-shutdown/proposal.md) 的能力；双 Editor 实测另外依赖 [第二工作区](../multiplayer-peer-workspace/proposal.md)。环境、配置、本地会话和远端联机各自证明自己的范围。
- 本机 loopback 监听由本轮 Host owner 持有，端口 0 以实际绑定结果为准；必要时只在既有 UdpTransport 暴露只读绑定地址，不引入 PortRegistry、网络服务端进程、Realm／Gate／MongoDB 服务或第三份 RPC 等待表。Mongo 类型注册不等于启动数据库服务。
- 实施后同步 references/reactive-and-network.md、Book/快速开始.md、Book/Unity启动与验证SOP.md 与 Docs/AI/module-index.md，保持正式传输选型仍待裁决。
- [业务交接 A3／G3](../../../Docs/handoff/Cooking-设计交接-2026-10-09.md) 已明确逻辑成员不随连接销毁，撤销原草案的删除／保留裁决门槛。关联 [业务目标规格](../../../Docs/Product/specs/README.md)，本项只落实最小身份和离线保留，不实现自动重连、Profile 认证、餐厅或领取系统。
- 本轮只生成规划；不创建 worktree、不改业务代码、不启动监听或 Editor、不运行测试，也不把规划视为联机已通过。

用户审核从本文“快速审核”开始；技术核对参见 [详细设计](design.md)、[会话规格](specs/product-session/spec.md)、[启动规格](specs/startup/spec.md) 和 [实施任务](tasks.md)。启动配置、第二工作区及框架 Stop 继续由各自 change 承接；本项只消费这些能力，不重复建立它们。
