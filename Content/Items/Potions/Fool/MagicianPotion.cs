using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用愚者牌

namespace zhashi.Content.Items.Potions.Fool
{
    public class MagicianPotion : LotMItem
    {
        // 设定途径和前置序列 (需要序列8 小丑)
        public override string Pathway => "Fool";
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
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();
                modPlayer.baseFoolSequence = 7; // 晋升序列7

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
                Main.NewText("你感觉双手变得异常灵巧，火焰在你的掌心欢呼...", 255, 100, 0);
                Main.NewText("晋升成功！序列7：魔术师！", 50, 255, 50);

                // 晋升送空气弹技能书/物品
                player.QuickSpawnItem(player.GetSource_Misc("Promotion"), ModContent.ItemType<Items.Weapons.Fool.AirBullet>());
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用智能配方生成器
            CreateDualRecipe(
                ModContent.ItemType<FoolCard>(), // 愚者牌

                (ItemID.BottledWater, 1),
                (ItemID.Wood, 100),       // 树人根茎 (替代：木材)
                (ItemID.BlackInk, 10),    // 黑豹脊髓 (替代：黑墨水)
                (ItemID.Sapphire, 20),    // 水形宝石 (替代：蓝宝石)
                (ItemID.Deathweed, 10)    // 迷幻草 (替代：死亡草)
            );
        }
    }
}