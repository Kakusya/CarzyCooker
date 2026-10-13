# Proposal

## Why

当前准备态可以移动既有工位，但成功后没有永久装修选择。需要让结算选择在下一 Preparing 转成可见的新工位，并延续同一厨房。

## What Changes

- 成功结算展示有限装修候选，选择一个后锁定，继续时安装一次。
- 首版样机只增一个普通台面或切配台，沿用现有能力定义；位置、朝向、数量来自可信配置并整体预检。
- 无合法候选可继续，不把旧物或未完成加工删除来腾位置；同一奖励不重复获取。
- 装修状态归持续餐厅，与本关 buff 分开；零星同样参与，不增加经济、评分奖励或菜单许可缩小规则。
- 首版内存延续；接入后续检查点时，选择和布局进入成功结果，而 Preparing 改动不独立写盘。
- 本轮仅规划，样机候选与验证取舍见 [execution-notes.md](execution-notes.md)。

## Capabilities

### New Capabilities

- `restaurant-renovation`：成功后永久装修选择、合法安装与跨关延续。

### Modified Capabilities

无；已有准备态移动/旋转合同不变。

## Impact

依赖 Solo 关卡、工作台、顾客收口和完整 UI。新增 RenovationConfig、候选引用与正常生成，扩展 Restaurant 状态、Continue 预检、布局安装及结算面板。新增工位与已有工位共用地形和表现 owner，不扩大地图或增加第三道菜；存档及联机装修扩展另案。
