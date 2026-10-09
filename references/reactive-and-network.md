# 响应式投影与网络数据约定

**非规格**；冲突以代码/表、进行中 change、已同步主规格及用户最新意图为准。**阅读 ≠ 授权**文中未做能力。

## 状态与 UI

UI 观察权威领域状态或明确 UI context，不保存网络 response 作为第二事实来源。每项独立表现只有一个更新 owner，首次投影与后续刷新走同一机制；不要叠 event、OnOpen 手动 Refresh、Bind 和 UI cache 更新同一控件。

外部 ReactiveBinding 是 manifest 声明的包；ET 的 IETReactive/ETReactiveSystem/Source/Bind 是仓库自己的生成机制。两套 attribute 不混用。ET owner 实现 IETReactive、声明 partial，System 符合 EntitySystemOf 与生成诊断，Bind 用 nameof 指向源。具体签名查 [响应式规格](../openspec/specs/reactive-ui/spec.md) 的源文件。

最小投影只依赖相关字段/语言/可用状态。Bind 只写表现，不发请求、不写领域状态、不承担生命周期清理。集合变更需按选中机制传播版本；不能假定普通集合修改就可观察。对象池与上下文重绑清理观察缓存，具体 Reset API 以实际包或 ET generator 为准。

## 数据与网络

明确类型身份、单位、更新语义和 owner。全量替换、增量应用与消息事件不同；缺字段不能用默认值覆盖未更新数据。池化对象在 Dispose/OnHide/OnRecycle 清自身状态；UI 不另存服务器缓存。

当前 main 分支的 ET 底座使用 ET message/handler/fiber、Session/RPC 与已有服务端 HttpComponent；GameHot/GF Network 运行时已移除。复用对应 owner，不复制 correlation/channel/请求字典。网络/解析/业务失败、取消、会话切换、stale 不 Apply；成功且当前的必需 payload 落到唯一 owner。传输 ACK 不等于业务结果。

UI 触发领域入口并观察状态，不拼 endpoint、不解析 wire response。没有实际 HTTP 消费者时不凭空建 HTTP gateway；当前服务端 HttpListener 能力不表示客户端产品流程已接入。

当前分支与手册残留差异见 [已知问题](../KnownIssues.md)。Session.Call 按 RpcId 管理等待者，取消移除回调，Session 销毁设置异常并清空；不自动等同于未来做饭重连语义。做饭固定 Tick、重连与存档设计尚未实施，不作为当前 helper 的保证。
