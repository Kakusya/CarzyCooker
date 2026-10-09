# Spec Delta

## ADDED Requirements

### Requirement: 当前协议生成域与输出

当前已启用协议 SHALL 按各 proto.conf 分别生成：ET-Client 配置起点 10000，输出 Client 与 ClientServer；ET-ClientServer 起点 20000，输出 ClientServer；ET-Admin 起点 30000，输出 DotNet/Model 消息。ET 生成器先递增再赋值，因此首消息分别为 10001、20001、30001。三个目标均为 ET 类型与命名空间；配置起点不等于首消息编号或固定域长度。

#### Scenario: 客户端请求协议修改

- **WHEN** 修改 ET-Client 的消息定义
- **THEN** 核对两处输出及其消费者，不能只更新 Client 而遗留 ClientServer 的旧消息

#### Scenario: Admin 协议修改

- **WHEN** 修改 ET-Admin 协议
- **THEN** 使用其 DotNet 输出和独立服务消费者，不因为另一个域也有 Opcode 就合并配置域

### Requirement: 协议顺序兼容性审阅

已发布协议变更 MUST 核对递归文件的排序及文件内消息顺序；现有生成器按当前顺序递增 Opcode，没有独立稳定编号表。源中的插入、移动或删除可能改变后续编号，生成成功不能证明新旧进程互通；输出清理也不是事务式整体替换保证。

#### Scenario: 在现有消息前插入新消息

- **WHEN** 新消息位于该生成域原有消息之前
- **THEN** 对受影响后续 Opcode 和对端生成版本进行兼容性审阅，不仅检查生成 C# 是否编译

#### Scenario: 域内没有 proto 文件

- **WHEN** active 配置域没有发现任何 proto 文件
- **THEN** 当前生成流程跳过该域的生成；末尾 success 日志不能证明该域本轮生成了有效消息

## Sources

- [协议发现与排序](../../../../../../Share/Tool/Proto2CS/Proto2CS.cs)
- [ET 消息与 Opcode 生成](../../../../../../Share/Tool/Proto2CS/Proto2CS.ET.cs)
- [ET-Client](../../../../../../Design/Proto/ET-Client/proto.conf)
- [ET-ClientServer](../../../../../../Design/Proto/ET-ClientServer/proto.conf)
- [ET-Admin](../../../../../../Design/Proto/ET-Admin/proto.conf)
