using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace zhashi.Content.Items.Vanities
{
    // [AutoloadEquip] 特性会自动寻找对应的装备贴图 Monocle_Face.png
    [AutoloadEquip(EquipType.Face)]
    public class Monocle : ModItem // 改为继承 ModItem，移除 LotM 逻辑限制
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.accessory = true; // 设为饰品槽位可佩戴
            Item.vanity = true;    // 标记为时装物品，不提供战斗属性

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(gold: 1);
        }

        public override void AddRecipes()
        {
            // 任何人都可以直接在工作台用 1 个玻璃制作
            CreateRecipe()
                .AddIngredient(ItemID.Glass, 1)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}