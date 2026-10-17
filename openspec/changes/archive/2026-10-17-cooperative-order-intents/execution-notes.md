# 执行说明

本案为十二方向第1项。用户已确认制作意向只提示、不独占订单，且原整批创建/实施授权持续有效。领域已实现并完成核心运行；UI及普通入口按下方当前结果记录，尚未全部验收时任务不勾选。

## 严谨程度与取舍

主要风险是意向被误作独占和旧订单/成员残留；不写资产或进度。用原Host统一裁决、独立投影及少量真实两成员/出餐/退出UTF足够；复用未变原制作、交付和工作认领证据，不能以它们替代新共享提示。接受真人分工体验、设备与正式传输未验证；实际重复阻止交付或残留串单再补对应测试，不建协调框架或全量矩阵。

验收发现原共享Session适配器收尾问题后，验证投入仅增加实际双等待者销毁/同Id新Session成功和原单owner取消两项；它们直接覆盖实例身份、等待者取消和消息回收边界，UI另覆真实KCP途中断线。正常成功、暂停及旧身份已有本案适用结果，不扩大为全部异步或全仓测试。

## 功能映射

| 任务/功能 | 最小运行 | 结果 |
| --- | --- | --- |
| 1.1 非独占和清理 | 下述2项真实KCP核心 | Passed：两人同单、第三人实际做汤/交付、本人改选/撤销、暂停、失联/恢复/Leave、同桌换客、超时/跨关与旧身份；独立DTO复制 |
| 2.1 集成寿命 | 新增真实KCP UI、上述核心及普通540×960 | Passed：Host/Client原按钮同单/取消、池重开旧回调、原交付和手动恢复；普通实际做汤、意向改选、暂停阅读及交付10分 |

无需再询问独占规则。首版一人一个当前意向、同单多人为有限交付选择；不扩展订单锁/批量意向/存档，不自动提交或归档。

## 实施与原始失败

Level持有成员→独立意向值；原HostPeer到HostSession有序邮箱提交，原命令身份/去重/终态复用。原Grid提交前按成员绑定、Waiting订单及期限清理，变化发同版投影，HUD结构键包含意向。Proto源尾部追加4消息和1投影DTO，原193个opcode全部保持；wire36→37，成功文件12不变。没有增加监听类型或持久字段。

首跑核心2 Failed：旧共用 `CoopFrontFixture` 只修改Solo level1客流，合作实际使用其他关，默认来客/期限干扰目标。新增本案合作夹具按实际首关配置稀疏客流/长等待，不改产品表；新跑第一项Passed。第二项到原下一关时等待装修候选不足，补原Prepared/合法选择步骤，另加入真实Leave覆盖后单项Passed。未把首次失败改写为通过。

- `EntryOrderIntentActualKcpTwoMembersSameOrderThirdCooksServesOnceAndCopiesIndependentValues`：本地/远端同单，第三连接成员沿真实切配、打蛋、煮汤和分装交付；只计一次分，原碗留桌，双方意向清理，已结束订单拒绝；DTO改写/回收不影响已装独立值。
- `EntryOrderIntentActualReplaceCancelPauseDisconnectResumeSameTableExpiryAndNextLevelRejectOldIdentity`：本人改选/撤销不动另一人；暂停拒绝撤销、断线清理和手动恢复不复制意向，旧绑定拒绝；原桌自然离席/清桌后新顾客与旧订单分离；真实Leave、超时、自然结算/原装修/Continue清空，旧关请求拒绝。

UI首跑在已完成双人意向/取消/池重开/原交付后，强制断开刚提交第二单、仍待RPC答复的Session，引发原异步适配器Error。定位是Session销毁先令InstanceId失效、再完成挂起RPC异常；仅在该实例失效条件将RPC异常转为取消，原finally仍回收请求/响应，不改变正常错误/超时或停止共用sender。它涉及共享等待者收尾，因此增加真实Session双owner待请求销毁/同Id新Session成功的UTF，并补跑原取消单owner用例，两项2/2 Passed，Console完整。UI另行新跑，不能以适配器通过代替界面验收。

原始输出保留于 `Unity/Temp/pipeline_test_status.json` 和 `Unity/Logs/Business-Delivery-Editor.log`；不复制或改写。接受真人分工和物理LAN/独立Player未验证；出现意向挡交付或退出残留才补对应路径，不默认全仓回归。

## 最终适用结果与普通检查

758个PlayMode已发现；本案当前5个不同适用实例均有最新Passed：2核心＋1实际Host/KCP UI＋2Session收尾用例，无Skipped/空集，Console覆盖完整。`EntryOrderIntentActualHostAndKcpUiSameOrderOriginalDeliveryPooledAndRecoveryCallbacks` 新运行Passed，原Launcher真实预制汤、营业开单、Host和Client同单/取消、HUD池化旧回调、Client取原碗/真实交付及途中断线/手动恢复新绑定均通过；不将首跑Session Error改为成功。

普通检查为UTF外原Launcher的Host内存菜单关401，540×960；原发布/准备/开局后真实指针回调沿切配→打蛋→煮汤→分装，查看订单命令数不变，设置/撤销/重设各一次（最后设置command23）。原暂停command24冻结订单，实际GameView含overlay截图 `Unity/Assets/Temp/pipeline-screenshots/order-intent-portrait.png` 已查看，制作意向成员1及“不独占/可多人”说明可读。恢复command25后原拿碗/到桌/交付command28实际Executed，原订单Served、10分、只交付1单、意向0；未写成功文件。普通自动化首段曾把PendingApproach错判终态而停止，原command6随后正常Executed；修正等待条件后从已放置原番茄继续，没有改业务状态或免费补物资。两个临时Editor.update回调在完成后各自解绑；自有Play已停，GameView原选择恢复并仅移除任务新增尺寸。

主规格orders-front-service已合并2要求，49份主规格严格通过；change严格通过。未提交、推送或归档。

## 2026-10-17 归档与提交

用户已明确授权本批归档及本地提交。任务和原逐项运行结果保留，主规格已核对；归档后全量严格检查74/74通过（50份主规格、24案）。仅调整规划状态与相对引用，未因归档重跑未变行为。提交范围排除工作区既有Android启动/后台、LAN和HandmadeBistro改动；本机设置与记录元数据依赖的平台存储定义及文件读写保留。不推送，设备、正式平衡和真人体验仍按原未验证边界。
