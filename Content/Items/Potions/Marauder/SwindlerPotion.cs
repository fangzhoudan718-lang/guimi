using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用恋人牌

namespace zhashi.Content.Items.Potions.Marauder
{
    public class SwindlerPotion : LotMItem
    {
        public override string Pathway => "Marauder";

        // 序列要求：需要序列9 (偷盗者)
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
            Item.rare = ItemRarityID.Orange; // 序列8 橙色稀有度
            Item.value = Item.sellPrice(gold: 2);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 600;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升逻辑：LotMItem 基类已确保玩家是序列9
            modPlayer.baseMarauderSequence = 8;

            // 文本反馈：橙色字体对应稀有度
            Main.NewText("你感到思维变得更加敏捷，言语中充满了魔力...", 255, 165, 0);
            Main.NewText("晋升成功：序列8 诈骗师！", 255, 165, 0);

            // 播放音效：使用比较清脆的声音
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, player.position);

            return true;
        }

        public override void AddRecipes()
        {
            // 使用双配方：支持 石板 或 恋人牌
            CreateDualRecipe(
                ModContent.ItemType<LoversCard>(), // 核心：恋人牌
                (ItemID.BottledWater, 1),
                (ItemID.Worm, 1),           // 迷魂虫幼虫 -> 蠕虫
                (ItemID.JungleSpores, 1),   // 人面笼草 -> 丛林孢子
                (ItemID.Lens, 1),           // 他人眼泪 -> 晶状体
                (ItemID.Sapphire, 1)        // 青金石 -> 蓝宝石
            );
        }
    }
}