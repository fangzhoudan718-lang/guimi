using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Marauder
{
    public class CryptologistPotion : LotMItem
    {
        public override string Pathway => "Marauder";
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
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 3);
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();
            if (modPlayer.baseMarauderSequence == 8)
            {
                modPlayer.baseMarauderSequence = 7;
                Main.NewText("纷乱的线条在眼中重组，你已掌握真理的钥匙。", 200, 50, 200);
                Main.NewText("晋升成功：序列7 解密学者！", 200, 50, 200);
                return true;
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<LoversCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.AntlionMandible, 5),
                (ItemID.EnchantedNightcrawler, 1),
                (ItemID.Book, 1), // 知识/解密
                (ItemID.Diamond, 1)
            );
        }
    }
}