using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用太阳牌

namespace zhashi.Content.Items.Potions.Sun
{
    public class SolarHighPriestPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Sun";
        public override int RequiredSequence => 8; // 需要序列8 (祈光人)

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
            Item.rare = ItemRarityID.Orange; // 序列7 橙色
            Item.value = Item.buyPrice(gold: 5);
            Item.buffType = BuffID.WellFed;
            Item.buffTime = 3600;
        }

        // 2. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var p = player.GetModPlayer<LotMPlayer>();

                // 晋升
                p.baseSunSequence = 7;
                // p.currentSunSequence = 7; // 可选：手动同步

                // 文本反馈
                Main.NewText("你感受到了太阳的威严，晋升为序列7：太阳神官！", 255, 215, 0); // 太阳金

                // 音效：Item29 是魔法/神圣相关的声音
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, player.position);

                // 特效：生成金色火焰粒子
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDust(player.position, player.width, player.height, DustID.GoldFlame, 0, 0, 100, default, 1.5f);
                }
            }
            return true;
        }

        // 3. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<SunCard>(), // 核心：太阳牌
                (ItemID.BottledWater, 1),
                (ItemID.Fireblossom, 3),   // 火焰花
                (ItemID.HellstoneBar, 5),  // 狱岩锭 (高热)
                (ItemID.HoneyBlock, 5),    // 蜂蜜块 (金色/神圣感)
                (ItemID.LavaBucket, 1),    // 岩浆桶 (光与热)
                (ItemID.GoldBar, 2)        // 金锭 (尊贵)
            );
        }
    }
}