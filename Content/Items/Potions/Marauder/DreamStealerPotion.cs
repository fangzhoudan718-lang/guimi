using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用恋人牌

namespace zhashi.Content.Items.Potions.Marauder
{
    public class DreamStealerPotion : LotMItem
    {
        public override string Pathway => "Marauder";

        // 逻辑修正：这是序列5魔药，需要当前是序列6(盗火人)才能服用
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
            Item.rare = ItemRarityID.Pink; // 序列5 标准稀有度
            Item.value = Item.sellPrice(gold: 10);

            // 保留你原本设计的副作用：梦境过载导致的窒息感
            Item.buffType = BuffID.Suffocation;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 基类 LotMItem 里的 CanUseItem 已经帮你检查了 baseMarauderSequence == 6
            // 这里只需要处理晋升逻辑
            if (modPlayer.baseMarauderSequence == 6)
            {
                modPlayer.baseMarauderSequence = 5;

                // 晋升文本
                Main.NewText("思维的边界开始模糊，你听到了周围无数梦境的低语...", 100, 100, 255);
                Main.NewText("晋升成功：序列5 窃梦家！", 147, 112, 219);
                Main.NewText("获得能力：【窃取意图】(攻击致使敌人呆滞) 与 【盗天机】(战斗中窃取增益)", 200, 200, 255);

                return true;
            }

            return true;
        }

        public override void AddRecipes()
        {
            // 使用双配方系统：恶魔祭坛 + 亵渎之石 OR 恋人牌
            CreateDualRecipe(
                ModContent.ItemType<LoversCard>(), // 核心材料：恋人牌
                (ItemID.BottledWater, 1),
                (ItemID.BlackLens, 1),    // 食梦鼠心脏 -> 黑晶状体 (符合梦境主题)
                (ItemID.SoulofNight, 5),  // 堕落之气 -> 暗影之魂
                (ItemID.Sapphire, 1),     // 天青石 -> 蓝宝石 (对应思维/梦境)
                (ItemID.Moonglow, 3)      // 薰衣草 -> 月光草
            );
        }
    }
}