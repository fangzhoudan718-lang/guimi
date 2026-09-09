using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items;

namespace zhashi.Content.Systems
{
    public class BlasphemySlateGen : ModSystem
    {
        public override void PostWorldGen()
        {
            // 遍历所有宝箱
            for (int chestIndex = 0; chestIndex < Main.maxChests; chestIndex++)
            {
                Chest chest = Main.chest[chestIndex];

                // 必须检查 chest 是否为 null
                if (chest != null)
                {
                    Tile tile = Main.tile[chest.x, chest.y];

                    if (tile.TileType == TileID.Containers || tile.TileType == TileID.Containers2)
                    {
                        // 【核心修复】20% 概率 (1/5)
                        if (Main.rand.NextBool(5))
                        {
                            // 寻找空位
                            for (int inventoryIndex = 0; inventoryIndex < 40; inventoryIndex++)
                            {
                                if (chest.item[inventoryIndex].type == ItemID.None)
                                {
                                    // 创建物品实例
                                    int type = ModContent.ItemType<BlasphemySlate>();
                                    chest.item[inventoryIndex].SetDefaults(type);

                                    // 设置为未观测状态 (-1)
                                    if (chest.item[inventoryIndex].ModItem is BlasphemySlate slate)
                                    {
                                        slate.recipeIndex = -1;
                                    }

                                    // 放入一个后就停止，防止一个箱子刷好几个
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}