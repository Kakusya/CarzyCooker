# Design

## Context

正式第一章为 `RestaurantChapterConfig.Id=1`，Solo 关链1101→1102→1103，合作关链2101→2102→2103；独立试玩201不属于本案。当前业务 tick=50ms。参数来源为 `LevelConfig.xlsx`、`CustomerFlowConfig.xlsx` 及对应当前 ET 导出数据；实施前重新核对源行，生成文件只读。

## Goals / Non-Goals

**Goals:** 让第一章的汤、吐司、沙拉递进有完成操作与理解反馈的空间，并保留自然收尾。

**Non-Goals:** 不增加难度模式、动态难度、经济系统、失败惩罚、教程专用物品或全局配方覆盖；不重做合作平衡。

## Decisions

### 首轮参数明确、作用范围小

| Solo关卡 | 菜单递进 | 营业tick 当前→首轮 | 首客tick 当前→首轮 | 间隔tick 当前→首轮 | 订单等待tick 当前→首轮 |
|---|---|---|---|---|---|
| 1101 | 汤 | 500→1800 | 20→120 | 120→600 | 300→1200 |
| 1102 | 汤、吐司 | 650→2100 | 20→120 | 100→440 | 280→1000 |
| 1103 | 汤、吐司、沙拉 | 800→2400 | 20→120 | 80→360 | 260→900 |

即营业90/105/120秒，首客6秒，间隔30/22/18秒，耐心60/50/45秒。`QueueCapacity=2`、`EatTicks=60`、订单序列、菜单、加成/装修候选和星阈值保持。三关营业窗口内计划到客约3/5/7次，实际接待还受原队列/桌位影响，不能把此估算写成已生成订单数。

该首轮方案刻意让第一关更宽松：不要求预先完成成品，也不靠自动化瞬移才能交付。第二关用烤箱与汤的并行，第三关在既有三菜菜单中练习取舍。所有星级仍是表现反馈，零星可以继续。

### 先验证路径和库存，再决定是否需要第二轮参数

保持共享手工/自动/烘烤时长、初始布局与有限供应；因这些配置也被其他菜单/合作使用，本案不直接改它们。测试和真实路径检查所有当前菜单的原料、载具、设备、干净器具及补货入口。至少完成第一关一张汤单、第二关一张吐司单、第三关一张沙拉单，并从开始营业走到自然收尾；可按正常流程补货和洗碗，不注入物品或跳过计时证明节奏。

在同一套入口/动作版本上记录一轮普通游玩：实际卡点、成功/超时、等待空档和收尾时长。第一关连正确操作也无法赶上、第二三关没有任何并行需求、或有限供应造成无合法解法时，针对原因改上述两表的局部参数，再验受影响关。没有明确异常则不无限调参、不搭建统计平台。若必须改变共享加工/供应/布局才能解决，报告该具体缺口并另定范围，不暗中扩成全局改造。

首轮数值是待体验验证的选择。工程验收能证明可完成、没有回归；“是否紧张而有趣”由实际玩家反馈确认，不能以固定成功率代替。

### 配置源到运行交接

```text
Design/Excel/ET（active=true）
  LevelConfig：仅1101/1102/1103的OperatingTicks
  CustomerFlowConfig：仅对应三行的FirstArrival/ArrivalInterval/OrderWait
  → ExcelExporter Check → 已有Luban五目标生成与复制
  → Tables配置（运行只读）
ProductRoot Fiber / SceneType.ProductRoot
  Component ProductRuntimeComponent
  Child ProductHost / SceneType.ProductHost
    Component HostSessionComponent + Ordered MailBox
    Child Match -> Child RestaurantRuntime
      Component GridKitchenComponent（持续实物/加工）
      Child Level（当前配置计时、顾客/订单/前厅）
  Child ProductClient / SceneType.ProductClient
    ClientSessionComponent -> GridClientComponent（投影）
    GridPresentationComponent / UIComponent / GFEntityComponent
```

仅配置变更，不新增消息或树节点。原 runtime 按显式章节/保存的 Next 创建 Level；原 Host 推进计时并发布快照/结果。UI读取同一投影，不设另一套教程时钟。单成员退出和全运行退出沿原边界，配置不持有等待者。

读取、检查和生成按 Luban skill：使用源工作簿保留样式/公式，`Bin/Tool.exe --AppType=ExcelExporter --Console=1 --Customs=Check,ShowCmd` 通过后才导出，失败停止。检查所有目标 diff，仅允许目标数据变化及必要生成结果，不手改 bytes/JSON/DT/DR。

### 旧档兼容与六案交接

已完成结果保持原分数、星级和领取事实。旧检查点的 Next/实物/布局/库存保持；恢复后新建目标 Level 采用当前版本节奏，这是配置更新的一致语义，不承诺保存旧版时长。201、2101系列及其他章节不受参数改动。

配置可独立落地；最终真人体验在 HUD、首局路径和情境动作可用后评价。若这些案尚未完成，先记录当前界面造成的操作卡点，不能将其误归为数值不足。

## Risks / Trade-offs

- [放宽后空等] → 不延长准备阶段；只记录并调整实际出现的长等待，不先建复杂节奏调度器。
- [共享配置外溢] → 仅六行指定字段，比较合作/试玩和其他章节，复用其未改规则证据。
- [旧档被误解释] → 一个有效继续记录确认 Next及实物，原完成记录只读不重算。

## Migration Plan

核对源表→改局部参数并 Check/导出→相关配置/三关/旧档 UTF→真实三关路径→回填数值与体验观察。回退仅还原源行并重新导表，不覆盖用户存档。风险与运行范围见 [execution-notes.md](execution-notes.md)。
