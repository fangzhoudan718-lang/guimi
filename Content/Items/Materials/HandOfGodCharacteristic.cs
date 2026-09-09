using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Items.Materials
{
    public class HandOfGodCharacteristic : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 99;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(platinum: 2);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                // 假设你有亵渎石板这个物品，如果没有请修改这里
                .AddIngredient(ModContent.ItemType<BlasphemySlate>(), 1)
                // 假设你有非凡血液这个物品
                .AddIngredient(ModContent.ItemType<ExtraordinaryBlood>(), 10)
                // 【核心修复】这里必须用 LunarBar，不能用 LuminiteBar
                .AddIngredient(ItemID.LunarBar, 5)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}