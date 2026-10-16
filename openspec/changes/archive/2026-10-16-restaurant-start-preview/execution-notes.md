# 探索与执行说明

## 2026-10-16 实施与实际验收

原 SoloSavePresentation／ProductEntryPresentation 保存本轮选项；正式确认才调用原 ChooseSoloNewGame／SendRoomEntry。目录从 RestaurantChapters.First 对应的实际 NextLevelConfigId 链和 Dish 定义生成，摘要与逐关菜单放在原结算区／滚动列表，不改表、协议、Prefab或生成绑定。Solo 增加 renderVersion 隔离同一HUD的旧选择；Host 绑定房间版本及Client实例，Client仅能查看本端目录。

| 任务／Scenario | UTF或实际操作 | 最新结果 |
|---|---|---|
| 1.1／1.2 正常、取消、原档不变、单次确认 | `EntryStartPreviewSoloCancelsReopensAndStartsExactChapterWithoutChangingExistingFile`：真实文件、第二章预览、取消／改选内存、HUD重开、旧确认、实际首关1501、原拿碗命令 | Passed |
| 1.1／1.2 Host开局、Client只读、旧房间按钮 | `EntryStartPreviewHostAndActualKcpClientReadOnlyCancelAndRetiredConfirmation`：真实KCP Client目录、无确认权限、零浏览命令；Host仅提交一次开局，双方同一首关，退役后旧确认无效 | Passed |
| 2.1 540×960普通入口 | 实际Launcher→单机→第二章，检查摘要和滚动菜单后确认，实际LevelConfigId1501／Confirmed；拿碗提交仅编号1（手工查看时PendingApproach，执行完成由上述UTF证明） | 入口与显示通过 |

PlayMode过滤 `EntryStartPreview`，首次2/2 Passed。实际540画面暴露原结算长文本缩得过小，已改为摘要＋原滚动列表；改变后的两项新运行再次2/2 Passed，Console覆盖完整、退出0。原始输出保留原生 `Unity/Temp/pipeline_test_status.json`，不复制。最终截图 `Unity/Assets/Temp/entry-start-preview.png` 已实际查看，摘要和列表可读，确认／返回可点。之后只修正预览保存提示的静态文案，保存行为未变。

原关卡流程测试的开局选择已显式改为“选择＋确认”两个真实指针点击（StartPreviewAndConfirm），不通过测试直接启动替代新入口。原API保持供显式启动参数使用。已同步操作说明和当前进度；真人易用性、目标设备、物理LAN仍未覆盖。未提交／推送／归档。

以下保留规划阶段记录，其中NotRun表示当时状态。

## 2026-10-16 依据与决定

新开章节和多种菜单按钮目前点击即建立餐厅，玩家开局前看不到所选章节的关序、菜单与保存方式。

现状依据：[实际视图源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/SoloSavePresentationSystem.cs)、[主规格](../../../specs/product-entry/spec.md)、[当前进度](../../../../CarzyCooker当前进度.md)。本案是建议新增的入口体验，不将现有核心系统误列为未实现；规划授权来自用户要求继续寻找并补充入口业务／UI change。

采用方案：沿 RestaurantChapters 的实际配置查链，保存本轮选中的 chapterId／menuVariant；正式点击才调用 ChooseSoloNewGame 或 SendRoomEntry。取消不分配世界，旧版本房间回调继续拒绝；不增加房间待选章节状态或 wire 字段。

已排除新增玩法规则、自动操作及协议／存档改版。已确认合同继续适用，未决传输、正式数值和额外人物不在本案裁决。用户未选择新增合作业务状态前，准备确认／分工意向／补货请求只列路线候选。

## 严谨程度与可复用证据

主要风险为预览参数与实际开局参数不一致，后果是显示误导或操作失效，属于可恢复的局部体验问题。采用原 owner、只读派生和原命令入口，最小相关 PlayMode UTF 加普通入口检查足够，不做全仓回归。

原行为未改变且功能映射仍适用时，复用对应归档 execution-notes 的运行结果；新增入口与改变行为必须单独运行。原始输出保留原位，失败与重试分开记录。若发现必须改表、生成物、UI 结构或增加 wire 字段，先修订计划及相称验收，不把它们作为隐含前置。

接受未验证项：本轮产品功能 NotRun；真人易用性、正式平衡、目标设备和物理 LAN 不由本机脚本证明。有具体目标设备遮挡或新行为变更时再补相应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 事实派生与业务边界 | Solo／Host 确认与取消、Client 只读及原档不受新开预览影响；新增行为断言，文件相关使用真实临时文件 | NotRun |
| 1.2 入口与本轮 UI | 普通 Launcher 实际进入、取消、重开、原合法动作；涉及合作时覆盖 Host 本地及真实 KCP Client | NotRun |
| 2.1 集成与交付 | 540×960 可读可点、原功能继续可用；对应每项 Scenario 记录真实用例／操作和原始输出 | NotRun |

## 规划校验

规划阶段历史记录（不代表当前状态）：本案 proposal／design／specs／tasks 均已齐备，`openspec validate restaurant-start-preview --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
