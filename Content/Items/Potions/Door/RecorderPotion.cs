using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Door
{
    /// <summary>
    /// 学徒/门途径 序列6：记录官 (Recorder)
    /// 前置序列：7 占星人
    /// </summary>
    public class RecorderPotion : LotMItem
    {
        public override string Pathway => "Door";
        public override int RequiredSequence => 7;

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
            Item.value = Item.buyPrice(gold: 1);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();
                if (modPlayer.baseDoorSequence != RequiredSequence) return false;
                modPlayer.baseDoorSequence = 6;
                modPlayer.currentDoorSequence = 6;
                if (Main.netMode == NetmodeID.MultiplayerClient) modPlayer.SyncPlayer(-1, -1, false);
                PromotionPulse.Raise(player, PromotionPulse.Door);
                SoundEngine.PlaySound(SoundID.Roar, player.position);
                Main.NewText("「我来到, 我看见, 我记录。」", 220, 180, 255);
                Main.NewText("异变的大脑在你颅内苏醒,从此你能记录目击到的非凡能力...", 220, 200, 130);
                Main.NewText("晋升成功：序列6 记录官！", 255, 215, 0);
                Main.NewText($"能力: 击杀非凡敌人获得记录；普通记录 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_RecordNormal)}] / 神性记录 [{LotMKeybinds.GetBindingText(LotMKeybinds.Door_RecordDivine)}]", 220, 200, 150);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DoorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Book, 3),              // 对应陈旧日记书页
                (ItemID.BlackLens, 1),         // 对应古老怨灵诅咒物，且保持本序列阶段可达
                (ItemID.Bone, 20),             // 对应完整阿斯曼之脑的死亡象征
                (ItemID.SoulofLight, 5),
                (ItemID.SoulofNight, 5)
            );
        }
    }
}
