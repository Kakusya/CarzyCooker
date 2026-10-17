# 执行说明

本案对应第4项，用户确认任意具有原采购权限成员处理，不增加Host审批；整批实施已授权。首版一人一个请求、每次一个来源的一包，已下单指原真实预留成功，不代表收到。实现及核心/UI运行已完成，普通入口仍按下方记录，不提前勾选。

## 严谨程度与取舍

主要风险是两人处理导致重复扣配额/造货，或协调退出误撤实际运输。把标记关联放原采购提交点，用一次真实KCP竞争/失败重试及原收货拆份旅程验证即可；无线上资产/新账本，不扩展交易系统。原运输、守恒、菜单许可未变证据可复用，新增关联与退出必须跑。接受正式平衡、设备和真人协作未验证；具体重复货物/资格差异触发对应补验。

## 功能映射

| 任务/功能 | 最小运行 | 结果 |
| --- | --- | --- |
| 1.1 请求和采购 | 下述3项实际核心用例 | Passed：发布零扣量、真实KCP竞争一次预留、原配额/菜单/暂停/收尾限制、替换/旧请求身份、同请求暂停后重试 |
| 2.1 持续运输/界面 | 第3核心、新UI及普通540×960 | Passed：退出/换关同一货物续运/原接收拆份、Host发布队员处理/池化/恢复旧回调；普通发布零采购→原处理/接收/拆份 |

没有Host审批等待态，不把任意其他独立采购自动当作处理请求；不增加自动补货或写成功档。不提交、推送或归档。

## 实施和原始失败

Level持每成员一条独立值（唯一RequestId/修订、来源、待处理或已下单和原ShipmentId），发布/替换/取消带预期旧身份，均经原HostPeer→HostSession邮箱；处理沿原SupplyIntent和原采购队列，在实际扣配额/建SupplyShipment同分支回填。每次处理校验真实处理者、发起者当前绑定、请求身份和原菜单/阶段/配额；没有第二套采购/余额。原handler及wire显式复制新增关联字段，Proto源尾部追加4消息/1DTO，原203个opcode全部保持至208；wire38→39，成功文件12不变。请求不进入保存载荷。

发现766个PlayMode。核心首跑竞争用例Passed；菜单/失败用例Failed于旧 `StartGrowth(true)` 夹具仍停在房间提案，没按新规则显式Ready/Start，因此远端没有世界投影。仅该共用测试辅助补原 `StartPublishedRoomForFixture`，产品没有自动Ready；该用例新跑Passed。持续货物用例首跑在Continue后要求进度完全不变，但原辅助已实际推进2个新关业务Tick；改为精确比较进度增量与真实Tick增量，保留Completed冻结及同实例断言，新跑Passed。原失败保留，未修改产品业务钟来迎合夹具。

- `EntrySupplyRequestActualKcpPublishDoesNotBuyAndTwoMembersRaceOneOriginalReservation`：真实远端发布零货物/零扣量、本地不能冒名撤另一人提示，Host本地/真实KCP并发处理只有1货物/1配额；最新原采购重放和另一新处理不重复；独立正常采购不标记另一请求已下单；DTO变异/回收后值保持。
- `EntrySupplyRequestActualOriginalQuotaMenuPauseAndDrainingFailuresKeepPendingAndOldReplacementCannotWrite`：原合法采购耗尽配额后两人处理都保留Pending；实际Growth菜单未许可橙子拒绝，旧修订撤销/处理不改替换项；暂停拒绝保留，恢复后同一请求真实成功；自然收尾拒绝新采购仍Pending，无额外预留。
- `EntrySupplyRequestActualPublisherLeaveAndNextLevelKeepSameShipmentThenOriginalReceiveAndSplit`：真实Leave清协调提示，原长途货物同实例/配额保留、Completed冻结、下一关按真实Tick续运；旧处理WrongLevel，原接收和拆份得到唯一原材料、包内减1、仅1货物。

`EntrySupplyRequestActualHostPublishesAndKcpMemberProcessesReceivesSplitsAndRecoversWithoutApproval` 首跑Passed，覆盖原Host发布/替换/撤销与HUD池重开旧回调、真实KCP队员明确处理Host请求（无Host审批、原Client权限不变）、原在途/已下单/接收/拆份分开，原材料身份守恒；Client自己新请求失联后清理，手动恢复新绑定不复制旧提示或重复采购。3核心＋1UI四个不同实例目前最新全部Passed，Console覆盖完整，无Skipped/空集。

原始输出保留于 `Unity/Temp/pipeline_test_status.json` 和 `Unity/Logs/Business-Delivery-Editor.log`；没有复制或改写。普通入口和文档收尾见后续最终记录。

## 普通入口和收尾

UTF外原Launcher Host内存菜单关401、540×960，原指针回调完成发布番茄请求（零采购）、替换鸡蛋/撤销、重新发布番茄及只读查看，明确处理command8才产生原货物；随后原接收command9、拆一份command10和暂停command11均实际Executed。最终请求Ordered，原货物Received且仅1条，配额4/5，原包装剩1份、唯一原材料在手槽；查看不增加命令。正常自动化曾因接收/拆份按钮使用短名前缀而等待，终止该临时回调后用原实际前缀继续同一货物，没有重新采购或补物资。

实际含overlay的GameView截图 `Unity/Assets/Temp/pipeline-screenshots/supply-request-portrait.png` 已查看，配额、已接收记录、包内1份和“已下单不代表已收货”分开展示，暂停只读可用。两个临时Editor.update流程分别在停止/完成后解绑，AppDomain临时报告清空、自有Play停止、GameView原index6恢复并移除任务新增尺寸；原用户成功文件不写入。已有原运输/菜单/守恒合同在本案4项运行中实际覆盖，无需再跑未变全仓套件。

主规格kitchen-supply合并2项要求，50份主规格严格通过，change严格通过；本案7份Markdown/13个本地引用及编码/空白检查通过，逐项任务已勾选。设备/物理LAN/真人协作及正式平衡仍未验证，未提交、推送或归档。

## 2026-10-17 归档与提交

用户已明确授权本批归档及本地提交。任务和原逐项运行结果保留，主规格已核对；归档后全量严格检查74/74通过（50份主规格、24案）。仅调整规划状态与相对引用，未因归档重跑未变行为。提交范围排除工作区既有Android启动/后台、LAN和HandmadeBistro改动；本机设置与记录元数据依赖的平台存储定义及文件读写保留。不推送，设备、正式平衡和真人体验仍按原未验证边界。
