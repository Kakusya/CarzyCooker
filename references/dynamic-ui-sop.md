# 动态 UI 结构与迁移 SOP

**非规格；阅读不自动授权工具或结构重构。** 使用本项目 ETUI/Widget/UIEntity/CodeBind，先加载 [UI 技能](../.agents/skills/carzycooker-ui/SKILL.md)；涉及表另加载 Luban，涉及 UIEntity 生命周期另读 Entity 技能。

1. 找到实际 Prefab、Mono/CodeBind 源、Luban 记录与 UIComponent/GFEntityComponent owner，核对生成字段、Canvas/RectTransform 和引用消费者。
2. 先确定结构变更范围及旧引用迁移方式；路径、资源名、ID、parent 变化分别检查，不只改可见布局。
3. 精确匹配本工程，在 Editor 安全操作中修改/保存；保护打开且未保存的场景，不覆盖其 YAML/meta。无可用安全 API 时先报告，不写临时脚本猜结构。
4. 必需绑定在 Prefab/CodeBind 源修复后重生成；不手改生成 partial，不在 System 重复 GetComponent 或复制业务状态。
5. 动态资源使用 UIEntity 表和独立类型 ID，不混普通 Entity；真实 owner 负责开关、取消在途加载、订阅和资源回收，不 SetActive 受管根替代生命周期。
6. 对当前行为验证连续 Open/Close、加载中退出、池重用、覆盖 Pause/Resume 和按钮订阅。未运行行为验收写 NotRun，不以序列化 diff 冒充正确。
7. 结构重构、表驱动行为和工具改进分开 change，同步绑定/表/调用者/文档。当前没有结构注册表、自动迁移 runner 或专用结构测试；需要它们另案实现。

基础说明见 [UI 开发](../Book/UI开发.md)、[生命周期](framework-lifecycle.md)、[生成链路](data-generation.md)。
