using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;
using zhashi.Content.Projectiles;

namespace zhashi.Content.Items.Potions.Wheel
{
    /// <summary>
    /// 命运途径 序列1：巨蛇 (Serpent of Mercury) - 水银之蛇 / 吞尾之蛇 / 命运之蛇
    /// 前置序列：2 先知
    /// 晋升仪式：在序列2状态下,触发命运启示5次 + 击败月亮领主
    /// </summary>
    public class SerpentPotion : LotMItem
    {
        public override string Pathway => "Wheel";
        public override int RequiredSequence => 2;

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Red; // 序列1 从神/天使之王 (红色)
            Item.value = Item.sellPrice(gold: 100);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            if (modPlayer.baseWheelSequence == 2)
            {
                string col1 = modPlayer.serpentRitualProgress >= LotMPlayer.SERPENT_RITUAL_TARGET ? "00FF00" : "FFFF00";
                string col2 = modPlayer.serpentRitualBeatMoonLord ? "00FF00" : "FF5555";
                string statusText = modPlayer.serpentRitualComplete ? "已完成" : "未完成";

                tooltips.Add(new TooltipLine(Mod, "RitualDesc",
                    "晋升仪式：在序列2状态下，触发命运启示 5 次，并击败月亮领主。"));
                tooltips.Add(new TooltipLine(Mod, "RitualP1",
                    $"[c/{col1}:启示次数: {modPlayer.serpentRitualProgress} / {LotMPlayer.SERPENT_RITUAL_TARGET}]"));
                tooltips.Add(new TooltipLine(Mod, "RitualP2",
                    $"[c/{col2}:月亮领主: {(modPlayer.serpentRitualBeatMoonLord ? "已击败" : "未击败")}]"));
                tooltips.Add(new TooltipLine(Mod, "RitualS",
                    $"[c/FFFFFF:仪式总状态: {statusText}]"));
            }
        }

        public override bool CanUseItem(Player player)
        {
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            if (!modPlayer.serpentRitualComplete)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText("仪式未完成: 需先在序列2状态下触发5次启示并击败月亮领主。", 255, 50, 50);
                }
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return true;

            var modPlayer = player.GetModPlayer<LotMPlayer>();
            modPlayer.baseWheelSequence = 1;
            modPlayer.currentWheelSequence = 1;

            SoundEngine.PlaySound(SoundID.Roar, player.position);
            SoundEngine.PlaySound(SoundID.Item104, player.position);
            Main.NewText("你看到了所有过去与所有未来，命运长河收束为一条银白色的巨蛇,在你身周盘绕...", 200, 200, 255);
            Main.NewText("晋升成功：序列1 巨蛇 — 水银之蛇,吞尾之蛇,命运之蛇。", 50, 255, 50);
            Main.NewText("能力：【命运循环 Y】| 【重启循环 H 自动死亡复活】| 【水银相位 10秒/3秒无敌】", 255, 255, 255);


            return true;
        }

        public override void AddRecipes()
        {
            // 序列1 用料: 月亮领主后的所有顶级材料
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.LunarBar, 25),              // 月光锭 ×25
                (ItemID.FragmentNebula, 15),        // 星云碎片 ×15
                (ItemID.FragmentVortex, 15),        // 漩涡碎片 ×15
                (ItemID.FragmentSolar, 15),        // 日耀碎片 ×15
                (ItemID.FragmentStardust, 15),      // 星尘碎片 ×15
                (ItemID.Ectoplasm, 30),             // 灵气 ×30
                (ItemID.LifeFruit, 5),              // 生命果 ×5
                (ItemID.HallowedBar, 30)            // 神圣锭 ×30
            );
        }
    }
}
