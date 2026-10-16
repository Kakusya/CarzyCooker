# Design

## Context

问题与范围见 [proposal.md](proposal.md)。本案需要先明确显示与真实操作、异步读取和当前 owner 的交接，因此保留设计。实际入口为 [ProductEntryPresentationSystem.cs](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryPresentationSystem.cs)，相邻数据在原 Product Model／ModelView，现有 UI 由 `UIGridKitchen` 托管。

## Goals / Non-Goals

**Goals:** 按创建／加入阶段显示合法地址示例、当前实际监听地址、复制入口和可理解的连接／取消／重试说明；把回环地址的适用范围说明白。

**Non-Goals:** 只复制公开 endpoint；不展示凭证／密钥，不自动连网测试或扫描网卡，不改变现有显式 LAN 授权与地址校验。错误仅按实际校验／握手结果说明，不能猜测防火墙或声称公网可用。 不新增业务权威、协议／文件格式或 UI 结构工程；完整范围外事项见 proposal。

## Decisions

### 实现方案

沿 RuntimeInstanceConfig 和当前 ListenEndpoint，视图调用现有复制缓冲 API；保留 ChooseCooperativeEntry／原 Join 和取消路径。输入失败停在原页并保留可编辑文本，重试创建新所属连接；不新增发现协议、监听或网络白名单。

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

本案切页与配置预览读取当前 Root／Client 的运行值及可信定义，不发 repository 或玩法请求。只有玩家进入原选档／授权等功能后才沿那些功能原有的读取路径；页面导航本身不初始化文件操作。

复制与填写不发送消息；确认加入才调用原 ChooseCooperativeEntry／JoinProduct，用新所属 Session 与原 Join 控制链；取消／重试沿原 runtime 切换，旧 Session 与旧尝试不能更新新页。

不涉及 repository 的页面切换只读当前配置／运行值，不为了展示初始化文件操作。请求适配器仍负责响应值复制与消息回收；只读查询答复仅说明读取结果，不能当作已开局、已保存或已领取。Event／Invoke 只沿原 SceneType 范围刷新观察，不新增 All 订阅，不把观察事件当请求成功。

### UI 协作与更新成本

只保留必要的页面、选择 ID、筛选及版本上下文；状态与权限每次从当前有效输入重验。与原菜谱／伙伴／队友详情共享区域时明确互斥，切回原目标仍可操作。补齐本案依赖字段比较，纯时间／位置更新不反复重建全部按钮，不额外逐帧复制／排序全状态。



实施协调见 proposal 中的相关案。对共享文件顺序整合，保留各案实际 delta；其他案尚未实施时，使用现有入口完成本案独立验收，不添加虚假占位按钮。

## Risks / Trade-offs

- [把仅适用回环的 endpoint 宣称为可跨机使用] → 运行本案正常与变化场景，操作前复验当前身份；读页面不提前提交效果。
- [旧结果与界面池重用] → 复用原 EntityRef、world／Level／binding、epoch／serial guard，取消后只收尾原等待。
- [只用文本断言冒充可玩] → 有效／无效输入、复制、真实 KCP 加入失败与取消重试；使用真实入口与有行为断言的相关 PlayMode UTF，逐功能结果见 execution-notes。
- [入口更易用尚无真人证据] → 功能结果只证明事实与可操作性，真人体验及设备差异保留明确未验证范围。

## Migration Plan

无需数据迁移。仅整合本案实际视图逻辑与相关测试，更新已有操作说明和当前进度；撤回入口即可恢复原导航，既有文件／世界规则不变。不在本案做提交、推送、归档或设备构建。
