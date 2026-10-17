# cooperative-kitchen-logistics Specification

## Purpose

定义本机合作厨房从有限原料采购、运输、接收、拆份到设施清理的共同物资合同，让 Host 与远端成员能在同一餐厅继续经营。保留真实物品、原容器和跨关货物，不增加无限来源、私人库存或持久经营权限。

## Requirements

### Requirement: 远端设施操作保持真实到位和原容器

成员 SHALL 在原水池或具名热水档接水，在有排液能力的水池排液，在垃圾桶处理固体／包装；执行时核对真实设施、四邻到位、目标布局、容器内容、卫生及加工占用。错误设施、未到位、不支持的混合内容或加工锁定按原规则拒绝；成功只处理允许的内容，保留不应消费的原容器。本批透明冷饮杯和调饮壶允许通过对应设施分别处理实际固体／液体，保留另一物态及原壳，不退款；其他容器不获得该例外。

#### Scenario: 从远处排液并重放
- **WHEN** Client 手持剩汤锅请求排液，并重放相同命令
- **THEN** 接收和行走阶段不消耗，到达合法水池后只处理一次剩余批次与引用，原锅保持；错误垃圾桶请求不改变熟汤

#### Scenario: 共址洗碗占用
- **WHEN** 水池正在被有效洗碗工作占用，另一成员请求接水或排液
- **THEN** 按既有设施占用规则拒绝，原工作与手持内容不变，不能因远端身份绕过占用

#### Scenario: 冷杯按物态分别清理
- **WHEN** Client 手持本批含珍珠和奶茶的冷杯，分别在水池排液或垃圾桶处理固体
- **THEN** 只移除对应实际物态，另一内容及原杯保持，剩余基底不增加；后续加料仍须按配方顺序，其他混合容器保持拒绝

### Requirement: 所有成员共用有限补货与唯一收货

合法在线成员 SHALL 使用同一有限配额采购，通过唯一业务时间运输，并在具名收货点接收实物包装。身份和业务上下文由可信入口校验；重复采购不重复扣额，满槽保留待收货，竞争同一货物只允许一次成功，不生成私人副本。

#### Scenario: 同一货物被两端接收
- **WHEN** Host 与 Client 对同一待收货提交合法接收
- **THEN** 只生成一个真实包装并消费该货物一次，另一请求失败且不再扣额或生货

#### Scenario: 收货槽占满后移动工位
- **WHEN** 收货槽被占用导致接收失败，随后在合法阶段调整工位并腾空
- **THEN** 货物仍可按原规则在当前实际收货点接收，旧布局目标请求拒绝，不吞货、不回填配额

### Requirement: 包装拆份和丢弃保持数量

两端 MUST 使用同一真实包装、内容物与手槽规则拆份和搬运。拆份只转移原物品身份；失败不扣份。清理装料包装或空壳沿既有垃圾桶规则，不退采购配额、不返还已销毁原料，也不通过容器嵌套复制物资。

#### Scenario: 拆份与清理竞争
- **WHEN** 两成员对同一包装先后提交拆份或丢弃，后续请求的预期内容已失效
- **THEN** 后续请求拒绝，实际转移和销毁的数量各只计算一次，未参与物品和原料配额不变

### Requirement: 物流随餐厅推进并同版显示

货物、配额、收货点和包装 MUST 属于持续厨房，并与关卡和物品以同一提交版本被各端观察。暂停与 Completed 按既有规则冻结运输；合法换关保留在途和待收货。单成员退出不销毁公共货物，新合法加入获取当前物流，不重置配额。

#### Scenario: 在途换关与后加入
- **WHEN** Host 合法换关时仍有在途货物，之后一名新成员加入
- **THEN** 同一货物保留原进度，新成员看到当前配额与货物，不增加初始物资；旧关采购／收货请求不能改变它

#### Scenario: 采购提交后请求者失联
- **WHEN** 已提交采购的成员在收到确认前失联
- **THEN** 保留已提交的公共货物和扣额事实，队友仍能等待并接收；失联收尾和新 Join 不重做采购

## Sources

- [Solo 清理规格](../../changes/archive/2026-10-14-solo-kitchen-clear-and-discard/proposal.md)、[Solo 供应规格](../../changes/archive/2026-10-14-solo-supply-delivery/proposal.md)。
- [设施实现](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridCleanupSystem.cs)、[供应实现](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/GridKitchen/GridSupplySystem.cs)、[设计](../../changes/archive/2026-10-14-cooperative-cleanup-supply/design.md)。

- [cooperative-cleanup-supply 逐功能运行与验收](../../changes/archive/2026-10-14-cooperative-cleanup-supply/execution-notes.md)。

- [茶饮实际加工与两模式交付](../../changes/archive/2026-10-17-kitchen-tea-beverage-batch/execution-notes.md)。
