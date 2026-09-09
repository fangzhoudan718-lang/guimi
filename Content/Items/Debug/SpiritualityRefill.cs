using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content;

namespace zhashi.Content.Items.Debug
{
    public class SpiritualityRefill : ModItem
    {
        // 【关键修复】使用原版魔力水晶贴图，解决蓝屏问题
        public override string Texture => "Terraria/Images/Item_" + ItemID.ManaCrystal;

        public override void SetStaticDefaults() { }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.rare = ItemRarityID.Red;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();
            modPlayer.spiritualityCurrent = modPlayer.spiritualityMax;
            Main.NewText("【测试】灵性已回满！", 0, 255, 255);
            return true;
        }
    }
}