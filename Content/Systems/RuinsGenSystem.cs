using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using System.Collections.Generic;
using Terraria.IO;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.GameContent.Generation;
// 我们改为手动用代码生成，不再需要 StructureHelper 的引用
// using StructureHelper; 

namespace zhashi.Content.Systems
{
    public class RuinsGenSystem : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            // 在 "Shinies" (生成矿石) 之后插入我们的生成步骤
            int shinyIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Shinies"));
            if (shinyIndex != -1)
            {
                tasks.Insert(shinyIndex + 1, new PassLegacy("Generating LotM Cathedral", GenerateCathedral));
            }
        }

        private void GenerateCathedral(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "正在具现愚者的古老教堂...";

            // 生成数量：2到4个
            int count = Main.rand.Next(2, 4);
            int placed = 0;
            int attempts = 0;

            while (placed < count && attempts < 2000)
            {
                attempts++;
                // 随机找个位置 (避开地图两边)
                int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                int y = (int)Main.worldSurface;

                // 寻找地面 (从地表向下扫描)
                while (y < Main.maxTilesY - 200 && !Main.tile[x, y].HasTile)
                {
                    y++;
                }

                // 检查地面是否平整适合建造 (简单的检查)
                if (Main.tile[x, y].HasTile && Main.tileSolid[Main.tile[x, y].TileType])
                {
                    // === 开始用代码生成建筑 ===
                    GenerateProceduralRuin(x, y - 1);

                    placed++;
                    // 稍微把下一个建筑推远一点，防止重叠
                    x += 200;
                }
            }
        }

        // == 手写的一个简易教堂废墟生成器 ==
        private void GenerateProceduralRuin(int centerX, int groundY)
        {
            int width = 20;  // 建筑宽度
            int height = 15; // 建筑高度
            int leftX = centerX - width / 2;
            int floorY = groundY;

            // 1. 清理空间 (把建筑范围内的树木、杂草清掉)
            for (int i = leftX - 2; i <= leftX + width + 2; i++)
            {
                for (int j = floorY - height - 5; j <= floorY; j++)
                {
                    // 也就是把方块设为无 (false)
                    Main.tile[i, j].ClearTile();
                }
            }

            // 2. 铺地板 (黑曜石砖)
            for (int i = leftX; i < leftX + width; i++)
            {
                WorldGen.PlaceTile(i, floorY, TileID.ObsidianBrick, true, true);
                // 顺便在地板下面填点土，防止悬空
                WorldGen.PlaceTile(i, floorY + 1, TileID.Dirt, true, true);
            }

            // 3. 建柱子 (左右两边的墙壁)
            for (int j = 0; j < height; j++)
            {
                WorldGen.PlaceTile(leftX, floorY - j, TileID.ObsidianBrick, true, true); // 左墙
                WorldGen.PlaceTile(leftX + width - 1, floorY - j, TileID.ObsidianBrick, true, true); // 右墙
            }

            // 4. 建屋顶 (简单的平顶)
            for (int i = leftX; i < leftX + width; i++)
            {
                WorldGen.PlaceTile(i, floorY - height, TileID.ObsidianBrick, true, true);
            }

            // 5. 放置背景墙 (让它看起来像个房子)
            for (int i = leftX + 1; i < leftX + width - 1; i++)
            {
                for (int j = floorY - height + 1; j < floorY; j++)
                {
                    WorldGen.PlaceWall(i, j, WallID.ObsidianBrickUnsafe);
                }
            }

            // 6. 放置战利品箱子 (在屋子中间)
            int chestX = centerX;
            int chestY = floorY - 1;

            // 放置金箱子
            int chestIndex = WorldGen.PlaceChest(chestX, chestY, style: 1);

            if (chestIndex != -1)
            {
                Chest chest = Main.chest[chestIndex];

                // 放入一些奖励
                chest.item[0].SetDefaults(ItemID.GoldCoin);
                chest.item[0].stack = 10;

                // 可以在这里加你Mod的物品，比如:
                // chest.item[1].SetDefaults(ModContent.ItemType<Items.Materials.ExtraordinaryBlood>());
            }
        }
    }
}