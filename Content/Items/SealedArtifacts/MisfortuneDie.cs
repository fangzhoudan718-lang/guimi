using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Buffs.Debuffs;

namespace zhashi.Content.Items.SealedArtifacts
{
    public class MisfortuneDie : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("封印物：厄运之骰"); // 如果没用hjson
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.accessory = true; // 设为饰品
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1);

            // 【已删除】Item.canBePlacedInVanityApparelSlots = false; 
            // 这行代码不存在，删除它即可。
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 1. 赋予厄运Buff
            player.AddBuff(ModContent.BuffType<ExtremeBadLuckBuff>(), 2);

            // 2. 增加仪式进度
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 只有当还未完成时才增加，封顶 86400
            if (modPlayer.misfortuneRitualTimer < LotMPlayer.MISFORTUNE_RITUAL_TARGET)
            {
                modPlayer.misfortuneRitualTimer++;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.GoldCoin, 1)
                .AddIngredient(ItemID.Deathweed, 5)
                .AddIngredient(ItemID.RottenChunk, 5)
                .AddIngredient(ItemID.Bone, 10)
                .AddTile(TileID.DemonAltar)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.GoldCoin, 1)
                .AddIngredient(ItemID.Deathweed, 5)
                .AddIngredient(ItemID.Vertebrae, 5)
                .AddIngredient(ItemID.Bone, 10)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}