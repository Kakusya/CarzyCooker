# Design

## Context

见 [proposal.md](proposal.md)、[buff 合同](specs/level-buffs/spec.md)。用户确认随机候选中选一个、本关生效。产品首个已明确效果是煮制加速；首版只交付该效果，不凭空宣布更多 buff 已定稿。

## Goals / Non-Goals

**Goals:** 候选固定、选择一次、效果可观察、旧作业不改耗时、换关不继承。

**Non-Goals:** 不做叠层、负面效果、通用互斥、随机公平统计平台、存档奖励或联网。

## Decisions

### 1. 当前关 owner

```text
Main Fiber / SceneType.ProductRoot
  Component ProcessInnerSender / ProductRuntime / Timer
  Child SceneType.ProductHost
    Child Match -> Child RestaurantRuntime
      Component GridKitchenComponent -> Child GridAutoProcess (frozen RequiredTicks)
      Child Level
        Component LevelBuff (Seed / DrawVersion / candidate DefIds / selected DefId)
        Child GridActor / Customer / Order / FrontWork
    Component HostSession + Ordered MailBox -> Child HostPeer + Ordered MailBox
  Child SceneType.ProductClient
    Component ClientSession + Ordered MailBox -> Component GridClient (copied buff values)
    Component GridPresentation / UIComponent / GFEntityComponent
```

配置验证后，创建 Level Preparing 时建立一次 Seed／候选，再公布首张投影；选择不创建另一个 Buff owner。新关释放旧 LevelBuff、重新创建，持续厨房的作业保留自己的冻结耗时。整体停止驱动与所属等待、关闭视图、Stop sender 后树释放；单界面关闭只取消显示等待。

### 2. 最小配置与随机抽取

BuffConfig 声明 DefId、显示名、EffectKind、DurationNumerator／Denominator、适用 BatchRecipeId。LevelConfig 增 AllowedBuffIds、CandidateCount、RequireBuffChoice。只支持 CookDurationScale；样机可用三个不同倍率定义展示两个候选，均为可调参数，后续有趣的效果种类仍另案。非空池去重、合法正比例与配方引用检查；必选关空池开局报配置错误，可选空池则不阻止开店。

候选抽取使用当前 Unity 运行环境内固定种子的局部 Random 无放回抽样；生产 Seed 创建一次，UTF 可提供受控 Seed。不承诺跨运行时算法版本或不同种子必然不同。UI Open、失败选择和暂停不推进抽样。样机不采用加权池、重抽道具或抽样统计门槛。

### 3. 选择与耗时

Choose 固定 LevelId／Generation、DrawVersion、ChoiceDefId。仅未暂停 Preparing、尚未选择、候选包含目标时提交；成功锁定并满足 Start 门槛。相同命令重放返回既有事实，新命令改选明确拒绝；不能以重开 UI 再选择。

新汤作业在合法 Begin 的同一提交里读取当前 LevelBuff，计算 `max(1, ceil(baseTicks * numerator / denominator))` 后存入作业 RequiredTicks。作业推进只读自己的 RequiredTicks；旧作业、换关留下的作业和已盛出汤不重新计算。选择没有材料写入，不能绕过完整煮制验证、暂停或自然时间。

### 4. 发送与接收

原 HUD 候选按钮 → ClientSession 所属 ChooseBuffRequest → LocalMessage 捕获 Peer ActorId → root sender.Call → Peer Ordered MailBox／ProductHost 拟 BuffPeerHandler → 带可信 Peer 的 BuffSubmitRequest → authority ActorId → HostSession MailBox／拟 BuffSubmitHandler。快速接受与原两段回执返回后，Restaurant 服务步验证准备态并一次提交；不能等待被暂停的业务 Tick 才恢复／拒绝。

每 Peer 独立候选／已选 DTO 与当前关共用提交版本 → LocalDelivery sender.Send(ClientSession ActorId) → ProductClient 拟 BuffSnapshotHandler → 校验旧关／绑定、复制值、回收 DTO → 发布观察供当前 owner 显示。handler 收尾输入，所属物理观察者收尾响应／在途责任；Event 只观察，退出和迟到按 [本地消息设计](../solo-local-message-contracts/design.md)。不新增 SceneType、请求表或网络端口。

### 5. 源链与交接

BuffConfig 位于 ET Product 源工作簿，LevelConfig 在本 change 增字段，__tables__／必要枚举注册 c,s,e，走实际 active ET Check／正常导出。Model 保存候选及选择，Hotfix 抽样／验证／耗时，原 HUD 提供基础候选操作，完整表现交给 UI change。协议末尾追加实际类型再生成，版本从实际基线递增；不改 wire 白名单。

依赖流程与汤作业已经真实实施，不要求顾客或完整 UI 先完成。验收比较所选比例与新作业 RequiredTicks，不能拿性能百分比实验代替行为断言。

### 6. 当前实际接入（2026-10-12，协议12）

原 owner 树落地 LevelBuff Component／普通值队列，Seed 在 InstallLevelAdditions 创建一次，候选局部 Random 无放回，DrawVersion=1；新关重建组件和种子。BuffConfig 源31行 c,s,e、LevelConfig 三个新增字段正常生成。首版 EffectKind=1 只表示 CookDurationScale，倍率3/4、2/3、1/2是样机参数。

实际请求为 ChooseBuffRequest／Response／SubmitRequest／SubmitResponse（10099–10102），BuffPeerHandler 在 ProductHost Peer MailBox 收外部请求并写入可信 Peer 身份，BuffSubmitHandler 在 HostSession MailBox 核验后入队；直接发内部类型到 Peer 拒绝。原两段 handler 回收输入、物理观察者回收响应，view 使用所属值等待并在 await 后核对实例／世界／绑定／关。当前关身份校验先于最新命令重放，避免另一个本地 Peer 换关后返回旧关成功事实。

Restaurant 的控制处理后、布局／厨房推进前处理选择；暂停和整体退出取消未执行选择，Start 检查必选门槛。厨房 Begin 只冻结新作业的 `max(1, ceil(baseTicks*n/d))`，旧作业不重新计算。原 Snapshot 42–46 字段分别是Seed、Draw、候选、已选和门槛，每Peer独立列表，客户端全部复制后同版安装／发布观察；没有第二个buff快照平台、SceneType或监听。HUD开关只改Presentation显示值，不抽样或提交命令；完整UI重开UTF待对应change。结果和明确限制见 [执行说明](execution-notes.md)。

## Risks / Trade-offs

- [测试要求不同种子绝不重复] → 只断言同种子复现、候选有效且不重复，不承诺随机不可能事件。
- [换关或选择改掉已开锅] → 只在 Begin 冻结，测试旧锅／新锅及暂停／跨关续煮。
- [首版候选效果单一] → 明确只有煮制加速的参数变体，更多效果不冒充已交付内容。

## Migration Plan

先验证池／比例与抽样，再接选择本地消息和 Start 门槛，再接新作业耗时，执行最小 UTF 与普通 Solo 选择煮汤。所有功能当前 NotRun，详见 [执行说明](execution-notes.md)。

## Sources

- [产品 Buff 边界](../../../Docs/Product/design.md)、[关卡 owner](../solo-restaurant-level-flow/design.md)、[汤作业](../grid-kitchen-soup-batch-cooking/design.md)。
- [现有输入与消息](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridMessages.cs)、[实际配置工程](../../../Design/Excel/ET/luban.conf)。
