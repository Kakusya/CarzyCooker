# Execution Notes

## Current status

核心前置7/7已实际交付，前厅案现已5/5。19个唯一前厅UTF最新有效结果Passed，受影响核心HUD补跑1/1 Passed；普通Host＋同Editor独立Fiber真实KCP受管两菜交付／原餐具回收／自然两关通过。ServeOrder／FrontWork已接入外部白名单、独立值复制与原Client所属Session调用，内部Submit／伙伴路径仍关闭；未新增消息类型或字段，保留协议17。完整物流与双Editor仍待后继；下文保留实施过程，最终验收见连续营业节。

## 实施过程记录

2.1：真实PlayMode `CoopFrontWork` 5/5 Passed，退出码0、Console覆盖完整。两玩家与伙伴争同一询问，仅一执行者／一认领代次，5业务步只推进5/FrozenTicks；远端实际清桌使同一原碗入脏池，停止后原进度冻结，伙伴接续且旧认领不能停止新工作，原碗洗净可被Client取回；真实KCP断线或Leave释放同一洗碗供Host接续，断线保留离线Participant、Leave删除成员，公共sender均可用，原碗只回池一次。只为测试建立明确初始顾客／脏桌条件，并抑制无关新来客；认领、移动、进度、收尾与取回都通过实际请求及业务步，不注入完成结果。

1.1尚未勾选：CoopFrontWire首次9项中5 Passed／4 Failed；两菜真实交付、错菜／错顾客保物、同桌自然旧客离席后旧请求拒绝及一次计分／回执关联通过。4项内部拒绝已实际关闭连接并收尾到Stopped，但测试误期待过渡Failed。已改为核对实际终态、原Session已销毁及Failure保留、Host与sender持续可用，等待新的4项运行。1.2已补前厅DTO与真实远端受管UI用例，尚待执行；2.2／3.1仍待验收。

内部拒绝修正后的4项新运行4/4 Passed，退出码0、Console覆盖完整；加上未变的5项交付结果，CoopFrontWire九项最新有效结果均Passed，1.1已完成。原Session销毁、Runtime.Failure保留、Client终态Stopped与Host／sender持续可用均由实际断言确认，未把过渡Failed写作最终状态。网络Serve／FrontWork各自复制全部当前世界／关／绑定／命令号及目标字段，进入原Peer→HostSession两段Ordered MailBox；复制响应恢复捕获wire RpcId，Client按原pending.RequestType匹配，读取值后原owner观察者释放DTO。内部伙伴请求仍仅从原authority本地Actor路径到达，未建新RPC表或第二权威。

1.2：CoopFrontProjection两项真实PlayMode最新2/2 Passed，退出码0、Console覆盖完整。真实NPC询问开单与成长、远端清桌后原碗入池及同一洗碗工作产生的快照，在篡改／回收DTO后仍保留独立顾客、订单、桌位、池内原碗、工作进度和伙伴路径／成长数据。普通Host Launcher实际煮汤，独立Fiber真实KCP Client通过所属受管HUD取原汤碗→走到真实餐桌→Pointer交付→客户离席后Pointer清桌；两请求各只发一次，关闭／池复用／重开不撤销已提交动作，旧Serve/Clear callback不发送，伙伴最终洗净原碗且只回干净池一次。没有给客户端复制权威或创建另一份订单状态。

2.2：CoopFrontLevel三个真实PlayMode用例3/3 Passed，退出码0、Console覆盖完整。最后一个远端实际洗碗使双方保持Draining，暂停冻结原进度／业务钟且远端恢复被HostControlOnly拒绝，Host恢复后洗净才自然Completed并结转原池碗；普通客流／伙伴真实询问后订单到期离席，双方自然0分0星，Host相同Continue原命令重放只报告一次安装事实，库存／世界保留且持碗落原格，新角色InstanceId与关身份改变，旧FrontWork／Serve原请求及旧关高版本快照拒绝；另一例同时冻结已开始NPC洗碗和远端当前步，取消远端未执行询问接近，恢复只继续原洗碗／当前步而不恢复旧目标。全部沿原Restaurant唯一业务步，未增加关卡状态机或客户端钟。

本案19个唯一UTF最新有效结果均Passed（wire9、工作5、投影与UI2、关尾3）。3.1普通入口两菜交付和自然两关尚待验收。原始输出留 `Unity/Temp/pipeline_test_status.json` 与 `Unity/Logs/solo-five-changes-editor-20261013.log`；没有复制结果、改写失败、第二工作区操作、提交或归档。

## 2026-10-13 普通入口连续营业：5/5

3.1经原Instance Launch／SceneHelper以`coop-front-ordinary`启动Host，沿核心案独立Fiber真实KCP接入Client，两个所属受管HUD／Board依次关闭与重开。所有业务动作来自Pointer按钮／地图输入，无物资、工作完成或结算注入。job `f0f55b678efd4efc825010b7f7ffedb1` completed、result.success=true，执行134534ms。

| 功能 | 实际操作／观察 | 结果 |
| --- | --- | --- |
| 分工两菜及控制 | Host实际Buff／布局／番茄切配后交接，Client完成蛋液、同锅煮制／搬锅／盛一份和吐司装盘；Host原暂停／恢复按钮冻结并接续原汤进度 | Passed |
| 真实订单与一次交付 | Host开始营业，Client用当前订单“前往／交付”各完成碗装汤、盘装吐司，内容消费后保留原餐具，首关得20分／2星 | Passed |
| 原餐具循环 | 顾客实际吃完离席，Client清桌原碗／盘，伙伴实际洗净后各回有限干净池一次；同一原碗在下一关由Client取回并放回原碗台 | Passed |
| 关尾及实物继续 | 首关自然排空Completed，Host下一关一次安装新Preparing，6件精确物品身份保持；Client新角色／新关／新Buff就绪，没有旧顾客或工作 | Passed |
| 连续零星经营 | Host选新Buff并再次营业，不交付的订单实际离席／前厅排空；Host及Client收到同一第二关0分／0星自然结算 | Passed |

world=`5b6c22a3ef47499383382fe0a4e9f40b`，Host member1／Client member2；首关1284102228541467、下一关1284102312429851，原汤碗1284102228541447／吐司盘1284102228541460。结束前Console groundTruth编译正常、Error0；2条既有ET慢RPC阈值提示来自同一次交付的两段调用，ServeOrderSubmit364ms／ServeOrder395ms，功能答复与断言均正常，保留提示不清Console，不为此扩大全量性能测量。实际已查看结算图 `Unity/Assets/Temp/pipeline-screenshots/cooperative-front-completed.png`；截图只佐证受管画面，不代替功能结果。随后只停止自有Host Play并释放fixture自有网络／Fiber。

首次普通入口job `f30f599ff6f54385a84a2c0864897b3b`在最后检查Failed：前述流程已走到Host第二关Completed，但脚本未等Client接收该次通知便直接断言。改为继续泵原远端Fiber、等待Client实际安装同关Completed，新job通过完整流程。中途截图命令超时并未产生有效证据，未重发该截图，也没有把接口观察超时当成业务作业终止；同一job一直轮询到明确终态。历史失败保留，不续跑或改写。

前厅算法／餐具规则／关卡评分未改；沿原owner、消息和单业务钟接入。已复用未变Solo前厅／关卡证据，只补新增真实KCP、DTO、UI及连续两关。Protocol17；本案未操作第二工作区、提交或归档，完整双Editor仍NotRun。

## Planning status

2026-10-13 新建前置联机案，承接原案前厅与跨关开发任务。仅规划，0/5，全部功能 NotRun；未运行测试或修改代码。

## Rigor and scope

主要风险是旧顾客目标误交付、并发工作多执行者、餐具复制和换关旧消息，影响本轮分数与厨房。用真实 KCP 的交付、工作、投影和关尾行为小集，加同 Editor 普通入口自然两关覆盖；原 Solo 客流／配方和核心传输已覆盖且未变时直接复用。

接受双 Editor、Player、LAN、联机装修／存档未验证，分别归后续范围。若本案改动实际影响核心请求复制或权限，则补那一项核心回归，不因进入新案重复全部套件。

## Acceptance mapping

下列为拟补行为范围，不是已运行用例。

| 任务／功能 | 拟用例或实际操作 | 结果 |
| --- | --- | --- |
| 1.1 交付／换客／拒绝与内部隔离 | CoopFrontWire | NotRun |
| 1.2 同版前厅投影／实际 HUD | CoopFrontProjection＋受管按钮 | NotRun |
| 2.1 单执行者／接续／原餐具 | CoopFrontWork | NotRun |
| 2.2 暂停／排空／零星／换关身份 | CoopFrontLevel | NotRun |
| 3.1 两端分工服务与自然两关 | 普通 Host＋同 Editor KCP 实操 | NotRun |

## Planning validation

2026-10-13：本案 `openspec validate cooperative-front-service --strict` 退出 0、valid；本轮 25 份规划及入口文档的 UTF-8、尾空白和 164 个本地引用检查通过。没有实施或运行功能，任务保持 0/5、NotRun。
