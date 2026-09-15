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
                // CanUseItem 是正常入口；这里再次校验，避免联机延迟或其他模组直接调用造成跨途径/降级。
                if (modPlayer.baseDoorSequence != 10 || modPlayer.IsBeyonder) return false;

                modPlayer.baseDoorSequence = 9;
                modPlayer.currentDoorSequence = 9;
                if (Main.netMode == NetmodeID.MultiplayerClient) modPlayer.SyncPlayer(-1, -1, false);
                PromotionPulse.Raise(player, PromotionPulse.Door);
                SoundEngine.PlaySound(SoundID.DoorOpen, player.position);
                Main.NewText("一扇虚幻的门在你心中开启,你听见了远方的脚步声...", 200, 180, 100);
                Main.NewText("晋升成功：序列9 学徒！", 255, 215, 0);
                Main.NewText($"能力: 按 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_OpenDoor)}] 开门，穿过附近墙壁。", 220, 220, 100);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DoorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Worm, 1),                  // 对应吞食宝石的蠕虫
                (ItemID.Amethyst, 1),              // 对应幻影水晶
                (ItemID.Deathweed, 1),             // 对应尸体上生长的花
                (ItemID.MudBlock, 5)               // 对应受灵界污染的土壤
            );
        }
    }
}
