# Tasks

用户已授权完成五案；前置 Solo 九案和设施清理已完成验收，清理提供包装所需固液／垃圾桶动作。字段、协议和 UI 源顺序修改，不与其它 change 并行导表；仅已实现并有对应运行证据的任务勾选。

## 1. 来源与持续 owner

- [x] 1.1 新增 SupplyConfig 源/注册、包装定义及具名单槽收货台，修改菜单闭包读取者并正常五目标 Check/生成；SoloSupplyConfig UTF 验证合法补货来源、缺来源/能力拒绝及没有免费半成品，更新供应配置说明。
- [x] 1.2 接入 Kitchen 的 Supply/Shipment owner 与 Restaurant 唯一业务钟；SoloSupplyLifecycle UTF 验证暂停/Completed 停钟、收尾禁新申请但旧货继续、跨关余额/进度/身份保留，同步树与阶段约定。

## 2. 命令与实物

- [x] 2.1 实施类型化采购预留和原业务编号/本地消息，正常 Proto 生成；SoloSupplyPurchase UTF 验证足额/不足、重复最新请求及旧关拒绝，断言余额和货物不部分写入。
- [x] 2.2 实施到货/显式接收及移动后的收货位解析；SoloSupplyReceive UTF 验证满槽等待、恢复空间、重复接收不复制和真实包内材料，更新收货说明。
- [x] 2.3 实施指定材料拆份和空包装到垃圾桶投弃；SoloSupplyPackage UTF 验证材料身份搬运、满手/目标变化拒绝、未到垃圾桶不得丢弃、含料包不得整体丢弃及不退款回库，同步设施处理边界。

## 3. UI 与集成验收

- [x] 3.1 合并原同版快照与当前 HUD 的采购/等待/接收/拆份反馈，必要时修改 Prefab/CodeBind 源并生成；实际 UI UTF 验证旧关回调无效和重开不重复采购。
- [x] 3.2 发现并执行相关 SoloSupply 与受源/来源校验影响的小集；普通 Launcher 耗尽原料→采购/满槽等待→拆份再制作→在途跨关，逐验收点记录 execution-notes，不扩大联网或全仓。
