using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用月亮牌

namespace zhashi.Content.Items.Potions.Moon
{
    public class WitchKingPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Moon";
        public override int RequiredSequence => 5; // 需要序列5 (深红学者)

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 34;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Yellow; // 序列4 半神 (黄色)
            Item.value = Item.sellPrice(gold: 20);
        }

        // 2. 显示仪式条件 (满月之夜)
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseMoonSequence == 5)
            {
                // 检查时间 (夜晚) 和 月相 (0 = 满月)
                bool isNight = !Main.dayTime;
                bool isFullMoon = Main.moonPhase == 0;

                string condColor = (isNight && isFullMoon) ? "00FF00" : "FF0000";

                string timeText = isNight ? "夜晚" : "需夜晚";
                string moonText = isFullMoon ? "满月" : "需满月";

                tooltips.Add(new TooltipLine(Mod, "RitualCond",
                    $"[c/{condColor}:仪式条件: {timeText} + {moonText}]"));

                // 提示：如果在夜晚但不是满月，告诉玩家还要等多久没太大必要，直接显示"需满月"即可
            }
        }

        // 3. 使用条件检查
        public override bool CanUseItem(Player player)
        {
            // 基类先检查序列
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseMoonSequence == 5)
            {
                // 1. 检查夜晚
                if (Main.dayTime)
                {
                    if (player.whoAmI == Main.myPlayer)
                        Main.NewText("仪式失败：必须在夜晚进行。", 255, 50, 50);
                    return false;
                }

                // 2. 检查满月 (MoonPhase 0 is Full Moon)
                if (Main.moonPhase != 0)
                {
                    if (player.whoAmI == Main.myPlayer)
                        Main.NewText("仪式失败：必须在满月之夜进行 (当前月相不符)。", 255, 50, 50);
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
            modPlayer.baseMoonSequence = 4;

            // 文本反馈
            CombatText.NewText(player.getRect(), Color.DarkViolet, "晋升：巫王！", true);
            Main.NewText("你感受到了来自月亮最深处的黑暗...", 180, 0, 255); // 深紫
            Main.NewText("仪式成功：满月见证了你的加冕。", 255, 255, 255);

            // 音效
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item119, player.position);

            // 半神晋升特效：黑暗月光
            for (int i = 0; i < 100; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(15, 15);
                // Shadowflame (暗影焰) 或 PurpleCrystalShard (紫水晶)
                Dust d = Dust.NewDustPerfect(player.Center, DustID.Shadowflame, speed, 0, default, 2f);
                d.noGravity = true;
            }

            return true;
        }

        // 5. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<MoonCard>(), // 核心：月亮牌
                (ItemID.BottledWater, 1),
                (ItemID.BrokenBatWing, 1),  // 蝙蝠翅膀 (夜之眷属)
                (ItemID.Ectoplasm, 10),     // 灵气
                (ItemID.DarkShard, 3),      // 暗黑碎片
                (ItemID.SoulofNight, 15),   // 暗影之魂
                (ItemID.MoonStone, 1)       // 月亮石 (核心材料)
            );
        }
    }
}