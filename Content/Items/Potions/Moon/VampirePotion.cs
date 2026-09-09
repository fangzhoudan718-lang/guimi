using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content.Items.Accessories; // 引用月亮牌

namespace zhashi.Content.Items.Potions.Moon
{
    public class VampirePotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Moon";
        public override int RequiredSequence => 8; // 需要序列8 (驯兽师)

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
            Item.rare = ItemRarityID.Orange; // 序列7 橙色
            Item.value = Item.sellPrice(gold: 3);
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 晋升
                modPlayer.baseMoonSequence = 7;
                // modPlayer.currentMoonSequence = 7;

                // 视觉与音效
                CombatText.NewText(player.getRect(), Color.DarkRed, "晋升：吸血鬼", true);
                Main.NewText("你感到心脏剧烈跳动，对鲜血产生了渴望...", 220, 20, 60); // 深红

                // 播放暗影/吸血音效 (Item103 是暗影束法杖的声音，比较低沉)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103, player.position);

                // 特效：鲜血飞溅
                for (int i = 0; i < 30; i++)
                {
                    Vector2 speed = Main.rand.NextVector2Circular(5, 5);
                    // DustID.Blood (鲜血)
                    Dust d = Dust.NewDustPerfect(player.Center, DustID.Blood, speed, 0, default, 1.5f);
                    d.noGravity = false; // 让血滴落下
                }
            }
            return true;
        }

        // 3. 多重双配方支持 (适配腐化/猩红)
        public override void AddRecipes()
        {
            // 方案 A：适配腐化世界 (暗影鳞片)
            CreateDualRecipe(
                ModContent.ItemType<MoonCard>(), // 核心：月亮牌
                (ItemID.BottledWater, 1),
                (ItemID.ShadowScale, 5),   // 暗影鳞片 (腐化材料)
                (ItemID.Bone, 10),         // 骨头 (亡灵/地牢)
                (ItemID.WaterCandle, 1),   // 水蜡烛 (吸引怪物/诅咒)
                (ItemID.Moonglow, 3),      // 月光草
                (ItemID.Deathweed, 3)      // 死亡草
            );

            // 方案 B：适配猩红世界 (组织样本)
            CreateDualRecipe(
                ModContent.ItemType<MoonCard>(), // 核心：月亮牌
                (ItemID.BottledWater, 1),
                (ItemID.TissueSample, 5),  // 组织样本 (血腥材料)
                (ItemID.Bone, 10),
                (ItemID.WaterCandle, 1),
                (ItemID.Moonglow, 3),
                (ItemID.Deathweed, 3)
            );
        }
    }
}