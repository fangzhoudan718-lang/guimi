using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Wheel
{
    public class RobotPotion : LotMItem
    {
        // 1. 基础设定
        public override string Pathway => "Wheel";
        public override int RequiredSequence => 9; // 父类会自动检查 baseWheelSequence == 9

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
            Item.value = Item.buyPrice(gold: 1);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 600;
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 这里不需要再手动检查 sequence == 9 了，因为父类已经帮我们把关了
                // 只要能进到这里，说明一定是序列9
                modPlayer.baseWheelSequence = 8;
                modPlayer.currentWheelSequence = 8;

                SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你的思维变得冰冷而精密...", 175, 75, 255);
                Main.NewText("晋升成功：序列8 机器！", 255, 215, 0);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 配方1：铁 + 银 (经典组合)
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(), // 核心
                (ItemID.BottledWater, 1),
                (ItemID.IronBar, 8),      // 增加数量，作为主体
                (ItemID.SilverBar, 5),    // 银锭：象征精密传导
                (ItemID.Glass, 3),        // 玻璃：象征光学传感器/镜头
                (ItemID.Chain, 2),        // 铁链：象征机械传动
                (ItemID.Topaz, 1)         // 黄玉：计算核心
            );

            // 配方2：铅 + 钨 (替代金属组合)
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.LeadBar, 8),
                (ItemID.TungstenBar, 5),
                (ItemID.Glass, 3),
                (ItemID.Chain, 2),
                (ItemID.Topaz, 1)
            );
        }
    }
}