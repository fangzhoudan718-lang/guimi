using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using zhashi.Content.Items.Accessories; // 引用恋人牌

namespace zhashi.Content.Items.Potions.Marauder
{
    public class ParasitePotion : LotMItem
    {
        public override string Pathway => "Marauder";

        // 逻辑修正：序列4魔药需要序列5才能喝
        public override int RequiredSequence => 5;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.rare = ItemRarityID.Yellow; // 半神级别，通常用黄色表示
            Item.value = Item.sellPrice(gold: 30);
        }

        // 1. 显示仪式进度 (半神仪式很重要)
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);

            var mp = Main.LocalPlayer.GetModPlayer<LotMPlayer>();

            // 只有当前是序列5时才显示仪式进度
            if (mp.baseMarauderSequence == 5)
            {
                // 动态变色：完成变绿，未完成变红
                string color = (mp.parasiteRitualProgress >= LotMPlayer.PARASITE_RITUAL_TARGET) ? "00FF00" : "FF0000";

                tooltips.Add(new TooltipLine(Mod, "Ritual",
                    $"[c/{color}:仪式进度: 窃取 {mp.parasiteRitualProgress} / {LotMPlayer.PARASITE_RITUAL_TARGET} 次非凡特性]"));

                tooltips.Add(new TooltipLine(Mod, "RitualDesc",
                    "仪式要求：从强敌身上窃取非凡特性（或贵重物品）。"));
            }
        }

        // 2. 检查仪式是否完成
        public override bool CanUseItem(Player player)
        {
            // 基类先检查序列是否为 5
            if (!base.CanUseItem(player)) return false;

            var mp = player.GetModPlayer<LotMPlayer>();

            // 检查仪式进度
            if (mp.baseMarauderSequence == 5 && mp.parasiteRitualProgress < LotMPlayer.PARASITE_RITUAL_TARGET)
            {
                int remaining = LotMPlayer.PARASITE_RITUAL_TARGET - mp.parasiteRitualProgress;
                Main.NewText($"仪式未完成：你需要再通过“窃取” {remaining} 次强敌的物品，以此作为供养。", 255, 100, 100);
                return false;
            }

            return true;
        }

        public override bool? UseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<LotMPlayer>();

            // 晋升逻辑
            modPlayer.baseMarauderSequence = 4;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position); // 晋升时的咆哮

            Main.NewText("你感到身体在分解，无数微小的虫豸重组了你的血肉...", 175, 238, 238);
            Main.NewText("你晋升为 序列4：寄生者 (半神)！", 255, 215, 0);
            Main.NewText("获得能力：【寄生】(允许寄生于他人体内) 与 【概念窃取】", 255, 255, 0);

            return true;
        }

        public override void AddRecipes()
        {
            CreateDualRecipe(
                ModContent.ItemType<LoversCard>(), // 核心：恋人牌
                (ItemID.BottledWater, 1),
                (ItemID.Ectoplasm, 5),    // 夺魂怪死后的结晶
                (ItemID.TruffleWorm, 1),  // 傀儡邪虫
                (ItemID.Amethyst, 1),     // 紫水晶
                (ItemID.SoulofNight, 5),  // 被囚禁的灵魂
                (ItemID.Grapes, 1)        // 樱桃李 (用葡萄代替)
            );
        }
    }
}