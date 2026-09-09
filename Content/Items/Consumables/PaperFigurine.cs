using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content; // 引用 LotMPlayer 所在的命名空间

namespace zhashi.Content.Items.Consumables
{
    public class PaperFigurine : ModItem
    {
        public override string Texture => "zhashi/Content/Items/Consumables/PaperFigurine";

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 1;
            Item.value = Item.buyPrice(0, 0, 5, 0);
            Item.rare = ItemRarityID.Blue;
            Item.consumable = true;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("纸人替身");
            // Tooltip.SetDefault("愚者途径的施法材料\n抵挡一次致命伤害并进行位移");
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.Book, 10)
                .AddTile(TileID.WorkBenches)
                // 【核心修改】添加合成条件
                // 只有 currentFoolSequence <= 9 (即开启了愚者途径) 的玩家可见并合成
                .AddCondition(new Condition("需要: 愚者途径 (序列9+)", () => Main.LocalPlayer.GetModPlayer<LotMPlayer>().currentFoolSequence <= 9))
                .Register();
        }
    }
}