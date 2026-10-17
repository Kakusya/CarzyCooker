# 执行说明

2026-10-17，用户要求将十二方向总结为change并全部完成；本案对应第6项本关复盘，范围不以只交付规划缩减。已接入原Completed界面、按菜计数和分数、返回结算及原owner寿命；正在逐点验收，未完成任务仍不勾选。

## 严谨程度与取舍

主要风险是重复计数和旧关结果串到新关；不写文件或改变业务规则。当前Orders完整投影、原结算和本地/KCP路径可复用。最小相关PlayMode UTF用真实交付/超时/自然完成，加一次普通入口与旧回调即可，不重跑全仓。已有前厅/结算裁决行为未变时复用原归档证据，但本案新显示必须实跑。真人理解、正式平衡和设备层不在本案验收，发现实际可读性/计数差异时补验。

## 功能映射

| 任务/功能 | 运行依据 | 结果 |
| --- | --- | --- |
| 1.1 按菜结果与分数 | `EntryServiceOverviewSoloActualDeliveryExpiryAndFinalScoreRemainReadOnly`真实汤交付10分/吐司超时后明细总和；`EntryServiceOverviewActualFullQueueZeroScoreAndNextLevelRetireOldRows`真实零分自然完成 | Passed；阅读不增命令，不改原权威星级/装修/继续 |
| 1.2 合作与寿命 | `EntryServiceOverviewActualKcpIdleViewerReceivesHostDeliveryAndReopensWithoutCommands`真实Host交付/远端清桌完成，双方复盘正文完全一致；上述Solo/FullQueue触发池化/换关前捕获的旧复盘返回回调 | Passed；原权限保持、旧回调不作用新关 |
| 1.2 普通入口 | 普通Launcher540×960→单机→内存试玩→确认→原加成/开店→自然零分Completed；复盘实际三菜各超时2单共6单，返回原结算阅读command4→4；原下一关command5 Executed，新Level Preparing、订单0 | Passed；截图service-recap-zero.png已查看 |

## 实施中的运行记录

PlayMode过滤category=EntryServiceRecap，发现真实3项并执行：2 Passed/1 Failed。Solo实际汤交付/吐司超时和Host/KCP真实出餐、远端清桌/自然收尾后两端复盘正文一致均Passed；零分旅程已查看零分复盘并返回结算，但随后夹具仍找“关闭营业总览”，该按钮此时已是“营业总览”。修正为当前原“关闭结算详情”后仅重跑该单项；不改产品规则，不增加超时。原始失败位于Unity/Temp/pipeline_test_status.json和Unity/Logs/Business-Delivery-Editor.log，未复制或改写。

修正后PlayMode精确过滤EntryServiceOverviewActualFullQueueZeroScoreAndNextLevelRetireOldRows，新运行1/1 Passed、exit0、Console覆盖完整。故本案3个不同相关旅程各有当前适用Passed结果；不因文档收尾重跑已通过的另外2项。

普通Launcher已完成上述旅程；自有Play已停止，仅移除自建540×960视图项并恢复原index6，未写用户成功记录。截图保留Unity/Assets/Temp/pipeline-screenshots/service-recap-zero.png。2/2任务完成，主规格restaurant-ui已同步本案2项新增要求；48份主规格严格验证通过。原规划检查不充当功能结果，未提交、推送或归档。

## 2026-10-17 归档与提交

用户已明确授权本批归档及本地提交。任务和原逐项运行结果保留，主规格已核对；归档后全量严格检查74/74通过（50份主规格、24案）。仅调整规划状态与相对引用，未因归档重跑未变行为。提交范围排除工作区既有Android启动/后台、LAN和HandmadeBistro改动；本机设置与记录元数据依赖的平台存储定义及文件读写保留。不推送，设备、正式平衡和真人体验仍按原未验证边界。
