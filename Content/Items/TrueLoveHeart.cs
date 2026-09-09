using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Items
{
    public class TrueLoveHeart : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.LifeCrystal;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("真爱之心"); // 1.4.4 版本不需要这个，直接用下面的
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 99;
            Item.value = Item.sellPrice(gold: 10); // 很贵重
            Item.rare = ItemRarityID.Red; // 红色稀有度
        }
    }
}