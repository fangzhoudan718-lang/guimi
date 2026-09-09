using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using zhashi.Content.Items.Accessories;
using zhashi.Content.Items.SealedArtifacts; // 引用封印物命名空间
using zhashi.Content.Buffs.Debuffs;

namespace zhashi.Content.Items.Potions.Wheel
{
    public class WinnerPotion : LotMItem
    {
        public override string Pathway => "Wheel";
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
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(gold: 10);
            Item.buffType = BuffID.Lucky;
            Item.buffTime = 3600;
        }

        // 仪式检查：检查计时器是否达到一天 (86400帧)
        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 检查佩戴时间是否足够
            if (modPlayer.misfortuneRitualTimer < LotMPlayer.MISFORTUNE_RITUAL_TARGET)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    // 计算还需要多久
                    int remainingSeconds = (LotMPlayer.MISFORTUNE_RITUAL_TARGET - modPlayer.misfortuneRitualTimer) / 60;
                    Main.NewText($"仪式未完成！你必须佩戴【灰色四叶草】经历完整的磨难。", 255, 50, 50);
                    Main.NewText($"当前进度: {modPlayer.misfortuneRitualTimer / 60}秒 / 1440秒 (还需 {remainingSeconds} 秒)", 255, 100, 100);
                }
                return false;
            }

            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<LotMPlayer>();

                // 晋升
                modPlayer.baseWheelSequence = 5;
                modPlayer.currentWheelSequence = 5;

                // 【消耗仪式进度】
                // 晋升成功后，重置计时器，防止重复利用
                modPlayer.misfortuneRitualTimer = 0;

                // 音效与特效
                SoundEngine.PlaySound(SoundID.Item29, player.position);

                Main.NewText("你已在厄运中证明了自身的强韧...", 255, 215, 0);
                Main.NewText("晋升成功：序列5 赢家！", 255, 215, 0);
                Main.NewText("即使骰子掷出一点，你也能将其改写为六点。", 200, 200, 200);
            }
            return true;
        }

        // 动态显示进度条
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 计算百分比
            float progress = (float)modPlayer.misfortuneRitualTimer / LotMPlayer.MISFORTUNE_RITUAL_TARGET;
            int percentage = (int)(progress * 100);

            // 颜色：未完成是红色，完成是绿色
            string colorHex = (progress >= 1f) ? "00FF00" : "FF0000";
            string status = (progress >= 1f) ? "已完成" : "进行中";

            // 添加提示行
            tooltips.Add(new TooltipLine(Mod, "RitualReq",
                $"[c/FF0000:仪式要求: 佩戴封印物“灰色四叶草”经历一整天]"));

            tooltips.Add(new TooltipLine(Mod, "RitualProgress",
                $"[c/{colorHex}:仪式进度: {percentage}% ({modPlayer.misfortuneRitualTimer / 60}/1440秒) - {status}]"));
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<WheelOfFortuneCard>(),
                (ItemID.BottledWater, 1),
                (ItemID.LuckyCoin, 1),
                (ItemID.HallowedBar, 5),
                (ItemID.SoulofLight, 5),
                (ItemID.Diamond, 2)
            );
        }
    }
}