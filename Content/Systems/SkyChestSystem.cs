using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items;

namespace zhashi.Content.Systems
{
    public class SkyChestSystem : ModSystem
    {
        public override void PostWorldGen()
        {
            // 存储所有找到的空岛箱子的索引
            List<int> skywareChestIndexes = new List<int>();

            // 遍历世界中所有的箱子
            for (int i = 0; i < Main.maxChests; i++)
            {
                Chest chest = Main.chest[i];
                // 确保箱子存在
                if (chest != null && chest.item[0] != null)
                {
                    // 获取箱子所在的物块信息
                    Tile tile = Main.tile[chest.x, chest.y];

                    // 判断是否为箱子物块 (TileID.Containers = 21)
                    // 并且判断样式是否为空岛箱 (Skyware Chest 的样式ID是 13)
                    // 每个样式占用 36 像素宽度的贴图坐标 (FrameX)
                    // 13 * 36 = 468
                    if (tile.TileType == TileID.Containers && tile.TileFrameX >= 468 && tile.TileFrameX < 468 + 36)
                    {
                        skywareChestIndexes.Add(i);
                    }
                }
            }

            // 如果找到了至少一个空岛箱子
            if (skywareChestIndexes.Count > 0)
            {
                // 随机选择其中一个
                int targetIndex = WorldGen.genRand.Next(skywareChestIndexes.Count);
                Chest targetChest = Main.chest[skywareChestIndexes[targetIndex]];

                // 尝试在箱子里找一个空位放入物品
                bool placed = false;
                for (int slot = 0; slot < 40; slot++)
                {
                    if (targetChest.item[slot].type == ItemID.None)
                    {
                        targetChest.item[slot].SetDefaults(ModContent.ItemType<UnshadowedCross>());
                        placed = true;
                        break;
                    }
                }

                // 如果箱子满了没空位，就强行替换最后一个格子
                if (!placed)
                {
                    targetChest.item[39].SetDefaults(ModContent.ItemType<UnshadowedCross>());
                }
            }
        }
    }
}