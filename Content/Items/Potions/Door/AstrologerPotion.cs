using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Door
{
    /// <summary>
    /// 学徒/门途径 序列7：占星人 (Astrologer)
    /// 前置序列：8 戏法大师
    /// </summary>
    public class AstrologerPotion : LotMItem
    {
        public override string Pathway => "Door";
        public override int RequiredSequence => 8;

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
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(silver: 80);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseDoorSequence = 7;
                modPlayer.currentDoorSequence = 7;

                SoundEngine.PlaySound(SoundID.Item104, player.position);
                Main.NewText("水晶球的迷雾在你眼中散开,星象浮现 —— 你看见了未来。", 255, 220, 120);
                Main.NewText("晋升成功：序列7 占星人！", 255, 215, 0);
                Main.NewText("能力: 灵性直觉(自动闪避) / 危险预感(+减伤) / 灵性干扰(敌人混乱) / 占星术 [G]", 255, 200, 130);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DoorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.FallenStar, 3),            // 坠落之星 (占星核心)
                (ItemID.Sapphire, 2),              // 蓝宝石 (水晶球材料)
                (ItemID.Bottle, 3),                // 瓶子 (装水晶碎屑)
                (ItemID.MagicMirror, 1)            // 魔镜 (灵性反射)
            );
        }
    }
}
