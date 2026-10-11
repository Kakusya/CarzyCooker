# 实施与验证取舍

用户已明确授权通过现有 Orca 建立长期第二端 `D:/MyWork/CarzyCooker-NetTest`，固定分支 `multiplayer-baseline`，纳入四项必要本地版本，以独立 ProductName/缓存/原生日志启动并联调。已通过 Orca 建立完整同仓 worktree，落实目录和固定分支；环境B时稳定版本为 `8b95e509`；维护C已纳入 `20582007`，业务源码只读。主开发目录继续使用当前 worktree，不另行隔离自身。

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
- AssetSet 实例已初始化，但当前文件系统/文件路径为 null；初次写入及扩展 `_1.dat/_2.dat` 未触发，NotRun，候选根属于各自 persistentDataPath。环境B阶段产品消息/KCP/角色反转及必要产品版本升级尚未执行；后续维护C及产品D/E另记，代理通信不作为产品网络证据。

## 必要版本维护 C

原请求 msg_7eddddb0eb5f，实际收件 msg_44e06cb21aa9、开始 msg_8dc0650b0424、终态 msg_b5510b53e4fe 均已读取。仅撤销自己旧 AGENTS 职责片段，git merge --ff-only 20582007 退出0后在新共同主体恢复稳定分支/路径/职责/版本；ProjectSettings未被目标改动覆盖，仍固定GDK/GameDevelopmentKit-NetTest。最终仅这两项自有差异，无新提交或业务改写。

稳定端重新编译实际completed/failed=false、groundTruth compilationFailed=false；产品Model/Hotfix/Loader类型与协议1可读，71个实际注册包可解析，本端Library独立。工具仍net8、本机仅.NET10 runtime；稳定端工具构建/执行NotRun，不重复生成或复制开发DLL。新增3条导入Warning（UXTool JSON时间不一致、JobTempAlloc及提示，无项目帧），原日志/Console保留，未掩盖为无警告。只纳入必要已有本地版本，没有Play/UTF/服务/打包/push/发布/PR/归档。

## 双 Editor 产品角色与交错验收 D/E

维护C后的20582007用于两端实际产品运行。D开发Host/稳定Client，实际49985、同Match/V2成员1/2在线，双向Probe回显裁决成员号；稳定Client业务空闲37.96秒仍Joined，原Session无在途堆积。稳定仅Dispose原Session后Failed/ProductConnectionClosed，开发Host只将原成员2标离线/V3，本地Probe仍成功。D最终msg_4a1529787adb已读取，双方只停止自己的Play并释放产品资源、开发监听可重新绑定。

E稳定Host/开发Client，实际58438、另一Match/V2成员1/2在线，两端Probe成功；开发Client业务空闲28.55秒仍Joined，原Session/心跳稳定。开发唯一Leave答复Committed=true，稳定权威/投影V3仅成员1，成员2及原Session/Peer移除且重复收尾不再增版；本地E-after-client-leave Probe成功，等待/在途0。E开始msg_39b288186069、观察msg_66a1be09720c、最终msg_3b949d8681a2已在原msg_13ef7260c88e线程实际读取并确认。

E稳定两次Stop后角色树已Dispose、senderStopped/等待0/Queue=null，58438独占UDP重绑定并释放、定点查询0；双方Play stopped、Editor ready、产品根和初始化入口清空。稳定静态World仅有既有Editor日志Logger/Options，不当作产品World。稳定Console598→636无新增Warning/Error且无reset/dropped，历史保留。D/E产品详细证据见 [产品执行记录](../2026-10-11-product-session-bootstrap/execution-notes.md)，工具动态端口与业务endpoint分开，配置身份与两端数据无串写。

稳定长期目录、Editor、终端与产品名保留，选定20582007覆盖四项必要代码；开发端最后仅补验收文档，不自动升级基线。最终稳定git差异只有AGENTS职责/分支片段与ProjectSettings产品名，业务源码未改、未提交/推送。AssetSet初次/扩展及自定义写入仍未验证，双Editor不扩大为物理两机或包体验收。

## 最终自审与收口

完整性12/12 tasks；正确性以实际worktree、版本维护、路径/设置重读、活跃终端原线程与D/E产品运行对应增量要求；一致性核对两端职责共同主体、固定身份和只清自身资源的边界。环境/代理消息/启动/产品结果分别记录，无未实施编排服务或跨重启持久去重声明。AssetSet未触发写入保留未验证，未来发现共写/新消费者才补对应检查。

最终五份规划、共用规程、Book/UnityCLI与启动SOP、模块索引和当前进度一致。四个change严格验证4/4、477个相关本地链接存在、git diff --check无空白错误；十九份主规格strict结果复用，不重复环境/UTF或产品运行。最终开发提交只收验收文档，长期稳定端20582007和两项自有差异保留，不推送、发布、PR、合回main、归档或删除目录。
