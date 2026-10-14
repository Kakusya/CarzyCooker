# Design

## Context

见 [proposal.md](proposal.md)。当前没有 Profile/SaveSlot/LoadedSave，Continue 只预检配置后换内存关。现有 ET.Core 的 MemoryPackHelper 可序列化普通数据，当前 Solo 容器、加工、布局和拟交接供应已有清楚 owner。本案不能把 GridWorldSnapshot 直接当产品档，也不序列化 Entity。

## Goals / Non-Goals

**Goals:** 同设备 PC Solo 主动新建成功检查点，写盘失败保持结算，重启恢复新运行。

**Non-Goals:** 不覆盖已有 Slot、不写所有玩家文件、不实现授权包、跨设备领取/安全协议、联机存档、DB 服务或断电恢复平台。未绑定档的内存 fixture 保持原 Continue 合同。

## Decisions

### 1. 本机 repository 与运行结果分别归属

```text
Main Fiber / ProductRoot
  ProductRuntime / Timer / ProcessInnerSender
  SoloSaveRepository + Ordered MailBox [Component, Solo only]
    local Profile metadata / file operations
  SoloSavePresentation / UIComponent -> UIGridKitchen [selection only, before Host]
  ProductHost (created after valid selection)
    Match
      Participant...
      SoloSaveBinding [Component, Profile/source/current result reference]
      SelectedSaveBaseline [Child, copied data]
      SoloSaveResult [Child, immutable encoded success data + async owner]
      RestaurantRuntime
        GridKitchenComponent -> Item / Station / Process / Supply...
        RenovationComponent
        Level -> GridActor / Customer / Order / FrontWork / Growth / Buff
        Companion
    HostSession + Ordered MailBox -> HostPeer + Ordered MailBox + LocalDelivery
  ProductClient
    ClientSession + Ordered MailBox -> GridClient
    GridPresentation / UIComponent / GFEntityComponent
```

Profile/Slot 的文件不是运行 Entity；Repository 只按本机规则操作文件和轻量元数据，不拥有世界。SelectedSaveBaseline 是校验通过的纯数据副本，归 Match；SaveResult 在当前成功时捕获且归 Match，文件操作不依赖释放后的厨房对象。不选档不创建假 LoadedSave。

创建顺序：表/根 sender → ProductRoot 的受管选档 UI / Solo repository →本机 Profile及选中记录完整校验 → Host Match/基线/Restaurant/Level → ProductClient/可信本地 Peer/Join →投影/界面。选择前没有客户端或 Host 世界；根 UI 关闭后正常游戏 UI 归 ProductClient，二者复用同一受管表单资源，不同时拥有一份 HUD。加载取消只退出自己的等待并重开选择，不预先创建餐厅。整体先停业务/显示等待及保存任务，原 sender.Stop 后释放本轮树；文件已提交事实留 repository，退出不能补成功结算。

### 2. 显式新建的检查点边界

选档提供新开与继续：新开只绑定本机 Profile 和保存模式，不预写成功 Slot；继续才载入选中的真实基线。没有来源档用明确空来源元数据表示，不创建假 LoadedSave。

在 Completed 且装修选择锁定或无候选时，通过同一本地控制链捕获 SaveResultId、本机 ProfileId、来源 Slot/revision、下一关定义和持续状态。已有完成态冻结业务，确保导出无并发世界写入。UI 显示“保存并继续”，明确首版新建独立记录；来源 Slot 永不覆盖。

Continue 在绑定存档模式先检查当前结果是否持久提交，无事实则 SaveRequired。保存请求先快速接受；Restaurant 服务步捕获不可变结果并发起所属保存。写入失败只是持久失败，不把自然成功变业务失败，保留同一结果供重试。持久完成后的原 Continue 预检/安装仍唯一执行；若下一配置非法，保留已保存事实且留在结算，不再写另一份档。

一次成功保存的载荷描述“完成关的持续状态 + 已确定待安装装修 + 下一关配置”，恢复时重建一次下一 Preparing。已在本次 Continue 应用的装修，不再次附送；原份数和供应余额绝不回满。准备中的移动不另存，下次自然成功再捕获。

### 3. 版本化纯数据与恢复

采用已引入的 [MemoryPackHelper](../../../../Unity/Assets/Scripts/Library/ET/Core/Runtime/Serialize/MemoryPackHelper.cs) 序列化显式 DTO，文件为有版本/平台标签及有界轻量头的包络，载荷为纯数据；不复用网络消息或池化 DTO。Header 保存 SlotId、OwnerProfileId、revision=1、ResultId/领取者、来源 Slot/revision、完成/下一关定义和显示元数据。浏览只读有界 Header，不完整反序列化每个厨房。

载荷为工位/物品/作业/供应等数据列表和保存内引用键。导出分配保存内 Item/Station/Process 键，保存 DefinitionId、位置、内容键、脏净、输入/输出关系、已用和冻结 RequiredTicks、剩余份数、余额、货物状态/剩余业务时间及装修计划。成功时仍在手槽的物品在载荷中按原规则归最后提交格；实际世界到 Continue 才退役角色。不保存 Member绑定、临时认领、路径、待执行命令、ET InstanceId/ActorId 或旧 Version。

载入前在普通值数据上验证版本/平台/Owner、Id唯一、引用闭合、容量、地形/出生位、配方和供应定义以及下一关计划；任何缺口明确拒绝。通过后创建新 Restaurant/World/Level 和运行 Id，将保存内键映射新 Id，不执行 AddConfiguredStation 的初始物资注入。原绝对 Tick 重基为新业务钟，恢复已用/剩余时间与冻结参数，防止旧 buff 重算耗时。新关角色、成长、buff/订单按既有新关规则创建。

### 4. 文件操作、幂等与失败

平台目录只由本地 HotfixView 的 Unity 平台入口取得 Application.persistentDataPath，传入 repository 的受控任务路径；网络和用户选项不提供绝对文件路径。目录分 PC 平台和本机 Profile，文件名仅由合法内部 SlotId 生成，解析后确认位于应用目录。不开任意导入/覆盖入口。

新记录先写同目录自己命名的暂存文件，完成写入和 Flush，再通过不覆盖目标的同卷 File.Move 公布最终记录。目标存在时只认可同 ResultId/Profile 的已提交记录，否则明确冲突，不替换它。异常仅清理当前写入拥有的暂存文件，不批量清目录、不删除来源档。提交成功后不保留暂存作为用户备份，不建立另一份可写领取账本。

幂等以 Header 的 ResultId/Profile 定位已提交记录；repository 从有效 Header 重建索引，所有新建请求经自身 Ordered MailBox 串行处理。提交后响应丢失可查询同一事实，不再次创建。Header和完整载荷在同一最终文件内，避免分开提交 Slot 和领取消耗两个文件。这里只验证代表性写入失败和重启；不承诺任意断电/文件系统故障下完备恢复。

### 5. 本地消息和异步 owner

选档：受管 UI → root sender.Call(repository.GetActorId()) → ProductRoot 的 repository Ordered MailBox/拟 SoloSaveSelectHandler →元数据/载荷完整校验 →原 Reply 返回普通选择数据 →捕获的 ProductRuntime 执行上述建树。核对 SceneType.All 消费者，不发布 Main 示例事件。未确认启动前不允许厨房输入。

保存复用现有关卡控制请求：UI → ClientSession 的 LevelCommandRequest（SaveAndContinue）→ LocalMessage.Peer ActorId → ProductHost 的 Peer Ordered MailBox/LevelPeerHandler →可信 LevelSubmitRequest → HostSession ActorId/Ordered MailBox/LevelSubmitHandler →快速回执 → Restaurant 服务步建立 SoloSaveResult。其 async owner 经 root sender.Call(repository ActorId)/ProductRoot 的 SoloSaveWriteHandler 执行文件操作；快速回执仅表示接受，不表示持久完成。

文件答复由原等待表返回；结果 owner 的唯一物理观察者复制/Dispose DTO，再 Call HostSession ActorId，发送可信 SoloSaveCompletedRequest。ProductHost 的 SoloSaveCompletedHandler 只允许 HostSession 目标，核对原结果实例与 World/Level，复制 Header 事实到 SoloSaveBinding 的完成队列。Restaurant 服务步即使在 Completed 也处理该队列，确认持久门槛后再用原 Continue 预检/安装，失败保留同一结果供重试。保存等待归结果 owner，UI 关闭只取消显示等待，不取消文件事实；await 后核对原 EntityRef。客户端仅复制 SaveEnabled/状态/失败/记录标识，持久载荷与网络快照独立，事件不触发写盘。

整个运行退出取消所属任务并停止 sender；晚到文件已提交只能作为本机记录被下一次查询，不安装已退出或新运行的关。没有额外请求字典或跨运行活 Entity。

### 6. UI、源与交接

轻量选择归 ProductRoot 的 UIComponent，成功保存反馈归现有 ProductClient UIComponent。两阶段复用已注册 UIGridKitchen、Prefab、CodeBind 和原按钮模板，无新增表/Prefab 或私有根面板。选择提供新开餐厅、继续记录和未绑定档的内存试玩；加载失败保留选择，取消退出当前 UI 等待并重开选择，不创建世界。Escape 也取消当前选择等待。正文明确只新建，不提供覆盖按钮。

先交接供应和装修实际字段，再导出 DTO与完整预检、文件提交、唯一完成门槛及恢复UI。Model 持 DTO/状态，Hotfix 持域校验与本机文件能力，ModelView/HotfixView 只传本地平台目录和显示。修改说明时将“内存试玩”和“绑定档单机”写清；Host/Client 仍保留内存模式，不能顺手打开联机领取。

## Risks / Trade-offs

- [失败或路径错误损坏用户档] → 只新建、不覆盖、受控路径和原档字节保持断言；不递归清理。
- [恢复叠初始物资或重算耗时] → 独立数据恢复路径和实际剩菜/加工/货物断言。
- [关闭 UI 后丢持久事实] → 保存归结果 owner，文件事实与显示等待分离。
- [先换关再保存失败] → 快速回执不代表提交，服务步持久门槛核对当前成功身份。

## Migration Plan

新增版本化 DTO/校验和 repository，再接单机选档、成功捕获/持久门槛和完整恢复。使用专属测试目录执行新建失败/冲突/幂等与实际文件往返 UTF，保护用户目录；普通 Launcher 必须保存后退出并重新进入验证新身份和持续状态。只做当前 PC Editor 单机，Player/AOT、跨设备、授权及断电故障保持 NotRun。
