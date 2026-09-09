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
                modPlayer.baseDoorSequence = 6;
                modPlayer.currentDoorSequence = 6;

                SoundEngine.PlaySound(SoundID.Roar, player.position);
                Main.NewText("「我来到, 我看见, 我记录。」", 220, 180, 255);
                Main.NewText("异变的大脑在你颅内苏醒,从此你能记录目击到的非凡能力...", 220, 200, 130);
                Main.NewText("晋升成功：序列6 记录官！", 255, 215, 0);
                Main.NewText("能力: 击杀有非凡能力的敌人时获得能力印记 [F普通记录 / C神性记录]", 220, 200, 150);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DoorCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.SoulofLight, 5),       // 光明之魂(记录纯净力量)
                (ItemID.SoulofNight, 5),       // 暗影之魂(记录黑暗力量)
                (ItemID.Bone, 30),             // 骨头(异变大脑符号)
                (ItemID.PinkGel, 10)           // 粉胶(灵性载体)
            );
        }
    }
}
