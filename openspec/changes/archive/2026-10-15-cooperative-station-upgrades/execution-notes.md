# Execution Notes

## 当前状态与范围

2026-10-15：用户已授权完成全部 16 个 change。本案已开放合作原工位单级升级；主工作区 Unity 6000.3.18f1，精确 projectPath=`D:/MyWork/CarzyCooker/Unity`，相关 PlayMode UTF 10/10 Passed，含两个普通 Launcher 真实入口 UI 用例。未扩张真机/构建范围。

PC LAN 与 Android 原案保留，不是本案依赖。没有提交、push、归档或操作第二工作区。合作升级当前仍只改变本轮内存餐厅，不据此声称联机存档已支持。

## 严谨程度与已接受边界

风险是双装升级或重算旧加工。相关 KCP/受管 HUD 小集覆盖重复、换关、目标变化和作业前后耗时；不扩大原 Solo 整套回归。

只选择实际改变行为的最小相关 PlayMode UTF 与普通 Editor 入口；可复用下列已交付主规格关联的原证据，前提是实现、消费者和验收点均未变。 不建立全目录矩阵、额外状态副本或通用测试平台。

接受未验证：真机、设备输入、Player/AOT、物理 LAN/公网不在此次规划内；全目录平衡、未选择内容批次及跨设备持久故障不由本案证明。 需要相关产品交付时另定范围；本案新增/修改行为或出现具体回归时补相应最小验证，不因整理/审查重复跑旧套件。

## 任务与未来验收映射

| 任务/功能 | 计划验证 | 当前结果 |
| --- | --- | --- |
| 1.1 合法候选/选择/投影 | 四个合作 LevelConfig 奖励列表经五目标 Check/导出；`CoopRenovationHostChoosesRemoteReadsLateJoinAndInstallsExactlyOnce`、`CoopRenovationDrinkLayoutInstallsSupportedAdditionWithoutMintingItems`、`CoopUpgradeWrongPhaseCannotSelectBeforeSettlement` | Passed，基础候选 1/2/3/4、饮品候选 3/4/5/6，结算前拒绝，选择/继续经原邮箱，投影字段复用 |
| 2.1 HUD 只读/目标/结果/失效 | `CoopUpgradeUiOrdinaryHostChoiceRemoteTargetsAndNewWorkKeepStation`、`CoopUpgradeRealKcpSelectionReplayLateTargetConflictAndResumeKeepSameStation` | Passed，远端显示目标和预期等级，无选择/继续按钮，直接 KCP 越权拒绝；晚到等级变化投影失败、旧现场不改 |
| 3.1 重复/旧目标/旧作业/耗时 | 上述选择用例、`CoopUpgradeRealKcpOldWorkKeepsFrozenTicksAndNewWorkCombinesOnce(false/true)`，以及四个 `CoopRenovation*` 核心回归 | Passed，工位/位置/内容保持；旧切配60/烘烤80不重算，新切配23/烘烤45一次合并；重放、冲突、旧关、旧绑定、恢复同成员读取等级均覆盖 |
| 4.1 普通入口合作升级与新作业 | 新 `CoopUpgradeUiOrdinaryHostChoiceRemoteTargetsAndNewWorkKeepStation` 与原 `CoopRenovationUiOrdinaryHostChoiceClientReadOnlyAndManagedContinue` | Passed，真实 Pointer 自然结算、远端只读、受管交接、Host 升级原切配台并启动45 Tick 新工作直至实际完成；原新增工位/阻挡拒绝/继续/移动流程仍通过 |

重点映射：一次升级/旧作业 → 真实 KCP 命令及份数/工位身份/冻结时长断言；可见与权限 → 受管 UI UTF + 普通 Editor。

## 可复用的既有合同

- [station-upgrades](../../../specs/station-upgrades/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。
- [cooperative-renovation](../../../specs/cooperative-renovation/spec.md)：仅复用其中原执行记录已覆盖且实现未变的范围，不把该主规格存在视为本案运行通过。

## 决策门槛

无新增必要产品裁决，沿用已有单级规则。

## 源与生成

移除 RewardKind=2 的非 Solo 跳过；控制资格、预期目标/等级、原位安装和冻结作业逻辑沿用原路径。HUD 增加远端只读升级目标、已选目标/预期等级及升级失效原因。消息字段、快照复制和保存格式均已有相应载荷，无 Proto/CodeBind 结构变更；协议仍为上一案新增后的 23。

源 `Design/Excel/ET/Datas/Product/LevelConfig.xlsx` 仅改 L4/L5 为 1,2,3,4，L12/L13 为 3,4,5,6。使用标准库定点替换这四个单元格，验证其余压缩包条目字节不变，保留公式/样式/隐藏表/验证结构；未安装 Excel 工具。`Bin/Tool.exe --AppType=ExcelExporter --Console=1 --Customs=Check,ShowCmd` 五目标成功后运行正常导出，退出码均0，无失败输出。本次派生变化只有 Client、ClientServer、Config/Luban 的 dtlevelconfig.bytes 及 Editor ET JSON；其余代码/ID/本地化无实质 diff，资源引用不变。

## 实际运行

先发现512个真实 PlayMode 用例，再以 category=`CooperativeUpgrade` 运行本案最小相关10项，全部 Passed、0 Failed/Skipped/Inconclusive，退出码0，约114秒；覆盖齐全后没有重跑或扩大到 Solo 全套。原始 stdout 为 `Unity/Temp/CooperativeUpgrade-UTF.log`，Pipeline 原输出留在 `Unity/Temp/pipeline_test_status.json`，未复制、改写或清 Console。编译 completed/failed=false。两个 UI 用例真实加载 Launcher，角色为普通 Host 加真实 KCP Client 独立 Fiber，不是物理 LAN 或双 Editor 证据。

旧 Solo 升级/文件兼容/一次取整算法与消费者未变，复用[原升级记录](../2026-10-14-interlevel-station-upgrade/execution-notes.md)相应证据；本案单独实跑合作新增的权限、旧作业及同版投影。主规格和动态进度按本轮结果同步。

下一次实施需根据届时实际源码/协议/格式核对；本轮推荐的有限内容与参数不自动晋级正式平衡。结果字段留空，运行后以原日志路径和实际退出/失败原因简短回填，不保存完整业务状态。
