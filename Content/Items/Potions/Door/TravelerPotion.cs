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
                if (modPlayer.baseDoorSequence != RequiredSequence) return false;
                modPlayer.baseDoorSequence = 5;
                modPlayer.currentDoorSequence = 5;
                if (Main.netMode == NetmodeID.MultiplayerClient) modPlayer.SyncPlayer(-1, -1, false);
                PromotionPulse.Raise(player, PromotionPulse.Door);
                SoundEngine.PlaySound(SoundID.Item104, player.position);
                Main.NewText("你站在世界的中央,任何位置都能成为你的下一步。", 220, 180, 255);
                Main.NewText("晋升成功：序列5 旅行家！", 255, 215, 0);
                Main.NewText($"能力: 旅行家之门 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_TravelerGate)}] / 闪现 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_Blink)}] / 无形之手 / 神性记录强化", 220, 200, 150);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DoorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Worm, 1),              // 对应魔虫主材料
                (ItemID.FrostCore, 1),         // 对应无影魔狼心脏
                (ItemID.HallowedBar, 10),      // 对应受困幽灵残留的高阶载体
                (ItemID.BlackInk, 3),          // 绘制星图
                (ItemID.SoulofFlight, 10)      // 穿行灵界
            );
        }
    }
}
