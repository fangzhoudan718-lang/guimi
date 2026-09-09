using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria.ID;
using Terraria.IO;
using System.Collections.Generic;
using SubworldLibrary;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Dimensions
{
    public class OriginCastle : Subworld
    {
        public override int Width => 1200;
        public override int Height => 800;
        public override bool ShouldSave => true;
        public override string Name => "Origin Castle";

        public override List<GenPass> Tasks => new List<GenPass>()
        {
            // 1. 清空区域
            new ActionGenPass("Clearing", (progress, configuration) =>
            {
                progress.Message = "Navigating the Gray Fog...";
                Main.worldSurface = Height / 2;
                Main.rockLayer = Main.worldSurface + 100;

                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        Tile tile = Main.tile[x, y];
                        tile.HasTile = false;
                        tile.WallType = 0;
                        tile.LiquidAmount = 0;
                        tile.TileType = 0;
                    }
                }
            }),

            // 2. 建造源堡大厅
            new ActionGenPass("Manifesting The Palace", (progress, configuration) =>
            {
                progress.Message = "Constructing the Divine Hall...";
                int centerX = Width / 2;
                int centerY = Height / 2;
                int floorLevel = centerY + 10;

                // --- 1. 铺设宏大的地板 (石板) ---
                // 宽度 100 格，厚度 5 格
                for (int x = centerX - 50; x <= centerX + 50; x++)
                {
                    for (int y = floorLevel; y <= floorLevel + 5; y++)
                    {
                        WorldGen.PlaceTile(x, y, TileID.StoneSlab, true, true);
                    }
                }

                // --- 2. 建造背景墙 (大理石墙) ---
                // 营造宫殿内部的感觉，高 20 格
                for (int x = centerX - 45; x <= centerX + 45; x++)
                {
                    for (int y = floorLevel - 20; y < floorLevel; y++)
                    {
                        WorldGen.PlaceWall(x, y, WallID.MarbleBlock);
                    }
                }

                // --- 3. 巨大的石柱 (左右两侧) ---
                for (int y = floorLevel - 20; y < floorLevel; y++)
                {
                    // 左柱
                    WorldGen.PlaceTile(centerX - 40, y, TileID.MarbleColumn, true, true);
                    WorldGen.PlaceTile(centerX - 39, y, TileID.MarbleColumn, true, true); // 加粗
                    
                    // 右柱
                    WorldGen.PlaceTile(centerX + 40, y, TileID.MarbleColumn, true, true);
                    WorldGen.PlaceTile(centerX + 39, y, TileID.MarbleColumn, true, true); // 加粗
                }

                // --- 4. 青铜长桌 (用大理石桌拼接模拟) ---
                // 长度 22 格 (左右各延伸 10 格)
                // 这里的 style 13 是大理石桌，看起来比较高贵
                for (int x = centerX - 10; x <= centerX + 10; x += 3) // 桌子通常宽3格左右
                {
                    WorldGen.PlaceTile(x, floorLevel - 1, TileID.Tables, true, true, style: 13);
                }

                // --- 5. 愚者的座位 (最上首的王座) ---
                // 放在中心位置
                // 清除中心桌子给王座腾位置，或者直接把王座放在桌子后面（背景墙上），这里我们放在桌子正中间替代桌子
                // 为了视觉效果，我们把王座放在桌子"后面"稍微高一点，或者直接作为中心点
                
                // 我们让长桌在王座前方展开。
                // 重修桌子布局：
                // 王座在 centerX
                WorldGen.PlaceTile(centerX, floorLevel - 1, TileID.Thrones, true, true, style: 0); // 0是默认王座

                // 桌子分列两旁 (或者作为长桌在王座前)
                // 还是还原原著：愚者在上首，长桌在面前延伸。
                // 泰拉瑞亚是2D的，我们做成“左右展开”的长桌，愚者在正中间。
                
                // 放置两侧的高背椅 (塔罗会成员)
                for (int x = centerX - 10; x <= centerX + 10; x += 3)
                {
                    if (x == centerX) continue; // 跳过中间的王座位置
                    
                    // 放置椅子
                    WorldGen.PlaceTile(x, floorLevel - 1, TileID.Chairs, true, true, style: 13); // 大理石椅
                }

                // --- 6. 光源 (烛台) ---
                // 放在柱子上或者悬浮
                WorldGen.PlaceTile(centerX - 30, floorLevel - 10, TileID.Chandeliers, true, true, style: 1); // 左吊灯
                WorldGen.PlaceTile(centerX + 30, floorLevel - 10, TileID.Chandeliers, true, true, style: 1); // 右吊灯

                // --- 7. 设置出生点 ---
                // 让玩家出生在王座上
                Main.spawnTileX = centerX;
                Main.spawnTileY = floorLevel - 3;
            })
        };

        public override void OnEnter()
        {
            Main.dayTime = false;
            Main.time = 16200;

            // 清理 NPC
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active)
                {
                    Main.npc[i].active = false;
                }
            }
        }
    }

    public class ActionGenPass : GenPass
    {
        private System.Action<GenerationProgress, GameConfiguration> _method;
        public ActionGenPass(string name, System.Action<GenerationProgress, GameConfiguration> method) : base(name, 1f)
        {
            _method = method;
        }
        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            _method(progress, configuration);
        }
    }
}