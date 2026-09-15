# 门途径特效素材来源与授权

> 0.4.5.3 更新：主体改为 `QuietDoor/` 保存的内置 image_gen 原创6帧门扉，处理与提示词一并保留。
> `MagicCore*`、大范围旋环与全屏领域旧素材仅留档，不再加载或绘制；以下用途表记录的是旧版本。
> 当前使用的第三方素材限 Kenney CC0 衍生的局部裂隙/碎片/盒体遮罩。新实现依据见 `REWORK-2026-09-14.md`。

本目录下的贴图分两类：自绘/合成素材，以及第三方 CC0 素材。所有第三方素材均为 **CC0 1.0（公共领域贡献）**，允许商用与再分发。

## 第三方素材

| 素材 | 来源 | 授权 | 用途 |
|---|---|---|---|
| Particle Pack | Kenney（https://kenney.nl/assets/particle-pack） | CC0 1.0（原文见 `FreeAssets/Kenney-ParticlePack-License.txt`） | 作为柔笔触，叠加成有机光环、暗雾、裂口、光尘等 |
| Fire Wrath - Magic Effect | Cethiel（https://opengameart.org/content/fire-wrath-magic-effect） | CC0 1.0 | 25 帧手工动画魔法序列帧。按门途径配色重新上色为 `MagicCoreViolet/Teal/Gold`，用于门开启、撕裂缝隙、领域核心与空间破碎 |

CC0 不要求署名，此处记录来源是为了可追溯与便于替换。

> 备注一：曾试接 OpenGameArt 的 CC0 体积渲染序列帧（Blender 特效包、2D spell effects），但成品的紫光体积风格与泰拉瑞亚像素画风冲突，已全部移除，相关图集与打包脚本一并删除。门后领域改用自绘的泰拉瑞亚风格贴图（`DomainSky/DomainSpires/DomainFog/DomainVignette`）。

> 备注二：`MagicCore*.png` 由 `Build-MagicFxAtlases.ps1` 从 Cethiel 的 1000×1000 拼合表按亮度重新上色生成；因为原图是黑底加色画法，这些图集保持原 alpha、以 Additive 方式绘制，不会产生白边。

## 自绘/合成素材

`StarDoorAtlas`、`DoorThemeChart`、`DoorThemeGate`、`DoorThemeKey`、`IllusoryDoorAtlas`、`SoftRingLarge`、`SoftRingThin`、`AstralSeal`、`TearAtlas`、`SpaceCrackAtlas`、`GlassBox`、`SoftVeil`、`SoftMist`、`DoorMote`、`SpaceShard` 由 `Draw-DoorAtlases.ps1` 用上述 CC0 素材作为笔刷重新绘制/合成。

`DomainSky`、`DomainSpires`、`DomainFog`、`DomainVignette` 由 `Build-DomainTextures.ps1` 生成（硬边像素风、抖动过渡、统一预乘 alpha）。

`SpatialDomainVoid` / `SpatialDomainFar` / `SpatialDomainNear`（空间牢笼的割离星系背景，512×512）与
`SpatialRiftVoid` / `SpatialRiftFar` / `SpatialRiftNear`（撕裂空间的竖长裂痕，160×640）由
`Build-SpatialLayers.ps1` 生成：固定层负责切口与暗角，另外两层星野各自带遮罩与抖动边缘，
在游戏里按视角偏移形成视差。裂痕外缘用确定性锯齿撕开，不是光滑椭圆。
同样使用 Kenney CC0 的烟雾笔刷打底，星点、星云带、抖动分色与边缘全部由脚本绘制，不含任何下载图片。

## 重新构建

```powershell
# 1. 下载并解压两个 CC0 素材包到 <SourceRoot>（目录内应包含 blender-volumetric-particles-effects_x 与 FX_x）
# 2. 重建门/领域自绘贴图
pwsh -File SourceAssets/Door/Draw-DoorAtlases.ps1
pwsh -File SourceAssets/Door/Build-DomainTextures.ps1
pwsh -File SourceAssets/Door/Build-SpatialLayers.ps1
# 3. 重建魔法核心图集（需要 Cethiel 的 SD.png，参数 -SourceSheet 指定路径）
pwsh -File SourceAssets/Door/Build-MagicFxAtlases.ps1
# 4. 生成预览（会还原预乘，显示与游戏一致）
pwsh -File SourceAssets/Door/previews/make-preview.ps1
```
