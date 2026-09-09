using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用力量牌

namespace zhashi.Content.Items.Potions
{
    // 1. 继承 LotMItem
    public class PugilistPotion : LotMItem
    {
        // 2. 设定途径和前置序列 (需要序列9 战士)
        public override string Pathway => "Giant";
        public override int RequiredSequence => 9;

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
            Item.rare = ItemRarityID.Green; // 绿色稀有度
            Item.value = Item.buyPrice(gold: 1);
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 1. 检查是否为序列9
            if (modPlayer.baseSequence == 9)
            {
                modPlayer.baseSequence = 8;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

                Main.NewText("你服用了格斗家魔药，肌肉与骨骼发出了爆鸣声... 晋升成功：序列8 格斗家！", 255, 150, 50);
                return true;
            }
            // 2. 检查是不是还没入门
            else if (modPlayer.baseSequence > 9)
            {
                Main.NewText("你的身体太孱弱了，先成为战士吧。", 200, 50, 50);
                return true;
            }
            else
            {
                Main.NewText("你已经是更高序列的存在，无需服用。", 200, 200, 200);
                return true;
            }
        }

        // ==========================================
        // 配方升级：支持力量牌免石板 (兼容腐化/猩红)
        // ==========================================
        public override void AddRecipes()
        {
            // 方案 A：腐化世界 (魔矿锭)
            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(), // 力量牌

                (ItemID.BottledWater, 1),
                (ItemID.DemoniteBar, 5), // 魔矿锭
                (ItemID.Gel, 50),        // 凝胶
                (ItemID.JungleSpores, 3) // 丛林孢子
            );

            // 方案 B：猩红世界 (猩红矿锭)
            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(), // 力量牌

                (ItemID.BottledWater, 1),
                (ItemID.CrimtaneBar, 5), // 猩红矿锭
                (ItemID.Gel, 50),
                (ItemID.JungleSpores, 3)
            );
        }
    }
}