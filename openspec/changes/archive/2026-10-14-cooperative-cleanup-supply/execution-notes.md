# Execution Notes

## 2026-10-14 归档收口

最终收口：全量严格验证 44/44（42 份主规格＋2 个保留的设备 change）通过；73 份相关 Markdown、561 个本地引用无缺失，编码／空白及 `git diff --check` 通过。八案已从活动列表移除，仅余 PC LAN／Android 各 0/5；本轮未新增 commit／push，原有未提交修改和临时资源保留。

本案已按用户要求归档，任务全部完成。已复用本文件逐功能 UTF／普通入口的最终有效结果；联机完整双 Editor 两种角色排列以 cooperative-restaurant-loopback 原记录为准。本次未改变实现或重跑功能测试。

本案 delta 已合并并逐块核对主规格；本批八案合计 47/47 任务、11 份 delta、41 项新增和 4 项修改要求、66 个场景，无批内规格冲突。保留各主规格已有标题／Purpose 和未涉及内容；本案范围外或历史 NotRun 不否定后续已交付的恢复、装修、饮品与保存能力，各归对应规格及执行记录。原始失败／日志保留原位，不作为本次新运行，也不提交额外证据包。下文未提交／未归档／尚待完成等文字保留当时阶段语境，以各节最终结果和本段归档状态为准。

## Current status

本案5/5，核心7/7／前厅5/5和Solo清理／供应均已实际交付。沿原协议17显式开放SupplyCommand及Grid13–17，外部请求复制完整世界／关／绑定／业务编号、来源／货物／包装／内容／布局到原Peer→HostSession两段Ordered MailBox，返回独立响应恢复wire RpcId，客户端按pending原类型消费值并回收DTO；内部SupplySubmit仍关闭。未新增消息结构、RPC表、库存或权威，原Host控制资格不扩展给个人采购／清理。

| 任务／功能 | 真实PlayMode／KCP与受管输入 | 最新结果 |
| --- | --- | --- |
| 1.1设施到位／卫生／锁定／原物与引用 | CoopLogisticsCleanup五项：远距只接收不生水，错设施／脏碗／不兼容盘拒绝；加工锅锁定，熟汤只退原批次引用，已盛汤碗保留；真实NPC洗碗占用水池拒绝另一次接水 | Passed |
| 1.2采购及两种收取入口／复制／回复／内部旧协议 | CoopLogisticsWire三项：所属KCP值调用重复采购只扣一次、额度不足零改动；SupplySubmit网络直达仅关闭请求者，旧协议／旧关采购收货拒绝；Grid16／17消费同一货物与原材料，原Rpc／业务编号匹配 | Passed |
| 2.1争货／满槽／布局／原材料／垃圾桶 | CoopLogisticsItems三项：两端请求同货仅生一包，满槽保留；移动收货点后旧布局最终拒绝，新请求在实际点接收；两端拆原内容，满手／旧内容拒绝，原材料／空壳只在垃圾桶销毁且不退款 | Passed |
| 2.2暂停／Completed／换关／失联／新Join／DTO | CoopLogisticsLifetime三项：原货1600冻结参数随唯一钟跨关、旧关请求拒绝、后加入不补库存；只泵Host提交采购，Client未读确认便断开，原货和扣额保留供Host收取；回收DTO后同包装／内容／余额保持独立 | Passed |
| 1.1／1.2／2.2受管远端UI | CoopLogisticsLifetimeUi：普通Host与独立Fiber真实KCP；Pointer采购／收货各一次，关闭／池复用／重开保留已提交事实，旧按钮不发送；实际拆份丢料／空包装不退款，原碗接水排液保留身份 | Passed |
| 3.1普通入口耗料补货／制作交付／在途换关 | 普通Host＋独立Fiber真实KCP，原受管HUD Pointer：耗尽面包，满收货点拒绝后接收并拆原材料制作吐司；剩汤排液、原材料及空包装实际到垃圾桶丢弃；两菜交付20分2星，原餐具回收，保留同一在途货物换关 | Passed |

15个唯一用例的最新有效结果Passed。首次14项12 Passed／2 Failed：Grid直达16／17被旧SubmitGridIntent动作上限15拒绝，补到17并加入工位存在校验，单项新运行Passed；旧布局测试仅等2步、未到位便读最终理由，改为等实际收货点最终拒绝，新单项Passed。其他未变12项结果复用；新增UI1/1 Passed，所有通过运行退出码0、Console覆盖完整。期间测试的var多声明编译错误修正后recompile_status completed／failed=false。历史失败不改写、不续跑。

物流开放后按当前范围迁移原核心未知动作测试，仅0／99仍是未知值，新增13–17由本案功能用例验证；修改后的未知动作2/2及核心受管HUD旧callback／池复用用例1/1均新运行Passed。其它核心／前厅未变行为证据直接复用，不重跑全仓。

### 普通入口3.1实际结果

2026-10-13，`CoopLogisticsAcceptance.Run` job `b2add8210e0c488caaa012921c07dfd3` 为completed／success=true，执行114458ms。普通InstanceLaunchWindow／SceneHelper启动Host，独立所属Fiber以实际动态endpoint连接KCP；所有操作沿受管HUD Pointer／地图输入，不注入完成状态、物资或时间。

| 功能 | 实际输入与断言 | 结果 |
| --- | --- | --- |
| 合作两菜、布局与暂停 | Host选Buff并移动交接台、切番茄；远端煮汤，Host暂停冻结／恢复；原吐司先实际丢弃使面包耗尽，补货后重做并交付，两菜20分2星 | Passed |
| 剩液与原锅 | 原锅1284104367636493排液，退批次1284104386511420，已盛汤碗1284104367636487保留；原空锅复用 | Passed |
| 采购、满槽与拆份制作 | 面包包1284104417969815，满收货点拒绝且配额／货物不变；腾空后接收，原材料1284104417969816烘焙并装原盘，另一材料1284104417969817及空包装实际丢弃，面包配额仍4 | Passed |
| 原餐具与自然换关 | 实际交付、清桌与伙伴洗净原碗／盘；自然Completed后实际下一关，精确4件库存身份不变，新角色／Buff及无旧工作，取回同一汤碗 | Passed |
| 在途停钟与跨关 | 番茄货物1284104430553175在Completed延迟250ms仍停进度；关1284104367636507→1284104480886784，货物原身份及冻结参数1600保留，运输946→1028且差值等于业务tick差，番茄配额仍4 | Passed |

代表性在途验收临时仅将`SupplyConfig.xlsx`的sheet1 H4从40改为1600，Check五目标、正常生成五目标及四语言均成功；运行货物实际RequiredTicks=1600。完成后H4恢复40，重新Check／正常生成成功并同步Import；另一次普通Host启动`VerifyDefaults`实际读到三个SupplyConfig源TransitTicks均40，world=a5c33a4050ac483390aefcec50b79efc，随后只停止本轮自己的Play。未手改生成物。

Console核对compilationFailed=false、Error=0；两条既有前厅慢RPC Warning留原位。`Unity/Assets/Temp/pipeline-screenshots/cooperative-logistics-carried.png`拍摄晚于功能job，此时货物已Ready，仅佐证后继画面；在途跨关由上述实际断言证明。前厅第二关0分0星的未变结果直接复用，未为文档收尾重复运行。

临时fixture只使用现有自己的Host／Client／Fiber／KCP，端口仍127.0.0.1:0、远端读实际endpoint，释放各自资源；只为明确边界设置初始脏桌或冻结货物时长，不注入完成／物资结果。原始输出留 `Unity/Temp/pipeline_test_status.json` 和 `Unity/Logs/solo-five-changes-editor-20261013.log`。没有第二工作区操作、提交或归档。

## Planning status（以下为实施前历史记录）

2026-10-13 新建前置联机案，承接原案清理／供应网络适配与物资边界。仅规划，0/5，全部功能 NotRun。Solo 供应同期仍在实施，本案不引用其中间文件作为已验收结果。

## Rigor and scope

主要风险是满槽吞货、重复预留／生货、远端绕设施和跨关补满，后果局限于本轮资源。沿原物品与货物 owner，用真实 KCP 的设施、数量和生命周期小集加一次实际补货营业链覆盖；复用已交付且未变的 Solo 来源／包装／配方校验。

接受仅本机同 Editor 的阶段验证，不要求重新测全部原料平衡或长期网络排列。双 Editor 由原案验收，Player／LAN／联机存档未验证；若更改前置字段或布局规则，才补对应原用例。原始输出留工具原位，不创建证据副本。

## Acceptance mapping

名称仅为拟补范围；执行时发现真实用例、记录每项终态。

| 任务／功能 | 拟用例或实际操作 | 结果 |
| --- | --- | --- |
| 1.1 到位／分类／锁定／原容器 | CoopLogisticsCleanup | NotRun |
| 1.2 wire／可信身份／重复与旧关 | CoopLogisticsWire | NotRun |
| 2.1 争货／满槽／搬工位／包装数量 | CoopLogisticsItems | NotRun |
| 2.2 时钟／换关／失联／后加入／投影 | CoopLogisticsLifetime＋实际 HUD | NotRun |
| 3.1 耗尽补货制作交付／清理／在途换关 | 普通 Host＋同 Editor KCP 实操 | NotRun |

## Planning validation

2026-10-13：本案 `openspec validate cooperative-cleanup-supply --strict` 退出 0、valid；本轮 25 份规划及入口文档的 UTF-8、尾空白和 164 个本地引用检查通过。任务仍为 0/5、NotRun，不把格式通过当作补货链通过。
