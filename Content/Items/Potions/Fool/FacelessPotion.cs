using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用愚者牌

namespace zhashi.Content.Items.Potions.Fool
{
    public class FacelessPotion : LotMItem
    {
        // 设定途径和前置序列 (需要序列7 魔术师)
        public override string Pathway => "Fool";
        public override int RequiredSequence => 7;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.LightRed; // 序列6级别
            Item.value = Item.sellPrice(gold: 10);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseFoolSequence = 6; // 晋升序列6

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你忘记了自己的模样，又似乎记起了所有人的模样...", 200, 200, 255);
                Main.NewText("晋升成功！序列6：无面人！", 50, 255, 50);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 配方 A: 腐化世界 (腐肉 RottenChunk)
            CreateDualRecipe(
                ModContent.ItemType<FoolCard>(), // 愚者牌

                (ItemID.BottledWater, 1),
                (ItemID.RottenChunk, 5),  // 腐肉
                (ItemID.Bone, 10),        // 骨头
                (ItemID.Deathweed, 1),    // 死亡草
                (ItemID.JungleSpores, 3), // 丛林孢子
                (ItemID.SharkFin, 1)      // 鲨鱼鳍
            );

            // 配方 B: 猩红世界 (脊椎 Vertebrae)
            CreateDualRecipe(
                ModContent.ItemType<FoolCard>(), // 愚者牌

                (ItemID.BottledWater, 1),
                (ItemID.Vertebrae, 5),    // 脊椎
                (ItemID.Bone, 10),
                (ItemID.Deathweed, 1),
                (ItemID.JungleSpores, 3),
                (ItemID.SharkFin, 1)
            );
        }
    }
}