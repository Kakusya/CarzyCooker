# Design

## Context

当前 `ProductOutcomeHandlers`、`GridPresentation` 与 Bistro 动画已经区分执行与结果，GF `SoundExtension.PlaySound` 提供表驱动音效。Sound表当前为空，产品尚未接入音效。`restaurant-ui` 的视觉提醒条款明确不使用新音频，本案通过精确 MODIFIED delta 开放加工完成音效，保留其他类别的视觉边界。

## Goals / Non-Goals

**Goals:** 动作有可辨的短反馈，成功、处理中和拒绝不会混淆；声音可关闭且不跨场景残留。

**Non-Goals:** 无新音频框架、音乐、语音、循环环境声、全员空间声场或新协议。先覆盖本人动作和当前餐厅的完工/结算事件，不为远端每次拿放广播额外事件。

## Decisions

### 有限事件与共享去重

| 类型 | 观察的已提交事实 | 声画表现 |
|---|---|---|
| 拿取 | 本人匹配的实际 Executed 拿取结果 | 轻短音、原持物变化 |
| 放置/入容器/分装 | 本人匹配的实际 Executed 结果 | 柔和落点音、原物件/份数变化 |
| 开始加工 | 原加工已实际开始 | 短启动音、原作业动画/进度 |
| 加工完成 | 新的有效工作完成结果 | 较亮提示、原完成状态与视觉提醒 |
| 成功交付 | 本人当前订单 Serve Executed | 简短确认音、复用原递餐动画 |
| 本关完成 | 当前 Level 首次进入自然 Completed | 短收尾音、原结算 |

收到请求、等待到位和拒绝仅沿既有文字/状态，不播放成功音。首装、重连、首次打开反馈 owner 时先记录当前基线，已发生的 outcome不回放。复用原 command/work/Level 身份及序号，而不是仅按一段时间判断重复；同一加工既有完成提醒又有 outcome时只触发一次。HUD重开不重置 client 级去重。

现有 `ProductCommandOutcomeValue.Kind=Grid` 不携带具体GridAction，不能仅靠该值猜是拿还是放。在原 `SendGridAction` 分配实际CommandId后、发送之前，向本机反馈owner登记“完整command上下文→反馈类别”的短生命周期观察记录；它不保留DTO、实物或可再次执行的命令。`ProductCommandOutcomeApplied` 的终态与该记录匹配后才发声；拒绝/取消/发送失败清除，失联/绑定退役清空，恢复不补登记旧命令。不用可被后续操作覆盖的单一LastCommand推断类型。启动加工与完工沿现有work stage/身份区分，与命令来源合并去重；具体事件通过明确的ProductClient消费者接入，避免轮询LastKitchenOutcome漏掉同一步多条事件。

同一投影步多个完工仍保留全部原视觉事实，声音合并为一次完工提示；音频同时播放预算为三个本案短音，超出时优先保留交付/完工/结算，丢弃较低优先级拿放，不积压到之后播放。单个音0.08–0.6秒，结算不超过1秒；以清楚、低干扰为目标，不将夸张闪烁/屏幕震动作为前置。

### 原始音频与GF加载

在 `Art/CarzyCooker` 保存本项目合成短音的源脚本/参数及说明，导出至现有 `AssetUtility.GetSoundAsset` 对应的 WAV 资源路径。首批六类声音，`Sound.xlsx` 预留本案1101–1106并在实施前查冲突；Loop=false、SpatialBlend=0、SoundGroupName=`Sound.Default`。ID由既有导表器生成，不手写常量映射。若实际制作改用外部素材，必须交付可用许可与来源，不能用来源不明文件补齐。

使用 `GameEntry.Sound.PlaySound(UGFSoundId...)` 和原资源服务，不创建 AudioSource 私有池。反馈 owner 记录自己的播放serial与加载归属；暂停/失联/退役停止本案音，恢复不回放被停止音。加载失败不影响原命令/世界，保持视觉结果并记录可定位错误。已核对GF `SoundManager.StopSound(serialId, fadeOutSeconds)` 会将加载中的serial加入加载后释放集合；本案复用这一取消能力，不另建加载框架。回调仍核对owner/epoch，不让失效结果更新新视图。

### 本机声音控制

通过既有详情容器提供独立“声音”入口，静音和0–100%音量（10%步进）标注即时生效并本机保存；默认未静音、60%。只控制 `Sound.Default`，不改尚未使用的Music/UISound。

复用 `SoundExtension.Mute/SetVolume` 与 GF Setting，明确采用其实际 `Setting.{group}Muted/Volume` 键。`ProcedureLaunch` 当前读取旧的 `Setting.SoundMuted/Volume`，因此产品显示初始化时读取上述分组键（缺失使用本案默认）并应用，避免重启后回到旧值；不顺手重构全局声音设置。调用现有 Setting.Save，失败说明未保存，当前值与原偏好草稿保持各自边界；不向 ProductPreferences JSON/存档/网络协议加入重复字段。

原设置页的字体/提醒仍保留草稿、取消和保存语义；独立声音页不以它们的取消按钮撤销即时音量。关闭视觉加工提醒仅影响视觉类别，关闭音效必须走明确声音开关，二者文案说明清楚。

### owner、状态与消息生命周期

```text
ProductRoot Fiber / SceneType.ProductRoot
  Component ProcessInnerSender / ProductRuntimeComponent
  Child ProductHost / SceneType.ProductHost
    HostSessionComponent + Ordered MailBox -> HostPeer + Ordered MailBox
    Match -> RestaurantRuntime -> GridKitchenComponent + Level（唯一权威）
  Child ProductClient / SceneType.ProductClient
    ClientSessionComponent + Ordered MailBox -> GridClientComponent（独立值）
    ProductFeedbackComponent（拟新增ModelView Component：已观察身份/序号/本案播放serial）
    GridPresentationComponent / UIComponent -> UIGridKitchen
    GFEntityComponent -> GridKitchenBoard（现有动画）
GF壳 GameEntry.Sound（实际播放/加载）与 GameEntry.Setting（本机声音偏好）
```

ProductFeedbackComponent由ProductClient管理，投影确认后安装基线，先于视图退出停止/注销本案播放，Client退役一并Dispose。它只存观察游标与资源句柄，不复制物品/订单世界；HUD关闭不影响业务也不重置游标。入口声音页只改GF本机偏好，不创建Host。

Host提交后 `ProductDelivery` 将独立DTO发送：本地从 LocalDelivery 获取 ClientSession.GetActorId，Root ProcessInnerSender.Send 入其Ordered MailBox；远端用RemoteDelivery.Session.Send，经ProductClientBinding进入同一ProductClient handler。原GridSnapshot、ProductCommandOutcome、KitchenWorkOutcome和LevelPhaseChanged处理器完成校验/安装后，反馈读取已提交的客户端值。没有新增请求、答复或联网消息；原等待者/DTO仍由现有handler和AsyncContext收尾，音频回调不持有消息对象。

Event仅通知已提交事实，使用明确ProductClient消费者，不新增SceneType.All；Invoke不用于音效裁决。观察包含world/binding/level/command或work身份，暂停遵守原业务状态；跨关持续厨房批次沿原世界/批次身份，不因换关重放。单人退出不停止其他客户端或共用sender。

## Risks / Trade-offs

- [重放/叠音] → 基线、语义身份去重和短音并发上限；不靠本机时钟推断业务完成。
- [技术播放通过但难听] → UTF看触发/静音/资源；真实扬声器或耳机试听另记。合成声可替换，混音审美需要人确认。
- [资源退出后播放] → owner生命周期、加载中退出测试，不用全局StopAllSound影响其他系统。

## Migration Plan

源音制作→Sound源注册和Check/生成→反馈观察适配→即时声音控制→真实动作/重连/退出与试听。若回退，同时撤回资源注册和显示接入并正常导表；业务/存档无需迁移。每个任务结果见 [execution-notes.md](execution-notes.md)。
