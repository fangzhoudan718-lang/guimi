using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;

namespace zhashi.Content.Items.Potions.Wheel
{
    public class MonsterPotion : LotMItem
    {
        // 1. 基础设定 (父类会自动识别 Wheel 并读取 baseWheelSequence)
        public override string Pathway => "Wheel";
        public override int RequiredSequence => 10;

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

        // 2. 晋升逻辑
        // 此时 base.CanUseItem 已经能正确识别命运途径，所以我们可以像 UnshadowedPotion 一样
        // 只写特有的检查（比如防止双修），或者如果父类已经处理了，这里甚至可以不写 CanUseItem
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 防止双修的检查 (根据你的战士魔药逻辑)
                if (modPlayer.IsBeyonder && modPlayer.baseWheelSequence == 10)
                {
                    Main.NewText("你的灵性已定型，无法开启第二条途径！", 255, 50, 50);
                    return true;
                }

                // 晋升
                modPlayer.baseWheelSequence = 9;
                modPlayer.currentWheelSequence = 9;

                SoundEngine.PlaySound(SoundID.ZombieMoan, player.position);
                Main.NewText("耳畔传来了虚幻的呢喃声...", 175, 75, 255);
                Main.NewText("晋升成功：序列9 怪物！", 255, 215, 0);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.Blinkroot, 1),
                (ItemID.Lens, 1)
            );
        }
    }
}