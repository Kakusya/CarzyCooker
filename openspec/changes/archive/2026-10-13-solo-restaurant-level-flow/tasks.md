# Tasks

## 1. 配置与持续 owner

- [x] 1.1 新增 LevelConfig 源、两关相同菜单样机并正常 Check／生成；增加 SoloLevelConfig UTF 验证下一关缺配置及出生位错误在创建前拒绝，同步配置说明。
- [x] 1.2 迁移 Restaurant／Level owner、GridActor 和全部 Match／Kitchen 查询与 Participant 枚举；SoloLevelOwnership UTF 断言唯一厨房、正确 Parent、成员数量及退出收尾，同步实际树说明。

## 2. 单一时钟与控制

- [x] 2.1 撤除旧厨房独立 IUpdate，接入唯一服务步／业务 Tick 与类型化控制本地消息；SoloLevelControl UTF 验证回执与最终阶段、无双推进、暂停后 Resume 仍能处理。
- [x] 2.2 实施取消未执行动作、当前移动步冻结、已开始工作冻结与接近认领释放；SoloLevelPause UTF 逐项断言恢复不重放、进度保留和旧认领无效，同步本轮已裁决暂停文档。

## 3. 阶段与换关

- [x] 3.1 实施准备、营业、停止新客、自然收尾和零星结算；SoloLevelPhases UTF 断言准备能备料、到点停止来客及零星可继续；真实前厅排空由顾客交接后补证，不能以空集代替。
- [x] 3.2 实施预检后换关、手持最后提交格落物、持续厨房与新角色、成长／buff／得分重置；SoloLevelCarry UTF 断言 ItemId／作业进度不变、库存不翻倍、旧关命令拒绝与非法下一关零写入，同步内存模式说明。

## 4. 集成验收

- [x] 4.1 在实际实例发现并执行相关 SoloLevel 和受 owner 迁移影响的 Grid／成员 UTF 小集，逐点记录 execution-notes，不重复未变化的发送器全套。
- [x] 4.2 普通 Launcher Solo 通过原 HUD 走准备备料、开店、暂停恢复、零星结算与下一关，核对保留实物和退出重开；只认实际操作，未接顾客时明确其验收未完成。
