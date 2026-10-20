# 执行说明与验收边界

2026-10-20归档收口：用户明确授权归档和本地提交。原功能与行为未变，复用本案逐项适用的Passed结果，不重复执行玩法套件；主规格对应关系及归档引用重新检查。

2026-10-20：已实施、完成功能验收并取得用户实际试听确认，5/5任务完成。本记录替代初始规划/中途3项通过但仍缺覆盖的状态。

## 严谨程度与投入

主要风险是异步加载串场、重复成功提示、误停其他声音或本机设置未保存。用真实GF资源/终态事件、拒绝、并发预算、冷加载退出、缺资源失败、本机文件与真实KCP暂停/重连验证，另由用户试听。不建立音频框架或全平台矩阵；接受设备音量差异、专业混音和Player/Android未验证，出现爆音/延迟/泄漏再补对应设备或生命周期。

## 资源与owner

原创合成源与参数在[Audio README](../../../../Art/CarzyCooker/Audio/README.md)，六个44.1kHz/16bit单声道WAV：拿取0.11秒、放置0.12、开工0.18、完工0.34、交付0.42、结算0.72。Sound.xlsx新增1101–1106，Sound.Default、Loop=false、SpatialBlend=0；实际Check/五目标正常生成及UGFSoundId完成，原ResourceRule增加Sound.Product并正常With Optimize刷新，无手改生成物。

ProductClient的ProductFeedbackComponent只持观察身份/游标、短生命周期command上下文及所属GF serial。原发送前登记完整上下文，匹配真实Executed才播放；Accepted/Pending不作成功，拒绝/发送失败清除。Grid结果不递增餐厅OutcomeSerial，按完整上下文和Pending移除去重；非Grid仍用原序号。明确ProductClient事件只观察已提交投影，原Host、Mailbox、Session及DTO所有权不变，见[design](design.md)。

首装/重连基线不播历史，HUD重开不换owner；同一步完工合并声音而视觉保留。并发最多3个，交付/完工/结算优先，不补播丢弃音；暂停/失联/退役只停所属serial，含GF加载中取消。声音页即时设置GF本机分组键，默认60%未静音，保存失败明确提示；原字号/提醒草稿取消语义不变，无存档/协议新增字段。

## 任务／功能 → 实际运行 → 结果

短名均为`ET.SoloRestaurantUiTests`实际PlayMode用例，除明确标注的ProductSessionTests。

| 任务 | UTF或实际操作及断言 | 结果 |
|---|---|---|
| 1.1 | `ProductFeedbackSixActualResourcesPlayAndOwnedStopPreservesAnotherSerial`：六资源逐一真正加载并开始播放、clip时长有效、无加载失败；源/表/资源路径核对 | Passed，最新1/1 |
| 2.1 | `ProductFeedbackActualPickupPlaceBakeFinishAndMuteDoNotReplay`真实拿面包/放烤箱/开工/完工/入盘，重复结果/HUD不重播，真实错误方向拒绝无成功音；完整三关旅程断言Serve和LevelComplete各3次；SixActualResources补3声预算/低声丢弃/Serve抢占；`ET.ProductSessionTests.EntryNoticesActualSameStepSoupBakeAndShipmentsAllAppearOnceAndStayBounded`保留同一步全部视觉事实 | 各相关用例Passed |
| 2.2 | `ProductFeedbackColdLoadingOwnerExitCancelsBeforeAnyPlaybackCallback`实际IsLoadingSound时退出，终态成功回调0；SixActualResources只停自身、不停另一播放；`ContextualActionsRealKcpOldCandidateCannotTakeReturnedSameInstance`真实远端加入静默、取物一次、HUD不重播，实际暂停停所属声音、恢复无补播、重连新基线静默；`ProductFeedbackActualMissingResourceKeepsProjectionAndNextCommandWorking`实际GF缺资源，错误可定位、播放记录释放，画面继续且原取碗Executed | 各相关用例Passed |
| 3.1 | `ProductFeedbackImmediateSettingsActualDiskFailureReloadAndOriginalDraftCancel`真实入口/厨房设置、0/60/静音/恢复、实际文件重载、被文件占用父路径保存失败与重试、产品重进、原字号取消；返回声音页后地图真实点击正常且设置本身零业务命令 | Passed，最新1/1 |
| 3.2 | 普通Launcher汤/吐司/沙拉生产、真实交付和自然结算旅程Passed；样板页六段WAV用户明确回复“已试听，可以接受” | 技术Passed；人工接受 |

以上5个不同ProductFeedback用例均有适用Passed结果，分别来自相关小集及单项作业，不声称单次5/5。真实KCP为同Editor独立Fiber/客户端和套接字，不是物理LAN/双进程。`BistroFramesSocketsWorkAndSingleCommittedServeFollowProjection`验证递餐动画不重复；原同一步视觉用例显式推进时钟，仅证明事件事实。

## 试听、修正及复核

用户确认对应[样板页](C:/Users/Administrator/.answer-me-with-html/pages/六案实施样板-视觉与试听-20261020-184602.html)的六个嵌入播放器；未提供设备型号、是否实际在游戏60%音量试听，不推定专业混音或多设备校准完成。

实际修正Grid终态观察序号、测试早于真实事件断言、EntityRef/选择页guard，以及声音页返回后地图未重新启用的生产问题。新增缺资源测试先误读不存在的Grid.HandItemId字段，已改为当前actor.HandItemId并重新编译通过，随后新作业1/1 Passed。没有改写失败输出或用户设置文件；设置夹具只操作自有临时路径并恢复原键/路径。

完整性5/5任务、5条要求（含原视觉提醒MODIFIED）；正确性10场景已映射；一致性符合原GF/ET owner和观察边界。主规格已同步，六案及51份主规格严格校验通过。原输出保留`Unity/Temp/pipeline_test_status.json`，随作业正常更新。已归档并获授权本地提交，未推送。
