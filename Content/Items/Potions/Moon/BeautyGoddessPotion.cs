using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用月亮牌

namespace zhashi.Content.Items.Potions.Moon
{
    public class BeautyGoddessPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Moon";
        public override int RequiredSequence => 2; // 需要序列2 (创生者)

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 38;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Purple; // 序列1 天使之王 (紫色)
            Item.value = Item.sellPrice(platinum: 20);
        }

        // 2. 显示仪式条件
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseMoonSequence == 2)
            {
                // 检测仪式状态 (实时)
                bool isSleeping = Main.LocalPlayer.sleeping.isSleeping;
                bool isUnderground = Main.LocalPlayer.ZoneRockLayerHeight || Main.LocalPlayer.ZoneDirtLayerHeight; // 只要是地下即可

                string condColor = (isSleeping && isUnderground) ? "00FF00" : "FF0000";

                string sleepText = isSleeping ? "正在沉睡" : "需在床上沉睡";
                string zoneText = isUnderground ? "身处地底" : "需深埋地下";

                tooltips.Add(new TooltipLine(Mod, "RitualCond",
                    $"[c/{condColor}:仪式条件: {sleepText} + {zoneText}]"));

                tooltips.Add(new TooltipLine(Mod, "LoreHint", "[c/FF69B4:提示: 模拟棺椁中的重生，在黑暗的地底沉睡...]"));
            }
        }

        // 3. 使用条件检查
        public override bool CanUseItem(Player player)
        {
            // 基类先检查序列
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseMoonSequence == 2)
            {
                // 检查仪式：地底 + 睡觉
                // 注：在泰拉瑞亚中，躺在床上时使用物品可能会打断睡眠，但 CanUseItem 会先判定。
                // 建议操作：躺在床上时，直接把药水拿在手上按左键。
                if (!player.sleeping.isSleeping || !(player.ZoneRockLayerHeight || player.ZoneDirtLayerHeight))
                {
                    if (player.whoAmI == Main.myPlayer)
                        Main.NewText("仪式条件未满足：你需要如尸体般深埋地下(岩石层)，并在黑暗中沉睡(躺在床上)。", 255, 50, 50);
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
            modPlayer.baseMoonSequence = 1;

            // 【核心设定】强制转变为女性 (美神权柄)
            if (player.Male)
            {
                player.Male = false;
                if (player.whoAmI == Main.myPlayer) Main.NewText("你的形态发生了本质的改变...", 255, 182, 193);
            }

            // 文本与音效
            CombatText.NewText(player.getRect(), Color.HotPink, "晋升：美神！", true);
            Main.NewText("世间的一切光辉都汇聚于你，万物因你的容颜而屏息。", 255, 105, 180); // 亮粉色

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item123, player.position); // 魔法音效

            // 美神晋升特效：粉色星云爆发
            for (int i = 0; i < 200; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(20, 20);
                // 使用 Nebula (星云) 粒子，代表神秘与美
                Dust d = Dust.NewDustPerfect(player.Center, DustID.PinkTorch, speed, 0, default, 4f);
                d.noGravity = true;

                if (i % 2 == 0)
                {
                    Dust d2 = Dust.NewDustPerfect(player.Center, DustID.VampireHeal, speed * 0.8f, 0, default, 2f);
                    d2.noGravity = true;
                }
            }

            return true;
        }

        // 5. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<MoonCard>(), // 核心：月亮牌
                (ItemID.BottledWater, 1),
                (ItemID.LunarBar, 20),         // 夜明锭 (月亮)
                (ItemID.FragmentNebula, 10),   // 星云碎片 (魔法/月亮)
                (ItemID.FragmentSolar, 10),    // 日耀
                (ItemID.FragmentStardust, 10), // 星尘
                (ItemID.FragmentVortex, 10),   // 星旋
                (ItemID.LifeCrystal, 5)        // 生命水晶 (生命/心脏)
            );
        }
    }
}