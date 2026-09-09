using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
// 【修复2】引用力量牌所在的命名空间，解决找不到 StrengthCard 的问题
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions
{
    // 【修复1】必须继承 LotMItem，而不是 ModItem，否则无法使用 CreateDualRecipe
    public class DawnKnightPotion : LotMItem
    {
        // 设定途径和前置序列 (父类 LotMItem 的功能)
        public override string Pathway => "Giant";
        public override int RequiredSequence => 7;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(gold: 10);
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSequence == 7)
            {
                modPlayer.baseSequence = 6;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

                Main.NewText("晨曦的光芒照亮了你的灵魂... 晋升成功：序列6 黎明骑士！", 255, 215, 0);
                Main.NewText("获得技能：【黎明铠甲】 (按 Z 键开启/关闭)", 255, 255, 255);
                return true;
            }
            else if (modPlayer.baseSequence > 7)
            {
                Main.NewText("你还未成为武器大师，无法承受晨曦之力。", 200, 50, 50);
                return true;
            }
            else
            {
                Main.NewText("你的光芒已足够耀眼。", 200, 200, 200);
                return true;
            }
        }

        public override void AddRecipes()
        {
            // 这里调用父类的方法
            CreateDualRecipe(
                ModContent.ItemType<StrengthCard>(),

                (ItemID.BottledWater, 1),
                (ItemID.HallowedBar, 5),
                (ItemID.SoulofLight, 10),
                (ItemID.Daybloom, 5)
            );
        }
    }
}