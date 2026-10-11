# Tasks

本轮已获 apply 授权，另获清理无用 example 表及数据的授权。以下按 [design.md](design.md) 实施；每组补自身必要的小集和文档，收口复用结果，不重跑全量套件。源表／Prefab／生成时分别加载 Luban、UI、UGFEntity、Unity CLI 技能；执行 UTF 加载 auto-testing-sop，监听／多 Editor 前加载 port-management。

## 1. 临时地图权威与格子玩法

- [x] 1.1 在现有 Model／Hotfix Product 下增加 Match 的 GridKitchenComponent、固定布局、角色／台面／物品数据与固定 Tick；验收为地图 owner 树与设计一致，Match.Children 名单遍历继续只处理 Participant，不创建假存档或第二可写世界。
- [x] 1.2 实现四方向 BFS、逐格移动、当前一步结束后改目标、穿行／两人停驻／满格等待和自动接近转向；验收为相关核心用例覆盖绕障碍、改目标取消旧互动，以及第三人等待后恢复、穿过满格。
- [x] 1.3 实现普通手槽／台面拿放的完整预检、唯一物品位置，以及绑定角色退出落物、多物品独立同格拾取；验收为争抢只有一次提交、失败位置不变、断线使用最后提交格、邻格／满手拾取拒绝且重复退出不复制物品。
- [x] 1.4 在现有 ET UTF fixture 中落地上述必要核心用例，按 Tools/AutoTesting 发现实际 PlayMode FullName 并执行该小集一次；验收为真实终态及 Console 覆盖，无空集／跳过冒充通过，原输出留原位。
- [x] 1.5 同步 `Docs/Product/design.md`、`specs/kitchen-item-processing/spec.md` 及相关 `backlog.md`／规格索引的移动、占格、交互、断线裁决，记录持续手工加工输入仍待实现；验收为旧冲突已明确替换，仅删除已裁决待办，其他未决条目保留。

## 2. Solo 与同一套本地／联网接入

- [x] 2.1 追加 `ZZGridKitchen.proto`、提升协商版本并通过现有 Proto2CS 生成双端消息；验收为新增命令／快照／结果字段齐全、旧 opcode 未漂移、未知／内部消息不能经 wire 白名单进入，生成文件不手改。
- [x] 2.2 接入 HostPeer→HostSession 快速意图接收、Tick 再校验、业务编号去重及各端独立投影；验收为调用只返回接收状态、自动接近不占住公共入口，最终结果与快照按世界／版本／绑定安装，广播无共享池化消息。
- [x] 2.3 增加稳定配置／窗口的 solo 和 SoloReady，拆分可选监听与共用权威初始化；接入加入／离线／Leave 的角色生命周期记录及整轮 Tick 收尾。验收为 Solo 不创建 NetComponent 监听，Host 仍报告实际 loopback endpoint，旧绑定不执行，单成员退出不停止 Sender；普通 Launcher 的默认入口按下述第 6 组修正。
- [x] 2.4 增加最小共同入口 UTF，涵盖 Solo、本地与远端同类动作、重复编号、错误身份／版本、旧投影及退出后队友继续交接；发现真实 PlayMode FullName 后只执行这组新增用例一次，复用第 1 组结果。验收为两条传输入口得到同一裁决、无新增私有 RPC 等待表，实际计数与终态留在执行说明。
- [x] 2.5 更新 `Docs/Product/architecture.md`／相关 cooperative-match 目标规格及 `long-term-goals.md` 的断线索引，补充适用启动 Book 页的 Solo 操作；验收为图和消息两端对应实现，明确离线 Participant 与移除角色、样机与正式餐厅的差别，不删除暂停／正式传输未决事项。

## 3. 托管 2D 地图与竖屏操作

- [x] 3.1 在 ModelView／HotfixView 增加 ProductClient 的 GridPresentation、托管 GridKitchenBoard 和 UIGridKitchen；通过 Unity CLI／Pipeline 创建 Prefab、CodeBind 和 GameEntry 地图 Entity 组。验收为类型与 serial ID 分离，UI／Entity 按现有 owner 唯一 Component API 打开，加载未完成退出不会绑定旧轮。
- [x] 3.2 在 ET 的 UI.xlsx／Entity.xlsx 源表追加注册行；按实际引用清理已无资源的旧 GF 示例数据，保留公共 UI／Entity／Scene／音频表结构及 ET 登录／启动配置，删除无消费者的 DTDemo 注册及源表。使用现有标准库保留工作簿其他结构，回读后执行既有 Luban Check、导出、ID 派生并刷新实际 ResourceCollection。验收为行／AssetName／分组和输出 diff 对应，删除限于授权示例且无未知删除，失败后不继续使用生成物；不手改 JSON、ID、bytes 或 XML 产物。
- [x] 3.3 实现统一 45° 地面投影、正交取景、淡格线／目标路径、厨房一屏和整体纵向拖动、角色插值、场景与 UI 默认动作及同格落物选择；验收为一次竖屏操作检查各项，拖动松手与 UI 点击不穿透触发走格，退出恢复有效原相机且不改无关 URP Asset。
- [x] 3.4 记录视图就绪／失败、实际资源和操作边界到 `CarzyCooker当前进度.md`，核对 Prefab／CodeBind／表／引用消费者的一致性；验收为会话 Ready 与地图 GridReady 分开，不以截图、资源刷新或编译冒充手机 Player 验收。

## 4. 最小贯通与交付记录

- [x] 4.1 通过既有入口完成一次本机 Host／Client 竖屏贯通：远处目标自动接近、两种入口交接、客户端断线、队友到同格选择拾物及整轮退出。验收为两端相同世界版本／物品身份、玩法继续可用且退出无旧轮回写；原始运行日志保留原位。第二工作区只按明确已提交 main 清单维护，若同步授权未给出则如实记缺口，不擅自提交或复制未提交代码。
- [x] 4.2 收集实际编译、生成、局部 UTF 和贯通结果，更新 execution-notes 的计数／耗时／原输出位置及 NotRun，执行本 change 严格校验与差异自审；验收为任务与规格对应，复用已有有效结果，无全量重跑、额外证据包或遗漏的未验证项。手机 Player／AOT／LAN／公网明确 NotRun。

## Workflow follow-up

## 5. 追加清理未使用的示例业务数据

- [x] 5.1 清空 UIEntity 的 WidgetTest、UnitConfig 四条演示单位和 AIConfig 三条攻击／巡逻数据，保留公共类型；删除无消费者的 OneConfig 注册与源表。回读工作簿并经五个目标 Check／导出生成全部派生结果，确认 Entity 仅地图、Scene／音频／UIEntity／Unit／AI 均为空，启动表未改变。
- [x] 5.2 去掉登录页动态测试 Widget 加载，以 Editor 清除嵌入测试节点并重新生成 CodeBind，移除 WidgetTest 独立资源与示例脚本，刷新 ResourceCollection；完成编译和普通 Launcher／Solo 启动小检查，保留有效登录与地图注册，复用既有玩法测试。
- [x] 5.3 同步当前进度、生成约定与执行说明，记录实际剩余注册、旧 Demo 地图退出当前可用范围和本轮验证结果，严格校验与差异自审；不提交、归档或同步第二工作区。

## 6. 修正用户直接启动的交付入口

- [x] 6.1 普通 Launcher 无显式请求时，在 ProcedureET 启动 Loader 前建立本轮默认 Solo 配置；显式 legacy／solo／host／client 继续使用原请求，失败不回退。同步 design、启动与实例 delta、快速开始及当前进度，明确旧的“普通 Launcher 进入示例”验收已被用户最新反馈替换。
- [x] 6.2 实际打开 Launcher.unity，直接进入 Play，不提交 Solo 请求、不用 eval 创建产品树或打开 UI；检查真实 Board／HUD 显示并截图，通过场景点格和 HUD 按钮验证移动与拿放，停止后再次直接运行确认入口仍有效。记录操作路径、实际画面、Console 与剩余限制；不以任务勾选或历史显式启动代替该验收。

- 实施完成后再按用户授权进入 verify／sync／archive；提交、push、PR 和归档不由本计划自动授权。
