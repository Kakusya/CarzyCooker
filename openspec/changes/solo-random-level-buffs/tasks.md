# Tasks

## 1. 配置与候选

- [x] 1.1 新增 BuffConfig 与 LevelConfig 允许池／数量／门槛字段，Check／正常生成；SoloBuffConfig UTF 验证真实表、配方／比例、必选空池错误与无 buff 可开店，说明首版仅煮制加速变体。
- [x] 1.2 在本关创建时固定 Seed 并无放回抽样；SoloBuffDraw UTF 验证受控同 Seed 复现、小池截断、候选无重复及 UI 重开不重抽，更新候选归属说明。

## 2. 选择与效果

- [x] 2.1 接入真实本地 Choose 消息、一次选择和 Start 门槛及原 HUD 候选入口；SoloBuffChoice UTF 验证候选外／暂停／非准备／旧关拒绝、重复事实、改选锁定与同版投影。
- [x] 2.2 接入新启动适用汤作业冻结 RequiredTicks；SoloBuffCooking UTF 验证向上取整和至少一 Tick、选择前旧锅不变、新锅加速、暂停与跨关旧锅保留，更新首版效果说明。

## 3. 集成验收

- [x] 3.1 发现并执行 SoloBuff 和实际被耗时计算改变的汤小集，按 execution-notes 逐点记录，不增加随机频率统计或性能门槛。
- [x] 3.2 普通 Launcher Solo 在准备态打开／重开候选、选择后开店煮汤、暂停恢复及下一关重新选择，检查旧作业和材料不被 buff 重写；与流程实际操作复用时明确覆盖点。
