# Design

## Context

生产图集266切片，涵盖63物品和16工位。独立样板203帧，只有一种厨师外观。当前Editor精确匹配主工程且Play停止；其他会话新增玩法不得被旧样板覆盖。

## Goals / Non-Goals

**Goals:** 在真实入口显示样板覆盖的资产和动画，保持新玩法资源、真实内容与owner回收。

**Non-Goals:** 不重画未覆盖资源、不改HUD布局、网络协议或业务规则、不构建Player，不自动提交／归档。

## Decisions

1. 新增紧凑BistroAtlas，不重导原生产图集。CSV单帧生成新布局代码，运行时一次切片缓存。保留原角色身份：本地玩家用新厨师，远端玩家／伙伴／顾客沿原不同外观。
   现有洗碗／打蛋也需动作，因此从同一已保存模型的作业姿态去除刀具，派生四方向32帧通用作业；运行时共235切片。离线样板203帧与原预览保持。
2. 固定TickMilliseconds=50，动画10fps即每2tick一帧。只使用传入的投影显示tick；暂停／结算冻结该tick，不另启Animator时钟。八帧carry在移动时播放，站定持物保持端物第一帧；无持物时idle；只有真实手工工作使用刀具，不把洗碗显示成切菜。
   位置插值保留原StepTicks上限；美术相位按快照tick及收到快照后的显示时间外推，不受该位置上限限制，避免稀疏快照使待机／液面停帧。暂停／结算使用冻结投影tick，不推进业务。
3. 本地真实Serve结果Executed且身份属于当前关／绑定时，在Mono缓存一次开始tick和成员generation。显示递餐，不发送请求，不以动画判定成功；Accepted／Rejected／重放同一结果／旧关不能触发。移动或新工作可覆盖短动作。
4. Hand点由每帧CSV生成，转换到当前Sprite pivot／PPU及scale，手持物跟随。朝北手持排序在身体后方，其余在前；容器内容随宿主。原料保持原料，只对实际Soup／OrangeJuice定义或Ready批次输出使用液面循环。
5. Prefab直接引用新增纹理，所有新切片归现有Mono Board本轮view。几何重配复用；ReleaseBoard释放切片并清显示缓存。现有ResourceCollection With Optimize刷新依赖。

## Owner / 消息路径

```text
原产品Fiber / ProductClient Scene
  ClientSessionComponent
    GridClientComponent -> 独立快照、结果值
  GridPresentationComponent -> 读取已提交投影与结果
  GFEntityComponent
    GridKitchenBoard (原Component，类型ID不变)
      MonoGridKitchenBoard -> 两张Prefab纹理、动态Sprite与短动作显示缓存
```

原UI命令经ProductClientSystem／本地sender或远端Session到同一权威裁决；原接收方提交结果后更新GridClient并发布观察事件。此次不改请求、ActorId／Mailbox或答复等待者；逐帧读取已有LastProductOutcome，只有本人已执行结果用于递餐显示。没有新增消息对象或等待者。本轮创建视图时切片、按投影显示；关卡切换清短动作；GF Hide／整体退出沿原ReleaseBoard回收。

## Risks / Trade-offs

- 新pivot和原图不同 → 用生成布局及手持点验证，不套旧0.17。
- 不同图集rect可重合 → 用(texture,rect)检查全定义唯一表现，保留原63物品覆盖。
- 逐帧分配 → 仅初始化切片与静态生成数组，热路径复用字典，不创建Sprite或临时集合。
- Editor繁忙 → 不自动停止其他Play／测试；先完成可独立工作，运行验收等待空闲。

## Migration Plan

生成新纹理／布局，编译并经Editor API设置导入与Prefab引用；运行新动画行为及原美术／生命周期小集；真实Launcher确认移动、加工、持物、暂停与交付，再同步说明。回退本次新增引用和渲染映射即可恢复原外观。
