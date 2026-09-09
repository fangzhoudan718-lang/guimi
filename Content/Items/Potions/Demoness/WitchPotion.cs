using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Demoness
{
    public class WitchPotion : LotMItem
    {
        public override string Pathway => "Demoness";
        public override int RequiredSequence => 8; // 需序列8

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("女巫魔药");
            // Tooltip.SetDefault("序列7：女巫\n获得绝美的容貌与非凡的魅力，掌握冰霜与黑魔法。\n注意：服食此魔药将改变性别。");
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
            Item.rare = ItemRarityID.Green; // 序列7 绿色稀有度
            Item.value = Item.sellPrice(gold: 1);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                if (!modPlayer.IsBeyonder || modPlayer.baseDemonessSequence != 8)
                {
                    Main.NewText("你不是教唆者，无法晋升！", 255, 50, 50);
                    return true;
                }

                modPlayer.baseDemonessSequence = 7;

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item28, player.position);

                // 【修正点】：注意这里的 M 要大写
                if (player.Male)
                {
                    Main.NewText("你感到身体在剧烈重组，原有的特征正在消失...", 255, 100, 200);
                    player.Male = false; // 【修正点】：改为大写 Male
                }

                Main.NewText("你获得了操控冰霜与黑炎的力量。", 100, 255, 255);
                Main.NewText("晋升成功！序列7：女巫！", 255, 20, 147);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 腐化配方
            CreateDualRecipe(
                ModContent.ItemType<DemonessCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Obsidifish, 1),    // 黑渊魔鱼 -> 黑曜石鱼
                (ItemID.CrystalShard, 3),  // 玛瑙孔雀蛋 -> 水晶碎块
                (ItemID.Daybloom, 1),      // 金色曼陀罗 -> 日光花
                (ItemID.ShadowScale, 3),   // 阴影蜥蜴 -> 暗影鳞片
                (ItemID.Waterleaf, 2)      // 水仙花 -> 水叶草
            );

            // 猩红配方
            CreateDualRecipe(
               ModContent.ItemType<DemonessCard>(),
               (ItemID.BottledWater, 1),
               (ItemID.Obsidifish, 1),
               (ItemID.CrystalShard, 3),
               (ItemID.Daybloom, 1),
               (ItemID.TissueSample, 3),  // 组织样本
               (ItemID.Waterleaf, 2)
           );
        }
    }
}