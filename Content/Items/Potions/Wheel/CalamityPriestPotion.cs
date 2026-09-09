using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Wheel
{
    public class CalamityPriestPotion : LotMItem
    {
        public override string Pathway => "Wheel";
        public override int RequiredSequence => 7; // 前置：序列7 幸运儿

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
            Item.rare = ItemRarityID.LightRed; // 序列6 变强了
            Item.value = Item.buyPrice(gold: 5);
            Item.buffType = BuffID.Lucky;
            Item.buffTime = 7200;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 晋升逻辑
                modPlayer.baseWheelSequence = 6;
                modPlayer.currentWheelSequence = 6;

                // 音效：雷声 (象征灾祸)
                SoundEngine.PlaySound(SoundID.Thunder, player.position);

                Main.NewText("天空阴沉了下来... 灾祸开始在你身边聚集。", 128, 0, 128);
                Main.NewText("晋升成功：序列6 灾祸教士！", 255, 215, 0);
                Main.NewText("获得能力：[灾祸光环] 与 [精神风暴]", 200, 200, 255);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.MeteoriteBar, 5), // 陨石：天降灾祸
                (ItemID.SoulofNight, 5),  // 暗影魂：灾厄
                (ItemID.CursedFlame, 2),  // 诅咒焰/灵液：痛苦与折磨
                (ItemID.Bone, 10)         // 骨头：死亡
            );

            // 猩红世界配方兼容
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.MeteoriteBar, 5),
                (ItemID.SoulofNight, 5),
                (ItemID.Ichor, 2),
                (ItemID.Bone, 10)
            );
        }
    }
}