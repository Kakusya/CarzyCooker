# 实施与验证取舍

用户已授权四个 change 按依赖实施；历史只规划描述不限制当前授权。产品根、共同有序裁决、本地通道、独立值投影及 KCP 桥接代码已写入，当前尚未完成整项验证和提交，不能把前置文件或编译当作全部产品结果。

主要风险是本地/远端两套名单、桥接改写 wire RpcId、Peer 提前释放及连接销毁误删 Participant。先实施共同有序裁决与真实本地消息小集，再以真实 loopback KCP 验证身份、复制、心跳及退出，最后双 Editor 两种角色排列。复用同源编译与适用回归，不做全量性能测量或 Player 构建。

用户已明确授权第二端 `D:/MyWork/CarzyCooker-NetTest`、`multiplayer-baseline` 的建立、必要版本维护与联调，待前置本地提交后纳入必要版本。厨房、存档、自动重连、正式传输选型和物理两机 LAN 均未验证、不实施；只有本机原型出现具体问题才补验相关路径。

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
- 双 Editor 产品两种角色排列仍 NotRun，稳定端仍为 8b95e509；还须必要本地提交后纳入版本、补剩余失败/关闭边界和规划/主规格同步，不能把目前小集当成整项交付。
- 后续4项目标小集分别 Passed：无监听 Join 4秒预算后失败、wire内部控制消息被拒且原成员离线/本地可Probe、已提交本地Join但确认前Peer关闭不会Joined、无效1026-byte载荷及空Leave明确拒绝且合法Probe仍可用。各次只跑对应新增项，不重跑已通过集合。
- 自审修复：本地 Join/Probe await 后同时核对客户端实例、原入口仍开放和绑定代次；关闭后已提交事实不回滚，但旧确认不改变客户端。异步失败的角色树先收尾，再通过Loader LastPostLateUpdate停止捕获初始化World，避免在Runner遍历期间销毁调度集合；暂停/焦点回调核对已停止owner。
- 真实 Editor PRODUCT-NOLISTENER-20261011-1：连接自身先取得后释放的端口127.0.0.1:63038，Failed/ProductJoinFailed: session call timeout...4000；root/初始化桥接清空，FiberManager/MessageQueue/CodeTypes=null。CodeRunner外壳仍存在直到Procedure离开，不将外壳IsRunning当产品可用。Console仅预期超时Warning，无Error；结束自己Play后普通Launcher=NotRequested/Config=null、Runner/Fiber可用且无产品根，legacy=Prepared/Legacy也走旧根，重复StopOwnedEntry收尾成功，不继承旧失败。

- 三项 ProductLifecycleBoundaries 首轮全部 Failed：候选版本固定帧断言过早（实际2）、Event 发布泛型 IScene 无法匹配 AEvent<Scene>（实际代码缺陷）、默认非池化 Session 的 SameAs 断言触发实体 ToString/Bson循环序列化（测试前置错误）。修复为具体 Scene 泛型、显式真实池化 Session 和布尔引用断言、构造早到较新通知后通过真实 Join/Leave 验证；新运行3/3 Passed，原失败未改写。快照输入构造只证明投影合并，本地请求与 Session/Timer 仍走真实 API。
- 自审另补直接 Runtime 组件 Dispose 复用 Stop、ClientSession Destroy 清绑定/投影、产品解码失败仅关闭原 Session及输入 buffer finally 回收；非产品场景仍传播原解码异常。同源 DotNet.Hotfix 初次 DOTNET 分支 catch变量未使用错误，修复预处理范围后0warning/0error（3.29s）。原生资源两项新运行2/2 Passed：绑定失败的局部树清理、直接Runtime.Dispose后同址可绑定；未知Opcode经真KCP被拒，原成员离线保留，其他成员可Probe。
- 普通/legacy 已复用真实 Editor 回归与已有启动配置 UTF，避免另建重场景 runner 来重复同一路线；本轮保留根失败/加入退出/对象复用的真实 UTF。该验证取舍不削减入口行为，若后续入口分流或全场景初始化出现具体回归，再补相应场景集。

- ProductAdmissionBounds 新运行2/2 Passed：释放自己占用socket后Host显式指定同一非零端口实际监听，冲突/直接Dispose后释放；本地未加入Probe被拒，远端未Join控制请求被拒，原5秒接入超时真实移除Session且其他成员可Probe。该运行只包含新增场景和新增指定端口断言，不重跑全套。
- 当前18个产品UTF各有有效Passed证据，跨目标小集合复用，不宣称做过最终18项整套重跑。最后两个小集Console覆盖完整，没有本轮新增Error；Unity实际编译终态通过，DotNet Core/Model/Hotfix适用编译均0warning/0error。
- 主规格新增 product-session/multiplayer-peer-workspaces，合并 startup 两项和 ai-collaboration 三项，保留原场景；strict 19/19通过。当前改动文档本地引用424项存在。原生默认meta空值行尾不手改；代码/文档空白检查与Unity自动EditorSettings序列化噪声单独处理，不提交缓存或临时文件。双Editor联调及最终一致性任务仍未勾选。
