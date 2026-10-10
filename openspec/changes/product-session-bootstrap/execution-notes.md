# 实施与验证取舍

用户已授权四个 change 按依赖实施；历史只规划描述不限制当前授权。产品根、共同有序裁决、本地通道、独立值投影及 KCP 桥接代码已在本地提交20582007；下列动态记录保留初次失败、修复与分阶段验证，不把前置文件或编译当作全部产品结果。

主要风险是本地/远端两套名单、桥接改写 wire RpcId、Peer 提前释放及连接销毁误删 Participant。先实施共同有序裁决与真实本地消息小集，再以真实 loopback KCP 验证身份、复制、心跳及退出，最后双 Editor 两种角色排列。复用同源编译与适用回归，不做全量性能测量或 Player 构建。

用户已明确授权第二端 `D:/MyWork/CarzyCooker-NetTest`、`multiplayer-baseline` 的建立、必要版本维护与联调，维护C已纳入20582007。厨房、存档、自动重连、正式传输选型和物理两机 LAN 均未验证、不实施；只有本机原型出现具体问题才补验相关路径。

## 当前动态结果（2026-10-11）

- 新 Proto 源追加 ZProductSession.proto，经既有 Proto2CS 生成 Client/ClientServer 输出。旧 48 个 Opcode 逐项语义比较保持，新增 13 个。工具需 .NET8，而当前只有 .NET10 runtime；单次 dotnet --roll-forward Major Tool.dll 生成成功，未安装运行时或改全局环境。没有改表、生成 DT/DR/数据或手改协议生成物。
- Unity 实际 CodeMode=ClientServer；新产品 Share 代码限定 !DOTNET，为 Unity 本机原型，不创建 .NET 服务端产品入口。Model 仍 noEngineReferences，通过现有 Loader 普通桥接类取得初始化 owner 与同步关闭，不直接依赖 MonoBehaviour API；Hotfix 显式引用既有 Game/Loader，无新增业务程序集。
- 首轮 PRODUCT-HOST-20261011-1 在 Config 加载阶段 Failed/ETEntryFailed，产品根未创建。实际栈指向 DTStartSceneConfig.LoadAsync→PostInit：人工 LubanPartial 扩展在并发加载期间读取尚未就绪的 Process 表。已将索引构建移到 PostResolveRef（当前 outputCodeDir 只覆盖相邻 Luban，不生成该 partial），不改派生表代码/数据；旧配置 await 使用捕获的 Config/Tables 并在恢复后核对 owner，避免旧轮回写新单例。初次失败保留原日志。
- 修复后编译终态 failed=false、Console groundTruth compilationFailed=false。第二轮 PRODUCT-HOST-20261011-2 实际 HostReady/Joined，监听 127.0.0.1:56192，MatchId=d7102115298640bb879a3f8c3a9da7be，成员/名单版本=1，本地无网络心跳。Probe 回显 local-host-2、裁决成员号1，真实调用栈经过 ProcessInnerSender→Mailbox→产品共同入口。
- 本地 Leave committed=true 后 Client=Stopped、Match 成员数=0，Host 阶段保留启动成功事实，监听仍持有，sender 未停止。随后 StopProduct 两次，senderStopped=true、pending=0、桥接 root 清空；同一实际 UDP 地址可重新绑定并释放。仅结束本轮 Play，没有关闭长期 Editor。
- 对本轮成功路径，从 Console cursor11720 起至11750，未新增 Warning/Error、同 session、无 reset/dropped。原日志 C:/Users/Administrator/AppData/Local/Temp/CarzyCooker-Multiplayer-Editor.log 保留。临时 Probe eval 首次错误调用 UniTaskExtensions.Forget(UniTaskVoid)，编译拒绝未执行，改用实例 Forget 后执行一次。
- 本地 6 项小集首轮 5 Passed/1 Failed：端口冲突确实发生，但断言误预期直接 SocketException，实际 UdpTransport 抛外层 Exception、InnerException=SocketException。修正断言后只补跑该 1 项，Passed，原先 5 项直接复用。测试程序集首次遗漏 MemoryPack 直接引用导致 CS0012，已补现有引用。
- 新增真实 KCP 小集 2/2 Passed：远端和本地共用名单及裁决成员号、Probe 回显、空闲跨过既有 10 秒阈值仍 Joined、显式 Leave 答复 committed=true且其他成员可 Probe；协议 999 被明确拒绝且 Match 不登记该成员。采用真实 UDP/KCP/Session/Timer，两份原等待表，非 mock。KCP Send 排队后留 100ms 原型关闭宽限，使下一次 Update 有机会发送 Leave/拒绝答复；不保证远端实际收到，不回滚已提交事实。
- 三次上述运行均保留原始 Pipeline 输出在 Unity/Temp/pipeline_test_status.json 原位，正常新作业覆盖，不复制/改写失败。最终两个补跑的 Console 覆盖完整，无本轮新增 Error。前置启动项 10 项 EditMode、本地 sender 9 项 PlayMode 结果继续复用。
- DotNet.Core 增量编译 0warning/0error（2.61s）；DotNet.Model 首次 --no-restore 缺 project.assets.json，恢复依赖后编译 0warning/0error（4.51s）。没有启动服务或 Player 构建。
- 此时双 Editor 产品两种角色排列仍 NotRun，稳定端为8b95e509；后续维护C纳入20582007，D/E结果另记下节，不将当时小集当成整项交付。
- 后续4项目标小集分别 Passed：无监听 Join 4秒预算后失败、wire内部控制消息被拒且原成员离线/本地可Probe、已提交本地Join但确认前Peer关闭不会Joined、无效1026-byte载荷及空Leave明确拒绝且合法Probe仍可用。各次只跑对应新增项，不重跑已通过集合。
- 自审修复：本地 Join/Probe await 后同时核对客户端实例、原入口仍开放和绑定代次；关闭后已提交事实不回滚，但旧确认不改变客户端。异步失败的角色树先收尾，再通过Loader LastPostLateUpdate停止捕获初始化World，避免在Runner遍历期间销毁调度集合；暂停/焦点回调核对已停止owner。
- 真实 Editor PRODUCT-NOLISTENER-20261011-1：连接自身先取得后释放的端口127.0.0.1:63038，Failed/ProductJoinFailed: session call timeout...4000；root/初始化桥接清空，FiberManager/MessageQueue/CodeTypes=null。CodeRunner外壳仍存在直到Procedure离开，不将外壳IsRunning当产品可用。Console仅预期超时Warning，无Error；结束自己Play后普通Launcher=NotRequested/Config=null、Runner/Fiber可用且无产品根，legacy=Prepared/Legacy也走旧根，重复StopOwnedEntry收尾成功，不继承旧失败。

- 三项 ProductLifecycleBoundaries 首轮全部 Failed：候选版本固定帧断言过早（实际2）、Event 发布泛型 IScene 无法匹配 AEvent<Scene>（实际代码缺陷）、默认非池化 Session 的 SameAs 断言触发实体 ToString/Bson循环序列化（测试前置错误）。修复为具体 Scene 泛型、显式真实池化 Session 和布尔引用断言、构造早到较新通知后通过真实 Join/Leave 验证；新运行3/3 Passed，原失败未改写。快照输入构造只证明投影合并，本地请求与 Session/Timer 仍走真实 API。
- 自审另补直接 Runtime 组件 Dispose 复用 Stop、ClientSession Destroy 清绑定/投影、产品解码失败仅关闭原 Session及输入 buffer finally 回收；非产品场景仍传播原解码异常。同源 DotNet.Hotfix 初次 DOTNET 分支 catch变量未使用错误，修复预处理范围后0warning/0error（3.29s）。原生资源两项新运行2/2 Passed：绑定失败的局部树清理、直接Runtime.Dispose后同址可绑定；未知Opcode经真KCP被拒，原成员离线保留，其他成员可Probe。
- 普通/legacy 已复用真实 Editor 回归与已有启动配置 UTF，避免另建重场景 runner 来重复同一路线；本轮保留根失败/加入退出/对象复用的真实 UTF。该验证取舍不削减入口行为，若后续入口分流或全场景初始化出现具体回归，再补相应场景集。

- ProductAdmissionBounds 新运行2/2 Passed：释放自己占用socket后Host显式指定同一非零端口实际监听，冲突/直接Dispose后释放；本地未加入Probe被拒，远端未Join控制请求被拒，原5秒接入超时真实移除Session且其他成员可Probe。该运行只包含新增场景和新增指定端口断言，不重跑全套。
- 当前18个产品UTF各有有效Passed证据，跨目标小集合复用，不宣称做过最终18项整套重跑。最后两个小集Console覆盖完整，没有本轮新增Error；Unity实际编译终态通过，DotNet Core/Model/Hotfix适用编译均0warning/0error。
- 主规格新增 product-session/multiplayer-peer-workspaces，合并 startup 两项和 ai-collaboration 三项，保留原场景；strict 19/19通过。当时改动文档本地引用424项存在。原生默认meta空值行尾不手改；代码/文档空白检查与Unity自动EditorSettings序列化噪声单独处理，不提交缓存或临时文件。当时双Editor联调及最终一致性任务未勾选，后续结果另记下节。

## 双 Editor D：开发 Host / 稳定 Client

两端20582007、协议1；run=PRODUCT-DUAL-20261011-D，开发development/Host实际127.0.0.1:49985，Match=2876d22a85f04416b1e35863471735fa。Host本地成员1，稳定Client成员2；两端V2、1/2在线，分别Probe D-host-local/D-peer-client回显各自成员号，稳定端原响应消费后释放。稳定Client只含ProductClient，无权威Match；本地没有网络心跳。稳定端业务空闲37.96秒后仍Joined、相同Session、无在途累积，成功心跳持续。

D请求msg_b94ca5cfccce、收件msg_30feeb9ca7cb、开始msg_c79217b91b05、观察msg_5b932e93470d、断线开始msg_c2e667af16c3、释放观察msg_07332b311a46、最终msg_4a1529787adb均在原线程实际读取。稳定端仅Dispose原Session模拟非Leave，Failed/ProductConnectionClosed保留，原Session/Root/Client/Fiber及World入口已释放、callbacks=0，后续没有Joined恢复；只结束自己Play。Host保留成员2离线、名单V3，D-after-peer-disconnect Probe成员1成功；然后仅自己Stop两次，原49985可重新绑定并释放，双方Editor ready/stopped，长期资源保留。

D稳定Console无新增Error，断线时仅预期ProductConnectionClosed Warning；开发端有一次既有actor耗时阈值Warning（Probe 201ms>200），结果成功，不当性能门槛或扩大测量。原日志仍各自原位，没有清Console或证据快照。稳定端旧捕获World对象仍留单例字典引用（现有Dispose末轮未Clear），静态入口/原活动资源已释放，不扩大到清理既有实现。观察时大整数JSON表示曾丢精度，后续以字符串核对原SessionInstanceId，不以JSON数字比较代替实例身份。

## 双 Editor E：稳定 Host / 开发 Client

两端仍为20582007、协议1；run=PRODUCT-DUAL-20261011-E，稳定nettest/Host实际127.0.0.1:58438，Match=c9e0cf20ec3e437198171f0e36093168。稳定Host本地成员1，开发Client成员2，两端V2且1/2在线；Probe E-peer-host-local/E-development-client分别回显成员1/2，稳定本地响应已释放。开发端没有权威Match；稳定本地不发网络心跳。开发Client空闲28.55秒，相同Session（InstanceId字符串647111538393481225），RPC8→22，pending=0、心跳未堆积，持续Joined。

E请求msg_13ef7260c88e、收件msg_155464542c1f、开始msg_6577542b705b、endpoint msg_a4b2e948dfbd、初始观察msg_8e18ca0e531c、两成员确认msg_0e9b4d780911、Leave收件msg_39b288186069、Leave观察msg_66a1be09720c均已实际读取。开发端唯一显式Leave收到ProductLeaveResponse RpcId75/Error0/Committed=true，随后Client=Stopped、原Session释放；稳定Host权威与投影V3仅成员1在线，成员2、原远端Peer/Session确实移除，重复原生收尾后仍V3。稳定本地Probe E-after-client-leave成功（成员1/Sequence2），响应已Dispose，peer.InFlight/actor等待归零。

开发端随后StopProduct两次且只结束自身E Play，实际Stopped/root及初始化桥接清空，FiberManager/MessageQueue/CodeTypes=null、CodeRunner=false，无failure。临时收尾eval首次直接比较EntityRef与null被编译拒绝、未执行；改为Session取值后执行一次成功，不重投Leave。开发Console至12709同session、无reset/dropped、新增Warning/Error为空；原始日志保留。

稳定最终msg_3b949d8681a2已实际读取并原线程确认。同一E runtime两次Stop均Stopped、版本3/3/3，原角色Scene/Authority/Match/Client已Dispose、senderStopped/requestCallback0/Queue=null；原58438独占UDP重绑定成功后释放，定点端口查询为0。仅自己Play停止、Editor ready，产品Root/初始化World/Init/Fiber清空。静态World只含EditorLogHelper创建的Logger/Options两个编辑器日志单例，不作为活动产品World或擅自销毁。稳定Console598→636同session、无reset/dropped、新增Warning/Error为空、compilationFailed=false，历史错误/警告保留。

D/E两种排列均已完成，稳定端固定产品名、长期Editor/终端/目录和20582007选定代码基线保留，仅AGENTS职责与ProjectSettings产品名两项自有差异。开发端收口提交只更新验收文档，不再次升级稳定代码或重跑已适用集合。本机双Editor不证明物理两机LAN、Player/AOT或CodeBytes热更路径，未运行范围保持NotRun。

## 最终自审与收口

完整性18/18 tasks；正确性对照增量与主规格，产品owner树、共同Match写入、wire编号/消息回收、深值投影、关闭资格/在途排空分别有源码及真实小集/D/E证据；一致性对齐现有ET分层、同一等待表与Loader生命周期，没有第二权威名单或新框架。发现的实际配置并发、事件泛型及直接销毁资源问题已修复，夹具/临时eval失败如实记录，不用最终通过覆盖旧失败。没有范围内剩余阻碍。

本次只为最终规划/状态文档变化检查四个change严格格式（4/4通过）、477个本地文档链接（全部存在）与git diff --check（无空白错误），十九份主规格strict通过结果直接复用；主规格自该结果后没有修改。五份规划、Book启动/验证SOP、模块索引、当前进度及共用规程已对齐当前结果。18项产品UTF及前置10/9项结果分小集复用，未重跑全套；包体、物理两机、CodeBytes及未触发存储消费者继续列明NotRun。
