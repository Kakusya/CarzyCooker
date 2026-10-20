# Proposal

## Why

现有圆润美术已覆盖玩法，但食物材质、手作感和动作连续性仍可提升。用户已选择温暖、有食欲的手作小馆，并授权在其他会话工作时先制作独立动画素材。

## What Changes

- 在独立 `Art/CarzyCooker/HandmadeBistro/` 制作可编辑 Blender 源和透明 PNG 帧／精灵表。
- 厨师提供四方向待机、行走、持物行走、切配、递餐；食物提供汤面、橙汁液面和蒸汽循环。静态器具、吐司和代表工位组成风格样板。
- 提供本地可播放、暂停、选方向和逐帧查看的素材预览，以及帧尺寸、时长、锚点和内容绑定说明。
- 正式图集、Prefab、HUD、业务代码与共享 Unity Editor 的接入留给后续交接。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `restaurant-art`：增加独立动画素材交付和离线预览约定，不改变已接入游戏的表现契约。

## Impact

只新增本 change 与独立美术目录。复用本机 Blender 4.5 与浏览器，不新增运行时依赖、表或网络服务。用户本轮明确授权制作与执行，规划完成后继续 apply。验证投入与未接入范围见 [execution-notes.md](execution-notes.md)。
