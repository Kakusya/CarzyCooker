# Design

## Context

见 [proposal.md](proposal.md)。原Root持SoloSaveRepository，SoloSavePresentation持选中Slot/revision和UI epoch；成功文件由原Repository校验。本案增加本机元数据写入，必须保护旧成功记录和异步UI身份。

## Goals / Non-Goals

**Goals:** 本机命名/收藏、可恢复默认显示、真实失败可重试。

**Non-Goals:** 不删除、覆盖、移动产品记录，不改变授权文件/签名/Owner或成功载荷版本。

## Decisions

### 状态树和本地请求

```text
Fiber / Scene(ProductRoot)
  +-- Component ProcessInnerSender
  +-- Component SoloSaveRepository + original Ordered MailBox
  |     +-- local metadata: Profile / Slot -> label / favorite
  +-- Component SoloSavePresentation (selection / draft / epoch)
  +-- UIComponent -> UIGridKitchen (1101)
  +-- ProductClient (when running): no authority over record labels
```

元数据单独置于原repository隔离目录，按合法Profile/Slot键映射，文件名不使用用户名称。采用已有MemoryPack和暂存后发布的文件方式，不引入依赖或重写原`.ccsave`。一次元数据写入先完整验证合法Slot、文本元素上限和控制字符，保留旧有效文件直到新文件提交成功；独立失败不修改成功记录。

实际文件为 `labels.cclabels`，含版本、Profile和各Slot的名称/收藏，最大256KiB；单次输入先限制4096个UTF-16代码单元，再用StringInfo验证32个文本元素及完整代理项。写入前走原Select并核对revision/ResultId，旧修订拒绝。浏览时沿原repository读取独立值，元数据不可读单独显示，仍允许检查和继续有效成功记录；不设置第二份进度权威。

在原ProductRoot消息边界增加本地元数据查询/修改请求，UI取得原repository.GetActorId()并用所属CallLocalValueAsync调用，ProductRoot对应handler串行校验/写文件再复制响应值。沿现有Design/Proto/ET-Client本地repository消息源追加定义，经原Proto2CS生成并保留既有opcode；不得手写生成类或注册另一套消息。新增请求不进入远端wire白名单。调用方/handler保持原输入交接与finally释放，await后核对EntityRef、选中Slot/revision、epoch；旧结果不覆盖新的输入草稿，不用Event承担写入或伪装网络传递。

Proto2CS按路径Ordinal排序连续编号；本案在设置案新增尾文件之后使用ZZZZZZZZZZZZZZZZLocalLabels.proto追加本地消息，不能插入原SoloSave.proto导致后续协议整体移号。生成后比较所有既有opcode值保持，而不是手改生成物。

默认名称由现有元数据产生；用户别名只是一层显示。收藏只影响当前浏览排序/筛选，不创建运行，列表中保留原ID/时间和继续/完成状态。另一个新Slot不自动继承标签；同Slot成功修订变化仍保留本机标签，但继续必须重新Select校验。

不采用直接改成功Header/重新签名方案，因为显示偏好不应改变可验证进度与来源。损坏元数据显式显示不可读且保留原文件；玩家仍可只读原进度，不自动重置或覆盖未知版本。

## Risks / Trade-offs

- [元数据误写进度] -> 临时repository真实文件往返、同Slot修订变化及两个Slot隔离断言；比较源文件实际字节保持，禁止hash清单。
- [写失败破坏旧标签] -> 实际文件锁/访问失败验证旧文件仍可读，新运行重试成功。
- [旧UI目标串档] -> 两记录选择切换、取消、池化与旧callback验证。

## Migration Plan

缺失元数据默认无别名/收藏，不改旧档；本案不为已有成功文件升版。扩展本地消息、repository及原选档页，运行结果按 [execution-notes.md](execution-notes.md)记录。回退可忽略独立元数据，不能删除用户已写标签。
