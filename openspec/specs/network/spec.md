# ET Session、RPC 与消息分发

## Purpose

记录当前 ET 消息管线与 owner/取消边界；GameHot/GF Network 运行时在本分支已移除。未来做饭传输选择仍待裁决。 当前基线为 main 分支的 ET 底座；静态格式/源码核对不代表产品构建、Editor、玩法或联网已验证。

## Requirements

### Requirement: ET 消息管线

ET 消息 SHALL 使用当前生成的请求/响应接口与 handler/fiber 分发；UI 不把 wire response 作为第二业务事实来源。

#### Scenario: ET 消息管线的适用行为

- **WHEN** 处理 ET 请求或响应
- **THEN** 进入相应 handler/owner，不恢复已删除的 GF packet 运行时

### Requirement: Session RPC 等待者

Session.Call MUST 分配 RpcId 并登记 requestCallbacks；OnResponse 移除对应回调后设置结果，取消移除回调并构造 ERR_Cancel 响应，Session 销毁设置异常并清空等待者。

#### Scenario: Session RPC 等待者的适用行为

- **WHEN** RPC 取消或 Session 销毁
- **THEN** 沿当前 SessionSystem 的明确清理路径处理，不把机制扩大为产品重连/存档保证

### Requirement: 未来网络范围

协作文档 MUST 区分底座的 ET/KCP/WebSocket 等传输能力与未来做饭产品选型；不把被移除的示例 endpoint 当作当前生产服务。

#### Scenario: 未来网络范围的适用行为

- **WHEN** 提出做饭联网需求
- **THEN** 保持传输选择待裁决，按具体行为/环境另行授权，不自动启动服务或更换传输

## Sources

- [Session 与 SessionSystem](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message/Session.cs)
- [NetComponent](../../../Unity/Assets/Scripts/Game/ET/Code/Model/Share/Module/Message/NetComponent.cs)
- [请求示例](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Server/Demo/Map/C2M_TestRobotCaseHandler.cs)
- [数据与投影](../../../references/reactive-and-network.md)
