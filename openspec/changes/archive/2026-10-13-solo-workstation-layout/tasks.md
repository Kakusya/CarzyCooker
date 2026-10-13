# Tasks

## 1. 工位基础与布局

- [x] 1.1 新增 StationConfig／InitialLayoutConfig 源与能力／槽／占地／操作位，Check／正常生成；SoloStationConfig UTF 验证普通单槽、声明的有限特殊容量及合法初始布局，更新设备配置说明并交接 Oven 能力。
- [x] 1.2 以真实配置替代初始化硬编码，迁移 Station 和统一地形读取者；SoloStationTerrain UTF 断言玩家／伙伴／顾客路径读取同一 Blocked，非法重叠／越界／入口不通配置拒绝，同步实际布局说明。

## 2. 准备态调整

- [x] 2.1 接入固定身份和版本的本地移动／旋转请求及一次预检提交；SoloStationLayout UTF 验证合法变换、旧版本、堵路、压物／压角色和营业／暂停拒绝，失败版本与关系零改变。
- [x] 2.2 保留原物品、容器和作业身份／进度，释放不再到位的人工认领并更新路线及 Board／原 HUD；SoloStationPreserve UTF 检查合法带物／带作业移动和手工接续，更新操作步骤。

## 3. 集成验收

- [x] 3.1 发现并执行 SoloStation 与受地形／创建迁移影响的现有 Grid 小集，在 execution-notes 逐项记录，不穷举布局。
- [x] 3.2 普通 Launcher Solo 在准备态移动／旋转一个带材料工位，拒绝堵路布局，开店后拒绝调整，查看同一物品和作业继续；伙伴／顾客接入后用该地图复用实际路径证据。
