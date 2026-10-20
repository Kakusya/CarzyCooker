# 执行说明与验收边界

2026-10-20归档收口：用户明确授权归档和本地提交。原功能与行为未变，复用本案逐项适用的Passed结果，不重复执行玩法套件；主规格对应关系及归档引用重新检查。

2026-10-20：已实施并完成功能验收，5/5任务完成。只整理原动作呈现及说明，未改装盘方向、物品守恒或裁决规则。

## 严谨程度与投入

主要风险是误推荐、遗漏原动作和旧候选跨目标执行。沿原候选/意图构造，验证代表生产、错误方向和一次真实KCP竞争即可，不建规则引擎或穷举配方。接受真人顺手程度、全内容组合及设备触感未验证；出现具体误推荐再补对应场景。

## 实施与owner

原按钮最多一主两次展示，其他原候选保留在更多动作，没有复制请求或建立第二份业务状态。短原因涵盖手满、锁定、容器不符、原料/份数不足。拿盘对烤箱提示先放回盘、取吐司、再向盘添加，不自动反向装盘、交换或重试。

候选按原world/level/binding、选择及实物事实失效。原SendGridAction→本地ActorId/Ordered MailBox或远端Session→同一Host裁决保持；[design](design.md)列明owner及消息两端，显示层只观察提交值。

## 任务／功能 → 实际运行 → 结果

短名均为`ET.SoloRestaurantUiTests`实际PlayMode用例。

| 任务 | UTF或实际操作及断言 | 结果 |
|---|---|---|
| 1.1 | `ContextualActionsOriginalCandidatesCompactSummaryAndWrongDirectionPreserveItems`：摘要最多3项，其余原按钮可达，阅读零请求；真实拿碗/放置/烘烤走原意图；完整旅程补齐盛装/Serve | Passed，最新1/1 |
| 1.2 | 同一本地用例：锁定烘烤短原因、拿盘对烤箱真实拒绝，物品身份/数量不变，非法动作不升主动作、详情给合法步骤；`ProductFeedbackActualPickupPlaceBakeFinishAndMuteDoNotReplay`补真实拒绝无成功音 | Passed |
| 2.1 | 本地用例、`SoloRestaurantUiLifecycleInputAndProjectionWithSoloBuffDrawReopen`及首单旅程：目标变化撤旧、池重开旧回调失效、暂停阅读及拖动不误发，后续原动作仅一次 | Passed |
| 2.2 | `ContextualActionsRealKcpOldCandidateCannotTakeReturnedSameInstance`：远端选碗，Host取走又放回同一实例，旧回调两次均无效；Host正常让路后远端重选/真实点击只拿一次；各自权限保持 | Passed |
| 3.1 | `FirstSessionPathActualThreeLevelsSoupToastSaladAndNaturalClosing`普通Launcher正常时间完成汤的拿料→切配/煮汤→分装→Serve及吐司/沙拉，摘要/更多与首单提示共同工作 | Passed，520.6秒 |

ContextualActions小集2/2 Passed，之后仅补跑新增锁定原因断言的本地用例1/1 Passed。KCP是同Editor独立Fiber/客户端及真实套接字，不是双进程或物理LAN。Host保留原表现Component并隐藏HUD，避免投影重建竞争视图。未新建网络/端口工具或修改第二工作区。

## 最终复核

完整性5/5任务、3条要求；正确性6场景覆盖可达、真实拒绝、目标失效；一致性符合原owner/裁决/输入纪律。主规格已同步，六案及51份主规格严格校验通过。原输出保留`Unity/Temp/pipeline_test_status.json`，失败不改写，后续作业正常更新。已归档并获授权本地提交，未推送。
