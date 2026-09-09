using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用月亮牌

namespace zhashi.Content.Items.Potions.Moon
{
    public class LifeGiverPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Moon";
        public override int RequiredSequence => 3; // 需要序列3 (召唤大师)

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 38;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Purple; // 序列2 天使 (紫色)
            Item.value = Item.sellPrice(platinum: 1);
        }

        // 2. 显示仪式条件 (濒死状态)
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseMoonSequence == 3)
            {
                // 计算当前血量百分比
                float hpPercent = (float)Main.LocalPlayer.statLife / Main.LocalPlayer.statLifeMax2;
                bool isDying = hpPercent <= 0.05f; // 5% 以下

                string statusColor = isDying ? "00FF00" : "FF0000";
                string statusText = isDying ? "已满足" : $"未满足 (当前 {hpPercent:P0})";

                tooltips.Add(new TooltipLine(Mod, "RitualCond",
                    $"[c/{statusColor}:仪式条件: 濒死状态 (血量 < 5%) - {statusText}]"));

                tooltips.Add(new TooltipLine(Mod, "LoreHint", "提示: 置之死地而后生，唯有接近死亡才能领悟生命的真谛。"));
            }
        }

        // 3. 使用条件检查
        public override bool CanUseItem(Player player)
        {
            // 基类先检查序列
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseMoonSequence == 3)
            {
                // 检查仪式：血量必须低于 5%
                if (player.statLife > player.statLifeMax2 * 0.05f)
                {
                    if (player.whoAmI == Main.myPlayer)
                    {
                        Main.NewText($"仪式未完成：你的生命力过于旺盛 ({player.statLife}/{player.statLifeMax2})。", 255, 50, 50);
                        Main.NewText("你需要将生命值降至 5% 以下才能服食。", 200, 200, 200);
                    }
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
            modPlayer.baseMoonSequence = 2;

            // 【核心效果】创生者：瞬间满血复活
            int healAmount = player.statLifeMax2 - player.statLife;
            player.statLife = player.statLifeMax2;
            player.HealEffect(healAmount);

            // 视觉与音效
            CombatText.NewText(player.getRect(), Color.LightGreen, "晋升：创生者！", true);
            Main.NewText("你感受到了万物滋长的喜悦，你即是生命本身。", 50, 255, 50); // 生命绿

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, player.position);

            // 特效：绿色的生命光辉爆发
            for (int i = 0; i < 150; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(20, 20);
                // 使用 Terra (泰拉) 或 Regen (回复) 粒子
                Dust d = Dust.NewDustPerfect(player.Center, DustID.TerraBlade, speed, 0, default, 2.5f);
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
                (ItemID.LunarBar, 5),          // 夜明锭 (月亮)
                (ItemID.FragmentSolar, 10),    // 四柱碎片 (创世元素)
                (ItemID.FragmentVortex, 10),
                (ItemID.FragmentNebula, 10),
                (ItemID.FragmentStardust, 10)
            );
        }
    }
}