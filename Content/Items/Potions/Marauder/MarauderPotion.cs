using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用恋人牌

namespace zhashi.Content.Items.Potions.Marauder
{
    public class MarauderPotion : LotMItem
    {
        public override string Pathway => "Marauder";
        public override int RequiredSequence => 0; // 0 代表无序列限制（凡人可服）

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
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 600;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 序列9 特有逻辑：检查是否已经是其他途径的非凡者
            if (modPlayer.IsBeyonder)
            {
                Main.NewText("你的灵性已定型，无法开启第二条途径，强行服用只会导致失控！", 255, 50, 50);
                return true; // 消耗物品但无效
            }

            // 晋升逻辑
            modPlayer.baseMarauderSequence = 9;
            Main.NewText("你的手指变得灵活，眼中闪烁着奇异的光芒... 你成为了[偷盗者]！", 50, 50, 200);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);

            return true;
        }

        // 修正后的配方逻辑：使用基类的 CreateDualRecipe
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<LoversCard>(), // 对应的高阶媒介 (恋人牌)
                (ItemID.BottledWater, 1),
                (ItemID.Stinger, 1),      // 毒刺
                (ItemID.PinkGel, 1),      // 粉凝胶
                (ItemID.Sapphire, 1)      // 蓝宝石
            );
        }
    }
}