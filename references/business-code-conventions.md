# 实现约定

**非规格**；冲突以代码/表、进行中 change、已同步主规格及用户最新意图为准。**阅读 ≠ 授权**文中未做能力。

由旧规范迁出，保留本底座适用规则；以下是新增代码约定，不声称所有存量代码都已符合。

## 分层和 owner

- 命名、缩进和可见性遵守根 [代码规范摘要](../AGENTS.md)；GF 私有字段与 ET 数据字段按各自现有模式，不机械重命名存量或生成字段。
- 当前 main 分支的 ET 底座不包含 GameHot；业务在 Unity/Assets/Scripts/Game/ET/Code 的适用层，稳定加载在 ET/Loader。
- ET 数据在 Model/ModelView，EntitySystemOf 逻辑在 Hotfix/HotfixView，沿用 ET、ET.Client、ET.Server 分区。
- 检查 asmdef、UNITY_ET/UNITY_HOTFIX 与 ET CodeMode 和 ProcedurePreset，不凭目录猜程序集。
- 状态由真实组件/实体持有。Mono view 提供 CodeBind 引用，不复制业务状态；ET/GF owner 是 UIComponent 和 GFEntityComponent。
- UI/Entity 读 [生命周期](framework-lifecycle.md)，配置/协议/绑定读 [生成约定](data-generation.md)。

## 复用、字段和错误

新增 API、容器、缓存或网络流程前搜索现有 owner。已有 API 直接调用，缺内聚能力先扩展原 owner，复杂单次步骤可提 private 方法。真实第二消费者/实现或实际外部替换才提最小公共类型/interface；不只为未来复用或方便 mock 包多层 gateway/facade。

优先 GameEntry.UI/Entity/CodeRunner、EntityContainer/ResourceContainer、ET Component/Child API、既有消息 handler、ET Session/RPC、TablesComponent 和 ExcelExporter。

业务字段相邻注释说明来源、身份、单位、特殊值、版本或生命周期；类型 ID 不等于实例 ID。显然局部变量、纯组件引用与生成字段不机械注释。网络与响应式任务还需读 [数据与投影约定](reactive-and-network.md)。

必需表、Prefab/CodeBind 引用、生产资源和成功 payload 缺失修源头，不到处添加默认对象、空集合或静默 return。合法空态、用户输入、可选字段、取消/stale、池/生命周期失效可检查，但必须有动作。现有可选 API 返回契约保持，迁移不强改框架。

错误由最近且能采取动作的边界负责；不能恢复/转换/补上下文就不 catch。不要重复记录，也不 catch 后返回空列表伪装成功。

## 异步和质量

使用项目 UniTask/UniTaskVoid，不引入第二异步模型。owner 退出取消任务，await 返回应用前检查取消和生命周期；不新建重复 alive/version 状态。派生层只清自身所有权。

验证与改动风险匹配，覆盖真实成功、拒绝、取消、旧结果和合法空态。低影响可逆文档改动不新增产品测试。编译、生成、联网和玩法各证明自己范围，未运行写 NotRun。禁止 SHA/hash 和额外 JSON 证据/状态副本。
