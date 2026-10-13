# Design

## Context

动机与范围见[proposal.md](proposal.md)，行为见[kitchen-batch-cooking delta](specs/kitchen-batch-cooking/spec.md)。本change已14/14实施，运行见[执行记录](execution-notes.md)。下列为提案建立时的基线事实；九案最终owner已按[流程设计](../2026-10-13-solo-restaurant-level-flow/design.md)迁移，UI程序集／受管结构按[UI设计](../2026-10-13-solo-restaurant-ui/design.md)接入，当前协议12。

- `GridKitchenComponent` 是 Match 的 Component，GridActor／GridStation／GridItem 是其 Child；50ms 固定 Tick 先应用成员变化，再处理角色意图，最后发布独立快照。Match 的 Children 仍仅作为 Participant。
- 当前备料已有 `List<long>` 内容、GridManualProcess、ExpectedTargetItemId／ExpectedProcessId 和协议 3，真实表／源码及 [归档验收](../2026-10-11-grid-kitchen-prep-processing/execution-notes.md) 可核对。汤新增行为尚未实施；历史备料通过不能替代煮汤验收。
- `GridPeerHandler` 向 `GridSubmitHandler` 注入可信 Peer 实例和代次；玩法入口快速答复，Tick 才改世界。`GridBroadcast` 为每个 Peer 建独立 DTO，`GridSnapshotHandler` 复制后回收消息。
- 四个 ET Code asmdef 沿用 `UNITY_ET` 等当前条件。数据及投影在 `Game.ET.Code.Model`，裁决在 Hotfix，Mono／受管视图数据在 ModelView，展示／输入在 HotfixView；现有 `CarzyCooker.ET.Tests` 引用 Model／Hotfix，没有 View 引用，视图输入仍需真实入口验收。产品数据继续限定 `!DOTNET`，不增加程序集。
- `ET.Entry` 在 ProductRoot 创建前加载配置；ET Luban 当前 active=true，五目标和 c／s／e 组已存在。协议使用当前 `ET-Client/proto.conf` 和 Proto2CS，不另设生成链。
- HUD 已有 HandText／HintText／StatusText、TargetsRectTransform／TargetTemplateButton，Board 与 HUD 由 Client Scene 的 GFEntityComponent／UIComponent 托管。

## Goals / Non-Goals

**Goals:** 用当前实物关系完成单机倾倒、自动煮制和逐份盛装；加工与玩家分离，验证单槽、份数和输出身份，全部经真实本地裁决。

**Non-Goals:** 不先重构正式 Kitchen／Level，不把锅做成独立网络 Actor，不新增通用工厂、调度器、状态缓存、私有请求等待表或测试／端口工具。只支持本汤配方的蛋液碗倾倒，不扩展通用嵌套、多份原料倾倒、清空或取料动作全集。

## Decisions

### 1. 顺序交接，共用原 owner

前置备料必须已实施并有对应功能验收，且表／协议／Tick／HUD 不再由另一会话同时写入，才进入 apply。届时核对真实 ItemId 内容、作业身份、动作选择和配置类型；若实际交付与本计划不同，先同步本 change，不为缺失前置临时补一套物品或手工系统。无需自动提交、归档或操作第二工作区。

本轮目标树（标记“前置”的节点由备料 change 交付，标记“新增”的节点由本 change 增加）：

```text
主 Editor / Main Fiber / Scene(ProductRoot)
  Components: ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Child Scene(ProductHost, Solo authority, no network listener)
    Child Match
      Child Participant... (member authority)
      Component GridKitchenComponent (world authority / fixed Tick)
        Child GridActor... (hand / binding generation)
        Child GridStation... (counter item / start-slot reference)
        Child GridItem... (DefinitionId / unique location / content IDs) [前置]
        Child GridManualProcess... (manual work) [前置]
        Child GridAutoProcess... (pot / inputs / progress / portions) [新增]
    Component HostSession + Ordered MailBox
      Child HostPeer + Ordered MailBox + LocalDelivery
  Child Scene(ProductClient, local player)
    Component ClientSession + Ordered MailBox
      Component GridClient (independent value projection)
    Component LocalMessage (trusted HostPeer reference)
    Component GridPresentation
    Component UIComponent -> UIGridKitchen
    Component GFEntityComponent -> GridKitchenBoard

```

AutoProcess 由 GridKitchen 释放，PotItemId／InputItemIds／StartStationId 为关系身份。锅和工位仅索引 ProcessId，不复制进度或份数；Actor／Participant／Session 不拥有自动作业。保留厨房同级 Process 的方向，当前不创建正式 RestaurantRuntime。

创建顺序：配置完成 → 原根／Sender → 地图、初始实物与本地入口 → Join → Tick 角色／快照 → 托管视图。Solo 不建监听。AutoProcess 只在合法启动提交时创建。把作业挂到灶台或持锅者会使端锅／角色退出错误带走作业，因此仍归厨房。正式流程后续迁移 owner 时复用本 Process，并撤去厨房独立 IUpdate，沿唯一餐厅驱动。

### 2. 最小配置与可调初值

沿用前置 `Product/GridItemConfig.xlsx` 的真实定义 ID，新增锅和番茄蛋花汤；碗接受集合补充汤，保持容量 1。锅容量 2，允许蛋液和番茄块各一份；针对当前部分投入校验配方前缀，拒绝原番茄、鸡蛋、重复同类和无关材料，不能只凭容量允许错误组合。

新增 `Design/Excel/ET/Datas/Product/GridAutoRecipeConfig.xlsx`，以 `DTGridAutoRecipeConfig/DRGridAutoRecipeConfig` 注册 c,s,e。最少字段为 Id、ContainerDefinitionId、InputDefinitionIds、InputCounts、RequiredTicks、OutputDefinitionId、OutputPortions、ServingContainerDefinitionId。两组输入字段一一对应，种类不重复、数量为正，定义引用有效；输出与目标容器兼容。用现有加载后的 Tables 查询，不另存定义副本；跨表核对置于加载完成边界，不在单表 PostInit 提前访问其他表。

本提案初值为番茄块×1＋蛋液×1、120 Tick、2 份，参数冻结于每次启动；它们只是可调样机参数，见 [execution-notes.md](execution-notes.md)。默认水完全不建物品。当前地图在未占用的厨房格 `(1,8)` 增灶台和空锅，`(3,14)` 增第二碗台及空碗；实际交接时若前置占用了这两格，只调整固定布局，保留厨房初始一屏可见与四邻可达。每个台面仍一外层物品，不在原碗台叠两只碗。

仅新增一条汤配方，不导入候选目录 F04／87 菜，也不新建专用工位表。灶台种类沿前置工位分类最小扩展。无热重载运行中的配方；不用硬编码同名汤作为配置缺失兜底。

### 3. 倾倒内容与默认目标动作

蛋液倾倒复用前置的容器转移校验，但作为明确的新意图：手持碗中须恰有一份已完成蛋液，目标为台面未锁定锅。成功只转移该蛋液 ItemId，清空碗内容索引，保留碗在原手槽；同步解除来源碗中已经完成的手工作业关联，不再消费旧鸡蛋或重做打蛋。原完成记录不得把空碗识别为仍在加工。

倾倒校验来源碗、内容身份、目标锅、允许材料数量、四邻位置、停驻及无活动手工锁；完整通过后统一提交。番茄块加入复用前置添加意图并补齐锅的配方前缀检查。不扩展为锅套碗，也不把字符串显示名当类型。

原共用动作解析增加下列分支；发起时冻结动作及两端 ItemId，到达后重验，不能改做替代动作：

| 状态／目标 | 默认动作 |
| --- | --- |
| 手持蛋液碗，目标未锁定锅 | 倾倒蛋液 |
| 手持番茄块，目标未锁定兼容锅 | 前置添加动作 |
| 空手，灶台有材料齐备的未开始锅 | 开始煮汤；另有直接“拿锅”按钮 |
| 空手，灶台／普通台面有进行中或完成锅 | 拿锅，作业和份数随锅 |
| 手持空碗，目标为台面完成锅 | 盛一份，冻结该批次身份 |
| 手持锅，目标为空台面 | 普通放置 |
| 其他状态 | 保留前置规则；不兼容／锁定明确拒绝 |

灶台只允许台面上的锅显式启动，满料锅放在普通台面不会自动启动；空手对普通台面锅仍是拿取。锅在手里／地面时继续煮，但盛装要求源锅先放到台面；本轮不增加角色对角色或直接地面盛装。

### 4. 自动作业与逐份提交

新增 `GridAutoProcess`，最少记录 RecipeId、PotItemId、InputItemIds、StartStationId、StartTick、RequiredTicks、ProgressTicks、OutputDefinitionId、TotalPortions、RemainingPortions 和 Cooking／Ready 状态。ProcessId 同时是本轮 BatchId；锅只保留该关系，无另一个可写完成标志或份数字段。

开始须完整校验配方、锅位置、源输入和原灶台没有进行中作业。开始 Tick 不推进；每次后续 Tick 在成员变化和互动后推进自动作业，再统一递增世界版本及广播。每个进行中作业每 Tick 至多推进一次。创建后不依赖原成员仍在线或锅仍在灶台；移动锅保留原灶台的进行中占用，到点后释放该占用，锅的批次保留至耗尽。原灶台可放其他物品，但旧作业尚未完成时不能启动第二作业。

到点只把同一 Process 转为 Ready、安装冻结的产量；原输入仍在锅内，没有 Soup Item。重复开始或相同业务编号不创建另一 Process，不重新冻结耗时／份数。Cooking／Ready 期间禁止添加、取料、清空及取消；原拿放不触碰内部锁定关系。

盛装要求玩家停驻且四邻源台面，手持兼容空碗，台面仍是原锅及原 Ready Process，RemainingPortions > 0。现有 ExpectedItemId／ExpectedTargetItemId 分别固定碗／锅，前置 ExpectedProcessId 在此固定 BatchId，纳入意图相等性和完整复制；不能只看汤名或锅 ID 接受已耗尽的旧批次请求。

完成完整预检后，一次提交新 Soup Item（新 Id、内容位置为原碗）、碗索引与份数减一。非最后一份保留原输入；最后一份在同一次提交中消费原输入、清锅内容／Process 引用、结束并释放 Process，锅与碗外部位置均不变。之后新材料可开始新的不同 ProcessId；旧盛装／启动重放不能复活旧批次。验证错误不预分配半份成果，也不先扣再补偿。保留当前 Tick 内已有有序处理，不引入通用事务框架。

### 5. 原消息路径与数据所有权

本地：`GridPresentation` → `GridClient.CreateGridCommand` → 前置所属异步 `ClientSession.CallProductValueAsync` → `LocalMessageComponent` 捕获可信 HostPeer EntityRef → 本 Fiber 根 `ProcessInnerSender.Call(peer.GetActorId())` → MessageQueue → Peer Ordered MailBox → ProductHost 的 `GridPeerHandler`。

本轮只走以上本地路径；远端传输与 wire 新动作资格留到独立联机提案，不向自己发 UDP。

共同：Peer 注入可信实例／代次 → `GridSubmitRequest` → sender.Call(HostSession.GetActorId()) → HostSession Ordered MailBox → ProductHost 的 `GridSubmitHandler` 快速验世界／绑定／协议及编号，返回 Accepted／Rejected；地图 Tick 处理到达及物品／批次再验后提交。开始成功仅表示作业启动；不让公共请求等待 120 Tick 或等待取完汤。

入口答复通过原两段 sender 等待表返回。提交后 `GridBroadcast` 为每个 Peer 生成独立 `GridWorldSnapshot` 与本人 `GridCommandResult` → LocalDelivery 的 sender.Send(ClientSession.GetActorId()) → 本地客户端 Ordered MailBox → ProductClient 的 `GridSnapshotHandler`／`GridResultHandler` → 完整复制内容图与 Process 值后回收消息 → `GridProjectionApplied`。Host 的 `GridWorldCommitted` 也是提交后的本端观察事件，不承担发送或请求答复；继续核对 SceneType.All 消费者，不新造 DynamicEvent 路径。

快照增加自动作业／批次状态、锅 ProcessId 与灶台当前作业关系；内容、进度、份数同版安装，旧世界／绑定／较旧版本不能应用。DTO 回收后投影仍有效，不保存 Host EntityRef 或消息集合。每 Tick 推进标记原世界 Dirty，不另建 UI 计时权威或 Delta 协议。

只改 `ZZGridKitchen.proto` 源再正常生成，入口与内部提交均完整传递目标与 Process 身份；若增加自动作业 DTO 类型，追加文件末尾，不插入重排旧 opcode。内部 GridSubmit 不进 wire 白名单，新增单机动作也不自动开放远端。版本从实际基线递增，不能硬退版本或自动改另一会话。

actor handler 负责输入释放，原所属值观察者 finally 消费响应与物理在途责任；本地通知由接收 handler 释放。沿 [本地消息收尾](../2026-10-13-solo-local-message-contracts/design.md) 检查 await 后 owner／原实例／绑定，不以业务等待取消提前结束物理责任。无第三份回调表，无“等加工完成”的长期 RPC。

### 6. 退出、展示与验收

单成员 Closing 立即撤销资格，下次地图 Tick 取消旧未执行意图、释放其手工认领并把手持锅落在最后提交格，随后自动作业仍推进。已提交 Leave 同样收尾物品、另按原规则移除 Participant；重复清理不再落锅。单成员不得 Stop 公共 sender。

整体先拒绝新意图并 StopGrid，不再推进自动作业；关闭原 GridPresentation／受管视图，结清本地 sender／Session 等待，再释放原树、Process 与实物。已完成但未盛装内容不变成存档；晚到请求、视图加载或 DTO 不能写到下一世界。

复用现有 Board／UIGridKitchen 和绑定，增加锅内容、进度／Ready、剩余份数及“拿锅／煮汤／倒蛋液／盛一份”标签。按钮可用性纳入 Process／内容状态，但不每 Tick 重建所有按钮；计数来自投影。维持原 owner 的打开／关闭及订阅释放，不直接 SetActive 受管根。优先不改 Prefab／CodeBind 结构；若必需，经安全 Editor 修改源并重生成，不手改 Bind.cs。

验收映射见 execution-notes。拟在现有 ProductSessionTests 增加 Transfer／Cooking／Portion／Local 小集，采用真实 Tables、活跃 World 拒绝保护和真正不建监听的 Solo fixture，分别泵消息与推进 50ms Tick。普通 Launcher Solo 实走完整输入链，不直接安装半成品替代整条可玩链。末份竞争只作为局部双本地调用者 UTF：提供足够初始空碗，不把两碗已占一碗的生产场景宣称能双人争末份。生产仍保持两碗，不为测试增加第三碗。

## Risks / Trade-offs

- [倾倒丢碗、复制蛋液] → 两端身份与内容关系完整预检，一次转移同一材料；UTF 同时断言原手槽、碗和锅。
- [盛装复制末份或旧批次复活] → 意图固定 BatchId、Tick 再验、最后输入消费与份数／输出一起提交。
- [端锅／本地角色退出错误停煮或灶台并发] → Process 归地图，起始灶台占用与外层锅位置分开；验证搬锅、Leave、工位占用和整体退出。
- [与备料会话互相覆盖] → 只建立新 change；apply 先取得前置完成交接，再串行改共享源与生成输出。
- [把片段当完整代表菜交付] → 只报告厨房制作／盛碗，订单提交、用餐和洗碗仍未实施，样机参数不转为正式平衡。

## Migration Plan

1. 后续获得 apply 授权并完成前置交接；载入实际表／协议合同及原功能结果，确认共享文件可写。
2. 保留前置数据，追加物品、自动配方与初始地图；五目标 Check 成功再导出，失败不使用旧产物。
3. 接入原 Tick 的倾倒／自动作业／盛装、消息与独立投影，正常生成协议并一致升级两端。
4. 完成最小相关本地 UTF 与真实 Solo 输入，逐功能填写结果；同步受影响文档并严格检查 change。

没有产品持久数据迁移。异常修复后创建新运行；撤回按明确范围逐项处理自己的源码／源表／协议，两端保持相同版本，不 reset 其他会话或用户修改。不自动同步主规格、归档、提交或发布。
