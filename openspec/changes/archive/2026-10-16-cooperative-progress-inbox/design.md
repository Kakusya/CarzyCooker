# Design

## Context

问题与范围见 [proposal.md](proposal.md)。本案需要先明确显示与真实操作、异步读取和当前 owner 的交接，因此保留设计。实际入口为 [ProductEntryClaimsSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryClaimsSystem.cs)，相邻数据在原 Product Model／ModelView，现有 UI 由 `UIGridKitchen` 托管。

## Goals / Non-Goals

**Goals:** 在模式页、房间和结算入口显示已持久化本端可领数量，提供结果详情、状态分类和已领取记录去向；个人领取仍显式操作。

**Non-Goals:** 只统计 repository 实际可用凭证；网络收到但未持久化不能计为可领取。刷新不发 ACK／领取或关闭资格，状态未知／查询失败不显示零；不跨设备承诺唯一领取。 不新增业务权威、协议／文件格式或 UI 结构工程；完整范围外事项见 proposal。

## Decisions

### 实现方案

复用 Root 的 ProgressInboxChanged 事件及原 Browse repository 请求；对原 owner 做可失效的显示计数，订阅限 ProductRoot，不复制持久 inbox。打开详情读已有 ProgressTicketSummary 可表达字段，已领取通过 Slot／revision 导航到选档，不自动加载或领档。

保持 `Game.ET.Code.ModelView` 数据与 `Game.ET.Code.HotfixView` 逻辑分层。视图类型仍为1101，沿 `UIComponent.AddUIFormComponentAsync<UIGridKitchen>`；资源仍为 `Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab`。使用原控制／列表／详情／模板绑定，生成的 Bind partial 只读。新增有限页面或动作分类使用具名 enum，数量、ID 和版本保持数值语义。

### 实际 owner 树与寿命

```text
Fiber / Scene(ProductRoot)
  +-- Component: ProcessInnerSender
  +-- Component: ProductRuntimeComponent
  +-- Component: ProductEntryPresentation (Root navigation/count owner, retained)
  +-- Component: SoloSavePresentation (when original solo selector is requested)
  +-- Component: UIComponent
  |     +-- Component: UIGridKitchen -> existing GF Mono view
  +-- Component: SoloSaveRepository + Ordered MailBox (when required)
  +-- Child: Scene(ProductClient) (after join/start)
  |     +-- Component: ClientSessionComponent + Ordered MailBox
  |           +-- Component: ProductRoomClient / GridClientComponent
  +-- Child: Scene(ProductHost) (Host/Solo runtime initialization)
        +-- Component: HostSessionComponent + Ordered MailBox
        |     +-- Child: HostPeer + Ordered MailBox
        +-- Child: Match (room can have no restaurant)
              +-- Child: RestaurantRuntime (after actual restaurant start)
                    +-- Component: GridKitchenComponent
                    +-- Child: Level
```

树中的 Child／Component 表示释放归属，字段引用不改变 parent。Host 写 Restaurant／Match，repository 写文件，客户端仅持独立投影；本案只增加属于原 Presentation 的选择、摘要及显示依赖。不会为“统一入口”再建可写业务副本。

创建继续走原启动和 UI 加载链，身份及投影未就绪不开放业务输入；Close／Dispose／页面换轮先失效旧选择与回调，晚到加载按原 AsyncOwner／EntityRef／epoch 收尾。新关／新世界不继承旧操作上下文。单成员退出不停止 Root 共享 sender，整轮停止继续由原运行拆树。

### 发送、接收与结果

读取沿本端 Root.ProcessInnerSender.Call(repository.GetActorId(), request) -> SoloSaveRepository.Ordered MailBox -> SceneType.ProductRoot 的原 repository handler，答复回同一 Call；响应在所属适配器／using 中复制独立值并 Dispose。读取不创建 Host 世界。await 后核对原 EntityRef、页面 epoch／serial、选择与当前运行，取消只结束显示等待，原已投递请求仍由既有收尾处理。

Browse 使用 ProgressRepositoryRequest -> ProgressRepositoryHandler（ProductRoot）；刷新仅查询已持久记录。玩家确认领取才发同一 handler 的原 Claim 操作，发布后的 receipt 决定状态；原 ACK／关闭回执仍由原业务 owner 管理。

不涉及 repository 的页面切换只读当前配置／运行值，不为了展示初始化文件操作。请求适配器仍负责响应值复制与消息回收；只读查询答复仅说明读取结果，不能当作已开局、已保存或已领取。Event／Invoke 只沿原 SceneType 范围刷新观察，不新增 All 订阅，不把观察事件当请求成功。

### UI 协作与更新成本

只读文件前置条件：原ProgressClaimFiles.Browse内部的Ticket/Close校验会调用OwnIdentity初始化方法。存在凭证时先由OfflineProfiles.RequireExistingOwnIdentity只读确认原私钥、公钥和自身信任均存在且一致，缺失直接返回查询错误；空inbox不初始化身份。这样不靠后台查询补建身份或自动信任，也不改变显式初始化、签名、持久化与领取发布。由真实临时文件缺失密钥/公钥，以及真实KCP外来Host凭证下缺失Client自身信任两条行为验收；本次不实现身份修复工具。

既有选档交接：领取详情先经原SoloSaveBrowse请求定位Slot并显示当前header修订，不从ProgressTicketSummary猜revision。离线Solo且没有Client/Host世界时，“前往单机选档”才发布既有SoloSaveSelectionRequested本地事件，携带短寿命Slot/Revision；ProductRoot的SoloSaveSelectionView接收并复验空闲Solo，创建原SoloSavePresentation，原Browse核对修订后沿ChooseSoloSave/原SoloSaveSelectRequest做完整只读检查。Root sender取得当前repository.GetActorId()，经其Ordered MailBox到原ProductRoot handler，答复在原using中读取并释放；明确“确认继续此记录”才重新读取并创建餐厅。修订不符返回RecordChanged，旧页面epoch/serial不操作新页。该事件只用于本端UI导航，不序列化、不代表业务已提交，也不经过网络；合作房间保留原Progress记录选择页，不为跳转停止房间或世界。

待办计数在ProductEntryPresentation的独立所属scope内查询，只使用已经存在的repository；无上下文显示未查询，显式打开原领取页才沿其Profile初始化入口。Root事件只失效，查询合并读取。成功的领取页Browse也更新同一计数并退役较早的后台答复。GridPresentation只观察Root显示版本变化并重建既有按钮，依赖方向保持Grid→Entry，不由Entry反调GridSystem。错误答复先复制错误值再在页面任务中处理，避免原请求适配器内抛出预期文件错误并触发Console异常。单次取消仅使显示序号失效，不停止Root共用sender或owner。

只保留必要的页面、选择 ID、筛选及版本上下文；状态与权限每次从当前有效输入重验。与原菜谱／伙伴／队友详情共享区域时明确互斥，切回原目标仍可操作。补齐本案依赖字段比较，纯时间／位置更新不反复重建全部按钮，不额外逐帧复制／排序全状态。

待办计数属于本端 Root 已持久化结果的派生显示。原 ProgressInboxChangedView 从“仅领取页可见时标记刷新”扩展为当前 owner 失效标记；下一允许刷新合并查询，不在每帧或每个按钮无条件遍历文件。未知与零明确区分，旧查询不能覆盖新页。

实施协调见 proposal 中的相关案。对共享文件顺序整合，保留各案实际 delta；其他案尚未实施时，使用现有入口完成本案独立验收，不添加虚假占位按钮。

## Risks / Trade-offs

- [展示通知先于持久化使玩家误认资格，或根事件影响其他运行] → 运行本案正常与变化场景，操作前复验当前身份；读页面不提前提交效果。
- [旧结果与界面池重用] → 复用原 EntityRef、world／Level／binding、epoch／serial guard，取消后只收尾原等待。
- [只用文本断言冒充可玩] → 持久化前后、领取／已领／关闭、失败取消、重复打开和普通 KCP 结果入口；使用真实入口与有行为断言的相关 PlayMode UTF，逐功能结果见 execution-notes。
- [入口更易用尚无真人证据] → 功能结果只证明事实与可操作性，真人体验及设备差异保留明确未验证范围。

## Migration Plan

无需数据迁移。仅整合本案实际视图逻辑与相关测试，更新已有操作说明和当前进度；撤回入口即可恢复原导航，既有文件／世界规则不变。不在本案做提交、推送、归档或设备构建。
