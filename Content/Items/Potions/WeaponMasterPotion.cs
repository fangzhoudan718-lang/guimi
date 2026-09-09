using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用力量牌

namespace zhashi.Content.Items.Potions
{
    // 1. 继承 LotMItem
    public class WeaponMasterPotion : LotMItem
    {
        // 2. 设定途径和前置序列 (需要序列8 格斗家)
        public override string Pathway => "Giant";
        public override int RequiredSequence => 8;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.rare = ItemRarityID.Orange; // 橙色稀有度
            Item.value = Item.buyPrice(gold: 3);
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 1. 检查是否为序列8
            if (modPlayer.baseSequence == 8)
            {
                modPlayer.baseSequence = 7;
                // 播放更强的吼声
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);
                Main.NewText("你对武器的理解达到了新的高度... 晋升成功：序列7 武器大师！", 255, 165, 0); // 橙色字
                return true;
            }
            // 2. 检查前置
            else if (modPlayer.baseSequence > 8)
            {
                Main.NewText("你还未掌握格斗家的技巧，无法晋升。", 200, 50, 50);
                return true;
            }
            else
            {
                Main.NewText("你已经是更高序列的存在，无需重复服用。", 200, 200, 200);
                return true;
            }
        }

        // ==========================================
        // 配方升级：支持力量牌免石板 (兼容腐化/猩红)
        // ==========================================
        public override void AddRecipes()
        {
            // 方案 A：腐化世界 (暗影鳞片)
            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(), // 力量牌

                (ItemID.BottledWater, 1),
                (ItemID.ShadowScale, 10), // 暗影鳞片
                (ItemID.Obsidian, 5),     // 黑曜石
                (ItemID.Stinger, 3)       // 毒刺
            );

            // 方案 B：猩红世界 (组织样本)
            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(), // 力量牌

                (ItemID.BottledWater, 1),
                (ItemID.TissueSample, 10), // 组织样本
                (ItemID.Obsidian, 5),
                (ItemID.Stinger, 3)
            );
        }
    }
}