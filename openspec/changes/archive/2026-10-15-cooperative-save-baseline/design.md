# Design

## Context

前案已交付普通模式菜单、无餐厅Match和合作三关终点；原Solo v1–v4载荷和SelectedSaveBaseline恢复仍可用，但原文件仓库只服务本机Solo。本案接入已确认的[离线协议](../../../../Docs/Product/offline-progress-protocol.md)，分离世界Host、文件Owner和已证明Participant，不开放无授权写入。动机见[proposal.md](proposal.md)。

## Goals / Non-Goals

PC房间可浏览本机成功记录或主动导入Owner授权，校验后建立唯一新餐厅，保持真实库存/批次/升级/运输与新运行身份。复用当前存档载荷及ET/UGF owner；不引入账号中心、云服务、远程撤销、Host迁移、领取/替换或任意断电保证。其他平台不从PC文件取得权限。

## Decisions

### 本端身份、公钥绑定及完整授权

复用ProductRoot的SoloSaveRepository本端有序入口，私钥只存其Identity目录，公钥可显式导出。首次导入先显示签发者、Profile/Key、来源和授权目标，再明确绑定公钥；已有Profile绑定不同Key/公钥拒绝，验证自签名不会自动建立信任。当前Unity Mono实测RSA.Create后设置KeySize仍为1024位，故创建使用指定2048位的RSACryptoServiceProvider且不持久化到CSP容器；解析按实际公钥模数校验，签名为运行时PKCS#1/SHA-256，不增加密码学依赖或文件hash证据工具。

签名封套固定版本/PC/用途，覆盖完整身份、公钥和业务体，先限制长度与版本再验签。ProfileProof与ReadRun+MatchAuthority用途分开。Owner为已导入绑定的Host公钥及本次Match签发完整基线和有限结果委托，仍没有ImportBranch或覆盖权。旧v1–v4首次进入授权链以当前合法源记录为可验证根锚，原SourceSlot和文件不改写。后续领取/导入须延续该锚。

### 实际ET owner树

```text
Product Fiber / SceneType.ProductRoot
  Components: ProductRuntime / Timer / CoroutineLock / ProcessInnerSender
  Component: SoloSaveRepository + Ordered MailBox
    本端文件: Solo成功记录 / Identity私钥 / Trusted公钥 / Exports
  Component: ProductEntryPresentation + UIComponent (只持浏览值/epoch/操作序号)
  Child Scene: ProductHost / SceneType.ProductHost
    Component: HostSession + Ordered MailBox
      Child: HostPeer + Ordered MailBox
        Generation / 一次性ProfileChallenge / 本绑定ProfileProved / 最近命令签名
        LocalDelivery or RemoteDelivery -> Session
    Child: Match (RoomVersion)
      Child: Participant (连接独立ProfileId/KeyId，恢复不换Profile)
      Component: CooperativeSaveBinding -> SelectedSaveBaseline引用/只读授权链
      Child: SelectedSaveBaseline (独立Header/Payload值，复用原类型)
      Child: RestaurantRuntime
        Component: GridKitchen -> Station / Item / Process / KitchenSupply
        Component: RestaurantRenovation
        Child: Level -> 新角色/顾客/订单/工作/Buff
    Component: NetComponent (既有实际KCP监听)
  Child Scene: ProductClient / SceneType.ProductClient
    Component: ClientSession + Ordered MailBox
      Components: ProductRoomClient (独立身份/基线概要) / GridClient (独立投影)
    Components: UIComponent / GFEntityComponent / GridPresentation (确认世界后建立)
```

仓库寿命长于一次餐厅；Match只持运行副本和授权值，不接收源路径、句柄或客户端快照倒灌。加载使用CooperativeSaveBinding引用原SelectedSaveBaseline，GridSaveRestore择取Solo或合作基线后走同一重建；不把SoloSaveBinding的个人写档资格直接给Host。

### 消息发送、校验、答复及观察

Profile挑战/证明和CooperativeBaseline是两个显式公开请求族。本地Client通过真实LocalMessage/HostPeer ActorId，经Root ProcessInnerSender/MessageQueue到ProductHost Peer Ordered MailBox，再以内部Submit到HostSession Ordered MailBox；远端经原Session/ProductWire白名单复制值并取得可信Peer，进入相同裁决。本地Host不对自己发UDP；内部Submit、文件读写和签名请求不进wire白名单。

HostSession发本Match/绑定随机挑战，Client经本端repository的ProductProfileSign有序消息签名，公开Proof返回同一HostSession核验可信公钥/挑战/绑定后才提交Profile身份。Participant保存逻辑Profile，ProfileProved归当前HostPeer；真实Resume保留成员但新Peer未证明，必须新挑战，旧Proof不能复用。不能用恢复凭证自动赋予文件或后续领取资格。

浏览/选档/查看导入/明确公钥绑定/授权导出由UI经本端ProcessInnerSender.Call(repository ActorId)到SceneType.ProductRoot handler，文件操作结束前关闭本端流；只返回独立值。Host公开加载请求携完整签名值、Match/绑定/CommandId/RoomVersion，无World/Level假身份。权威校验可信本地Host、其当前已证明Profile、签发者绑定、用途/目标、源Header和完整Payload后创建。记录最近签名与房间版本支持同一命令重放一次，冲突或旧上下文拒绝。

创建先安装Match的基线值，按已校验NextLevel配置重建新Restaurant/厨房，再提交Match.Restaurant、递增RoomVersion并答复Executed；异常释放部分餐厅及基线，保留房间和源文件。各在线成员走原新世界队列，RoomSnapshot只给独立公开身份/Owner/Slot/revision概要，Grid快照与本人角色确认后才开放输入。状态提交后发布观察事件，不由Event裁决状态。

### 取消、退役与释放

UI复用原UIGridKitchen，动态输入框接收文件路径但不传wire；其创建、隐藏、字体和释放沿原Mono owner，无Prefab/CodeBind结构变更。跨await校验Root/UI epoch、Client实例/绑定及操作序号。读取期间取消只使旧答复失效；原物理请求仍由既有拥有者回收。文件值校验完准备提交世界时隐藏读取取消，避免声称撤销已提交事实。预期业务拒绝作为拥有者调用的值返回，在外层更新提示，不在底层read回调抛异常触发全局Error。

关闭餐厅先退役原树，再由CooperativeSaveBindingSystem清理基线及Selected子对象，保留Match/Participant/Session/Root sender；新开内存餐厅不会复用旧载荷或叠加库存。绑定清理独立于广播，保持静态调用图单向。退出整个运行才结束根及原请求；Dispose不临时签发成功或写档。

## Risks / Trade-offs

错误后果主要是越权加载、旧文件改变、库存重复或旧异步答复创建世界。用隔离真实文件、实际RSA/KCP、现行与实际旧格式、冻结半批次/运输/升级及普通UI取消/拒绝/分工覆盖；复用未改的原恢复算法证据，不做全仓穷举。实际结果和失败修复逐项见[execution-notes.md](execution-notes.md)。无设备/物理LAN/Player/AOT/任意断电验收。

## Migration Plan

wire从24到25；追加ProductProgress Proto及RoomSnapshot末尾字段，经既有Proto2CS生成，旧Opcode不重排。旧Solo文件格式和读取规则保持v1–v4；本案不写个人合作结果，不提升为已支持领取/覆盖。无Excel改动，不为纯代码导表；没有新服务、端口工具或依赖。未提交、push、归档或操作第二工作区。

## Sources

- [签名与本端身份](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/OfflineProfiles.cs)、[基线裁决](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/CooperativeBaselineSystem.cs)。
- [Peer/HostSession和repository handler](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductProgressMessages.cs)。
- [受管入口](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryProgressSystem.cs)、[共用恢复](../../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSaveRestore.cs)。
- [离线协议](../../../../Docs/Product/offline-progress-protocol.md)、[逐功能证据](execution-notes.md)。
