using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Door
{
    /// <summary>
    /// 学徒/门途径 序列9：学徒 (Apprentice)
    /// </summary>
    public class ApprenticePotion : LotMItem
    {
        public override string Pathway => "Door";
        public override int RequiredSequence => 10;  // 起始途径,需求10(凡人)

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
            Item.value = Item.buyPrice(silver: 50);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 防止双修
                if (modPlayer.IsBeyonder && modPlayer.baseDoorSequence == 10)
                {
                    Main.NewText("你的灵性已定型，无法开启第二条途径！", 255, 50, 50);
                    return true;
                }

                modPlayer.baseDoorSequence = 9;
                modPlayer.currentDoorSequence = 9;

                SoundEngine.PlaySound(SoundID.DoorOpen, player.position);
                Main.NewText("一扇虚幻的门在你心中开启,你听见了远方的脚步声...", 200, 180, 100);
                Main.NewText("晋升成功：序列9 学徒！", 255, 215, 0);
                Main.NewText("能力: 短按 [开门键 / 默认E] 穿过附近的墙壁。", 220, 220, 100);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DoorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Wood, 5),                  // 木头(门的象征)
                (ItemID.SilverCoin, 5),            // 银币(学徒的束脩)
                (ItemID.Compass, 1)                // 指南针(寻路工具)
            );
        }
    }
}
