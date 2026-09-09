using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using zhashi.Content.Items.Accessories; // 引用月亮牌

namespace zhashi.Content.Items.Potions.Moon
{
    public class BeastTamerPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Moon";
        public override int RequiredSequence => 9; // 需要序列9 (药师)

        public override void SetStaticDefaults()
        {
            // 名字和描述在 HJSON 中
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Green; // 序列8 绿色
            Item.value = Item.sellPrice(gold: 1);
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 晋升
                modPlayer.baseMoonSequence = 8;
                // modPlayer.currentMoonSequence = 8; // 可选：手动同步

                // 视觉与音效
                CombatText.NewText(player.getRect(), Color.Orange, "晋升：驯兽师", true);
                Main.NewText("你能听懂野兽的低语，力量充盈全身...", 255, 140, 0); // 兽性橙

                // 播放咆哮音效，符合驯兽师特征
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);
            }
            return true;
        }

        // 3. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<MoonCard>(), // 核心：月亮牌
                (ItemID.BottledWater, 1),
                (ItemID.Sapphire, 1),            // 蓝宝石 (灵性)
                (ItemID.JungleSpores, 3),        // 丛林孢子 (自然/野性)
                (ItemID.Daybloom, 1),            // 太阳花
                (ItemID.LesserHealingPotion, 1), // 弱效治疗药水 (生命力)
                (ItemID.IronBar, 1)              // 铁锭 (驯化工具)
            );
        }
    }
}