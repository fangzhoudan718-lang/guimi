# Content 目录约定

源码采用“内容类型 → 途径/领域”的两层结构：

- `Items/Potions/<Pathway>`：各途径魔药与晋升物品。
- `Items/Weapons/<Pathway>`：各途径武器；`Artifacts` 放封印物武器，`Ritual` 放仪式武器。
- `Projectiles/<Pathway>`：各途径弹幕；`Bosses`、`Pets`、`Ritual` 分别存放首领、宠物和仪式弹幕。
- `Buffs/<Pathway>`：各途径增益、减益和诅咒。
- `Effects/<Pathway>`：纯视觉或屏幕特效；`Shared` 存放跨途径特效。
- `Systems/Gameplay`：持续玩法系统。
- `Systems/Rituals`：独立的晋升仪式判定器。
- `Systems/WorldGeneration`：世界生成与宝箱注入。
- `Systems/Infrastructure`：音频预加载等基础设施。

## 资源路径规则

本次整理只改变 `.cs` 源码位置，不移动 `.png`、音频等资源，也不修改已有命名空间。
tModLoader 默认贴图路径仍由类型命名空间决定，因此旧资源键保持有效。
可编辑的 PSD 等源素材统一放在根目录 `SourceAssets`，该目录不会被打入 `.tmod`。

后续新增内容时，源码应直接放入上述分类。若希望同时移动贴图，必须显式覆盖 `Texture`
属性或同步调整命名空间和全部字符串资源键，并在提交前执行一次 tModLoader 正式构建。
