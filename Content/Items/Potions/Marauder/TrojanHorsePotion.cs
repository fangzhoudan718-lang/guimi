using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用恋人牌

namespace zhashi.Content.Items.Potions.Marauder
{
    public class TrojanHorsePotion : LotMItem
    {
        public override string Pathway => "Marauder";

        // 序列要求：需要序列3 (欺瞒导师)
        public override int RequiredSequence => 3;

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.rare = ItemRarityID.Red; // 序列2 天使 (红色)
            Item.value = Item.sellPrice(platinum: 1); // 价值连城
        }

        // 1. 显示仪式进度 (天使晋升仪式)
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var mp = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            if (mp.baseMarauderSequence == 3)
            {
                // 计算秒数
                int currentSec = mp.trojanRitualTimer / 60;
                int targetSec = LotMPlayer.TROJAN_RITUAL_TARGET / 60;

                string color = (mp.trojanRitualTimer >= LotMPlayer.TROJAN_RITUAL_TARGET) ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "Ritual",
                    $"[c/{color}:仪式进度: 寄生时长 {currentSec} / {targetSec} 秒]"));

                tooltips.Add(new TooltipLine(Mod, "RitualDesc",
                    "仪式要求：长时间维持【寄生】状态，取代命运。"));
            }
        }

        // 2. 仪式检查
        public override bool CanUseItem(Player player)
        {
            // 基类检查是否为序列3
            if (!base.CanUseItem(player)) return false;

            var mp = player.GetModPlayer<LotMPlayer>();

            // 检查仪式时间
            if (mp.baseMarauderSequence == 3 && mp.trojanRitualTimer < LotMPlayer.TROJAN_RITUAL_TARGET)
            {
                int secondsLeft = (LotMPlayer.TROJAN_RITUAL_TARGET - mp.trojanRitualTimer) / 60;
                Main.NewText($"仪式未完成：你需要寄生并顶替他人身份更长时间... (剩余 {secondsLeft} 秒)", 255, 50, 50);
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升逻辑
            modPlayer.baseMarauderSequence = 2;

            // 音效与文本
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);
            Main.NewText("你感觉自己沉入了命运的长河，无数的可能性在眼前展开...", 175, 238, 238);
            Main.NewText("晋升成功：序列2 命运木马 (天使)！", 255, 215, 0);

            // 获得短暂无敌 (适应高维状态)
            player.immune = true;
            player.immuneTime = 300; // 5秒无敌

            return true;
        }

        public override void AddRecipes()
        {
            // 使用双配方：支持 石板 或 恋人牌
            CreateDualRecipe(
                ModContent.ItemType<LoversCard>(), // 核心：恋人牌
                (ItemID.BottledWater, 1),
                (ItemID.Ectoplasm, 10),            // 灵性材料
                (ItemID.SoulofLight, 10),          // 光之魂
                (ItemID.SoulofNight, 10),          // 暗之魂
                (ItemID.TruffleWorm, 1),           // 灵之虫 -> 松露虫 (稀有虫类)
                (ItemID.EnchantedNightcrawler, 1), // 星之虫 -> 附魔夜行者
                (ItemID.HolyWater, 5)              // 古老河水 -> 圣水
            );
        }
    }
}