# Spec Delta

## ADDED Requirements

### Requirement: 合作地址与连接帮助可访问

系统 SHALL 提供以下入口行为：按创建／加入阶段显示合法地址示例、当前实际监听地址、复制入口和可理解的连接／取消／重试说明；把回环地址的适用范围说明白。

#### Scenario: 合作地址与连接帮助的正常使用
- **WHEN** Host 创建房间后复制地址，Client 粘贴合法目标并加入
- **THEN** 复制值来自当前实例，连接进度与真实状态对应，一次确认只发起一次加入

### Requirement: 合作地址与连接帮助保持事实与操作边界

系统 MUST 遵守以下边界：只复制公开 endpoint；不展示凭证／密钥，不自动连网测试或扫描网卡，不改变现有显式 LAN 授权与地址校验。错误仅按实际校验／握手结果说明，不能猜测防火墙或声称公网可用。

#### Scenario: 合作地址与连接帮助的取消或状态变化
- **WHEN** 输入格式错误、回环地址用于异机或连接超时后取消
- **THEN** 页面解释实际适用范围和错误，取消收尾原尝试，旧答复不能影响重试或后续单机

#### Scenario: 合作地址与连接帮助关闭重开与旧回调
- **WHEN** 玩家关闭并重开本入口，随后旧页面回调或迟到结果到达
- **THEN** 页面只作用于当前有效上下文，原操作不因重开重复提交，旧结果不覆盖新选择，原合法入口仍可使用

## Sources

- [当前主规格](../../../../../specs/product-entry/spec.md)。
- [实际实现前提](../../../../../../Unity/Assets/Scripts/Game/ET/Code/HotfixView/Client/Product/ProductEntryPresentationSystem.cs)。
- [本案设计](../../design.md)、[执行与验证边界](../../execution-notes.md)。

本增量描述本案入口合同；已按对应任务实施并运行，逐项结果见 execution-notes。现有底层能力只复用行为未变部分，不由规格存在推断入口验收。
