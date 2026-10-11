# Proposal

## Why

当前产品会话已经支持本地与远端共用裁决，但还没有可操作的厨房地图。先把竖屏点格行走、相邻交接和断线落物做成样机，验证操作是否顺畅，再接完整烹饪。

## What Changes

- 新增固定示例地图：2D 场景与角色、正对地图边的 45° 俯视效果、正交显示；厨房一屏看全，前厅在下方，通过纵向滑动整张地图查看。地面常驻淡格线，点击高亮目标与路线。
- 新增四方向自动寻路；移动中改目标先完成当前一步。玩家可互相穿过，每格最多两人停驻；目的格满员时在最后一个可停驻格等待，空位出现后继续。
- 场景目标和 UI 目标都直接提交默认动作；远处台面自动接近至四邻格，自动转向后执行一次。首轮只做普通物品拿取、放置与交接，普通手槽和台面仍一格一物。
- 意外断线移除场上角色，手持物留在 Host 最后提交的所在格。落物不阻挡行走，可以同格存放多个独立物品；只能同格拾取，并选择具体物品。
- 新增 `solo` 启动：普通 Launcher 无显式请求时默认进入 Solo 地图，也支持显式 Solo。本地 Host 权威加本地客户端，不创建网络监听；显式 legacy／Host／Client 保持按请求启动。单机、Host 本地玩家和远端 Client 共用业务命令、固定 Tick、规则与独立投影，Host 唯一写入世界事实。
- **BREAKING**：扩展原型协议并提升协商版本，旧版本端不能混用；沿用 UDP-KCP 本机验证，不裁决正式传输。
- 实现时同步产品设计中旧的连续移动、不可重叠和断线手持物待裁决记录。
- 根据追加授权清理示例数据：公共 UIEntity／Scene／音频与 ET Unit／AI 表保留结构、清空演示记录，删除无消费者的 DTDemo／OneConfig 表及登录页测试 Widget；保留启动配置与本轮地图／HUD 注册，不恢复已删除的示例资源。

范围外：完整菜谱与加工、顾客订单、餐厅经营、伙伴、供应、摆放编辑、存档、身份恢复、暂停、公网／LAN、Host 迁移及手机 Player／AOT 验收。已确认的“点击开始手工作业，停止或移动中断并保留进度”记录为后续加工输入约束，本轮不实现加工系统。

## Capabilities

### New Capabilities

- `grid-kitchen`：格子地图、四方向移动与停驻容量、目标交互、物品位置、断线落物及竖屏地图操作。

### Modified Capabilities

- `runtime-instances`：启动配置新增不带 endpoint 的 `solo` 角色，普通 Launcher 默认 Solo，继续支持显式 legacy／Host／Client。
- `startup`：普通 Launcher 在 Loader 启动前建立默认 Solo 配置，直接加载地图和 HUD。
- `product-session`：增加 Solo 会话路径、固定 Tick 玩法命令与快照接入，明确成员与场上角色分别退出。

## Impact

- 稳定入口：`Unity/Assets/Scripts/Game/Procedure/ProcedureET.cs`、`Base/RuntimeInstance/`、实例启动窗口及产品启动桥；普通 Launcher 改为默认 Solo，不依赖窗口草稿。
- ET：`Code/Model/Share/Product/`、`Hotfix/Share/Product/` 增加地图数据与裁决；`ModelView/Client/`、`HotfixView/Client/` 增加投影视图与输入。沿用 ProductRoot／ProductHost／ProductClient、现有本地发送器和 MailBox，不另建业务架构或 RPC 等待表。
- 协议：在 `Design/Proto/ET-Client/` 追加地图消息源，经现有 Proto2CS 生成双端消息；不手改生成物。
- 资源与表：新增托管地图 UGFEntity、操作 ETUI 的 Prefab／CodeBind，修改 ET `UI.xlsx`／`Entity.xlsx` 源表并按现有 Luban 链路校验导出；地图组在实际 GameEntry 配置中注册。
- 产品文档：实现时同步 `Docs/Product/`、相关待办和当前进度；临时样机不冒充已选存档的 RestaurantRuntime。
- 采用局部 UTF 与一次竖屏／本机双端操作验证，复用已有会话和异步生命周期结果，不安排全量套件或包体构建。风险、取舍与未验证项见 [execution-notes.md](execution-notes.md)。
