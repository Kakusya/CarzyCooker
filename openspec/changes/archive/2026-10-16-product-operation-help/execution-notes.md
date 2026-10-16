# 探索与执行说明

2026-10-16 原18案收尾复核：失败说明接入后，打开操作帮助会先撤下旧处理页。本次在EntryAdvice真实KCP用例验证Host暂停后Client原动作被拒绝Paused，说明跳原阶段帮助再返回说明；旧帮助链接不串页，Tick/命令不变，最后仅Host明确恢复。受变KCP单项1/1 Passed、Console完整且退出0；原六主题/输入行为未改，复用下述原结果，不重复整组。设计图澄清角色挂Level、厨房只持索引。

## 2026-10-16 实施与实际验收

原 GridPresentation/UI1101 已加入可开关的“操作帮助”。地图输入、拿放、容器、加工接续、当前阶段、角色权限共六类说明；主题和菜谱/补货/前厅链接使用原可滚动动作区，正文复用原详情区。进入时与菜谱/伙伴/其他详情互斥，关闭恢复原动作分组。说明明确工位单击会执行当前互动、拖动释放不追加点击；没有新快捷键、设备支持、教程状态或存档标记。

GridOverviewPage.Help、GridHelpTopic/HelpSerial 和返回组仅为本端显示选择；原 HUD EntityRef/epoch、world/关/generation/binding 限定回调，关闭/池重开/换关失效。正文只随帮助轮次、阶段和暂停变化生成，不随每个厨房Tick重建。阅读和导航无请求、无自动暂停/恢复、无文件操作；真正制作仍走原按钮和消息裁决。

PlayMode 实际发现709项，新 `EntryHelp` 类别2/2 Passed、Skipped=0；补齐正文依赖缓存后同一受变2项再运行2/2 Passed，Console覆盖完整且无新增Error、退出码0。编译0错误/0警告；最初按原EntityRef类型补充Runtime实体转换，域重载期间一次recompile 400未启动测试，ready后才执行。原始输出保留 `Unity/Temp/pipeline_test_status.json` 和本轮工具记录，未复制、改写或清Console。

| 任务／功能与Scenario | 实际用例／操作和断言 | 结果 |
|---|---|---|
| 1.1/1.2 正常：准备/营业阅读、菜谱往返 | `EntryHelpSoloTopicsRecipeContainerAndActualDragKeepCommandsExplicitAcrossReopen`：六类帮助逐一阅读，业务编号不变；原菜谱实际打开，再回帮助互斥原详情；实际开店后阶段说明变为营业，阅读不暂停 | Passed |
| 1.1 容器说明与原行为一致 | 同一Solo用例：真实拿鸡蛋/添加碗/拿原碗；查看容器说明不改手持或内容ID，再实际放/拿交接台，原碗和原内容一起移动，无复制/丢失 | Passed |
| 1.1/1.2 失效/取消：暂停与池重用 | 同一Solo用例：主动暂停后阅读不会恢复或推进Tick；关闭/重开原HUD后旧分类/关闭回调不改新页面，原明确恢复仍可用 | Passed |
| 1.1 实际输入与单次动作 | 同一Solo用例：原拿放每次一次请求；关闭帮助后真实ProcessGridPointer拖动使Board相机位置改变，释放不追加命令 | Passed |
| 1.2 真实KCP/角色限制 | `EntryHelpActualKcpClientReadsRoleAndPausedStateWithoutGainingHostControls`：Client从真实独立投影读队员权限，无Host开店/暂停/换关控件；Host原本地消息暂停/恢复，Client阅读和重开无业务命令，旧帮助导航无效；最后一次原KCP拿碗实际Executed | Passed |
| 2.1 540×960及Host说明 | 普通Launcher单机201查看输入/容器，实际滚动原动作区找到三个链接，菜谱往返均command0；关闭帮助后拿碗command1 Executed。实际拖动相机Y 7.111244→3.7096312，编号仍1。随后普通Host关401查看房主权限，编号仍为开局1；关闭后原拿碗command2 Executed，手持非零 | 已实际操作并查看画面 |

三张540×960实际画面：`Unity/Assets/Temp/entry-operation-help-map.png`、`entry-operation-help-containers-and-links.png`、`entry-operation-help-host-role.png`。短正文和六类入口可读可点，原滚动区底部的三个链接实际可访问。手动是准备态，营业/暂停及真实KCP由上述UTF覆盖；本轮自有Play已停止。没有产品功能测试失败，原UI与世界消息规则未改。

严谨程度：风险为错误说明输入或阅读偷偷改变状态，使用实际消息计数、容器ID、真实拖动、暂停、池重用和KCP控制限制覆盖已足够；不建立教程平台或全仓回归。原手工/自动加工规则沿既有主规格与原加工验收复用，不重跑制作全链。未验证真人理解、Player、Android真机与物理LAN；出现具体设备遮挡、输入规则改变或新角色权限时补对应范围。

收尾：本案5份规划/执行文档及手册、当前进度共7份Markdown，169个本地引用、编码/行尾检查与严格校验通过。三个任务按CLI返回行逐项核对并刷新确认3/3、all_done；原18案17/18、51/54。未提交、推送、同步主规格或归档。

下文为建案时的规划记录，当前结果以上方为准。

## 2026-10-16 依据与决定

当前 HintText 只简述点击目标和拖动，菜谱／首单说明则聚焦制作。新玩家缺少随时查看地图输入、拿放、工作接续、暂停和模式权限的统一帮助。

依据：[实际源码](../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/GridKitchen/GridPresentationSystem.cs)、[UI 主规格](../../../specs/restaurant-ui/spec.md)、[长期目标](../../../../long-term-goals.md)。本案为本轮用户要求扩展入口的规划，不表示功能已实现。

方案：在原 Presentation 增加短寿命帮助分类与详情选择，使用现有文本／滚动区；按当前模式和阶段生成适用说明，与菜谱／队友详情互斥。不添加教程业务状态机、遮罩强制步骤或存档标记。

## 严谨程度与验证取舍

主要风险是帮助文案承诺了没有实现的输入或把阅读变成强制教程，后果为局部界面误导或误操作。沿原消息水位、输入 guard 与 owner，以最小相关 PlayMode UTF 和普通 Launcher 入口检查覆盖足够；不引入新框架或全仓回归。原命令／守恒／生命周期的行为未变部分复用适用归档记录；新增入口必须实际运行。

本轮全部产品验收 NotRun。真人理解、目标设备、物理 LAN 及未实现输入不由脚本证明；发生具体功能或设备回归再补对应范围。

## 功能验收映射

| 任务／功能 | 实际验收计划 | 当前结果 |
|---|---|---|
| 1.1 解释事实与边界 | 实际点击／拖动、容器操作与阶段／角色说明、阅读无命令及重开，有行为断言的最小 PlayMode 小集 | NotRun |
| 1.2 界面与生命周期 | 普通 Launcher 本地及真实 KCP Client，取消／重开／旧回调及原动作可用 | NotRun |
| 2.1 集成 | 540×960 可读可点，逐项 Scenario 与实际运行对应并核对文档／严格格式 | NotRun |

## 规划校验

本案 proposal／design／specs／tasks 均已齐备，`openspec validate product-operation-help --strict` 退出码0。全批18案的91份 Markdown／314个本地引用检查通过，无 UTF-8 替换字符或行尾空白。全批54项实施任务均未勾选；本案产品功能仍 NotRun，没有启动 Unity 或运行 UTF。原始输出留在当前工具记录，不创建额外证据包。
