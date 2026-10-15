# cooperative-renovation Specification

## Purpose

把已经交付的新增工位装修带入合作餐厅，让 Host 在结算后选择、双方看见同一结果，并在下一关一次安装真实工位；保持远端控制限制和失败保物，明确首版不含升级奖励或联机个人存档。

## Requirements

### Requirement: Host 独占装修控制
合作装修 MUST 仅接受当前 Host 本地成员的合法选择和继续请求，远端成员可查看候选和结果但不能越权修改。旧关或失效绑定请求不得生效。

#### Scenario: 远端绕过按钮发请求
- **WHEN** 远端 Client 直接提交装修选择
- **THEN** 拒绝并保持候选、已选奖励和厨房不变

### Requirement: 双方观察同一装修事实
已提交装修候选、选择与安装结果 SHALL 向双方提供独立投影，后加入者取得当前事实，不依赖历史 UI 点击重演。

#### Scenario: 选择后另一成员加入
- **WHEN** Host 已选奖励而另一成员随后合法加入
- **THEN** 新成员看到同一已选结果，不另生成一组奖励

### Requirement: 新增工位一次安装
合法 Continue MUST 在布局及奖励预检通过后一次安装新增工位或合法原工位单级升级；失败零改动，重复或旧请求不能多造工位或升级。升级保留原身份、占地、内容和已开始作业参数；候选只含可兑现的目标及预期等级。

#### Scenario: 安装失败后重复请求
- **WHEN** 布局不满足安装条件，随后相同请求再次到达
- **THEN** 不创建部分工位或新 Level，双方能看到失败原因，原物品保持

### Requirement: 合作装修不写个人检查点
合作装修 MUST 只改变本轮内存餐厅，不隐式写入 Host 或远端的 Solo 存档。

#### Scenario: 合作完成装修后退出
- **WHEN** 整轮合作运行结束
- **THEN** 本轮餐厅被释放，不能声称已保存任一成员的单机进度

## Sources

- [合作原工位升级增量](../../changes/archive/2026-10-15-cooperative-station-upgrades/execution-notes.md)。
- [本轮实施与逐项运行记录](../../changes/archive/2026-10-14-cooperative-renovation-choice/execution-notes.md)
- [共同网络入口](../../../Unity/Assets/Scripts/Game/ET/Code/Hotfix/Share/Product/ProductWire.cs)
