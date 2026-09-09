using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用太阳牌

namespace zhashi.Content.Items.Potions.Sun
{
    public class BardPotion : LotMItem
    {
        // 1. 定义途径
        public override string Pathway => "Sun";

        // 序列要求：0 代表无序列门槛 (凡人可服)
        public override int RequiredSequence => 0;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useTurn = true;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue; // 序列9 蓝色
            Item.value = Item.buyPrice(silver: 10);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 3600;
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

                // 晋升逻辑：太阳途径
                modPlayer.baseSunSequence = 9;

                // 播放音效
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position); // 升级音效

                Main.NewText("你感觉喉咙变得灼热，心中充满了勇气！", 255, 215, 0); // 太阳金色
                Main.NewText("晋升成功：序列9 歌颂者！", 255, 215, 0);
                Main.NewText("获得能力：【歌颂】(光照与Buff)", 255, 255, 200);
            }
            return true;
        }

        public override void AddRecipes()
        {
            // 使用双配方：支持 石板 或 太阳牌
            CreateDualRecipe(
                ModContent.ItemType<SunCard>(), // 核心：太阳牌
                (ItemID.BottledWater, 1),
                (ItemID.Sunflower, 1),  // 向日葵 (太阳象征)
                (ItemID.Feather, 1),    // 羽毛 (天使/光)
                (ItemID.Ale, 1),        // 麦酒 (歌颂/勇气)
                (ItemID.Daybloom, 1),   // 太阳花
                (ItemID.Moonglow, 1)    // 月光草 (平衡)
            );
        }
    }
}