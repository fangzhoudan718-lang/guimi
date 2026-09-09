using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用魔女牌 (如果有的话)

namespace zhashi.Content.Items.Potions.Demoness
{
    public class AssassinPotion : LotMItem
    {
        // 1. 定义途径：Demoness (对应原本的刺客/魔女途径)
        public override string Pathway => "Demoness";

        // 序列要求：0 代表无序列门槛 (凡人可服)
        public override int RequiredSequence => 0;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("刺客魔药");
            // Tooltip.SetDefault("序列9：刺客\n获得鹰的视力与黑暗中的潜行能力，身体变得轻盈致命。");
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
            Item.rare = ItemRarityID.Blue; // 序列9 蓝色稀有度
            Item.value = Item.sellPrice(silver: 50);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 300;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                LotMPlayer modPlayer = player.GetModPlayer<LotMPlayer>();

                // 【核心逻辑】检查是否已是非凡者 (防止多重途径)
                if (modPlayer.IsBeyonder)
                {
                    Main.NewText("你的灵性已定型，无法开启第二条途径，强行服用只会导致失控！", 255, 50, 50);
                    return true; // 消耗掉作为惩罚
                }

                // 晋升逻辑：开启刺客途径序列9
                modPlayer.baseDemonessSequence = 9;

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, player.position);

                // 黑色/深紫色的提示文字
                Main.NewText("你感到身体变得轻盈，眼前的黑暗似乎不再是阻碍...", 180, 100, 255);
                Main.NewText("晋升成功！序列9：刺客！", 50, 50, 50);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<DemonessCard>(), // 核心：魔女牌

                (ItemID.BottledWater, 1), // 基础：水瓶
                (ItemID.SpecularFish, 1), // 阴影枪鱼 -> 镜面鱼 (拥有特殊的视觉)
                (ItemID.Stinger, 2),      // 双尾毒蛇毒液 -> 毒刺 (致命毒素)
                (ItemID.Deathweed, 1),    // 黑夜精油 -> 死亡草 (黑夜与死亡气息)
                (ItemID.Waterleaf, 1)     // 安达曼纯露 -> 水叶草 (平静与水)
            );
        }
    }
}