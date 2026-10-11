# 执行说明

## 本轮状态

2026-10-11：用户明确“开始apply”，随后两次授权按实际用途清理 example 表及数据，又要求继续完成 change。地图权威、共同消息／投影、无监听 Solo、托管 2D Board／HUD、源表／资源注册、示例清理和最小贯通已实施；原地图任务 16 项，追加清理 3 项。Luban 五个目标 Check、导出及 ID／本地化生成通过，Unity 最终完整编译通过。verify 发现行走成功时机与 HUD 文案不一致，已统一为到达才完成、等待保持进行中、替换取消旧行走；本次 Core 4/4 Passed，复用原 Wire 3/3，七项各有 Passed 证据。540×960 竖屏 Solo 和一次真实 KCP 贯通通过；Host／Client 使用同一 Editor 内独立 Fiber 与独立客户端投影，依次查看两端视图，未声称本轮双 Editor 或手机 Player 已验收。追加清理后的普通 Launcher 登录页与 Solo 地图启动也通过，见文末记录。

用户随后指出普通 Launcher 仍进入示例。上述登录页和显式 Solo 的历史验证不能证明直接 Play 的产品体验；此前交付表述过宽。已追加默认入口修正两项任务，当前共 21 项，普通 Launcher 默认 Solo，真实直接 Play、竖屏 Board／HUD 和移动／拿放检查通过，具体结果见文末；烹饪与订单仍未实施。

仓库原有 `UniversalRenderPipelineAsset.asset` 修改保留。实施阶段未提交、push、PR、归档或同步第二工作区；后续用户明确授权归档与 commit，收口见文末。没有把未提交代码复制给第二工作区。

## 严谨程度与验证取舍

主要风险是多人拿取重复物品、移动／停驻规则互相冲突，以及旧绑定或晚到加载写入新一轮；后果是可定位、可重启样机恢复的玩法错误。采用 Host 单写者、完整预检后提交和原 owner 生命周期，针对这些风险补少量 UTF；一次竖屏操作及本机双端交接验证输入、视觉和消息链路即可。不建立风险评分、穷举交通矩阵、额外测试平台或统计性能门槛。

复用已归档的产品会话与异步生命周期验证，仅把新玩法贯通作为增量检查。源工作簿涉及文件覆盖，另核对未被占用、指定注册和授权示例行、保留其余 ZIP 包项并回读，导出检查失败就停止，不套用“样机可重启”来降低文件保护。当前接受双 Editor 新玩法、公网、LAN、真实手机触控、Player／AOT、长期运行、极端交通拥堵和恶意超大请求的穷尽验证未覆盖；出现真实卡路、物品重复／丢失、旧轮回写或明显卡顿时，再针对具体疑点补验。

追加清理的实际风险是删除消费者仍需的类型、登录 Prefab 留下空绑定，以及导出目录或源文件发生越界删除。保留公共类型，只在无手写消费者时整表删除；源包项回读、编译和各一次普通 Launcher／Solo 启动足以覆盖本次风险。地图规则和消息未改变，复用六项 UTF 与已有 KCP 贯通，不重复运行。接受旧 Demo 单位／AI 演示随数据移除而不可用；出现实际启动加载失败、丢绑定或新业务访问空配置时，再针对具体入口补验。独立 DotNet 编译与 Player 构建本次 NotRun。

## 决策账本

| 主题 | 用户确认的行为 | 本轮落点 |
| --- | --- | --- |
| 画面 | 手机竖屏；2D 场景与角色；俯视 45°、水平方向正对地图边；正交显示 | 简单精灵占位，不引入 3D 角色或菱形地图 |
| 地图 | 厨房一屏看全；餐厅在下方，可滑动查看；整张地图纵向滚动 | 固定示例布局，前厅只提供空间与视图占位 |
| 可读性 | 常驻淡格线，点击高亮目标与路径 | 世界点击和 UI 使用同一格坐标／目标 ID |
| 移动 | 上下左右；点击远格自动寻路；走完当前一格再替换目标 | Host 固定 Tick 推进，显示端只插值 |
| 玩家占格 | 允许互相穿过，每格最多两人站立；满格在最后可停驻格等待，空出后继续 | 站立容量不等于总联机人数，不把经过角色作为静态墙 |
| 交互入口 | 可点击场景目标，也可在 UI 选择对应目标；两种入口直接执行默认动作 | 都产生相同意图，不增加第二次确认按钮 |
| 远处目标 | 自动走到可达四邻格后执行；点击时自动转向目标 | 同一次意图只执行一次，执行前重新检查 |
| 手工作业 | 点击开始；停止或移动时中断，保留进度 | 后续加工输入约束，本轮只拿取／放置 |
| 意外断线 | 移除角色，手持物原格留下可拾取物品 | 使用 Host 已提交格，保留离线 Participant，不承诺重连 |
| 地面物品 | 不挡路；同格允许多个独立物品，选择具体一个；仅同格拾取 | ItemId 不合堆；邻格不能拿地面物品 |
| 权威与复用 | Host 唯一权威；单机和联网尽可能复用 | Solo 是无监听的本地 Host＋本地 Client，规则／Tick／投影相同 |

## 调查依据与冲突

- [产品会话主规格](../../../specs/product-session/spec.md)及 `ProductRuntimeBootstrap.cs`：当前 Host 无条件创建 UDP 监听，没有 Solo 分支；本地玩家已经经 ET 本地消息到 HostPeer 和 HostSession 有序入口。
- `ProductAuthority.FillJoin`／`Changed` 对 Match 的 Children 使用 `Cast<Participant>()`。地图选用 Match 的 Component，地图实体放在该 Component 下，避免破坏名单遍历。
- [产品目标树](../../../../Docs/Product/architecture.md)：正式餐厅要求选中并校验 LoadedSave。首轮采用明确的临时样机组件，不创建假存档／RestaurantRuntime／Level，不写持久进度。
- [产品设计](../../../../Docs/Product/design.md)中的连续移动和禁止玩家重叠、[产品待办](../../../../Docs/Product/backlog.md)中的断线物品冲突，与本次裁决不同。用户已知晓差异；实现时同步正式设计、对应目标规格和待办，只删除实际已裁决条目。
- `UI.xlsx` 表头含 Id／CSName／AssetName／UIGroupName／AllowMultiInstance／PauseCoveredUIForm；`Entity.xlsx` 含 Id／CSName／AssetName／EntityGroupName／Priority。CSName 属 ge 组；ID 来自公共 Editor JSON 派生，不能手改。现有 UI 有 Default 组；Entity 无默认地图组，需要显式注册地图组。
- 当前 ET `luban.conf` 为 active，输出 gameclient／gameeditor／client／clientserver／editor；非 Check 导出有目标清理与复制。原始工作簿、资源引用和输出 diff 必须检查，不用旧产物继续。
- Proto2CS 按文件路径 Ordinal 排序。新增末尾 `ZZGridKitchen.proto`，不在现有消息序列中插入；仍须核对生成 diff 和协议版本，不能仅凭命名推断兼容。
- 环境没有 openpyxl；规划使用现有 Python 标准库 XML／ZIP 针对两个源工作簿追加注册行，不安装依赖或新增专用工具。未来实施时仍核对原表结构与占用。
- 现有 ET Tests fixture 可建立专属 ProductRoot，手动推进时间与 Fiber；不需要新测试框架。历史证据见 [会话执行记录](../2026-10-11-product-session-bootstrap/execution-notes.md)及 [异步生命周期执行记录](../2026-10-11-manage-owned-async-lifecycle/execution-notes.md)，只复用其实际验证范围。

## 工程默认值与未决范围

当前样机参数：7×18，KitchenBottom=6，固定 Tick 50ms、每步 5 Tick、停驻容量 2、拖动阈值 14px；这些与美术占位色属于可调整参数，不作为最终平衡或总人数承诺。首轮不做预测回滚；客户端可以立刻显示点击目标，实际位置与拿放结果由 Host 快照确认。

单个角色主动 Leave 沿用同一物品保留收尾，避免旧接口退出时吞掉手持物；整体结束直接释放临时世界，不生成存档。重复传输只防止重复执行同一业务命令，不新增自动重试／离线恢复。暂停队列、正式传输、存档与完整加工仍未裁决或未实施。

## 验证记录

| 检查 | 本轮状态 | 实际范围／限制 |
| --- | --- | --- |
| OpenSpec 严格校验、差异自审 | Passed（接受 Unity 序列化尾空格） | `openspec validate portrait-grid-kitchen-prototype --strict`；普通 `git diff --check` 仅报告新资源规则三个空字段自动生成的尾空格，接受现有 Unity 序列化风格，`git -c core.whitespace=-blank-at-eol diff --check` 通过；地图 16 项及追加清理 3 项，未归档 |
| 最终完整 Unity 编译 | Passed | `recompile_status` completed、failed=false；Console groundTruth compilationFailed=false、compiling=false、Error=0，包含完整视图、owned 请求适配和 Solo 请求交接修复 |
| 核心 UTF | Passed，4/4 | `ET.ProductSessionTests.GridCore*`：路径／改目标、两人停驻与穿行／等待恢复、争抢／断线提交格／同格拾取与容器身份；verify 修复后追加行走到达／替换结果断言，真实终态 4 Passed、0 Failed、0 Skipped，UTF duration=0.17s |
| 共同入口 UTF | Passed，3/3 | `ET.ProductSessionTests.GridWire*`：Solo 无监听、真实 KCP、本地／远端拿放、重复编号／载荷冲突、旧绑定／版本／世界、投影独立复制／旧包、内部 wire 拒绝、多落物选择／满手拒绝及队友继续；UTF duration=0.29s，不含工具发现和 Play 切换 |
| Proto2CS | Passed | 既有 Tool 生成 Client／ClientServer；原 opcode 10001–10061 未漂移，新增 10062–10071；协商版本 2，旧版明确拒绝 |
| 源表注册／Prefab／CodeBind | Passed | UI Id=1101，Entity Id=80000；其余 ZIP 项字节保持不变，回读通过。Prefab 经 Pipeline 创建和序列化绑定，真实两端 GridReady；GameEntry 只追加 GridKitchen 组 |
| Luban Check／导出 | Passed | 清理后五个目标均通过 Check，现有 Tool 导出、UGF ID 派生、ET Client／ClientServer／Config 复制及本地化均有成功日志；首次失败保持原记录，未关闭校验 |
| ResourceCollection | Passed（集合范围） | 删除 Hot 失效规则，修正 LockStep 目录，追加 Product UI；现有 With Optimize 保留 Launcher 与共享字体。追加清理后相对 HEAD 移除 98 条引用（首轮 97 条加 WidgetTest），被移出引用没有仍存在的源资产；ET.Code／HybridCLR 源规则保留，空目录无 DLL 输入，本轮不生成包体 |
| 视图输入与收尾 | Passed（Editor） | 540×960 正交、地面投影反算、厨房全景、整图纵向平移、拖动和 UI 命中不提交走格；Scene 点击拿碗、UI 放置同一碗，整体 Stop 关闭本轮展示、恢复原相机并禁用 Board 相机。复用所属加载取消基础，未补穷举迟到资源排列 |
| 本机 KCP 双端玩法 | Passed（单 Editor、独立 Fiber） | Host 本地 Scene 拿取／UI 放交接台，远端 Scene 接近拿取；世界及版本一致。断线角色移除、Participant 保留离线，Host 同格 UI 选物并继续放置；碗 ItemId 和内容始终保持，整轮 Stop 与临时 Client Fiber 释放通过 |
| 双 Editor 新玩法／手机 Player／AOT／LAN／公网 | NotRun | 第二工作区未同步未提交代码；其他为本变更范围外 |

两组 UTF 都经 `Tools/AutoTesting/autotesting/run.py` 实际 PlayMode 发现后按 `testName` 前缀执行，初次合计 6 Passed。verify 的具体结果时机修复增加一项核心测试，并仅重新执行 Core 组四项，Wire 组三项继续复用；当前七项各有 Passed 证据，不声称七项在同一次作业执行。原始 Pipeline 输出留在 `Unity/Temp/pipeline_test_status.json`，工具按既有入口覆盖该文件，目前为 Core 4/4 组，duration=0.17s；两组原始终态均在本会话工具记录。Editor 原日志在 `Unity/Logs/GridKitchen-Editor.log`，未清 Console、复制证据包或改写测试结果。没有重跑历史全部套件。

首次测试源码缺少 UniTask 扩展 using，编译失败后补齐并重新编译；消息广播接入触发 ET0013 静态调用环，已把既有 Deliver 抽到 ProductDelivery，消除 Authority→Grid→Broadcast→Authority 环，后续编译和 GridWire 通过。不新增私有 RPC 等待表。生成工具要求 net8.0，而当前机器仅有 .NET 10.0.8，命令进程用 `DOTNET_ROLL_FORWARD=Major` 执行，未安装或更改系统 runtime；不宣称验证过本机 .NET 8 runtime。

源表改写的首次内存 ZIP 校验因复用 ZipInfo 导致校验失败，在写盘之前停止；改为复制 ZipInfo 并逐项比对后完成两行追加，未覆盖失败包。完整 Check 实际失败后没有继续导出。

首轮追加授权后，源清理为 Entity 14 行、UI 4 行、Scene 2 行、UISound 2 行、Music 3 行、Sound 5 行，共 30 条失效 GF 数据；DTDemo 另整体移除注册、源表及正常生成链路的 DR／DT／bytes／meta。当时保留了 OneConfig 和 Unit／AI／UIEntity 数据，随后用户要求继续清理，当前状态以文末记录为准。UI=7、Entity=1，Scene 和音频为空表，公共及 ET 配置在真实产品启动中加载成功。移除 66 个生成器仅换行的写入，未手改生成业务内容。

视图首次编译暴露无用 URP using 和缺失 GameFramework／UnityGameFramework.Runtime 引用，已移除 using、补齐实际消费的程序集引用后通过。真实 Solo 启动发现 RuntimeInstanceRequest.Submit 对无 endpoint 角色读空 Address，改为只为 Host／Client 构造 endpoint，随后真实 SoloReady／GridReady 成功。HUD 按实际手槽和落物列表变化更新按钮，固定 Tick 和普通移动不反复销毁按钮；最新编译包含此收口和 PendingResults 初始化清空。

收口静态检查发现插值 elapsed 原来最多覆盖一个 Tick，移动两帧快照之间会停在一步的 20%；改为仅在已确认起终格之间覆盖整步时长。为这个具体新疑点补一次短 Solo 视图检查：从 `(1,10)` 到 `(1,13)`，逐帧检查五个目标按钮 instance ID 不变，实际精灵在单步内观察到 progress=0.9320067，通过后停止所属运行。不重跑 Core／Wire 小集；这次检查也覆盖最新 PendingResults 初始化和 Solo 请求交接的正常启动，仍不冒充更多 UTF 用例。

按用户手机竖屏方向补齐 PlayerSettings：既有 AutoRotation 改为 Portrait，默认窗口 1024×768 改为 540×960；通过 Editor API 保存并回读。仅这三个配置字段变化，手机 Player／AOT 仍 NotRun。

一次资源菜单报告已执行但 Console 实际 CheckRule 失败，未当作刷新通过；清掉已不存在的 Hot 规则后刷新成功。规则刷新会移除 Optimize 生成的 Launcher／字体，因实际差异显示两条仍存在的启动资产，补一次必要 With Optimize 后两条保留；其他删除均已无源文件。ET.Code／HybridCLR 的 97 条旧集合引用中相关 DLL 已不在源目录，本轮未以资源集合或 Editor 成功声称热更包已准备。

贯通的动态 C# 使用现有 fixture 方式创建临时 ProductRoot Fiber（91001），接入既有 FiberManager、Timer、ProcessInnerSender、ProductClient、Session 和 Join，不注册第二份 BootstrapBridge，不改第二工作区或新增测试平台。Host 监听由 OS 分配 `127.0.0.1:58681`；远端 Member=2，世界 `62c0bc9df3e94a0289cf4f72bc47641b`，接取前双方 Version=19；碗 ItemId=`1283935544803334`，断线提交格 `(3,9)`，内容仍为“占位食材”。运行细节以 Editor 原日志和本会话 eval 原输出为准。临时 Fiber 已从 Manager 移除，所有所属 Play 已停止，GameView 临时分辨率及所属截图已清理，不保留额外证据包。

Console 历史含编译失败、首次资源规则失败及工具调用错误；没有清 Console。一次截图路径带 `..` 被 Pipeline 拒绝，改用允许路径后截图并检查；该条不是玩法异常。恢复临时 GameView 时旧索引越界，已按当前尺寸数量夹取有效索引。最终编译 groundTruth Error=0；UTF 的 6/6 与后续 Editor 贯通各证明自身范围，不把工具错误隐藏为从未发生。

首轮清理依据公共 SceneExtension／SoundExtension 的实际类型消费保留表结构；UI 保留 ET 登录／帮助／大厅及 Lockstep 注册，Entity 保留结构及新地图注册。DTDemo 无业务消费者，删除注册和源表，由生成器清理派生输出。当时保留的 OneConfig 和 Unit／AI／UIEntity 数据已在下述追加清理中进一步处理，不作为当前保留清单。回读源、核对预期输出删除并以编译和启动检查覆盖实际风险；不恢复 GF 示例、不关闭校验、不引入新工具。

## 追加清理业务示例数据（2026-10-11 17:34–17:38）

用户明确要求继续清理 UIEntity、Entity、音效等数据，因为当前业务很少。读取全部 ET 数据工作簿并查手写消费者后，确认 Entity 已仅剩本轮 GridKitchenBoard（80000），音效／音乐／UI 音效及 Scene 已空；新增清理 UIEntity WidgetTest 一条、UnitConfig 四条、AIConfig 两个工作表三条，共八条普通示例记录，累计 38 条。OneConfig 没有手写消费者且是 one 模式演示配置，整表删除注册／源文件，由既有生成器移除双端 DT／DR、bytes 和 Tables 注册；不填假默认数据维持演示表。

UIEntity／Unit／AI 仍被公共 API 或模块消费其类型，因此保留表结构与生成类型，数据清空。四张 StartConfig 源工作簿与 HEAD 字节逐一比对完全相同。最终公共配置为 UI=7（六条 ET Demo／Lockstep 初始化链路注册，加本轮 HUD=1101）、Entity=1（本轮地图=80000）；UIEntity=0、Scene=0、Sound=0、UISound=0、Music=0、ET Unit=0、AI=0，DTDemo／OneConfig 均已删除。旧 Demo 硬编码单位 1001 和 AI 演示配置不再可用，已在 KnownIssues／当前进度明确。

通过精确 projectPath 的 Pipeline 加载登录 Prefab，移除直接子节点 Test_WidgetTest、Test1_RectTransform、Test2_RectTransform（包含嵌套测试 Widget），调用当前 CodeBind 生成器重新生成 MonoUIFormLogin.Bind.cs，并在编译后重新序列化账户／密码／登录按钮三个有效绑定。删除独立 WidgetTest Prefab 与 UIWidgetTest／MonoUIWidgetTest／UIWidgetTestSystem 源码及其 Bind 派生文件，全部资源删除经 AssetDatabase 处理 meta；登录 OnOpen 不再发起动态 Widget 加载。保留通用 UIWidget／UIEntity 基础设施。

本次五个目标 Check／导出及 ID／本地化生成均有真实 5/5 成功日志和退出码 0。回读所有空表、唯一地图行／七条 UI 注册及启动源；ET Client／ClientServer／Config 中 AI／Unit bytes 均为合法空数据，OneConfig 输出移除。恢复 59 个生成器仅换行的无语义写入。Unity recompile_status=completed、failed=false，Console 原生 Error=0、compilationFailed=false。资源 With Optimize 真实成功日志后，GUID 差异核对相对 HEAD 删除 98 个引用、其中仍存在源资产数为 0；八个新增引用仍对应地图和修正后的 LockStep 资源。

普通 Launcher 实际进入 Demo 登录页：账户／密码／登录按钮引用有效、登录按钮可用、子 Widget=0；公共 UIEntity、ET Unit／AI 均已加载为空，GetDataTable("DTOneConfig")=null。随后停止该运行，再启动 cleanup-20261011/main-solo：SoloReady、GridReady=true，世界 eb3e75436bef431caca12aa3cc426bc9，Board／HUD 实际加载、HostScene 没有 NetComponent，空配置表仍有效。两次检查均有原日志 `[Cleanup] ... passed`，最终 Editor 为 stopped／ready。Console cursor=359→450 的 91 条完整记录无新增 Error／Warning、dropped=false、reset=false；没有清 Console。

一次动态 eval 检查误用了 internal FiberManager.Get，工具编译拒绝，未执行检查体；改为只读反射查看主 Fiber 后通过，这不是项目编译或产品异常。原日志留在 Unity/Logs/GridKitchen-Editor.log 与本会话工具输出，没有新增持久测试平台、结果副本或证据包。已有六项玩法 UTF 和真实 KCP 结果复用，未额外重跑。原用户 URP 六字段修改完整保留；未提交、归档或同步第二工作区。

## 继续 apply 与 verify 收口（2026-10-11）

复查发现 InstallIntent 原先把 Move 的目的地安装立即报告 Executed，而 HUD 将该状态显示为“已完成”；原 design 也把这解释成“目的地已安装”，与接收／最终结果分离的玩家语义不一致。已同步 design 和 product-session delta：Move 与自动接近在行走或等待空位时保持 PendingApproach，Move 只有实际提交到达才 Executed；替换任何未完成动作均 Cancelled。行走中仍先走完当前一步，规则与物品提交不变。

主要增量风险是提前显示完成、旧命令结果覆盖新目标、等待时被当成成功。因此只补新结果时机用例及满格等待状态断言，重新执行直接受影响的 GridCore 四项；Wire 三项、真实 KCP、竖屏操作、Luban 和空表启动结果复用，不因 verify 阶段再跑一遍。一次测试断言括号写错导致四条语法编译错误，已修正并重新编译；后续 recompile_status=completed、failed=false、compilationFailed=false。工具实际发现 38 项 PlayMode，只选 GridCore 四项，终态 4 Passed、0 Failed、0 Skipped、0 Inconclusive，duration=0.17s，包含到达一次成功、替换取消且旧命令不成功、投影进行中／完成和满格等待。Console 完整覆盖无新增 Error；最终 cursor=482、原生 Error=0、compiling=false。未清历史错误，未重写原测试作业。

参考文档 reactive-and-network 原先仍称固定 Tick 未实施、协议版本 1，已同步实际地图 owner 树、Solo、共同命令与通知两端、版本 2 和退出收尾。当前进度更新为七项各有 Passed 证据。用户 URP 改动仍为原六字段（6 增／6 删）；未提交、同步第二工作区或做 Player 构建。

### Verify 结论（默认 Launcher 修正前）

| 维度 | 结论与证据 |
| --- | --- |
| 完整性 | 19/19 任务；三个 delta 共 17 个 ADDED/MODIFIED Requirement（grid-kitchen 10、product-session 5、runtime-instances 2），全部找到实现。没有 REMOVED/RENAMED Requirement |
| 正确性 | 17/17 需求映射；39 个 Scenario 已检查对应代码条件，代表性路径由 Core 4、既有 Wire 3、历史实例／会话 UTF、竖屏与 KCP 操作覆盖。下列三项缺少专门的运行断言，Scenario Coverage 对这三项为 Not verified，未把静态检查冒充全部动态验收 |
| 一致性 | 实际 Match Component owner、既有 SceneType/Ordered MailBox、快速接收与 Tick 提交、独立投影、原 UI/Entity owner 和四程序集落点符合 design；表／协议／CodeBind 经源与生成入口完成。没有第二份权威状态或私有 RPC 等待表 |

实现映射：格子显示与输入由 `MonoGridKitchenBoard`／`GridPresentationSystem` 负责；寻路、停驻、交互、落物和 Tick 位于 `GridKitchenSystem`；共同接收、广播、投影分别为 `GridMessages`、`ProductWire`、`GridClientSystem`；Solo 配置与创建／退出为 `RuntimeInstanceConfig`、`RuntimeInstanceRequest`、`ProductRuntimeBootstrap`、`ProductRuntimeSystem`。Core／Wire 用例集中在 `Unity/Assets/Tests/Editor/ET/GridKitchenTests.cs`，既有实例配置测试在 `Unity/Assets/Tests/Editor/RuntimeInstanceTests.cs`。

CRITICAL：0。WARNING：3，均为既有失败分支缺少专门运行断言，并非已发现实现失效：

- “不可达目标”：`GridKitchenSystem.cs:204` 的接近规划和 BFS 返回 Unreachable，现有用例验证绕障碍但没有封闭目标的专门断言。后续修改布局／障碍规则或发现越墙时，补该目标拒绝且位置不变的局部用例。
- “斜邻位置不是可操作位置”：`GridKitchenSystem.cs:204` 只取四邻接近格，`:293` 执行前再校验 Manhattan 距离 1；已有寻路／远处接近覆盖，没有斜邻且四邻全不可达的专门运行断言。后续改接近位置选择或出现斜取时补该局部用例。
- “满手或满台面”：`GridKitchenSystem.cs:293` 对台面拿／放预检拒绝不合法手槽或台面；现有用例覆盖争抢、地面满手和成功交接，未专门断言持物操作有物台面。后续改交换／容量规则或出现错误转移时补原两物品位置不变的局部用例。

SUGGESTION：0。已执行的检查没有发现关键缺陷；Scenario Coverage 的上述三项专门运行证据未验证，不宣称所有场景通过。这些有限缺口在当前样机范围接受，不扩大为全量测试或新增归档前置条件。双 Editor 新玩法、手机 Player/AOT、LAN/公网、持久餐厅及极端长时运行继续 NotRun。

### 主规格同步评估

默认 Launcher 修正后共有四个 delta：新增 `grid-kitchen` 主规格（10 项需求）；`product-session` 修改所有者／Solo 需求并增加四项；`runtime-instances` 修改角色、endpoint 和普通 Launcher 三项；`startup` 修改 ET 固定入口及产品角色根入口分流两项。合计 14 项新增、6 项修改，20 项 Requirement／50 个 Scenario，无删除／重命名。归档前发现 startup 根入口分流旧条目仍将普通 Launcher 与 legacy 并列，补入同一 delta，保留原三项 Scenario 并增加显式 legacy 场景；这只对齐已验证入口，不修改实现或扩大玩法。现有主规格其他需求及 Purpose 保留；没有同步阻塞。同步和归档按用户明确授权执行，不能因任务完成自动归档。

## 普通 Launcher 交付入口修正（2026-10-11 19:15–19:37）

用户打开 Launcher 后仍看到示例，反馈属实。旧 tasks 2.3 明确保留普通 Launcher 的旧 ET 路线，实际验收也只证明登录页和显式 Solo 可用；将其描述为直接可体验的交付不成立。用户最新反馈替换这个旧边界，追加第 6 组任务，并重开 4.2 完成交付记录。

ProcedureET 仅在 RuntimeInstanceContext 为 NotRequested 时，在 CodeRunner／Loader 启动前建立新一轮 Solo 配置；Prepared 的显式 legacy／solo／host／client 不覆盖，Failed 不回退。默认入口沿现有 ProductRoot、共同本地消息、权威 Tick 和托管视图创建链，不在调试脚本中创建产品树或打开 UI。已同步 proposal、design、startup／runtime-instances delta、快速开始、启动 SOP、当前进度和 KnownIssues。

本次主要风险是默认值覆盖显式角色、再次运行继承旧状态，以及实际 HUD 被遮挡。只运行一次完整编译、停止后再次直接 Play 和实际渲染／输入检查即可覆盖；已通过的 Core 4／Wire 3、KCP、Luban 与空表结果复用。显式请求分支本次静态核对，不重复角色矩阵；双 Editor 新玩法、手机 Player／AOT、真实手指输入和 LAN／公网仍 NotRun。出现启动继承、HUD 失效或手机触控问题时，再补对应入口。

检查精确作用于主工作区 Unity（6000.3.18f1，pid 88628，Pipeline 7800）。开始时 Editor 已停止且 Assets/Launcher.unity 无未保存修改，本工程没有待提交实例请求。ProcedureET 修改后的完整编译为 completed、failed=false、errors=[]，Console compilationFailed=false。直接调用 editor_play，没有提交 Solo 请求，连续三轮均实际 SoloReady／GridReady：第一轮世界 `8d920bff117747508b859704f4487b98`，第二轮 `f8c933cb27854b9f8c4417963e60e31c`，第三轮 `7cdf90e4bed740a891c43d1df8f24118`。每次前一轮停止后再启动，当前保留第三轮运行供用户查看。

第二轮在实际 540×960 GameView 检查场景与 HUD 操作：

- 将格 `(2,10)` 经真实 Board Camera 投影到屏幕 `(197.027,477.002)`，调用既有 ProcessGridPointer 的按下／松开入口；真实帧推进后投影位置到达 `(2,10)`，结果为 Executed。
- 通过 EventSystem.RaycastAll 确认真实“拿 碗台”按钮可命中，再发送 pointerClickHandler；Host 提交后手持碗，ItemId=`1283944300412934`，内容仍为一份“占位食材”。
- 同样命中并点击真实“放 交接台”按钮；Host 提交后手槽为空、交接台持有同一 ItemId／内容，角色位于 `(3,9)`，Version=19，结果 Executed。

以上是工具通过实际场景输入函数与 UI 射线／事件执行的检查，没有物理手指操作，也没有直接写角色、手槽或台面状态。输入后的结果由原消息和 Tick 裁决，日志 `[LauncherDirect] ... passed` 留在既有 Editor 日志及本会话原输出。

真实截图暴露底座 DEBUGGER 浮层遮住顶部 HUD。停止所属第二轮运行后，经 PrefabUtility／SerializedObject 把 GameEntry 的 DebuggerComponent.m_ActiveWindow 默认设置为 AlwaysClose（3），保留组件，然后第三次直接 Play 确认顶部状态、操作提示、手持物及五个按钮均可见。最终截图为 [实际 Game 画面](../../../../Unity/Temp/pipeline-screenshots/launcher-default-frame.png)，由 ScreenCapture.CaptureScreenshot 保存并实际查看，未合成或改图；Pipeline 的 camera-only screenshot 不包含 ScreenSpaceOverlay HUD，未将它作为 UI 证据。GameView 保留 CarzyCooker Portrait（540×960）供查看。

Console 增量 540→603 完整覆盖、无 Error，有一条 `actor rpc time > 200: 754 ET.GridCommandRequest` Warning，出现在动态检查期间；没有将其当作输入性能验收。第三轮 603→635 完整覆盖、无 Error／Warning，groundTruth compilationFailed=false、compiling=false、consoleErrors=0、consoleWarnings=0；未清 Console。一次只读动态反射未包含 FlattenHierarchy 导致工具检查失败，修正后通过，不将工具失败隐去或当作产品已通过。

最终严格 OpenSpec 校验和差异检查通过（Unity 自动序列化尾空格沿既有约定接受），21/21 任务完成。GameEntry 本次只增加 Debugger 默认关闭覆盖；此前地图 Entity 组保留。用户 URP 仍为原六字段的 6 增／6 删，没有扩大修改。当前交付是有地图与基础 HUD 的占位样机，能走格和拿放；完整烹饪、订单、顾客、美术和手机包体验收不在已完成范围。

## 用户授权归档与提交（2026-10-11）

用户明确“归档并且 commit”。四份 delta 已合入主规格：新增 grid-kitchen，修改 product-session、runtime-instances 和 startup，共 14 项新增、6 项修改；全部 20 个变更需求的内容及场景与主规格一致。已有规格的标题、Purpose 和未涉及内容保持；21 份主规格及本 change 严格验证通过。

21/21 任务完成，change 移至 `openspec/changes/archive/2026-10-11-portrait-grid-kitchen-prototype/`，`.openspec.yaml` 保留，内部相对链接与当前文档索引已更新。editor-worktree-label 不在本次归档范围。归档不新增玩法或动态测试；Core 4／Wire 3、真实 KCP 和三次普通 Launcher 操作／渲染结果继续复用，手机及其他 NotRun 保留。

本次 main 提交范围包括地图样机、普通 Launcher 默认 Solo、基础 HUD、授权示例表／Widget 清理、源与正常生成物、资源及规格文档。原用户 URP 六字段修改留在工作区，不纳入提交；原始运行日志、截图、测试状态和缓存不提交。未 push、发布或同步第二工作区。