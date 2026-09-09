using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content.Items.Accessories; // 引用月亮牌

namespace zhashi.Content.Items.Potions.Moon
{
    public class ScarletScholarPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Moon";
        public override int RequiredSequence => 6; // 需要序列6 (魔药教授)

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
            Item.rare = ItemRarityID.Pink; // 序列5 粉色
            Item.value = Item.sellPrice(gold: 10);
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 晋升
                modPlayer.baseMoonSequence = 5;
                // modPlayer.currentMoonSequence = 5;

                // 视觉与音效
                CombatText.NewText(player.getRect(), Color.Crimson, "晋升：深红学者", true);
                Main.NewText("你的眼中映照出一轮猩红的圆月...", 220, 20, 60); // 深红

                // 播放诡异的魔法音效 (Item119 是疯狂飞斧的声音，有点诡异感，或者用 Item103)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item119, player.position);

                // 特效：深红色月光/血气
                for (int i = 0; i < 40; i++)
                {
                    // 使用 VampireHeal (吸血鬼) 粒子，非常符合深红学者主题
                    Dust d = Dust.NewDustPerfect(player.Center, DustID.VampireHeal, Main.rand.NextVector2Circular(5, 5), 0, default, 1.5f);
                    d.noGravity = true;
                }
            }
            return true;
        }

        // 3. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<MoonCard>(), // 核心：月亮牌
                (ItemID.BottledWater, 1),
                (ItemID.HallowedBar, 5),     // 神圣锭
                (ItemID.SoulofNight, 10),    // 暗影之魂 (黑夜/月亮)
                (ItemID.SoulofLight, 5),     // 光明之魂 (月光)
                (ItemID.UnicornHorn, 1)      // 独角兽角 (纯洁/灵性材料)
            );
        }
    }
}