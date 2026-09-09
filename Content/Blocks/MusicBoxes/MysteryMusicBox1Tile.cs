using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.ID; // 引用ID以使用粒子和Gore
using zhashi.Content.Items.Placeable.MusicBoxes;

namespace zhashi.Content.Blocks.MusicBoxes
{
    public class MysteryMusicBox1Tile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileObsidianKill[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.addTile(Type);

            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(200, 200, 200), name);

            MusicLoader.AddMusicBox(
                Mod,
                MusicLoader.GetMusicSlot(Mod, "Assets/Music/Music1"),
                ModContent.ItemType<MysteryMusicBox1>(),
                ModContent.TileType<MysteryMusicBox1Tile>()
            );
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 16, 48, ModContent.ItemType<MysteryMusicBox1>());
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<MysteryMusicBox1>();
        }

        // ★★★ 新增：绘制特效 (音符粒子) ★★★
        public override void DrawEffects(int i, int j, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            Tile tile = Main.tile[i, j];

            // 判断是否处于播放状态 (FrameY >= 36)
            if (tile.TileFrameY >= 36)
            {
                // ★ 调试功能：播放时发出微弱的光 (确认代码是否运行)
                // 如果音乐盒亮了，说明代码判定成功；如果不亮，说明 FrameY 没变，或者是贴图问题导致的绘制中断
                Lighting.AddLight(new Vector2(i * 16, j * 16), 0.5f, 0.5f, 0.2f); // 淡黄色光

                // 确保只在 2x2 物块的“左上角”那个格子运行逻辑，避免每个格子都刷一次
                // 36 是 2x2 物块的一帧高度/宽度步长
                if (tile.TileFrameX % 36 == 0 && tile.TileFrameY % 36 == 0)
                {
                    // 提高概率：每帧 1/50 的概率生成 (如果不明显可以改成 1/30)
                    if (Main.rand.NextBool(50))
                    {
                        // 1. 确定生成位置：在物块中心上方一点点
                        Vector2 spawnPos = new Vector2(i * 16 + 16, j * 16 - 12);

                        // 2. 生成音符 Gore (原版音符 ID: 570, 571, 572)
                        int noteStyle = Main.rand.Next(570, 573);

                        // NewGore 参数: 源, 位置, 速度, ID
                        int g = Gore.NewGore(
                            new EntitySource_TileUpdate(i, j),
                            spawnPos,
                            new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -1f), // 慢慢向上飘
                            noteStyle
                        );

                        // 这一步很重要：确保 Gore 不会因为是在物块内部生成而被“剔除”
                        if (Main.gore.IndexInRange(g))
                        {
                            Main.gore[g].sticky = false; // 不粘在方块上
                            Main.gore[g].timeLeft = 120; // 存活时间
                        }

                        
                        Dust.NewDust(spawnPos, 4, 4, DustID.BlueCrystalShard, 0, -2f);
                    }
                }
            }
        }
    }
}