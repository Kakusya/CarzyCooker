# 模块实现约定

**非规格**；冲突以代码/表、进行中 change、已同步主规格及用户最新意图为准。**阅读 ≠ 授权**文中未做能力。这里只写本项目实际用法、坑点和开关，不照搬官方全文。

| 参考 | 何时先读 |
| --- | --- |
| [业务代码约定](business-code-conventions.md) | 分层、程序集、owner、复用、错误与异步 |
| [UI / Entity 生命周期](framework-lifecycle.md) | 创建、重复进入退出、在途加载、事件与资源回收 |
| [生成链路](data-generation.md) | Excel/Luban、Proto、CodeBind、ID、本地化 |
| [响应式投影与网络](reactive-and-network.md) | 唯一状态来源、UI 观察、消息/HTTP、取消与 stale |
| [已知问题](../KnownIssues.md) | 采用现有 helper、解释行为、提出修复 |
| [端口管理 SOP](port-management.md) | 新监听、固定 endpoint、多 Editor 的发现与消费者核对 |
| [动态 UI SOP](dynamic-ui-sop.md) | Prefab/CodeBind/Canvas 结构修改与引用迁移 |

完整源码/手册/规格/技能路由见 [模块索引](../Docs/AI/module-index.md)。框架用法在 [Book](../Book/README.md)，未来设计在 [产品资料](../Docs/Product/README.md)，不与主规格混为一层。

新增模块用法总结放 `references/<模块>.md`，补上“非规格、阅读不授权”的页眉，并更新入口路由。原 `Docs/Development/` 对应页面只保留跳转，正文在本目录或根 `KnownIssues.md` 唯一维护。
