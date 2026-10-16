# Design

## Context

问题与范围见 [proposal.md](proposal.md)。本案需要先明确显示与真实操作、异步读取和当前 owner 的交接，因此保留设计。实际入口为 [SoloSavePresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)，相邻数据在原 Product Model／ModelView，现有 UI 由 `UIGridKitchen` 托管。

## Goals / Non-Goals

**Goals:** 提供记录详情与显式继续，展示章节／关序、结果类型、时间／修订／来源和已校验厨房摘要；按继续／完成分类及时间排序，刷新保留有效选择。

**Non-Goals:** 详情通过原只读 Select 路径完整校验后派生，不安装世界、不写记录；完成记录只查看。确认继续前重新读取并核对 Slot／revision 等本次选择事实；文件变化时更新预览并要求重新确认。 不新增业务权威、协议／文件格式或 UI 结构工程；完整范围外事项见 proposal。

## Decisions

### 实现方案

复用 SoloSaveSelectRequest 到 Root repository 的只读请求，响应转独立摘要，不在 UI 长期缓存整份载荷；最终继续重新校验当前文件。保持 v12 与旧有效格式适配，不增加 rename／delete／自动覆盖或额外索引文件。

保持 `Game.ET.Code.ModelView` 数据与 `Game.ET.Code.HotfixView` 逻辑分层。视图类型仍为1101，沿 `UIComponent.AddUIFormComponentAsync<UIGridKitchen>`；资源仍为 `Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab`。使用原控制／列表／详情／模板绑定，生成的 Bind partial 只读。新增有限页面或动作分类使用具名 enum，数量、ID 和版本保持数值语义。

### 实际 owner 树与寿命

```text
Fiber / Scene(ProductRoot)
  +-- Component: ProcessInnerSender
  +-- Component: ProductRuntimeComponent
  +-- Component: ProductEntryPresentation or SoloSavePresentation
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

只读使用 SoloSaveSelectRequest -> SoloSaveSelectHandler（ProductRoot）；确认继续重新读取并完整校验，匹配已展示身份后才 InitializeSavedProduct。读取与展示不发 WriteRequest，完成记录不会进入继续流程。

不涉及 repository 的页面切换只读当前配置／运行值，不为了展示初始化文件操作。请求适配器仍负责响应值复制与消息回收；只读查询答复仅说明读取结果，不能当作已开局、已保存或已领取。Event／Invoke 只沿原 SceneType 范围刷新观察，不新增 All 订阅，不把观察事件当请求成功。

### UI 协作与更新成本

只保留必要的页面、选择 ID、筛选及版本上下文；状态与权限每次从当前有效输入重验。与原菜谱／伙伴／队友详情共享区域时明确互斥，切回原目标仍可操作。补齐本案依赖字段比较，纯时间／位置更新不反复重建全部按钮，不额外逐帧复制／排序全状态。

预览和继续是两次有明确目的的读取，确认时复核 Slot／revision／ResultId 等完整选择身份；文件已变化则显示新摘要并要求重新确认，不引入 hash、锁定整文件或自动写回。

实施协调见 proposal 中的相关案。对共享文件顺序整合，保留各案实际 delta；其他案尚未实施时，使用现有入口完成本案独立验收，不添加虚假占位按钮。

## Risks / Trade-offs

- [把预览当成加载成功或对已变化文件继续使用旧确认] → 运行本案正常与变化场景，操作前复验当前身份；读页面不提前提交效果。
- [旧结果与界面池重用] → 复用原 EntityRef、world／Level／binding、epoch／serial guard，取消后只收尾原等待。
- [只用文本断言冒充可玩] → 真实临时文件预览／变化／损坏／取消与普通选档恢复；使用真实入口与有行为断言的相关 PlayMode UTF，逐功能结果见 execution-notes。
- [入口更易用尚无真人证据] → 功能结果只证明事实与可操作性，真人体验及设备差异保留明确未验证范围。

## Migration Plan

无需数据迁移。仅整合本案实际视图逻辑与相关测试，更新已有操作说明和当前进度；撤回入口即可恢复原导航，既有文件／世界规则不变。不在本案做提交、推送、归档或设备构建。
