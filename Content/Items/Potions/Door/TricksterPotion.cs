using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Door
{
    /// <summary>
    /// 学徒/门途径 序列8：戏法大师 (Trickster)
    /// 前置序列：9 学徒
    /// </summary>
    public class TricksterPotion : LotMItem
    {
        public override string Pathway => "Door";
        public override int RequiredSequence => 9;

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
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(silver: 80);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();
                if (modPlayer.baseDoorSequence != RequiredSequence) return false;
                modPlayer.baseDoorSequence = 8;
                modPlayer.currentDoorSequence = 8;
                if (Main.netMode == NetmodeID.MultiplayerClient) modPlayer.SyncPlayer(-1, -1, false);
                PromotionPulse.Raise(player, PromotionPulse.Door);
                SoundEngine.PlaySound(SoundID.Item25, player.position);
                Main.NewText("你学会了无数奇特的小戏法,如同马戏团里最受欢迎的表演者...", 220, 200, 120);
                Main.NewText("晋升成功：序列8 戏法大师！", 255, 215, 0);
                Main.NewText($"能力: [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_TrickSwitch)}]切换戏法，[{LotMKeybinds.GetBindingText(LotMKeybinds.Door_TrickCast)}]释放戏法。", 220, 220, 100);
                Main.NewText("12种戏法: 闪光/黑幕/转移气体/巨响/冰冻射线/电击/造雾/刮风/点火/摔倒术/驱物/逃脱(自动)", 200, 200, 200);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DoorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.SharkFin, 1),              // 对应深海枪鱼之血
                (ItemID.Cobweb, 20),               // 对应线球草
                (ItemID.Fireblossom, 1),           // 对应盛开的红栗花
                (ItemID.Stinger, 3)                // 对应食灵者胃袋，避免邪恶世界类型锁
            );
        }
    }
}
