using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Door
{
    /// <summary>
    /// 学徒/门途径 序列5：旅行家 (Traveler)
    /// 前置序列：6 记录官
    /// </summary>
    public class TravelerPotion : LotMItem
    {
        public override string Pathway => "Door";
        public override int RequiredSequence => 6;

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
            Item.value = Item.buyPrice(gold: 2);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseDoorSequence = 5;
                modPlayer.currentDoorSequence = 5;

                SoundEngine.PlaySound(SoundID.Item104, player.position);
                Main.NewText("你站在世界的中央,任何位置都能成为你的下一步。", 220, 180, 255);
                Main.NewText("晋升成功：序列5 旅行家！", 255, 215, 0);
                Main.NewText("能力: 旅行家之门 [J] / 闪现 [K] / 无形之手(物品自动吸取) / 神性记录槽+3", 220, 200, 150);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DoorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.MagicMirror, 1),       // 魔镜(传送原型)
                (ItemID.Compass, 1),           // 指南针(定位)
                (ItemID.FallenStar, 5),        // 坠落之星(灵性载体)
                (ItemID.SoulofFlight, 5),      // 翼之魂(瞬移)
                (ItemID.GoldCoin, 5)           // 金币(财富)
            );
        }
    }
}
