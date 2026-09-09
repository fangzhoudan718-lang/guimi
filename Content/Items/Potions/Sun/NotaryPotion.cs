using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用太阳牌

namespace zhashi.Content.Items.Potions.Sun
{
    public class NotaryPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Sun";
        public override int RequiredSequence => 7; // 需要序列7 (太阳神官)

        public override void SetStaticDefaults()
        {
            // 名字和描述在 HJSON 中
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
            Item.rare = ItemRarityID.Pink; // 序列6 粉色
            Item.value = Item.sellPrice(gold: 5);
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 晋升
                modPlayer.baseSunSequence = 6;
                // modPlayer.currentSunSequence = 6; // 可选：手动同步

                // 文本反馈
                Main.NewText("你感到契约的力量在体内凝结，你拥有了公证有效性的权柄！", 255, 215, 0); // 金色
                Main.NewText("晋升成功：序列6 公证人！", 255, 215, 0);
                Main.NewText("获得能力：【公证】(强化Buff效果) | 【光之利刃】", 255, 255, 200);

                // 音效：Item29 是魔法/神圣相关的声音
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, player.position);
            }
            return true;
        }

        // 3. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<SunCard>(), // 核心：太阳牌
                (ItemID.BottledWater, 1),
                (ItemID.HallowedBar, 5),   // 神圣锭 (神圣)
                (ItemID.SoulofFlight, 3),  // 飞翔之魂 (天空/太阳)
                (ItemID.PixieDust, 10),    // 妖精尘 (光尘)
                (ItemID.Sunflower, 2)      // 向日葵
            );
        }
    }
}