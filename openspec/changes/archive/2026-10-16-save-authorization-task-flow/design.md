# Design

## Context

问题与范围见 [proposal.md](proposal.md)。本案需要先明确显示与真实操作、异步读取和当前 owner 的交接，因此保留设计。实际入口为 [ProductEntryProgressSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryProgressSystem.cs)，相邻数据在原 Product Model／ModelView，现有 UI 由 `UIGridKitchen` 托管。

## Goals / Non-Goals

**Goals:** 按我想继续他人餐厅、导入独立进度、导出给朋友和替换我的记录组织流程，逐步说明来源／目标／用途／结果，并提供返回和可取消步骤。

**Non-Goals:** ReadRun、ImportBranch、Replace 三种权限不能互换；信任公钥、导入和有损替换保留既有独立明确确认。查看、返回与切换任务不写档、不自动信任；提交阶段不伪称可取消已落盘结果。 不新增业务权威、协议／文件格式或 UI 结构工程；完整范围外事项见 proposal。

## Decisions

### 实现方案

复用 ProductEntryPresentation 中 Progress／ConfirmImport／ReplacePreview 状态及原 repository 操作，增加具名视图任务枚举，不重写签发／验证／原子发布。显示用户易读术语及可展开身份明细，不暴露私钥／凭证载荷，文件路径仍由玩家主动提供。

保持 `Game.ET.Code.ModelView` 数据与 `Game.ET.Code.HotfixView` 逻辑分层。视图类型仍为1101，沿 `UIComponent.AddUIFormComponentAsync<UIGridKitchen>`；资源仍为 `Assets/Res/UI/UIForm/Product/UIGridKitchen.prefab`。使用原控制／列表／详情／模板绑定，生成的 Bind partial 只读。新增有限页面或动作分类使用具名 enum，数量、ID 和版本保持数值语义。

### 实际 owner 树与寿命

```text
Fiber / Scene(ProductRoot)
  +-- Component: ProcessInnerSender
  +-- Component: ProductRuntimeComponent
  +-- Component: ProductEntryPresentation (retained entry/claims owner)
  +-- Component: SoloSavePresentation (only while original Solo selector is open)
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

查询／检查继续用原 ProgressFileRequest 对应 handler；明确导入或替换才发送 SaveAuthorizationRequest -> SaveAuthorizationHandler（ProductRoot）-> 既有文件验证／发布，答复复制合法 receipt 后才显示提交。流程页面不新增写权限，也不改变替换用途与修订验证。

`AuthorizationViewTask` 和 `AuthorizationExportPurpose` 仅选择本端入口。切换任务先失效 ProgressSerial、清理原按钮与路径，再撤下上轮检查/目标；显示的检查路径锚定实际来源，路径修改后必须重查。只读查询可取消显示等待，信任、签发、导入和替换提交期间不提供取消。最近一次原 receipt 的 Slot/Profile/revision 仅用于结果显示，不能重新裁决资格。

导入按“检查文件→用途核对→明确公钥信任→独立导入确认→实际新记录”交接；成功后可查看原记录页，再明确跳转原 Solo 选档检查，仍须原确认才开局。替换按原可领取结果→原目标修订预览→单独有损确认，不提供新档领取捷径。导出先选三种用途，再读本机记录；只读/替换许可继续核对接收者公共身份。高级入口保留原身份验证及文件操作。

ReadRun 的本地房主先经原 ClientSession/HostPeer 控制链证明 Profile，再把原授权文件交给现有房间裁决；Host 的 SceneType.ProductHost 有序入口验证当前 Match/Host 后创建只读来源世界并通过原房间投影返回。远端 KCP Client 经过其 Session/网络桥进入同一控制裁决，本页不增加载入权限；Client 可检查文件但不能用本地 UI 成为房主。没有把 Event 当授权或提交答复。

不涉及 repository 的页面切换只读当前配置／运行值，不为了展示初始化文件操作。请求适配器仍负责响应值复制与消息回收；只读查询答复仅说明读取结果，不能当作已开局、已保存或已领取。Event／Invoke 只沿原 SceneType 范围刷新观察，不新增 All 订阅，不把观察事件当请求成功。

### UI 协作与更新成本

只保留必要的页面、选择 ID、筛选及版本上下文；状态与权限每次从当前有效输入重验。与原菜谱／伙伴／队友详情共享区域时明确互斥，切回原目标仍可操作。补齐本案依赖字段比较，纯时间／位置更新不反复重建全部按钮，不额外逐帧复制／排序全状态。

540×960 下完整来源标识按字段分行，不用一个长行压缩全部正文。身份明细使用原详情区域，完整路径留在原可编辑路径框，明细/导出结果显示文件名；不新增 Prefab、CodeBind 或第二套布局组件。



实施协调见 proposal 中的相关案。对共享文件顺序整合，保留各案实际 delta；其他案尚未实施时，使用现有入口完成本案独立验收，不添加虚假占位按钮。

## Risks / Trade-offs

- [简化流程时合并了原来独立的信任与有损确认] → 运行本案正常与变化场景，操作前复验当前身份；读页面不提前提交效果。
- [旧结果与界面池重用] → 复用原 EntityRef、world／Level／binding、epoch／serial guard，取消后只收尾原等待。
- [只用文本断言冒充可玩] → 各用途导航与原实际文件链、取消／失败／修订冲突及源档保留；使用真实入口与有行为断言的相关 PlayMode UTF，逐功能结果见 execution-notes。
- [入口更易用尚无真人证据] → 功能结果只证明事实与可操作性，真人体验及设备差异保留明确未验证范围。

## Migration Plan

无需数据迁移。仅整合本案实际视图逻辑与相关测试，更新已有操作说明和当前进度；撤回入口即可恢复原导航，既有文件／世界规则不变。不在本案做提交、推送、归档或设备构建。
