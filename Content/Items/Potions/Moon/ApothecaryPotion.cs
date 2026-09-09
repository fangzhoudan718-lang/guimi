using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework; // 使用 Color
using zhashi.Content.Items.Accessories; // 引用月亮牌

namespace zhashi.Content.Items.Potions.Moon
{
    public class ApothecaryPotion : LotMItem
    {
        // 1. 定义途径
        public override string Pathway => "Moon";

        // 序列要求：0 代表无序列门槛 (凡人可服)
        public override int RequiredSequence => 0;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("药师魔药");
        }

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
            Item.rare = ItemRarityID.Blue; // 序列9 蓝色
            Item.value = Item.sellPrice(silver: 50);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 【核心逻辑】检查是否已是非凡者 (防止多重途径)
                if (modPlayer.IsBeyonder)
                {
                    Main.NewText("你的灵性已定型，无法开启第二条途径！强行服用导致魔药失效...", 255, 50, 50);
                    // 返回 true 代表消耗掉物品 (惩罚机制)
                    return true;
                }

                // 晋升逻辑：月亮途径
                modPlayer.baseMoonSequence = 9;

                // 视觉反馈
                CombatText.NewText(player.getRect(), Color.LightGreen, "晋升：药师", true);
                Main.NewText("你感觉自己对草药与生命有了更深的理解...", 100, 255, 100); // 药草绿

                // 音效
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用双配方：支持 石板 或 月亮牌
            CreateDualRecipe(
                ModContent.ItemType<MoonCard>(), // 核心：月亮牌
                (ItemID.BottledWater, 1),
                (ItemID.Stinger, 3),    // 毒刺 (药性/毒性)
                (ItemID.Glowstick, 7),  // 发光棒 (微光)
                (ItemID.Deathweed, 1)   // 死亡草 (药材)
            );
        }
    }
}