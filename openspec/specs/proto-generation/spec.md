# Proto2CS 配置域、序列化类型与 Opcode 顺序

## Purpose

记录当前 CarzyCooker 底座的Proto2CS 配置域、序列化类型与 Opcode 顺序。依据源码和配置建立静态契约，新增实现约定不代表存量代码已全部符合；本次不运行产品构建、玩法或编辑器验证，不描述未来做饭业务已实现。

## Requirements

### Requirement: 配置与顺序

Proto2CS SHALL 从 Design/Proto 直接子目录的 active proto.conf 读取目标，并递归排序 proto 文件，按消息顺序从 startOpcode 递增生成。

#### Scenario: 配置与顺序的适用行为

- **WHEN** 在发布协议中插入或移动消息
- **THEN** 评估后续 Opcode 变化，不用重新生成成功掩盖 wire 兼容变化

### Requirement: 序列化隔离

ET-Client、ET-ClientServer、ET-Admin MUST 使用各自 ET/MemoryPack 生成域；区间和输出以当前 proto.conf 为准。工具保留 UGF 生成器不代表当前 main 分支的 ET 底座有 GameHot/GF packet 运行时。

#### Scenario: 序列化隔离的适用行为

- **WHEN** 读取 ET-Admin 与其他 ET 配置域
- **THEN** 记录不同生成/服务域，不简单合成一个全局区间或混用消息类型

## Sources

- [Proto2CS.cs](../../../Share/Tool/Proto2CS/Proto2CS.cs)
- [Proto2CS.ET.cs](../../../Share/Tool/Proto2CS/Proto2CS.ET.cs)
- [Proto2CS.UGF.cs](../../../Share/Tool/Proto2CS/Proto2CS.UGF.cs)
- [proto.conf](../../../Design/Proto/ET-Admin/proto.conf)
- [proto.conf](../../../Design/Proto/ET-Client/proto.conf)
- [proto.conf](../../../Design/Proto/ET-ClientServer/proto.conf)
