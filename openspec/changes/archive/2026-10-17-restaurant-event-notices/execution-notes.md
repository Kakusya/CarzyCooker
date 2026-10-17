# 执行说明

本案对应十二方向第5项经营提醒，用户已要求实施全部方向；实现及下述实际验收完成。复用本批本机设置，不新增通知平台、音效素材或网络协议。

## 严谨程度与取舍

风险是同一步漏结果、重连历史重播和旧回调误操作。已有Outcome独立值事件与版本校验可复用，需补真实同一步多结果、首次基线及恢复/换关验证；不重构原消息管线。采用有限最近8条可查看值，不声称完整历史。最小相关PlayMode UTF加普通入口和KCP足够；真人注意力效果、外部系统通知及设备测试保持未验证，若实际提醒噪声或漏报出现再按具体类别调整。

## 功能映射

| 任务/功能 | 验收入口 | 结果 |
| --- | --- | --- |
| 1.1 三类事件 | `EntryNoticesActualSameStepSoupBakeAndShipmentsAllAppearOnceAndStayBounded`；`EntryNoticesActualDeadlinesPauseExpiryPreferencesAndFirstProjectionDoNotReplay` | Passed：真实汤/烘烤及两货物同一步结束全部出现，重复值不重播，最近8条有界；真实订单临期/到期、暂停、开关无历史补播 |
| 1.1/1.2 生命周期 | `EntryNoticesActualWorkAndShipmentContinueAcrossLevelAndPausedCompletion`；以下两项UI用例 | Passed：自然完成/继续后同一汤和货物保留身份，暂停不完成、恢复各出现一次；HUD重开保留列表，新绑定重建基线，已丢弃成品不改选替代物 |
| 1.2 实际UI | `EntryNoticesActualSoloTargetConsumptionSettingsAndHudPoolRemainReadOnly` | Passed：真实烤制→提醒查看→原取物/丢弃→失效说明，HUD池化旧按钮无效；真实采购/到货→原补货入口→接收；本地设置保存立即清关闭类别 |
| 1.2 实际KCP | `EntryNoticesActualKcpFirstBaselineLiveArrivalDisconnectAndResumeDoNotReplay` | Passed：Host已有到货不补播，真实Client采购后新到货出现；查看无业务命令，真实断线/手动恢复不重播且旧行无效 |
| 1.2 普通入口 | 普通Launcher内存试玩，540×960；`notices-portrait.png` | Passed：面包原拿/放/烘烤命令1/2/3后出现完工提醒；打开与查看均保持命令3，定位原成品；再原取物命令4实际Executed、手持原成品。截图已查看，正文可读，无缩字，原UI操作保持 |

## 运行与边界（2026-10-17）

五项不同适用UTF均已Passed，无Skipped或空集。核心首跑2 Passed/1 Failed：跨关测试角色仍在赶路，开工前关已完成（`LevelCompleted / Cancelled`）；按原移动动作预先到灶台附近，仅新跑失败用例1/1 Passed。UI首跑2 Failed：一个错误假设恢复必须回None页（原供货页可保留），一个遗漏“全部动作”分类切换导致采购按钮未列出；修正测试按原契约断言后新跑2/2 Passed。没有修改业务行为以迎合错误测试。

原始运行输出保留 `Unity/Temp/pipeline_test_status.json` 和 `Unity/Logs/Business-Delivery-Editor.log`；未复制或改写失败结果。测试仅在隔离目录保存本机设置，不更改用户成功记录。普通入口仅内存试玩，退出自有Play，恢复原GameView索引6，移除本次540×960临时尺寸并清理本次SessionState键；未新增存档。

显示保留最近8项直到被新项挤出/类别关闭/相应owner退出，不设墙钟自动消失。暂停冻结临期和业务事实，没有帧计时寿命。首次投影、新绑定是基线；换关仅清旧订单提醒，持续货物和作业不重复。实际到货枚举为 `ShipmentStage.Ready`。提醒目标只读跳原查看入口，无新消息、等待者或写入权威。

已同步主规格和玩家说明；严格格式及文档引用检查另见本次命令输出。真人注意力、实体设备和正式平衡未验，不扩展构建。本案未提交、推送或归档。

## 2026-10-17 归档与提交

用户已明确授权本批归档及本地提交。任务和原逐项运行结果保留，主规格已核对；归档后全量严格检查74/74通过（50份主规格、24案）。仅调整规划状态与相对引用，未因归档重跑未变行为。提交范围排除工作区既有Android启动/后台、LAN和HandmadeBistro改动；本机设置与记录元数据依赖的平台存储定义及文件读写保留。不推送，设备、正式平衡和真人体验仍按原未验证边界。
