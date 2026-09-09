using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content.Items.Accessories; // 引用月亮牌

namespace zhashi.Content.Items.Potions.Moon
{
    public class PotionsProfessorPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Moon";
        public override int RequiredSequence => 7; // 需要序列7 (吸血鬼)

        public override void SetStaticDefaults()
        {
            // 名字和描述在 HJSON 中
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Pink; // 序列6 粉色
            Item.value = Item.sellPrice(gold: 5);
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 晋升
                modPlayer.baseMoonSequence = 6;
                // modPlayer.currentMoonSequence = 6;

                // 视觉与音效
                CombatText.NewText(player.getRect(), Color.MediumPurple, "晋升：魔药教授", true);
                Main.NewText("你洞悉了物质与灵性的转化规律...", 200, 100, 255); // 魔法紫

                // 播放魔法音效
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item107, player.position);

                // 特效：药水气泡
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(player.position, player.width, player.height, DustID.BubbleBlock, 0, -2, 0, Color.Purple, 1.2f);
                }
            }
            return true;
        }

        // 3. 多重双配方支持
        public override void AddRecipes()
        {
            // 方案 A：适配猩红 (灵液) - 支持 牌/石板
            CreateDualRecipe(
                ModContent.ItemType<MoonCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.LifeCrystal, 1),    // 生命结晶 (生命)
                (ItemID.Ichor, 5),          // 灵液 (灵性材料)
                (ItemID.SharkFin, 3)        // 鲨鱼鳍
            );

            // 方案 B：适配腐化 (咒火) - 支持 牌/石板
            CreateDualRecipe(
                ModContent.ItemType<MoonCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.LifeCrystal, 1),
                (ItemID.CursedFlame, 5),    // 咒火 (灵性材料)
                (ItemID.SharkFin, 3)
            );
        }
    }
}