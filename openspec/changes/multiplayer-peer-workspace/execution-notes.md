# 实施与验证取舍

用户已明确授权通过现有 Orca 建立长期第二端 `D:/MyWork/CarzyCooker-NetTest`，固定分支 `multiplayer-baseline`，纳入四项必要本地版本，以独立 ProductName/缓存/原生日志启动并联调。已通过 Orca 建立完整同仓 worktree，落实目录和固定分支；当前稳定版本 `8b95e509`，业务源码只读。主开发目录继续使用当前 worktree，不另行隔离自身。

主要风险是应用数据与 Editor 日志共写、目标选错和升级覆盖稳定职责。采用真实路径/实例发现、两端不同设置值保存重读及原线程收件/开始/结果回报，不建立新编排服务，不复制活动 Library。环境、消息、启动、产品结果分开记录。

仅释放本轮拥有且获准结束的资源，保留长期目录与原用户数据。未触发 AssetSet 扩展或未覆盖的自定义写入列为未验证；发现真实共写或协议缺失时才补对应检查。无 push、发布、PR、合回 main 或自动归档。

## 2026-10-11 环境与原线程验证

- 稳定端完整子模块依赖初始化成功；Library、Temp、Logs、UserSettings 分别位于本端且不是目录链接。两端 Editor 6000.3.18f1、Pipeline 0.8.0-exp.1 按精确 projectPath 可发现；稳定端包解析和编译终态 failed=false。动态工具端口不用作产品 endpoint。
- 运行前稳定端保存 CompanyName=GDK、ProductName=GameDevelopmentKit-NetTest，开发端保持 GameDevelopmentKit。设置实际文件分别为 `C:/Users/Administrator/AppData/LocalLow/GDK/GameDevelopmentKit-NetTest/GameFrameworkSetting.dat` 与 `.../GameDevelopmentKit/GameFrameworkSetting.dat`；Resource 实际 PersistentData 读写根同各自产品目录，ReadOnlyPath 属各自 StreamingAssets。
- B 原请求 `msg_c6bfb66f0895`，收件、开始、观察 `msg_d696c2a18f6d`、拒绝/重读 `msg_f4b316d66f77`、最终 `msg_b60695cd15bc` 均在原线程实际读取；主端最终确认也已回复。稳定端 B 用完整 AllScenes→SceneHelper 仅启动一次，Host 配置 PEER-ENV-20261011-B/nettest，实际 Failed/ProductBootstrapUnavailable，CodeRunner=false、Init/FiberManager=null；仅证明该基线明确缺产品入口。
- 两端同名临时键分别写 stable-B/development-B、保存并从各自实际文件重读；交错写入后再次重读仍各自原值。各端只删除自己的指定键并保存/重读确认不存在，稳定端其余 6 项原值保持。只结束各自本轮 Play，长期 Editor/终端/目录保留。
- 重复 B、错误目标、B Play 忙碌及先作废 VOID-B 后请求执行均在接收方启动前拒绝。实际 CanLaunch 返回 EditorBusy；检查前后配置为同一对象、B 身份和 Play 未被替换，无新 StartScene。目标、重复、作废由当前活跃代理按规程记忆处理，未声称自动执行器或跨重启持久去重。
- 稳定端原生日志 `D:/MyWork/CarzyCooker-NetTest/Unity/Logs/Multiplayer-Peer-Editor.log`，独立 UPM 路径同目录 Multiplayer-Peer-UPM.log；开发端保留原 `C:/Users/Administrator/AppData/Local/Temp/CarzyCooker-Multiplayer-Editor.log`，未重启它。原生日志参数和工程归属已核对。
- A 首次探测遗漏 AllScenes 前置，Launcher 不在构建列表导致失败，原线程已终态，不续跑；B 是新授权运行。B 新增 UnityEditor.Search.SearchDatabase ArgumentOutOfRangeException 一次，无项目帧，保留原 Console/日志；不宣称全部 Console 无错误。开发端清理 eval 首次把 void Save 当 bool 导致编译拒绝、未执行，改正后清理成功。
- AssetSet 实例已初始化，但当前文件系统/文件路径为 null；初次写入及扩展 `_1.dat/_2.dat` 未触发，NotRun，候选根属于各自 persistentDataPath。产品消息/KCP/角色反转及必要产品版本升级尚未执行；不是本次代理通信的通过范围。
