using System.Collections.Generic;
using Microsoft.Xna.Framework; // 用于特效 Vector2
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用太阳牌

namespace zhashi.Content.Items.Potions.Sun
{
    public class LightSeekerPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Sun";
        public override int RequiredSequence => 3; // 需要序列3 (正义导师)

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 40;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Red; // 序列2 天使 (红色)
            Item.value = Item.sellPrice(platinum: 5);
        }

        // 2. 显示仪式条件
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有序列3才显示详细仪式
            if (modPlayer.baseSunSequence == 3)
            {
                // 环境与状态检测 (实时)
                bool isDay = Main.dayTime;
                bool hasArtifact = Main.LocalPlayer.HasItem(ItemID.DayBreak) || Main.LocalPlayer.HasItem(ItemID.SolarEruption);
                bool isHealthy = Main.LocalPlayer.statLife >= Main.LocalPlayer.statLifeMax2;

                string condColor = (isDay && hasArtifact && isHealthy) ? "00FF00" : "FF0000";

                string dayText = isDay ? "正午" : "需白天";
                string itemText = hasArtifact ? "圣物" : "缺日耀圣物(破晓/日耀喷发)";
                string hpText = isHealthy ? "状态完好" : "需满血";

                tooltips.Add(new TooltipLine(Mod, "RitualCond",
                    $"[c/{condColor}:仪式条件: {dayText} + {itemText} + {hpText}]"));
            }
        }

        // 3. 使用条件检查
        public override bool CanUseItem(Player player)
        {
            // 基类先检查序列
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseSunSequence == 3)
            {
                bool isDay = Main.dayTime;
                bool hasArtifact = player.HasItem(ItemID.DayBreak) || player.HasItem(ItemID.SolarEruption);
                bool isHealthy = player.statLife >= player.statLifeMax2;

                if (!isDay || !hasArtifact || !isHealthy)
                {
                    if (player.whoAmI == Main.myPlayer)
                        Main.NewText("仪式条件未满足：需在烈日下(白天)、携带日耀圣物，并保持完美状态(满血)。", 255, 50, 50);
                    return false;
                }
            }
            return true;
        }

        // 4. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升
            modPlayer.baseSunSequence = 2;

            // 音效
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, player.position);

            Main.NewText("你将核心的正义与太阳的权柄捆绑，化作了一道纯净的光...", 255, 255, 0); // 纯光黄
            Main.NewText("晋升天使！序列2：逐光者。", 255, 69, 0); // 神性红

            // 天使晋升特效：日耀爆发
            for (int i = 0; i < 200; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(20, 20);
                Dust d = Dust.NewDustPerfect(player.Center, DustID.SolarFlare, speed, 0, default, 4f);
                d.noGravity = true;
            }

            return true;
        }

        // 5. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<SunCard>(), // 核心：太阳牌
                (ItemID.BottledWater, 1),
                (ItemID.FragmentSolar, 20), // 日耀碎片 (太阳神性)
                (ItemID.SoulofLight, 20),   // 光之魂 (纯净)
                (ItemID.LifeFruit, 5),      // 生命果 (生命力)
                (ItemID.Ectoplasm, 10),     // 灵气
                (ItemID.Book, 1)            // 书 (契约/知识)
            );
        }
    }
}