using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria.IO;
using Terraria.ID;
using System.Collections.Generic;
using SubworldLibrary;
using Terraria.GameContent.Generation;
using Microsoft.Xna.Framework;

namespace zhashi.Content.Dimensions
{
    public class MirrorWorld : Subworld
    {
        public override int Width => 1200; // 稍微改小一点以便快速生成测试
        public override int Height => 600;

        // ★★★ 核心修改1：暂时设为 false，防止读取旧的“坏”存档 ★★★
        // 等你觉得世界生成完美了，再改回 true
        public override bool ShouldSave => false;

        public override string Name => "Mirror Realm"; // 改个名字，确保生成新世界

        public override List<GenPass> Tasks => new List<GenPass>()
        {
            // 1. 强力清空 (确保无墙)
            new PassLegacy("Clearing", (progress, configuration) =>
            {
                progress.Message = "Polishing the Mirror Surface...";
                Main.worldSurface = Height / 2;
                Main.rockLayer = Main.worldSurface + 200;

                for (int x = 0; x < Main.maxTilesX; x++)
                {
                    for (int y = 0; y < Main.maxTilesY; y++)
                    {
                        Tile tile = Main.tile[x, y];
                        tile.ClearEverything();
                        tile.WallType = 0; // 确保墙壁ID为0 (透明/无)
                    }
                }
            }),

            // 2. 生成岛屿 (移除冰块)
            new PassLegacy("Shattered Glass Islands", (progress, configuration) =>
            {
                progress.Message = "Reflecting reality...";
                int islandCount = Width / 10;

                for (int i = 0; i < islandCount; i++)
                {
                    int x = WorldGen.genRand.Next(50, Width - 50);
                    int y = WorldGen.genRand.Next(50, Height - 50);

                    // ★★★ 核心修改2：只保留玻璃和水晶，删除了 IceBlock ★★★
                    int tileType = TileID.Glass;
                    if (WorldGen.genRand.NextBool(5)) tileType = TileID.CrystalBlock;

                    GenerateShardIsland(x, y, tileType);
                }
            }),

            // 3. 再次清理 (双重保险，防止生成岛屿时带出了墙)
            new PassLegacy("Wall Cleanup", (progress, configuration) =>
            {
                for (int x = 0; x < Main.maxTilesX; x++)
                {
                    for (int y = 0; y < Main.maxTilesY; y++)
                    {
                        Main.tile[x, y].WallType = 0;
                    }
                }
            }),

            // 4. 设置出生点
            new PassLegacy("Spawn Point", (progress, configuration) =>
            {
                int centerX = Width / 2;
                int centerY = Height / 2;
                // 生成出生平台
                for (int x = centerX - 10; x < centerX + 10; x++)
                {
                    for (int y = centerY; y < centerY + 3; y++)
                    {
                        WorldGen.PlaceTile(x, y, TileID.Glass, true, true);
                        Main.tile[x, y].WallType = 0; // 再次确保没墙
                    }
                }
                Main.spawnTileX = centerX;
                Main.spawnTileY = centerY - 2;
            })
        };

        private void GenerateShardIsland(int x, int y, int tileType)
        {
            int size = WorldGen.genRand.Next(5, 12);
            WorldGen.TileRunner(x, y, size, WorldGen.genRand.Next(3, 8), tileType, true, 0f, 0f, true, true);
        }
    }
}