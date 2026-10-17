# 执行说明

本案对应第9项，用户确认Solo玩家或Host选自动/询问优先/洗碗优先，已开始工作不中断；整批实施授权持续。本案只改原工作资格内顺序，自动明确沿原询问优先，不增加小麦能力。4项核心UTF及2条UI/KCP旅程均有最新适用Passed，普通Launcher540×960验收完成。

## 严谨程度与取舍

主要风险是切换打断原工作或Client越权，范围为可恢复的运行偏好，不改表/长期成长。用真实双候选、原工作进度、跨关和实际KCP小集及普通界面足够；复用未变原单执行者/第三次成长证据，不重跑全仓。接受真人偏好、正式数值和设备层未验证；实际收尾停滞、冻结耗时变化或权限差异再补对应运行。

## 功能映射

| 任务/功能 | 最小运行 | 结果 |
| --- | --- | --- |
| 1.1 排序/权限/不中断 | 下述三参数核心测试及自然收口/文件测试，KCP权限旅程 | Passed：三偏好选合法候选、同类原Id顺序及无候选回退；待认领/前往/工作中切换不改当前事实，暂停/非法值/Client/旧关拒绝，真实自然收口 |
| 2.1 实际界面 | `EntryCompanionPreferenceActualSoloChoicesWorkWashNaturalCloseNextAndRetiredCallbacks` | Passed：三选择的原UI提交及当前值；HUD池化旧按钮无效，暂停旧按钮不发命令；真实询问/出餐/清桌/洗碗、成长至少3、自然完成及原下一关；新关成长0而偏好保留 |
| 2.1 Host/KCP | `EntryCompanionPreferenceActualKcpReadOnlyPermissionWorkWashNaturalCloseNextAndRetiredCallbacks` | Passed：Host原本地控制和KCP同值，Client无改值按钮且直接发请求也被HostControlOnly拒绝；KCP角色实际交付/清桌，小麦真实洗碗/成长，自然换关后同值，旧关按钮不能修改 |
| 2.1 文件/新运行 | 下述核心自然收口/实际成功文件测试 | Passed：同Restaurant跨关保留，角色/成长清零；合法成功文件重建及关店后新owner新建均自动；原文件字节保持 |
| 2.1 普通入口 | 普通Launcher内存试玩540×960 | Passed：伙伴分类选洗碗优先command1，原加成/开店command2/3；小麦原Ask进度0.1时用原UI切询问优先command4，原同一WorkId完成、生成真实订单、完成数1，command4实际Executed；当前岗位和原工作反馈可读 |

本轮连续关卡保留偏好，关店或成功档重建运行恢复自动；不为此给成功文件升版。无需再问同一组分工规则，不自动提交或归档。

## 核心运行（2026-10-17）

- `EntryCompanionPreferenceActualCandidatesFallbackAndPendingApproachingWorkingSwitchKeepOriginalWork(Automatic/AskFirst/WashFirst)`：3/3 Passed。原合法实体夹具建立两桌Ask和一件脏餐具Wash，实际调度/本地认领/业务步完成三工作；分别验证原Id排序与偏好类别先选，无优先类回退。请求尚待认领、前往中及工作5步后用真实偏好请求切换，原WorkId/ClaimSerial/路径/冻结耗时/进度/成长不变；暂停禁止改、进度冻结；恢复后原餐具洗净、实际累计成长3。非法偏好99拒绝。
- `EntryCompanionPreferenceActualNaturalCloseNextLevelStaleCommandAndSuccessFileRestoreAutomatic`：最新单项1/1 Passed。实际内容章自然收口、完成后设置拒绝、原装修/继续，成长清零而本轮偏好保留；旧关请求拒绝。真实成功文件读入新运行默认自动、原字节不变；关闭后再新建运行也自动。
- 首跑排序3 Failed：测试夹具把两位顾客放同一桌，原合法目标校验正确跳过失效的旧顾客；将第二顾客放第二桌后新运行3 Passed。追加“关闭后新开”覆盖时错误复用已Started的Runtime导致 `ProductAlreadyStarted`；改用原新运行入口创建新owner，单项新跑Passed，未改业务以迎合夹具。
- 原189个opcode逐项保持，Proto尾部新增4个控制/内部提交消息；原快照源追加偏好字段，wire版本34→35，实际生成Client/ClientServer。仅控制请求/响应进入原wire白名单，内部提交及本机设置/标签仍不对远端开放；实际KCP旅程通过，不把生成本身当联网证据。

上述6个不同适用测试实例（核心三参数加一条文件/寿命、两条UI）全部Passed，无Skipped/空集。原始失败与后续新运行分别保留在Pipeline原结果位置 `Unity/Temp/pipeline_test_status.json` 和 `Unity/Logs/Business-Delivery-Editor.log`，未复制/改写；没有因文档阶段重跑全部套件。原单执行者/第三次成长公式复用未变实现及既有证据，本轮三实际工作和UI成长/洗碗作适用回归。

普通截图 `Unity/Assets/Temp/pipeline-screenshots/companion-preference-work.png` 已实际查看，当前岗位、完成数、实际订单与原地图可读。自有Play已停，GameView恢复索引6，移除本次540×960尺寸及SessionState键；内存试玩未新增或改写成功文件。主规格及玩家说明已同步，严检/引用检查按实际命令结果；未提交、推送、归档或操作第二工作区。

## 2026-10-17 归档与提交

用户已明确授权本批归档及本地提交。任务和原逐项运行结果保留，主规格已核对；归档后全量严格检查74/74通过（50份主规格、24案）。仅调整规划状态与相对引用，未因归档重跑未变行为。提交范围排除工作区既有Android启动/后台、LAN和HandmadeBistro改动；本机设置与记录元数据依赖的平台存储定义及文件读写保留。不推送，设备、正式平衡和真人体验仍按原未验证边界。
