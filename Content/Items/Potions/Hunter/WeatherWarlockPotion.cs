using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用红祭司牌

namespace zhashi.Content.Items.Potions.Hunter
{
    public class WeatherWarlockPotion : LotMItem
    {
        // 1. 定义途径和前置序列
        public override string Pathway => "Hunter";
        public override int RequiredSequence => 3; // 需要序列3 (战争主教)

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.rare = ItemRarityID.Red; // 序列2 天使 (红色)
            Item.value = Item.buyPrice(platinum: 5);
        }

        // 2. 显示仪式进度 (天气共鸣)
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var modPlayer = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有序列3才显示仪式
            if (modPlayer.baseHunterSequence == 3)
            {
                // 检测是否永久完成
                string statusColor = modPlayer.weatherRitualComplete ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "RitualDesc", "晋升仪式: 使用[气象符文]，连续触发 10 次天气共鸣。"));
                tooltips.Add(new TooltipLine(Mod, "RitualProgress", $"[c/{statusColor}:仪式状态: {(modPlayer.weatherRitualComplete ? "已完成" : "未完成")}]"));

                // 如果没完成，显示当前次数
                if (!modPlayer.weatherRitualComplete && modPlayer.weatherRitualCount > 0)
                {
                    tooltips.Add(new TooltipLine(Mod, "RitualCount", $"当前共鸣: {modPlayer.weatherRitualCount}/10"));
                }
            }
        }

        // 3. 使用条件检查
        public override bool CanUseItem(Player player)
        {
            // 基类先检查是否是序列3
            if (!base.CanUseItem(player)) return false;

            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 检查仪式是否完成 (weatherRitualComplete 是一个永久标记，只要达成过10次连击就会为true)
            if (modPlayer.baseHunterSequence == 3 && !modPlayer.weatherRitualComplete)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText("你还未展现出对天气的绝对掌控... (需连续使用符文10次)", 255, 50, 50);
                }
                return false;
            }

            return true;
        }

        // 4. 晋升逻辑
        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升
            modPlayer.baseHunterSequence = 2;

            // 音效与文本
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);

            Main.NewText("风暴、雷电、迷雾听从你的号令！", 0, 255, 255); // 天气蓝
            Main.NewText("晋升成功：序列2 天气术士 (天使)！", 0, 255, 255);

            // 晋升后可以重置仪式状态（虽然玩家已经是序列2了，但这是好习惯）
            modPlayer.weatherRitualComplete = false;
            modPlayer.weatherRitualCount = 0;

            return true;
        }

        // 5. 双配方支持
        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<RedPriestCard>(), // 核心：红祭司牌
                (ItemID.BottledWater, 1),
                (ItemID.FragmentSolar, 10),   // 日耀 (火灾/旱灾)
                (ItemID.FragmentVortex, 10),  // 星旋 (风暴)
                (ItemID.FragmentNebula, 10),  // 星云 (迷雾/神秘)
                (ItemID.FragmentStardust, 10) // 星尘 (异象)
            );
        }
    }
}