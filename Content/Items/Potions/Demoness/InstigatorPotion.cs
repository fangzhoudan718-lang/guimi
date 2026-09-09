using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Demoness
{
    public class InstigatorPotion : LotMItem
    {
        public override string Pathway => "Demoness";
        public override int RequiredSequence => 9; // 需要序列9才能服用

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("教唆者魔药");
            // Tooltip.SetDefault("序列8：教唆者\n擅长挑拨离间，制造混乱，借他人之手消灭敌人。");
        }

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
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 80);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                if (!modPlayer.IsBeyonder || modPlayer.baseDemonessSequence != 9)
                {
                    Main.NewText("你没有吸收刺客魔药，或者序列不匹配！", 255, 50, 50);
                    return true;
                }

                modPlayer.baseDemonessSequence = 8; // 晋升为序列8
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);

                Main.NewText("你学会了如何用言语挑动人心的阴暗面...", 255, 105, 180); // 粉色文字
                Main.NewText("晋升成功！序列8：教唆者！", 255, 105, 180);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DemonessCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Bone, 5),             // 凶暴怨灵 -> 骨头
                (ItemID.RottenChunk, 3),      // 恶念魔人血 -> 腐肉
                (ItemID.ShadowScale, 2),      // 阴影毒蛇鳞 -> 暗影鳞片
                (ItemID.Waterleaf, 1)         // 蓝色妖姬 -> 水叶草
            );

            // 添加猩红之地版本的配方
            CreateDualRecipe(
               ModContent.ItemType<DemonessCard>(),
               (ItemID.BottledWater, 1),
               (ItemID.Bone, 5),
               (ItemID.Vertebrae, 3),        // 椎骨
               (ItemID.TissueSample, 2),     // 组织样本
               (ItemID.Waterleaf, 1)
           );
        }
    }
}