using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Wheel
{
    public class LuckyPotion : LotMItem
    {
        public override string Pathway => "Wheel";
        public override int RequiredSequence => 8; // 前置是序列8 机器

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
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(gold: 2);
            Item.buffType = BuffID.Lucky; // 喝完给个原版幸运Buff做彩蛋
            Item.buffTime = 3600;
        }

        // 逻辑已由父类 LotMItem 接管，这里只需处理特效
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 这里父类已经检查过序列了，直接写晋升逻辑
                modPlayer.baseWheelSequence = 7;
                modPlayer.currentWheelSequence = 7;

                SoundEngine.PlaySound(SoundID.Item4, player.position);

                // 更加戏剧性的提示
                Main.NewText("你感觉世界变得对你友善了一些...", 255, 215, 0);
                Main.NewText("晋升成功：序列7 幸运儿！", 255, 215, 0);
                Main.NewText("你开始偶尔遇上莫名其妙的好事。", 200, 200, 200);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Goldfish, 1),   // 金鱼 (象征好运)
                (ItemID.Sunflower, 1),  // 太阳花 (积极向上)
                (ItemID.Blinkroot, 1)
            );
        }
    }
}